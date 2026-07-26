using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Transform cameraRig;
    public Transform centerEyeAnchor;

    void Update()
    {
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (input.sqrMagnitude < 0.01f) return;

        Vector3 forward = centerEyeAnchor.forward;
        Vector3 right = centerEyeAnchor.right;

        // movement horizontal only
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (forward * input.y + right * input.x) * moveSpeed * Time.deltaTime;
        cameraRig.position += move;
    }
}