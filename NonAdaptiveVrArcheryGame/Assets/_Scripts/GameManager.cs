using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    #region Singleton
    static GameManager instance;

    public static GameManager Instance {get {return instance;}}
    
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
}