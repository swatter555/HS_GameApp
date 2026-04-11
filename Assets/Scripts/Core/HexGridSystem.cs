using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Patterns;
using HammerAndSickle.Models;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Authoritative coordinate math for the pointy-top, odd-r hex grid with top-left origin.
    /// Row 0 is the highest world Y; Y decreases as row increases.
    /// Odd rows stagger right by half a hex width.
    /// </summary>
    public class HexGridSystem : Singleton<HexGridSystem>
    {
        #region Constants

        private const string CLASS_NAME = nameof(HexGridSystem);
        private const int MIN_DIMENSION = 10;

        // Hex geometry in world units (hex pixel size / background map PPU)
        // Hex sprites are 256px; background map is at 100 PPU.
        // Each hex cell = 256/100 = 2.56 world units to align with the background.
        private const float HEX_WIDTH = GameData.GameData.HexSize / GameData.GameData.MapPPU;   // 2.56f
        private const float HALF_HEX_WIDTH = HEX_WIDTH * 0.5f;                 // 1.28f
        private const float VERTICAL_SPACING = HEX_WIDTH * 0.75f;              // 1.92f

        // Direction offsets for pointy-top, odd-r (matches HexMapUtil order: NE, E, SE, SW, W, NW)
        private static readonly Vector2Int[] EvenRowDirections =
        {
            new(-1, -1), // NW
            new(0, -1),  // NE
            new(1, 0),   // E
            new(0, 1),   // SE
            new(-1, 1),  // SW
            new(-1, 0)   // W
        };

        private static readonly Vector2Int[] OddRowDirections =
        {
            new(0, -1),  // NW
            new(1, -1),  // NE
            new(1, 0),   // E
            new(1, 1),   // SE
            new(0, 1),   // SW
            new(-1, 0)   // W
        };

        #endregion // Constants

        #region Fields

        private int _mapWidth;
        private int _mapHeight;

        #endregion // Fields

        #region Properties

        /// <summary>Map width in columns.</summary>
        public int MapWidth => _mapWidth;

        /// <summary>Map height in rows.</summary>
        public int MapHeight => _mapHeight;

        /// <summary>True after Initialize has been called with valid dimensions.</summary>
        public bool IsInitialized => _mapWidth > 0 && _mapHeight > 0;

        #endregion // Properties

        #region Initialization

        /// <summary>
        /// Sets map dimensions. Must be called before any coordinate operations.
        /// </summary>
        /// <param name="width">Number of hex columns (>= 10).</param>
        /// <param name="height">Number of hex rows (>= 10).</param>
        public void Initialize(int width, int height)
        {
            if (width < MIN_DIMENSION || height < MIN_DIMENSION)
                throw new ArgumentException(
                    $"{CLASS_NAME}.Initialize: dimensions must be >= {MIN_DIMENSION}. Got {width}x{height}.");

            _mapWidth = width;
            _mapHeight = height;
        }

        #endregion // Initialization

        #region Coordinate Conversion

        /// <summary>
        /// Converts hex grid coordinates to a Unity world position.
        /// Row 0 maps to the highest world Y (top-left origin). Odd rows stagger right.
        /// </summary>
        /// <param name="hexPos">Hex grid position (col, row).</param>
        /// <returns>World-space Vector3 (x, y=0, z=0) suitable for 2D rendering.</returns>
        public Vector3 HexToWorld(Position2D hexPos)
        {
            int col = hexPos.IntX;
            int row = hexPos.IntY;

            float x = col * HEX_WIDTH;

            // Odd-row stagger: shift right by half a hex width
            if ((row & 1) == 1)
                x += HALF_HEX_WIDTH;

            // Match Unity tilemap convention: row 0 at bottom, increasing row moves upward (positive Y)
            float y = row * VERTICAL_SPACING;

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Converts a Unity world position to the nearest hex grid coordinate.
        /// Inverse of HexToWorld.
        /// </summary>
        /// <param name="worldPos">World-space position.</param>
        /// <returns>Nearest hex grid position (col, row).</returns>
        public Position2D WorldToHex(Vector3 worldPos)
        {
            // Approximate row from Y
            int row = Mathf.RoundToInt(worldPos.y / VERTICAL_SPACING);

            // Remove odd-row stagger before rounding column
            float adjustedX = worldPos.x;
            if ((row & 1) == 1)
                adjustedX -= HALF_HEX_WIDTH;

            int col = Mathf.RoundToInt(adjustedX / HEX_WIDTH);

            return new Position2D(col, row);
        }

        /// <summary>
        /// Converts a screen position to a hex grid coordinate via camera ray.
        /// </summary>
        /// <param name="screenPos">Screen-space position (e.g. Input.mousePosition).</param>
        /// <param name="cam">Camera used for the conversion.</param>
        /// <returns>Nearest hex grid position.</returns>
        public Position2D ScreenToHex(Vector3 screenPos, Camera cam)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            worldPos.z = 0f;
            return WorldToHex(worldPos);
        }

        #endregion // Coordinate Conversion

        #region Neighbor Queries

        /// <summary>
        /// Gets the neighboring hex position in the specified direction.
        /// </summary>
        /// <param name="pos">Starting hex position.</param>
        /// <param name="dir">Hex direction (NE=0 through NW=5).</param>
        /// <returns>Neighbor's hex position.</returns>
        public Position2D GetNeighborPosition(Position2D pos, HexDirection dir)
        {
            int row = pos.IntY;
            var offsets = (row & 1) == 1 ? OddRowDirections : EvenRowDirections;
            var offset = offsets[(int)dir];
            return new Position2D(pos.IntX + offset.x, pos.IntY + offset.y);
        }

        /// <summary>
        /// Gets all six neighbor positions for a hex.
        /// </summary>
        /// <param name="pos">Center hex position.</param>
        /// <returns>List of 6 neighbor positions (some may be out of bounds).</returns>
        public List<Position2D> GetAllNeighborPositions(Position2D pos)
        {
            var neighbors = new List<Position2D>(6);
            for (int i = 0; i < 6; i++)
                neighbors.Add(GetNeighborPosition(pos, (HexDirection)i));
            return neighbors;
        }

        #endregion // Neighbor Queries

        #region Distance

        /// <summary>
        /// Calculates hex distance between two positions using cube coordinate conversion.
        /// </summary>
        /// <param name="a">First hex position.</param>
        /// <param name="b">Second hex position.</param>
        /// <returns>Distance in hex steps.</returns>
        public int GetHexDistance(Position2D a, Position2D b)
        {
            var cubeA = OffsetToCube(a);
            var cubeB = OffsetToCube(b);

            return (Mathf.Abs(cubeA.x - cubeB.x) +
                    Mathf.Abs(cubeA.y - cubeB.y) +
                    Mathf.Abs(cubeA.z - cubeB.z)) / 2;
        }

        /// <summary>
        /// Converts odd-r offset coordinates to cube coordinates.
        /// </summary>
        private static Vector3Int OffsetToCube(Position2D offset)
        {
            int col = offset.IntX;
            int row = offset.IntY;

            int x = col - (row - (row & 1)) / 2;
            int z = row;
            int y = -x - z;

            return new Vector3Int(x, y, z);
        }

        #endregion // Distance

        #region Bounds Checking

        /// <summary>
        /// Checks whether a hex position is within the current map bounds.
        /// Enforces the odd-row last-column rule (odd rows have one fewer column).
        /// </summary>
        /// <param name="pos">Hex position to check.</param>
        /// <returns>True if in bounds.</returns>
        public bool IsInBounds(Position2D pos)
        {
            if (!IsInitialized) return false;

            int col = pos.IntX;
            int row = pos.IntY;

            if (col < 0 || row < 0 || row >= _mapHeight) return false;

            // Odd-row last-column rule: odd rows are missing the last column
            int maxCol = (row & 1) == 1 ? _mapWidth - 1 : _mapWidth;
            return col < maxCol;
        }

        #endregion // Bounds Checking
    }
}
