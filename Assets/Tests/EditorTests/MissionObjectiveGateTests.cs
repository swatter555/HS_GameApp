using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Helpers;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the C6/C7 mission-objective machinery: MapLoader.ApplyMissionObjectiveStamp
    /// (clear-then-stamp — the authored isObjective value is dead; missing hexes refuse the load;
    /// non-strongholds warn but stamp), HexMapUtil.CountMissionObjectives (held/total over the stamped
    /// flags, fail-open (0,0)), and the C7 fractional gate — BattleManager.RequiredObjectiveCount
    /// (ceil with the §2.3 float-trap defence, clamped [1, total]) + MissionObjectiveGateMet, the ONE
    /// predicate all three gate call sites share. The fraction-1.0 tests are the C6 backward-compat proof.
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
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(map, 1.0f), "And the gate is vacuously met.");
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

        #region CountMissionObjectives + the C7 gate

        /// <summary>A map with <paramref name="total"/> stamped objectives, the first <paramref name="red"/> Red-held.</summary>
        private static HexMap MapWithObjectives(int total, int red)
        {
            var map = MapFixtures.UniformMap();
            for (int i = 0; i < total; i++)
            {
                var t = MapFixtures.At(map, i % 8 + 1, i / 8 + 1);
                t.IsObjective = true;
                t.TileControl = i < red ? TileControl.Red : TileControl.Blue;
            }
            return map;
        }

        [Test]
        public void Gate_NoObjectives_VacuouslyMet_AtAnyFraction()
        {
            Assert.AreEqual((0, 0), HexMapUtil.CountMissionObjectives(MapFixtures.UniformMap()),
                "A scenario declaring no objectives has no gate.");
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(MapFixtures.UniformMap(), 1.0f));
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(MapFixtures.UniformMap(), 0.5f));
        }

        [Test]
        public void Gate_NullMap_FailsOpen_AtAnyFraction()
        {
            Assert.AreEqual((0, 0), HexMapUtil.CountMissionObjectives(null),
                "Null map fails OPEN — logged, never unwinnable (the existing C6 contract).");
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(null, 1.0f));
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(null, 0.5f));
        }

        [Test]
        public void Gate_AnyUnheldObjective_Blocks_AtFractionOne()
        {
            // The C6 backward-compat proof: at fraction 1.0 the behaviour is exactly all-of-them.
            var map = MapWithObjectives(total: 2, red: 1);

            Assert.AreEqual((1, 2), HexMapUtil.CountMissionObjectives(map));
            Assert.IsFalse(BattleManager.MissionObjectiveGateMet(map, 1.0f), "One unheld objective closes the gate.");

            MapFixtures.At(map, 2, 1).TileControl = TileControl.Red;
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(map, 1.0f), "All Red → gate open.");
        }

        [Test]
        public void Gate_NeutralObjective_CountsAsUnheld()
        {
            // "Held" means RED — a Grey objective is not held either (the offensive scenario's
            // starting state for objectives in no-man's-land).
            var map = MapFixtures.UniformMap();
            var grey = MapFixtures.At(map, 4, 4);
            grey.IsObjective = true;
            grey.TileControl = TileControl.Grey;

            Assert.AreEqual((0, 1), HexMapUtil.CountMissionObjectives(map));
            Assert.IsFalse(BattleManager.MissionObjectiveGateMet(map, 1.0f));
        }

        [Test]
        public void Gate_PartialHold_MeetsFractionalGate()
        {
            // Bob's Khost setting: fraction 0.5 over 9 objectives → any 5 of 9.
            Assert.IsTrue(BattleManager.MissionObjectiveGateMet(MapWithObjectives(9, 5), 0.5f),
                "5 of 9 at fraction 0.5 meets the gate (requires ceil(4.5) = 5).");
            Assert.IsFalse(BattleManager.MissionObjectiveGateMet(MapWithObjectives(9, 4), 0.5f),
                "4 of 9 does not.");
        }

        #endregion // CountMissionObjectives + the C7 gate

        #region RequiredObjectiveCount (the C7 arithmetic)

        [Test]
        public void RequiredObjectiveCount_Fraction1_IsAllOfThem()
        {
            // The C6 equivalence: fraction 1.0 (and anything above, pre-clamped by IsValid) demands
            // every objective, exactly — no float arithmetic on the default path.
            Assert.AreEqual(1, BattleManager.RequiredObjectiveCount(1, 1.0f));
            Assert.AreEqual(9, BattleManager.RequiredObjectiveCount(9, 1.0f));
            Assert.AreEqual(12, BattleManager.RequiredObjectiveCount(12, 1.0f));
        }

        [Test]
        public void RequiredObjectiveCount_RoundsUp()
        {
            Assert.AreEqual(5, BattleManager.RequiredObjectiveCount(9, 0.5f), "ceil(4.5) = 5.");
            Assert.AreEqual(2, BattleManager.RequiredObjectiveCount(4, 0.5f), "Exact halves stay exact.");
            Assert.AreEqual(2, BattleManager.RequiredObjectiveCount(3, 0.5f), "ceil(1.5) = 2.");
            Assert.AreEqual(7, BattleManager.RequiredObjectiveCount(9, 0.75f), "ceil(6.75) = 7.");
        }

        [Test]
        public void RequiredObjectiveCount_FloatEdge_DoesNotOverCount()
        {
            // ⭐ THE FLOAT TRAP (editor C7 §2.3): 0.3f is 0.30000001…, so a naive
            // Math.Ceiling(10 * 0.3f) lands on 4. The round-then-ceil defence must report 3.
            Assert.AreEqual(3, BattleManager.RequiredObjectiveCount(10, 0.3f));
            Assert.AreEqual(7, BattleManager.RequiredObjectiveCount(10, 0.7f), "0.7f errs LOW — right either way.");
            Assert.AreEqual(6, BattleManager.RequiredObjectiveCount(20, 0.3f), "Same trap at a larger total.");
        }

        [Test]
        public void RequiredObjectiveCount_NeverZero_WhenObjectivesDeclared()
        {
            Assert.AreEqual(1, BattleManager.RequiredObjectiveCount(9, 0.01f),
                "A declared gate always demands at least one — the clamp floor.");
            Assert.AreEqual(0, BattleManager.RequiredObjectiveCount(0, 0.5f),
                "No objectives → nothing demanded (the vacuous case).");
        }

        #endregion // RequiredObjectiveCount
    }
}
