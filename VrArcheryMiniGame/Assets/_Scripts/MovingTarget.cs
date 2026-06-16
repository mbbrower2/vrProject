using UnityEngine;
using TMPro;

public class MovingTarget : MonoBehaviour, IHittable
{
    private Rigidbody rb;
    private bool stopped = false;
    private Vector3 nextposition;
    private Vector3 originPosition;

    [SerializeField] private int health = 1;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float arriveThreshold, movementRadius = 2;
    [SerializeField] float targetScoreValue;

    private bool isMoving => TargetManager.Instance.IsMoving;
    private float speed => TargetManager.Instance.Speed;
    private float size => TargetManager.Instance.Size;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originPosition = transform.position;

        if (isMoving)
            nextposition = GetNewMovementPosition();
        else
            stopped = true;
    }

    private void Start()
    {
        TargetManager.Instance.RegisterTarget();
        TargetManager.Instance.OnTargetSettingsChanged += ApplySettings;
        ApplySettings();
    }

    private void OnDestroy()
    {
        if (TargetManager.Instance != null)
            TargetManager.Instance.OnTargetSettingsChanged -= ApplySettings;
    }

    private void ApplySettings()
    {
        stopped = !isMoving;
        if (!stopped)
            nextposition = GetNewMovementPosition();
        transform.localScale = Vector3.one * size;
    }

    private Vector3 GetNewMovementPosition()
    {
        return originPosition + (Vector3)Random.insideUnitCircle * movementRadius;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((rb.isKinematic || collision.gameObject.CompareTag("Laser")) == false)
        {
            audioSource.Play();
        }
    }

    public void GetHit(Vector3 hitpoint)
    {
        health--;

        if (health <= 0)
        {
            rb.isKinematic = false;
            stopped = true;

            TargetManager.Instance.ReportTargetDown();
            var accuracyScore = CalculateAccuracy(hitpoint);
            var scoreForTarget = accuracyScore + targetScoreValue;
            GameManager.Instance.PlayerScored(scoreForTarget);

            return;
        }
    }

    private void FixedUpdate()
    {
        if (!stopped)
        {
            if (Vector3.Distance(transform.position, nextposition) < arriveThreshold)
            {
                nextposition = GetNewMovementPosition();
            }

            Vector3 direction = nextposition - transform.position;
            rb.MovePosition(transform.position + direction.normalized * Time.fixedDeltaTime * speed);
        }
    }

    public float CalculateAccuracy(Vector3 hitpoint)
    {
        var maxDistance = 0.12f;
        var distanceFromTarget = Vector3.Distance(transform.position, hitpoint);
        var clampedDistance = Mathf.Clamp(distanceFromTarget, 0f, maxDistance);
        var score = (1f - (clampedDistance / maxDistance)) * 100f;
        return score;
    }
}

public interface IHittable
{
    void GetHit(Vector3 hitpoint);
}