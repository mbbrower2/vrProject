using UnityEngine;
using System.Collections.Generic;
using System.Threading;

public enum Difficulty { Easy, Medium, Hard }

public class OscHandler : MonoBehaviour
{
    public static OscHandler Instance { get; private set; }

    [Header("Debug")]
    public bool debugLogs = true;
    private const string LogTag = "[OscHandler]";
    private int mainThreadId;

    private List<bool> boredSignals = new List<bool>();

    // Note: no Time.* calls in here, because oscTransformation may be
    // called from an OSC receiver thread, where Time.* throws.
    void Log(string msg)
    {
        if (debugLogs) Debug.Log($" ABCD {LogTag} {msg}");
    }

    void LogWarn(string msg)
    {
        if (debugLogs) Debug.LogWarning($"{LogTag} {msg}");
    }

    private void Awake()
    {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;

        if (Instance == null)
        {
            Instance = this;
            Log($"Awake: registered as Instance on '{gameObject.name}' (id {GetInstanceID()})");
        }
        else
        {
            LogWarn($"Awake: a second OscHandler on '{gameObject.name}' - destroying it " +
                    $"(existing Instance is on '{Instance.gameObject.name}')");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            LogWarn("OnDestroy: the active Instance is being destroyed - OscHandler.Instance is now null");
            Instance = null;
        }
    }

    public void oscTransformation(bool bored)
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;
        if (threadId != mainThreadId)
            LogWarn($"oscTransformation called from a background thread ({threadId}, main is {mainThreadId}). " +
                    "The signal list is not thread-safe.");

        boredSignals.Add(bored);

        int boredCount = 0;
        foreach (bool b in boredSignals) if (b) boredCount++;
        Log($"Signal received: bored={bored}. Totals: {boredCount} bored / {boredSignals.Count} total");
    }

    public Difficulty GetRecommendedDifficulty(Difficulty currentDifficulty)
    {
        int boredCount = 0;
        foreach (bool b in boredSignals)
        {
            if (b) boredCount++;
        }

        Log($"GetRecommendedDifficulty: current={currentDifficulty}, " +
            $"signals={boredSignals.Count}, bored={boredCount}, notBored={boredSignals.Count - boredCount}, " +
            $"majority threshold: bored > {boredSignals.Count / 2}");

        Difficulty recommended;
        string reason;

        if (boredSignals.Count == 0)
        {
            // No messages received — cycle through difficulties in order
            reason = "NoSignals";
            switch (currentDifficulty)
            {
                case Difficulty.Easy: recommended = Difficulty.Medium; break;
                case Difficulty.Medium: recommended = Difficulty.Hard; break;
                case Difficulty.Hard: recommended = Difficulty.Easy; break;
                default: recommended = currentDifficulty; break;
            }
        }
        else if (boredCount > boredSignals.Count / 2)
        {
            // Player is bored — increase difficulty
            reason = "MajorityBored";
            recommended = currentDifficulty == Difficulty.Easy
                ? Difficulty.Medium
                : Difficulty.Hard;
        }
        else
        {
            // Player is thinking too hard — decrease or keep same difficulty
            reason = "MajorityNotBored";
            recommended = currentDifficulty == Difficulty.Hard
                ? Difficulty.Medium
                : Difficulty.Easy;
        }

        Log($"GetRecommendedDifficulty: reason={reason}, {currentDifficulty} -> {recommended}" +
            (recommended == currentDifficulty ? " (no change)" : ""));

        if (recommended != currentDifficulty)
        {
            Log($"Sending DifficultyChanged analytics (AnalyticsManager.Instance={(AnalyticsManager.Instance != null ? "found" : "NULL")})");
            AnalyticsManager.Instance?.DifficultyChanged(
                currentDifficulty.ToString(),
                recommended.ToString(),
                reason,
                boredCount,
                boredSignals.Count);
            Log("DifficultyChanged analytics call returned");
        }

        return recommended;
    }

    public void ClearSignals()
    {
        Log($"ClearSignals: clearing {boredSignals.Count} signals");
        boredSignals.Clear();
    }
}