using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject _explosionPrefab;

    [Header("Movement")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float rotateSpeed = 300f;

    [Header("Target")]
    [SerializeField] private float hitDistance = .5f;

    [Header("Deviation")]
    [SerializeField] private float deviationAmount = 2f;
    [SerializeField] private float deviationSpeed = 2f;

    [Header("Score")]
    [SerializeField] private float scoreForGettingHit = -20f;
    [SerializeField] private float scoreForProjectile = 20f;

    private bool missileDestroyed = false;

    // The missile STILL targets the camera.
    private Transform Target =>
        MissileManager.Instance != null
            ? MissileManager.Instance.Target
            : null;

    private void Start()
    {
        // no longer need tags because of layers
    }

    private void FixedUpdate()
    {
        if (missileDestroyed)
            return;

        Transform target = Target;

        if (target == null)
            return;

        // --------------------------------------------------
        // DISTANCE TO CAMERA
        // --------------------------------------------------

        Vector3 directionToTarget =
            target.position - rb.position;

        float distanceToTarget =
            directionToTarget.magnitude;

        // If we are close enough to the camera,
        // treat this as a hit.
        if (distanceToTarget <= hitDistance)
        {
            HandleCameraHit(target);
            return;
        }

        // --------------------------------------------------
        // HOMING
        // --------------------------------------------------

        Vector3 targetDirection =
            directionToTarget.normalized;

        PredictMovement(target, leadTimePercentage);
        AddDeviation(leadTimePercentage);
        RotateRocket(leadTimePercentage);
    }

    private void HandleCameraHit(Transform target)
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

        // use world deviation for accuracy on the camera positon
        Vector3 predictionOffset =
            deviation *
            deviationAmount *
            leadTimePercentage;

        deviatedPrediction = standardPrediction + predictionOffset;
    }

    private void RotateRocket(float leadTimePercentage)
    {
        Vector3 heading = deviatedPrediction - transform.position;

        if (heading.sqrMagnitude < 0.001f)
            return;

        missileDestroyed = true;

        // tuned turn so fixes ciricling issue
        float closeRangeTurnBoost = Mathf.Lerp(3f, 1f, leadTimePercentage);
        float effectiveRotateSpeed = rotateSpeed * closeRangeTurnBoost;

        rb.MoveRotation(
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                effectiveRotateSpeed * Time.deltaTime
            )
        );

        MissileManager.Instance.ReportMissileDown();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (missileDestroyed)
            return;

        float distance = Vector3.Distance(target.position, transform.position);

        // Arrow destroys missile.
        if (collision.gameObject.CompareTag("Arrow"))
        {
            missileDestroyed = true;

            Explode();

            GameManager.Instance.PlayerScored(
                scoreForProjectile
            );

            MissileManager.Instance.ReportMissileDown();

            return;
        }
    }

    private void Explode()
    {
        if (_explosionPrefab != null)
        {
            Instantiate(
                _explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }
}