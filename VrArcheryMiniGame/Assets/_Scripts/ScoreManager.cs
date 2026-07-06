using UnityEngine;
using TMPro;
using UnityEngine.Analytics;

public class ScoreManager : MonoBehaviour
{
    #region Singleton
    static ScoreManager instance;

    public static ScoreManager Instance {get {return instance;}}
    private float currentScore = 0f; 

    [SerializeField] TextMeshProUGUI scoreText;
    

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

    public void UpdateScoreText(float score)
    {
        AnalyticsManager.Instance.UpdateScore(GameManager.Instance.GetCurrentSceneName(), score);
        currentScore += score;
        scoreText.text = "Score: " + currentScore.ToString("0");
        
    }
}