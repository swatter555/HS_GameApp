using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// AI1a board analysis (AI-Design-Supplement Part 4): mobility edge costs, multi-source distance
    /// fields, region segmentation/merging/edges, and chokepoint detection — on small synthetic maps.
    /// Single-row strips keep odd-r adjacency trivial (E/W only) where geometry isn't the point.
    /// </summary>
    [TestFixture]
    public class BoardAnalysisTests
    {
        #region Fixtures

        private static HexTile Tile(int x, int y, TerrainType terrain)
        {
            var tile = new HexTile(new Vector2Int(x, y));
            tile.SetTerrain(terrain);
            tile.MovementCost = terrain switch
            {
                TerrainType.Clear => 1,
                TerrainType.Forest => 2,
                TerrainType.Rough => 3,
                TerrainType.Marsh => 4,
                TerrainType.Mountains => 5,
                TerrainType.MinorCity => 1,
                TerrainType.MajorCity => 1,
                _ => 0,
            };
            return tile;
        }

        // HexMap enforces a 10×10 minimum; the ctor does NOT prefill tiles (dictionary-backed), so
        // fixtures live on a 12×12 canvas with only the hexes under test populated — everything
        // else is null (off-map) to the analysis modules.
        private const int CANVAS = 12;

        /// <summary>1-row strip of <paramref name="terrains"/> at y=0 — adjacency is pure E/W.</summary>
        private static HexMap Strip(params TerrainType[] terrains)
        {
            var map = new HexMap("test-strip", CANVAS, CANVAS);
            for (int x = 0; x < terrains.Length; x++)
                map.SetHexAt(Tile(x, 0, terrains[x]));
            return map;
        }

        private static HexMap OpenField(int width, int height)
        {
            var map = new HexMap("test-field", CANVAS, CANVAS);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map.SetHexAt(Tile(x, y, TerrainType.Clear));
            return map;
        }

        private static Position2D P(int x, int y) => new Position2D(x, y);

        /// <summary>River (optionally bridged) on the East edge of hex (x, 0).</summary>
        private static void RiverEastOf(HexMap map, int x, bool bridged)
        {
            HexTile tile = map.GetHexAt(P(x, 0));
            tile.RiverBorders.SetBorder(HexDirection.E, true);
            if (bridged) tile.BridgeBorders.SetBorder(HexDirection.E, true);
        }

        #endregion // Fixtures

        #region Mobility

        [Test]
        public void GroundStepCost_RoadToRoad_HalvesFloorMinOne()
        {
            HexMap map = Strip(TerrainType.Marsh, TerrainType.Marsh);
            HexTile a = map.GetHexAt(P(0, 0));
            HexTile b = map.GetHexAt(P(1, 0));

            Assert.AreEqual(4, MobilityMap.GroundStepCost(a, b, HexDirection.E), "no road");

            a.SetIsRoad(true);
            b.SetIsRoad(true);
            Assert.AreEqual(2, MobilityMap.GroundStepCost(a, b, HexDirection.E), "road-to-road halves");

            b.SetIsRoad(false);
            Assert.AreEqual(4, MobilityMap.GroundStepCost(a, b, HexDirection.E), "road-to-nonroad is full cost");
        }

        [Test]
        public void GroundStepCost_River_BlocksUnlessBridged()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear);
            RiverEastOf(map, 0, bridged: false);
            HexTile a = map.GetHexAt(P(0, 0));
            HexTile b = map.GetHexAt(P(1, 0));

            Assert.AreEqual(-1, MobilityMap.GroundStepCost(a, b, HexDirection.E), "unbridged river blocks");
            Assert.AreEqual(-1, MobilityMap.GroundStepCost(b, a, HexDirection.W), "blocked from either side");

            a.BridgeBorders.SetBorder(HexDirection.E, true);
            Assert.AreEqual(1, MobilityMap.GroundStepCost(a, b, HexDirection.E), "bridge restores crossing");
        }

        [Test]
        public void GroundDistanceField_MultiSource_TakesNearestSource()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear,
                               TerrainType.Clear, TerrainType.Clear);

            var field = MobilityMap.GroundDistanceField(map, new[] { P(0, 0), P(4, 0) });

            Assert.AreEqual(0, field[P(0, 0)]);
            Assert.AreEqual(1, field[P(1, 0)]);
            Assert.AreEqual(2, field[P(2, 0)]);
            Assert.AreEqual(1, field[P(3, 0)], "reached cheaper from the right-hand source");
            Assert.AreEqual(0, field[P(4, 0)]);
        }

        #endregion // Mobility

        #region Region graph

        [Test]
        public void RegionGraph_TerrainClassSplit_EdgeCarriesRiverAndBridgeTexture()
        {
            // 3 Clear | river+bridge | 3 Mountains → two regions, one 1-wide bridged edge.
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear,
                               TerrainType.Mountains, TerrainType.Mountains, TerrainType.Mountains);
            RiverEastOf(map, 2, bridged: true);

            RegionGraph graph = RegionGraph.Build(map);

            Assert.AreEqual(2, graph.Regions.Count);
            Assert.AreNotEqual(graph.RegionOf[P(0, 0)], graph.RegionOf[P(3, 0)]);
            Assert.AreEqual(1, graph.Edges.Count);

            RegionEdge edge = graph.Edges[0];
            Assert.AreEqual(1, edge.ConnectionWidth);
            Assert.AreEqual(1, edge.RiverPairs);
            Assert.AreEqual(1, edge.BridgePairs);
        }

        [Test]
        public void RegionGraph_UnbridgedRiver_SplitsWithNoEdge()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear,
                               TerrainType.Clear, TerrainType.Clear, TerrainType.Clear);
            RiverEastOf(map, 2, bridged: false);

            RegionGraph graph = RegionGraph.Build(map);

            Assert.AreEqual(2, graph.Regions.Count, "one plain, two banks");
            Assert.AreNotEqual(graph.RegionOf[P(2, 0)], graph.RegionOf[P(3, 0)]);
            Assert.AreEqual(0, graph.Edges.Count, "no ground crossing → no edge");
        }

        [Test]
        public void RegionGraph_TinyFragment_MergesIntoNeighbor()
        {
            // Single Forest hex inside a Clear strip is below minRegionSize and must be absorbed.
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear,
                               TerrainType.Forest,
                               TerrainType.Clear, TerrainType.Clear, TerrainType.Clear);

            RegionGraph graph = RegionGraph.Build(map);

            Assert.IsTrue(graph.Regions.All(r => r.Hexes.Count >= 3), "no undersized region survives");
            int forestRegion = graph.RegionOf[P(3, 0)];
            Assert.IsTrue(forestRegion == graph.RegionOf[P(2, 0)] || forestRegion == graph.RegionOf[P(4, 0)],
                "forest hex was absorbed by an adjacent bank");
        }

        [Test]
        public void RegionGraph_ObjectiveMetadata_Accumulates()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear);
            HexTile objective = map.GetHexAt(P(1, 0));
            objective.SetIsObjective(true);
            objective.VictoryValue = 20f;

            RegionGraph graph = RegionGraph.Build(map);
            Region region = graph.Regions[graph.RegionOf[P(1, 0)]];

            Assert.AreEqual(1, region.ObjectiveCount);
            Assert.AreEqual(20f, region.ObjectiveValue, 1e-4f);
        }

        #endregion // Region graph

        #region Chokepoints

        [Test]
        public void Chokepoints_StripInternalsAreArticulation_EndpointsAreNot()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear,
                               TerrainType.Clear, TerrainType.Clear);

            ChokepointAnalysis choke = ChokepointAnalysis.Build(map);

            Assert.IsTrue(choke.ArticulationHexes.Contains(P(2, 0)), "internal strip hex cuts the graph");
            Assert.IsFalse(choke.ArticulationHexes.Contains(P(0, 0)), "endpoint is not a cut vertex");
            Assert.IsFalse(choke.ArticulationHexes.Contains(P(4, 0)), "endpoint is not a cut vertex");
        }

        [Test]
        public void Chokepoints_OpenField_HasNone()
        {
            HexMap map = OpenField(3, 3);

            ChokepointAnalysis choke = ChokepointAnalysis.Build(map);

            Assert.AreEqual(0, choke.ArticulationHexes.Count, "an open field has no cut vertices");
        }

        [Test]
        public void Chokepoints_BridgedCrossing_IsRecorded()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear, TerrainType.Clear);
            RiverEastOf(map, 1, bridged: true);

            ChokepointAnalysis choke = ChokepointAnalysis.Build(map);

            Assert.AreEqual(1, choke.BridgeCrossings.Count);
            Assert.AreEqual(P(1, 0), choke.BridgeCrossings[0].A);
            Assert.AreEqual(P(2, 0), choke.BridgeCrossings[0].B);
            Assert.IsTrue(choke.ArticulationHexes.Contains(P(1, 0)), "bridgehead is a cut vertex");
            Assert.IsTrue(choke.ArticulationHexes.Contains(P(2, 0)), "bridgehead is a cut vertex");
        }

        #endregion // Chokepoints

        #region Orchestrator

        [Test]
        public void BoardAnalysis_Build_PopulatesBothProducts()
        {
            HexMap map = Strip(TerrainType.Clear, TerrainType.Clear, TerrainType.Clear);

            BoardAnalysis analysis = BoardAnalysis.Build(map);

            Assert.IsNotNull(analysis.Regions);
            Assert.IsNotNull(analysis.Chokepoints);
            Assert.AreEqual(1, analysis.Regions.Regions.Count);
        }

        #endregion // Orchestrator
    }
}
