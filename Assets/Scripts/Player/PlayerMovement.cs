using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5.0f;

    private new Rigidbody2D rigidbody;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
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
        // To keep that PAC-MAN style movements, we'll figure if we can move either vertical or horizontal, then move in that direction.
        Vector2 position = transform.position;
        Vector2 direction = new Vector2(h, v).normalized;
        rigidbody.MovePosition(position + (speed * Time.fixedDeltaTime * direction));
    }
}
