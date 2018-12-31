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

        tileDetails.Fill(min, max, tileDetails.single);
        eDirection ghostDoor = GenerateGhostBox(out ghostMin, out ghostMax);

        // Check which kind of symetry we need and set the min and max accordingly.
        if(isVerticalMirror)
        {
            max.y = (max.y + min.y) >> 1;
        }

        if (isHorizontalMirror)
        {
            max.x = (max.x + min.x) >> 1;
        }

        GenerateMaze(min, max, ghostMax, ghostMin);
        Finish();

        collider.enabled = true;
    }

    private void Finish()
    {
        Rect rect = new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int location = min;

        for (; location.y < max.y; ++location.y)
        {
            for(location.x = min.x; location.x < max.x; ++location.x)
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

    private void GenerateMaze(Vector3Int min, Vector3Int max, Vector3Int ghostMax, Vector3Int ghostMin)
    {
        List<Vector3Int> walls = new List<Vector3Int>();
        Vector3Int delta = max - min;

        // Pick the start location along the wall
        Vector3Int cellIncr;
        if (isVerticalMirror)
        {
            cellIncr = new Vector3Int(Random.Range(1, delta.x), 0, 0);
            PlaceHall(min + cellIncr, walls, min, max);
        }

        if (isHorizontalMirror)
        {
            cellIncr = new Vector3Int(0, Random.Range(1, delta.y), 0);
            PlaceHall(min + cellIncr, walls, min, max);
        }

        while (walls.Count > 0)
        {
            int i = Random.Range(0, walls.Count);
            if (CanRemoveWall(walls[i], min, max))
            {
                PlaceHall(walls[i], walls, min, max);
            }

            walls.RemoveAt(i);
        }
    }

    private bool CanRemoveWall(Vector3Int location, Vector3Int min, Vector3Int max)
    {
        TileBase tile = tileDetails.tileMap.GetTile(location);

        if(tile == null)
        {
            return false;
        }

        /*if(location.x == min.x || location.y == min.y)
        {
            return false;
        }

        if (location.x == max.x && !isHorizontalMirror)
        {
            return false;
        }

        if(location.y == max.y && !isVerticalMirror)
        {
            return false;
        }*/

        // Check Vertical
        int hallCount = 0;
        if(tileDetails.tileMap.GetTile(location + Vector3Int.up) == null)
        {
            ++hallCount;
        }

        if(tileDetails.tileMap.GetTile(location + Vector3Int.down) == null)
        {
            ++hallCount;

            if(hallCount > 2) {
                return false;
            }
        }

        if(tileDetails.tileMap.GetTile(location + Vector3Int.right) == null)
        {
            ++hallCount;

            if (hallCount > 2)
            {
                return false;
            }
        }

        if(tileDetails.tileMap.GetTile(location + Vector3Int.left) == null)
        {
            ++hallCount;

            if (hallCount > 2)
            {
                return false;
            }
        }

        if (tileDetails.tileMap.GetTile(location + Vector3Int.up + Vector3Int.left) == null)
        {
            ++hallCount;

            if (hallCount > 2)
            {
                return false;
            }
        }

        if (tileDetails.tileMap.GetTile(location + Vector3Int.down + Vector3Int.right) == null)
        {
            ++hallCount;

            if (hallCount > 2)
            {
                return false;
            }
        }

        if (tileDetails.tileMap.GetTile(location + Vector3Int.right + Vector3Int.up) == null)
        {
            ++hallCount;

            if (hallCount > 2)
            {
                return false;
            }
        }

        if (tileDetails.tileMap.GetTile(location + Vector3Int.left + Vector3Int.down) == null)
        {
            ++hallCount;

            if (hallCount > 2)
            {
                return false;
            }
        }

        return true;
    }

    private bool PlaceHall(Vector3Int location, List<Vector3Int> walls, Vector3Int min, Vector3Int max)
    {
        RemoveWall(location, walls, min, max);

        // Add corresponding walls if we are on the edge
        if (location.x == min.x)
        {
            Vector3Int newLocation = location;
            newLocation.x = max.x;
            RemoveWall(newLocation, walls, min, max);
        }
        else if(location.y == min.y)
        {
            Vector3Int newLocation = location;
            newLocation.y = max.y;
            RemoveWall(newLocation, walls, min, max);
        }

        return true;
    }

    private void RemoveWall(Vector3Int location, List<Vector3Int> walls, Vector3Int min, Vector3Int max)
    {
        tileDetails.RemoveWall(location);

        if(location.x > min.x)
        {
            Vector3Int checkWall = location + Vector3Int.left;

            if (tileDetails.tileMap.GetTile(checkWall) != null)
            {
                walls.Add(checkWall);
            }
        }

        if (location.y > min.y)
        {
            Vector3Int checkWall = location + Vector3Int.down;

            if (tileDetails.tileMap.GetTile(checkWall) != null)
            {
                walls.Add(checkWall);
            }
        }

        if (location.x < max.x)
        {
            Vector3Int checkWall = location + Vector3Int.right;

            if (tileDetails.tileMap.GetTile(checkWall) != null)
            {
                walls.Add(checkWall);
            }
        }

        if (location.y < max.y)
        {
            Vector3Int checkWall = location + Vector3Int.up;

            if (tileDetails.tileMap.GetTile(checkWall) != null)
            {
                walls.Add(checkWall);
            }
        }

        // Mirror results
        if (isVerticalMirror)
        {
            Vector3Int newLocation = location;
            newLocation.y = (max.y << 1) - location.y;
            tileDetails.RemoveWall(newLocation); 
        }

        if (isHorizontalMirror)
        {
            Vector3Int newLocation = location;
            newLocation.x = (max.x << 1) - location.x;
            tileDetails.RemoveWall(newLocation);
        }
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
