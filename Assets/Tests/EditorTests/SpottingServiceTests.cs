using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Integration tests for the dual-domain spotting sweep (§12.3): a spotter uses its GROUND range against
    /// ground targets and its (often longer) AIR range against airborne targets. The crux is the air-defence
    /// platform — a SAM detects aircraft far (air 6) but reveals ground units only at the basic 2 — and the
    /// NOE attack-helo exception (HELO is a GROUND target, an EmbarkedHelo lift is an AIR target). Exercises
    /// the live SpottingService against a registered player/AI roster (mirrors MovementTests' harness).
    /// </summary>
    [TestFixture]
    public class SpottingServiceTests : BaseTestFixture
    {
        private const int SPOT_X = 2;   // spotter column; all units share row Y so hex distance == |Δx|
        private const int ROW_Y = 5;

        #region Helpers

        private HexMap CreateClearMap(int width = 16, int height = 12)
        {
            var map = new HexMap("TestMap", MapConfig.Small);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(TerrainType.Clear);
                    map.SetHexAt(hex);
                }
            map.BuildNeighborRelationships();
            return map;
        }

        /// <summary>Player spotter of the given classification at column <paramref name="x"/> on the shared row.</summary>
        private CombatUnit Spotter(UnitClassification classification, int x = SPOT_X)
        {
            var unit = new CombatUnit("Spotter", classification, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            unit.SetPosition(new Position2D(x, ROW_Y));
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        /// <summary>AI target of the given classification at column <paramref name="x"/> on the shared row.</summary>
        private CombatUnit Target(UnitClassification classification, int x, SpottedLevel spotted = SpottedLevel.Level0)
        {
            var unit = new CombatUnit("Target", classification, UnitRole.GroundCombat, Side.AI, Nationality.MJ);
            unit.SetPosition(new Position2D(x, ROW_Y));
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            unit.SetSpottedLevel(spotted);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            GameManager.InvalidateOccupancy();
            GameDataManager.CurrentHexMap = CreateClearMap();
        }

        #endregion // Helpers

        #region Air-defence dual-domain crux

        [Test]
        public void Sweep_SamVsFixedWing_UsesLongAirRange()
        {
            // SAM air range = 6: a fixed-wing target at distance 5 (within 6) is spotted.
            var sam = Spotter(UnitClassification.SAM);
            var fgt = Target(UnitClassification.FGT, SPOT_X + 5);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, fgt.SpottedLevel,
                "SAM should detect a fixed-wing target within its air range (6)");
        }

        [Test]
        public void Sweep_SamVsFixedWing_BeyondAirRange_NotSpotted()
        {
            // Distance 7 exceeds the SAM air range (6) — air search is bounded, not unlimited.
            var sam = Spotter(UnitClassification.SAM);
            var fgt = Target(UnitClassification.FGT, SPOT_X + 7);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level0, fgt.SpottedLevel,
                "A fixed-wing beyond the SAM air range stays invisible");
        }

        [Test]
        public void Sweep_SamVsGround_UsesBasicGroundRange()
        {
            // SAM ground range = 2: a ground unit at distance 2 is spotted; one at distance 4 is NOT —
            // the SAM's long range is air-search only.
            var sam = Spotter(UnitClassification.SAM);
            var near = Target(UnitClassification.TANK, SPOT_X + 2);
            var far = Target(UnitClassification.TANK, SPOT_X + 4);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, near.SpottedLevel, "Ground unit within ground range (2) is spotted");
            Assert.AreEqual(SpottedLevel.Level0, far.SpottedLevel, "Ground unit beyond ground range (2) is NOT spotted by the SAM");
        }

        #endregion // Air-defence dual-domain crux

        #region NOE attack helo vs air-assault lift

        [Test]
        public void Sweep_SamVsAttackHelo_TreatedAsGroundTarget()
        {
            // The NOE exception: an attack helo (HELO) is a GROUND target, so the SAM uses its ground range (2).
            // At distance 4 the helo escapes — even though a fixed-wing at the same distance would be caught (air 6).
            var sam = Spotter(UnitClassification.SAM);
            var helo = Target(UnitClassification.HELO, SPOT_X + 4);
            var fgt = Target(UnitClassification.FGT, SPOT_X + 3);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level0, helo.SpottedLevel, "NOE attack helo is spotted on the ground range (2), so distance 4 escapes");
            Assert.AreEqual(SpottedLevel.Level1, fgt.SpottedLevel, "A fixed-wing at distance 3 is caught on the air range (6)");
        }

        [Test]
        public void Sweep_SamVsEmbarkedHeloLift_TreatedAsAirTarget()
        {
            // An AM/MAM air-assault lift (EmbarkedHelo state) IS an air target — a lift cannot hide as easily as
            // an NOE gunship — so the SAM's air range (6) catches it at distance 5.
            var sam = Spotter(UnitClassification.SAM);
            var lift = Target(UnitClassification.AM, SPOT_X + 5);
            lift.SetCurrentEmbarkmentState(EmbarkmentState.EmbarkedHelo);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, lift.SpottedLevel,
                "An EmbarkedHelo air-assault lift is an air target, caught within the SAM air range (6)");
        }

        [Test]
        public void Sweep_SamVsDismountedAirMobile_TreatedAsGroundTarget()
        {
            // A dismounted AM (NotEmbarked) is a ground target — ground range (2), so distance 4 escapes.
            var sam = Spotter(UnitClassification.SAM);
            var am = Target(UnitClassification.AM, SPOT_X + 4);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level0, am.SpottedLevel,
                "A dismounted air-mobile unit is a ground target, beyond the SAM ground range (2)");
        }

        #endregion // NOE attack helo vs air-assault lift

        #region Baseline + decay

        [Test]
        public void Sweep_GroundSpotter_BasicRangeTwoUnchanged()
        {
            // A plain ground combat spotter still works on the basic ground range (2).
            var tank = Spotter(UnitClassification.TANK);
            var near = Target(UnitClassification.MOT, SPOT_X + 2);
            var far = Target(UnitClassification.MOT, SPOT_X + 3);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, near.SpottedLevel, "Ground spotter sees a ground target at distance 2");
            Assert.AreEqual(SpottedLevel.Level0, far.SpottedLevel, "Ground spotter does not see a ground target at distance 3");
        }

        [Test]
        public void Decay_RespectsDualDomain_AirTargetHeld_GroundTargetDecays()
        {
            // ProcessSpottingDecay delegates the "still in range?" test to IsCurrentlySpotted, which is now
            // dual-domain. A SAM at distance 5 keeps a fixed-wing visible (air 6) but lets a ground unit decay (ground 2).
            var sam = Spotter(UnitClassification.SAM);
            var air = Target(UnitClassification.FGT, SPOT_X + 5, SpottedLevel.Level2);   // dist 5 ≤ air 6 → held
            var ground = Target(UnitClassification.TANK, SPOT_X + 4, SpottedLevel.Level2); // dist 4 > ground 2 → decays

            SpottingService.ProcessSpottingDecay();

            Assert.AreEqual(SpottedLevel.Level2, air.SpottedLevel, "Fixed-wing within air range holds its level (no decay)");
            Assert.AreEqual(SpottedLevel.Level1, ground.SpottedLevel, "Ground unit beyond ground range decays one step");
        }

        #endregion // Baseline + decay
    }
}
