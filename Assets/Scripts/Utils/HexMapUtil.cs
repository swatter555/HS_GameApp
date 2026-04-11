using System;
using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core;
using HammerAndSickle.Services;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Result of a movement range BFS computation.
    /// </summary>
    public struct MovementRangeResult
    {
        /// <summary>Reachable hexes mapped to their accumulated MP cost.</summary>
        public Dictionary<Position2D, int> Reachable;

        /// <summary>Hexes where ZoC-to-ZoC halt rule would end movement.</summary>
        public HashSet<Position2D> ZocTerminals;
    }

    /// <summary>
    /// Static utility class for hex map operations including coordinate conversions,
    /// neighbor calculations, and tactical queries for the pointy-top, odd-r hex system.
    /// </summary>
    public static class HexMapUtil
    {
        #region Constants

        private const string CLASS_NAME = nameof(HexMapUtil);

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
        /// </summary>
        [Obsolete("Use HexGridSystem.Instance.HexToWorld() instead.")]
        public static Vector3 HexToWorldPosition(Position2D hexCoords)
        {
            return HexGridSystem.Instance.HexToWorld(hexCoords);
        }

        /// <summary>
        /// Converts Unity world position back to hex coordinates.
        /// </summary>
        [Obsolete("Use HexGridSystem.Instance.WorldToHex() instead.")]
        public static Position2D WorldPositionToHex(Vector3 worldPos)
        {
            return HexGridSystem.Instance.WorldToHex(worldPos);
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

        #region Tactical Query Methods

        /// <summary>
        /// Gets all hexes within a specified radius of a center position using ring iteration.
        /// </summary>
        public static List<HexTile> GetHexesInRadius(HexMap map, Position2D center, int radius)
        {
            try
            {
                var result = new List<HexTile>();
                if (map == null || radius <= 0) return result;

                // Use cube coordinate distance for accurate radius check
                foreach (var hex in map)
                {
                    if (hex == null) continue;
                    int dist = GetHexDistance(center, hex.Position);
                    if (dist > 0 && dist <= radius)
                        result.Add(hex);
                }

                return result;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexesInRadius), ex);
                return new List<HexTile>();
            }
        }

        /// <summary>
        /// Gets all hexes along a line between two positions using cube coordinate interpolation.
        /// </summary>
        public static List<HexTile> GetHexesInLine(HexMap map, Position2D start, Position2D end)
        {
            try
            {
                var result = new List<HexTile>();
                if (map == null) return result;

                int distance = GetHexDistance(start, end);
                if (distance == 0) return result;

                var cubeA = OffsetToCube(start);
                var cubeB = OffsetToCube(end);

                for (int i = 1; i <= distance; i++)
                {
                    float t = (float)i / distance;
                    // Lerp in cube space
                    float cx = cubeA.x + (cubeB.x - cubeA.x) * t;
                    float cy = cubeA.y + (cubeB.y - cubeA.y) * t;
                    float cz = cubeA.z + (cubeB.z - cubeA.z) * t;

                    // Round to nearest cube coordinate
                    var rounded = CubeRound(cx, cy, cz);
                    var offset = CubeToOffset(rounded);
                    var hex = map.GetHexAt(offset);
                    if (hex != null)
                        result.Add(hex);
                }

                return result;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexesInLine), ex);
                return new List<HexTile>();
            }
        }

        /// <summary>
        /// Computes valid movement destinations using Dijkstra BFS with terrain costs,
        /// occupancy filtering, river/bridge rules, road bonuses, and ZoC handling.
        /// </summary>
        public static MovementRangeResult GetValidMoveDestinations(HexMap map, CombatUnit unit)
        {
            var result = new MovementRangeResult
            {
                Reachable = new Dictionary<Position2D, int>(),
                ZocTerminals = new HashSet<Position2D>()
            };

            try
            {
                if (map == null || unit == null) return result;

                int maxMP = Mathf.RoundToInt(unit.MovementPoints.Current);
                if (maxMP <= 0) return result;

                bool isAir = unit.IsAirUnit || unit.IsHelicopter;
                var gdm = GameDataManager.Instance;

                // Build enemy ZoC set from spotted enemies (air units ignore ZoC)
                var enemyZocHexes = new HashSet<Position2D>();
                if (!isAir)
                {
                    foreach (var enemy in gdm.GetAIUnits())
                    {
                        if (enemy.SpottedLevel == SpottedLevel.Level0) continue;
                        if (!enemy.ProjectsZoC) continue;

                        var neighbors = GetAllNeighborPositions(enemy.MapPos);
                        foreach (var n in neighbors)
                            enemyZocHexes.Add(n);
                    }
                }

                // Dijkstra BFS with sorted list as priority queue
                var costs = new Dictionary<Position2D, int> { [unit.MapPos] = 0 };
                var frontier = new SortedList<int, List<Position2D>>();
                AddToFrontier(frontier, 0, unit.MapPos);

                // Track which hexes are ZoC-to-ZoC terminals
                var zocTerminals = new HashSet<Position2D>();

                while (frontier.Count > 0)
                {
                    // Dequeue lowest cost
                    int lowestCost = frontier.Keys[0];
                    var positions = frontier[lowestCost];
                    var current = positions[positions.Count - 1];
                    positions.RemoveAt(positions.Count - 1);
                    if (positions.Count == 0) frontier.RemoveAt(0);

                    int currentCost = costs[current];
                    if (currentCost > lowestCost) continue; // stale entry

                    // If this node is a ZoC terminal, don't expand further
                    if (zocTerminals.Contains(current)) continue;

                    bool currentInZoC = enemyZocHexes.Contains(current);
                    var currentTile = map.GetHexAt(current);
                    if (currentTile == null) continue;

                    for (int d = 0; d < 6; d++)
                    {
                        var dir = (HexDirection)d;
                        var neighborPos = GetNeighborPosition(current, dir);
                        var neighborTile = map.GetHexAt(neighborPos);
                        if (neighborTile == null) continue;

                        // Compute movement cost for this step
                        int stepCost = ComputeStepCost(unit, currentTile, neighborTile, dir, isAir);
                        if (stepCost < 0) continue; // blocked

                        int totalCost = currentCost + stepCost;
                        if (totalCost > maxMP) continue;

                        // Occupancy filtering
                        if (!isAir)
                        {
                            // Enemy ground unit blocks entry (spotted only)
                            if (gdm.IsHexOccupiedByEnemyGround(neighborPos, unit.Side))
                                continue;
                        }
                        else
                        {
                            // Air units cannot stop on hex with enemy air
                            // (but can pass through — handled at final filter)
                        }

                        if (!costs.ContainsKey(neighborPos) || totalCost < costs[neighborPos])
                        {
                            costs[neighborPos] = totalCost;
                            AddToFrontier(frontier, totalCost, neighborPos);

                            // ZoC-to-ZoC: if we're in ZoC and neighbor is also in ZoC, mark terminal
                            if (!isAir && currentInZoC && enemyZocHexes.Contains(neighborPos))
                                zocTerminals.Add(neighborPos);
                        }
                    }
                }

                // Build final result: exclude start position and hexes blocked by occupancy
                foreach (var kvp in costs)
                {
                    if (kvp.Key == unit.MapPos) continue;

                    if (!isAir)
                    {
                        // Cannot stop on friendly ground unit
                        if (gdm.IsHexOccupiedByFriendlyGround(kvp.Key, unit.Side))
                            continue;
                    }
                    else
                    {
                        // Air cannot stop on enemy air
                        var enemyAir = gdm.GetAirUnitAtHex(kvp.Key);
                        if (enemyAir != null && enemyAir.Side != unit.Side)
                            continue;
                    }

                    result.Reachable[kvp.Key] = kvp.Value;
                }

                result.ZocTerminals = zocTerminals;
                return result;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetValidMoveDestinations), ex);
                return result;
            }
        }

        /// <summary>
        /// A* pathfinding using the same cost rules as the BFS.
        /// Returns the optimal path (start exclusive, destination inclusive).
        /// </summary>
        public static List<HexTile> FindPath(HexMap map, CombatUnit unit, Position2D start, Position2D end)
        {
            try
            {
                if (map == null || unit == null) return new List<HexTile>();

                bool isAir = unit.IsAirUnit || unit.IsHelicopter;

                var cameFrom = new Dictionary<Position2D, Position2D>();
                var gScore = new Dictionary<Position2D, int> { [start] = 0 };
                var fScore = new Dictionary<Position2D, int> { [start] = GetHexDistance(start, end) };

                var openSet = new SortedList<int, List<Position2D>>();
                AddToFrontier(openSet, fScore[start], start);
                var inOpen = new HashSet<Position2D> { start };

                while (openSet.Count > 0)
                {
                    int lowestF = openSet.Keys[0];
                    var positions = openSet[lowestF];
                    var current = positions[positions.Count - 1];
                    positions.RemoveAt(positions.Count - 1);
                    if (positions.Count == 0) openSet.RemoveAt(0);
                    inOpen.Remove(current);

                    if (current == end)
                        return ReconstructPath(map, cameFrom, current, start);

                    var currentTile = map.GetHexAt(current);
                    if (currentTile == null) continue;

                    for (int d = 0; d < 6; d++)
                    {
                        var dir = (HexDirection)d;
                        var neighborPos = GetNeighborPosition(current, dir);
                        var neighborTile = map.GetHexAt(neighborPos);
                        if (neighborTile == null) continue;

                        int stepCost = ComputeStepCost(unit, currentTile, neighborTile, dir, isAir);
                        if (stepCost < 0) continue;

                        int tentativeG = gScore[current] + stepCost;

                        if (!gScore.ContainsKey(neighborPos) || tentativeG < gScore[neighborPos])
                        {
                            cameFrom[neighborPos] = current;
                            gScore[neighborPos] = tentativeG;
                            int f = tentativeG + GetHexDistance(neighborPos, end);
                            fScore[neighborPos] = f;

                            if (!inOpen.Contains(neighborPos))
                            {
                                AddToFrontier(openSet, f, neighborPos);
                                inOpen.Add(neighborPos);
                            }
                        }
                    }
                }

                return new List<HexTile>(); // no path found
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(FindPath), ex);
                return new List<HexTile>();
            }
        }

        #endregion // Tactical Query Methods

        #region Unit Placement Methods

        /// <summary>
        /// Places a unit at the specified hex position.
        /// </summary>
        public static bool PlaceUnitAt(HexMap map, CombatUnit unit, Position2D position)
        {
            try
            {
                if (map == null || unit == null) return false;
                if (map.GetHexAt(position) == null) return false;

                unit.MapPos = position;
                GameDataManager.Instance.InvalidateOccupancy();
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(PlaceUnitAt), ex);
                return false;
            }
        }

        /// <summary>
        /// Removes a unit from its current hex position (sets to zero).
        /// </summary>
        public static bool RemoveUnitAt(HexMap map, CombatUnit unit)
        {
            try
            {
                if (map == null || unit == null) return false;

                unit.MapPos = Position2D.Zero;
                GameDataManager.Instance.InvalidateOccupancy();
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(RemoveUnitAt), ex);
                return false;
            }
        }

        /// <summary>
        /// Moves a unit to a new position and sets facing toward the destination.
        /// </summary>
        public static bool MoveUnitTo(HexMap map, CombatUnit unit, Position2D newPosition)
        {
            try
            {
                if (map == null || unit == null) return false;
                if (map.GetHexAt(newPosition) == null) return false;

                Position2D oldPos = unit.MapPos;
                unit.MapPos = newPosition;

                // Set facing toward destination
                var facingDir = GetDirectionBetween(oldPos, newPosition);
                if (facingDir.HasValue)
                    unit.Facing = facingDir.Value;

                GameDataManager.Instance.InvalidateOccupancy();
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(MoveUnitTo), ex);
                return false;
            }
        }

        #endregion // Unit Placement Methods

        #region Pathfinding Helpers

        /// <summary>
        /// Computes the MP cost for a single step from currentTile to neighborTile.
        /// Returns -1 if the step is blocked.
        /// </summary>
        private static int ComputeStepCost(CombatUnit unit, HexTile currentTile, HexTile neighborTile,
            HexDirection direction, bool isAir)
        {
            // Air units: flat 1 MP per hex, ignore all terrain and rivers
            if (isAir) return 1;

            // Ground: impassable blocks entry
            if (neighborTile.Terrain == TerrainType.Impassable) return -1;

            // Ground: water blocks entry (amphibious crossing handled separately by MovementController)
            if (neighborTile.Terrain == TerrainType.Water) return -1;

            // River border check: both directions must agree
            bool hasRiver = HasRiverBetween(currentTile, neighborTile, direction);
            if (hasRiver)
            {
                bool hasBridge = HasBridgeBetween(currentTile, neighborTile, direction);
                bool hasDestroyedBridge = HasDestroyedBridgeBetween(currentTile, neighborTile, direction);

                if (hasDestroyedBridge && !hasBridge) return -1; // destroyed bridge blocks
                if (!hasBridge) return -1; // no bridge, blocked for non-amphibious ground
            }

            int baseCost = neighborTile.MovementCost;
            if (baseCost <= 0) return -1; // safety for impassable

            // Road bonus: road-to-road halves cost, floor, min 1
            if (currentTile.IsRoad && neighborTile.IsRoad)
                baseCost = Math.Max(1, baseCost / 2);

            // TODO: Rail movement not implemented. Policy undecided.

            return baseCost;
        }

        /// <summary>
        /// Checks if there is a river border between two adjacent hexes (cross-checks both sides).
        /// </summary>
        private static bool HasRiverBetween(HexTile from, HexTile to, HexDirection direction)
        {
            bool fromSide = from.RiverBorders != null && from.RiverBorders.GetBorder(direction);
            var opposite = GetOppositeDirection(direction);
            bool toSide = to.RiverBorders != null && to.RiverBorders.GetBorder(opposite);
            return fromSide || toSide;
        }

        /// <summary>
        /// Checks if there is a bridge (regular or pontoon) between two adjacent hexes.
        /// </summary>
        private static bool HasBridgeBetween(HexTile from, HexTile to, HexDirection direction)
        {
            var opposite = GetOppositeDirection(direction);

            bool fromBridge = (from.BridgeBorders != null && from.BridgeBorders.GetBorder(direction))
                           || (from.PontoonBridgeBorders != null && from.PontoonBridgeBorders.GetBorder(direction));
            bool toBridge = (to.BridgeBorders != null && to.BridgeBorders.GetBorder(opposite))
                         || (to.PontoonBridgeBorders != null && to.PontoonBridgeBorders.GetBorder(opposite));
            return fromBridge || toBridge;
        }

        /// <summary>
        /// Checks if there is a destroyed bridge between two adjacent hexes.
        /// </summary>
        private static bool HasDestroyedBridgeBetween(HexTile from, HexTile to, HexDirection direction)
        {
            var opposite = GetOppositeDirection(direction);

            bool fromDamaged = from.DamagedBridgeBorders != null && from.DamagedBridgeBorders.GetBorder(direction);
            bool toDamaged = to.DamagedBridgeBorders != null && to.DamagedBridgeBorders.GetBorder(opposite);
            return fromDamaged || toDamaged;
        }

        /// <summary>
        /// Determines which HexDirection goes from position a to adjacent position b.
        /// Returns null if b is not adjacent to a.
        /// </summary>
        public static HexDirection? GetDirectionBetween(Position2D a, Position2D b)
        {
            for (int d = 0; d < 6; d++)
            {
                var dir = (HexDirection)d;
                if (GetNeighborPosition(a, dir) == b)
                    return dir;
            }
            return null;
        }

        private static void AddToFrontier(SortedList<int, List<Position2D>> frontier, int cost, Position2D pos)
        {
            if (!frontier.TryGetValue(cost, out var list))
            {
                list = new List<Position2D>();
                frontier[cost] = list;
            }
            list.Add(pos);
        }

        private static List<HexTile> ReconstructPath(HexMap map, Dictionary<Position2D, Position2D> cameFrom,
            Position2D current, Position2D start)
        {
            var path = new List<HexTile>();
            while (current != start)
            {
                var tile = map.GetHexAt(current);
                if (tile != null) path.Add(tile);
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Rounds fractional cube coordinates to the nearest valid cube hex.
        /// </summary>
        private static Vector3Int CubeRound(float x, float y, float z)
        {
            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);

            float xDiff = Mathf.Abs(rx - x);
            float yDiff = Mathf.Abs(ry - y);
            float zDiff = Mathf.Abs(rz - z);

            if (xDiff > yDiff && xDiff > zDiff)
                rx = -ry - rz;
            else if (yDiff > zDiff)
                ry = -rx - rz;
            else
                rz = -rx - ry;

            return new Vector3Int(rx, ry, rz);
        }

        /// <summary>
        /// Converts cube coordinates back to odd-r offset coordinates.
        /// </summary>
        private static Position2D CubeToOffset(Vector3Int cube)
        {
            int col = cube.x + (cube.z - (cube.z & 1)) / 2;
            int row = cube.z;
            return new Position2D(col, row);
        }

        #endregion // Pathfinding Helpers

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