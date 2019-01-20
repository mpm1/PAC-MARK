using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnvironmentGenerator : EnvironmentBase
{
    private enum eDirection { East = 0x01, North = 0x02, South = 0x04, West = 0x08, All = 0x0F};
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

    private List<Node> nodes;

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
        Rect rect = GetContainingRect();
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));

        Vector3Int ghostMin, ghostMax;

        //Due to how the collider is not reseting, we will disable it to begin with and enable it after.
        Collider2D collider = tileDetails.tileMap.GetComponent<TilemapCollider2D>();
        collider.enabled = false;

        Fill(min, max);
        eDirection ghostDoor = GenerateGhostBox(out ghostMin, out ghostMax);

        Vector3Int startLocation = new Vector3Int((ghostMin.x + ghostMax.x >> 1), ghostMin.y - 1, min.z);

        GenerateMaze(startLocation, min, max, ghostMin, ghostMax);
        Finish();

        collider.enabled = true;

        nodes = new List<Node>(CalculateNodes(tileDetails.tileMap));
    }

    private void GeneratePellets(Vector3Int playerStart, Vector3Int min, Vector3Int max, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        Vector3Int location = min;

        for(; location.y <= max.y; ++location.y)
        {
            for(location.x = min.x; location.x <= max.x; ++location.x)
            {
                if (tileDetails.tileMap.GetTile(location) == null && location != playerStart 
                    && !(location.x > ghostMin.x && location.x < ghostMax.x && location.y > ghostMin.y && location.y < ghostMax.y))
                {
                    PlacePellet(location, max);
                }
            }
        }
        
    }

    private void PlacePellet(Vector3Int location, Vector3Int reflect)
    {
        tileDetails.AddObject(smallPellet, tileDetails.GetWorldCoordinates(location));

        // Mirroring
        if (isVerticalMirror)
        {
            Vector3Int newLocation = location;
            newLocation.y = reflect.y - location.y;
            tileDetails.AddObject(smallPellet, tileDetails.GetWorldCoordinates(newLocation));
        }

        if (isHorizontalMirror)
        {
            Vector3Int newLocation = location;
            newLocation.x = reflect.x - location.x;
            tileDetails.AddObject(smallPellet, tileDetails.GetWorldCoordinates(newLocation));
        }

        if (mirroring == eMirror.Both)
        {
            Vector3Int newLocation = location;
            newLocation.y = reflect.y - location.y;
            newLocation.x = reflect.x - location.x;
            tileDetails.AddObject(smallPellet, tileDetails.GetWorldCoordinates(newLocation));
        }
    }

    private void Fill(Vector3Int mapMin, Vector3Int mapMax)
    {
		tileDetails.Fill(mapMin, mapMax, tileDetails.single);
    }

    private void Finish()
    {
        // Remove the ghost door
        Rect rect = new Rect(transform.position - (ghostBoxScale / 2.0f), ghostBoxScale);
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int location = new Vector3Int((min.x + max.x) >> 1, max.y, min.z);

        tileDetails.RemoveWall(location);
        tileDetails.RemoveWall(location + Vector3Int.left);
        tileDetails.RemoveWall(location + Vector3Int.right);

        // Set the tiles to the proper shape
        rect = new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
        min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        location = min;
        
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

    private void GenerateMaze(Vector3Int startLocation, Vector3Int mapMin, Vector3Int mapMax, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        List<Vector3Int> walls = new List<Vector3Int>();

        //TODO min and max for mirroring.
        Vector3Int min = mapMin;
        Vector3Int max = mapMax;
        if (isVerticalMirror)
        {
            max.y = (min.y + max.y) >> 1;
        }

        if (isHorizontalMirror)
        {
            max.x = (min.x + max.x) >> 1;
        }

        // Start removing walls
        RemoveWall(startLocation, min, max, mapMin, mapMax, walls);

        while (walls.Count > 0)
        {
            int i = Random.Range(0, walls.Count);
            RemoveWall(walls[i], min, max, mapMin, mapMax, walls);
            walls.RemoveAt(i);
        }

        GeneratePellets(startLocation, min, max, ghostMin, ghostMax);
    }

    private void RemoveWall(Vector3Int location, Vector3Int min, Vector3Int max, Vector3Int mapMin, Vector3Int mapMax, List<Vector3Int> walls)
    {
        Vector3Int[] sideWalls =
        {
            location + Vector3Int.up,
            location + Vector3Int.right,
            location + Vector3Int.down,
            location + Vector3Int.left
        };

        // Check if we can remove the wall
        int holeCount = 0;

        Vector3Int dist = mapMax - mapMin;
        dist.x = Mathf.Abs(dist.x);
        dist.y = Mathf.Abs(dist.y);

        for (int i = 0; i < 4; ++i)
        {
            Vector3Int checkVal = sideWalls[i];
            //while (checkVal.y < mapMin.y) { checkVal.y += dist.y; }
            //while (checkVal.y > mapMax.y) { checkVal.y -= dist.y; }
            //while (checkVal.x < mapMin.x) { checkVal.x += dist.x; }
            //while (checkVal.x > mapMax.x) { checkVal.x -= dist.x; }
            if (checkVal.y < mapMin.y) { continue; }
            if (checkVal.y > mapMax.y) { continue; }
            if (checkVal.x < mapMin.x) { continue; }
            if (checkVal.x > mapMax.x) { continue; }
            TileBase tile = tileDetails.tileMap.GetTile(checkVal);

            if(tile == null)
            {
                ++holeCount;
            }
        }

        if(holeCount > 1)
        {
            return;
        }

        // Remove the wall
        Vector3Int reflect = mapMin + mapMax;
        tileDetails.tileMap.SetTile(location, null);

        // Mirroring
        if (isVerticalMirror)
        {
            Vector3Int newLocation = location;
            newLocation.y = reflect.y - location.y;

            tileDetails.tileMap.SetTile(newLocation, null);
        }

        if (isHorizontalMirror)
        {
            Vector3Int newLocation = location;
            newLocation.x = reflect.x - location.x;

            tileDetails.tileMap.SetTile(newLocation, null);
        }

        if (mirroring == eMirror.Both)
        {
            Vector3Int reflectDir = reflect * (Vector3Int.right + Vector3Int.up);
            tileDetails.tileMap.SetTile(reflectDir - location, null);
        }

        // Add surrounding walls to the list
        foreach (Vector3Int wall in sideWalls)
        {
            if (tileDetails.tileMap.GetTile(wall) != null)
            {
                if (wall.x >= min.x || wall.x <= max.x || wall.y >= min.y || wall.y <= max.y)
                {
                    walls.Add(wall);
                }

            }
        }
    }

    //  private void RemoveWall(Vector3Int location, Vector3Int min, Vector3Int max, Vector3Int mapMin, Vector3Int mapMax, List<Vector3Int> walls)
    //  {
    //      Vector3Int[] checkWalls =
    //      {
    //          location + Vector3Int.up + Vector3Int.up,
    //          location + Vector3Int.right + Vector3Int.right,
    //          location + Vector3Int.down + Vector3Int.down,
    //          location + Vector3Int.left + Vector3Int.left
    //      };

    //      Vector3Int[] sideWalls =
    //      {
    //          location + Vector3Int.up,
    //          location + Vector3Int.right,
    //          location + Vector3Int.down,
    //          location + Vector3Int.left
    //      };

    //      // Check if we can remove the wall
    //      int holeCount = 0;
    //      int checkMax = 1;

    //      // TODO evaluate halls on the edge properly.
    //      if (location.x == mapMin.x || location.x == mapMax.x)
    //      {
    //          if (location.y != mapMin.y || location.y != mapMax.y)
    //          {
    //              checkMax = 2;
    //          }
    //}else if(location.y == mapMin.y || location.y == mapMax.y)
    //      {
    //          checkMax = 2;
    //      }

    //      for(int i = 0; i < 4; ++i)
    //      {
    //          if(tileDetails.tileMap.GetTile(sideWalls[i]) != null)
    //          {
    //              if (checkWalls[i].x >= mapMin.x && checkWalls[i].y >= mapMin.y && checkWalls[i].x <= mapMax.x && checkWalls[i].y <= mapMax.y)
    //              {
    //                  if (tileDetails.tileMap.GetTile(checkWalls[i]) == null)
    //                  {
    //                      ++holeCount;
    //                  }
    //              }
    //          }
    //          else
    //          {
    //              ++holeCount;
    //          }

    //          if(holeCount > checkMax)
    //          {
    //              return;
    //          }
    //      }

    //      // Remove the wall
    //      Vector3Int reflect = mapMin + mapMax;
    //      tileDetails.tileMap.SetTile(location, null);

    //      // Mirroring
    //      if (isVerticalMirror)
    //      {
    //          Vector3Int newLocation = location;
    //          newLocation.y = reflect.y - location.y;

    //          tileDetails.tileMap.SetTile(newLocation, null);
    //      }

    //      if (isHorizontalMirror)
    //      {
    //          Vector3Int newLocation = location;
    //          newLocation.x = reflect.x - location.x;

    //          tileDetails.tileMap.SetTile(newLocation, null);
    //      }

    //      if (mirroring == eMirror.Both)
    //      {
    //          Vector3Int reflectDir = reflect * (Vector3Int.right + Vector3Int.up);
    //          tileDetails.tileMap.SetTile(reflectDir - location, null);
    //      }

    //      // Add surrounding walls to the list
    //      foreach(Vector3Int wall in sideWalls)
    //      {
    //          if(tileDetails.tileMap.GetTile(wall) != null)
    //          {
    //              if (wall.x >= min.x || wall.x <= max.x || wall.y >= min.y || wall.y <= max.y)
    //              {
    //                  walls.Add(wall);
    //              }

    //          }
    //      }
    //  }

    private void CreateWall(Vector3Int start, Vector3Int end, Vector3Int mapMin, Vector3Int mapMax, Vector3Int ghostMin, Vector3Int ghostMax)
    {
        Vector3Int reflect = mapMin + mapMax;

        //TODO check if we will intersect with the ghost box;

        tileDetails.DrawLine(start, end);

        // Mirroring
        if (isVerticalMirror)
        {
            Vector3Int newStart = start;
            newStart.y = reflect.y - start.y;

            Vector3Int newEnd = end;
            newEnd.y = reflect.y - end.y;

            tileDetails.DrawLine(newStart, newEnd);
        }

        if (isHorizontalMirror)
        {
            Vector3Int newStart = start;
            newStart.x = reflect.x - start.x;

            Vector3Int newEnd = end;
            newEnd.x = reflect.x - end.x;

            tileDetails.DrawLine(newStart, newEnd);
        }

        if(mirroring == eMirror.Both)
        {
            Vector3Int reflectDir = reflect * (Vector3Int.right + Vector3Int.up);
            tileDetails.DrawLine(reflectDir - start, reflectDir - end);
        }
    }

    private bool HasDoorDirection(int input, eDirection direction)
    {
        return (input & (int)direction) == (int)direction;
    }

    public override IEnumerable<Node> GetNodes()
    {
        return nodes.AsReadOnly();
    }

    public override Node GetClosetNode(Vector2 pos)
    {
        float distance = float.MaxValue;
        Node result = nodes[0];

        // TODO sort the nodes
        foreach(Node node in nodes)
        {
            float checkDistance = Vector2.Distance(pos, node.location);
            if(checkDistance < distance)
            {
                distance = checkDistance;
                result = node;

                if(distance < float.Epsilon)
                {
                    return result;
                }
            }
        }

        return result;
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

    public Vector3 GetWorldCoordinates(Vector3Int tileCoordinates)
    {
        return tileMap.CellToWorld(tileCoordinates);
    }

    public void AddObject(GameObject objectPrefab, Vector3 worldLocation)
    {
        GameObject obj = Object.Instantiate(objectPrefab, tileMap.transform.parent);
        obj.transform.position = worldLocation;
    }
}
