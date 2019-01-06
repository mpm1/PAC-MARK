using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentCamera : MonoBehaviour
{
    public GameObject environment;
    public PlayerMovement player;
    public bool followPlayer = false;
    public float smoothing = 0.2f;

    private new Camera camera;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        Vector3 newPosition;
        float zoom;
        float z = transform.position.z;

        if (followPlayer)
        {
            newPosition = player.transform.position;
            zoom = 2.0f; //TODO: increase the camera size to show all ghosts.
        }
        else
        {
            newPosition = environment.transform.position;
            zoom = Mathf.Max(environment.transform.lossyScale.x, environment.transform.lossyScale.y) / 2.0f;
        }

        newPosition.z = z;
        transform.position = Vector3.Lerp(transform.position, newPosition, smoothing);
        camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, zoom, smoothing);
    }
}
