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
    /// M2 displacement (§6.8 / §7.9): the board consequences of a stand outcome — retreat/rout into the rear
    /// arc, posture drop, shatter withdrawal, and the Surrender Check when boxed in. Uses a clear HexMap
    /// fixture with the defender at (6,6) and an attacker due WEST (so the rear arc is the eastern trio NE/E/SE).
    /// </summary>
    [TestFixture]
    public class RetreatResolverTests : BaseTestFixture
    {
        private const float TOL = 0.001f;
        private static readonly Position2D DefHex = new Position2D(6, 6);

        private HexMap _map;

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            _map = CreateClearMap(12, 12);
            GameDataManager.CurrentHexMap = _map;
            GameManager.InvalidateOccupancy();
        }

        #region Fixture helpers

        private HexMap CreateClearMap(int width, int height)
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

        private CombatUnit Place(UnitClassification cls, Side side, Position2D pos,
            DeploymentPosition dp = DeploymentPosition.Deployed)
        {
            var nat = side == Side.Player ? Nationality.USSR : Nationality.MJ;
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, side, nat);
            u.SetPosition(pos);
            u.SetDeploymentPosition(dp);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            GameManager.RegisterCombatUnit(u);
            return u;
        }

        /// <summary>Defender at (6,6) + an attacker on its WEST edge (rear arc = NE/E/SE).</summary>
        private (CombatUnit atk, CombatUnit def) Engagement(DeploymentPosition defPosture = DeploymentPosition.Deployed)
        {
            var def = Place(UnitClassification.INF, Side.Player, DefHex, defPosture);
            var atk = Place(UnitClassification.INF, Side.AI, HexMapUtil.GetNeighborPosition(DefHex, HexDirection.W));
            return (atk, def);
        }

        #endregion // Fixture helpers

        [Test]
        public void Hold_DoesNotMoveOrVacate()
        {
            var (atk, def) = Engagement();
            var d = RetreatResolver.ResolveDisplacement(atk, def, StandOutcome.Hold, _map, new FixedRollRandom(1));

            Assert.IsFalse(d.Moved, "no displacement on hold");
            Assert.AreEqual(DefHex, def.MapPos, "stays put");
            Assert.IsFalse(d.AutomaticAdvanceAvailable, "hex not vacated");
        }

        [Test]
        public void Retreat_MovesOneHexIntoRearArc_AndVacates()
        {
            var (atk, def) = Engagement();
            Position2D atkPos = atk.MapPos;

            var d = RetreatResolver.ResolveDisplacement(atk, def, StandOutcome.Retreat, _map, new FixedRollRandom(1));

            Assert.IsTrue(d.Moved);
            Assert.AreEqual(1, d.HexesRetreated);
            Assert.AreEqual(def.MapPos, d.FinalPosition, "result tracks the unit's new hex");
            Assert.AreEqual(DefHex, d.VacatedHex, "original hex is freed");
            Assert.IsTrue(d.AutomaticAdvanceAvailable);
            Assert.Greater(HexMapUtil.GetHexDistance(atkPos, def.MapPos), 1, "retreated away from the attacker");
        }

        [Test]
        public void Rout_MovesTwoHexes_AndDropsOneDugInTier()
        {
            var (atk, def) = Engagement(DeploymentPosition.Fortified);

            var d = RetreatResolver.ResolveDisplacement(atk, def, StandOutcome.Rout, _map, new FixedRollRandom(1));

            Assert.IsTrue(d.PostureDropped, "rout strips a dug-in tier");
            Assert.AreEqual(DeploymentPosition.Entrenched, def.DeploymentPosition, "Fortified → Entrenched");
            Assert.AreEqual(2, d.HexesRetreated, "two open-map steps");
            Assert.AreEqual(def.MapPos, d.FinalPosition);
        }

        [Test]
        public void Shatter_WithPath_QuitsFieldAndSurvives_TakesExtraDamage()
        {
            var (atk, def) = Engagement();
            float hp0 = def.HitPoints.Current;

            var d = RetreatResolver.ResolveDisplacement(atk, def, StandOutcome.Shatter, _map, new FixedRollRandom(1));

            Assert.IsTrue(d.RemovedFromMap, "breaks and quits the field");
            Assert.IsFalse(d.Destroyed, "shatter is a survival, not a kill");
            Assert.IsTrue(d.AutomaticAdvanceAvailable);
            Assert.AreEqual(hp0 - GameData.SHATTER_EXTRA_DAMAGE, def.HitPoints.Current, TOL, "+4 shatter damage");
        }

        [Test]
        public void BlockedRetreat_PassedSurrenderCheck_HoldsInPlaceAtCost()
        {
            // Wall off the entire rear arc (NE/E/SE of the defender) so there is no valid candidate.
            _map.GetHexAt(HexMapUtil.GetNeighborPosition(DefHex, HexDirection.NE)).SetTerrain(TerrainType.Impassable);
            _map.GetHexAt(HexMapUtil.GetNeighborPosition(DefHex, HexDirection.E)).SetTerrain(TerrainType.Impassable);
            _map.GetHexAt(HexMapUtil.GetNeighborPosition(DefHex, HexDirection.SE)).SetTerrain(TerrainType.Impassable);
            var (atk, def) = Engagement();

            // Trained surrender check = 10; roll 11 > 10 → holds in place.
            var d = RetreatResolver.ResolveDisplacement(atk, def, StandOutcome.Retreat, _map, new QueueRollRandom(11));

            Assert.IsTrue(d.SurrenderHeldInPlace);
            Assert.IsFalse(d.Moved, "no hex to retreat to");
            Assert.AreEqual(DeploymentPosition.Deployed, def.DeploymentPosition, "forced to bare Deployed");
            Assert.AreEqual(GameData.MAX_HP - GameData.SURRENDER_SURVIVAL_LOSS, def.HitPoints.Current, TOL);
            Assert.IsFalse(d.AutomaticAdvanceAvailable, "a held hex is not vacated");
        }

        [Test]
        public void Retreat_FromNonAdjacentFirer_UsesGeneralBearing()
        {
            // Firer three hexes WEST of the defender (non-adjacent — like indirect fire). The defender should
            // still find a rear-arc retreat (eastward) via the general-direction fallback.
            var def = Place(UnitClassification.INF, Side.Player, DefHex);
            var firer = Place(UnitClassification.INF, Side.AI, new Position2D(3, 6));

            var d = RetreatResolver.ResolveDisplacement(firer, def, StandOutcome.Retreat, _map, new FixedRollRandom(1));

            Assert.IsTrue(d.Moved, "a non-adjacent firer still yields a rear-arc retreat");
            Assert.AreEqual(1, d.HexesRetreated);
            Assert.Greater(
                HexMapUtil.GetHexDistance(firer.MapPos, def.MapPos),
                HexMapUtil.GetHexDistance(firer.MapPos, DefHex),
                "retreated further from the firer");
        }

        [Test]
        public void BlockedRetreat_FailedSurrenderCheck_Destroyed()
        {
            _map.GetHexAt(HexMapUtil.GetNeighborPosition(DefHex, HexDirection.NE)).SetTerrain(TerrainType.Impassable);
            _map.GetHexAt(HexMapUtil.GetNeighborPosition(DefHex, HexDirection.E)).SetTerrain(TerrainType.Impassable);
            _map.GetHexAt(HexMapUtil.GetNeighborPosition(DefHex, HexDirection.SE)).SetTerrain(TerrainType.Impassable);
            var (atk, def) = Engagement();

            // Trained check = 10; roll 5 ≤ 10 → surrenders.
            var d = RetreatResolver.ResolveDisplacement(atk, def, StandOutcome.Retreat, _map, new QueueRollRandom(5));

            Assert.IsTrue(d.Surrendered);
            Assert.IsTrue(d.Destroyed);
            Assert.IsTrue(d.RemovedFromMap);
            Assert.IsTrue(d.AutomaticAdvanceAvailable, "a failed check empties the hex");
        }
    }
}
