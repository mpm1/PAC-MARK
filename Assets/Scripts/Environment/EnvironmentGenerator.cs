using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnvironmentGenerator : MonoBehaviour
{
    private enum eDirection { East = 0, North, South, West };

    public GameObject smallPellet;
    public GameObject largePellet;
    public int maxLargePellets = 6;
    public int minLargePellets = 2;

    public Vector2 ghostBoxScale;
    public float ghostBoxDoorSize;

    public Tilemap tileMap;

    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        int pelletCount = Mathf.RoundToInt(Random.Range(minLargePellets, maxLargePellets));

        eDirection ghostDoor = GenerateGhostBox();
    }

    private eDirection GenerateGhostBox()
    {
        eDirection doorDirection = (eDirection)Mathf.RoundToInt(Random.Range(0, 3));

        return doorDirection;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
