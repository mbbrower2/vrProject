using UnityEngine;

public class MissileManager : MonoBehaviour
{
    public GameObject missle;
    public static MissileManager Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;
    public Transform Target => target;

    private int currentMissileCount = 0;
    public int CurrentMissileCount => currentMissileCount; 
    public int maxMissles;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Fall back to the main camera if no target was assigned in the Inspector.
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {   
        if (currentMissileCount < maxMissles)
        {
            Vector3 randomSpawnPosition = new Vector3(
                Random.Range(-20, 21),
                5,
                Random.Range(-20, 21)
            );
            Instantiate(missle, randomSpawnPosition, Quaternion.identity);
            // Missile now pulls its target from MissileManager.Instance.Target itself,
            // so no SetTarget call is needed here.
            RegisterMissile();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
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