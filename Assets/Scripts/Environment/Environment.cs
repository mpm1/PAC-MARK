using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static EnvironmentBase;

public class Environment : EnvironmentBase
{
    public Tilemap tileMap;

    private List<Node> nodes;

    // Start is called before the first frame update
    public void Awake()
    {
        nodes = new List<Node>(CalculateNodes(tileMap));
    }

    public override Node GetClosetNode(Vector2 pos)
    {
        float distance = float.MaxValue;
        Node result = nodes[0];

        // TODO sort the nodes
        foreach (Node node in nodes)
        {
            float checkDistance = Vector2.Distance(pos, node.location);
            if (checkDistance < distance)
            {
                distance = checkDistance;
                result = node;

                if (distance < float.Epsilon)
                {
                    return result;
                }
            }
        }

        return result;
    }

    public override IEnumerable<Node> GetNodes()
    {
        return nodes.AsReadOnly();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
