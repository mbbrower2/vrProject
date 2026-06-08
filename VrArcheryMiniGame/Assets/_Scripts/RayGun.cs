using UnityEngine;
using UnityEngine.InputSystem;

public class RayGun : MonoBehaviour
{
    [SerializeField] private InputActionReference shootingActionReference;
    [SerializeField] public LineRenderer linePrefab;
    [SerializeField] public Transform shootingPoint;
    [SerializeField] public float maxLineDistance = 5f;
    [SerializeField] public float lineShowTimer = 0.2f;
    [SerializeField] private GameObject stickingArrowPrefab;

    void OnEnable()
    {
        if (shootingActionReference != null)
            shootingActionReference.action.performed += OnShootPerformed;
    }

    void OnDisable()
    {
        if (shootingActionReference != null)
            shootingActionReference.action.performed -= OnShootPerformed;
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        Shoot();
    }

    public void Shoot()
    {
        Vector3 origin = shootingPoint.position;
        Vector3 direction = shootingPoint.forward;

        LineRenderer line = Instantiate(linePrefab);
        line.positionCount = 2;
        line.SetPosition(0, origin);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxLineDistance))
        {
            line.SetPosition(1, hit.point);

            GameObject burnPoint = Instantiate(stickingArrowPrefab);
            burnPoint.transform.position = hit.point;
            burnPoint.transform.forward = direction;

            if (hit.rigidbody != null)
                burnPoint.transform.parent = hit.rigidbody.transform;

            hit.collider.GetComponent<IHittable>()?.GetHit(hit.point);
        }
        else
        {
            line.SetPosition(1, origin + direction * maxLineDistance);
        }

        Destroy(line.gameObject, lineShowTimer);
    }
}