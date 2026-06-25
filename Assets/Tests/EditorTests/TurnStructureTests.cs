using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the §3 turn-structure boundary helpers: per-turn activity flags,
    /// PlayerRefresh/AI_Refresh unit refresh (§3.3), and Upkeep efficiency recovery (§3.5.8).
    /// Exercises the BattleManager static helpers directly (no scene / coroutine needed) —
    /// the coroutine phase ordering itself is verified in play mode.
    /// </summary>
    [TestFixture]
    public class TurnStructureTests : BaseTestFixture
    {
        #region Helpers

        private static CombatUnit MakeUnit() =>
            new CombatUnit("TurnTestUnit", UnitClassification.INF, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR);

        #endregion // Helpers

        #region Per-turn activity flags (§7.15.8)

        [Test]
        public void TurnFlags_DefaultFalse_MarkSets_ResetClears()
        {
            var unit = MakeUnit();

            Assert.IsFalse(unit.HasMovedThisTurn, "New unit should not be flagged moved.");
            Assert.IsFalse(unit.HasFoughtThisTurn, "New unit should not be flagged fought.");

            unit.MarkMovedThisTurn();
            unit.MarkFoughtThisTurn();
            Assert.IsTrue(unit.HasMovedThisTurn, "MarkMovedThisTurn should set the moved flag.");
            Assert.IsTrue(unit.HasFoughtThisTurn, "MarkFoughtThisTurn should set the fought flag.");

            unit.ResetTurnFlags();
            Assert.IsFalse(unit.HasMovedThisTurn, "ResetTurnFlags should clear the moved flag.");
            Assert.IsFalse(unit.HasFoughtThisTurn, "ResetTurnFlags should clear the fought flag.");
        }

        #endregion // Per-turn activity flags

        #region Refresh (§3.3.1 / §3.3.2)

        [Test]
        public void RefreshUnitForNewTurn_RestoresActionsAndMP_AndClearsFlags()
        {
            var unit = MakeUnit();
            unit.MovementPoints.SetMax(8);
            unit.MovementPoints.SetCurrent(8);

            // Spend the turn: drain MP, drain the move action, flag activity.
            unit.MovementPoints.SetCurrent(0);
            unit.MoveActions.SetCurrent(0);
            unit.MarkMovedThisTurn();
            unit.MarkFoughtThisTurn();

            BattleManager.RefreshUnitForNewTurn(unit);

            Assert.AreEqual(unit.MovementPoints.Max, unit.MovementPoints.Current,
                "MP should be refreshed to max at Refresh (§3.3.2).");
            Assert.AreEqual(unit.MoveActions.Max, unit.MoveActions.Current,
                "MoveActions should be refreshed to max at Refresh (§3.3.1).");
            Assert.IsFalse(unit.HasMovedThisTurn, "Refresh should clear the moved flag.");
            Assert.IsFalse(unit.HasFoughtThisTurn, "Refresh should clear the fought flag.");
        }

        #endregion // Refresh

        #region Upkeep efficiency recovery (§3.5.8)

        [Test]
        public void UpkeepRecovery_Idle_RecoversTwoTiers_CapFull()
        {
            var unit = MakeUnit();
            unit.SetEfficiencyLevel(EfficiencyLevel.NormalOperations); // two below Full

            // Neither moved nor fought this turn → +2 tiers (§7.15.8.1), capped Full.
            BattleManager.ApplyUpkeepRecovery(unit);

            Assert.AreEqual(EfficiencyLevel.FullOperations, unit.EfficiencyLevel,
                "Idle unit should recover +2 tiers (Normal → Full).");
        }

        [Test]
        public void UpkeepRecovery_Moved_RecoversOneTier()
        {
            var unit = MakeUnit();
            unit.SetEfficiencyLevel(EfficiencyLevel.NormalOperations);
            unit.MarkMovedThisTurn();

            // Moved but did not fight → +1 tier (§7.15.8.2).
            BattleManager.ApplyUpkeepRecovery(unit);

            Assert.AreEqual(EfficiencyLevel.CombatOperations, unit.EfficiencyLevel,
                "Moved-only unit should recover +1 tier (Normal → Combat).");
        }

        [Test]
        public void UpkeepRecovery_Fought_NoRecovery()
        {
            var unit = MakeUnit();
            unit.SetEfficiencyLevel(EfficiencyLevel.NormalOperations);
            unit.MarkFoughtThisTurn();

            // Fought → 0 recovery (§7.15.8.3), even if it also moved.
            unit.MarkMovedThisTurn();
            BattleManager.ApplyUpkeepRecovery(unit);

            Assert.AreEqual(EfficiencyLevel.NormalOperations, unit.EfficiencyLevel,
                "A unit that fought should not recover Efficiency this Upkeep.");
        }

        #endregion // Upkeep efficiency recovery
    }
}
