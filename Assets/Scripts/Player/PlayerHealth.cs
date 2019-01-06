using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public bool isAlive = true;

    private PlayerMovement playerMovement;
    private Animator animator;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    public void Hit(GameObject attacker)
    {
        playerMovement.enabled = false;
        animator.SetTrigger("Died");
    }
}
