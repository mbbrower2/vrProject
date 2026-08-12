using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    public string nextSceneName { get; private set; }

    private int remainingTargets;

    public bool IsMoving { get; private set; } = false;
    public float Speed { get; private set; } = 0f;
    public float MovementRadius { get; private set; } = 0f;
    public float Size { get; private set; } = 1f;

    public event Action OnTargetSettingsChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

    }

    private void Start()
    {
        var defaults = GameManager.Instance.GetSettingsForScene(
            GameManager.Instance.GetCurrentScene()
        );
        
        SetNextSceneName(SceneManager.GetActiveScene().name);
        SetMoving(defaults.isMoving);
        SetSpeed(defaults.speed);
        SetSize(defaults.size);
    }

    public void RegisterTarget()
    {
        remainingTargets++;
    }

    public void ReportTargetDown()
    {
        remainingTargets--;

        if (remainingTargets <= 0)
        {
            //If this scene is the tutorial just move on to the next scene
            if (GameManager.Instance.GetCurrentSceneName() == "General")
            {
                SceneManager.LoadScene("ArcherySceneL1");
                return;
            }
            AnalyticsManager.Instance.MovingToNextLevel(GameManager.Instance.GetCurrentSceneName(), nextSceneName);

            //If the player isnt moving on to the next scene save all the current settings
            if (nextSceneName == GameManager.Instance.GetCurrentSceneName())
            {
                GameManager.GameScene currentScene = GameManager.Instance.GetCurrentScene();
                GameManager.Instance.SetIsMoving(currentScene, IsMoving);
                GameManager.Instance.SetSpeed(currentScene, Speed);
                GameManager.Instance.SetSize(currentScene, Size);
            }
            else
            {
                GameManager.Instance.ResetSceneSettings();
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void SetNextSceneName(string sceneName)
    {
        nextSceneName = sceneName;
    }

    public void SetMoving(bool moving)
    {
        IsMoving = moving;
        Speed = 1f;
        MovementRadius = 5f;
        OnTargetSettingsChanged?.Invoke();
    }

    public void SetSpeed(float speed)
    {
        Speed = Mathf.Clamp(speed, 0f, GameManager.Instance.MaxSpeed);
        OnTargetSettingsChanged?.Invoke();
    }

    public void SetSize(float size)
    {
        Size = Mathf.Clamp(size, GameManager.Instance.MinSize, GameManager.Instance.OriginalSize);
        OnTargetSettingsChanged?.Invoke();
    }
}