using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// AI2b — the SpottingService symmetric sweep (AI-Design-Supplement Part 3.2): AI spotters vs
    /// player units under the same §12.3 dual-domain ranges, feeding the BELIEF STORE and never
    /// touching CombatUnit.SpottedLevel (which stays "player's view of AI units"). Mirrors the
    /// SpottingServiceTests harness with the sides swapped.
    /// </summary>
    [TestFixture]
    public class AIPerceptionSweepTests : BaseTestFixture
    {
        private const int SPOT_X = 2;
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

        /// <summary>AI-side spotter on the shared row.</summary>
        private CombatUnit AISpotter(UnitClassification classification, int x = SPOT_X)
        {
            var unit = new CombatUnit("AISpotter", classification, UnitRole.GroundCombat, Side.AI, Nationality.MJ);
            unit.SetPosition(new Position2D(x, ROW_Y));
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            GameManager.RegisterCombatUnit(unit);
            return unit;
        }

        /// <summary>Player-side target on the shared row.</summary>
        private CombatUnit PlayerTarget(UnitClassification classification, int x)
        {
            var unit = new CombatUnit("PlayerTarget", classification, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            unit.SetPosition(new Position2D(x, ROW_Y));
            unit.SetDeploymentPosition(DeploymentPosition.Deployed);
            unit.SetSpottedLevel(SpottedLevel.Level0); // pin the before-state — the ctor does not default to Level0
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

        [Test]
        public void AISweep_InRange_FeedsBeliefStore_NeverCombatUnit()
        {
            var perception = new AIPerceptionState();
            AISpotter(UnitClassification.INF);                                   // ground range 2
            CombatUnit tank = PlayerTarget(UnitClassification.TANK, SPOT_X + 2); // at exactly range

            SpottingService.RecomputeAIPerception(perception, currentTurn: 1);

            Assert.AreEqual(SpottedLevel.Level1, perception.LevelOf(tank.UnitID), "belief store gained the contact");
            Assert.AreEqual(SpottedLevel.Level0, tank.SpottedLevel, "CombatUnit.SpottedLevel is untouched — that field is the player's view of AI units");
            ContactRecord contact = perception.GetContact(tank.UnitID);
            Assert.AreEqual(UnitClassification.TANK, contact.Classification);
            Assert.AreEqual(new Position2D(SPOT_X + 2, ROW_Y), contact.LastKnownPos);
        }

        [Test]
        public void AISweep_OutOfRange_NoContact()
        {
            var perception = new AIPerceptionState();
            AISpotter(UnitClassification.INF);                                   // ground range 2
            CombatUnit tank = PlayerTarget(UnitClassification.TANK, SPOT_X + 3); // one hex beyond

            SpottingService.RecomputeAIPerception(perception, currentTurn: 1);

            Assert.AreEqual(SpottedLevel.Level0, perception.LevelOf(tank.UnitID));
        }

        [Test]
        public void AISweep_ContactLost_DecaysToGhost()
        {
            var perception = new AIPerceptionState();
            AISpotter(UnitClassification.INF);
            CombatUnit tank = PlayerTarget(UnitClassification.TANK, SPOT_X + 1);

            SpottingService.RecomputeAIPerception(perception, currentTurn: 1);
            Assert.AreEqual(SpottedLevel.Level1, perception.LevelOf(tank.UnitID));

            tank.SetPosition(new Position2D(SPOT_X + 9, ROW_Y)); // drives off into the dark

            SpottingService.StepAIPerceptionDecay(perception, currentTurn: 2);

            Assert.AreEqual(SpottedLevel.Level0, perception.LevelOf(tank.UnitID), "Level1 contact went dark");
            GhostContact ghost = perception.GetGhost(tank.UnitID);
            Assert.IsNotNull(ghost, "lost contact persists as a ghost");
            Assert.AreEqual(new Position2D(SPOT_X + 1, ROW_Y), ghost.LastKnownPos, "ghost anchors to LAST KNOWN position, not the true one");
        }
    }
}
