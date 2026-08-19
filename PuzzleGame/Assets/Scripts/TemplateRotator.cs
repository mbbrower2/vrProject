using UnityEngine;

public class TemplateRotator : MonoBehaviour
{
    public float rotationSpeed = 90f;

    void Update()
    {
        bool bHeld = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
        bool xHeld = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);

        if (BlockSpawner.CurrentlyHeld != null)
        {
            Transform visual = BlockSpawner.CurrentlyHeld.VisualWrapper;
            if (visual != null)
            {
                if (bHeld) visual.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                if (xHeld) visual.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.World);
            }
        }
        else
        {
            if (bHeld) transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            if (xHeld) transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}