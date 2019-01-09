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
        Vector2 direction = new Vector2(waypoint.x, waypoint.y).normalized;
        rigidbody.MovePosition(position + (speed * Time.fixedDeltaTime * direction));

        // Set the eye movement
        Vector3 eyeVector = (player.transform.position - transform.position).normalized;
        eyes.localPosition = eyePosition + (eyeVector * eyeRadius);
    }

    protected float CalculateFScore(Vector2 target, Vector2 current, float score)
    {
        return score;
    }
    
    protected Vector2 FindNextPathLocation(Vector2 targetLocation)
    {
        //TODO: optimize to reduce garbage colleciton
        MapPathNode start = new MapPathNode(environment.GetClosetNode(transform.position));
        MapPathNode target = new MapPathNode(environment.GetClosetNode(targetLocation));

        start.score = 0;
        List<MapPathNode> closedSet = new List<MapPathNode>();
        SortedList<float, MapPathNode> openSet = new SortedList<float, MapPathNode>();
        openSet.Add(start.score, start);
        
        while(openSet.Count > 0)
        {
            MapPathNode current = openSet.Values[0];
            
            if(current == target)
            {
                MapPathNode? check = current;
                MapPathNode? lastBefore = current;

                while(check.Value.cameFrom != null)
                {
                    lastBefore = check;
                    check = lastBefore.Value.cameFrom;
                }

                return lastBefore.Value.node.Value.location;
            }

            openSet.RemoveAt(0);
            closedSet.Add(current);

            foreach(Node? child in current.node.Value.connections)
            {
                MapPathNode addChild;

                if (child == null || closedSet.Contains(addChild = new MapPathNode(child.Value)))
                {
                    continue;
                }
                
                float tenitiveScore = 1.0f + current.score;

                int index = openSet.IndexOfValue(addChild);
                if(index >= 0 && tenitiveScore >= openSet.Values[index].score)
                {
                    continue;
                }else
                {
                    openSet.RemoveAt(index);
                }

                addChild.score = tenitiveScore;
                addChild.cameFrom = current;
                addChild.fScore = CalculateFScore(targetLocation, addChild.node.Value.location, addChild.score);
                openSet.Add(addChild.score, addChild);
            }
        }

        return start.node.Value.location;
    }

    protected abstract Vector2 CalculateNextWaypoint();
}

public struct MapPathNode
{
    public Node? node;

    /// <summary>
    /// A value used for pathfinding
    /// </summary>
    public float score;
    public float fScore;

    public MapPathNode? cameFrom;

    public MapPathNode(Node node)
    {
        this.node = node;
        score = 0.0f;
        fScore = 0.0f;
        cameFrom = null;
    }

    public static bool operator==(MapPathNode a, MapPathNode b)
    {
        return a.node == b.node;
    }

    public static bool operator !=(MapPathNode a, MapPathNode b)
    {
        return a.node != b.node;
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
