using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using HammerAndSickle.Utils;

namespace HammerAndSickle.Models.AI
{
    /// <summary>Terrain family a region is built from (AI-Design-Supplement Part 4.2).</summary>
    public enum RegionTerrainClass
    {
        Open,     // Clear
        Broken,   // Forest, Rough, Marsh
        Mountain, // Mountains
        Urban     // MinorCity, MajorCity
    }

    /// <summary>One region of the abstracted board — a contiguous, same-terrain-class patch of ground.</summary>
    public sealed class Region
    {
        public int Id;
        public RegionTerrainClass TerrainClass;
        public List<Position2D> Hexes = new List<Position2D>();

        /// <summary>Representative hex (nearest to the centroid) — stable anchor for .aii references and debug labels.</summary>
        public Position2D Seed;

        public int ObjectiveCount;
        public float ObjectiveValue;   // Σ VictoryValue over IsObjective hexes
        public int RoadHexes;
        public bool HasFort;
        public bool HasAirbaseSite;    // isAirbase-flagged hexes persist as sites (§35.4.5)
        public bool HasPort;
    }

    /// <summary>Adjacency between two regions, with the tactical texture of the boundary.</summary>
    public sealed class RegionEdge
    {
        public int RegionA;            // lower id
        public int RegionB;            // higher id
        public int ConnectionWidth;    // ground-traversable adjacent hex pairs across the boundary
        public int RiverPairs;         // boundary pairs carrying a river edge (bridged or not)
        public int BridgePairs;        // boundary pairs with an intact bridge
        public bool RoadLink;          // any traversable road-to-road pair crosses here
    }

    /// <summary>
    /// The region abstraction of a HexMap (AI-Design-Supplement Part 4.2): passable hexes are segmented
    /// into contiguous same-terrain-class regions (flood fill over ground-traversable adjacency, so an
    /// unbridged river splits a plain into two banks — emergent), undersized fragments are merged into
    /// their widest-bordering neighbor, and region adjacency is annotated with connection width and
    /// river/bridge/road texture. ~15–30 regions on a full map; L3 reasons over this graph, not hexes.
    /// </summary>
    public sealed class RegionGraph
    {
        private const string CLASS_NAME = nameof(RegionGraph);

        public List<Region> Regions = new List<Region>();
        public List<RegionEdge> Edges = new List<RegionEdge>();
        public Dictionary<Position2D, int> RegionOf = new Dictionary<Position2D, int>();

        /// <summary>Edges touching a region.</summary>
        public IEnumerable<RegionEdge> EdgesOf(int regionId) =>
            Edges.Where(e => e.RegionA == regionId || e.RegionB == regionId);

        #region Build

        public static RegionGraph Build(HexMap map, int minRegionSize = 3)
        {
            var graph = new RegionGraph();
            try
            {
                if (map == null) throw new ArgumentNullException(nameof(map));

                // Deterministic tile order (row-major) so region ids are stable run-to-run.
                var tiles = new List<HexTile>();
                foreach (HexTile t in map)
                    if (t != null && Classify(t.Terrain).HasValue)
                        tiles.Add(t);
                tiles.Sort((a, b) =>
                {
                    int y = ((int)a.Position.Y).CompareTo((int)b.Position.Y);
                    return y != 0 ? y : ((int)a.Position.X).CompareTo((int)b.Position.X);
                });

                // 1. Flood fill: same class + ground-traversable adjacency.
                var regionOf = new Dictionary<Position2D, int>();
                int nextId = 0;
                foreach (HexTile tile in tiles)
                {
                    if (regionOf.ContainsKey(tile.Position)) continue;
                    FloodFill(map, tile, nextId++, regionOf);
                }

                // 2. Merge undersized fragments into the neighbor sharing the widest traversable border.
                MergeSmallRegions(map, regionOf, minRegionSize);

                // 3. Compact ids, build Region objects + metadata, then edges.
                graph.RegionOf = Compact(regionOf);
                BuildRegions(map, graph);
                BuildEdges(map, graph);
                return graph;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Build), e);
                return graph;
            }
        }

        private static RegionTerrainClass? Classify(TerrainType t) => t switch
        {
            TerrainType.Clear     => RegionTerrainClass.Open,
            TerrainType.Forest    => RegionTerrainClass.Broken,
            TerrainType.Rough     => RegionTerrainClass.Broken,
            TerrainType.Marsh     => RegionTerrainClass.Broken,
            TerrainType.Mountains => RegionTerrainClass.Mountain,
            TerrainType.MinorCity => RegionTerrainClass.Urban,
            TerrainType.MajorCity => RegionTerrainClass.Urban,
            _                     => (RegionTerrainClass?)null, // Water, Impassable — barriers, not regions
        };

        private static void FloodFill(HexMap map, HexTile start, int id, Dictionary<Position2D, int> regionOf)
        {
            RegionTerrainClass cls = Classify(start.Terrain).Value;
            var queue = new Queue<HexTile>();
            queue.Enqueue(start);
            regionOf[start.Position] = id;

            while (queue.Count > 0)
            {
                HexTile current = queue.Dequeue();
                for (int d = 0; d < 6; d++)
                {
                    var dir = (HexDirection)d;
                    HexTile next = map.GetHexAt(HexMapUtil.GetNeighborPosition(current.Position, dir));
                    if (next == null || regionOf.ContainsKey(next.Position)) continue;
                    if (Classify(next.Terrain) != cls) continue;
                    if (MobilityMap.GroundStepCost(current, next, dir) < 0) continue; // river banks split
                    regionOf[next.Position] = id;
                    queue.Enqueue(next);
                }
            }
        }

        private static void MergeSmallRegions(HexMap map, Dictionary<Position2D, int> regionOf, int minSize)
        {
            var unmergeable = new HashSet<int>();
            for (int guard = 0; guard < 1024; guard++)
            {
                // Smallest undersized region first.
                var sizes = regionOf.GroupBy(kv => kv.Value).ToDictionary(g => g.Key, g => g.Count());
                int target = -1;
                foreach (KeyValuePair<int, int> kv in sizes.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key))
                {
                    if (kv.Value >= minSize) break;
                    if (unmergeable.Contains(kv.Key)) continue;
                    target = kv.Key;
                    break;
                }
                if (target < 0) return;

                // Count traversable border pairs per neighboring region.
                var borderPairs = new Dictionary<int, int>();
                foreach (KeyValuePair<Position2D, int> kv in regionOf.Where(kv => kv.Value == target))
                {
                    HexTile tile = map.GetHexAt(kv.Key);
                    for (int d = 0; d < 6; d++)
                    {
                        var dir = (HexDirection)d;
                        HexTile next = map.GetHexAt(HexMapUtil.GetNeighborPosition(kv.Key, dir));
                        if (next == null || !regionOf.TryGetValue(next.Position, out int other) || other == target) continue;
                        if (MobilityMap.GroundStepCost(tile, next, dir) < 0) continue;
                        borderPairs.TryGetValue(other, out int n);
                        borderPairs[other] = n + 1;
                    }
                }

                if (borderPairs.Count == 0)
                {
                    unmergeable.Add(target); // isolated pocket — legitimate tiny region
                    continue;
                }

                int absorber = borderPairs.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
                foreach (Position2D pos in regionOf.Where(kv => kv.Value == target).Select(kv => kv.Key).ToList())
                    regionOf[pos] = absorber;
            }
        }

        private static Dictionary<Position2D, int> Compact(Dictionary<Position2D, int> regionOf)
        {
            var remap = new Dictionary<int, int>();
            var result = new Dictionary<Position2D, int>(regionOf.Count);
            foreach (KeyValuePair<Position2D, int> kv in regionOf.OrderBy(kv => kv.Value))
            {
                if (!remap.TryGetValue(kv.Value, out int compactId))
                {
                    compactId = remap.Count;
                    remap[kv.Value] = compactId;
                }
                result[kv.Key] = compactId;
            }
            return result;
        }

        private static void BuildRegions(HexMap map, RegionGraph graph)
        {
            int count = graph.RegionOf.Count == 0 ? 0 : graph.RegionOf.Values.Max() + 1;
            var classVotes = new Dictionary<RegionTerrainClass, int>[count];
            for (int i = 0; i < count; i++)
            {
                graph.Regions.Add(new Region { Id = i });
                classVotes[i] = new Dictionary<RegionTerrainClass, int>();
            }

            foreach (KeyValuePair<Position2D, int> kv in graph.RegionOf)
            {
                Region region = graph.Regions[kv.Value];
                HexTile tile = map.GetHexAt(kv.Key);
                region.Hexes.Add(kv.Key);

                RegionTerrainClass cls = Classify(tile.Terrain).Value;
                classVotes[kv.Value].TryGetValue(cls, out int votes);
                classVotes[kv.Value][cls] = votes + 1;

                if (tile.IsObjective) { region.ObjectiveCount++; region.ObjectiveValue += tile.VictoryValue; }
                if (tile.IsRoad) region.RoadHexes++;
                if (tile.IsFort) region.HasFort = true;
                if (tile.IsAirbase) region.HasAirbaseSite = true;
                if (tile.IsPort) region.HasPort = true;
            }

            // Majority terrain class (deterministic: ties break toward the lower enum value) —
            // merged fragments must not flip a region's class by iteration order.
            for (int i = 0; i < count; i++)
            {
                if (classVotes[i].Count == 0) continue;
                graph.Regions[i].TerrainClass = classVotes[i]
                    .OrderByDescending(kv => kv.Value).ThenBy(kv => (int)kv.Key)
                    .First().Key;
            }

            // Seed = region hex nearest the coordinate average.
            foreach (Region region in graph.Regions)
            {
                if (region.Hexes.Count == 0) continue;
                float ax = region.Hexes.Average(p => p.X);
                float ay = region.Hexes.Average(p => p.Y);
                var avg = new Position2D((float)Math.Round(ax), (float)Math.Round(ay));
                region.Seed = region.Hexes
                    .OrderBy(p => HexMapUtil.GetHexDistance(p, avg))
                    .ThenBy(p => (int)p.Y).ThenBy(p => (int)p.X)
                    .First();
            }
        }

        private static void BuildEdges(HexMap map, RegionGraph graph)
        {
            // NE/E/SE cover each undirected hex pair exactly once (their opposites are SW/W/NW).
            HexDirection[] half = { HexDirection.NE, HexDirection.E, HexDirection.SE };
            var acc = new Dictionary<(int, int), RegionEdge>();

            foreach (KeyValuePair<Position2D, int> kv in graph.RegionOf)
            {
                HexTile tile = map.GetHexAt(kv.Key);
                foreach (HexDirection dir in half)
                {
                    HexTile next = map.GetHexAt(HexMapUtil.GetNeighborPosition(kv.Key, dir));
                    if (next == null || !graph.RegionOf.TryGetValue(next.Position, out int other)) continue;
                    if (other == kv.Value) continue;

                    (int, int) key = kv.Value < other ? (kv.Value, other) : (other, kv.Value);
                    if (!acc.TryGetValue(key, out RegionEdge edge))
                    {
                        edge = new RegionEdge { RegionA = key.Item1, RegionB = key.Item2 };
                        acc[key] = edge;
                    }

                    bool river = MobilityMap.EdgeHasRiver(tile, next, dir);
                    bool traversable = MobilityMap.GroundStepCost(tile, next, dir) >= 0;
                    if (traversable) edge.ConnectionWidth++;
                    if (river) edge.RiverPairs++;
                    if (river && MobilityMap.EdgeHasBridge(tile, next, dir)) edge.BridgePairs++;
                    if (traversable && tile.IsRoad && next.IsRoad) edge.RoadLink = true;
                }
            }

            // An edge exists only where ground can actually cross.
            graph.Edges = acc.Values.Where(e => e.ConnectionWidth > 0)
                                    .OrderBy(e => e.RegionA).ThenBy(e => e.RegionB).ToList();
        }

        #endregion // Build
    }
}
