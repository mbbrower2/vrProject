using UnityEngine;

public class MissileManager : MonoBehaviour
{
    public GameObject missle;
    public static MissileManager Instance { get; private set; }
    private int currentMissileCount = 0;
    public int CurrentMissileCount => currentMissileCount; 
    public int maxMissles;

    // Update is called once per frame
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    void Update()
    {   
        if (currentMissileCount < maxMissles)
        {
            Vector3 randomSpawnPosition = new Vector3(
                Random.Range(-20, 21),
                5,
                Random.Range(-20, 21)
            );
            GameObject newMissile = Instantiate(missle, randomSpawnPosition, Quaternion.identity);
            newMissile.GetComponent<Missile>().SetTarget(Camera.main.transform); // <-- Set target here
            RegisterMissile();
        }
    }

    public void updateMaxMissles(int numMissles)
    {
        maxMissles = numMissles;
    }

    public int getMaxMissles()
    {
        return maxMissles;
    }

    public int getCurrMissles()
    {
        return currentMissileCount;
    }

    public void RegisterMissile()
    {
        currentMissileCount++;
    }

    public void ReportMissileDown()
    {
        currentMissileCount--;
        if(currentMissileCount < 0)
        {
            currentMissileCount = 0;
        } 
    }
}
