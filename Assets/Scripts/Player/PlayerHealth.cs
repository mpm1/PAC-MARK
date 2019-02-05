using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public bool isAlive = true;
    public Menu menu;

    private PlayerMovement playerMovement;
    private Animator animator;
    private Rigidbody2D rigidbody;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Hit(GameObject attacker)
    {
        playerMovement.enabled = false;
        rigidbody.simulated = false;

        animator.SetTrigger("Died");
    }

    public void AfterDie()
    {
        menu.TriggerGameOver();
    }
}
