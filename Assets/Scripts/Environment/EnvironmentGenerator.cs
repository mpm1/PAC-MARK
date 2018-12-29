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

    public Vector3 ghostBoxScale;
    public float ghostBoxDoorSize;

    [Header("Tile Details")]
    public TileDrawer tileDetails;

    private void Awake()
    {
        tileDetails.Init();
        Generate();
    }

    private void Generate()
    {
        tileDetails.Clear();

        int pelletCount = Mathf.RoundToInt(Random.Range(minLargePellets, maxLargePellets));
        eDirection ghostDoor = GenerateGhostBox();
    }

    private eDirection GenerateGhostBox()
    {
        eDirection doorDirection = (eDirection)Mathf.RoundToInt(Random.Range(0, 3));
        Rect rect = new Rect(transform.position - (ghostBoxScale / 2.0f), ghostBoxScale);
        Vector3Int tl = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int tr = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMin, 0.0f));
        Vector3Int bl = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMax, 0.0f));
        Vector3Int br = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));

        tileDetails.DrawLine(tl, tr);
        tileDetails.DrawLine(tl, bl);
        tileDetails.DrawLine(bl, br);
        tileDetails.DrawLine(br, tr);

        return doorDirection;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        Gizmos.DrawWireCube(transform.position, ghostBoxScale);
    }
}

[System.Serializable]
public class TileDrawer
{
    public Tilemap tileMap;
    public Tile vertical, horizontal;
    public Tile north, south, east, west;
    public Tile northEast, northWest;
    public Tile southEast, southWest;
    public Tile verticalEast, verticalWest;
    public Tile horizontalNorth, horizontalSouth;
    public Tile all;
    public Tile single;

    private Tile[] positions;

    public void Init()
    {
        positions = new Tile[] { single, north, south, vertical, east, northEast, southEast, verticalEast, west, northWest, southWest, verticalWest, horizontal, horizontalNorth, horizontalSouth, all };
    }

    public void Clear()
    {
        tileMap.ClearAllTiles();
    }

    public void DrawLine(Vector3Int start, Vector3Int end)
    {
        // Using Bresenham's line algorithm
        if(System.Math.Abs(end.y - start.y) < System.Math.Abs(end.x - start.x))
        {
            if(start.x > end.x)
            {
                PlotLineLow(end, start);
            }
            else
            {
                PlotLineLow(start, end);
            }
        }
        else
        {
            if(start.y > end.y)
            {
                PlotLineHigh(end, start);
            }
            else
            {
                PlotLineHigh(start, end);
            }
        }
    }

    private void PlotLineHigh(Vector3Int start, Vector3Int end)
    {
        Vector3Int delta = end - start;
        Vector3Int location = start;
        int xi = 1;

        if (delta.x < 0)
        {
            xi = -1;
            delta.x = -delta.x;
        }

        int D = (2 * delta.x) - delta.y;

        for (; location.y <= end.y; ++location.y)
        {
            PlaceWall(location);
            if (D > 0)
            {
                location.x += xi;
                D -= (2 * delta.y);
            }

            D += (2 * delta.x);
        }
    }

    private void PlotLineLow(Vector3Int start, Vector3Int end)
    {
        Vector3Int delta = end - start;
        Vector3Int location = start;
        int yi = 1;

        if (delta.y < 0)
        {
            yi = -1;
            delta.y = -delta.y;
        }

        int D = (2 * delta.y) - delta.x;

        for(; location.x <= end.x; ++location.x)
        {
            PlaceWall(location);
            if(D > 0)
            {
                location.y += yi;
                D -= (2 * delta.x);
            }

            D += (2 * delta.y);
        }
    }

    private void PlaceWall(Vector3Int position)
    {
        TileBase tile = tileMap.GetTile(position);

        if(tile == null)
        {
            tileMap.SetTile(position, single);
        }

        UpdateTile(position, true);
    }

    private void UpdateTile(Vector3Int position, bool updateChildren)
    {
        uint result = 0;
        Vector3Int eastPos = position + new Vector3Int(1, 0, 0);
        Vector3Int westPos = position + new Vector3Int(-1, 0, 0);
        Vector3Int northPos = position + new Vector3Int(0, 1, 0);
        Vector3Int southPos = position + new Vector3Int(0, -1, 0);

        if(tileMap.GetTile(northPos) != null)
        {
            if(updateChildren) UpdateTile(northPos, false);
            result = result | 0x000001;
        }

        if (tileMap.GetTile(southPos) != null)
        {
            if(updateChildren) UpdateTile(southPos, false);
            result = result | 0x000002;
        }

        if (tileMap.GetTile(eastPos) != null)
        {
            if(updateChildren) UpdateTile(eastPos, false);
            result = result | 0x000004;
        }

        if (tileMap.GetTile(westPos) != null)
        {
            if(updateChildren) UpdateTile(westPos, false);
            result = result | 0x000008;
        }

        tileMap.SetTile(position, positions[result]);
    }

    public Vector3Int GetTileCoordinates(Vector3 worldCoordinates)
    {
        return tileMap.WorldToCell(worldCoordinates);
    }
}
