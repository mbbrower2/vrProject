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
        try
        {
            // 1. Initialize core Unity Gaming Services
            await UnityServices.InitializeAsync();

            // 2. Opt-in/Start data collection (Required for privacy compliance)
            AnalyticsService.Instance.StartDataCollection();

            _isInitialized = true;
            Debug.Log("ABCD Unity Analytics successfully initialized.");
        }
        catch (Exception e)
        {
            Debug.LogError($"ABCD Analytics failed to initialize: {e.Message}");
        }
    }

    public void MovingToNextLevel(string currentScene, string nextScene)
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("MovingToNextLevel")
        {
            {"participantID", participantID},
            { "currentScene", currentScene},
            { "nextScene", nextScene}
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void TargetHit(string currentScene, float accuracy )
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("TargetHit")
        {
            {"participantID", participantID},
            { "currentScene", currentScene},
            { "accuracy", accuracy}
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

        CustomEvent myEvent = new CustomEvent("DrtTargetResponse")
        {
            {"participantID", participantID},
            { "currentScene", currentScene},
            { "timeElapsed", timeElapsed}
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void UpdateScore(string currentScene, float score )
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("UpdateScore")
        {
            {"participantID", participantID},
            { "currentScene", currentScene},
            { "score", score}
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void UpdateSpeed(string currentScene ,float speed)
    {
                if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("UpdateSpeed")
        {
            {"participantID", participantID},
            { "currentScene", currentScene},
            { "newSpeed", speed}
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }

    public void UpdateSize(string currentScene ,float size)
    {
                if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("UpdateSize")
        {
            {"participantID", participantID},
            { "currentScene", currentScene},
            { "newSpeed", size}
        };

        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush();
    }
}