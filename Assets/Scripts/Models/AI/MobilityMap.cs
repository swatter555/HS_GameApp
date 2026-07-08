using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using HammerAndSickle.Utils;

namespace HammerAndSickle.Models.AI
{
    /// <summary>
    /// Unit-agnostic ground mobility over a HexMap (AI-Design-Supplement Part 4.1) — the shared cost
    /// basis for region edges, chokepoints, avenues, and travel-time estimates. Air movement needs no
    /// field (flat 1 MP/hex, §5.13.1 — plain hex distance suffices).
    /// ⚠ DRIFT NOTE: GroundStepCost mirrors the terrain/river/road rules of HexMapUtil.ComputeStepCost
    /// (§4.3/§4.4/§5.2) MINUS the unit-dependent parts (ZoC, occupancy, embarked states) — keep in sync
    /// if the movement rules change.
    /// </summary>
    public static class MobilityMap
    {
        private const string CLASS_NAME = nameof(MobilityMap);

        #region Edge Costs & Features

        /// <summary>
        /// MP cost for a ground unit to step from → to (adjacent, via <paramref name="direction"/>);
        /// −1 = blocked (impassable/water target, or an unbridged/destroyed river edge, §4.4).
        /// </summary>
        public static int GroundStepCost(HexTile from, HexTile to, HexDirection direction)
        {
            if (from == null || to == null) return -1;
            if (to.Terrain == TerrainType.Impassable || to.Terrain == TerrainType.Water) return -1;

            if (EdgeHasRiver(from, to, direction) && !EdgeHasBridge(from, to, direction))
                return -1; // unbridged (or destroyed-bridge) river edge blocks ground movement

            int baseCost = to.MovementCost;
            if (baseCost <= 0) return -1;

            if (from.IsRoad && to.IsRoad)
                baseCost = Math.Max(1, baseCost / 2); // §5.2.1 road-to-road, floor, min 1

            return baseCost;
        }

        /// <summary>True if a river runs along this edge (either side's border data, §4.4).</summary>
        public static bool EdgeHasRiver(HexTile from, HexTile to, HexDirection direction)
        {
            HexDirection opposite = HexMapUtil.GetOppositeDirection(direction);
            return (from?.RiverBorders != null && from.RiverBorders.GetBorder(direction))
                || (to?.RiverBorders != null && to.RiverBorders.GetBorder(opposite));
        }

        /// <summary>True if an intact bridge (regular or pontoon) spans this edge (§4.4.1–.2).</summary>
        public static bool EdgeHasBridge(HexTile from, HexTile to, HexDirection direction)
        {
            HexDirection opposite = HexMapUtil.GetOppositeDirection(direction);
            bool fromSide = (from?.BridgeBorders != null && from.BridgeBorders.GetBorder(direction))
                         || (from?.PontoonBridgeBorders != null && from.PontoonBridgeBorders.GetBorder(direction));
            bool toSide = (to?.BridgeBorders != null && to.BridgeBorders.GetBorder(opposite))
                       || (to?.PontoonBridgeBorders != null && to.PontoonBridgeBorders.GetBorder(opposite));
            return fromSide || toSide;
        }

        #endregion // Edge Costs & Features

        #region Distance Fields

        /// <summary>
        /// Multi-source Dijkstra over ground step costs: cheapest MP cost to reach every ground-traversable
        /// hex from the nearest source (sources cost 0). The workhorse behind avenue extraction, travel-time
        /// bids, and supply-side reachability estimates.
        /// </summary>
        public static Dictionary<Position2D, int> GroundDistanceField(HexMap map, IEnumerable<Position2D> sources)
        {
            var costs = new Dictionary<Position2D, int>();
            try
            {
                if (map == null) throw new ArgumentNullException(nameof(map));
                if (sources == null) throw new ArgumentNullException(nameof(sources));

                var frontier = new SortedList<int, List<Position2D>>();
                foreach (Position2D src in sources)
                {
                    if (map.GetHexAt(src) == null) continue;
                    costs[src] = 0;
                    Enqueue(frontier, 0, src);
                }

                while (frontier.Count > 0)
                {
                    int cost = frontier.Keys[0];
                    List<Position2D> bucket = frontier.Values[0];
                    Position2D current = bucket[bucket.Count - 1];
                    bucket.RemoveAt(bucket.Count - 1);
                    if (bucket.Count == 0) frontier.RemoveAt(0);

                    if (costs[current] < cost) continue; // stale entry
                    HexTile currentTile = map.GetHexAt(current);
                    if (currentTile == null) continue;

                    for (int d = 0; d < 6; d++)
                    {
                        var dir = (HexDirection)d;
                        Position2D next = HexMapUtil.GetNeighborPosition(current, dir);
                        HexTile nextTile = map.GetHexAt(next);
                        if (nextTile == null) continue;

                        int step = GroundStepCost(currentTile, nextTile, dir);
                        if (step < 0) continue;

                        int total = cost + step;
                        if (!costs.TryGetValue(next, out int known) || total < known)
                        {
                            costs[next] = total;
                            Enqueue(frontier, total, next);
                        }
                    }
                }
                return costs;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GroundDistanceField), e);
                return costs;
            }
        }

        private static void Enqueue(SortedList<int, List<Position2D>> frontier, int cost, Position2D pos)
        {
            if (!frontier.TryGetValue(cost, out List<Position2D> bucket))
            {
                bucket = new List<Position2D>();
                frontier.Add(cost, bucket);
            }
            bucket.Add(pos);
        }

        #endregion // Distance Fields
    }
}
