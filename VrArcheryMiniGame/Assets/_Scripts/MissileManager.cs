using UnityEngine;

public class MissileManager : MonoBehaviour
{
    public GameObject missle;

    public static MissileManager Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;

    public Transform Target => target;

    private int currentMissileCount = 0;

    public int CurrentMissileCount =>
        currentMissileCount;

    public int maxMissles;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // always target the main camera, overriding anything set in the Inspector
        if (Camera.main != null)
        {
            target = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("MissileManager: Camera.main is null at Awake. Make sure your camera is tagged 'MainCamera'.");
        }
    }

    // Update is called once per frame
    void Update()
    {   
        // Fallback in case Camera.main wasn't ready during Awake (e.g. camera
        // spawned in after this script, or script execution order issue).
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        if (currentMissileCount < maxMissles)
        {
            Vector3 randomSpawnPosition =
                new Vector3(
                    Random.Range(-20f, 21f),
                    5f,
                    Random.Range(-20f, 21f)
                );

            Instantiate(
                missle,
                randomSpawnPosition,
                Quaternion.identity
            );

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

        if (currentMissileCount < 0)
        {
            currentMissileCount = 0;
        }
    }
}