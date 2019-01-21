﻿using System.Collections.Generic;
using UnityEngine;
using static EnvironmentBase;

public enum eMovementType
{
    Chase = 0,
    Flank,
    Random,
    Evade
};

public static class GhostMovementExtensions {
    public static GhostMovementPattern GetMovementPattern(this eMovementType movementType)
    {
        switch (movementType)
        {
            case eMovementType.Chase:
                return new ChaseMovementPattern();

            case eMovementType.Evade:
                return new EvadeMovementPattern();

            case eMovementType.Flank:
                return new FlankMovementPattern();
        }

        return new RandomMovementPattern();
    }
}

public class GhostMovement : MonoBehaviour
{
    private static Vector2[] movementDirections = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
    public float speed = 3f;
    public float eyeRadius = 0.15f;

    public eMovementType regularMovement = eMovementType.Chase;
    public eMovementType vulnerableMovement = eMovementType.Evade;

    protected Environment environment;
    protected PlayerMovement player;

    private Vector2 waypoint;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private new SpriteRenderer renderer;
    private Transform eyes;
    private Vector3 eyePosition;
    private GhostHealth health;

    private GhostMovementPattern regularPattern;
    private GhostMovementPattern vulnerablePattern;

    private MapPathNode lastPath = null;

    private void OnDrawGizmos()
    {
        if (lastPath != null)
        {
            Gizmos.color = renderer.color;
            Vector2 lastLocation = lastPath.node.Value.location;
            MapPathNode current = lastPath;

            while(current != null)
            {
                Gizmos.DrawLine(lastLocation, current.node.Value.location);

                lastLocation = current.node.Value.location;
                current = current.cameFrom;
            }

            Gizmos.DrawLine(lastLocation, transform.position);
        }
    }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        renderer = GetComponent<SpriteRenderer>();

        environment = GameObject.FindObjectOfType<Environment>();
        player = GameObject.FindObjectOfType<PlayerMovement>();
        health = GetComponent<GhostHealth>();

        eyes = transform.GetChild(0);
        eyePosition = eyes.localPosition;

        regularPattern = regularMovement.GetMovementPattern();
        vulnerablePattern = vulnerableMovement.GetMovementPattern();
    }

    private void Update()
    {
        if (health.IsAlive())
        {
            waypoint = FindNextPathLocation(health.IsVulnerable() ? vulnerablePattern : regularPattern);
        }
        else
        {
            waypoint = environment.center;
        }
    }

    private void FixedUpdate()
    {
        animator.SetBool("isMoving", waypoint.magnitude > 0.0f);

        if(waypoint.x < 0.0f)
        {
            renderer.flipX = true;
        }else if (waypoint.x > 0.0f)
        {
            renderer.flipX = false;
        }

        Vector2 position = transform.position;
        Vector2 direction = (waypoint - (Vector2)transform.position).normalized;
        rigidbody.MovePosition((Vector2)transform.position + (speed * Time.fixedDeltaTime * direction));

        // Set the eye movement
        Vector3 eyeVector = (player.transform.position - transform.position).normalized;
        eyes.localPosition = eyePosition + (eyeVector * eyeRadius);
    }

    private void LateUpdate()
    {
        if (environment != null)
        {
            environment.HandleObjectOnEdge(transform);
        }

        if (!health.IsAlive())
        {
            if(Vector2.Distance((Vector2)transform.position, waypoint) < 0.1f)
            {
                health.Alive();
            }
        }
    }

    protected Vector2 FindNextPathLocation(GhostMovementPattern pattern)
    {
        Vector2 targetLocation = pattern.CalculateTarget(transform, environment, player.gameObject);

        //TODO: optimize to reduce garbage colleciton
        MapPathNode start = new MapPathNode(environment.GetClosetNode(transform.position));
        MapPathNode target = new MapPathNode(environment.GetClosetNode(targetLocation));

        start.score = 0;
        SortedList<Vector2, MapPathNode> closedSet = new SortedList<Vector2, MapPathNode>(new Vector2Comparer());
        SortedList<float, MapPathNode> openSet = new SortedList<float, MapPathNode>(new MapPathNodeComparer());
        openSet.Add(start.fScore, start);
        
        while(openSet.Count > 0)
        {
            MapPathNode current = openSet.Values[0];
            
            if(current == target)
            {
                lastPath = current;
                MapPathNode check = current;
                MapPathNode lastBefore = current;

                while(check.cameFrom != null)
                {
                    lastBefore = check;
                    check = lastBefore.cameFrom;
                }

                Vector2 result = lastBefore.node.Value.location;
                
                if(result.x <= environment.min.x || result.x >= environment.max.x || result.y <= environment.min.y || result.y >= environment.max.y)
                {
                    result = (Vector2)transform.position + (lastBefore.direction * speed);
                }

                return result;
            }

            openSet.RemoveAt(0);
            closedSet.Add(current.node.Value.location, current);

            for(int i = 0; i < 4; ++i) {
                Node? child = current.node.Value.connections[i];

                if (child == null || closedSet.ContainsKey(child.Value.location))
                {
                    continue;
                }

                MapPathNode addChild = new MapPathNode(child.Value);
                float tenitiveScore = 1.0f + current.score;

                int index = openSet.IndexOfValue(addChild);
                if (index >= 0)
                {
                    if (tenitiveScore >= openSet.Values[index].score)
                    {
                        continue;
                    }
                    else
                    {
                        addChild = openSet.Values[index];
                        openSet.RemoveAt(index);
                    }
                }

                addChild.score = tenitiveScore;
                addChild.cameFrom = current;
                addChild.fScore = pattern.CalculateFScore(targetLocation, addChild.node.Value.location, addChild.score);
                addChild.direction = movementDirections[i];
                openSet.Add(addChild.fScore, addChild);
            }
        }

        return start.node.Value.location;
    }
}

public class Vector2Comparer : IComparer<Vector2>
{
    public int Compare(Vector2 a, Vector2 b)
    {
        int result = a.y.CompareTo(b.y);

        if(result == 0)
        {
            return a.x.CompareTo(b.x);
        }

        return result;
    }
}

public class MapPathNodeComparer : IComparer<float>
{
    public int Compare(float x, float y)
    {
        int result = x.CompareTo(y);

        if (result == 0)
        {
            // This is done to allow duplicate entries in the sorted list.
            return 1;
        }

        return result;
    }
}

public class MapPathNode
{
    public Node? node;
    public Vector2 direction; // This may be different in order to account for the torus world of pacman

    /// <summary>
    /// A value used for pathfinding
    /// </summary>
    public float score;
    public float fScore;
    
    public MapPathNode cameFrom;

    public MapPathNode(Node node)
    {
        this.node = node;
        score = 0.0f;
        fScore = 0.0f;
        cameFrom = null;
    }

    public static bool operator==(MapPathNode a, MapPathNode b)
    {
        if ((object)a == null || (object)b == null)
        {
            return (object)a == null && (object)b == null;
        }

        return a.node == b.node;
    }

    public static bool operator !=(MapPathNode a, MapPathNode b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj != null && typeof(MapPathNode).IsAssignableFrom(obj.GetType()))
        {
            return this == (MapPathNode)obj;
        }

        return false;
    }
}

public interface GhostMovementPattern
{
    Vector2 CalculateTarget(Transform position, Environment environment, GameObject target);
    float CalculateFScore(Vector2 target, Vector2 current, float score);
}

public class ChaseMovementPattern : GhostMovementPattern
{
    public float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return Vector2.Distance(target, current) * score;
    }

    public Vector2 CalculateTarget(Transform position, Environment environment, GameObject target)
    {
        return (Vector2)target.transform.position;
    }
}

public class RandomMovementPattern : GhostMovementPattern
{
    private Vector2 target = GenerateTarget(new Vector2(-10.0f, -10.0f), new Vector2(10.0f, 10.0f));
    private float targetCooldown = 0.0f;

    private static Vector2 GenerateTarget(Vector2 min, Vector2 max)
    {
        return new Vector2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
    }

    public float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return Vector2.Distance(target, current) * score;
    }

    public Vector2 CalculateTarget(Transform position, Environment environment, GameObject player)
    {
        targetCooldown -= Time.deltaTime;

        if(targetCooldown <= 0.0f)
        {
            target = GenerateTarget(environment.min, environment.max);
            targetCooldown = UnityEngine.Random.Range(3.0f, 5.0f);
        }

        return target;
    }
}

public class EvadeMovementPattern : GhostMovementPattern
{
    protected Vector2 lastPlayerPosition = new Vector2(0, 0);

    public float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return (-1.0f * Vector2.Distance(lastPlayerPosition, current)) * score;
    }

    public Vector2 CalculateTarget(Transform position, Environment environment, GameObject target)
    {
        lastPlayerPosition = target.transform.position;

        Vector2 movement = position.position - target.transform.position;

        return (Vector2)position.position + (movement.normalized * 5.0f);
    }
}

public class FlankMovementPattern : GhostMovementPattern
{
    protected Vector2 lastPlayerPosition = new Vector2(0, 0);

    public float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return (-1.0f * Vector2.Distance(lastPlayerPosition, current)) * score;
    }

    public Vector2 CalculateTarget(Transform position, Environment environment, GameObject target)
    {
        lastPlayerPosition = target.transform.position;

        return (Vector2)target.transform.position;
    }
}
