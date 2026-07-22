using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{

    static GameManager instance;

    public static GameManager Instance { get { return instance; } }
    public int MaxMissles = 4;

    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float minSize = 0.5f;
    public float OriginalSize { get; private set; } = 1f;
    public float MaxSpeed => maxSpeed;
    public float MinSize => minSize;

    [System.Serializable]
    public struct SceneSettings
    {
        public bool isMoving;
        public float speed;
        public float movementRadius;
        public float size;
    }

    public enum GameScene { L1, L2, L3, Unknown }

    // Original/default values for each scene.
    private static readonly Dictionary<GameScene, SceneSettings> defaultSceneSettings = new()
    {
        [GameScene.L1] = new SceneSettings { isMoving = false, speed = 0f, movementRadius = 0f, size = 1f },
        [GameScene.L2] = new SceneSettings { isMoving = true,  speed = 1f, movementRadius = 2f, size = 1f },
        [GameScene.L3] = new SceneSettings { isMoving = true,  speed = 1f, movementRadius = 3f, size = 1f },
    };

    // Live settings, initialized from defaultSceneSettings. Get/set values through this at runtime.
    private Dictionary<GameScene, SceneSettings> sceneSettings;

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

    public int GetMaxMissiles()
    {
        return MaxMissles;
    }

    // Reinitializes sceneSettings back to the original default values.
    public void ResetSceneSettings()
    {
        sceneSettings = new Dictionary<GameScene, SceneSettings>(defaultSceneSettings);
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

    public float GetMovementRadius(GameScene scene) => sceneSettings.TryGetValue(scene, out var s) ? s.movementRadius : default;
    public void SetMovementRadius(GameScene scene, float value)
    {
        var s = sceneSettings.TryGetValue(scene, out var existing) ? existing : default;
        s.movementRadius = value;
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
