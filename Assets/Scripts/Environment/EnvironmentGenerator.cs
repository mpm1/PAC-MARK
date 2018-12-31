using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnvironmentGenerator : MonoBehaviour
{
    private enum eDirection { East = 0, North, South, West };
    public enum eMirror {
        Vertical = 0x01,
        Horizontal = 0x02,
        Both = 0x03
    }

    public GameObject smallPellet;
    public GameObject largePellet;
    public int maxLargePellets = 6;
    public int minLargePellets = 2;
    public int seed = 0;
    public eMirror mirroring = eMirror.Vertical;

    public Vector3 ghostBoxScale;
    public float ghostBoxDoorSize;

    private bool isVerticalMirror
    {
        get
        {
            return ((int)mirroring & (int)eMirror.Vertical) == (int)eMirror.Vertical;
        }
    }

    private bool isHorizontalMirror
    {
        get
        {
            return ((int)mirroring & (int)eMirror.Horizontal) == (int)eMirror.Horizontal;
        }
    }

    [Header("Tile Details")]
    public TileDrawer tileDetails;

    private void Awake()
    {
        tileDetails.Init();
        Generate(seed);
    }

    private void Generate(int seed)
    {
        Random.InitState(seed);

        int pelletCount = Random.Range(minLargePellets, maxLargePellets);

        // Find the size of the map
        Rect rect = new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));

        Vector3Int ghostMin, ghostMax;

        //Due to how the collider is not reseting, we will disable it to begin with and enable it after.
        Collider2D collider = tileDetails.tileMap.GetComponent<TilemapCollider2D>();
        collider.enabled = false;
        
        eDirection ghostDoor = GenerateGhostBox(out ghostMin, out ghostMax);

        GenerateMaze(min, max, ghostMin, ghostMax);
        Finish();

        collider.enabled = true;
    }

    private void Finish()
    {
        Rect rect = new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int location = min;

        for (; location.y <= max.y; ++location.y)
        {
            for(location.x = min.x; location.x <= max.x; ++location.x)
            {
                tileDetails.UpdateTile(location, false);
            }
        }
    }

    private eDirection GenerateGhostBox(out Vector3Int min, out Vector3Int max)
    {
        eDirection doorDirection = (eDirection)Random.Range(0, 4);

        Rect rect = new Rect(transform.position - (ghostBoxScale / 2.0f), ghostBoxScale);
        min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int incr = new Vector3Int(1, 1, 0);

        tileDetails.Fill(min + incr, max - incr, null);

        return doorDirection;
    }

    private void GenerateMaze(Vector3Int mapMin, Vector3Int mapMax, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        Vector3Int min = mapMin;
        Vector3Int max = mapMax;

        // Check which kind of symetry we need and set the min and max accordingly.
        if (isVerticalMirror)
        {
            max.y = (max.y + min.y) >> 1;
        }

        if (isHorizontalMirror)
        {
            max.x = (max.x + min.x) >> 1;
        }

        Vector3Int[] border =
        {
            min,
            new Vector3Int(max.x, min.y, min.z),
            max,
            new Vector3Int(min.x, max.y, max.z)
        };

        for (int i = 0; i < 4; ++i)
        {
            int j = i == 3 ? 0 : i + 1;

            CreateWall(border[i], border[j], mapMin, mapMax, ghostMin, ghostMax);
        }
        
        GenerateChamber(min, max, mapMin, mapMax, ghostMin, ghostMax);
    }

    private Vector3Int GenerateSplitLocation(Vector3Int min, Vector3Int max, Vector3Int mapMin, Vector3Int mapMax, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        Vector3Int delta = max - min;

        // Only get even locations
        Vector3Int split = min + new Vector3Int(Random.Range(1, delta.x >> 1) << 1, Random.Range(1, delta.y >> 1) << 1, 0);

        CreateWall(new Vector3Int(min.x, split.y, min.z), new Vector3Int(max.x, split.y, max.x), mapMin, mapMax, ghostMin, ghostMax);
        CreateWall(new Vector3Int(split.x, min.y, min.z), new Vector3Int(split.x, max.y, max.x), mapMin, mapMax, ghostMin, ghostMax);

        return split;
    }

    private void RemoveWall(Vector3Int location, Vector3Int mapMin, Vector3Int mapMax)
    {
        tileDetails.tileMap.SetTile(location, null);

        //TODO: Mirroring
    }

    private void CreateWall(Vector3Int start, Vector3Int end, Vector3Int mapMin, Vector3Int mapMax, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        //TODO check if we will intersect with the ghost box;

        tileDetails.DrawLine(start, end);

        //TODO: Mirroring
    }

    private void GenerateChamber(Vector3Int min, Vector3Int max, Vector3Int mapMin, Vector3Int mapMax, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        if(min.x > max.x || min.y > max.y)
        {
            return;
        }

        Vector3Int delta = max - min;
        if (delta.x < 3 || delta.y < 3)
        {
            return;
        }

        Vector3Int split = GenerateSplitLocation(min, max, mapMin, mapMax, ghostMin, ghostMax);

        // Remove the neccisary walls.
        Vector3Int top = max;
        Vector3Int bottom = min;
        Vector3Int left = min;
        Vector3Int right = max;

        top.x = split.x + (Random.value > 0.5 ? 1 : -1);
        bottom.x = top.x;

        left.y = split.y + (Random.value > 0.5 ? 1 : -1);
        right.y = top.y;
        
        RemoveWall(top, mapMin, mapMax);
        RemoveWall(bottom, mapMin, mapMax);
        RemoveWall(left, mapMin, mapMax);
        RemoveWall(right, mapMin, mapMax);

        // Find the new intersection
        GenerateChamber(new Vector3Int(min.x, split.y, min.z), new Vector3Int(split.x, max.y, min.z), mapMin, mapMax, ghostMin, ghostMax);
        GenerateChamber(split, max, mapMin, mapMax, ghostMin, ghostMax);
        GenerateChamber(new Vector3Int(split.x, min.y, min.z), new Vector3Int(max.x, split.y, min.z), mapMin, mapMax, ghostMin, ghostMax);
        GenerateChamber(min, split, mapMin, mapMax, ghostMin, ghostMax);

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

    public void Clear(Vector3Int? min = null, Vector3Int? max = null)
    {
        if (min == null || max == null)
        {
            tileMap.ClearAllTiles();
        }
        else
        {
            Vector3Int delta = max.Value - min.Value;
            tileMap.BoxFill(min.Value, null, 0, 0, delta.x, delta.y);
        }
    }

    public void Fill(Vector3Int min, Vector3Int max, TileBase tile)
    {
        Vector3Int location = min;
        

        for(; location.y <= max.y; ++location.y)
        {
            for(location.x = min.x; location.x <= max.x; ++location.x)
            {
                tileMap.SetTile(location, tile);
            }
        }
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
            PlaceWall(location, true);
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
            PlaceWall(location, true);
            if(D > 0)
            {
                location.y += yi;
                D -= (2 * delta.x);
            }

            D += (2 * delta.y);
        }
    }

    public void RemoveWall(Vector3Int position, bool updateTile = false)
    {
        TileBase tile = tileMap.GetTile(position);

        if (tile != null)
        {
            tileMap.SetTile(position, null);

            if (updateTile)
            {
                UpdateTile(position, true);
            }
        }
    }

    public void PlaceWall(Vector3Int position, bool updateTile = false)
    {
        TileBase tile = tileMap.GetTile(position);

        if(tile == null)
        {
            tileMap.SetTile(position, single);
        }

        if (updateTile)
        {
            UpdateTile(position, true);
        }
    }

    public void UpdateTile(Vector3Int position, bool updateChildren)
    {
        uint result = 0;
        Vector3Int eastPos = position + Vector3Int.right;
        Vector3Int westPos = position + Vector3Int.left;
        Vector3Int northPos = position + Vector3Int.up;
        Vector3Int southPos = position + Vector3Int.down;

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

        if (tileMap.GetTile(position) != null)
        {
            tileMap.SetTile(position, positions[result]);
        }
    }

    public Vector3Int GetTileCoordinates(Vector3 worldCoordinates)
    {
        return tileMap.WorldToCell(worldCoordinates);
    }
}
