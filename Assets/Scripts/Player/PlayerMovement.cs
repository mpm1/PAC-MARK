using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5.0f;

    private new Rigidbody2D rigidbody;
    private Animator animator;
    private new SpriteRenderer renderer;
    private Environment environment;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        renderer = GetComponent<SpriteRenderer>();

        environment = GameObject.FindObjectOfType<Environment>();
    }

    private void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        //TODO check if we can go in the direction selected.
        if(Mathf.Abs(h) < Mathf.Abs(v))
        {
            h = 0.0f;
            v = Input.GetAxisRaw("Vertical");
        }
        else
        {
            h = Input.GetAxisRaw("Horizontal");
            v = 0.0f;
        }

        Move(h, v);
    }

    private void Move(float h, float v)
    {
        float rotation = rigidbody.rotation;

        if (h > 0.0f)
        {
            renderer.flipX = false;
            rigidbody.MoveRotation(0f);
            animator.SetBool("isMoving", true);
        }
        else if(h < 0.0f)
        {
            rigidbody.MoveRotation(0f);
            renderer.flipX = true;
            animator.SetBool("isMoving", true);
        }
        else if(v < 0.0f)
        {
            renderer.flipX = false;
            rigidbody.MoveRotation(-90f);
            animator.SetBool("isMoving", true);
        }
        else if(v > 0.0f)
        {
            renderer.flipX = false;
            rigidbody.MoveRotation(90f);
            animator.SetBool("isMoving", true);
        }
        else{
            animator.SetBool("isMoving", false);
        }
        
        
        // To keep that PAC-MAN style movements, we'll figure if we can move either vertical or horizontal, then move in that direction.
        Vector2 position = transform.position;
        Vector2 direction = new Vector2(h, v).normalized;
        rigidbody.MovePosition(position + (speed * Time.fixedDeltaTime * direction));
        
    }

    private void LateUpdate()
    {
        if(environment != null)
        {
            environment.HandleObjectOnEdge(transform);
        }
    }
}
