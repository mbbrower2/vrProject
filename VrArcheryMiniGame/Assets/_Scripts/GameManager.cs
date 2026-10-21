using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{

    static GameManager instance;

    public static GameManager Instance { get { return instance; } }
    public int MaxMissles = 4;

    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float minSize = 0.25f;
    public float OriginalSize { get; private set; } = 1f;
    public float MaxSpeed => maxSpeed;
    public float MinSize => minSize;

    [Header("Replays")]
    [Tooltip("How many times a level can be played before HandleEngaged moves the player on.")]
    [SerializeField] private int maxPlaysPerLevel = 3;
    public int MaxPlaysPerLevel => maxPlaysPerLevel;

    [Header("End of game")]
    [Tooltip("The game ends after the last level (L3) has been played this many times.")]
    [SerializeField] private int lastLevelRuns = 3;
    [Tooltip("The game ends this many minutes after the first level starts.")]
    [SerializeField] private float sessionLengthMinutes = 15f;
    [Tooltip("Seconds to wait after the game ends before quitting, so the final analytics event can upload.")]
    [SerializeField] private float quitDelaySeconds = 2f;

    private bool timerStarted = false;
    private float sessionStartTime;
    public bool IsGameOver { get; private set; } = false;
    public float ElapsedSessionTime => timerStarted ? Time.unscaledTime - sessionStartTime : 0f;

    [System.Serializable]
    public struct SceneSettings
    {
        public bool isMoving;
        public float speed;
        public float size;
    }

    public enum GameScene { L1, L2, L3, Unknown }

    // Original/default values for each scene.
    private static readonly Dictionary<GameScene, SceneSettings> defaultSceneSettings = new()
    {
        [GameScene.L1] = new SceneSettings { isMoving = false, speed = 0f, size = 1f },
        [GameScene.L2] = new SceneSettings { isMoving = true,  speed = 1f, size = 1f },
        [GameScene.L3] = new SceneSettings { isMoving = true,  speed = 1f, size = 1f },
    };

    // Live settings, initialized from defaultSceneSettings. Get/set values through this at runtime.
    private Dictionary<GameScene, SceneSettings> sceneSettings;

    // How many times each level has been played in a row.
    // Cleared whenever the player moves to a different level.
    private readonly Dictionary<GameScene, int> playCounts = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this);
        ResetSceneSettings();
    }

    private void Update()
    {
        if (!timerStarted || IsGameOver)
            return;

        if (ElapsedSessionTime >= sessionLengthMinutes * 60f)
        {
            EndGame("TimeLimit");
        }
    }

    // ---------- Level starts / play counts ----------

    // Called by TargetManager when a level starts.
    // Starts the session timer the first time a real level loads and counts the play.
    public void OnLevelStarted(GameScene scene)
    {
        if (scene == GameScene.Unknown || IsGameOver)
            return; // ignore tutorial/other scenes

        if (!timerStarted)
        {
            timerStarted = true;
            sessionStartTime = Time.unscaledTime;
        }

        playCounts[scene] = GetPlayCount(scene) + 1;
    }

    public int GetPlayCount(GameScene scene) =>
        playCounts.TryGetValue(scene, out var c) ? c : 0;

    public void ResetPlayCounts() => playCounts.Clear();

    public bool HasReachedMaxPlays(GameScene scene) =>
        GetPlayCount(scene) >= maxPlaysPerLevel;

    // True once the last level has been played the required number of times.
    public bool IsLastLevelComplete(GameScene scene) =>
        scene == GameScene.L3 && GetPlayCount(scene) >= lastLevelRuns;

    // ---------- Ending the game ----------

    public void EndGame(string reason)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        AnalyticsManager.Instance.GameEnded(GetCurrentSceneName(), reason, ElapsedSessionTime, score);

        // Freeze gameplay (targets, missiles, physics) while analytics uploads
        Time.timeScale = 0f;

        StartCoroutine(QuitAfterDelay());
    }

    private System.Collections.IEnumerator QuitAfterDelay()
    {
        // Realtime so it still runs while timeScale is 0
        yield return new WaitForSecondsRealtime(quitDelaySeconds);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stop Play mode in the editor
#else
        Application.Quit(); // close the app in a build
#endif
    }

    // ---------- Scene settings ----------

    public int GetMaxMissiles()
    {
        return MaxMissles;
    }

    // Reinitializes sceneSettings back to the original default values
    // and clears the replay counts (called when moving to a different level).
    public void ResetSceneSettings()
    {
        sceneSettings = new Dictionary<GameScene, SceneSettings>(defaultSceneSettings);
        ResetPlayCounts();
    }

    public bool GetIsMoving(GameScene scene) => sceneSettings.TryGetValue(scene, out var s) ? s.isMoving : default;
    public void SetIsMoving(GameScene scene, bool value)
    {
        var s = sceneSettings.TryGetValue(scene, out var existing) ? existing : default;
        s.isMoving = value;
        sceneSettings[scene] = s;
    }

    public float GetSpeed(GameScene scene) => sceneSettings.TryGetValue(scene, out var s) ? s.speed : default;
    public void SetSpeed(GameScene scene, float value)
    {
        var s = sceneSettings.TryGetValue(scene, out var existing) ? existing : default;
        s.speed = value;
        sceneSettings[scene] = s;
    }

    public float GetSize(GameScene scene) => sceneSettings.TryGetValue(scene, out var s) ? s.size : default;
    public void SetSize(GameScene scene, float value)
    {
        var s = sceneSettings.TryGetValue(scene, out var existing) ? existing : default;
        s.size = value;
        sceneSettings[scene] = s;
    }

    private static float score;
    public float Score { get { return score; } }

    public void PlayerScored(float targetValue)
    {
        score = score + targetValue;
        AnalyticsManager.Instance.UpdateScore(GetCurrentSceneName(), score);
        ScoreManager.Instance.UpdateScoreText(score);
    }

    // Lookup by scene name (kept for backward compatibility with existing callers).
    public SceneSettings GetSettingsForScene(string sceneName)
    {
        return GetSettingsForScene(StringToGameScene(sceneName));
    }

    // Preferred lookup by enum, avoids string comparisons.
    // Returns the current (live) settings, which may differ from the defaults if they've been modified.
    public SceneSettings GetSettingsForScene(GameScene scene)
    {
        if (sceneSettings.TryGetValue(scene, out var settings))
            return settings;

        Debug.LogWarning($"No settings found for scene: {scene}");
        return default;
    }

    public GameScene GetCurrentScene()
    {
        return StringToGameScene(SceneManager.GetActiveScene().name);
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

    private GameScene StringToGameScene(string sceneName)
    {
        return sceneName switch
        {
            "ArcherySceneL1" => GameScene.L1,
            "ArcherySceneL2" => GameScene.L2,
            "ArcherySceneL3" => GameScene.L3,
            _ => GameScene.Unknown
        };
    }
}