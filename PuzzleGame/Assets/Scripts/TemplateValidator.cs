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
    public List<TemplateData> levels = new List<TemplateData>();

    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Game Over Timer")]
    public float totalTimeLimit = 900f;

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
    private float totalElapsed = 0f;
    private bool timerRunning = false;
    private bool gameOver = false;
    private int currentLevelIndex = 0;
    private List<float> levelTimes = new List<float>();
    public TemplateData CurrentLevel => (currentLevelIndex < levels.Count) ? levels[currentLevelIndex] : null;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controlDisplay = FindAnyObjectByType<ControlDisplay>();
    }

    void OnEnable()
    {
        currentLevelIndex = 0;
        levelTimes.Clear();
        totalElapsed = 0f;
        gameOver = false;
        LoadLevel(currentLevelIndex);
    }

    void OnDisable()
    {
        StopTimer();
    }

    void Update()
    {
        if (!timerRunning || gameOver) return;

        elapsedTime += Time.deltaTime;
        totalElapsed += Time.deltaTime;
        UpdateTimerDisplay();

        if (totalTimeLimit > 0f && totalElapsed >= totalTimeLimit)
            TriggerGameOver();

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

    void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        StopTimer();

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

        controlDisplay?.ShowGameOver(levelTimes, currentLevelIndex + 1, levels.Count);
    }

    void LoadLevel(int index)
    {
        if (index >= levels.Count)
        {
            HandleAllLevelsComplete();
            return;
        }

        spawner.templateData = levels[index];
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

            // Collision checker, can delete now
            //blockParent.layer = LayerMask.NameToLayer("PuzzleBlock");

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
                (bMax.z - bMin.z + 1) * info.blockData.cellSize);

            Vector3 boundsCenter = new Vector3(
                (bMin.x + bMax.x) * 0.5f * info.blockData.cellSize,
                (bMin.y + bMax.y) * 0.5f * info.blockData.cellSize,
                (bMin.z + bMax.z) * 0.5f * info.blockData.cellSize);

            BoxCollider physicsCol = blockParent.AddComponent<BoxCollider>();
            physicsCol.isTrigger = true;
            physicsCol.center = boundsCenter;
            physicsCol.size = boundsSize;

            activeBlocks.Add(blockParent);
        }
    }

    public void CheckCompletion()
    {
        if (gameOver) return;

        foreach (SnapSlot slot in spawner.spawnedSlots)
            if (!slot.isOccupied) return;

        foreach (SnapSlot slot in spawner.spawnedSlots)
        {
            BlockSpawner occupant = slot.occupant;
            if (occupant != null && !occupant.FullyMatched) return;
        }

        StopTimer();
        levelTimes.Add(elapsedTime);

        controlDisplay?.ShowLevelComplete(currentLevelIndex + 1, elapsedTime, levels.Count, levelTimes);

        if (completionSound != null && audioSource != null)
            audioSource.PlayOneShot(completionSound);

        StartCoroutine(AdvanceToNextLevel());
    }

    IEnumerator AdvanceToNextLevel()
    {
        // lock all blocks
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

        currentLevelIndex++;
        LoadLevel(currentLevelIndex);
    }

    void HandleAllLevelsComplete()
    {
        controlDisplay?.ShowEndScreen(levelTimes);
        onAllLevelsComplete?.Invoke();
    }

    public void ResetLevel()
    {
        if (gameOver) return;
        StopTimer();

        foreach (GameObject block in activeBlocks)
            if (block != null) Destroy(block);
        activeBlocks.Clear();

        spawner.BuildTemplate();
        SpawnBlocks();
        StartTimer();
    }
}