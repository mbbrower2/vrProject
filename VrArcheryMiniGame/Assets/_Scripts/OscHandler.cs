using UnityEngine;

public class OscHandler : MonoBehaviour
{
    public static OscHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // OSC logic lives here — affects all targets globally
    public void oscTransformation(bool bored)
    {
        Debug.Log("IN OSC TRANSFORMATION");
        if (bored)
            HandleBored();
        else
            HandleEngaged();
    }

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
        if (!TargetManager.Instance.IsMoving)
        {
            TargetManager.Instance.SetMoving(true);
            return;
        }

        if (TargetManager.Instance.Speed < GameManager.Instance.MaxSpeed)
        {
            float updateSpeed = TargetManager.Instance.Speed + 1;
            AnalyticsManager.Instance.UpdateSpeed(GameManager.Instance.GetCurrentSceneName(), updateSpeed);
            TargetManager.Instance.SetSpeed(updateSpeed);
            return;
        }

        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                TryNextLevel();
                break;

            case GameManager.GameScene.L2:
                if (TargetManager.Instance.Size > GameManager.Instance.MinSize)
                {
                    float updateTargetSize = TargetManager.Instance.Size - 1;
                    TargetManager.Instance.SetSize(updateTargetSize);
                    AnalyticsManager.Instance.UpdateSize(GameManager.Instance.GetCurrentSceneName(), updateTargetSize);
                }
                else
                    TryNextLevel();
                break;

            case GameManager.GameScene.L3:
                if (TargetManager.Instance.Size > GameManager.Instance.MinSize)
                {
                    float updateTargetSize = TargetManager.Instance.Size - 1;
                    TargetManager.Instance.SetSize(updateTargetSize);
                    AnalyticsManager.Instance.UpdateSize(GameManager.Instance.GetCurrentSceneName(), updateTargetSize);
                } else if (MissileManager.Instance.getMaxMissles() < GameManager.Instance.GetMaxMissiles()) {
                    int numMissles = MissileManager.Instance.getMaxMissles();
                    MissileManager.Instance.updateMaxMissles(numMissles + 1);
                }
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
        bool canSlowDown;

        if (!TargetManager.Instance.IsMoving)
        {
            TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.Instance.GetCurrentScene()));
            return;
        }

        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                canSlowDown = TrySlowDown();
                if (!canSlowDown)
                    TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L1));
                break;

            case GameManager.GameScene.L2:
                if (TargetManager.Instance.Size < GameManager.Instance.OriginalSize)
                    TargetManager.Instance.SetSize(TargetManager.Instance.Size + 1);
                else
                {
                    canSlowDown = TrySlowDown();
                    if (!canSlowDown)
                        TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L2));
                }
                break;

            case GameManager.GameScene.L3:
                if (TargetManager.Instance.Size < GameManager.Instance.OriginalSize) {
                    TargetManager.Instance.SetSize(TargetManager.Instance.Size + 1);
                } else if (MissileManager.Instance.getMaxMissles() >= GameManager.Instance.GetMaxMissiles())
                {
                    int numMissles = MissileManager.Instance.getMaxMissles();
                    MissileManager.Instance.updateMaxMissles(numMissles - 1);
                }
                else
                {
                    canSlowDown = TrySlowDown();
                    if (!canSlowDown)
                        TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L3));
                }
                break;
        }
    }

    /**
    * Return true if it is possible to slow down else return false
    **/
    private bool TrySlowDown()
    {
        if (TargetManager.Instance.Speed > 0)
        {
            var newSpeed = TargetManager.Instance.Speed - 1;
            TargetManager.Instance.SetSpeed(newSpeed);
            if (newSpeed == 0)
                TargetManager.Instance.SetMoving(false);
            return true;
        }
        return false;
    }


    private void TryNextLevel()
    {
        switch (GameManager.Instance.GetCurrentScene())
        {
            case GameManager.GameScene.L1:
                TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L2));
                break;

            case GameManager.GameScene.L2:
                TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L3));
                break;

            case GameManager.GameScene.L3:
                TargetManager.Instance.SetNextSceneName(GameManager.Instance.GameSceneToString(GameManager.GameScene.L3));
                break;
        }
    }
}
