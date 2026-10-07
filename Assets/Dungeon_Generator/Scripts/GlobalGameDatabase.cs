using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GlobalGameDatabase", menuName = "Spelunky/Global Database")]
public class GlobalGameDatabase : ScriptableObject
{
    public const int ROOM_WIDTH = 19;
    public const int ROOM_HEIGHT = 12;

    [Header("Prefabs")]
    public GameObject wallTilePrefab;
    public GameObject platformPrefab;
    public GameObject playerPrefab;
    public GameObject exitPortalPrefab;
    public GameObject spikePrefab;
    public GameObject keyPrefab;

    [Header("--- 1-Door Templates (Dead Ends) ---")]
    public List<RoomTemplate> openTopRooms = new List<RoomTemplate>();
    public List<RoomTemplate> openBottomRooms = new List<RoomTemplate>();
    public List<RoomTemplate> openLeftRooms = new List<RoomTemplate>();
    public List<RoomTemplate> openRightRooms = new List<RoomTemplate>();

    [Header("--- 2-Door Straight Templates ---")]
    public List<RoomTemplate> topBottomRooms = new List<RoomTemplate>();
    public List<RoomTemplate> leftRightRooms = new List<RoomTemplate>();

    [Header("--- 2-Door Corner Templates (L-Turns) ---")]
    public List<RoomTemplate> bottomRightRooms = new List<RoomTemplate>();
    public List<RoomTemplate> rightTopRooms = new List<RoomTemplate>();
    public List<RoomTemplate> topLeftRooms = new List<RoomTemplate>();
    public List<RoomTemplate> leftBottomRooms = new List<RoomTemplate>();

    [Header("--- 3-Door T-Shape Templates ---")]
    public List<RoomTemplate> topBottomRightRooms = new List<RoomTemplate>();
    public List<RoomTemplate> topBottomLeftRooms = new List<RoomTemplate>();
    public List<RoomTemplate> topLeftRightRooms = new List<RoomTemplate>();
    public List<RoomTemplate> bottomLeftRightRooms = new List<RoomTemplate>();

    [Header("--- 4-Door Templates ---")]
    public List<RoomTemplate> allOpenRooms = new List<RoomTemplate>();

    public List<RoomTemplate> GetRoomsOfType(RoomType type)
    {
        return type switch
        {
            RoomType.OPEN_TOP => openTopRooms,
            RoomType.OPEN_BOTTOM => openBottomRooms,
            RoomType.OPEN_LEFT => openLeftRooms,
            RoomType.OPEN_RIGHT => openRightRooms,

            RoomType.TOP_BOTTOM_OPEN => topBottomRooms,
            RoomType.LEFT_RIGHT_OPEN => leftRightRooms,

            RoomType.BOTTOM_RIGHT_OPEN => bottomRightRooms,
            RoomType.RIGHT_TOP_OPEN => rightTopRooms,
            RoomType.TOP_LEFT_OPEN => topLeftRooms,
            RoomType.LEFT_BOTTOM_OPEN => leftBottomRooms,

            RoomType.TOP_BOTTOM_RIGHT_OPEN => topBottomRightRooms,
            RoomType.TOP_BOTTOM_LEFT_OPEN => topBottomLeftRooms,
            RoomType.TOP_LEFT_RIGHT_OPEN => topLeftRightRooms,
            RoomType.BOTTOM_LEFT_RIGHT_OPEN => bottomLeftRightRooms,

            RoomType.ALL_OPEN => allOpenRooms,
            _ => leftRightRooms
        };
    }
}