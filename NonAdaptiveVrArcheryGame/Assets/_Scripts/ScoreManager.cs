using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    #region Singleton
    static ScoreManager instance;
    public static ScoreManager Instance { get { return instance; } }

    [Tooltip("Optional. If empty (or destroyed by a scene load), the object tagged 'ScoreText' is found automatically.")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] private string scoreTextTag = "ScoreText";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The score text may live in the level scene and get destroyed on reload; re-find it.
        if (scoreText == null)
        {
            GameObject go = null;
            try { go = GameObject.FindWithTag(scoreTextTag); }
            catch (UnityException) { /* tag not defined in Tags & Layers */ }

            if (go != null) scoreText = go.GetComponent<TextMeshProUGUI>();
        }

        RefreshText();
    }

    /// <summary>
    /// Displays the running total. GameManager owns the score and sends analytics,
    /// so this only updates the UI.
    /// </summary>
    public void UpdateScoreText(float score)
    {
        SetText(score);
    }

    private void RefreshText()
    {
        if (GameManager.Instance != null)
            SetText(GameManager.Instance.Score);
    }

    private void SetText(float score)
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score.ToString("0");
    }
}