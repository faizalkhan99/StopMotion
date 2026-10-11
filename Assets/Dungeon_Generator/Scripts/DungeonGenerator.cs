// using UnityEngine;
// using System.Collections.Generic;

// public class SpelunkyDungeonGenerator : MonoBehaviour
// {
//     private class RoomNode
//     {
//         public int x, y;
//         public bool openTop;
//         public bool openBottom;
//         public bool openLeft;
//         public bool openRight;
//         public bool isVisited; // Guarantees 100% reachability via graph traversal
//     }

//     [Header("Database Reference")]
//     [SerializeField] private GlobalGameDatabase database;

//     [Header("Grid Layout Settings")]
//     [SerializeField] private int gridWidth = 4;
//     [SerializeField] private int gridHeight = 4;

//     [Header("Generation Tuning")]
//     [Range(0f, 1f)]
//     [SerializeField] private float extraLoopChance = 0.25f; // Adds extra interconnecting doors for loops/tricks

//     private RoomNode[,] grid;
//     private Transform mapParent;

//     [ContextMenu("Generate Level")]
//     public void GenerateLevel()
//     {
//         if (database == null)
//         {
//             Debug.LogError("[SpelunkyGenerator] Assign GlobalGameDatabase reference!");
//             return;
//         }

//         ClearLevel();
//         mapParent = new GameObject("Generated_Spelunky_Dungeon").transform;
//         mapParent.SetParent(transform);

//         InitializeGrid();

//         // Pass 1: Build critical path from top to bottom
//         BuildCriticalPath();

//         // Pass 2: Connect EVERY remaining unreached cell to the accessible map (Spanning Tree)
//         ConnectAllRemainingRooms();

//         // Pass 3: Add optional interconnecting loops/tricks
//         AddExtraTrickLoops();

//         // Pass 4: Hard clamp boundaries against the void
//         EnforceBoundaries();

//         // Pass 5: Spawn templates for all 16 grid cells
//         InstantiateAllRooms();
//     }

//     private void InitializeGrid()
//     {
//         grid = new RoomNode[gridWidth, gridHeight];
//         for (int x = 0; x < gridWidth; x++)
//         {
//             for (int y = 0; y < gridHeight; y++)
//             {
//                 grid[x, y] = new RoomNode { x = x, y = y, isVisited = false };
//             }
//         }
//     }

//     private void BuildCriticalPath()
//     {
//         int currX = Random.Range(0, gridWidth);
//         int currY = gridHeight - 1; // Start top row

//         grid[currX, currY].isVisited = true;

//         while (currY > 0)
//         {
//             int move = Random.Range(0, 3); // 0 = Left, 1 = Right, 2 = Down

//             if (move == 0 && currX > 0 && !grid[currX - 1, currY].isVisited)
//             {
//                 // Move Left (Bi-directional handshake)
//                 ConnectRoomsHorizontally(currX, currX - 1, currY);
//                 currX--;
//                 grid[currX, currY].isVisited = true;
//             }
//             else if (move == 1 && currX < gridWidth - 1 && !grid[currX + 1, currY].isVisited)
//             {
//                 // Move Right (Bi-directional handshake)
//                 ConnectRoomsHorizontally(currX, currX + 1, currY);
//                 currX++;
//                 grid[currX, currY].isVisited = true;
//             }
//             else
//             {
//                 // Move Down (Bi-directional handshake)
//                 ConnectRoomsVertically(currY, currY - 1, currX);
//                 currY--;
//                 grid[currX, currY].isVisited = true;
//             }
//         }
//     }

//     private void ConnectAllRemainingRooms()
//     {
//         // Queue containing all currently accessible/connected room coordinates
//         Queue<Vector2Int> reachableQueue = new Queue<Vector2Int>();

//         for (int x = 0; x < gridWidth; x++)
//         {
//             for (int y = 0; y < gridHeight; y++)
//             {
//                 if (grid[x, y].isVisited)
//                 {
//                     reachableQueue.Enqueue(new Vector2Int(x, y));
//                 }
//             }
//         }

//         // BFS Traversal: Expand outward to ensure every unvisited cell attaches to an active room
//         while (reachableQueue.Count > 0)
//         {
//             Vector2Int current = reachableQueue.Dequeue();
//             List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current.x, current.y);

//             foreach (Vector2Int neighbor in unvisitedNeighbors)
//             {
//                 if (!grid[neighbor.x, neighbor.y].isVisited)
//                 {
//                     // Connect neighbor to current reachable node
//                     ConnectAdjacentNodes(current.x, current.y, neighbor.x, neighbor.y);
//                     grid[neighbor.x, neighbor.y].isVisited = true;
//                     reachableQueue.Enqueue(neighbor);
//                 }
//             }
//         }
//     }

//     private void AddExtraTrickLoops()
//     {
//         for (int x = 0; x < gridWidth; x++)
//         {
//             for (int y = 0; y < gridHeight; y++)
//             {
//                 // Optionally connect horizontally
//                 if (x < gridWidth - 1 && Random.value < extraLoopChance)
//                 {
//                     ConnectRoomsHorizontally(x, x + 1, y);
//                 }
//                 // Optionally connect vertically
//                 if (y < gridHeight - 1 && Random.value < extraLoopChance)
//                 {
//                     ConnectRoomsVertically(y, y + 1, x);
//                 }
//             }
//         }
//     }

//     private void ConnectAdjacentNodes(int x1, int y1, int x2, int y2)
//     {
//         if (x1 == x2)
//         {
//             int minY = Mathf.Min(y1, y2);
//             int maxY = Mathf.Max(y1, y2);
//             ConnectRoomsVertically(maxY, minY, x1);
//         }
//         else if (y1 == y2)
//         {
//             int minX = Mathf.Min(x1, x2);
//             int maxX = Mathf.Max(x1, x2);
//             ConnectRoomsHorizontally(minX, maxX, y1);
//         }
//     }

//     private void ConnectRoomsHorizontally(int leftX, int rightX, int y)
//     {
//         int minX = Mathf.Min(leftX, rightX);
//         int maxX = Mathf.Max(leftX, rightX);

//         grid[minX, y].openRight = true;
//         grid[maxX, y].openLeft = true;
//     }

//     private void ConnectRoomsVertically(int topY, int bottomY, int x)
//     {
//         int minY = Mathf.Min(topY, bottomY);
//         int maxY = Mathf.Max(topY, bottomY);

//         grid[x, maxY].openBottom = true;
//         grid[x, minY].openTop = true;
//     }

//     private List<Vector2Int> GetUnvisitedNeighbors(int x, int y)
//     {
//         List<Vector2Int> neighbors = new List<Vector2Int>();

//         if (x > 0 && !grid[x - 1, y].isVisited) neighbors.Add(new Vector2Int(x - 1, y));
//         if (x < gridWidth - 1 && !grid[x + 1, y].isVisited) neighbors.Add(new Vector2Int(x + 1, y));
//         if (y > 0 && !grid[x, y - 1].isVisited) neighbors.Add(new Vector2Int(x, y - 1));
//         if (y < gridHeight - 1 && !grid[x, y + 1].isVisited) neighbors.Add(new Vector2Int(x, y + 1));

//         return neighbors;
//     }

//     private void EnforceBoundaries()
//     {
//         for (int x = 0; x < gridWidth; x++)
//         {
//             for (int y = 0; y < gridHeight; y++)
//             {
//                 if (x == 0) grid[x, y].openLeft = false;
//                 if (x == gridWidth - 1) grid[x, y].openRight = false;
//                 if (y == gridHeight - 1) grid[x, y].openTop = false;
//                 if (y == 0) grid[x, y].openBottom = false; // Never open bottom on bottom floor
//             }
//         }
//     }

//     private void InstantiateAllRooms()
//     {
//         int spawnedCount = 0;

//         for (int x = 0; x < gridWidth; x++)
//         {
//             for (int y = 0; y < gridHeight; y++)
//             {
//                 RoomType resolvedType = ResolveRoomType(grid[x, y]);
//                 RoomTemplate template = GetValidTemplate(resolvedType);

//                 if (template != null)
//                 {
//                     SpawnRoomFromTemplate(template, x, y);
//                     spawnedCount++;
//                 }
//                 else
//                 {
//                     Debug.LogError($"[SpelunkyGenerator] Missing room template for type {resolvedType} at ({x}, {y})!");
//                 }
//             }
//         }

//         Debug.Log($"[SpelunkyGenerator] Level Generated. {spawnedCount}/{gridWidth * gridHeight} rooms successfully connected and instantiated.");
//     }

//     // private RoomType ResolveRoomType(RoomNode node)
//     // {
//     //     int count = (node.openTop ? 1 : 0) + (node.openBottom ? 1 : 0) + 
//     //                 (node.openLeft ? 1 : 0) + (node.openRight ? 1 : 0);

//     //     if (count >= 3) return RoomType.ALL_OPEN;

//     //     // Dual Straights
//     //     if (node.openTop && node.openBottom) return RoomType.TOP_BOTTOM_OPEN;
//     //     if (node.openLeft && node.openRight) return RoomType.LEFT_RIGHT_OPEN;

//     //     // Dual Corners (L-Turns)
//     //     if (node.openBottom && node.openRight) return RoomType.BOTTOM_RIGHT_OPEN;
//     //     if (node.openRight && node.openTop) return RoomType.RIGHT_TOP_OPEN;
//     //     if (node.openTop && node.openLeft) return RoomType.TOP_LEFT_OPEN;
//     //     if (node.openLeft && node.openBottom) return RoomType.LEFT_BOTTOM_OPEN;

//     //     // Single Dead Ends
//     //     if (node.openTop) return RoomType.OPEN_TOP;
//     //     if (node.openBottom) return RoomType.OPEN_BOTTOM;
//     //     if (node.openLeft) return RoomType.OPEN_LEFT;
//     //     if (node.openRight) return RoomType.OPEN_RIGHT;

//     //     return RoomType.LEFT_RIGHT_OPEN;
//     // }
//     private RoomType ResolveRoomType(RoomNode node)
//     {
//         // 4-Door Intersections
//         if (node.openTop && node.openBottom && node.openLeft && node.openRight)
//             return RoomType.ALL_OPEN;

//         // 3-Door T-Shapes
//         if (node.openTop && node.openBottom && node.openRight && !node.openLeft)
//             return RoomType.TOP_BOTTOM_RIGHT_OPEN;
//         if (node.openTop && node.openBottom && node.openLeft && !node.openRight)
//             return RoomType.TOP_BOTTOM_LEFT_OPEN;
//         if (node.openTop && node.openLeft && node.openRight && !node.openBottom)
//             return RoomType.TOP_LEFT_RIGHT_OPEN;
//         if (node.openBottom && node.openLeft && node.openRight && !node.openTop)
//             return RoomType.BOTTOM_LEFT_RIGHT_OPEN;

//         // 2-Door Straights
//         if (node.openTop && node.openBottom) return RoomType.TOP_BOTTOM_OPEN;
//         if (node.openLeft && node.openRight) return RoomType.LEFT_RIGHT_OPEN;

//         // 2-Door Corners (L-Turns)
//         if (node.openBottom && node.openRight) return RoomType.BOTTOM_RIGHT_OPEN;
//         if (node.openRight && node.openTop) return RoomType.RIGHT_TOP_OPEN;
//         if (node.openTop && node.openLeft) return RoomType.TOP_LEFT_OPEN;
//         if (node.openLeft && node.openBottom) return RoomType.LEFT_BOTTOM_OPEN;

//         // 1-Door Dead Ends
//         if (node.openTop) return RoomType.OPEN_TOP;
//         if (node.openBottom) return RoomType.OPEN_BOTTOM;
//         if (node.openLeft) return RoomType.OPEN_LEFT;
//         if (node.openRight) return RoomType.OPEN_RIGHT;

//         return RoomType.LEFT_RIGHT_OPEN;
//     }

//     private RoomTemplate GetValidTemplate(RoomType targetType)
//     {
//         List<RoomTemplate> pool = database.GetRoomsOfType(targetType);

//         if (pool == null || pool.Count == 0) pool = database.GetRoomsOfType(RoomType.ALL_OPEN);
//         if (pool == null || pool.Count == 0) pool = database.GetRoomsOfType(RoomType.LEFT_RIGHT_OPEN);

//         if (pool != null && pool.Count > 0)
//         {
//             return pool[Random.Range(0, pool.Count)];
//         }

//         return null;
//     }

//     private void SpawnRoomFromTemplate(RoomTemplate template, int gridX, int gridY)
//     {
//         Vector3 roomWorldOffset = new Vector3(
//             gridX * GlobalGameDatabase.ROOM_WIDTH,
//             gridY * GlobalGameDatabase.ROOM_HEIGHT,
//             0f
//         );

//         for (int x = 0; x < GlobalGameDatabase.ROOM_WIDTH; x++)
//         {
//             for (int y = 0; y < GlobalGameDatabase.ROOM_HEIGHT; y++)
//             {
//                 TileType tile = template.GetTile(x, y);
//                 if (tile == TileType.Empty) continue;

//                 Vector3 tilePos = roomWorldOffset + new Vector3(x, y, 0f);
//                 SpawnTileFromType(tile, tilePos);
//             }
//         }
//     }

//     private void SpawnTileFromType(TileType tile, Vector3 position)
//     {
//         switch (tile)
//         {
//             case TileType.Wall:
//                 if (database.wallTilePrefab) Instantiate(database.wallTilePrefab, position, Quaternion.identity, mapParent);
//                 break;
//             case TileType.Platform:
//                 if (database.platformPrefab) Instantiate(database.platformPrefab, position, Quaternion.identity, mapParent);
//                 break;
//             case TileType.Spike:
//                 if (database.spikePrefab) Instantiate(database.spikePrefab, position, Quaternion.identity, mapParent);
//                 break;
//             case TileType.PlayerStart:
//                 if (database.playerPrefab) Instantiate(database.playerPrefab, position, Quaternion.identity);
//                 break;
//             case TileType.ExitPortal:
//                 if (database.exitPortalPrefab) Instantiate(database.exitPortalPrefab, position, Quaternion.identity, mapParent);
//                 break;
//             case TileType.Key:
//                 if (database.keyPrefab) Instantiate(database.keyPrefab, position, Quaternion.identity, mapParent);
//                 break;
//         }
//     }

//     public void ClearLevel()
//     {
//         if (mapParent != null)
//         {
//             DestroyImmediate(mapParent.gameObject);
//         }
//     }
// }























using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    private class RoomNode
    {
        public int x, y;
        public bool openTop;
        public bool openBottom;
        public bool openLeft;
        public bool openRight;
        public bool isVisited;
        public bool isMainPath; // Marks rooms belonging to the critical path
    }

    [Header("Database Reference")]
    [SerializeField] private GlobalGameDatabase database;

    [Header("Grid Layout Settings")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;

    [Header("Generation Tuning")]
    [Range(0f, 1f)]
    [SerializeField] private float extraLoopChance = 0.25f;

    private RoomNode[,] grid;
    private Transform mapParent;

    // Hierarchy Folder Transforms
    private Transform mainPathParent;
    private Transform branchParent;
    private Transform turnParent;
    private Transform deadEndParent;

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        if (database == null)
        {
            Debug.LogError("Assign GlobalGameDatabase reference!");
            return;
        }

        ClearLevel();

        // Setup Root and Category Folders in Hierarchy
        mapParent = new GameObject("Generated_Dungeon").transform;
        mapParent.SetParent(transform);

        mainPathParent = new GameObject("MainPath_Rooms").transform;
        mainPathParent.SetParent(mapParent);

        branchParent = new GameObject("Branch_Rooms").transform;
        branchParent.SetParent(mapParent);

        turnParent = new GameObject("Turn_Rooms").transform;
        turnParent.SetParent(mapParent);

        deadEndParent = new GameObject("DeadEnd_Rooms").transform;
        deadEndParent.SetParent(mapParent);

        InitializeGrid();
        BuildCriticalPath();
        ConnectAllRemainingRooms();
        AddExtraTrickLoops();
        EnforceBoundaries();
        InstantiateAllRooms();
    }

    private void InitializeGrid()
    {
        grid = new RoomNode[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = new RoomNode { x = x, y = y, isVisited = false, isMainPath = false };
            }
        }
    }

    private void BuildCriticalPath()
    {
        int currX = Random.Range(0, gridWidth);
        int currY = gridHeight - 1; // Top row start

        grid[currX, currY].isVisited = true;
        grid[currX, currY].isMainPath = true;

        while (currY > 0)
        {
            int move = Random.Range(0, 3); // 0 = Left, 1 = Right, 2 = Down

            if (move == 0 && currX > 0 && !grid[currX - 1, currY].isVisited)
            {
                ConnectRoomsHorizontally(currX, currX - 1, currY);
                currX--;
                grid[currX, currY].isVisited = true;
                grid[currX, currY].isMainPath = true;
            }
            else if (move == 1 && currX < gridWidth - 1 && !grid[currX + 1, currY].isVisited)
            {
                ConnectRoomsHorizontally(currX, currX + 1, currY);
                currX++;
                grid[currX, currY].isVisited = true;
                grid[currX, currY].isMainPath = true;
            }
            else
            {
                ConnectRoomsVertically(currY, currY - 1, currX);
                currY--;
                grid[currX, currY].isVisited = true;
                grid[currX, currY].isMainPath = true;
            }
        }
    }

    private void ConnectAllRemainingRooms()
    {
        Queue<Vector2Int> reachableQueue = new Queue<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y].isVisited)
                {
                    reachableQueue.Enqueue(new Vector2Int(x, y));
                }
            }
        }

        while (reachableQueue.Count > 0)
        {
            Vector2Int current = reachableQueue.Dequeue();
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current.x, current.y);

            foreach (Vector2Int neighbor in unvisitedNeighbors)
            {
                if (!grid[neighbor.x, neighbor.y].isVisited)
                {
                    ConnectAdjacentNodes(current.x, current.y, neighbor.x, neighbor.y);
                    grid[neighbor.x, neighbor.y].isVisited = true;
                    // Side-branch rooms remain marked as isMainPath = false
                    reachableQueue.Enqueue(neighbor);
                }
            }
        }
    }

    private void AddExtraTrickLoops()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x < gridWidth - 1 && Random.value < extraLoopChance)
                {
                    ConnectRoomsHorizontally(x, x + 1, y);
                }
                if (y < gridHeight - 1 && Random.value < extraLoopChance)
                {
                    ConnectRoomsVertically(y, y + 1, x);
                }
            }
        }
    }

    private void ConnectAdjacentNodes(int x1, int y1, int x2, int y2)
    {
        if (x1 == x2)
        {
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);
            ConnectRoomsVertically(maxY, minY, x1);
        }
        else if (y1 == y2)
        {
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);
            ConnectRoomsHorizontally(minX, maxX, y1);
        }
    }

    private void ConnectRoomsHorizontally(int leftX, int rightX, int y)
    {
        int minX = Mathf.Min(leftX, rightX);
        int maxX = Mathf.Max(leftX, rightX);

        grid[minX, y].openRight = true;
        grid[maxX, y].openLeft = true;
    }

    private void ConnectRoomsVertically(int topY, int bottomY, int x)
    {
        int minY = Mathf.Min(topY, bottomY);
        int maxY = Mathf.Max(topY, bottomY);

        grid[x, maxY].openBottom = true;
        grid[x, minY].openTop = true;
    }

    private List<Vector2Int> GetUnvisitedNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        if (x > 0 && !grid[x - 1, y].isVisited) neighbors.Add(new Vector2Int(x - 1, y));
        if (x < gridWidth - 1 && !grid[x + 1, y].isVisited) neighbors.Add(new Vector2Int(x + 1, y));
        if (y > 0 && !grid[x, y - 1].isVisited) neighbors.Add(new Vector2Int(x, y - 1));
        if (y < gridHeight - 1 && !grid[x, y + 1].isVisited) neighbors.Add(new Vector2Int(x, y + 1));

        return neighbors;
    }

    private void EnforceBoundaries()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x == 0) grid[x, y].openLeft = false;
                if (x == gridWidth - 1) grid[x, y].openRight = false;
                if (y == gridHeight - 1) grid[x, y].openTop = false;
                if (y == 0) grid[x, y].openBottom = false;
            }
        }
    }

    private void InstantiateAllRooms()
    {
        int spawnedCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                RoomNode node = grid[x, y];
                RoomType resolvedType = ResolveRoomType(node);
                RoomTemplate template = GetValidTemplate(resolvedType);

                if (template != null)
                {
                    SpawnRoomFromTemplate(template, node, resolvedType);
                    spawnedCount++;
                }
                else
                {
                    Debug.LogError($"[SpelunkyGenerator] Missing room template for type {resolvedType} at ({x}, {y})!");
                }
            }
        }

        Debug.Log($"[SpelunkyGenerator] Level Generated and Organized. {spawnedCount}/{gridWidth * gridHeight} rooms successfully parented.");
    }

    private RoomType ResolveRoomType(RoomNode node)
    {
        if (node.openTop && node.openBottom && node.openLeft && node.openRight)
            return RoomType.ALL_OPEN;

        if (node.openTop && node.openBottom && node.openRight && !node.openLeft)
            return RoomType.TOP_BOTTOM_RIGHT_OPEN;
        if (node.openTop && node.openBottom && node.openLeft && !node.openRight)
            return RoomType.TOP_BOTTOM_LEFT_OPEN;
        if (node.openTop && node.openLeft && node.openRight && !node.openBottom)
            return RoomType.TOP_LEFT_RIGHT_OPEN;
        if (node.openBottom && node.openLeft && node.openRight && !node.openTop)
            return RoomType.BOTTOM_LEFT_RIGHT_OPEN;

        if (node.openTop && node.openBottom) return RoomType.TOP_BOTTOM_OPEN;
        if (node.openLeft && node.openRight) return RoomType.LEFT_RIGHT_OPEN;

        if (node.openBottom && node.openRight) return RoomType.BOTTOM_RIGHT_OPEN;
        if (node.openRight && node.openTop) return RoomType.RIGHT_TOP_OPEN;
        if (node.openTop && node.openLeft) return RoomType.TOP_LEFT_OPEN;
        if (node.openLeft && node.openBottom) return RoomType.LEFT_BOTTOM_OPEN;

        if (node.openTop) return RoomType.OPEN_TOP;
        if (node.openBottom) return RoomType.OPEN_BOTTOM;
        if (node.openLeft) return RoomType.OPEN_LEFT;
        if (node.openRight) return RoomType.OPEN_RIGHT;

        return RoomType.LEFT_RIGHT_OPEN;
    }

    private RoomTemplate GetValidTemplate(RoomType targetType)
    {
        List<RoomTemplate> pool = database.GetRoomsOfType(targetType);

        if (pool == null || pool.Count == 0) pool = database.GetRoomsOfType(RoomType.ALL_OPEN);
        if (pool == null || pool.Count == 0) pool = database.GetRoomsOfType(RoomType.LEFT_RIGHT_OPEN);

        if (pool != null && pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        return null;
    }

    private void SpawnRoomFromTemplate(RoomTemplate template, RoomNode node, RoomType resolvedType)
    {
        Vector3 roomWorldOffset = new Vector3(
            node.x * GlobalGameDatabase.ROOM_WIDTH,
            node.y * GlobalGameDatabase.ROOM_HEIGHT,
            0f
        );

        // Determine Category Name & Parent Folder
        Transform targetParent = GetParentFolderForNode(node, resolvedType);
        string categoryName = GetCategoryLabel(node, resolvedType);

        // Create a dedicated container object for this specific room instance
        GameObject roomContainer = new GameObject($"{categoryName}_Room_X{node.x}_Y{node.y}");
        roomContainer.transform.position = roomWorldOffset;
        roomContainer.transform.SetParent(targetParent);

        // Populate tiles inside the room container
        for (int x = 0; x < GlobalGameDatabase.ROOM_WIDTH; x++)
        {
            for (int y = 0; y < GlobalGameDatabase.ROOM_HEIGHT; y++)
            {
                TileType tile = template.GetTile(x, y);
                if (tile == TileType.Empty) continue;

                Vector3 tilePos = roomWorldOffset + new Vector3(x, y, 0f);
                SpawnTileFromType(tile, tilePos, roomContainer.transform);
            }
        }
    }

    private Transform GetParentFolderForNode(RoomNode node, RoomType type)
    {
        // 1. Critical Path takes top priority in organization
        if (node.isMainPath) return mainPathParent;

        // 2. Dead ends (1 door open)
        if (IsDeadEnd(type)) return deadEndParent;

        // 3. Turns / Corners (2 door L-turns)
        if (IsTurn(type)) return turnParent;

        // 4. Everything else (Supporting branches, straights, junctions)
        return branchParent;
    }

    private string GetCategoryLabel(RoomNode node, RoomType type)
    {
        if (node.isMainPath) return "MainPath";
        if (IsDeadEnd(type)) return "DeadEnd";
        if (IsTurn(type)) return "Turn";
        return "Branch";
    }

    private bool IsDeadEnd(RoomType type)
    {
        return type == RoomType.OPEN_TOP || type == RoomType.OPEN_BOTTOM ||
               type == RoomType.OPEN_LEFT || type == RoomType.OPEN_RIGHT;
    }

    private bool IsTurn(RoomType type)
    {
        return type == RoomType.BOTTOM_RIGHT_OPEN || type == RoomType.RIGHT_TOP_OPEN ||
               type == RoomType.TOP_LEFT_OPEN || type == RoomType.LEFT_BOTTOM_OPEN;
    }

    private void SpawnTileFromType(TileType tile, Vector3 position, Transform roomParent)
    {
        GameObject prefabToSpawn = tile switch
        {
            TileType.Wall => database.wallTilePrefab,
            TileType.Platform => database.platformPrefab,
            TileType.Spike => database.spikePrefab,
            TileType.PlayerStart => database.playerPrefab,
            TileType.ExitPortal => database.exitPortalPrefab,
            TileType.Key => database.keyPrefab,
            _ => null
        };

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, position, Quaternion.identity, roomParent);
        }
    }

    public void ClearLevel()
    {
        if (mapParent != null)
        {
            DestroyImmediate(mapParent.gameObject);
        }
    }
}