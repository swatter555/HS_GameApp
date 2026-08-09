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
        public void Sweep_SamVsAirborneHeloLift_TreatedAsAirTarget()
        {
            /* An AM/MAM air-assault lift in flight IS an air target — a lift cannot hide as easily as an
             * NOE gunship — so the SAM's air range (6) catches it at distance 5. ⚠ REWRITTEN 2026-08-08
             * (P1/D1): the old test forced the never-written EmbarkmentState, so this §12.3 rule never
             * actually fired in play. The real fact is the active profile's medium: Embarked + helos in
             * the bay = riding helicopters. */
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();

            var sam = Spotter(UnitClassification.SAM);
            var lift = Target(UnitClassification.AM, SPOT_X + 5);
            lift.EquipmentBays.InitializeEquipmentBays("Lift",
                WeaponType.INF_AM_SV, WeaponType.NONE, WeaponType.HEL_MI8T_SV);
            lift.SetDeploymentPosition(DeploymentPosition.Embarked);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, lift.SpottedLevel,
                "An airborne air-assault lift is an air target, caught within the SAM air range (6)");
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
        public void Decay_RespectsDualDomain_AirFloorHeld_GroundTargetDecays()
        {
            // The SUSTAINED FLOOR (§12.6.2) is computed with the same dual-domain range selection as the sweep.
            // A SAM at distance 5 sustains a Level1 floor on a fixed-wing (air range 6) but sustains nothing on
            // a ground unit at distance 4 (ground range 2), which therefore decays away.
            var sam = Spotter(UnitClassification.SAM);
            var air = Target(UnitClassification.FGT, SPOT_X + 5, SpottedLevel.Level1);     // dist 5 ≤ air 6 → floor L1
            var ground = Target(UnitClassification.TANK, SPOT_X + 4, SpottedLevel.Level1); // dist 4 > ground 2 → floor L0

            SpottingService.ProcessSpottingDecay();

            Assert.AreEqual(SpottedLevel.Level1, air.SpottedLevel, "Fixed-wing inside air range is sustained at its floor");
            Assert.AreEqual(SpottedLevel.Level0, ground.SpottedLevel, "Ground unit beyond ground range has no floor and decays away");
        }

        [Test]
        public void Decay_EarnedRungsAboveTheFloor_ErodeOneStepAtATime()
        {
            // §12.6.3 (rewritten): rungs bought with combat or IntelActions sit ABOVE the passive floor and
            // erode one per Refresh — not the old single-step collapse to Level1, which on the six-rung ladder
            // would wipe out three IntelActions of investment in one turn.
            var tank = Spotter(UnitClassification.TANK);
            var target = Target(UnitClassification.MOT, SPOT_X + 2, SpottedLevel.Level5); // dist 2 → floor L1

            SpottingService.ProcessSpottingDecay();
            Assert.AreEqual(SpottedLevel.Level4, target.SpottedLevel, "one rung per Refresh");

            SpottingService.ProcessSpottingDecay();
            Assert.AreEqual(SpottedLevel.Level3, target.SpottedLevel);

            SpottingService.ProcessSpottingDecay();
            SpottingService.ProcessSpottingDecay();
            Assert.AreEqual(SpottedLevel.Level1, target.SpottedLevel, "erosion stops at the sustained floor");

            SpottingService.ProcessSpottingDecay();
            Assert.AreEqual(SpottedLevel.Level1, target.SpottedLevel, "the floor itself never decays while contact holds");
        }

        [Test]
        public void Decay_AdjacentEnemy_HoldsEarnedRungs()
        {
            // §12.6.3: physical contact preserves hard-won intel. The spotter is adjacent, so a Level5 contact
            // does not erode at all — this is what makes "keep somebody in contact" a live tactical decision.
            var tank = Spotter(UnitClassification.TANK);
            var target = Target(UnitClassification.MOT, SPOT_X + 1, SpottedLevel.Level5);

            SpottingService.ProcessSpottingDecay();
            SpottingService.ProcessSpottingDecay();

            Assert.AreEqual(SpottedLevel.Level5, target.SpottedLevel, "adjacency holds every earned rung");
        }

        [Test]
        public void Sweep_AdjacentSpotter_EarnsLevel2_AndAdjacentRecon_EarnsLevel3()
        {
            // §12.4.2: range establishes contact, adjacency decides what it is worth. A RECON unit standing
            // next to a target reads it one rung deeper than a line unit does.
            var recon = Spotter(UnitClassification.RECON);
            var adjacent = Target(UnitClassification.MOT, SPOT_X + 1);
            var atRange = Target(UnitClassification.MOT, SPOT_X + 3);

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level3, adjacent.SpottedLevel, "adjacent RECON earns Level3");
            Assert.AreEqual(SpottedLevel.Level1, atRange.SpottedLevel, "the same RECON at range earns only contact");
        }

        #endregion // Baseline + decay
    }
}
