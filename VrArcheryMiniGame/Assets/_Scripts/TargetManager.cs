using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    public string nextSceneName { get; private set; }
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float minSize = 0.5f;

    private int remainingTargets;

    public bool IsMoving { get; private set; } = false;
    public float Speed { get; private set; } = 0f;
    public float MovementRadius { get; private set; } = 0f;
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

    private void Start()
    {
        var defaults = GameManager.Instance.GetDefaultsForScene(
            SceneManager.GetActiveScene().name
        );

        //TODO: when testing the osc logic this should bee commented out and the line bellow uncommented
        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                SetNextSceneName("ArcherySceneL2");
                break;

            case GameManager.GameScene.L2:
                SetNextSceneName("ArcherySceneL3");
                break;

            case GameManager.GameScene.L3:
                SetNextSceneName("ArcherySceneL2");
                break;
        }
        
        //SetNextSceneName(SceneManager.GetActiveScene().name);
        SetMoving(defaults.isMoving);
        SetSpeed(defaults.speed);
        SetMovementRadius(defaults.movementRadius);
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
        MovementRadius = 2f;
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

        public void SetMovementRadius(float movementRadius)
    {
        MovementRadius = movementRadius;
        OnTargetSettingsChanged?.Invoke();
    }

    // OSC logic lives here — affects all targets globally
    //TODO: this should go into its own osc script so that messages arent getting received inside of target manager
    public void oscTransformation(bool bored)
    {
        if (bored)
            HandleBored();
        else
            HandleEngaged();
    }

    //TODO: if the person doesnt move to the next level we should save the speed settings and shit so it doesnt start from the beginning 
    // ^^ this info can go in the game manager and then if we move to the next scene just set it back to the default
    /**
    If the target isnt moving, set the target to moving
    If the target is already moving, check the speed that its moving
    L1 logic 
        - if the target is moving at its make speed, the next scene should be scene 2
    L2 logic 
        - if the target is moving at its max speed try and make the target smaller
        - if the targets cant get any smaller the next scene should be scene 3
    L3 logic 
        - if the target is moving at its max speed try and make the target smaller
        - if the targets are as small as they can get add more projectiles
        - if the targets cant get any faster or smaller and you cant add more projectiles then replay the scene
        - we replay the scene for L3 because there are no more levels
    **/
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

        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                TryNextLevel();
                break;

            case GameManager.GameScene.L2:
                if (Size > minSize)
                    SetSize(Size - 1);
                else
                    TryNextLevel();
                break;

            case GameManager.GameScene.L3:
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
            SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.Instance.GetCurrentScene()));
            return;
        }

        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                canSlowDown = TrySlowDown();
                if (!canSlowDown)
                    SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L1));
                break;

            case GameManager.GameScene.L2:
                if (Size < OriginalSize)
                    SetSize(Size + 1);
                else
                {
                    canSlowDown = TrySlowDown();
                    if (!canSlowDown)
                        SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L2));
                }
                break;

            case GameManager.GameScene.L3:
                // TODO: reduce projectiles first
                if (Size < OriginalSize)
                    SetSize(Size + 1);
                else
                {
                    canSlowDown = TrySlowDown();
                    if (!canSlowDown)
                        SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L3));
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
        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L2));
                break;

            case GameManager.GameScene.L2:
                SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L3));
                break;
            
            case GameManager.GameScene.L3:
                SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L3));
                break;
        }
    }
}