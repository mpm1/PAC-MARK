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
    private Collider2D[] colliders;
    private new EnvironmentCamera camera;

    private void Awake()
    {
        mGhosts.Add(this);

        animator = GetComponent<Animator>();
        environment = GameObject.FindObjectOfType<Environment>();

        colliders = GetComponents<Collider2D>();
        camera = GameObject.FindObjectOfType<EnvironmentCamera>();
    }

    private void Update()
    {
        if(IsVulnerable())
        {
            vulnerableTime -= Time.deltaTime;

            if(vulnerableTime <= 0.0f)
            {
                animator.SetBool("isVulnerable", false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth player = collision.GetComponent<PlayerHealth>();

        if(player != null)
        {
            if (IsVulnerable())
            {
                camera.TriggerPlayerFocus();
                Died(player);
            }
            else
            {
                player.Hit(gameObject);
            }
        }
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    public void Died(PlayerHealth player)
    {
        animator.SetTrigger("Died");
        animator.ResetTrigger("Alive");
        isAlive = false;

        foreach(Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }

    public void Alive()
    {
        animator.SetTrigger("Alive");
        animator.ResetTrigger("Died");
        vulnerableTime = 0.0f;
        isAlive = true;

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
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

    public bool IsVulnerable()
    {
        return vulnerableTime > 0.0f;
    }

    public void MakeVulnerable()
    {
        animator.SetBool("isVulnerable", true);
        vulnerableTime = vulnerableCooldownTime;
    }
}
