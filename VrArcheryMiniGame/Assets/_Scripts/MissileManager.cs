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

        // Keep the camera as the target.
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    private void Update()
    {
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