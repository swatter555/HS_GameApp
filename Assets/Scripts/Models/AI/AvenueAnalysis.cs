using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using HammerAndSickle.Utils;

namespace HammerAndSickle.Models.AI
{
    /// <summary>One avenue of approach — a cheap ground corridor from a source area to a target area.</summary>
    public sealed class Avenue
    {
        public int Id;
        public Position2D Source;                 // the source hex the cheapest path started from
        public Position2D Target;                 // the target hex it reached
        public List<Position2D> Path = new List<Position2D>(); // source → target inclusive
        public int PathCost;                      // MP cost along the path (unpenalized)
        public double CoverFraction;              // fraction of path hexes with any terrain block (§7.5.6.2)
    }

    /// <summary>
    /// Avenues of approach (AI-Design-Supplement Part 4.4): k diverse cheapest ground corridors from a
    /// source set (force concentrations, deployment zones, map-edge entries) to a target set (objective
    /// hexes). Diversity via iterative path penalties — each found path adds a flat per-hex surcharge,
    /// pushing later searches onto genuinely different corridors; identical repeats stop the search.
    /// Defense reads the enemy's avenues as its threat template; offense and raids read its own.
    /// AD-exposure rating joins at L2 (needs live unit data) — L1 rates cost and cover only.
    /// </summary>
    public static class AvenueAnalysis
    {
        private const string CLASS_NAME = nameof(AvenueAnalysis);

        /// <summary>Flat per-hex surcharge added to hexes of already-found avenues (diversity pressure).</summary>
        public const int REUSE_PENALTY = 2;

        public static List<Avenue> FindAvenues(
            HexMap map, IEnumerable<Position2D> sources, IEnumerable<Position2D> targets, int k = 3)
        {
            var avenues = new List<Avenue>();
            try
            {
                if (map == null) throw new ArgumentNullException(nameof(map));

                var sourceList = (sources ?? throw new ArgumentNullException(nameof(sources)))
                    .Where(p => map.GetHexAt(p) != null).ToList();
                var targetSet = new HashSet<Position2D>(
                    (targets ?? throw new ArgumentNullException(nameof(targets)))
                    .Where(p => map.GetHexAt(p) != null));
                if (sourceList.Count == 0 || targetSet.Count == 0) return avenues;

                var penalties = new Dictionary<Position2D, int>();
                for (int i = 0; i < k; i++)
                {
                    List<Position2D> path = CheapestPath(map, sourceList, targetSet, penalties, out int trueCost);
                    if (path == null) break;
                    if (avenues.Any(a => a.Path.SequenceEqual(path))) break; // penalties exhausted the alternatives

                    var avenue = new Avenue
                    {
                        Id = avenues.Count,
                        Source = path[0],
                        Target = path[path.Count - 1],
                        Path = path,
                        PathCost = trueCost,
                        CoverFraction = path.Count == 0 ? 0.0 :
                            path.Count(p => CombatMath.BlockTier(map.GetHexAt(p).Terrain) != TerrainBlockTier.None)
                            / (double)path.Count,
                    };
                    avenues.Add(avenue);

                    foreach (Position2D pos in path)
                    {
                        penalties.TryGetValue(pos, out int pen);
                        penalties[pos] = pen + REUSE_PENALTY;
                    }
                }
                return avenues;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(FindAvenues), e);
                return avenues;
            }
        }

        /// <summary>
        /// Multi-source Dijkstra to the first-reached target under (step cost + reuse penalty);
        /// returns the path (source → target) and its TRUE (unpenalized) cost, or null if unreachable.
        /// </summary>
        private static List<Position2D> CheapestPath(
            HexMap map, List<Position2D> sources, HashSet<Position2D> targets,
            Dictionary<Position2D, int> penalties, out int trueCost)
        {
            trueCost = 0;
            var best = new Dictionary<Position2D, int>();
            var parent = new Dictionary<Position2D, Position2D>();
            var frontier = new SortedList<int, List<Position2D>>();

            foreach (Position2D src in sources)
            {
                best[src] = 0;
                Enqueue(frontier, 0, src);
            }

            var visited = new HashSet<Position2D>();
            while (frontier.Count > 0)
            {
                int cost = frontier.Keys[0];
                List<Position2D> bucket = frontier.Values[0];
                Position2D current = bucket[bucket.Count - 1];
                bucket.RemoveAt(bucket.Count - 1);
                if (bucket.Count == 0) frontier.RemoveAt(0);

                if (visited.Contains(current)) continue;
                visited.Add(current);

                if (targets.Contains(current))
                {
                    // Rebuild the path, then price it without penalties.
                    var path = new List<Position2D> { current };
                    Position2D walk = current;
                    while (parent.TryGetValue(walk, out Position2D prev))
                    {
                        path.Add(prev);
                        walk = prev;
                    }
                    path.Reverse();

                    for (int i = 1; i < path.Count; i++)
                    {
                        HexTile a = map.GetHexAt(path[i - 1]);
                        HexTile b = map.GetHexAt(path[i]);
                        HexDirection? dir = HexMapUtil.GetDirectionBetween(path[i - 1], path[i]);
                        if (dir.HasValue) trueCost += Math.Max(0, MobilityMap.GroundStepCost(a, b, dir.Value));
                    }
                    return path;
                }

                HexTile currentTile = map.GetHexAt(current);
                if (currentTile == null) continue;

                for (int d = 0; d < 6; d++)
                {
                    var dir = (HexDirection)d;
                    Position2D next = HexMapUtil.GetNeighborPosition(current, dir);
                    HexTile nextTile = map.GetHexAt(next);
                    if (nextTile == null) continue;

                    int step = MobilityMap.GroundStepCost(currentTile, nextTile, dir);
                    if (step < 0) continue;

                    penalties.TryGetValue(next, out int pen);
                    int total = cost + step + pen;
                    if (!best.TryGetValue(next, out int known) || total < known)
                    {
                        best[next] = total;
                        parent[next] = current;
                        Enqueue(frontier, total, next);
                    }
                }
            }
            return null;
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
    }
}
