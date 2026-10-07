using UnityEngine;

public enum RoomType
{
    // Single Openings (1 Door - Dead Ends)
    OPEN_TOP,
    OPEN_BOTTOM,
    OPEN_LEFT,
    OPEN_RIGHT,

    // Dual Straights (2 Doors)
    TOP_BOTTOM_OPEN,
    LEFT_RIGHT_OPEN,

    // Dual Corners (2 Doors - L-Turns)
    BOTTOM_RIGHT_OPEN,
    RIGHT_TOP_OPEN,
    TOP_LEFT_OPEN,
    LEFT_BOTTOM_OPEN,

    // T-Shapes (3 Doors - Precise Intersections)
    TOP_BOTTOM_RIGHT_OPEN,
    TOP_BOTTOM_LEFT_OPEN,
    TOP_LEFT_RIGHT_OPEN,
    BOTTOM_LEFT_RIGHT_OPEN,

    // All Openings (4 Doors - Center Intersections Only)
    ALL_OPEN
}

public enum TileType
{
    Empty = 0,
    Wall = 1,
    Platform = 2,
    Spike = 3,
    PlayerStart = 4,
    ExitPortal = 5,
    Key = 6
}

[CreateAssetMenu(fileName = "RoomTemplate_01", menuName = "Spelunky/Room Template")]
public class RoomTemplate : ScriptableObject
{
    [Header("Room Type Definition")]
    public RoomType roomType;

    [HideInInspector]
    public TileType[] gridData;

    public void InitializeGrid()
    {
        int totalCells = GlobalGameDatabase.ROOM_WIDTH * GlobalGameDatabase.ROOM_HEIGHT;
        if (gridData == null || gridData.Length != totalCells)
        {
            gridData = new TileType[totalCells];
        }
    }

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= GlobalGameDatabase.ROOM_WIDTH || y < 0 || y >= GlobalGameDatabase.ROOM_HEIGHT)
            return TileType.Empty;

        return gridData[y * GlobalGameDatabase.ROOM_WIDTH + x];
    }

    public void SetTile(int x, int y, TileType type)
    {
        if (x < 0 || x >= GlobalGameDatabase.ROOM_WIDTH || y < 0 || y >= GlobalGameDatabase.ROOM_HEIGHT)
            return;

        gridData[y * GlobalGameDatabase.ROOM_WIDTH + x] = type;
    }

    public void ClearGrid()
    {
        gridData = new TileType[GlobalGameDatabase.ROOM_WIDTH * GlobalGameDatabase.ROOM_HEIGHT];
    }
}