using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Helpers;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the C6 mission-objective machinery: MapLoader.ApplyMissionObjectiveStamp
    /// (clear-then-stamp — the authored isObjective value is dead; missing hexes refuse the load;
    /// non-strongholds warn but stamp) and HexMapUtil.AllMissionObjectivesHeld (the victory gate —
    /// vacuously true with no objectives, false while any stamped hex is not Red).
    /// </summary>
    [TestFixture]
    public class MissionObjectiveGateTests : BaseTestFixture
    {
        #region Helpers

        private static ScenarioManifest ManifestWith(params (int x, int y)[] objectives)
        {
            var list = new List<MissionObjective>();
            foreach (var (x, y) in objectives)
                list.Add(new MissionObjective { X = x, Y = y });

            return new ScenarioManifest { MissionObjectives = list };
        }

        #endregion // Helpers

        #region ApplyMissionObjectiveStamp (C6b)

        [Test]
        public void Stamp_ClearsAuthoredFlags_ThenStampsManifest()
        {
            var map = MapFixtures.UniformMap();
            // Simulate a legacy map with authored flags — the file's value must be DEAD.
            MapFixtures.At(map, 1, 1).IsObjective = true;
            MapFixtures.At(map, 2, 2).IsObjective = true;
            // The manifest's objectives sit on stronghold hexes elsewhere.
            MapFixtures.At(map, 4, 4).SetTerrain(TerrainType.MinorCity);
            MapFixtures.At(map, 5, 5).SetTerrain(TerrainType.MajorCity);

            MapLoader.ApplyMissionObjectiveStamp(map, ManifestWith((4, 4), (5, 5)));

            Assert.IsFalse(MapFixtures.At(map, 1, 1).IsObjective, "Authored flag must be cleared — the file's value is ignored.");
            Assert.IsFalse(MapFixtures.At(map, 2, 2).IsObjective, "Authored flag must be cleared.");
            Assert.IsTrue(MapFixtures.At(map, 4, 4).IsObjective, "Manifest objective must be stamped.");
            Assert.IsTrue(MapFixtures.At(map, 5, 5).IsObjective, "Manifest objective must be stamped.");
        }

        [Test]
        public void Stamp_EmptyManifestList_ClearsEverything_NoGate()
        {
            var map = MapFixtures.UniformMap();
            MapFixtures.At(map, 3, 3).IsObjective = true;   // stale authored flag

            MapLoader.ApplyMissionObjectiveStamp(map, new ScenarioManifest());

            Assert.IsFalse(MapFixtures.At(map, 3, 3).IsObjective, "No declared objectives → a fully clean map.");
            Assert.IsTrue(HexMapUtil.AllMissionObjectivesHeld(map), "And the gate is vacuously met.");
        }

        [Test]
        public void Stamp_ObjectiveOnMissingHex_RefusesTheLoad()
        {
            var map = MapFixtures.UniformMap(12, 12);

            Assert.Throws<InvalidDataException>(
                () => MapLoader.ApplyMissionObjectiveStamp(map, ManifestWith((50, 50))),
                "G6 doctrine: an objective off the map means manifest and .map were not exported together — " +
                "silently skipping would make the victory gate quietly easier.");
        }

        [Test]
        public void Stamp_NonStrongholdObjective_WarnsButStamps()
        {
            // Stronghold placement is an authoring CONVENTION (Bob's call) — the stamp warns loudly
            // but does not refuse, and the flag still lands.
            var map = MapFixtures.UniformMap();

            MapLoader.ApplyMissionObjectiveStamp(map, ManifestWith((6, 6)));   // plain Clear

            Assert.IsTrue(MapFixtures.At(map, 6, 6).IsObjective,
                "An open-ground objective is legal (warned, not refused) — the gate must still see it.");
        }

        #endregion // ApplyMissionObjectiveStamp

        #region AllMissionObjectivesHeld (the gate)

        [Test]
        public void Gate_NoObjectives_VacuouslyHeld()
        {
            Assert.IsTrue(HexMapUtil.AllMissionObjectivesHeld(MapFixtures.UniformMap()),
                "A scenario declaring no objectives has no gate.");
            Assert.IsTrue(HexMapUtil.AllMissionObjectivesHeld(null), "Null map fails OPEN — logged, never unwinnable.");
        }

        [Test]
        public void Gate_AnyUnheldObjective_Blocks()
        {
            var map = MapFixtures.UniformMap();
            var held = MapFixtures.At(map, 2, 2);
            held.IsObjective = true;
            held.TileControl = TileControl.Red;
            var unheld = MapFixtures.At(map, 8, 8);
            unheld.IsObjective = true;
            unheld.TileControl = TileControl.Blue;

            Assert.IsFalse(HexMapUtil.AllMissionObjectivesHeld(map), "One unheld objective closes the gate.");

            unheld.TileControl = TileControl.Red;
            Assert.IsTrue(HexMapUtil.AllMissionObjectivesHeld(map), "All Red → gate open.");
        }

        [Test]
        public void Gate_NeutralObjective_AlsoBlocks()
        {
            // "Controls them all" means RED — a Grey objective is not held either (the offensive
            // scenario's starting state for objectives in no-man's-land).
            var map = MapFixtures.UniformMap();
            var grey = MapFixtures.At(map, 4, 4);
            grey.IsObjective = true;
            grey.TileControl = TileControl.Grey;

            Assert.IsFalse(HexMapUtil.AllMissionObjectivesHeld(map));
        }

        #endregion // AllMissionObjectivesHeld
    }
}
