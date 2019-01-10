using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Environment;

public abstract class GhostBase : MonoBehaviour
{
    public float speed = 3f;
    public float eyeRadius = 0.15f;

    protected Environment environment;
    protected PlayerMovement player;

    private Vector2 waypoint;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private new SpriteRenderer renderer;
    private Transform eyes;
    private Vector3 eyePosition;

    private MapPathNode lastPath = null;

    private void OnDrawGizmos()
    {
        if (lastPath != null)
        {
            Gizmos.color = Color.red;
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

        eyes = transform.GetChild(0);
        eyePosition = eyes.localPosition;
    }

    private void Update()
    {
        waypoint = CalculateNextWaypoint();
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

    protected float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return Vector2.Distance(target, current) * score;
    }
    
    protected Vector2 FindNextPathLocation(Vector2 targetLocation)
    {
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

                return lastBefore.node.Value.location;
            }

            openSet.RemoveAt(0);
            closedSet.Add(current.node.Value.location, current);

            foreach(Node? child in current.node.Value.connections)
            {
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
                addChild.fScore = CalculateFScore(targetLocation, addChild.node.Value.location, addChild.score);
                openSet.Add(addChild.fScore, addChild);
            }
        }

        return start.node.Value.location;
    }

    protected abstract Vector2 CalculateNextWaypoint();
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
