using System;
using System.Collections.Generic;
using HammerAndSickle.Services;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Static utility class for hex map operations including coordinate conversions,
    /// neighbor calculations, and tactical queries for the pointy-top, odd-r hex system.
    /// </summary>
    public static class HexMapUtil
    {
        #region Constants

        private const string CLASS_NAME = nameof(HexMapUtil);

        // Hex geometry constants for pointy-top orientation
        private const float HEX_WIDTH = 1.0f;
        private const float HEX_HEIGHT = 0.866025404f; // sqrt(3)/2
        private const float HEX_HORIZONTAL_SPACING = 0.75f; // 3/4 of hex width
        private const float HEX_VERTICAL_SPACING = HEX_HEIGHT;

        // Direction vectors for pointy-top, odd-r hex system
        private static readonly Vector2Int[] EvenRowDirections = new Vector2Int[]
        {
            new (-1, -1), // NW
            new (0, -1),  // NE  
            new (1, 0),   // E
            new (0, 1),   // SE
            new (-1, 1),  // SW
            new (-1, 0)   // W
        };

        private static readonly Vector2Int[] OddRowDirections = new Vector2Int[]
        {
            new (0, -1),  // NW
            new (1, -1),  // NE
            new (1, 0),   // E
            new (1, 1),   // SE
            new (0, 1),   // SW
            new (-1, 0)   // W
        };

        #endregion // Constants

        #region Coordinate Conversion Methods

        /// <summary>
        /// Converts Vector2Int hex coordinates to Coordinate2D for runtime use.
        /// </summary>
        /// <param name="hexCoords">Hex coordinates from JSON data</param>
        /// <returns>Coordinate2D for runtime storage</returns>
        public static Position2D Vector2IntToCoordinate2D(Vector2Int hexCoords)
        {
            try
            {
                return new Position2D(hexCoords.x, hexCoords.y);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Vector2IntToCoordinate2D), ex);
                return Position2D.Zero;
            }
        }

        /// <summary>
        /// Converts Coordinate2D hex coordinates to Vector2Int for serialization.
        /// </summary>
        /// <param name="coords">Runtime hex coordinates</param>
        /// <returns>Vector2Int for JSON serialization</returns>
        public static Vector2Int Coordinate2DToVector2Int(Position2D coords)
        {
            try
            {
                return new Vector2Int(Mathf.RoundToInt(coords.X), Mathf.RoundToInt(coords.Y));
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Coordinate2DToVector2Int), ex);
                return Vector2Int.zero;
            }
        }

        /// <summary>
        /// Converts hex coordinates to Unity world position for rendering.
        /// Uses pointy-top, odd-r hex layout.
        /// </summary>
        /// <param name="hexCoords">Hex grid coordinates</param>
        /// <returns>World position for Unity rendering</returns>
        public static Vector3 HexToWorldPosition(Position2D hexCoords)
        {
            try
            {
                float x = hexCoords.X * HEX_HORIZONTAL_SPACING;
                float z = hexCoords.Y * HEX_VERTICAL_SPACING;

                // Offset odd rows for hex grid alignment
                if (Mathf.RoundToInt(hexCoords.Y) % 2 == 1)
                {
                    x += HEX_HORIZONTAL_SPACING * 0.5f;
                }

                return new Vector3(x, 0f, z);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(HexToWorldPosition), ex);
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Converts Unity world position back to hex coordinates.
        /// </summary>
        /// <param name="worldPos">World position from Unity</param>
        /// <returns>Hex coordinates</returns>
        public static Position2D WorldPositionToHex(Vector3 worldPos)
        {
            try
            {
                // Convert world position to approximate hex coordinates
                float y = worldPos.z / HEX_VERTICAL_SPACING;
                float x = worldPos.x / HEX_HORIZONTAL_SPACING;

                // Adjust for odd row offset
                if (Mathf.RoundToInt(y) % 2 == 1)
                {
                    x -= 0.5f;
                }

                return new Position2D(Mathf.Round(x), Mathf.Round(y));
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(WorldPositionToHex), ex);
                return Position2D.Zero;
            }
        }

        #endregion // Coordinate Conversion Methods

        #region Neighbor Calculation Methods

        /// <summary>
        /// Gets the position of a neighboring hex in the specified direction.
        /// Uses pointy-top, odd-r hex coordinate system.
        /// </summary>
        /// <param name="hexPos">Starting hex position</param>
        /// <param name="direction">Direction to the neighbor</param>
        /// <returns>Position of the neighboring hex</returns>
        public static Position2D GetNeighborPosition(Position2D hexPos, HexDirection direction)
        {
            try
            {
                int row = Mathf.RoundToInt(hexPos.Y);
                bool isOddRow = (row % 2) == 1;

                Vector2Int[] directions = isOddRow ? OddRowDirections : EvenRowDirections;
                Vector2Int offset = directions[(int)direction];

                return new Position2D(hexPos.X + offset.x, hexPos.Y + offset.y);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetNeighborPosition), ex);
                return hexPos; // Return original position on error
            }
        }

        /// <summary>
        /// Gets all neighbor positions for a given hex.
        /// </summary>
        /// <param name="hexPos">Center hex position</param>
        /// <returns>Array of 6 neighbor positions</returns>
        public static Position2D[] GetAllNeighborPositions(Position2D hexPos)
        {
            try
            {
                Position2D[] neighbors = new Position2D[6];

                for (int i = 0; i < 6; i++)
                {
                    neighbors[i] = GetNeighborPosition(hexPos, (HexDirection)i);
                }

                return neighbors;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllNeighborPositions), ex);
                return new Position2D[6]; // Return empty array on error
            }
        }

        /// <summary>
        /// Calculates the hex grid distance between two positions.
        /// </summary>
        /// <param name="a">First hex position</param>
        /// <param name="b">Second hex position</param>
        /// <returns>Distance in hex grid units</returns>
        public static int GetHexDistance(Position2D a, Position2D b)
        {
            try
            {
                // Convert offset coordinates to cube coordinates for distance calculation
                var cubeA = OffsetToCube(a);
                var cubeB = OffsetToCube(b);

                return (Mathf.Abs(cubeA.x - cubeB.x) +
                       Mathf.Abs(cubeA.y - cubeB.y) +
                       Mathf.Abs(cubeA.z - cubeB.z)) / 2;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexDistance), ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets the opposite direction for a given hex direction.
        /// </summary>
        /// <param name="direction">Original direction</param>
        /// <returns>Opposite direction</returns>
        public static HexDirection GetOppositeDirection(HexDirection direction)
        {
            try
            {
                return (HexDirection)(((int)direction + 3) % 6);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetOppositeDirection), ex);
                return direction; // Return original direction on error
            }
        }

        /// <summary>
        /// Converts odd-r offset coordinates to cube coordinates for calculations.
        /// </summary>
        /// <param name="offset">Offset coordinates</param>
        /// <returns>Cube coordinates (X, Y, z)</returns>
        private static Vector3Int OffsetToCube(Position2D offset)
        {
            int col = Mathf.RoundToInt(offset.X);
            int row = Mathf.RoundToInt(offset.Y);

            int x = col - (row - (row & 1)) / 2;
            int z = row;
            int y = -x - z;

            return new Vector3Int(x, y, z);
        }

        #endregion // Neighbor Calculation Methods

        #region Tactical Query Methods (Stubs)

        /// <summary>
        /// Gets all hexes within a specified radius of a center position.
        /// </summary>
        /// <param name="map">Hex map to search</param>
        /// <param name="center">Center position</param>
        /// <param name="radius">Search radius in hex units</param>
        /// <returns>List of hexes within radius</returns>
        public static List<HexTile> GetHexesInRadius(HexMap map, Position2D center, int radius)
        {
            try
            {
                // TODO: Implement radius-based hex collection
                // TODO: Use GetHexDistance for range checking
                // TODO: Optimize with spiral or ring-based iteration

                AppService.CaptureUiMessage($"GetHexesInRadius stub: center {center}, radius {radius}");
                return new List<HexTile>();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexesInRadius), ex);
                return new List<HexTile>();
            }
        }

        /// <summary>
        /// Gets all hexes along a line between two positions.
        /// </summary>
        /// <param name="map">Hex map to search</param>
        /// <param name="start">Starting position</param>
        /// <param name="end">Ending position</param>
        /// <returns>List of hexes along the line</returns>
        public static List<HexTile> GetHexesInLine(HexMap map, Position2D start, Position2D end)
        {
            try
            {
                // TODO: Implement line-drawing algorithm for hexes
                // TODO: Use cube coordinate interpolation
                // TODO: Handle edge cases and ensure continuity

                AppService.CaptureUiMessage($"GetHexesInLine stub: from {start} to {end}");
                return new List<HexTile>();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexesInLine), ex);
                return new List<HexTile>();
            }
        }

        /// <summary>
        /// Finds the shortest path between two hexes using terrain costs.
        /// </summary>
        /// <param name="map">Hex map to search</param>
        /// <param name="start">Starting position</param>
        /// <param name="end">Ending position</param>
        /// <returns>List of hexes forming the optimal path</returns>
        public static List<HexTile> FindPath(HexMap map, Position2D start, Position2D end)
        {
            try
            {
                // TODO: Implement A* pathfinding algorithm
                // TODO: Use hex movement costs from terrain types
                // TODO: Consider unit movement restrictions
                // TODO: Handle blocked hexes and obstacles

                AppService.CaptureUiMessage($"FindPath stub: from {start} to {end}");
                return new List<HexTile>();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FindPath), ex);
                return new List<HexTile>();
            }
        }
        #endregion

        #region Unit Placement Methods (Stubs)
        /// <summary>
        /// Places a unit at the specified hex position.
        /// </summary>
        /// <param name="map">Hex map</param>
        /// <param name="unitId">Unit identifier</param>
        /// <param name="position">Target hex position</param>
        /// <returns>True if placement successful, false otherwise</returns>
        public static bool PlaceUnitAt(HexMap map, string unitId, Position2D position)
        {
            try
            {
                // TODO: Implement unit placement logic
                // TODO: Validate hex is empty and accessible
                // TODO: Update hex occupancy information
                // TODO: Handle stacking rules and restrictions

                AppService.CaptureUiMessage($"PlaceUnitAt stub: unit {unitId} at {position}");
                return false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(PlaceUnitAt), ex);
                return false;
            }
        }

        /// <summary>
        /// Removes a unit from its current hex position.
        /// </summary>
        /// <param name="map">Hex map</param>
        /// <param name="unitId">Unit identifier</param>
        /// <returns>True if removal successful, false otherwise</returns>
        public static bool RemoveUnitAt(HexMap map, string unitId)
        {
            try
            {
                // TODO: Implement unit removal logic
                // TODO: Find hex containing the unit
                // TODO: Clear occupancy information
                // TODO: Handle multi-unit hexes if stacking allowed

                AppService.CaptureUiMessage($"RemoveUnitAt stub: unit {unitId}");
                return false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(RemoveUnitAt), ex);
                return false;
            }
        }

        /// <summary>
        /// Moves a unit from one hex to another.
        /// </summary>
        /// <param name="map">Hex map</param>
        /// <param name="unitId">Unit identifier</param>
        /// <param name="newPosition">Target position</param>
        /// <returns>True if move successful, false otherwise</returns>
        public static bool MoveUnitTo(HexMap map, string unitId, Position2D newPosition)
        {
            try
            {
                // TODO: Implement unit movement logic
                // TODO: Validate movement rules and restrictions
                // TODO: Update both source and destination hexes
                // TODO: Handle movement costs and action consumption

                AppService.CaptureUiMessage($"MoveUnitTo stub: unit {unitId} to {newPosition}");
                return false;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(MoveUnitTo), ex);
                return false;
            }
        }

        /// <summary>
        /// Gets all hexes currently occupied by units.
        /// </summary>
        /// <param name="map">Hex map to search</param>
        /// <returns>List of hexes with units</returns>
        public static List<HexTile> GetOccupiedHexes(HexMap map)
        {
            try
            {
                // TODO: Implement occupied hex detection
                // TODO: Check each hex for unit presence
                // TODO: Return efficient collection for tactical queries

                AppService.CaptureUiMessage("GetOccupiedHexes stub called");
                return new List<HexTile>();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetOccupiedHexes), ex);
                return new List<HexTile>();
            }
        }

        /// <summary>
        /// Finds valid movement destinations for a unit within movement range.
        /// </summary>
        /// <param name="map">Hex map</param>
        /// <param name="unitPosition">Current unit position</param>
        /// <param name="movementPoints">Available movement points</param>
        /// <returns>List of valid destination hexes</returns>
        public static List<HexTile> GetValidMoveDestinations(HexMap map, Position2D unitPosition, int movementPoints)
        {
            try
            {
                // TODO: Implement movement range calculation
                // TODO: Consider terrain movement costs
                // TODO: Account for unit movement restrictions
                // TODO: Filter out blocked or occupied hexes

                AppService.CaptureUiMessage($"GetValidMoveDestinations stub: from {unitPosition}, {movementPoints} MP");
                return new List<HexTile>();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetValidMoveDestinations), ex);
                return new List<HexTile>();
            }
        }

        #endregion // Unit Placement Methods

        #region Validation Methods

        /// <summary>
        /// Validates that a coordinate is within the specified map bounds.
        /// </summary>
        /// <param name="position">Position to validate</param>
        /// <param name="mapSize">Map dimensions</param>
        /// <returns>True if position is valid, false otherwise</returns>
        public static bool IsValidPosition(Position2D position, Vector2Int mapSize)
        {
            try
            {
                return position.X >= 0 && position.X < mapSize.x &&
                       position.Y >= 0 && position.Y < mapSize.y;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsValidPosition), ex);
                return false;
            }
        }

        /// <summary>
        /// Clamps a position to be within map bounds.
        /// </summary>
        /// <param name="position">Position to clamp</param>
        /// <param name="mapSize">Map dimensions</param>
        /// <returns>Position clamped to map bounds</returns>
        public static Position2D ClampToMapBounds(Position2D position, Vector2Int mapSize)
        {
            try
            {
                return Position2D.Clamp(position,
                                        Position2D.Zero,
                                        new Position2D(mapSize.x - 1, mapSize.y - 1));
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ClampToMapBounds), ex);
                return position;
            }
        }

        #endregion // Validation Methods
    }
}