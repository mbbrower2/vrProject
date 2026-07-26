using UnityEngine;

public class DrtTargetSpawner : MonoBehaviour
{
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private TemplateValidator templateValidator;

    private float timer;
    private GameObject currentTarget; // tracks the active target, if any

    void Update()
    {   

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            // Only spawn if there's no target currently alive
            if (currentTarget == null)
            {
                SpawnTarget();
            }
        }
    }

    void SpawnTarget()
    {
        Vector3 spawnPos = GetSpawnPosition();

        currentTarget = Instantiate(targetPrefab, spawnPos, Quaternion.identity);

        DrtTarget targetScript = currentTarget.GetComponent<DrtTarget>();
  
        if (targetScript != null)
        {
            targetScript.Initialize(Time.time, templateValidator.CurrentLevel.templateName);
        }
    }

    Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = Random.Range(0, spawnPoints.Length);
            return spawnPoints[index].position;
        }

        return new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }
}