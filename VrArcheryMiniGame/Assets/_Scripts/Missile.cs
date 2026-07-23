using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject _explosionPrefab;

    [Header("Movement")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float rotateSpeed = 300f;

    [Header("Prediction")]
    [SerializeField] private float maxDistancePredict = 100f;
    [SerializeField] private float minDistancePredict = 5f;
    [SerializeField] private float maxTimePrediction = 5f;

    private Vector3 standardPrediction;
    private Vector3 deviatedPrediction;

    [Header("Deviation")]
    [SerializeField] private float deviationAmount = 5f;
    [SerializeField] private float deviationSpeed = 2f;

    [Header("Score")]
    [SerializeField] float scoreForGettingHit = -20;
    [SerializeField] float scoreForProjectile = 20;

    // Target now comes from MissileManager instead of being set per-missile.
    private Transform Target => MissileManager.Instance != null ? MissileManager.Instance.Target : null;

    private void Start()
    {
        // The "return" in OnCollisionEnter only skips our game logic — physics
        // still resolves the actual collision and knocks the missile off its
        // trajectory before that code even runs. To let the missile fly
        // straight through Environment objects, tell the physics engine to
        // ignore those collisions entirely at spawn time.
        Collider missileCollider = GetComponent<Collider>();
        if (missileCollider != null)
        {
            foreach (GameObject environmentObject in GameObject.FindGameObjectsWithTag("Environment"))
            {
                Collider environmentCollider = environmentObject.GetComponent<Collider>();
                if (environmentCollider != null)
                {
                    Physics.IgnoreCollision(missileCollider, environmentCollider);
                }
            }
        }
    }
    private void Update()
    {
        Transform target = Target;

        if (target == null)
        {
            Debug.LogWarning($"{gameObject.name}: No target assigned in MissileManager.");
            return;
        }

        // Move forward continuously
        rb.linearVelocity = transform.forward * speed;

        float leadTimePercentage = Mathf.InverseLerp(
            minDistancePredict,
            maxDistancePredict,
            Vector3.Distance(transform.position, target.position)
        );

        PredictMovement(target, leadTimePercentage);
        AddDeviation(leadTimePercentage);
        RotateRocket();
    }

    private void PredictMovement(Transform target, float leadTimePercentage)
    {
        // Since the camera has no Rigidbody, just aim at its current position.
        standardPrediction = target.position;
    }

    private void AddDeviation(float leadTimePercentage)
    {
        Vector3 deviation = new Vector3(
            Mathf.Cos(Time.time * deviationSpeed),
            0,
            Mathf.Sin(Time.time * deviationSpeed)
        );

        Vector3 predictionOffset =
            transform.TransformDirection(deviation) *
            deviationAmount *
            leadTimePercentage;

        deviatedPrediction = standardPrediction + predictionOffset;
    }

    private void RotateRocket()
    {
        Vector3 heading = deviatedPrediction - transform.position;

        if (heading.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(heading);

        rb.MoveRotation(
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            )
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        Transform target = Target;
        if (target == null)
            return;

        // Environment collisions are ignored via Physics.IgnoreCollision in Start(),
        // so this shouldn't fire for Environment-tagged objects anymore. Kept as a
        // safety net in case an Environment object spawns in after this missile does.
        if (collision.gameObject.CompareTag("Environment"))
        {
            return;
        }
        float distance = Vector3.Distance(target.position, transform.position);

        // Check if missile hit from the front (dot > 0 means missile is in front of target)
        Vector3 directionToMissile = (transform.position - target.position).normalized;
        bool hitFromFront = Vector3.Dot(target.forward, directionToMissile) > 0f;

        // Hit the target (player / camera object)
        if (distance <= 5f && !collision.gameObject.CompareTag("Arrow"))
        {
            if (!hitFromFront)
            {
                Destroy(gameObject); // Silent destroy, no score
                MissileManager.Instance.ReportMissileDown();
                return;
            }
            Explode();
            GameManager.Instance.PlayerScored(scoreForGettingHit);
            MissileManager.Instance.ReportMissileDown();
            return;
        }

        // Hit by an arrow
        if (collision.gameObject.CompareTag("Arrow"))
        {
            Explode();
            GameManager.Instance.PlayerScored(scoreForProjectile);
            MissileManager.Instance.ReportMissileDown();
            return;
        }
    }

    private void Explode()
    {
        if (_explosionPrefab != null)
        {
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}