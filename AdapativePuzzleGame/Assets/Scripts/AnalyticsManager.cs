using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;

public class AnalyticsManager : MonoBehaviour
{
    static AnalyticsManager instance;
    public static AnalyticsManager Instance { get { return instance; } }
    [SerializeField] public string participantID;

    private bool _isInitialized = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    async void Start()
    {
        // 1. Initialize core Unity Gaming Services
        await UnityServices.InitializeAsync();

        // 2. Opt-in/Start data collection (Required for privacy compliance)
        AnalyticsService.Instance.StartDataCollection();

        _isInitialized = true;
    }

    // timedOut = true when the player was moved on automatically after the
    // per-level time limit, false when they solved the puzzle.
    // Difficulties are "Easy", "Medium", "Hard", or "None" when there is no next level.
    public void MovingToNextLevel(string currentLevel, string currentDifficulty, string nextLevel, string nextDifficulty, float timeElapsed, bool timedOut)
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("MovingToNextLevel")
        {
            { "participantID", participantID },
            { "currentScene", currentLevel },
            { "currentDifficulty", currentDifficulty },
            { "nextScene", nextLevel },
            { "nextDifficulty", nextDifficulty },
            { "timeTaken", timeElapsed },
            { "timedOut", timedOut }
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    // Sent once when a run ends, either by finishing the last level or by
    // hitting the session time limit.
    // endReason: "AllLevelsFinished" or "SessionTimeLimit".
    public void SessionEnded(float totalTimePlayed, int puzzlesSeen, int puzzlesCompleted, string endReason)
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("SessionEnded")
        {
            { "participantID", participantID },
            { "totalTimePlayed", totalTimePlayed },
            { "puzzlesSeen", puzzlesSeen },
            { "puzzlesCompleted", puzzlesCompleted },
            { "endReason", endReason }
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    // Sent whenever the difficulty actually changes.
    // reason: "NoSignals", "MajorityBored" or "MajorityNotBored".
    public void DifficultyChanged(string previousDifficulty, string newDifficulty, string reason, int boredSignals, int totalSignals)
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("DifficultyChanged")
        {
            { "participantID", participantID },
            { "previousDifficulty", previousDifficulty },
            { "newDifficulty", newDifficulty },
            { "reason", reason },
            { "boredSignals", boredSignals },
            { "totalSignals", totalSignals }
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void DrtTargetResponse(string currentScene, float timeElapsed)
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("DrtTargetResponseTime")
        {
            { "participantID", participantID },
            { "currentScene", currentScene },
            { "responseTime", timeElapsed }
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }
}