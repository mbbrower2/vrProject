using UnityEngine;
using UnityEngine.SceneManagement;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    [SerializeField] private string nextSceneName;
    private int remainingTargets;

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
        Debug.Log("ABCD TARGET registered  TARGET count " + remainingTargets);
    }

    public void ReportTargetDown()
    {
        
        remainingTargets--;
Debug.Log("ABCD TARGET DOWN REMAINING TARGET " + remainingTargets);
        if (remainingTargets <= 0)
        {
            Debug.Log("ABCD no targets remaining moving to " + nextSceneName);
            AnalyticsManager.Instance.MovingToNextLevel(GameManager.Instance.GetCurrentSceneName(), nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}