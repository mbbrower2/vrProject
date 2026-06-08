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
    }

    public void ReportTargetDown()
    {
        remainingTargets--;

        if (remainingTargets <= 0)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}