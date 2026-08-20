using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the §17.3 grading ladder (V9) + the §17.x mission-objective gate cap (C6):
    /// BattleManager.GradeBattleResult / OneRungBelow / ComputeEarlyFinishBonus. Cuts and shares are
    /// exact binary fractions (16ths) so boundary assertions cannot wobble on float representation.
    /// </summary>
    [TestFixture]
    public class VictoryGradeTests : BaseTestFixture
    {
        // Standard fixture: s0 = 0.5, cuts 9/16 (0.5625) / 10/16 (0.625) / 12/16 (0.75).
        // Mirrored defeat cuts land on 7/16, 6/16, 4/16 — all exact.
        private const float S0 = 0.5f;
        private const float Minor = 0.5625f;
        private const float Major = 0.625f;
        private const float Decisive = 0.75f;

        /// <summary>A ledger whose PlayerShare is exactly <paramref name="sixteenths"/>/16.</summary>
        private static VictoryLedger Share16(int sixteenths) =>
            new VictoryLedger(sixteenths, 16 - sixteenths, 0f);

        private static BattleResult Grade(int sixteenths, bool held = true,
            BattleResult required = BattleResult.MinorVictory) =>
            BattleManager.GradeBattleResult(Share16(sixteenths), S0, Minor, Major, Decisive, held, required);

        #region The ladder (V9.1/V9.2)

        [Test]
        public void Grade_AllSevenRungs_AtAndBetweenCuts()
        {
            Assert.AreEqual(BattleResult.DecisiveVictory, Grade(13), "Above the decisive cut.");
            Assert.AreEqual(BattleResult.DecisiveVictory, Grade(12), "AT the decisive cut — cuts are inclusive upward.");
            Assert.AreEqual(BattleResult.MajorVictory, Grade(11));
            Assert.AreEqual(BattleResult.MajorVictory, Grade(10), "AT the major cut.");
            Assert.AreEqual(BattleResult.MinorVictory, Grade(9), "AT the minor cut.");
            Assert.AreEqual(BattleResult.Draw, Grade(8), "The starting share grades Draw by construction.");
            Assert.AreEqual(BattleResult.MinorDefeat, Grade(7), "AT the mirrored minor cut — defeat cuts are inclusive downward.");
            Assert.AreEqual(BattleResult.MajorDefeat, Grade(6), "AT the mirrored major cut.");
            Assert.AreEqual(BattleResult.MajorDefeat, Grade(5));
            Assert.AreEqual(BattleResult.DecisiveDefeat, Grade(4), "AT the mirrored decisive cut.");
            Assert.AreEqual(BattleResult.DecisiveDefeat, Grade(2));
        }

        [Test]
        public void Grade_MirrorsAroundTheActualStartingShare_NotHalf()
        {
            // V9.1: a defensive-ish start at s0 = 0.25 — the same 0.3 share that would be deep defeat
            // against a 0.5 anchor is a DRAW here, because the player has not LOST anything.
            var result = BattleManager.GradeBattleResult(new VictoryLedger(3f, 7f, 0f), 0.25f,
                0.5f, 0.625f, 0.75f, objectivesHeld: true, BattleResult.Draw);

            Assert.AreEqual(BattleResult.Draw, result,
                "The stalemate premise is a convention, not an assumption — the mirror anchor is the REAL starting share.");
        }

        #endregion // The ladder

        #region Degenerate guards (C1 / V5.4)

        [Test]
        public void Grade_NoScoringDeclared_IsAlwaysDraw()
        {
            // C1: all-zero thresholds. Without this guard a zero decisive cut grades EVERY share
            // DecisiveVictory — the day-one instant-win bug caught in the v2 spec.
            var result = BattleManager.GradeBattleResult(Share16(16), S0, 0f, 0f, 0f,
                objectivesHeld: false, BattleResult.MinorVictory);

            Assert.AreEqual(BattleResult.Draw, result, "No scoring declared → Draw, gate irrelevant.");
        }

        [Test]
        public void Grade_ZeroValueMap_IsDraw()
        {
            var result = BattleManager.GradeBattleResult(new VictoryLedger(0f, 0f, 0f), S0,
                Minor, Major, Decisive, objectivesHeld: true, BattleResult.MinorVictory);

            Assert.AreEqual(BattleResult.Draw, result, "An unscored map grades Draw (V5.4) — every pre-rebalance shipped map.");
        }

        #endregion // Degenerate guards

        #region The C6 gate cap

        [Test]
        public void Grade_GateUnmet_Offensive_CapsAtDraw()
        {
            Assert.AreEqual(BattleResult.Draw, Grade(13, held: false, BattleResult.MinorVictory),
                "A decisive SHARE with a lost objective is not a victory — capped one rung below the required MinorVictory.");
        }

        [Test]
        public void Grade_GateUnmet_Defensive_CapsAtMinorDefeat()
        {
            // THE case the one-rung-below rule exists for: a flat Draw cap would let a defender lose
            // their objectives and still pass a Draw-required scenario.
            Assert.AreEqual(BattleResult.MinorDefeat, Grade(8, held: false, BattleResult.Draw),
                "Losing your objectives fails a defensive scenario even at a held share.");
        }

        [Test]
        public void Grade_GateUnmet_Withdrawal_CapsAtMajorDefeat()
        {
            Assert.AreEqual(BattleResult.MajorDefeat, Grade(7, held: false, BattleResult.MinorDefeat),
                "A fighting withdrawal that loses its objectives grades below its required MinorDefeat.");
        }

        [Test]
        public void Grade_GateUnmet_TakesTheWorseOfShareAndCap()
        {
            Assert.AreEqual(BattleResult.MajorDefeat, Grade(6, held: false, BattleResult.MinorVictory),
                "The cap only removes the upside — a share already grading MajorDefeat stays MajorDefeat.");
        }

        [Test]
        public void Grade_GateMet_LeavesTheShareGrade()
        {
            Assert.AreEqual(BattleResult.DecisiveVictory, Grade(13, held: true, BattleResult.MinorVictory));
        }

        /// <summary>Map with <paramref name="total"/> stamped objectives, first <paramref name="red"/> Red.</summary>
        private static HexMap ObjectiveMap(int total, int red)
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
        public void Grade_FractionalGateMet_LeavesTheShareGrade()
        {
            // C7 end-to-end: the REAL map-driven gate feeds the grade — 5 of 9 at fraction 0.5 is met,
            // so a decisive share grades DecisiveVictory. This is the interaction the pass exists for:
            // partial objective holds now earn the middle and upper rungs instead of collapsing to Draw.
            bool gate = BattleManager.MissionObjectiveGateMet(ObjectiveMap(9, 5), 0.5f);

            Assert.IsTrue(gate);
            Assert.AreEqual(BattleResult.DecisiveVictory, Grade(13, held: gate, BattleResult.MinorVictory));
        }

        [Test]
        public void Grade_FractionalGateUnmet_StillCapsOneRungBelow()
        {
            // §2.4 invariant, asserted at the pure layer: 4 of 9 at 0.5 leaves the gate unmet, so even
            // a decisive share caps one rung below the required MinorVictory. Because ALL THREE gate
            // call sites (grading, early end, auto-end) run this same MissionObjectiveGateMet, the
            // auto-end can never fire at a rung the grade would then deny — the sites cannot disagree.
            bool gate = BattleManager.MissionObjectiveGateMet(ObjectiveMap(9, 4), 0.5f);

            Assert.IsFalse(gate);
            Assert.AreEqual(BattleResult.Draw, Grade(13, held: gate, BattleResult.MinorVictory));
        }

        [Test]
        public void OneRungBelow_StepsTowardDefeat_AndSaturates()
        {
            Assert.AreEqual(BattleResult.Draw, BattleManager.OneRungBelow(BattleResult.MinorVictory));
            Assert.AreEqual(BattleResult.MinorDefeat, BattleManager.OneRungBelow(BattleResult.Draw));
            Assert.AreEqual(BattleResult.DecisiveDefeat, BattleManager.OneRungBelow(BattleResult.DecisiveDefeat),
                "The bottom rung caps to itself — the degenerate authoring case noted in C6.");
        }

        #endregion // The C6 gate cap

        #region Ladder reachability audit (editor's pass-close catch)

        [Test]
        public void Audit_BalancedStart_NoFindings()
        {
            var findings = BattleManager.AuditLadderReachability(0.5f, Minor, Major, Decisive);

            Assert.IsEmpty(findings, "At the stalemate premise every rung is reachable — the mirror behaves.");
        }

        [Test]
        public void Audit_OffensiveStart_NamesAllThreeDeadDefeatRungs()
        {
            // Shipped Khost: s0 = 0.226 against 0.55/0.65/0.8 — all three mirrored defeat cuts are
            // negative, so every non-victory grades Draw and total collapse reads as a near-miss.
            var findings = BattleManager.AuditLadderReachability(0.2258f, 0.55f, 0.65f, 0.8f);

            Assert.AreEqual(3, findings.Count, "All three defeat rungs are dead and each must be NAMED.");
            StringAssert.Contains("MinorDefeat", findings[0]);
            StringAssert.Contains("MajorDefeat", findings[1]);
            StringAssert.Contains("DecisiveDefeat", findings[2]);
        }

        [Test]
        public void Audit_StartAboveMinorCut_FlagsMetAtStart()
        {
            // The inverse degenerate: a defensive-ish start ABOVE the minor cut is a victory before
            // the first order. (Defeat side stays healthy here: 1.2 − 0.8 = 0.4 > 0.)
            var findings = BattleManager.AuditLadderReachability(0.6f, 0.55f, 0.65f, 0.8f);

            Assert.AreEqual(1, findings.Count);
            StringAssert.Contains("MinorVictory", findings[0]);
            StringAssert.Contains("met before the first order", findings[0]);
        }

        [Test]
        public void Audit_NoScoringDeclared_IsSilent()
        {
            Assert.IsEmpty(BattleManager.AuditLadderReachability(0.5f, 0f, 0f, 0f),
                "An unscored scenario has no ladder to audit.");
        }

        #endregion // Ladder reachability audit

        #region Early-finish bonus (V10.2 / C3)

        [Test]
        public void EarlyFinishBonus_UnusedTurnsTimesSteadyIncomeTimesMultiplier()
        {
            // Steady income = 20 stipend + 100 × 0.05 = 25; 6 turns × 25 × 1.25 = 187.5 → 188.
            int bonus = BattleManager.ComputeEarlyFinishBonus(new VictoryLedger(100f, 60f, 0f),
                stipend: 20, incomeRate: 0.05f, unusedTurns: 6, multiplier: 1.25f);

            Assert.AreEqual(188, bonus);
        }

        [Test]
        public void EarlyFinishBonus_ExcludesTheProgressBonus_ByConstruction()
        {
            // The bonus prices what SITTING STILL would have earned — and sitting still never pays
            // the high-water progress bonus. Signature makes it structural: no bonus-rate parameter.
            int bonus = BattleManager.ComputeEarlyFinishBonus(new VictoryLedger(100f, 0f, 0f),
                stipend: 0, incomeRate: 0.1f, unusedTurns: 10, multiplier: 1.0f);

            Assert.AreEqual(100, bonus, "10 turns × 10 steady income × 1.0 — nothing else.");
        }

        [Test]
        public void EarlyFinishBonus_NoUnusedTurns_PaysNothing()
        {
            int bonus = BattleManager.ComputeEarlyFinishBonus(new VictoryLedger(500f, 0f, 0f),
                stipend: 50, incomeRate: 1f, unusedTurns: 0, multiplier: 2f);

            Assert.AreEqual(0, bonus, "Cashing out on the last turn is just ending the battle.");
        }

        #endregion // Early-finish bonus
    }
}
