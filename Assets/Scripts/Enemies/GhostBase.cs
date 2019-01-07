using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GhostBase : MonoBehaviour
{
    public float speed = 3f;
    public float eyeRadius = 0.15f;

    protected Environment environment;
    protected PlayerMovement player;

    private Vector2 waypoint;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private new SpriteRenderer renderer;
    private Transform eyes;
    private Vector3 eyePosition;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        renderer = GetComponent<SpriteRenderer>();

        environment = GameObject.FindObjectOfType<Environment>();
        player = GameObject.FindObjectOfType<PlayerMovement>();

        eyes = transform.GetChild(0);
        eyePosition = eyes.localPosition;
    }

    private void Update()
    {
        waypoint = CalculateNextWaypoint();
    }

    private void FixedUpdate()
    {
        animator.SetBool("isMoving", waypoint.magnitude > 0.0f);

        if(waypoint.x < 0.0f)
        {
            renderer.flipX = true;
        }else if (waypoint.x > 0.0f)
        {
            renderer.flipX = false;
        }

        Vector2 position = transform.position;
        Vector2 direction = new Vector2(waypoint.x, waypoint.y).normalized;
        rigidbody.MovePosition(position + (speed * Time.fixedDeltaTime * direction));

        // Set the eye movement
        Vector3 eyeVector = (player.transform.position - transform.position).normalized;
        eyes.localPosition = eyePosition + (eyeVector * eyeRadius);
    }

    protected Vector2 FindNextPathNode(Vector2 targetLocation)
    {
        //TODO: path finding
        return targetLocation;
    }

    protected abstract Vector2 CalculateNextWaypoint();
}
