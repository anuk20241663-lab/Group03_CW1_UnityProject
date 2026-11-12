
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2.5f, -4f);
    public float smoothSpeed = 8f;
    public float lookHeight = 0.7f;

    void LateUpdate()
    {
        if (target == null)
            return;

        // Calculate the camera's desired position.
        Vector3 desiredPosition = target.position + offset;

        // Smoothly follow the player.
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // Keep the player in view.
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}

