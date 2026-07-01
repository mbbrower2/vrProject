using UnityEngine;

public class DrtTarget : MonoBehaviour, IHittable
{
    private Rigidbody rb;
    private Vector3 originPosition;
    private float spawnTime;
    private bool hasCollided = false;

    public void Initialize(float timeSpawned)
    {
        spawnTime = timeSpawned;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originPosition = transform.position;
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((rb.isKinematic || collision.gameObject.CompareTag("Laser")) == false)
        {
            RegisterCollision(collision.gameObject.name);
        }
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

        // Do whatever you need with this value — send it to a GameManager,
        // add to a stats list, trigger a score event, etc.
        // Example: GameManager.Instance.RecordTargetLifetime(lifetime);
        AnalyticsManager.Instance.DrtTargetResponse( GameManager.Instance.GetCurrentSceneName(), lifetime);
        Destroy(gameObject);
    }

    public void GetHit(Vector3 hitpoint)
    {    
        rb.isKinematic = false;
        return;
    }
}