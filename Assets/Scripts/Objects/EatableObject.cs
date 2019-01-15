using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatableObject : MonoBehaviour
{
    public int score = 10;
    public bool triggerGhostVulnerable = false;
     
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerScore playerScore = collision.gameObject.GetComponent<PlayerScore>();
        if(playerScore != null)
        {
            playerScore.AddScore(score);
            AnimateAndDestroy();

            if (triggerGhostVulnerable)
            {
                foreach(GhostHealth ghost in GhostHealth.Ghosts)
                {
                    ghost.MakeVulnerable();
                }
            }
        }
    }

    protected void AnimateAndDestroy()
    {
        Destroy(gameObject);
    }
}
