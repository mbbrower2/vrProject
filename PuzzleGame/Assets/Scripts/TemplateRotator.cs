using UnityEngine;

public class TemplateRotator : MonoBehaviour
{
    public float rotationSpeed = 2f;

    private Quaternion previousControllerRotationRight;
    private Quaternion previousControllerRotationLeft;

    void Update()
    {
        // B (right controller) rotate horizontally
        Transform target = BlockSpawner.CurrentlyHeld != null
            ? BlockSpawner.CurrentlyHeld.transform
            : transform;

        if (OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            Quaternion currentRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            Quaternion delta = currentRotation * Quaternion.Inverse(previousControllerRotationRight);
            float yaw = delta.eulerAngles.y;
            if (yaw > 180f) yaw -= 360f;
            target.Rotate(Vector3.up, yaw * rotationSpeed, Space.World);
            previousControllerRotationRight = currentRotation;
        }

        // X (left controller) rotate vertically
        if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            Quaternion currentRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            Quaternion delta = currentRotation * Quaternion.Inverse(previousControllerRotationLeft);
            float pitch = delta.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            target.Rotate(Vector3.right, pitch * rotationSpeed, Space.World);
            previousControllerRotationLeft = currentRotation;
        }

        // Always update previous rotations 
        if (!OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            previousControllerRotationRight = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

        if (!OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch))
            previousControllerRotationLeft = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
    }
}