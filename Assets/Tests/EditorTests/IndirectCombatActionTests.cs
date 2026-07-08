using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Integration tests for the indirect-fire orchestrator (IndirectCombatAction.Execute, §7.13) — the model-layer
    /// caller above CombatResolver.ResolveIndirectAttack, mirroring GroundCombatActionTests for the direct path.
    /// Covers the 2026-07-06 routing rule (adjacent targets are LEGAL indirect shots), the counter-battery economy
    /// (§7.13.5.7 — 1 OpportunityAction + flat-50% supply, suppressed without the action), the fired-vs-shelled
    /// supply asymmetry, AI-firer auto-reveal (§7.13.5.4), non-adjacent displacement, and kill reporting.
    /// Real weapon profiles; seeded dice (FixedRollRandom).
    /// </summary>
    [TestFixture]
    public class IndirectCombatActionTests : BaseTestFixture
    {
        private const float TOL = 0.001f;

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
            GameDataManager.CurrentHexMap = CreateClearMap();
        }

        #region Helpers

        private HexMap CreateClearMap(int width = 16, int height = 16)
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

        private CombatUnit Build(UnitClassification cls, WeaponType deployed, Side side, Position2D pos,
            SpottedLevel spotted = SpottedLevel.Level0, int mp = 12, float supply = 5f)
        {
            var nat = side == Side.Player ? Nationality.USSR : Nationality.MJ;
            var role = cls == UnitClassification.SPA ? UnitRole.GroundCombatIndirect : UnitRole.GroundCombat;
            var u = new CombatUnit("U", cls, role, side, nat);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            u.SetEfficiencyLevel(EfficiencyLevel.FullOperations);
            u.SetPosition(pos);
            u.MovementPoints.SetMax(mp);
            u.MovementPoints.SetCurrent(mp);
            u.DaysSupply.SetMax(5f);
            u.DaysSupply.SetCurrent(supply);
            u.SetSpottedLevel(spotted);
            GameManager.RegisterCombatUnit(u);
            return u;
        }

        /// <summary>2S19 Msta: IR 5 — the test battery for both firer and counter-battery target.</summary>
        private CombatUnit Artillery(Side side, Position2D pos, SpottedLevel spotted = SpottedLevel.Level0) =>
            Build(UnitClassification.SPA, WeaponType.SPA_2S19_SV, side, pos, spotted);

        private CombatUnit Infantry(Side side, Position2D pos, SpottedLevel spotted = SpottedLevel.Level0) =>
            Build(UnitClassification.INF, WeaponType.INF_REG_SV, side, pos, spotted);

        #endregion // Helpers

        #region Validation (rejected before any cost)

        [Test]
        public void Reject_OutOfIndirectRange_NoCostPaid()
        {
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Infantry(Side.AI, new Position2D(5, 11), SpottedLevel.Level2); // distance 6 > IR 5

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsFalse(r.Executed, "Beyond IR is rejected");
            StringAssert.Contains("range", r.Reason);
            Assert.AreEqual(1f, firer.CombatActions.Current, "No CombatAction spent on a rejected shot");
            Assert.AreEqual(12f, firer.MovementPoints.Current, "No MP spent on a rejected shot");
        }

        [Test]
        public void Reject_UnspottedEnemy()
        {
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Infantry(Side.AI, new Position2D(5, 8)); // in range, SpottedLevel0

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsFalse(r.Executed, "Cannot shell what you cannot see");
            StringAssert.Contains("spotted", r.Reason);
        }

        #endregion // Validation

        #region Routing rule + action economy

        [Test]
        public void AdjacentTarget_IsLegal_AndResolvesAsIndirect()
        {
            // The 2026-07-06 routing rule: an indirect-fire unit NEVER fights a direct engagement — an adjacent
            // target is a legal §7.13 shot (range [1, IR] includes 1) with no universal return fire.
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Infantry(Side.AI, new Position2D(6, 5), SpottedLevel.Level2); // adjacent

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed, "Adjacent is inside [1, IR] — legal indirect shot");
            Assert.GreaterOrEqual(r.DamageToTarget, 1, "a connecting hit lands ≥ 1");
            Assert.IsFalse(r.CounterBatteryFired, "infantry has no counter-battery");
            Assert.AreEqual(0, r.DamageToFirer, "no universal return fire on the indirect path (§7.13)");
        }

        [Test]
        public void Success_SpendsEconomy_AndSupplyOnlyForTheShooter()
        {
            // FixedRollRandom(1): connecting hit, target holds; every 1d100 degradation roll = 1 → EL drops for
            // BOTH sides (§7.15.3), but supply models SHOOTING: only the firer rolls §7.15.5 — a shelled unit
            // that fired nothing loses no supply.
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Infantry(Side.AI, new Position2D(5, 8), SpottedLevel.Level2); // distance 3
            float combatCost = firer.GetCombatMovementCost();

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed);
            Assert.AreEqual(0f, firer.CombatActions.Current, "1 CombatAction spent (§8.2.1)");
            Assert.AreEqual(12f - combatCost, firer.MovementPoints.Current, TOL, "25% max MP spent (§8.2.1)");
            Assert.IsTrue(firer.HasFoughtThisTurn, "firer flagged fought");
            Assert.IsTrue(target.HasFoughtThisTurn, "target flagged fought");

            Assert.AreEqual(EfficiencyLevel.CombatOperations, firer.EfficiencyLevel, "firer Full → Combat (§7.15.3)");
            Assert.AreEqual(EfficiencyLevel.CombatOperations, target.EfficiencyLevel, "target Full → Combat (§7.15.3)");
            Assert.AreEqual(4f, firer.DaysSupply.Current, TOL, "firer lost 1 supply (§7.15.5 — it shot)");
            Assert.AreEqual(5f, target.DaysSupply.Current, TOL, "shelled non-CB target loses NO supply");
        }

        #endregion // Routing rule + action economy

        #region Counter-battery (§7.13.5)

        [Test]
        public void CounterBattery_Fires_SpendsTargetOpportunityAction_AndSupply()
        {
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Artillery(Side.AI, new Position2D(5, 8), SpottedLevel.Level2); // dist 3 ≤ IR 5 both ways
            Assert.GreaterOrEqual(target.OpportunityActions.Current, 1f, "precondition: SPA has an OpportunityAction");

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed);
            Assert.IsTrue(r.CounterBatteryFired, "artillery target in range returns fire (§7.13.5)");
            Assert.GreaterOrEqual(r.DamageToFirer, 1, "a connecting CB hit lands ≥ 1");
            Assert.AreEqual(0f, target.OpportunityActions.Current, "CB costs the target 1 OpportunityAction (§7.13.5.7)");
            Assert.AreEqual(4f, target.DaysSupply.Current, TOL, "CB flat-50% supply roll of 1 → −1 supply (§7.15.6)");
        }

        [Test]
        public void CounterBattery_Suppressed_WhenTargetHasNoOpportunityAction()
        {
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Artillery(Side.AI, new Position2D(5, 8), SpottedLevel.Level2);
            target.OpportunityActions.SetCurrent(0f); // budget exhausted — cannot pay for the CB shot

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed);
            Assert.IsFalse(r.CounterBatteryFired, "no OpportunityAction → no counter-battery (§7.13.5.7)");
            Assert.AreEqual(0, r.DamageToFirer, "suppressed CB deals nothing");
            Assert.AreEqual(5f, target.DaysSupply.Current, TOL, "no CB shot → no CB supply cost");
        }

        #endregion // Counter-battery

        #region Reveal, displacement, destruction

        [Test]
        public void AIFirer_IsRevealed_OnFiring()
        {
            // Fog is one-directional (SpottedLevel lives on AI units): an unspotted AI battery that fires gains
            // a level (§7.13.5.4). Target is a player unit — no spotting gate applies to it.
            var firer = Artillery(Side.AI, new Position2D(5, 8));   // SpottedLevel0
            var target = Infantry(Side.Player, new Position2D(5, 5));

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed);
            Assert.AreEqual(SpottedLevel.Level1, firer.SpottedLevel, "firing exposes the battery (+1 level)");
        }

        [Test]
        public void HardHit_DisplacesTarget_FromNonAdjacentBearing()
        {
            // FixedRollRandom(8): heavy hit → high Shock → stand roll 8 dislodges. The retreat bearing derives
            // from the DISTANT firer (§7.13 / M4 GetGeneralDirection) — this exercises that non-adjacent path.
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Infantry(Side.AI, new Position2D(5, 8), SpottedLevel.Level2);

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(8));

            Assert.IsTrue(r.Executed);
            Assert.AreNotEqual(StandOutcome.Hold, r.TargetOutcome, "heavy shelling dislodges the target");
            Assert.IsTrue(r.TargetMoved || r.TargetRemovedFromMap, "target vacates the hex");
            Assert.IsFalse(r.TargetDestroyed, "a full-HP infantry is not destroyed by the hit");
        }

        [Test]
        public void LethalHit_Destroys_Unregisters_AndReportsPrestige()
        {
            var firer = Artillery(Side.Player, new Position2D(5, 5));
            var target = Infantry(Side.AI, new Position2D(5, 8), SpottedLevel.Level2);
            target.TakeDamage(target.HitPoints.Max - 1f); // 1 HP
            string targetId = target.UnitID;

            var r = IndirectCombatAction.Execute(firer, target, GameDataManager.CurrentHexMap, new FixedRollRandom(8));

            Assert.IsTrue(r.Executed);
            Assert.IsTrue(r.TargetDestroyed, "1 HP − connecting hit → destroyed");
            Assert.IsTrue(r.TargetRemovedFromMap);
            Assert.Greater(r.PrestigeOwedToFirer, 0, "half purchase cost owed on a kill (§18.2.3)");
            Assert.IsNull(GameManager.GetCombatUnit(targetId), "a destroyed unit is unregistered");
        }

        #endregion // Reveal, displacement, destruction
    }
}
