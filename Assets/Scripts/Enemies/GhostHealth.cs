using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostHealth : MonoBehaviour
{
    public float vulnerableCooldownTime = 12.0f; 

    public float vulnerableTime = 0.0f;

    private static List<GhostHealth> mGhosts = new List<GhostHealth>();
    public static IReadOnlyList<GhostHealth> Ghosts
    {
        get
        {
            return mGhosts.AsReadOnly();
        }
    }

    private bool isAlive = true;
    private Animator animator;
    private Environment environment;

    private void Awake()
    {
        mGhosts.Add(this);

        animator = GetComponent<Animator>();
        environment = GameObject.FindObjectOfType<Environment>();
    }

    private void Update()
    {
        if(vulnerableTime > 0.0f)
        {
            vulnerableTime -= Time.deltaTime;

            if(vulnerableTime <= 0.0f)
            {
                animator.SetBool("isVulnerable", true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth player = collision.GetComponent<PlayerHealth>();

        if(player != null)
        {
            if (vulnerableTime > 0.0f)
            {
                Died(player);
            }
            else
            {
                player.Hit(gameObject);
            }
        }
    }

    public void Died(PlayerHealth player)
    {
        animator.SetTrigger("Died");
        isAlive = false;

        //TODO: Disable collision and move back to the center.
    }

    public bool CanMove(ref Vector2 location)
    {
        if (!isAlive)
        {
            if (Vector2.Distance(location, environment.center) <= 5.0f)
            {
                animator.ResetTrigger("Died");
                isAlive = true;
            }
        }

        return true;
    }

    public void MakeVulnerable()
    {
        animator.SetBool("isVulnerable", true);
        vulnerableTime = vulnerableCooldownTime;
    }
}
