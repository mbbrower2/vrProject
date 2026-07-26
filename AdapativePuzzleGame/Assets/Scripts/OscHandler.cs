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

        Debug.Log("ABCD OCS HANDLER CREATED");
    }

    public void oscTransformation(bool bored)
    {
        Debug.Log("ABCD message recevied");
        boredSignals.Add(bored);
        
    }

    public Difficulty GetRecommendedDifficulty(Difficulty currentDifficulty)
    {
        if (boredSignals.Count == 0)
        {
            // No messages received — cycle through difficulties in order
            switch (currentDifficulty)
            {
                case Difficulty.Easy: return Difficulty.Medium;
                case Difficulty.Medium: return Difficulty.Hard;
                case Difficulty.Hard: return Difficulty.Easy;
                default: return currentDifficulty;
            }
        }

        int boredCount = 0;
        foreach (bool b in boredSignals)
        {
            if (b) boredCount++;
        }

        bool majorityBored = boredCount > boredSignals.Count / 2;

        if (majorityBored)
        {
            // Player is bored — increase difficulty
            if (currentDifficulty == Difficulty.Easy)
                return Difficulty.Medium;
            else
                return Difficulty.Hard;
        }
        else
        {
            // Player if im thinking too hard — decrease or keep same difficulty
            if (currentDifficulty == Difficulty.Hard)
                return Difficulty.Medium;
            else
                return Difficulty.Easy;
        }
    }

    public void ClearSignals()
    {
        boredSignals.Clear();
    }
}
