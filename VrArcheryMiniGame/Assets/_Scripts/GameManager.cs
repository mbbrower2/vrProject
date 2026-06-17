using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Singleton
    static GameManager instance;

    public static GameManager Instance {get {return instance;}}

    [System.Serializable]
    public struct SceneSettings
    {
        public bool isMoving;
        public float speed;
        public float movementRadius;
        public float size;
    }
    public enum GameScene { L1, L2, L3, Unknown }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        } else
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this);
    }
    #endregion

    private static float score;
    public float Score {get {return score;}}

    public void PlayerScored(float targetValue)
    {
        score = score + targetValue;
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