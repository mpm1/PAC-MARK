﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class Environment : MonoBehaviour
{
    protected Node[] CalculateNodes(Tilemap map)
    {
        Rect rect = GetContainingRect();
        Vector3Int min = map.WorldToCell(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = map.WorldToCell(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int dist = max - min;
        Node?[] result = new Node?[dist.x * dist.y];

        System.Func<Vector3Int, int> getIndex = (position) =>
        {
            while(position.x < min.x)
            {
                position.x += dist.x;
            }

            while(position.x > max.x)
            {
                position.x -= dist.x;
            }

            while (position.y < min.y)
            {
                position.y += dist.y;
            }

            while (position.y > max.y)
            {
                position.y -= dist.y;
            }

            Vector3Int calc = position - min;

            return calc.x + (calc.y * dist.x);
        };

        // Find all walkable tiles
        Vector3Int location = min;
        int index = 0;
        for(; location.y <= max.y; ++location.y)
        {
            for(location.x = min.x; location.x <= max.x; ++location.x)
            {
                if(map.GetTile(location) != null)
                {
                    Node node = new Node(map.CellToWorld(location));

                    result[index] = node;
                }
                else
                {
                    result[index] = null;
                }

                ++index;
            }
        }

        // Connect all walkable tiles
        location = min;
        index = 0;
        Vector3Int[] checkPattern = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
        for (; location.y <= max.y; ++location.y)
        {
            for (location.x = min.x; location.x <= max.x; ++location.x)
            {
                if(result[index] != null)
                {
                    for(int i = 0; i < 4; ++i)
                    {
                        result[index].Value.SetConnection(i, result[getIndex(location + checkPattern[i])]);
                    }
                }

                ++index;
            }
        }

        return result.Where(n => n != null).Select(n => n.Value).ToArray();
    }

    protected Rect GetContainingRect()
    {
        return new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void HandleObjectOnEdge(Transform body)
    {
        Rect container = GetContainingRect();
        Vector2 location = body.position;

        // For now we will snap the movement if the center is past the edge.
        // In the future we will need to wrap this movement.
        if (!container.Contains(location))
        {
            while (location.y < container.yMin)
            {
                location.y += container.height;
            }

            while (location.y > container.yMax)
            {
                location.y -= container.height;
            }

            while (location.x < container.xMin)
            {
                location.x += container.width;
            }

            while (location.x > container.xMax)
            {
                location.x -= container.width;
            }

            body.position = location;
        }
    }

    public abstract IEnumerable<Node> GetNodes();
    public abstract Node GetClosetNode(Vector2 pos);
    
    public struct Node
    {
        /// <summary>
        /// The location in world space for this node.
        /// </summary>
        public Vector2 node;
        
        /// <summary>
        /// Unobstructed paths to other nodes.
        /// </summary>
        public Node?[] connections;

        /// <summary>
        /// A value used for pathfinding
        /// </summary>
        public float score;
        public float fScore;

        public Node? cameFrom;

        private int mConnectionCount;
        public int connectionCount
        {
            get
            {
               return mConnectionCount;
            }
        }

        public Node(Vector2 node)
        {
            this.node = node;
            mConnectionCount = 0;
            score = 0.0f;
            fScore = 0.0f;
            connections = new Node?[4];
            cameFrom = null;
        }

        public static bool operator==(Node a, Node b)
        {
            return a.node == b.node;
        }

        public static bool operator !=(Node a, Node b)
        {
            return a.node != b.node;
        }

        public void SetConnection(int index, Node? node)
        {
            if(index >= 0 && index < 4)
            {
                connections[index] = node;
            }

            mConnectionCount = 0;
            foreach(Node? connection in connections)
            {
                if(connection != null)
                {
                    ++mConnectionCount;
                }
            }
        }
    }
}
