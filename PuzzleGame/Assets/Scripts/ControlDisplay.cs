using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;

public class ControlDisplay : MonoBehaviour
{
    [Header("References")]
    public TemplateValidator templateValidator;
    public Canvas instructionCanvas;
    public TextMeshProUGUI instructionText;

    [Header("Settings")]
    public float fadeOutDuration = 0.5f;
    public float hintDisplayDuration = 2f;

    private bool gameStarted = false;
    private bool gameEnded = false;
    private bool gameOver = false;
    private bool awaitingNextLevel = false;
    private CanvasGroup canvasGroup;
    private Coroutine hintFadeCoroutine;

    public bool ReadyForNextLevel { get; private set; } = false;

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
        if (!gameStarted && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            StartGame();

        if (awaitingNextLevel && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            ConfirmNextLevel();

        if ((gameEnded || gameOver) && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            RestartGame();
    }

    void ShowInstructions()
    {
        if (instructionText == null) return;
        instructionText.text =
            "<size=120%><b>HOW TO PLAY</b></size>\n\n" +
            "<b>CONTROLS</b>\n\n" +
            "<b>Grab Blocks</b>\n" +
            "  Grip any block to pick it up.\n" +
            "  Hold near a slot to preview placement.\n" +
            "  Release to snap into position.\n\n" +
            "<b>Rotate Held Block</b>\n" +
            "  Hold <b>B</b> (right) -> rotate horizontal\n" +
            "  Hold <b>X</b> (left) -> rotate vertical\n" +
            "  (Rotates template when no block held)\n\n" +
            "<b>Reset Puzzle</b>\n" +
            "  Press <b>Y</b> (left) to reset all blocks.\n\n" +
            "<b>GOAL</b>\n\n" +
            "  Fit the right pieces into the\n" +
            "  glowing template shape.\n" +
            "  No pieces may hang outside!\n\n" +
            "<b>Tip:</b> Orientation matters ->\n" +
            "rotate pieces before placing.\n\n" +
            "n------------------------\n" +
            "  <b>Press A to Start</b>  ";
    }

    public void ShowHintFlash(int hintCount)
    {
        if (instructionText == null) return;

        if (hintFadeCoroutine != null)
            StopCoroutine(hintFadeCoroutine);

        // Activate canvas before theread
        instructionCanvas.gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        instructionText.text = $"<size=120%><b>Hint #{hintCount} recorded</b></size>\n\n" +
                            "This will appear on your results.";

        hintFadeCoroutine = StartCoroutine(HintFlashCoroutine());
    }

    IEnumerator HintFlashCoroutine()
    {
        yield return new WaitForSeconds(hintDisplayDuration);

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
            yield return null;
        }

        instructionCanvas.gameObject.SetActive(false);
        hintFadeCoroutine = null;
    }

    public void ShowLevelComplete(int levelIndex, float levelTime, int totalLevels, List<float> allTimes, bool isLastLevel)
    {
        if (instructionText == null) return;

        if (hintFadeCoroutine != null)
        {
            StopCoroutine(hintFadeCoroutine);
            hintFadeCoroutine = null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        instructionCanvas.gameObject.SetActive(true);

        int mins = Mathf.FloorToInt(levelTime / 60f);
        int secs = Mathf.FloorToInt(levelTime % 60f);

        string text = $"<size=120%><b>Level {levelIndex} Complete!</b></size>\n\n" +
                      $"Time: <b>{mins:00}:{secs:00}</b>\n\n";

        if (allTimes.Count > 1)
        {
            text += "<b>Level Times:</b>\n";
            for (int i = 0; i < allTimes.Count; i++)
            {
                int m = Mathf.FloorToInt(allTimes[i] / 60f);
                int s = Mathf.FloorToInt(allTimes[i] % 60f);
                text += $"  Level {i + 1}: {m:00}:{s:00}\n";
            }
        }

        if (!isLastLevel)
        {
            text += "\n------------------------\n<b>Press A for Next Level</b>";
            awaitingNextLevel = true;
            ReadyForNextLevel = false;
        }

        instructionText.text = text;
    }

    void ConfirmNextLevel()
    {
        awaitingNextLevel = false;
        ReadyForNextLevel = true;
        StartCoroutine(FadeOutCanvas());
        StartCoroutine(ResetReadyFlag());
    }

    IEnumerator ResetReadyFlag()
    {
        yield return null;
        yield return null;
        ReadyForNextLevel = false;
    }

    public void ShowEndScreen(List<float> allTimes, List<TemplateValidator.HintRecord> hints)
    {
        if (instructionText == null) return;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        instructionCanvas.gameObject.SetActive(true);
        gameEnded = true;

        float total = 0f;
        string timesText = "";
        for (int i = 0; i < allTimes.Count; i++)
        {
            total += allTimes[i];
            int m = Mathf.FloorToInt(allTimes[i] / 60f);
            int s = Mathf.FloorToInt(allTimes[i] % 60f);
            timesText += $"  Level {i + 1}: {m:00}:{s:00}\n";
        }

        int totalMins = Mathf.FloorToInt(total / 60f);
        int totalSecs = Mathf.FloorToInt(total % 60f);

        instructionText.text =
            "<size=130%><b>All Done!</b></size>\n\n" +
            "<b>Level Times:</b>\n" +
            timesText +
            $"\n<b>Total: {totalMins:00}:{totalSecs:00}</b>\n\n" +
            BuildHintText(hints) +
            "-----------------------------\n" +
            "  <b>Press A to Play Again</b>";
    }

    public void ShowGameOver(List<float> completedTimes, int reachedLevel, int totalLevels, List<TemplateValidator.HintRecord> hints)
    {
        if (instructionText == null) return;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        instructionCanvas.gameObject.SetActive(true);
        gameOver = true;

        string timesText = "";
        for (int i = 0; i < completedTimes.Count; i++)
        {
            int m = Mathf.FloorToInt(completedTimes[i] / 60f);
            int s = Mathf.FloorToInt(completedTimes[i] % 60f);
            timesText += $"  Level {i + 1}: {m:00}:{s:00}\n";
        }

        string progress = completedTimes.Count == 0
            ? "No levels completed.\n"
            : $"Completed {completedTimes.Count}/{totalLevels} levels.\n\n" +
              "<b>Times:</b>\n" + timesText;

        instructionText.text =
            "<size=130%><b>Time's Up!</b></size>\n\n" +
            progress +
            "\n" + BuildHintText(hints) +
            "-----------------------------\n" +
            "  <b>Press A to Try Again</b>";
    }

    string BuildHintText(List<TemplateValidator.HintRecord> hints)
    {
        if (hints == null || hints.Count == 0)
            return "<b>Hints:</b> None\n\n";

        string text = $"<b>Hints ({hints.Count} total):</b>\n";
        foreach (var h in hints)
        {
            int m = Mathf.FloorToInt(h.timestamp / 60f);
            int s = Mathf.FloorToInt(h.timestamp % 60f);
            text += $"  Level {h.level} at {m:00}:{s:00}\n";
        }
        return text + "\n";
    }

    void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        if (templateValidator != null)
            templateValidator.enabled = true;
        StartCoroutine(FadeOutCanvas());
    }

    void RestartGame()
    {
        gameEnded = false;
        gameOver = false;
        gameStarted = false;
        awaitingNextLevel = false;
        ReadyForNextLevel = false;

        if (templateValidator != null)
        {
            templateValidator.enabled = false;
            templateValidator.enabled = true;
        }

        StartCoroutine(FadeOutCanvas());
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
    }
}