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
        Collider missileCollider = GetComponent<Collider>();

        if (missileCollider != null)
        {
            foreach (GameObject environmentObject in
                     GameObject.FindGameObjectsWithTag("Environment"))
            {
                Collider environmentCollider =
                    environmentObject.GetComponent<Collider>();

                if (environmentCollider != null)
                {
                    Physics.IgnoreCollision(
                        missileCollider,
                        environmentCollider
                    );
                }
            }
        }
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

        // --------------------------------------------------
        // SMALL WOBBLE
        // --------------------------------------------------
        //
        // IMPORTANT:
        // The wobble does NOT move the target point.
        // The missile still fundamentally aims at
        // the camera.
        //

        Vector3 wobble =
            new Vector3(
                Mathf.Sin(Time.time * deviationSpeed),
                Mathf.Cos(Time.time * deviationSpeed * 0.7f),
                0f
            );

        Quaternion wobbleRotation =
            Quaternion.AngleAxis(
                deviationAmount,
                transform.forward
            );

        Vector3 deviatedDirection =
            wobbleRotation * targetDirection;

        // --------------------------------------------------
        // ROTATE
        // --------------------------------------------------

        Quaternion targetRotation =
            Quaternion.LookRotation(deviatedDirection);

        Quaternion newRotation =
            Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );

        rb.MoveRotation(newRotation);

        // --------------------------------------------------
        // MOVE
        // --------------------------------------------------

        rb.linearVelocity =
            newRotation * Vector3.forward * speed;
    }

    private void HandleCameraHit(Transform target)
    {
        if (missileDestroyed)
            return;

        missileDestroyed = true;

        // Determine whether the missile hit from the front.
        Vector3 directionToMissile =
            (transform.position - target.position).normalized;

        bool hitFromFront =
            Vector3.Dot(
                target.forward,
                directionToMissile
            ) > 0f;

        // Hit from behind = destroy silently.
        if (!hitFromFront)
        {
            Destroy(gameObject);

            MissileManager.Instance.ReportMissileDown();

            return;
        }

        // Hit from front = player gets penalized.
        Explode();

        GameManager.Instance.PlayerScored(
            scoreForGettingHit
        );

        MissileManager.Instance.ReportMissileDown();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (missileDestroyed)
            return;

        // Environment is ignored by Physics.IgnoreCollision.
        if (collision.gameObject.CompareTag("Environment"))
        {
            return;
        }

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