using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    [System.Serializable]
    public struct SceneSettings
    {
        public bool isMoving;
        public float speed;
        public float movementRadius;
        public float size;
    }
    public enum GameScene { L1, L2, L3, Unknown }

    [Header("Session")]
    [Tooltip("Levels in the order they are played. Each is played 'playsPerLevel' times.")]
    [SerializeField] private string[] levelOrder = { "ArcherySceneL1", "ArcherySceneL2", "ArcherySceneL3" };
    [SerializeField] private int playsPerLevel = 3;
    [Tooltip("Session length in seconds. The clock starts when the first level in Level Order loads.")]
    [SerializeField] private float sessionDurationSeconds = 15f * 60f;
    [Tooltip("Scene loaded when the game ends. If it isn't in Build Settings, the app quits instead.")]
    [SerializeField] private string endSceneName = "GameOver";

    private int currentLevelIndex = 0;
    private int currentPlay = 1;          // 1-based play-through number of the current level
    private bool sessionStarted = false;
    private bool sessionEnded = false;
    private bool isLoading = false;
    private float sessionStartTime;

    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentPlay => currentPlay;
    public int PlaysPerLevel => playsPerLevel;
    public bool SessionEnded => sessionEnded;
    public float ElapsedSeconds => sessionStarted ? Time.realtimeSinceStartup - sessionStartTime : 0f;
    public float TimeRemaining => Mathf.Max(0f, sessionDurationSeconds - ElapsedSeconds);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TryStartSession(GetCurrentSceneName());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoading = false;
        TryStartSession(scene.name);
    }

    private void TryStartSession(string sceneName)
    {
        if (sessionStarted || sessionEnded) return;

        int index = System.Array.IndexOf(levelOrder, sceneName);
        if (index < 0) return; // intro / tutorial scenes don't start the clock

        sessionStarted = true;
        sessionStartTime = Time.realtimeSinceStartup;
        currentLevelIndex = index;
        currentPlay = 1;
        Debug.Log($"Session started in {sceneName}");
    }

    private void Update()
    {
        if (sessionStarted && !sessionEnded && ElapsedSeconds >= sessionDurationSeconds)
        {
            EndGame("TimeLimit");
        }
    }

    public bool IsManagedLevel(string sceneName)
    {
        return System.Array.IndexOf(levelOrder, sceneName) >= 0;
    }

    /// <summary>
    /// Called when every target in a managed level is down.
    /// Replays the level until it has been played 'playsPerLevel' times, then moves on.
    /// Ends the game after the last level's final play-through.
    /// </summary>
    public void LevelCompleted()
    {
        if (sessionEnded || isLoading) return;

        string current = GetCurrentSceneName();
        string next;

        if (currentPlay < playsPerLevel)
        {
            currentPlay++;
            next = levelOrder[currentLevelIndex];
        }
        else if (currentLevelIndex < levelOrder.Length - 1)
        {
            currentLevelIndex++;
            currentPlay = 1;
            next = levelOrder[currentLevelIndex];
        }
        else
        {
            EndGame("AllLevelsComplete");
            return;
        }

        Debug.Log($"Level complete: {current}. Loading {next} (play {currentPlay}/{playsPerLevel})");
        AnalyticsManager.Instance.MovingToNextLevel(current, next);
        isLoading = true;
        SceneManager.LoadScene(next);
    }

    private void EndGame(string reason)
    {
        if (sessionEnded) return;
        sessionEnded = true;
        isLoading = true;

        Debug.Log($"Game over ({reason}) after {ElapsedSeconds:0}s");
        AnalyticsManager.Instance.GameEnded(GetCurrentSceneName(), reason, ElapsedSeconds, currentPlay);

        if (!string.IsNullOrEmpty(endSceneName) && Application.CanStreamedLevelBeLoaded(endSceneName))
        {
            SceneManager.LoadScene(endSceneName);
        }
        else
        {
            StartCoroutine(QuitAfterDelay(2f)); // give analytics a moment to flush
        }
    }

    private IEnumerator QuitAfterDelay(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static float score;
    public float Score { get { return score; } }

    public void PlayerScored(float targetValue)
    {
        score = score + targetValue;
        AnalyticsManager.Instance.UpdateScore(GetCurrentSceneName(), score);
        ScoreManager.Instance.UpdateScoreText(score);
    }

    private static readonly Dictionary<string, SceneSettings> sceneDefaults = new()
    {
        ["ArcherySceneL1"] = new SceneSettings { isMoving = false, speed = 0f, movementRadius = 0f, size = 1f },
        ["ArcherySceneL2"] = new SceneSettings { isMoving = true,  speed = 1f, movementRadius = 2f, size = 1f },
        ["ArcherySceneL3"] = new SceneSettings { isMoving = true,  speed = 1f, movementRadius = 3f, size = 1f },
    };

    public SceneSettings GetDefaultsForScene(string sceneName)
    {
        if (sceneDefaults.TryGetValue(sceneName, out var settings))
            return settings;

        Debug.LogWarning($"No defaults found for scene: {sceneName}");
        return default;
    }

    public GameScene GetCurrentScene()
    {
        return SceneManager.GetActiveScene().name switch
        {
            "ArcherySceneL1" => GameScene.L1,
            "ArcherySceneL2" => GameScene.L2,
            "ArcherySceneL3" => GameScene.L3,
            _ => GameScene.Unknown
        };
    }

    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public string GameSceneToString(GameScene scene)
    {
        return scene switch
        {
            GameScene.L1 => "ArcherySceneL1",
            GameScene.L2 => "ArcherySceneL2",
            GameScene.L3 => "ArcherySceneL3",
            _ => SceneManager.GetActiveScene().name
        };
    }
}