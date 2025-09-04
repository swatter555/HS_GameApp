using UnityEngine;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Constants for the hex map.
    /// </summary>
    public class HexMapConstants
    {
        // Hex grid orientation.
        public const bool IsPointyTop = true;

        // Hex size constant.
        public const int HexSize = 256;

        // Rendering constants.
        public const float PixelScaleX = 1;
        public const float PixelScaleY = 1;
        public const int SpritePPU = 256;

        // Hex grid size constants.
        public const int SmallHexWidth = 32;
        public const int SmallHexHeight = 21;
        public const int LargeHexWidth = 32;
        public const int LargeHexHeight = 42;

        // Vector represents no hex is selected.
        public static readonly Vector2Int NoHexSelected = new(-1, -1);

        /// <summary>
        /// Gets vertical spacing for the hex grid.
        /// </summary>
        /// <returns></returns>
        public static float GetVerticalSpacing()
        {
            return HexSize * 0.75f;
        }

        // Version of the .map file format.
        public const int CurrentMapDataVersion = 1;
    }

    /// <summary>
    /// Pointy-Top hex directions.
    /// </summary>
    public enum HexDirection
    {
        NE,
        E,
        SE,
        SW,
        W,
        NW
    }

    /// <summary>
    /// HexTile border types.
    /// </summary>
    public enum BorderType
    {
        None,
        River,
        Bridge,
        DestroyedBridge,
        PontoonBridge
    }

    /// <summary>
    /// Types of bridges.
    /// </summary>
    public enum BridgeType
    {
        Regular,
        DamagedRegular,
        Pontoon
    }

    /// <summary>
    /// Types of map icons.
    /// </summary>
    public enum MapIconType
    {
        Airbase,
        Fort
    }

    /// <summary>
    /// The terrain BaseUnitValueType of the hex.
    /// </summary>
    public enum TerrainType
    {
        Water,
        Clear,
        Forest,
        Rough,
        Marsh,
        Mountains,
        MinorCity,
        MajorCity,
        Impassable
    }

    /// <summary>
    /// Movement cost crossing a hex, based on 20 movement points.
    /// </summary>
    public enum HexMovementCost
    {
        Impassable = 999,
        Water = 1,
        Plains = 1,
        Forest = 2,
        Rough = 3,
        Marsh = 4,
        Mountains = 5,
        MinorCity = 1,
        MajorCity = 1
    }

    /// <summary>
    /// Describes which side control a tile.
    /// </summary>
    public enum TileControl
    {
        Red,  // Friendly.
        Blue, // OPFOR.
        Grey, // Neutral.
        None  // No control.
    }

    /// <summary>
    /// Helps track nationality control within a faction.
    /// </summary>
    public enum DefaultTileControl
    {
        None,
        BE, // Belgium
        DE, // Denmark
        FR, // France
        MJ, // Mujahideen
        NE, // Netherlands
        SV, // Soviet Union
        UK, // United Kingdom
        US, // United States
        GE, // Germany
        CH, // China
        IR, // Iran
        IQ, // Iraq
        SA, // Saudi Arabia
        KW  // Kuwait
    }

    /// <summary>
    /// The types of map configurations.
    /// </summary>
    public enum MapConfig
    {
        Small,
        Large,
        None
    }
    
    /// <summary>
    /// The types of hex outlines.
    /// </summary>
    public enum HexOutlineColor
    {
        Black,
        White,
        Grey
    }

    /// <summary>
    /// The color types for map text elements.
    /// </summary>
    public enum TextColor
    {
        Black,
        White,
        Gold,
        Red,
        Blue,
        Grey,
        Yellow,
        Green,
        Teal
    }

    /// <summary>
    /// The text size for the map element
    /// </summary>
    public enum TextSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// The font weight for the map elelemt
    /// </summary>
    public enum FontWeight
    {
        Light,
        Medium,
        Bold
    }
}