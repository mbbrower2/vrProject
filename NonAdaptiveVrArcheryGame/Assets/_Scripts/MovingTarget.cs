using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MovingTarget : MonoBehaviour, IHittable
{
    private Rigidbody rb;
    private bool stopped = false;
    private Vector3 nextposition;
    private Vector3 originPosition;

    [SerializeField] private int health = 1;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float arriveThreshold, movementRadius = 2, speed = 1;
    [SerializeField] private bool isMoving = true;
    [SerializeField] float targetScoreValue;

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
        if (stopped == false)
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

        // Clamp so misses outside the target always score 0
        var clampedDistance = Mathf.Clamp(distanceFromTarget, 0f, maxDistance);

        // Invert so bullseye (distance = 0) = 100, edge (distance = max) = 0
        var score = (1f - (clampedDistance / maxDistance)) * 100f;

        return score;
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;

        if (isMoving)
        {
            stopped = false;
            nextposition = GetNewMovementPosition();
        }
        else
        {
            stopped = true;
        }
    }
}

public interface IHittable
{
    void GetHit(Vector3 hitpoint);
}