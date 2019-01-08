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
}
