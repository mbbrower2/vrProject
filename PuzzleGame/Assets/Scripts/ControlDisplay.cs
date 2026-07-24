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

    private bool gameStarted = false;
    private bool gameEnded = false;
    private CanvasGroup canvasGroup;

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

    // Called by TemplateValidator after each level completes
    public void ShowLevelComplete(int levelIndex, float levelTime, int totalLevels, List<float> allTimes)
    {
        if (instructionText == null) return;

        // Make canvas visible again if it was hidden
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

        if (levelIndex < totalLevels)
            text += "\n-------------------------\n<b>Now you move onto this next level</b>";

        instructionText.text = text;
    }

    public void ShowEndScreen(List<float> allTimes)
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
            "-------------------------\n" +
            "  <b>Press A to Play Again</b> ";
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
        gameStarted = false;

        // Hide canvas, re-show instructions, restart validator
        StartCoroutine(FadeOutCanvas());

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
    }
}