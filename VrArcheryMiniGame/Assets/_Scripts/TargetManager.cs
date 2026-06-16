using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    [SerializeField] private string nextSceneName;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float minSize = 0.5f;

    private int remainingTargets;

    public bool IsMoving { get; private set; } = true;
    public float Speed { get; private set; } = 1f;
    public float Size { get; private set; } = 1f;
    public float OriginalSize { get; private set; } = 1f;
    public float MaxSpeed => maxSpeed;
    public float MinSize => minSize;

    public event Action OnTargetSettingsChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void SetMoving(bool moving)
    {
        IsMoving = moving;
        OnTargetSettingsChanged?.Invoke();
    }

    public void SetSpeed(float speed)
    {
        Speed = Mathf.Clamp(speed, 0f, maxSpeed);
        OnTargetSettingsChanged?.Invoke();
    }

    public void SetSize(float size)
    {
        Size = Mathf.Clamp(size, minSize, OriginalSize);
        OnTargetSettingsChanged?.Invoke();
    }

    // OSC logic lives here — affects all targets globally
    public void oscTransformation(bool bored)
    {
        if (bored)
            HandleBored();
        else
            HandleEngaged();
    }

    private void HandleBored()
    {
        if (!IsMoving)
        {
            SetMoving(true);
            return;
        }

        if (Speed < maxSpeed)
        {
            SetSpeed(Speed + 1);
            return;
        }

        switch (GetCurrentScene())
        {
            case GameScene.L1:
                TryNextLevel();
                break;

            case GameScene.L2:
                if (Size > minSize)
                    SetSize(Size - 1);
                else
                    TryNextLevel();
                break;

            case GameScene.L3:
                // TODO: add projectile logic
                if (Size > minSize)
                    SetSize(Size - 1);
                else
                    TryNextLevel();
                break;
        }
    }

    /**
    If the target isnt moving, the next scene should be the same as the current scene
    L1 logic 
        - if the target is moving, try and slow it down, if you cant slow it down the next scene is the same as the current scene 
    L2 logic 
        - if the target is small make it bigger
        - if the target isnt small, try and slow it down
            - if you cant slow it down then repeat the same level
    L3 logic 
        - if there are extra projectiles, decrease them
        - if no extra projectiles then try and make target bigger
        - if the target isnt small, try and slow it down
            - if you cant slow it down then repeat the same level
    **/
    private void HandleEngaged()
    {
        // Declare once here, used across all cases below
        bool canSlowDown;

        if (!IsMoving)
        {
            nextSceneName = GameSceneToString(GetCurrentScene());
            return;
        }

        switch (GetCurrentScene())
        {
            case GameScene.L1:
                canSlowDown = TrySlowDown();
                if (!canSlowDown)
                    nextSceneName = GameSceneToString(GameScene.L1);
                break;

            case GameScene.L2:
                if (Size < OriginalSize)
                    SetSize(Size + 1);
                else
                {
                    canSlowDown = TrySlowDown();
                    if (!canSlowDown)
                        nextSceneName = GameSceneToString(GameScene.L2);
                }
                break;

            case GameScene.L3:
                // TODO: reduce projectiles first
                if (Size < OriginalSize)
                    SetSize(Size + 1);
                else
                {
                    canSlowDown = TrySlowDown();
                    if (!canSlowDown)
                        nextSceneName = GameSceneToString(GameScene.L3);
                }
                break;
        }
    }

    /**
    * Return true if it is possible to slow down else return false
    **/
    private bool TrySlowDown()
    {
        if (Speed > 0)
        {
            var newSpeed = Speed - 1;
            SetSpeed(newSpeed);
            if (newSpeed == 0)
                SetMoving(false);
            return true;
        }
        return false;
    }

    private void TryNextLevel()
    {
        switch (GetCurrentScene())
        {
            case GameScene.L1:
                nextSceneName = GameSceneToString(GameScene.L2);
                break;

            case GameScene.L2:
                nextSceneName = GameSceneToString(GameScene.L3);
                break;
        }
    }

    private enum GameScene { L1, L2, L3, Unknown }

    private GameScene GetCurrentScene()
    {
        return SceneManager.GetActiveScene().name switch
        {
            "ArcherySceneL1" => GameScene.L1,
            "ArcherySceneL2" => GameScene.L2,
            "ArcherySceneL3" => GameScene.L3,
            _ => GameScene.Unknown
        };
    }

    // Converts a GameScene enum back to its scene name string
    private string GameSceneToString(GameScene scene)
    {
        return scene switch
        {
            GameScene.L1 => "ArcherySceneL1",
            GameScene.L2 => "ArcherySceneL2",
            GameScene.L3 => "ArcherySceneL3",
            _ => nextSceneName // fall back to whatever is already set
        };
    }
}