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
    private float elapsedTime = 0f;
    private bool timerRunning = false;
    private List<float> levelTimes = new List<float>();
    private TemplateData currentLevel;
    public TemplateData CurrentLevel => currentLevel;
    private Difficulty currentDifficulty;
    private Dictionary<Difficulty, List<TemplateData>> levelPools;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controlDisplay = FindAnyObjectByType<ControlDisplay>();
    }

    void OnEnable()
    {
        levelTimes.Clear();
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
    }

    void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        // Y button to reset current level
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            ResetLevel();
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
        Debug.Log("ABCD currentLevel difficulty is " + currentLevel);
        if (currentLevel == null)
        {
            HandleAllLevelsComplete();
            return;
        }

        LoadLevel();
    }

    void LoadLevel()
    {
        Debug.Log($"ABCD Loading level: {currentLevel.templateName} ({currentDifficulty})");

        spawner.templateData = currentLevel;
        spawner.BuildTemplate();

        SpawnBlocks();
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

        int totalLevels = easyLevels.Count + mediumLevels.Count + hardLevels.Count;
        controlDisplay?.ShowLevelComplete(
            levelTimes.Count,
            elapsedTime,
            totalLevels,
            levelTimes);

        if (completionSound != null && audioSource != null)
            audioSource.PlayOneShot(completionSound);

        StartCoroutine(AdvanceToNextLevel());
    }

    IEnumerator AdvanceToNextLevel()
    {
        // Lock all blocks
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
        TemplateData nextLevel = PickLevelFromDifficulty(currentDifficulty);
        string nextLevelName = nextLevel != null ? nextLevel.templateName : "None";
        AnalyticsManager.Instance.MovingToNextLevel(currentLevelName, nextLevelName, elapsedTime);

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

        for (int i = 0; i < levelTimes.Count; i++)
        {
            int mins = Mathf.FloorToInt(levelTimes[i] / 60f);
            int secs = Mathf.FloorToInt(levelTimes[i] % 60f);
            Debug.Log($"  Level {i + 1}: {mins:00}:{secs:00}");
        }

        controlDisplay?.ShowEndScreen(levelTimes);
        onAllLevelsComplete?.Invoke();
    }

    public void ResetLevel()
    {
        StopTimer();

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
        StartTimer();

        Debug.Log($"Level reset: {currentLevel?.templateName}");
    }

    Difficulty GetDifficultyForLevel(TemplateData level)
    {
        if (easyLevels.Contains(level)) return Difficulty.Easy;
        if (mediumLevels.Contains(level)) return Difficulty.Medium;
        return Difficulty.Hard;
    }
}
