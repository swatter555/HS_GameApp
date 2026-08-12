using System.Collections.Generic;
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
    /// The six-rung intel ladder (§12.2, ratified 2026-07-24): which fields each rung publishes, the two
    /// bucket tiers (§12.5.2), deterministic error (§12.5.5), and the source-ceiling progression rules that
    /// replace the old "+1 per spotting hit" model (§12.4).
    ///
    /// The point of the ladder is an economy: passive looking buys the cheap rungs, equipment counts and
    /// morale have to be paid for with combat or IntelActions, and nothing accumulates by staring. These
    /// tests pin each half of that bargain.
    /// </summary>
    [TestFixture]
    public class IntelLadderTests : BaseTestFixture
    {
        private const int SPOT_X = 2;
        private const int ROW_Y = 5;

        #region Helpers

        private HexMap CreateClearMap(int width = 16, int height = 12)
        {
            var map = new HexMap("TestMap", width, height);
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

        // Equipment entries join their number and label with a NON-BREAKING space so a flowed list wraps only
        // between entries, never inside one. Built from a char code rather than a literal character so no
        // editor or formatter can quietly turn it into an ordinary space and make these assertions vacuous.
        private const char NBSP = (char)0x00A0;

        private static string Entry(int count, string label) => count + NBSP.ToString() + label;

        private CombatUnit Unit(Side side, int x, UnitClassification classification = UnitClassification.MOT)
        {
            var unit = new CombatUnit(
                side == Side.Player ? "Friendly" : "Hostile",
                classification, UnitRole.GroundCombat, side,
                side == Side.Player ? Nationality.USSR : Nationality.MJ);

            unit.SetPosition(new Position2D(x, ROW_Y));
            unit.SetDeploymentPosition(DeploymentPosition.Entrenched);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        /// <summary>A unit with known, non-zero equipment so bucket and error behaviour is observable.</summary>
        private CombatUnit StockedUnit(Side side, int x)
        {
            var unit = Unit(side, x);
            unit.EquipmentBays.TotalIntelStats = new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 1000 },
                { WeaponType.TANK_T55A_SV, 100 },
                { WeaponType.IFV_BMP1_SV, 40 },
                { WeaponType.ART_LIGHT_SV, 20 },
                { WeaponType.ART_HEAVY_SV, 10 },
                { WeaponType.SPSAM_9K31_SV, 8 },
            };
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

        #region Rung contents (§12.2)

        [Test]
        public void Level1_PublishesNothing_TheIconIsTheWholeReport()
        {
            // §12.2.2: Level1 is CONTACT. Even the name is withheld — everything the player gets at this rung
            // comes off the map icon.
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);

            var report = enemy.GetIntelReport(SpottedLevel.Level1);

            Assert.AreNotEqual(enemy.UnitName, report.UnitName, "the name is not earned until Level2");
            Assert.AreEqual(0, report.TANK, "no equipment below Level4");
            Assert.AreEqual(0, report.GetEquipmentEntries().Count);
        }

        [Test]
        public void Level2_AddsNameOnly_NotDeploymentOrEquipment()
        {
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);

            var report = enemy.GetIntelReport(SpottedLevel.Level2);

            Assert.AreEqual(enemy.UnitName, report.UnitName);
            Assert.AreEqual(0, report.TANK, "equipment is a Level4 product");
        }

        [Test]
        public void Level3_AddsDeployment_StillNoEquipment()
        {
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);

            var report = enemy.GetIntelReport(SpottedLevel.Level3);

            Assert.AreEqual(DeploymentPosition.Entrenched, report.DeploymentPosition);
            Assert.AreEqual(0, report.TANK);
        }

        [Test]
        public void Level4_AddsEquipment_ButNotExperienceOrEfficiency()
        {
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);
            enemy.SetExperienceLevel(ExperienceLevel.Elite);

            var report = enemy.GetIntelReport(SpottedLevel.Level4);

            Assert.Greater(report.TANK, 0, "equipment arrives at Level4");
            Assert.AreNotEqual(ExperienceLevel.Elite, report.UnitExperienceLevel,
                "training assessment is a Level5 product — shooting at someone does not tell you how good they are");
        }

        [Test]
        public void Level5_AddsExperienceAndEfficiency()
        {
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);
            enemy.SetExperienceLevel(ExperienceLevel.Elite);

            var report = enemy.GetIntelReport(SpottedLevel.Level5);

            Assert.AreEqual(ExperienceLevel.Elite, report.UnitExperienceLevel);
            Assert.Greater(report.TANK, 0);
        }

        [Test]
        public void Level4_CarriesMoreErrorThanLevel5()
        {
            // §12.2.9: 16% at Level4, 8% at Level5. Better intel must TIGHTEN the estimate, so the two rungs
            // lean the same direction and differ only in magnitude — the Level5 figure is nearer the truth.
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);
            int truth = 100;

            int l4 = enemy.GetIntelReport(SpottedLevel.Level4).TANK;
            int l5 = enemy.GetIntelReport(SpottedLevel.Level5).TANK;

            Assert.LessOrEqual(Mathf.Abs(l5 - truth), Mathf.Abs(l4 - truth),
                "the Level5 estimate is never further from the truth than the Level4 one");
        }

        #endregion // Rung contents

        #region Full view (§12.2.7) and bucket tiers (§12.5.2)

        [Test]
        public void FullReport_IsNotReachableThroughTheLadder()
        {
            // §12.2.7: FULL is an ownership fact, not a rung. Even the top rung is a coarse, lossy view.
            var unit = StockedUnit(Side.Player, SPOT_X);

            var full = unit.GetFullIntelReport();
            var top = unit.GetIntelReport(SpottedLevel.Level5);

            Assert.IsTrue(full.IsFullDetail);
            Assert.IsFalse(top.IsFullDetail, "no SpottedLevel produces the full view");
        }

        [Test]
        public void FullReport_HasZeroError_AndSeventeenBucketDetail()
        {
            var unit = StockedUnit(Side.Player, SPOT_X);

            var full = unit.GetFullIntelReport();

            Assert.AreEqual(100, full.TANK, "friendly counts are exact");
            Assert.AreEqual(30, full.ART, "ART_LIGHT + ART_HEAVY, kept separate from rockets");

            var entries = full.GetEquipmentEntries();
            CollectionAssert.Contains(entries, Entry(1000, "men"));
            CollectionAssert.Contains(entries, Entry(100, "tanks"));
            CollectionAssert.Contains(entries, Entry(8, "SAMs"),
                "the full view keeps SAMs distinct from AAA and AT");
        }

        [Test]
        public void EnemyReport_CollapsesToCoarseBuckets()
        {
            // §12.5.2: the enemy view merges ART+ROC into "guns" and SAM+AAA+AT into "AA".
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);

            var entries = enemy.GetIntelReport(SpottedLevel.Level5).GetEquipmentEntries();
            string joined = string.Join("|", entries).Replace(NBSP, ' ');

            StringAssert.Contains("AA", joined, "SAMs are reported only as generic air defence");
            StringAssert.DoesNotContain("SAMs", joined, "the SAM/AAA distinction is a friendly-only detail");
            StringAssert.DoesNotContain("rockets", joined, "tube vs rocket artillery is a friendly-only detail");
        }

        #endregion // Full view and bucket tiers

        #region Error determinism (§12.5.5)

        [Test]
        public void EquipmentError_IsStableAcrossRepeatedReads()
        {
            // §12.5.5: reselecting the same enemy must reprint the same numbers. A per-request re-roll both
            // jittered the display and leaked the truth to anyone willing to sample repeatedly.
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);

            var first = enemy.GetIntelReport(SpottedLevel.Level4);
            for (int i = 0; i < 8; i++)
            {
                var again = enemy.GetIntelReport(SpottedLevel.Level4);
                Assert.AreEqual(first.TANK, again.TANK);
                Assert.AreEqual(first.Personnel, again.Personnel);
            }
        }

        [Test]
        public void EquipmentError_MovesWhenTheUnitActuallyChanges()
        {
            // The seed is keyed on current HP, so a report changes when the unit changes — and only then.
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);
            int before = enemy.GetIntelReport(SpottedLevel.Level4).Personnel;

            enemy.HitPoints.SetCurrent(enemy.HitPoints.Max * 0.5f);
            int after = enemy.GetIntelReport(SpottedLevel.Level4).Personnel;

            Assert.AreNotEqual(before, after, "a unit at half strength does not report the same numbers");
        }

        [Test]
        public void EquipmentError_StaysWithinTheRungsBand()
        {
            var enemy = StockedUnit(Side.AI, SPOT_X + 5);

            int tanks = enemy.GetIntelReport(SpottedLevel.Level4).TANK;

            int maxDrift = Mathf.CeilToInt(100 * (GameData.MAX_INTEL_ERROR / 100f)) + 1; // +1 for rounding
            Assert.LessOrEqual(Mathf.Abs(tanks - 100), maxDrift, "error never exceeds the rung's band");
        }

        #endregion // Error determinism

        #region Source ceilings (§12.4)

        [Test]
        public void PassiveSpotting_NeverClimbsPastItsCeiling_HoweverManySweeps()
        {
            // §12.4.1: the level is the best ceiling EARNED, not a tally of looks. Ten sweeps from a line unit
            // at range are worth exactly one — this is what stops a player from learning everything by waiting.
            var spotter = Unit(Side.Player, SPOT_X);
            var enemy = Unit(Side.AI, SPOT_X + 2);

            for (int i = 0; i < 10; i++) SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, enemy.SpottedLevel);
        }

        [Test]
        public void GroundIntelAction_RaisesAdjacentEnemyOneRung_CeilingLevel5()
        {
            // §12.4.5: the patient path. Adjacency required, one rung per action, and the ONLY route to Level5.
            var actor = Unit(Side.Player, SPOT_X);
            var enemy = Unit(Side.AI, SPOT_X + 1);
            enemy.SetSpottedLevel(SpottedLevel.Level2);

            SpottingService.ApplyGroundIntelAction(actor);
            Assert.AreEqual(SpottedLevel.Level3, enemy.SpottedLevel);

            SpottingService.ApplyGroundIntelAction(actor);
            SpottingService.ApplyGroundIntelAction(actor);
            Assert.AreEqual(SpottedLevel.Level5, enemy.SpottedLevel);

            SpottingService.ApplyGroundIntelAction(actor);
            Assert.AreEqual(SpottedLevel.Level5, enemy.SpottedLevel, "Level5 is the ceiling, not a waypoint");
        }

        [Test]
        public void GroundIntelAction_IgnoresNonAdjacentEnemies()
        {
            var actor = Unit(Side.Player, SPOT_X);
            var far = Unit(Side.AI, SPOT_X + 2);
            far.SetSpottedLevel(SpottedLevel.Level2);

            SpottingService.ApplyGroundIntelAction(actor);

            Assert.AreEqual(SpottedLevel.Level2, far.SpottedLevel, "there is no intel RANGE — only adjacency");
        }

        [Test]
        public void GroundIntelAction_DoesNotTouchFriendlies()
        {
            var actor = Unit(Side.Player, SPOT_X);
            var friend = Unit(Side.Player, SPOT_X + 1);
            friend.SetSpottedLevel(SpottedLevel.Level0);

            SpottingService.ApplyGroundIntelAction(actor);

            Assert.AreEqual(SpottedLevel.Level0, friend.SpottedLevel);
        }

        [Test]
        public void DirectCombat_SetsTheEnemyToLevel4()
        {
            // §12.4.6: the fast route. One action buys what three IntelActions would.
            var attacker = Unit(Side.Player, SPOT_X);
            var defender = Unit(Side.AI, SPOT_X + 1);

            SpottingService.ApplyDirectCombatContact(attacker, defender);

            Assert.AreEqual(SpottedLevel.Level4, defender.SpottedLevel);
        }

        [Test]
        public void DirectCombat_NeverLowersAHigherLevel()
        {
            // §12.4.3: "set to" sources raise only. Attacking a unit you had walked to Level5 must not
            // discard the intel you paid for.
            var attacker = Unit(Side.Player, SPOT_X);
            var defender = Unit(Side.AI, SPOT_X + 1);
            defender.SetSpottedLevel(SpottedLevel.Level5);

            SpottingService.ApplyDirectCombatContact(attacker, defender);

            Assert.AreEqual(SpottedLevel.Level5, defender.SpottedLevel);
        }

        #endregion // Source ceilings
    }
}
