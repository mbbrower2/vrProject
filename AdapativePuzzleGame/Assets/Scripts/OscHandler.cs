using UnityEngine;
using System.Collections.Generic;

public enum Difficulty { Easy, Medium, Hard }

public class OscHandler : MonoBehaviour
{
    public static OscHandler Instance { get; private set; }

    private List<bool> boredSignals = new List<bool>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void oscTransformation(bool bored)
    {
        boredSignals.Add(bored);
    }

    public Difficulty GetRecommendedDifficulty(Difficulty currentDifficulty)
    {
        int boredCount = 0;
        foreach (bool b in boredSignals)
        {
            if (b) boredCount++;
        }

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

        if (recommended != currentDifficulty)
        {
            AnalyticsManager.Instance?.DifficultyChanged(
                currentDifficulty.ToString(),
                recommended.ToString(),
                reason,
                boredCount,
                boredSignals.Count);
        }

        return recommended;
    }

    public void ClearSignals()
    {
        boredSignals.Clear();
    }
}