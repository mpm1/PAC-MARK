using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostHealth : MonoBehaviour
{
    private float vulnerableCooldownTime = 12.0f; 
    private float vulnerableTime = 0.0f;

    private void Update()
    {
        if(vulnerableTime > 0.0f)
        {
            vulnerableTime -= Time.fixedTime;
        }
    }

    public void MakeVulnerable()
    {

    }
}
