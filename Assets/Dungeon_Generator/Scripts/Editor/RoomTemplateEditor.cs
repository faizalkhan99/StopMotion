#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomTemplate))]
public class RoomTemplateEditor : Editor
{
    private RoomTemplate template;
    private TileType selectedTileType = TileType.Wall;

    private void OnEnable()
    {
        template = (RoomTemplate)target;
        template.InitializeGrid();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Room Type Category Selector
        EditorGUI.BeginChangeCheck();
        template.roomType = (RoomType)EditorGUILayout.EnumPopup("Room Type Category", template.roomType);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(template);
        }

        EditorGUILayout.Space(10);

        // 2. Tile Selection Palette
        EditorGUILayout.LabelField("Select Tile Type to Paint:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
        {
            if (type == TileType.Empty) continue;

            GUI.backgroundColor = (selectedTileType == type) ? Color.green : Color.white;
            if (GUILayout.Button(type.ToString(), GUILayout.Height(25)))
            {
                selectedTileType = type;
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 3. Clear Grid Button
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear Entire Grid", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Grid", "Are you sure you want to empty the grid?", "Yes", "No"))
            {
                Undo.RecordObject(template, "Clear Grid");
                template.ClearGrid();
                EditorUtility.SetDirty(template);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField($"Paint Grid ({GlobalGameDatabase.ROOM_WIDTH}x{GlobalGameDatabase.ROOM_HEIGHT}):", EditorStyles.boldLabel);

        // 4. Guaranteed Visible Painting Grid Area
        const float cellSize = 20f;
        const float cellPadding = 2f;
        float totalWidth = GlobalGameDatabase.ROOM_WIDTH * (cellSize + cellPadding);
        float totalHeight = GlobalGameDatabase.ROOM_HEIGHT * (cellSize + cellPadding);

        // Reserve exact layout space so it never collapses to 0 height
        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight, GUILayout.Width(totalWidth), GUILayout.Height(totalHeight));
        Event e = Event.current;

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && gridRect.Contains(e.mousePosition))
        {
            float localX = e.mousePosition.x - gridRect.x;
            float localY = e.mousePosition.y - gridRect.y;

            int gridX = Mathf.FloorToInt(localX / (cellSize + cellPadding));
            int gridY = GlobalGameDatabase.ROOM_HEIGHT - 1 - Mathf.FloorToInt(localY / (cellSize + cellPadding));

            if (gridX >= 0 && gridX < GlobalGameDatabase.ROOM_WIDTH && gridY >= 0 && gridY < GlobalGameDatabase.ROOM_HEIGHT)
            {
                TileType currentTile = template.GetTile(gridX, gridY);

                if (e.button == 0 && currentTile != selectedTileType) // Left-Click Paint
                {
                    Undo.RecordObject(template, "Paint Tile");
                    template.SetTile(gridX, gridY, selectedTileType);
                    EditorUtility.SetDirty(template);
                    e.Use();
                }
                else if (e.button == 1 && currentTile != TileType.Empty) // Right-Click Erase
                {
                    Undo.RecordObject(template, "Erase Tile");
                    template.SetTile(gridX, gridY, TileType.Empty);
                    EditorUtility.SetDirty(template);
                    e.Use();
                }
            }
        }

        // Render Grid Cells Visually
        for (int y = GlobalGameDatabase.ROOM_HEIGHT - 1; y >= 0; y--)
        {
            int visualY = GlobalGameDatabase.ROOM_HEIGHT - 1 - y;
            for (int x = 0; x < GlobalGameDatabase.ROOM_WIDTH; x++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + x * (cellSize + cellPadding),
                    gridRect.y + visualY * (cellSize + cellPadding),
                    cellSize,
                    cellSize
                );

                TileType tile = template.GetTile(x, y);
                Handles.DrawSolidRectangleWithOutline(cellRect, GetTileColor(tile), Color.black);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private Color GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Wall => new Color(0.2f, 0.2f, 0.2f),
            TileType.Platform => new Color(0.6f, 0.4f, 0.2f),
            TileType.Spike => Color.red,
            TileType.PlayerStart => Color.cyan,
            TileType.ExitPortal => Color.magenta,
            TileType.Key => Color.yellow,
            _ => new Color(0.85f, 0.85f, 0.85f)
        };
    }
}

// Database Guard: Prevents dragging wrong room types into incorrect database lists
[CustomEditor(typeof(GlobalGameDatabase))]
public class GlobalGameDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GlobalGameDatabase db = (GlobalGameDatabase)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            ValidateList(db.openTopRooms, RoomType.OPEN_TOP, "Open Top");
            ValidateList(db.openBottomRooms, RoomType.OPEN_BOTTOM, "Open Bottom");
            ValidateList(db.openLeftRooms, RoomType.OPEN_LEFT, "Open Left");
            ValidateList(db.openRightRooms, RoomType.OPEN_RIGHT, "Open Right");

            ValidateList(db.topBottomRooms, RoomType.TOP_BOTTOM_OPEN, "Top Bottom Open");
            ValidateList(db.leftRightRooms, RoomType.LEFT_RIGHT_OPEN, "Left Right Open");

            ValidateList(db.bottomRightRooms, RoomType.BOTTOM_RIGHT_OPEN, "Bottom Right Open");
            ValidateList(db.rightTopRooms, RoomType.RIGHT_TOP_OPEN, "Right Top Open");
            ValidateList(db.topLeftRooms, RoomType.TOP_LEFT_OPEN, "Top Left Open");
            ValidateList(db.leftBottomRooms, RoomType.LEFT_BOTTOM_OPEN, "Left Bottom Open");

            ValidateList(db.topBottomRightRooms, RoomType.TOP_BOTTOM_RIGHT_OPEN, "Top Bottom Right Open");
            ValidateList(db.topBottomLeftRooms, RoomType.TOP_BOTTOM_LEFT_OPEN, "Top Bottom Left Open");
            ValidateList(db.topLeftRightRooms, RoomType.TOP_LEFT_RIGHT_OPEN, "Top Left Right Open");
            ValidateList(db.bottomLeftRightRooms, RoomType.BOTTOM_LEFT_RIGHT_OPEN, "Bottom Left Right Open");

            ValidateList(db.allOpenRooms, RoomType.ALL_OPEN, "All Open");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ValidateList(System.Collections.Generic.List<RoomTemplate> list, RoomType requiredType, string categoryName)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null && list[i].roomType != requiredType)
            {
                EditorUtility.DisplayDialog("Invalid Room Assignment!", 
                    $"The room template '{list[i].name}' is set to '{list[i].roomType}'.\nIt cannot be placed in the '{categoryName}' list!", "OK");
                list.RemoveAt(i);
            }
        }
    }
}
#endif