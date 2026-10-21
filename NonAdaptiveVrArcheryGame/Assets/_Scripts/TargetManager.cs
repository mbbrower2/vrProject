using UnityEngine;
using UnityEngine.SceneManagement;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    [Tooltip("Only used for scenes NOT in GameManager's Level Order (e.g. the tutorial).")]
    [SerializeField] private string nextSceneName;
    private int remainingTargets;
    private bool levelFinished = false;

    private void Awake()
    {
        if (Instance == null || Instance == this)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterTarget()
    {
        remainingTargets++;
        Debug.Log("TARGET registered, count " + remainingTargets);
    }

    public void ReportTargetDown()
    {
        if (levelFinished) return;

        remainingTargets--;
        Debug.Log("TARGET down, remaining " + remainingTargets);

        if (remainingTargets > 0) return;
        levelFinished = true;

        GameManager gm = GameManager.Instance;
        string currentScene = SceneManager.GetActiveScene().name;

        if (gm != null && gm.IsManagedLevel(currentScene))
        {
            // GameManager decides: replay this level, go to the next one, or end the game.
            gm.LevelCompleted();
        }
        else
        {
            // Tutorial / non-level scenes keep their own hand-set next scene.
            Debug.Log("No targets remaining, moving to " + nextSceneName);
            AnalyticsManager.Instance.MovingToNextLevel(currentScene, nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}