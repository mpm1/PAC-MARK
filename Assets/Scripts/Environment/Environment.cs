using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Environment : MonoBehaviour
{

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

    public abstract IList<Node> GetNodes();
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
        public IList<Node> connections;

        public Node(Vector2 node)
        {
            this.node = node;
            connections = new List<Node>();
        }
    }
}
