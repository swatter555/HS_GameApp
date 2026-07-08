using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using HammerAndSickle.Utils;

namespace HammerAndSickle.Models.AI
{
    /// <summary>A bridged river crossing — always a chokepoint edge (AI-Design-Supplement Part 4.3).</summary>
    public struct BridgeCrossing
    {
        public Position2D A;
        public Position2D B;
    }

    /// <summary>
    /// Chokepoint analysis over the ground-traversable graph (AI-Design-Supplement Part 4.3):
    /// articulation hexes (cut vertices whose loss disconnects local mobility — passes, isthmuses,
    /// bridgeheads) plus every intact bridge crossing. Consumed by avenue rating, defensive-trace
    /// anchoring, and ambush-site ranking. Rebuild with BoardAnalysis when bridge state changes.
    /// </summary>
    public sealed class ChokepointAnalysis
    {
        private const string CLASS_NAME = nameof(ChokepointAnalysis);

        public HashSet<Position2D> ArticulationHexes = new HashSet<Position2D>();
        public List<BridgeCrossing> BridgeCrossings = new List<BridgeCrossing>();

        #region Build

        public static ChokepointAnalysis Build(HexMap map)
        {
            var result = new ChokepointAnalysis();
            try
            {
                if (map == null) throw new ArgumentNullException(nameof(map));

                // Node set: every ground-enterable hex (Water/Impassable are barriers, not nodes).
                var nodes = new List<HexTile>();
                foreach (HexTile t in map)
                    if (t != null && t.Terrain != TerrainType.Water && t.Terrain != TerrainType.Impassable)
                        nodes.Add(t);
                nodes.Sort((a, b) =>
                {
                    int y = ((int)a.Position.Y).CompareTo((int)b.Position.Y);
                    return y != 0 ? y : ((int)a.Position.X).CompareTo((int)b.Position.X);
                });

                FindArticulationHexes(map, nodes, result.ArticulationHexes);
                FindBridgeCrossings(map, nodes, result.BridgeCrossings);
                return result;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Build), e);
                return result;
            }
        }

        /// <summary>Classic articulation-point DFS (Hopcroft–Tarjan) over ground-traversable adjacency.</summary>
        private static void FindArticulationHexes(HexMap map, List<HexTile> nodes, HashSet<Position2D> result)
        {
            var disc = new Dictionary<Position2D, int>();
            var low = new Dictionary<Position2D, int>();
            int time = 0;

            foreach (HexTile root in nodes)
            {
                if (disc.ContainsKey(root.Position)) continue;
                int rootChildren = 0;
                Dfs(root, null, isRoot: true, ref rootChildren);
                if (rootChildren > 1) result.Add(root.Position); // root rule
            }

            void Dfs(HexTile tile, Position2D? parent, bool isRoot, ref int rootChildren)
            {
                disc[tile.Position] = low[tile.Position] = ++time;

                for (int d = 0; d < 6; d++)
                {
                    var dir = (HexDirection)d;
                    HexTile next = map.GetHexAt(HexMapUtil.GetNeighborPosition(tile.Position, dir));
                    if (next == null) continue;
                    if (MobilityMap.GroundStepCost(tile, next, dir) < 0) continue;

                    if (!disc.ContainsKey(next.Position))
                    {
                        if (isRoot) rootChildren++;
                        int dummy = 0;
                        Dfs(next, tile.Position, isRoot: false, ref dummy);

                        low[tile.Position] = Math.Min(low[tile.Position], low[next.Position]);
                        if (!isRoot && low[next.Position] >= disc[tile.Position])
                            result.Add(tile.Position);
                    }
                    else if (!(parent.HasValue && next.Position.Equals(parent.Value)))
                    {
                        low[tile.Position] = Math.Min(low[tile.Position], disc[next.Position]);
                    }
                }
            }
        }

        private static void FindBridgeCrossings(HexMap map, List<HexTile> nodes, List<BridgeCrossing> result)
        {
            // NE/E/SE cover each undirected pair exactly once.
            HexDirection[] half = { HexDirection.NE, HexDirection.E, HexDirection.SE };
            foreach (HexTile tile in nodes)
            {
                foreach (HexDirection dir in half)
                {
                    HexTile next = map.GetHexAt(HexMapUtil.GetNeighborPosition(tile.Position, dir));
                    if (next == null) continue;
                    if (MobilityMap.EdgeHasRiver(tile, next, dir) && MobilityMap.EdgeHasBridge(tile, next, dir))
                        result.Add(new BridgeCrossing { A = tile.Position, B = next.Position });
                }
            }
        }

        #endregion // Build
    }
}
