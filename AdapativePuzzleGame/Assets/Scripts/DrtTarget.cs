using UnityEngine;

public class DrtTarget : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 originPosition;
    private float spawnTime;
    private bool hasCollided = false;
    private string levelName;

    public void Initialize(float timeSpawned, string levelName)
    {
        spawnTime = timeSpawned;
        this.levelName = levelName;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        RegisterCollision(collision.gameObject.name);

    }

    // If your target uses a Trigger collider instead of a solid one
    void OnTriggerEnter(Collider other)
    {

        RegisterCollision(other.gameObject.name);
    }

    void RegisterCollision(string otherName)
    {
        if (hasCollided) return; // avoid double-logging on multi-contact collisions
        hasCollided = true;

        float lifetime = Time.time - spawnTime;

        AnalyticsManager.Instance.DrtTargetResponse(levelName, lifetime);
        Destroy(gameObject);
    }

}