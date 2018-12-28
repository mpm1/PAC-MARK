using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatableObject : MonoBehaviour
{
    public int score = 10;
     
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerScore playerScore = collision.gameObject.GetComponent<PlayerScore>();
        if(playerScore != null)
        {
            playerScore.AddScore(score);
            AnimateAndDestroy();
        }
    }

    protected void AnimateAndDestroy()
    {
        Destroy(gameObject);
    }
}
