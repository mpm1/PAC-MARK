using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blinky : GhostBase
{
    protected override Vector2 CalculateNextWaypoint()
    {
        Vector2 location = (Vector2)player.transform.position;

        return FindNextPathLocation(location);
    }

    protected override float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return Vector2.Distance(target, current) * score;
    }
}
