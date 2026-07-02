using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialTarget : MonoBehaviour, IHittable
{
    private Rigidbody rb;
    private Vector3 originPosition;

    [SerializeField] private int health = 1;
    [SerializeField] private AudioSource audioSource;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originPosition = transform.position;
    }

    private void Start()
    {
        TargetManager.Instance.RegisterTarget();
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

            TargetManager.Instance.ReportTargetDown();

            return;
        }

    }


}