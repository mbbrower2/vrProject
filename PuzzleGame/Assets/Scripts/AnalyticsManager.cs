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

    public void MovingToNextLevel(string currentLevel, string nextLevel, float timeElapsed)
    {
        if (!_isInitialized)
        {
            return; // don't try to record if analytics isn't ready
        }

        CustomEvent myEvent = new CustomEvent("MovingToNextLevel")
        {
            {"participantID", participantID},
            { "currentLevel", currentLevel},
            { "nextLevel", nextLevel},
            { "timeElapsed", timeElapsed}
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

}