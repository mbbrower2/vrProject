using UnityEngine;

public class Missile : MonoBehaviour
{
[SerializeField] private Transform target;

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

//TODO: the path of the rocket needs to be updated a little bit so that its in the line of vision of the player
private void Update()
{
    if (target == null)
    {
        Debug.LogWarning($"{gameObject.name}: No target assigned.");
        return;
    }

    // Move forward continuously
    rb.linearVelocity = transform.forward * speed;

    float leadTimePercentage = Mathf.InverseLerp(
        minDistancePredict,
        maxDistancePredict,
        Vector3.Distance(transform.position, target.position)
    );

    PredictMovement(leadTimePercentage);
    AddDeviation(leadTimePercentage);
    RotateRocket();
}

private void PredictMovement(float leadTimePercentage)
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

public void SetTarget(Transform t)
{
    target = t;
}
}