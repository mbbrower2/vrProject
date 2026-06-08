using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    #region Singleton
    static ScoreManager instance;

    public static ScoreManager Instance {get {return instance;}}

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
        scoreText.text = "Score: " + score.ToString("0");
    }
}