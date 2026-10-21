using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Oculus.Interaction;

public class ControlDisplay : MonoBehaviour
{
    [Header("References")]
    public TemplateValidator templateValidator;
    public Canvas instructionCanvas;
    public TextMeshProUGUI instructionText;

    [Header("Settings")]
    public float fadeOutDuration = 0.5f;

    private bool gameStarted = false;
    private bool gameEnded = false;
    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    void Start()
    {
        if (templateValidator != null)
            templateValidator.enabled = false;

        canvasGroup = instructionCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = instructionCanvas.gameObject.AddComponent<CanvasGroup>();

        ShowInstructions();
    }

    void Update()
    {
        // Start game
        if (!gameStarted && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            StartGame();

        // Restart after game ended
        if (gameEnded && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            RestartGame();
    }

    void ShowInstructions()
    {
        if (instructionText == null) return;
        instructionText.text =
            "<size=120%><b>HOW TO PLAY</b></size>\n\n" +
            "<b>CONTROLS</b>\n\n" +
            "<b>Grab Blocks</b>\n" +
            "  Pinch or grip any block to pick it up.\n" +
            "  Hold near a slot to preview placement.\n" +
            "  Release to snap into position.\n\n" +
            "<b>Rotate Template</b>\n" +
            "  Hold <b>B</b> (right) -> rotate horizontal\n" +
            "  Hold <b>X</b> (left) -> rotate vertical\n\n" +
            "<b>Reset Puzzle</b>\n" +
            "  Press <b>Y</b> (left) to reset all blocks.\n\n" +
            "<b>GOAL</b>\n\n" +
            "  Fit the right pieces into the\n" +
            "  glowing template shape.\n" +
            "  No pieces may hang outside!\n\n" +
            "<b>Tip:</b> Orientation matters - try\n" +
            "rotating pieces before placing.\n\n" +
            "---------------------------------\n" +
            "  <b>Press A to Start</b>";
    }

    // ---------- Shared helpers ----------

    void ShowCanvas()
    {
        // Cancel any fade still running so it can't hide the screen we're about to show
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        instructionCanvas.gameObject.SetActive(true);
    }

    bool IsTimedOut(int levelNumber)
    {
        return templateValidator != null && templateValidator.WasLevelTimedOut(levelNumber);
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        return $"{m:00}:{s:00}";
    }

    string FormatLevelTimes(List<float> times)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < times.Count; i++)
        {
            sb.Append($"  Level {i + 1}: {FormatTime(times[i])}");
            if (IsTimedOut(i + 1)) sb.Append("  <i>(timed out)</i>");
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // ---------- Screens called by TemplateValidator ----------

    // Called after each level completes
    public void ShowLevelComplete(int levelIndex, float levelTime, int totalLevels, List<float> allTimes)
    {
        if (instructionText == null) return;
        ShowCanvas();

        string text = $"<size=120%><b>Level {levelIndex} Complete!</b></size>\n\n" +
                      $"Time: <b>{FormatTime(levelTime)}</b>\n\n";

        if (allTimes.Count > 1)
            text += "<b>Level Times:</b>\n" + FormatLevelTimes(allTimes);

        if (levelIndex < totalLevels)
            text += "\n-------------------------\n<b>Now you move onto this next level</b>";

        instructionText.text = text;
    }

    // Called when the per-level time limit runs out
    public void ShowLevelTimedOut(int levelIndex, int totalLevels, List<float> allTimes)
    {
        if (instructionText == null) return;
        ShowCanvas();

        string text = $"<size=120%><b>Level {levelIndex}: Time's Up</b></size>\n\n";

        if (allTimes.Count > 1)
            text += "<b>Level Times:</b>\n" + FormatLevelTimes(allTimes);

        if (levelIndex < totalLevels)
            text += "\n-------------------------\n<b>Moving on to the next level</b>";

        instructionText.text = text;
    }

    public void ShowGameOver(List<float> completedTimes, int reachedLevel, int totalLevels)
    {
        if (instructionText == null) return;
        ShowCanvas();
        gameEnded = true;

        int solved = 0;
        for (int i = 0; i < completedTimes.Count; i++)
            if (!IsTimedOut(i + 1)) solved++;

        string progress = completedTimes.Count == 0
            ? "No levels completed.\n"
            : $"Completed {solved}/{totalLevels} levels.\n\n" +
              "<b>Times:</b>\n" + FormatLevelTimes(completedTimes);

        instructionText.text =
            "<size=130%><b>Time's Up!</b></size>\n\n" +
            progress +
            "\n-----------------------------\n" +
            "  <b>Press A to Try Again</b>";
    }

    public void ShowEndScreen(List<float> allTimes)
    {
        if (instructionText == null) return;
        ShowCanvas();
        gameEnded = true;

        float total = 0f;
        foreach (float t in allTimes) total += t;

        instructionText.text =
            "<size=130%><b>All Done!</b></size>\n\n" +
            "<b>Level Times:</b>\n" +
            FormatLevelTimes(allTimes) +
            $"\n<b>Total: {FormatTime(total)}</b>\n\n" +
            "-------------------------\n" +
            "  <b>Press A to Play Again</b> ";
    }

    // ---------- Game flow ----------

    void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;

        if (templateValidator != null)
            templateValidator.enabled = true;

        fadeRoutine = StartCoroutine(FadeOutCanvas());
    }

    void RestartGame()
    {
        gameEnded = false;
        gameStarted = true; // restart goes straight into play

        fadeRoutine = StartCoroutine(FadeOutCanvas());

        if (templateValidator != null)
        {
            templateValidator.enabled = false;
            templateValidator.enabled = true;
        }
    }

    IEnumerator FadeOutCanvas()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
            yield return null;
        }
        instructionCanvas.gameObject.SetActive(false);
        fadeRoutine = null;
    }
}