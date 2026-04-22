using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    private Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f); // -10 Z keeps the camera behind the 2D action

    // We call this from the Player script when they spawn
    public void SetTarget(Transform playerTransform, PlayerRole role)
    {
        target = playerTransform;

        // 1. Zoom the camera in so they don't see the whole maze
        Camera.main.orthographicSize = 7f; // Adjust this number if it's too close/far

        // 2. Rotate the camera based on gravity!
        if (role == PlayerRole.PlayerX)
        {
            // Player X looks normal
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (role == PlayerRole.PlayerY)
        {
            // Player Y's camera rotates -90 degrees. 
            // This magically turns the Left Wall into the Bottom Floor!
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, -90f);
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Smoothly track the player
            Vector3 desiredPosition = target.position + offset;
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}