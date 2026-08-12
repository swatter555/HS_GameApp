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
    /// D3 — §5.13.2 helicopter over-water grace. A helo MAY end a turn over Water, but must reach land by
    /// the end of its NEXT turn or it is lost at sea, taking anything it carries with it. Two halves are
    /// tested here: that a helo is ALLOWED to stop over water at all (it was previously displaced back to
    /// land by <see cref="HexMapUtil.FindNearestLegalRestingHex"/>), and that the grace clock gives exactly
    /// one turn.
    ///
    /// ⚠ The clock runs at UPKEEP, not Refresh — Refresh fires before the unit has had the move the rule
    /// exists to give it, so a Refresh check would mean zero turns of grace rather than one.
    /// </summary>
    [TestFixture]
    public class OverWaterGraceTests : BaseTestFixture
    {
        private const int ROW_Y = 5;
        private const int WATER_X = 8;   // a single water column; everything else is Clear

        private HexMap _map;

        #region Helpers

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            GameManager.InvalidateOccupancy();
            GameDataManager.ClearLossLedger();
            _map = CreateMapWithWaterColumn();
            GameDataManager.CurrentHexMap = _map;
        }

        /// <summary>Clear everywhere except one full column of Water, so "reach land" is always one hex away.</summary>
        private HexMap CreateMapWithWaterColumn(int width = 16, int height = 12)
        {
            var map = new HexMap("TestMap", width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(x == WATER_X ? TerrainType.Water : TerrainType.Clear);
                    map.SetHexAt(hex);
                }
            map.BuildNeighborRelationships();
            return map;
        }

        private CombatUnit Build(string name, UnitClassification cls, UnitRole role,
            WeaponType deployed, WeaponType embarked, int x)
        {
            var u = new CombatUnit(name, cls, role, Side.Player, Nationality.USSR);
            u.EquipmentBays.InitializeEquipmentBays(name, deployed, WeaponType.NONE, embarked);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetPosition(new Position2D(x, ROW_Y));
            GameManager.RegisterCombatUnit(u);
            return u;
        }

        /// <summary>An attack helicopter — flying by its DEPLOYED profile, so always airborne.</summary>
        private CombatUnit Helo(int x) =>
            Build("Gunship", UnitClassification.HELO, UnitRole.GroundCombat,
                WeaponType.HEL_MI24V_SV, WeaponType.NONE, x);

        private CombatUnit Infantry(int x) =>
            Build("Riflemen", UnitClassification.INF, UnitRole.GroundCombat,
                WeaponType.INF_REG_SV, WeaponType.NONE, x);

        private static Position2D Water => new Position2D(WATER_X, ROW_Y);
        private static Position2D Land => new Position2D(WATER_X - 2, ROW_Y);

        #endregion // Helpers

        #region May a helicopter stop over water at all?

        [Test]
        public void Helo_MayComeToRestOverWater()
        {
            /* ⚠ THE FIX D3 TURNS ON. `CanRestAt` rejected Water for anything in the GROUND domain, and a
             * helicopter occupies the ground domain for stacking — so a helo ordered onto water used to be
             * quietly displaced back to land by the post-move settlement, and the over-water rule could
             * never come up at all. */
            var helo = Helo(WATER_X);

            var settled = HexMapUtil.FindNearestLegalRestingHex(_map, helo, helo.MapPos);

            Assert.AreEqual(Water, settled, "a helicopter stays where it stopped, over the water");
        }

        [Test]
        public void GroundUnit_IsStillDisplacedOffWater()
        {
            /* The permission is keyed on "is it flying RIGHT NOW", so it must not have leaked to anything
             * walking. A rifle regiment somehow standing on water is still moved to dry land. */
            var inf = Infantry(WATER_X);

            var settled = HexMapUtil.FindNearestLegalRestingHex(_map, inf, inf.MapPos);

            Assert.AreNotEqual(Water, settled, "a ground unit may not rest on water");
            Assert.AreEqual(TerrainType.Clear, _map.GetHexAt(settled).Terrain, "it is put on land");
        }

        #endregion // May a helicopter stop over water

        #region The grace clock

        [Test]
        public void FirstUpkeepOverWater_StartsTheClockAndSurvives()
        {
            var helo = Helo(WATER_X);

            bool lost = BattleManager.ApplyOverWaterGrace(helo, _map);

            Assert.IsFalse(lost, "the first turn over water is the grace turn");
            Assert.IsTrue(helo.EndedTurnOverWater, "the clock is running");
            Assert.IsNotNull(GameManager.GetCombatUnit(helo.UnitID), "still on the map");
        }

        [Test]
        public void SecondConsecutiveUpkeepOverWater_IsLost()
        {
            var helo = Helo(WATER_X);

            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map), "turn 1: grace");
            bool lost = BattleManager.ApplyOverWaterGrace(helo, _map);

            Assert.IsTrue(lost, "turn 2 still at sea: lost");
            Assert.IsNull(GameManager.GetCombatUnit(helo.UnitID), "removed from the map");
        }

        [Test]
        public void ReachingLand_ClearsTheClock_AndGraceIsFullyRestored()
        {
            /* The flag is a "was it already out there last Upkeep" bit, not a countdown, so making landfall
             * has to reset it completely — otherwise a helo that crossed water once would die instantly the
             * next time it did, which is not one turn of grace, it is one turn of grace per battle. */
            var helo = Helo(WATER_X);
            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map));
            Assert.IsTrue(helo.EndedTurnOverWater);

            helo.SetPosition(Land);
            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map), "on land, nothing happens");
            Assert.IsFalse(helo.EndedTurnOverWater, "the clock is cleared by making landfall");

            helo.SetPosition(Water);
            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map),
                "a later crossing gets its own full turn of grace");
            Assert.IsTrue(helo.EndedTurnOverWater);
        }

        [Test]
        public void HeloOnLand_IsNeverTouched()
        {
            var helo = Helo(WATER_X - 2);

            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map));
            Assert.IsFalse(helo.EndedTurnOverWater);
        }

        #endregion // The grace clock

        #region Scope and consequences

        [Test]
        public void FixedWingOverWater_IsNotSubjectToTheRule()
        {
            /* ⚠ §5.13.2 is a HELICOPTER rule. A fixed-wing aircraft parked over water is the unbuilt
             * §5.13.5 auto-return gap; drowning it here would disguise that gap as a working feature. */
            var jet = Build("Jet", UnitClassification.ATT, UnitRole.AirGroundAttack,
                WeaponType.ATT_SU25_SV, WeaponType.NONE, WATER_X);

            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(jet, _map), "turn 1");
            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(jet, _map), "turn 2 — still untouched");
            Assert.IsFalse(jet.EndedTurnOverWater);
            Assert.IsNotNull(GameManager.GetCombatUnit(jet.UnitID));
        }

        [Test]
        public void ALoadedLiftGoesDownWithItsRegiment_AndBooksItsEquipment()
        {
            /* ⚠ The lift IS the regiment — one UnitID, one HP pool, riding its Embarked profile — so there
             * is no second unit to dispose of. What DOES need doing explicitly is the loss booking: no
             * damage event fires when a unit is lost at sea, and `TakeDamage` is the only automatic hook
             * into the ledger (§3.6d). Same reason a surrender books its own equipment. */
            var lift = Build("Air Assault", UnitClassification.AM, UnitRole.GroundCombat,
                WeaponType.INF_AM_SV, WeaponType.HEL_MI8T_SV, WATER_X);
            lift.SetDeploymentPosition(DeploymentPosition.Embarked);

            Assert.AreEqual(MovementMedium.Helo, MovementModeService.CurrentMedium(lift),
                "precondition: riding its helicopters, so it is a helo for this rule");

            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(lift, _map), "grace turn");
            Assert.IsTrue(BattleManager.ApplyOverWaterGrace(lift, _map), "lost on the second");

            Assert.IsNull(GameManager.GetCombatUnit(lift.UnitID), "the regiment goes with the lift");

            var ledger = GameDataManager.GetLossLedger(Side.Player);
            Assert.Greater(ledger.Count, 0,
                "equipment lost at sea is booked — no damage event fires, so nothing else would book it");
        }

        [Test]
        public void AHeloHaltedOverWaterWithNoMovement_StillGetsItsFullGraceTurn()
        {
            /* The interaction worth pinning (D3's own note): a helicopter halted over water by air-defence
             * fire has 0 MP and 0 actions for the rest of THAT turn. It is not doomed by that — Refresh
             * restores its movement, so it gets a real chance to fly out. It dies only if it is still over
             * water at the following Upkeep. */
            var helo = Helo(WATER_X);
            helo.ForceSetMovementPoints(0);
            helo.ForceSetActions(0, 0, 0);

            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map),
                "being shot to a standstill over water does not itself kill the sortie");

            BattleManager.RefreshUnitForNewTurn(helo);
            Assert.Greater(helo.MovementPoints.Current, 0f, "it has the movement to escape with");

            helo.SetPosition(Land);
            Assert.IsFalse(BattleManager.ApplyOverWaterGrace(helo, _map), "and escaping works");
            Assert.IsFalse(helo.EndedTurnOverWater);
        }

        #endregion // Scope and consequences
    }
}
