using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Oculus.Interaction;

public class TemplateValidator : MonoBehaviour
{
    public TemplateSpawner spawner;
    public AudioClip completionSound;
    public float delayBeforeNextLevel = 2f;
    public UnityEvent onAllLevelsComplete;

    [Header("Levels")]
    public List<TemplateData> easyLevels = new List<TemplateData>();
    public List<TemplateData> mediumLevels = new List<TemplateData>();
    public List<TemplateData> hardLevels = new List<TemplateData>();
    public float totalTimeLimit = 900f;

    [Tooltip("Seconds allowed on a single level before automatically moving on. 0 = no limit.")]
    public float levelTimeLimit = 300f;

    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Block Spawning")]
    public GameObject blockBasePrefab;
    [System.Serializable]
    public class BlockSpawnInfo
    {
        public BlockData blockData;
        public Vector3 position;
    }
    public List<BlockSpawnInfo> blockSpawns = new List<BlockSpawnInfo>();

    private AudioSource audioSource;
    private ControlDisplay controlDisplay;
    private List<GameObject> activeBlocks = new List<GameObject>();
    private float elapsedTime = 0f;      // time on the current level
    private float totalElapsed = 0f;     // time across the whole session
    private bool timerRunning = false;
    private bool gameOver = false;
    private bool awaitingNextLevel = false;
    private List<float> levelTimes = new List<float>();
    private HashSet<int> timedOutLevels = new HashSet<int>(); // 1-based level numbers
    private TemplateData currentLevel;
    public TemplateData CurrentLevel => currentLevel;
    private Difficulty currentDifficulty;
    private Dictionary<Difficulty, List<TemplateData>> levelPools;

    // Session analytics
    private HashSet<TemplateData> seenLevels = new HashSet<TemplateData>();
    private bool sessionReported = false;

    // 0-based index of the level currently being played
    private int currentLevelIndex => levelTimes.Count;
    public int TotalLevelCount => easyLevels.Count + mediumLevels.Count + hardLevels.Count;
    public bool WasLevelTimedOut(int levelNumber) => timedOutLevels.Contains(levelNumber);

    // Levels finished by actually solving them (not by timing out)
    int CompletedLevelCount => levelTimes.Count - timedOutLevels.Count;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controlDisplay = FindAnyObjectByType<ControlDisplay>();
    }

    void OnEnable()
    {
        levelTimes.Clear();
        timedOutLevels.Clear();
        seenLevels.Clear();
        sessionReported = false;
        totalElapsed = 0f;
        gameOver = false;
        awaitingNextLevel = false;

        levelPools = new Dictionary<Difficulty, List<TemplateData>>
        {
            { Difficulty.Easy, new List<TemplateData>(easyLevels) },
            { Difficulty.Medium, new List<TemplateData>(mediumLevels) },
            { Difficulty.Hard, new List<TemplateData>(hardLevels) }
        };
        currentDifficulty = Difficulty.Easy;
        OscHandler.Instance?.ClearSignals();
        LoadNextLevel();
    }

    void OnDisable()
    {
        StopTimer();
        // Disabling a component does not stop its coroutines, so do it explicitly
        // (otherwise a pending AdvanceToNextLevel can fire after a restart).
        StopAllCoroutines();
    }

    void Update()
    {
        if (gameOver) return;

        totalElapsed += Time.deltaTime;

        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        if (totalTimeLimit > 0f && totalElapsed >= totalTimeLimit)
        {
            TriggerGameOver();
            return;
        }

        // Per-level limit: move on automatically after levelTimeLimit seconds
        if (levelTimeLimit > 0f && timerRunning && !awaitingNextLevel && elapsedTime >= levelTimeLimit)
        {
            LevelTimedOut();
            return;
        }

        // Y button to reset current level
        if (!awaitingNextLevel && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            ResetLevel();
    }

    void LevelTimedOut()
    {
        Debug.Log($"Level timed out after {levelTimeLimit}s: {currentLevel?.templateName} ({currentDifficulty})");

        StopTimer();
        elapsedTime = levelTimeLimit;
        UpdateTimerDisplay();

        levelTimes.Add(elapsedTime);
        timedOutLevels.Add(levelTimes.Count);

        controlDisplay?.ShowLevelTimedOut(levelTimes.Count, TotalLevelCount, levelTimes);

        StartCoroutine(AdvanceToNextLevel());
    }

    void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        StopTimer();
        StopAllCoroutines();

        DisableBlockInteraction();

        ReportSessionEnd("SessionTimeLimit");

        controlDisplay?.ShowGameOver(levelTimes, currentLevelIndex + 1, TotalLevelCount);
    }

    void ReportSessionEnd(string endReason)
    {
        if (sessionReported) return;
        sessionReported = true;

        Debug.Log($"Session ended ({endReason}): {totalElapsed:F1}s played, " +
                  $"{seenLevels.Count} puzzles seen, {CompletedLevelCount} completed");

        AnalyticsManager.Instance?.SessionEnded(totalElapsed, seenLevels.Count, CompletedLevelCount, endReason);
    }

    void StartTimer()
    {
        elapsedTime = 0f;
        timerRunning = true;
    }

    void StopTimer()
    {
        timerRunning = false;
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    TemplateData PickLevelFromDifficulty(Difficulty difficulty)
    {
        // Try the requested difficulty first
        if (levelPools.ContainsKey(difficulty) && levelPools[difficulty].Count > 0)
        {
            List<TemplateData> candidates = levelPools[difficulty];
            TemplateData picked = candidates[Random.Range(0, candidates.Count)];
            candidates.Remove(picked);
            return picked;
        }

        // Fall back to any difficulty that still has levels
        foreach (var kvp in levelPools)
        {
            if (kvp.Value.Count > 0)
            {
                TemplateData picked = kvp.Value[Random.Range(0, kvp.Value.Count)];
                kvp.Value.Remove(picked);
                return picked;
            }
        }

        return null;
    }

    void LoadNextLevel()
    {
        currentLevel = PickLevelFromDifficulty(currentDifficulty);
        if (currentLevel == null)
        {
            HandleAllLevelsComplete();
            return;
        }

        LoadLevel();
    }

    void LoadLevel()
    {
        Debug.Log($"Loading level: {currentLevel.templateName} ({currentDifficulty})");

        seenLevels.Add(currentLevel);

        spawner.templateData = currentLevel;
        spawner.BuildTemplate();

        SpawnBlocks();
        awaitingNextLevel = false;
        StartTimer();
    }

    void SpawnBlocks()
    {
        foreach (GameObject block in activeBlocks)
            if (block != null) Destroy(block);
        activeBlocks.Clear();

        foreach (var info in blockSpawns)
        {
            if (info.blockData == null || blockBasePrefab == null) continue;

            GameObject blockParent = Instantiate(blockBasePrefab, info.position, Quaternion.identity);
            blockParent.name = $"Block_{info.blockData.blockName}";
            blockParent.transform.localScale = Vector3.one;

            MeshRenderer mr = blockParent.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            Collider col = blockParent.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            BlockSpawner bs = blockParent.AddComponent<BlockSpawner>();
            bs.blockData = info.blockData;

            foreach (Vector3Int cell in info.blockData.cells)
            {
                Vector3 localPos = new Vector3(cell.x, cell.y, cell.z) * info.blockData.cellSize;

                GameObject cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cellObj.transform.SetParent(blockParent.transform);
                cellObj.transform.localPosition = localPos;
                cellObj.transform.localRotation = Quaternion.identity;
                cellObj.transform.localScale = Vector3.one * 0.1f;

                DestroyImmediate(cellObj.GetComponent<Collider>());

                Renderer r = cellObj.GetComponent<Renderer>();
                if (r != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = info.blockData.color;
                    r.material = mat;
                }
            }

            // Bounding box collider covering all cells
            Vector3Int bMin = info.blockData.cells[0];
            Vector3Int bMax = info.blockData.cells[0];
            foreach (Vector3Int cell in info.blockData.cells)
            {
                bMin = Vector3Int.Min(bMin, cell);
                bMax = Vector3Int.Max(bMax, cell);
            }

            Vector3 boundsSize = new Vector3(
                (bMax.x - bMin.x + 1) * info.blockData.cellSize,
                (bMax.y - bMin.y + 1) * info.blockData.cellSize,
                (bMax.z - bMin.z + 1) * info.blockData.cellSize
            );

            Vector3 boundsCenter = new Vector3(
                (bMin.x + bMax.x) * 0.5f * info.blockData.cellSize,
                (bMin.y + bMax.y) * 0.5f * info.blockData.cellSize,
                (bMin.z + bMax.z) * 0.5f * info.blockData.cellSize
            );

            BoxCollider physicsCol = blockParent.AddComponent<BoxCollider>();
            physicsCol.isTrigger = true;
            physicsCol.center = boundsCenter;
            physicsCol.size = boundsSize;

            activeBlocks.Add(blockParent);
        }
    }

    public void CheckCompletion()
    {
        // Ignore late snaps after a timeout / completion / game over
        if (gameOver || awaitingNextLevel) return;

        foreach (SnapSlot slot in spawner.spawnedSlots)
        {
            if (!slot.isOccupied)
            {
                Debug.Log($"Not complete - slot at {slot.gridPos} is not occupied");
                return;
            }
        }

        foreach (SnapSlot slot in spawner.spawnedSlots)
        {
            BlockSpawner occupant = slot.occupant;
            if (occupant != null && !occupant.FullyMatched)
            {
                Debug.Log($"Not complete - block '{occupant.name}' has cells off-template");
                return;
            }
        }

        Debug.Log($"Level complete: {currentLevel.templateName} ({currentDifficulty}). Time: {elapsedTime:F2}s");
        StopTimer();
        levelTimes.Add(elapsedTime);

        controlDisplay?.ShowLevelComplete(
            levelTimes.Count,
            elapsedTime,
            TotalLevelCount,
            levelTimes);

        if (completionSound != null && audioSource != null)
            audioSource.PlayOneShot(completionSound);

        StartCoroutine(AdvanceToNextLevel());
    }

    void DisableBlockInteraction()
    {
        foreach (GameObject block in activeBlocks)
        {
            if (block == null) continue;
            foreach (var mb in block.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (mb.GetType().Name.Contains("Interactable") ||
                    mb.GetType().Name.Contains("Grabbable"))
                    mb.enabled = false;
            }
        }
    }

    IEnumerator AdvanceToNextLevel()
    {
        awaitingNextLevel = true;
        DisableBlockInteraction();

        yield return new WaitForSeconds(delayBeforeNextLevel);

        // Only allow difficulty change if current difficulty has no remaining levels
        bool currentPoolExhausted = levelPools.ContainsKey(currentDifficulty) && levelPools[currentDifficulty].Count == 0;

        if (currentPoolExhausted && OscHandler.Instance != null)
        {
            currentDifficulty = OscHandler.Instance.GetRecommendedDifficulty(currentDifficulty);
        }

        OscHandler.Instance?.ClearSignals();

        // Peek at the next level for analytics before loading
        string currentLevelName = currentLevel != null ? currentLevel.templateName : "None";
        string currentLevelDifficulty = currentLevel != null ? GetDifficultyForLevel(currentLevel).ToString() : "None";
        TemplateData nextLevel = PickLevelFromDifficulty(currentDifficulty);
        string nextLevelName = nextLevel != null ? nextLevel.templateName : "None";
        // Use the level's actual pool rather than currentDifficulty, because
        // PickLevelFromDifficulty falls back to another pool when one runs out.
        string nextLevelDifficulty = nextLevel != null ? GetDifficultyForLevel(nextLevel).ToString() : "None";
        // The level that just ended is the last entry in levelTimes (1-based number = Count)
        bool currentLevelTimedOut = timedOutLevels.Contains(levelTimes.Count);
        AnalyticsManager.Instance?.MovingToNextLevel(
            currentLevelName, currentLevelDifficulty,
            nextLevelName, nextLevelDifficulty,
            elapsedTime, currentLevelTimedOut);

        if (nextLevel == null)
        {
            HandleAllLevelsComplete();
            yield break;
        }

        currentLevel = nextLevel;
        LoadLevel();
    }

    void HandleAllLevelsComplete()
    {
        Debug.Log("All levels complete!");
        gameOver = true;
        StopTimer();

        for (int i = 0; i < levelTimes.Count; i++)
        {
            int mins = Mathf.FloorToInt(levelTimes[i] / 60f);
            int secs = Mathf.FloorToInt(levelTimes[i] % 60f);
            string tag = timedOutLevels.Contains(i + 1) ? " (timed out)" : "";
            Debug.Log($"  Level {i + 1}: {mins:00}:{secs:00}{tag}");
        }

        ReportSessionEnd("AllLevelsFinished");

        controlDisplay?.ShowEndScreen(levelTimes);
        onAllLevelsComplete?.Invoke();
    }

    public void ResetLevel()
    {
        if (gameOver || awaitingNextLevel) return;

        foreach (GameObject block in activeBlocks)
            if (block != null) Destroy(block);
        activeBlocks.Clear();

        // Put the current level back into the correct pool
        if (currentLevel != null)
        {
            Difficulty levelDiff = GetDifficultyForLevel(currentLevel);
            if (!levelPools[levelDiff].Contains(currentLevel))
                levelPools[levelDiff].Add(currentLevel);
        }

        spawner.BuildTemplate();
        SpawnBlocks();
        // The level timer keeps running through a reset so the
        // per-level time limit can't be dodged by resetting.

        Debug.Log($"Level reset: {currentLevel?.templateName}");
    }

    Difficulty GetDifficultyForLevel(TemplateData level)
    {
        if (easyLevels.Contains(level)) return Difficulty.Easy;
        if (mediumLevels.Contains(level)) return Difficulty.Medium;
        return Difficulty.Hard;
    }
}