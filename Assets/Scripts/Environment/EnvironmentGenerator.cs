using UnityEngine;
using UnityEngine.Tilemaps;

public class EnvironmentGenerator : MonoBehaviour
{
    private enum eDirection { East = 0, North, South, West };

    public GameObject smallPellet;
    public GameObject largePellet;
    public int maxLargePellets = 6;
    public int minLargePellets = 2;
    public int seed = 0;
    public bool isVerticalSymetrical = false;
    public bool isHorizontalSymetrical = true;

    [Range(1, 20)]
    public int minStartTiles = 4;
    [Range(1, 20)]
    public int maxStartTiles = 6;

    public Vector3 ghostBoxScale;
    public float ghostBoxDoorSize;

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

        tileDetails.Clear();
        SetStartTiles();
        eDirection ghostDoor = GenerateGhostBox();

        // Rpgressivly grow each starting tile location until we have single tile sized halways.
        for(int i = 0; i < 100; ++i)
        {
            if (!GrowMap())
            {
                break;
            }
        }
    }

    private void SetStartTiles()
    {
        int tileCount = Random.Range(minStartTiles, maxStartTiles);
        Rect rect = new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int size = max - min;
        Vector3Int halfSize = new Vector3Int(size.x >> 1, size.y >> 1, size.z >> 1);
        Vector3Int axis = min + halfSize;

        for (int i = 0; i < tileCount; ++i)
        {
            Vector3Int location = new Vector3Int(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0);

            tileDetails.PlaceWall(location);

            if (isVerticalSymetrical || isHorizontalSymetrical)
            {
                Vector3Int newLocation = location;

                if (isVerticalSymetrical)
                {
                    newLocation.y = (axis.y << 1) - newLocation.y;
                }

                if (isHorizontalSymetrical)
                {
                    newLocation.x = (axis.x << 1) - newLocation.x;
                }

                tileDetails.PlaceWall(newLocation);
                ++i;
            }
        }
    }

    private eDirection GenerateGhostBox()
    {
        eDirection doorDirection = (eDirection)Random.Range(0, 3);

        Rect rect = new Rect(transform.position - (ghostBoxScale / 2.0f), ghostBoxScale);
        Vector3Int tl = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int tr = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMin, 0.0f));
        Vector3Int bl = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMax, 0.0f));
        Vector3Int br = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));

        tileDetails.Clear(tl, br);
        tileDetails.DrawLine(tl, tr);
        tileDetails.DrawLine(tl, bl);
        tileDetails.DrawLine(bl, br);
        tileDetails.DrawLine(br, tr);

        return doorDirection;
    }

    private bool GrowMap()
    {
        bool hasGrown = false;
        Rect rect = new Rect(transform.position - (transform.lossyScale / 2.0f), transform.lossyScale);
        Vector3Int min = tileDetails.GetTileCoordinates(new Vector3(rect.xMin, rect.yMin, 0.0f));
        Vector3Int max = tileDetails.GetTileCoordinates(new Vector3(rect.xMax, rect.yMax, 0.0f));
        Vector3Int location = min;

        for(location.y = min.y; location.y <= max.y; ++location.y)
        {
            for(location.x = min.x; location.x <= max.x; ++location.x)
            {
                hasGrown |= tileDetails.GrowTile(location, min, max);
            }
        }

        return hasGrown;
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
    private Tilemap tileMapBuffer;

    public void Init()
    {
        tileMapBuffer = tileMap;
        positions = new Tile[] { single, north, south, vertical, east, northEast, southEast, verticalEast, west, northWest, southWest, verticalWest, horizontal, horizontalNorth, horizontalSouth, all };
    }

    public void Clear(Vector3Int? tl = null, Vector3Int? br = null)
    {
        if (tl == null || br == null)
        {
            tileMap.ClearAllTiles();
        }
        else
        {
            Vector3Int delta = br.Value - tl.Value;
            tileMap.BoxFill(tl.Value, null, 0, 0, delta.x, delta.y);
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

    public bool GrowTile(Vector3Int location, Vector3Int minBounds, Vector3Int maxBounds)
    {
        TileBase tile = tileMap.GetTile(location);
        bool canGrow = false;

        Vector3Int[] checkLocations =
        {
            location + new Vector3Int(-2, -2, 0),
            location + new Vector3Int(0, -2, 0),
            location + new Vector3Int(2, -2, 0),
            location + new Vector3Int(-2, 0, 0),
            location + new Vector3Int(2, 0, 0),
            location + new Vector3Int(-2, 2, 0),
            location + new Vector3Int(0, 2, 0),
            location + new Vector3Int(2, 2, 0)
        };

        Vector3Int[] newLocations =
        {
            location + new Vector3Int(-1, -1, 0),
            location + new Vector3Int(0, -1, 0),
            location + new Vector3Int(1, -1, 0),
            location + new Vector3Int(-1, 0, 0),
            location + new Vector3Int(1, 0, 0),
            location + new Vector3Int(-1, 1, 0),
            location + new Vector3Int(0, 1, 0),
            location + new Vector3Int(1, 1, 0)
        };

        if (tile == single)
        {
            for(int i = 0; i < 8; ++i)
            {
                if(CanGrow(newLocations[i], minBounds, maxBounds) && tileMap.GetTile(checkLocations[i]) == null)
                {
                    canGrow = true;
                    tileMapBuffer.SetTile(newLocations[i], single);
                }
            }

            //tileMap.SetTile(location, all);
        }

        return canGrow;
    }

    private bool CanGrow(Vector3Int location, Vector3Int minBounds, Vector3Int maxBounds)
    {
        if(location.x < minBounds.x || location.y < minBounds.y || location.x > maxBounds.x || location.y > maxBounds.y)
        {
            return false;
        }

        return tileMap.GetTile(location) == null;
    }
}
