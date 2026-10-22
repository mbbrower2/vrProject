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

    [Header("Debug")]
    public bool debugLogs = true;
    [Tooltip("Seconds between periodic status logs. 0 = off.")]
    public float statusLogInterval = 10f;
    private float statusLogTimer = 0f;
    private const string LogTag = "[TemplateValidator]";

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

    // ---------- Debug helpers ----------

    void Log(string msg)
    {
        if (debugLogs) Debug.Log($"ABCD {LogTag} [f{Time.frameCount} t={Time.time:F2}] {msg}", this);
    }

    void LogWarn(string msg)
    {
        if (debugLogs) Debug.LogWarning($"ABCD {LogTag} [f{Time.frameCount} t={Time.time:F2}] {msg}", this);
    }

    string PoolSummary()
    {
        if (levelPools == null) return "(pools not built yet)";
        int e = levelPools.ContainsKey(Difficulty.Easy) ? levelPools[Difficulty.Easy].Count : -1;
        int m = levelPools.ContainsKey(Difficulty.Medium) ? levelPools[Difficulty.Medium].Count : -1;
        int h = levelPools.ContainsKey(Difficulty.Hard) ? levelPools[Difficulty.Hard].Count : -1;
        return $"Easy={e} Medium={m} Hard={h}";
    }

    string StateSummary()
    {
        int occupied = 0, total = 0;
        if (spawner != null)
        {
            total = spawner.spawnedSlots.Count;
            foreach (var s in spawner.spawnedSlots)
                if (s != null && s.isOccupied) occupied++;
        }
        return $"level='{currentLevel?.templateName}' diff={currentDifficulty} " +
               $"levelTime={elapsedTime:F1}s total={totalElapsed:F1}s timerRunning={timerRunning} " +
               $"awaitingNextLevel={awaitingNextLevel} gameOver={gameOver} enabled={enabled} " +
               $"slots={occupied}/{total} levelsDone={levelTimes.Count} pools[{PoolSummary()}]";
    }

    // ---------- Lifecycle ----------

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controlDisplay = FindAnyObjectByType<ControlDisplay>();

        int validatorCount = FindObjectsByType<TemplateValidator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        Log($"Awake on '{gameObject.name}' (id {GetInstanceID()}). " +
            $"spawner={(spawner != null ? spawner.name : "NULL")}, " +
            $"controlDisplay={(controlDisplay != null ? "found" : "NULL")}, " +
            $"audioSource={(audioSource != null ? "found" : "NULL")}, " +
            $"blockBasePrefab={(blockBasePrefab != null ? blockBasePrefab.name : "NULL")}, " +
            $"levels: easy={easyLevels.Count} medium={mediumLevels.Count} hard={hardLevels.Count}, " +
            $"TemplateValidators in scene={validatorCount}");

        if (validatorCount > 1)
            LogWarn($"There are {validatorCount} TemplateValidators in the scene. BlockSpawner uses FindAnyObjectByType, " +
                    "so CheckCompletion may be going to the wrong one.");
        if (spawner == null)
            Debug.LogError($"{LogTag} spawner is not assigned!", this);
    }

    void OnEnable()
    {
        Log($"OnEnable - starting a new session. OscHandler.Instance={(OscHandler.Instance != null ? "found" : "NULL")}, " +
            $"AnalyticsManager.Instance={(AnalyticsManager.Instance != null ? "found" : "NULL")}");

        levelTimes.Clear();
        timedOutLevels.Clear();
        seenLevels.Clear();
        sessionReported = false;
        totalElapsed = 0f;
        statusLogTimer = 0f;
        gameOver = false;
        awaitingNextLevel = false;

        levelPools = new Dictionary<Difficulty, List<TemplateData>>
        {
            { Difficulty.Easy, new List<TemplateData>(easyLevels) },
            { Difficulty.Medium, new List<TemplateData>(mediumLevels) },
            { Difficulty.Hard, new List<TemplateData>(hardLevels) }
        };
        currentDifficulty = Difficulty.Easy;
        Log($"Pools built: {PoolSummary()}");

        OscHandler.Instance?.ClearSignals();
        LoadNextLevel();
    }

    void OnDisable()
    {
        Log($"OnDisable - stopping timer and all coroutines. State: {StateSummary()}");
        StopTimer();
        // Disabling a component does not stop its coroutines, so do it explicitly
        // (otherwise a pending AdvanceToNextLevel can fire after a restart).
        StopAllCoroutines();
    }

    void Update()
    {
        if (debugLogs && statusLogInterval > 0f)
        {
            statusLogTimer += Time.deltaTime;
            if (statusLogTimer >= statusLogInterval)
            {
                statusLogTimer = 0f;
                Log($"Status: {StateSummary()}");
            }
        }

        if (gameOver) return;

        totalElapsed += Time.deltaTime;

        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        if (totalTimeLimit > 0f && totalElapsed >= totalTimeLimit)
        {
            Log($"Total time limit reached ({totalTimeLimit}s)");
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
        {
            Log("Y pressed - resetting level");
            ResetLevel();
        }
    }

    void LevelTimedOut()
    {
        Log($"Level timed out after {levelTimeLimit}s: {currentLevel?.templateName} ({currentDifficulty})");

        StopTimer();
        elapsedTime = levelTimeLimit;
        UpdateTimerDisplay();

        levelTimes.Add(elapsedTime);
        timedOutLevels.Add(levelTimes.Count);

        controlDisplay?.ShowLevelTimedOut(levelTimes.Count, TotalLevelCount, levelTimes);

        StartAdvance("LevelTimedOut");
    }

    void TriggerGameOver()
    {
        if (gameOver) { LogWarn("TriggerGameOver called but gameOver is already true"); return; }
        Log($"TriggerGameOver. State: {StateSummary()}");
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

        Log($"Session ended ({endReason}): {totalElapsed:F1}s played, " +
            $"{seenLevels.Count} puzzles seen, {CompletedLevelCount} completed");

        AnalyticsManager.Instance?.SessionEnded(totalElapsed, seenLevels.Count, CompletedLevelCount, endReason);
    }

    void StartTimer()
    {
        elapsedTime = 0f;
        timerRunning = true;
        Log("Level timer started");
    }

    void StopTimer()
    {
        if (timerRunning) Log($"Level timer stopped at {elapsedTime:F2}s");
        timerRunning = false;
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Picks a random level from the target difficulty. If that pool is empty,
    // falls back to the NEAREST difficulty that still has levels. When two pools
    // are equally near (Medium requested, both Easy and Hard available), it goes
    // in the direction the difficulty was moving: harder if target >= previous,
    // easier otherwise. pickedFrom is the pool the level actually came from.
    TemplateData PickLevelFromDifficulty(Difficulty target, Difficulty previous, out Difficulty pickedFrom)
    {
        pickedFrom = target;
        Log($"PickLevelFromDifficulty(target={target}, previous={previous}). Pools before: {PoolSummary()}");

        bool preferHarder = (int)target >= (int)previous;
        int min = (int)Difficulty.Easy;
        int max = (int)Difficulty.Hard;

        // Search order: target first, then outward by distance
        List<Difficulty> searchOrder = new List<Difficulty> { target };
        for (int dist = 1; dist <= max - min; dist++)
        {
            int harder = (int)target + dist;
            int easier = (int)target - dist;
            int first = preferHarder ? harder : easier;
            int second = preferHarder ? easier : harder;
            if (first >= min && first <= max) searchOrder.Add((Difficulty)first);
            if (second >= min && second <= max) searchOrder.Add((Difficulty)second);
        }

        foreach (Difficulty diff in searchOrder)
        {
            if (!levelPools.ContainsKey(diff) || levelPools[diff].Count == 0) continue;

            List<TemplateData> candidates = levelPools[diff];
            TemplateData picked = candidates[Random.Range(0, candidates.Count)];
            candidates.Remove(picked);
            pickedFrom = diff;

            if (picked == null)
                LogWarn($"Picked a NULL level from the {diff} pool - check for empty entries in the level lists");
            if (diff != target)
                LogWarn($"{target} pool is empty - fell back to nearest pool: {diff}");
            Log($"Picked '{picked?.templateName}' from {diff}. Pools after: {PoolSummary()}");
            return picked;
        }

        Log("All pools empty - no level picked");
        return null;
    }

    void LoadNextLevel()
    {
        Log("LoadNextLevel");
        currentLevel = PickLevelFromDifficulty(currentDifficulty, currentDifficulty, out Difficulty actualDifficulty);
        if (currentLevel == null)
        {
            Log("LoadNextLevel: no level returned -> HandleAllLevelsComplete");
            HandleAllLevelsComplete();
            return;
        }
        currentDifficulty = actualDifficulty;

        LoadLevel();
    }

    void LoadLevel()
    {
        Log($"Loading level: {currentLevel.templateName} ({currentDifficulty}), " +
            $"template slots={(currentLevel.slots != null ? currentLevel.slots.Length : 0)}");

        seenLevels.Add(currentLevel);

        spawner.templateData = currentLevel;
        spawner.BuildTemplate();
        Log($"Template built with {spawner.spawnedSlots.Count} slots");

        SpawnBlocks();
        awaitingNextLevel = false;
        StartTimer();
        Log($"Level loaded. State: {StateSummary()}");
    }

    void SpawnBlocks()
    {
        int destroyed = 0;
        foreach (GameObject block in activeBlocks)
            if (block != null) { Destroy(block); destroyed++; }
        activeBlocks.Clear();
        Log($"SpawnBlocks: destroyed {destroyed} old blocks, spawning {blockSpawns.Count} entries");

        foreach (var info in blockSpawns)
        {
            if (info.blockData == null || blockBasePrefab == null)
            {
                LogWarn($"Skipping block spawn entry: blockData={(info.blockData != null ? info.blockData.blockName : "NULL")}, " +
                        $"blockBasePrefab={(blockBasePrefab != null ? "ok" : "NULL")}");
                continue;
            }

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

        Log($"SpawnBlocks: {activeBlocks.Count} blocks active");
    }

    public void CheckCompletion()
    {
        Log($"CheckCompletion called on '{gameObject.name}' (id {GetInstanceID()}). State: {StateSummary()}");

        // Ignore late snaps after a timeout / completion / game over
        if (gameOver || awaitingNextLevel)
        {
            LogWarn($"CheckCompletion ignored: gameOver={gameOver}, awaitingNextLevel={awaitingNextLevel}");
            return;
        }

        // Diagnostic pass: report everything that is blocking completion (does not change logic)
        int nullSlots = 0;
        var unoccupied = new List<string>();
        var checkedBlocks = new HashSet<BlockSpawner>();
        var offTemplateBlocks = new List<string>();
        foreach (SnapSlot slot in spawner.spawnedSlots)
        {
            if (slot == null) { nullSlots++; continue; }
            if (!slot.isOccupied) { unoccupied.Add(slot.gridPos.ToString()); continue; }
            BlockSpawner occ = slot.occupant;
            if (occ != null && checkedBlocks.Add(occ) && !occ.FullyMatched)
                offTemplateBlocks.Add(occ.name);
        }

        int total = spawner.spawnedSlots.Count;
        Log($"Completion check: {total - unoccupied.Count - nullSlots}/{total} slots occupied, " +
            $"{checkedBlocks.Count} distinct blocks placed, nullSlots={nullSlots}");
        if (nullSlots > 0)
            LogWarn($"{nullSlots} slot references are null/destroyed - spawnedSlots may be stale");
        if (unoccupied.Count > 0)
            Log($"Unoccupied slots ({unoccupied.Count}): {string.Join(", ", unoccupied)}");
        if (offTemplateBlocks.Count > 0)
            Log($"Blocks with cells hanging off the template: {string.Join(", ", offTemplateBlocks)}");

        foreach (SnapSlot slot in spawner.spawnedSlots)
        {
            if (!slot.isOccupied)
            {
                Log($"Not complete - slot at {slot.gridPos} is not occupied");
                return;
            }
        }

        foreach (SnapSlot slot in spawner.spawnedSlots)
        {
            BlockSpawner occupant = slot.occupant;
            if (occupant != null && !occupant.FullyMatched)
            {
                Log($"Not complete - block '{occupant.name}' has cells off-template");
                return;
            }
        }

        Log($"Level complete: {currentLevel.templateName} ({currentDifficulty}). Time: {elapsedTime:F2}s");
        StopTimer();
        levelTimes.Add(elapsedTime);

        controlDisplay?.ShowLevelComplete(
            levelTimes.Count,
            elapsedTime,
            TotalLevelCount,
            levelTimes);

        if (completionSound != null && audioSource != null)
            audioSource.PlayOneShot(completionSound);

        StartAdvance("CheckCompletion");
    }

    void StartAdvance(string caller)
    {
        Log($"{caller} -> starting AdvanceToNextLevel coroutine " +
            $"(activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled})");
        if (!gameObject.activeInHierarchy)
            Debug.LogError($"{LogTag} GameObject '{gameObject.name}' is inactive - StartCoroutine will fail!", this);
        StartCoroutine(AdvanceToNextLevel());
    }

    void DisableBlockInteraction()
    {
        int disabled = 0;
        foreach (GameObject block in activeBlocks)
        {
            if (block == null) continue;
            foreach (var mb in block.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                if (mb.GetType().Name.Contains("Interactable") ||
                    mb.GetType().Name.Contains("Grabbable"))
                {
                    mb.enabled = false;
                    disabled++;
                }
            }
        }
        Log($"DisableBlockInteraction: disabled {disabled} components on {activeBlocks.Count} blocks");
    }

    IEnumerator AdvanceToNextLevel()
    {
        awaitingNextLevel = true;
        Log($"AdvanceToNextLevel: started. Waiting {delayBeforeNextLevel}s (Time.timeScale={Time.timeScale})");
        if (Time.timeScale == 0f)
            LogWarn("Time.timeScale is 0 - WaitForSeconds will never finish!");

        DisableBlockInteraction();

        yield return new WaitForSeconds(delayBeforeNextLevel);

        Log($"AdvanceToNextLevel: wait finished. State: {StateSummary()}");

        // Ask OscHandler for a new difficulty after every level
        Difficulty previousDifficulty = currentDifficulty;
        Difficulty targetDifficulty = currentDifficulty;

        if (OscHandler.Instance != null)
        {
            targetDifficulty = OscHandler.Instance.GetRecommendedDifficulty(currentDifficulty);
            Log($"AdvanceToNextLevel: OscHandler recommended {previousDifficulty} -> {targetDifficulty}");
        }
        else
        {
            LogWarn("AdvanceToNextLevel: OscHandler.Instance is NULL - keeping current difficulty");
        }

        OscHandler.Instance?.ClearSignals();

        // Peek at the next level for analytics before loading
        string currentLevelName = currentLevel != null ? currentLevel.templateName : "None";
        string currentLevelDifficulty = currentLevel != null ? GetDifficultyForLevel(currentLevel).ToString() : "None";
        TemplateData nextLevel = PickLevelFromDifficulty(targetDifficulty, previousDifficulty, out Difficulty actualDifficulty);

        // Keep currentDifficulty in sync with the pool the level actually came from,
        // so the next recommendation starts from the real difficulty.
        if (nextLevel != null)
        {
            currentDifficulty = actualDifficulty;
            Log($"AdvanceToNextLevel: difficulty {previousDifficulty} -> {currentDifficulty}" +
                (actualDifficulty != targetDifficulty ? $" (recommended {targetDifficulty}, but that pool was empty)" : ""));
        }

        string nextLevelName = nextLevel != null ? nextLevel.templateName : "None";
        string nextLevelDifficulty = nextLevel != null ? actualDifficulty.ToString() : "None";
        // The level that just ended is the last entry in levelTimes (1-based number = Count)
        bool currentLevelTimedOut = timedOutLevels.Contains(levelTimes.Count);

        Log($"AdvanceToNextLevel: {currentLevelName} ({currentLevelDifficulty}) -> {nextLevelName} ({nextLevelDifficulty}), " +
            $"timedOut={currentLevelTimedOut}. Sending analytics (AnalyticsManager.Instance={(AnalyticsManager.Instance != null ? "found" : "NULL")})");

        AnalyticsManager.Instance?.MovingToNextLevel(
            currentLevelName, currentLevelDifficulty,
            nextLevelName, nextLevelDifficulty,
            elapsedTime, currentLevelTimedOut);

        Log("AdvanceToNextLevel: analytics call returned");

        if (nextLevel == null)
        {
            Log("AdvanceToNextLevel: no next level -> HandleAllLevelsComplete");
            HandleAllLevelsComplete();
            yield break;
        }

        currentLevel = nextLevel;
        LoadLevel();
        Log("AdvanceToNextLevel: finished");
    }

    void HandleAllLevelsComplete()
    {
        Log("All levels complete!");
        gameOver = true;
        StopTimer();

        for (int i = 0; i < levelTimes.Count; i++)
        {
            int mins = Mathf.FloorToInt(levelTimes[i] / 60f);
            int secs = Mathf.FloorToInt(levelTimes[i] % 60f);
            string tag = timedOutLevels.Contains(i + 1) ? " (timed out)" : "";
            Log($"  Level {i + 1}: {mins:00}:{secs:00}{tag}");
        }

        ReportSessionEnd("AllLevelsFinished");

        controlDisplay?.ShowEndScreen(levelTimes);
        onAllLevelsComplete?.Invoke();
    }

    public void ResetLevel()
    {
        if (gameOver || awaitingNextLevel)
        {
            LogWarn($"ResetLevel ignored: gameOver={gameOver}, awaitingNextLevel={awaitingNextLevel}");
            return;
        }

        foreach (GameObject block in activeBlocks)
            if (block != null) Destroy(block);
        activeBlocks.Clear();

        // (The current level is NOT put back into its pool - doing that made
        // the same level get picked again after it was solved.)

        spawner.BuildTemplate();
        SpawnBlocks();
        // The level timer keeps running through a reset so the
        // per-level time limit can't be dodged by resetting.

        Log($"Level reset: {currentLevel?.templateName}. State: {StateSummary()}");
    }

    Difficulty GetDifficultyForLevel(TemplateData level)
    {
        if (easyLevels.Contains(level)) return Difficulty.Easy;
        if (mediumLevels.Contains(level)) return Difficulty.Medium;
        return Difficulty.Hard;
    }
}