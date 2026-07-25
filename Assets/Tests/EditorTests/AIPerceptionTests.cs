using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.AI;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// AI2a — the AI belief store (AI-Design-Supplement Part 3): contact progression, §12.6 decay
    /// mirroring, ghost lifecycle, and the R2 decay-grace dial. Pure model — the SpottingService
    /// symmetric sweep that feeds it is AI2b.
    /// </summary>
    [TestFixture]
    public class AIPerceptionTests
    {
        private static Position2D P(int x, int y) => new Position2D(x, y);

        private static void Spot(AIPerceptionState s, string id, int turn, int x = 5, int y = 5,
            SpottedLevel ceiling = SpottedLevel.Level1) =>
            s.RecordSpot(id, P(x, y), turn, UnitClassification.TANK, 100, 2, ceiling);

        /// <summary>No live contact on anything — every tracked unit decays from wherever it is.</summary>
        private static readonly Dictionary<string, SpottedLevel> NoneInRange = new Dictionary<string, SpottedLevel>();

        /// <summary>A sustained floor of <paramref name="level"/> on one unit (§12.6.2).</summary>
        private static Dictionary<string, SpottedLevel> Floor(string id, SpottedLevel level) =>
            new Dictionary<string, SpottedLevel> { { id, level } };

        [Test]
        public void RecordSpot_RaisesToCeiling_RepeatedLooksDoNotAccumulate()
        {
            // §12.4.1 (rewritten 2026-07-24): a contact is RAISED TO the source's ceiling, never incremented.
            // Six looks from the same passive spotter are worth exactly one look.
            var s = new AIPerceptionState();
            for (int i = 0; i < 6; i++) Spot(s, "u1", turn: 1);

            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"));
            Assert.AreEqual(1, s.Contacts.Count);
        }

        [Test]
        public void RecordSpot_NeverLowersAnAlreadyHigherLevel()
        {
            // §12.4.3: a "set to" source raises up to its ceiling and never pulls a higher level back down —
            // a distant look must not undo what adjacency already bought.
            var s = new AIPerceptionState();
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level3);
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level1);

            Assert.AreEqual(SpottedLevel.Level3, s.LevelOf("u1"));
        }

        [Test]
        public void StepDecay_ContactAtItsSustainedFloor_Holds()
        {
            var s = new AIPerceptionState();
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level2);

            s.StepDecay(currentTurn: 2, Floor("u1", SpottedLevel.Level2));

            Assert.AreEqual(SpottedLevel.Level2, s.LevelOf("u1"));
        }

        [Test]
        public void StepDecay_RungsAboveTheFloor_ErodeEvenWhileStillInSensorRange()
        {
            // The symmetry rule that a boolean "is it in range" test would get wrong (§12.9): being watched
            // from a distance sustains Level1, it does NOT preserve the Level4 an earlier engagement bought.
            var s = new AIPerceptionState();
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level4);

            s.StepDecay(2, Floor("u1", SpottedLevel.Level1));
            Assert.AreEqual(SpottedLevel.Level3, s.LevelOf("u1"));

            s.StepDecay(3, Floor("u1", SpottedLevel.Level1));
            Assert.AreEqual(SpottedLevel.Level2, s.LevelOf("u1"));

            s.StepDecay(4, Floor("u1", SpottedLevel.Level1));
            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"), "erosion stops at the floor");

            s.StepDecay(5, Floor("u1", SpottedLevel.Level1));
            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"), "the floor itself never decays");
            Assert.IsNull(s.GetGhost("u1"), "a sustained contact never ghosts");
        }

        [Test]
        public void StepDecay_AdjacentContact_Holds()
        {
            // §12.6.3: adjacency holds a contact at its current rung even when it is outside sensor range.
            var s = new AIPerceptionState();
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level4);

            s.StepDecay(2, NoneInRange, new HashSet<string> { "u1" });
            s.StepDecay(3, NoneInRange, new HashSet<string> { "u1" });

            Assert.AreEqual(SpottedLevel.Level4, s.LevelOf("u1"));
        }

        [Test]
        public void StepDecay_OutOfRange_DropsOneRungPerTurn_ThenGhosts()
        {
            // §12.6.3 (rewritten): graduated decay. The old model collapsed Level2+ → Level1 in a single step,
            // which on the six-rung ladder would discard several rungs at once.
            var s = new AIPerceptionState();
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level4);

            s.StepDecay(2, NoneInRange);
            Assert.AreEqual(SpottedLevel.Level3, s.LevelOf("u1"), "one rung, not a collapse");

            s.StepDecay(3, NoneInRange);
            Assert.AreEqual(SpottedLevel.Level2, s.LevelOf("u1"));

            s.StepDecay(4, NoneInRange);
            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"));

            s.StepDecay(5, NoneInRange);
            Assert.AreEqual(SpottedLevel.Level0, s.LevelOf("u1"), "§12.6.4: Level1 → gone");
            Assert.IsNotNull(s.GetGhost("u1"), "lost contact becomes a ghost");
            Assert.AreEqual(0, s.Contacts.Count);
        }

        [Test]
        public void Ghost_UncertaintyGrows_AndExpires()
        {
            var s = new AIPerceptionState { GhostLifetimeTurns = 3 };
            Spot(s, "u1", 1);              // Level1
            s.StepDecay(2, NoneInRange);   // → ghost at turn 2

            GhostContact g = s.GetGhost("u1");
            Assert.IsNotNull(g);
            Assert.AreEqual(P(5, 5), g.LastKnownPos);
            Assert.AreEqual(2, g.UncertaintyRadius(currentTurn: 3), "1 turn lost × 2 MP");
            Assert.AreEqual(6, g.UncertaintyRadius(currentTurn: 5));

            s.StepDecay(6, NoneInRange);   // 6 − 2 > 3 → expired
            Assert.IsNull(s.GetGhost("u1"));
        }

        [Test]
        public void RecordSpot_ReacquiredGhost_BecomesFreshContact()
        {
            var s = new AIPerceptionState();
            Spot(s, "u1", 1);
            s.StepDecay(2, NoneInRange); // ghost

            Spot(s, "u1", 4, x: 7, y: 5);

            Assert.IsNull(s.GetGhost("u1"), "§12.6.6: re-establishing contact resets decay");
            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"));
            Assert.AreEqual(P(7, 5), s.GetContact("u1").LastKnownPos);
        }

        [Test]
        public void DecayGrace_R2Dial_DelaysDecay()
        {
            var s = new AIPerceptionState { DecayGraceTurns = 2 };
            Spot(s, "u1", 1, ceiling: SpottedLevel.Level2); // last seen turn 1

            s.StepDecay(2, NoneInRange); // 2−1 ≤ 2 → held by grace
            s.StepDecay(3, NoneInRange); // 3−1 ≤ 2 → held by grace
            Assert.AreEqual(SpottedLevel.Level2, s.LevelOf("u1"), "R2 grace holds the contact");

            s.StepDecay(4, NoneInRange); // 4−1 > 2 → decays one rung
            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"));
        }

        [Test]
        public void RemoveUnit_ClearsBothStores_NoGhost()
        {
            var s = new AIPerceptionState();
            Spot(s, "u1", 1);
            s.RemoveUnit("u1");

            Assert.AreEqual(SpottedLevel.Level0, s.LevelOf("u1"));
            Assert.IsNull(s.GetGhost("u1"), "a watched kill leaves no ghost");
        }

        [Test]
        public void RefreshContact_UpdatesTrackWithoutLevelGain()
        {
            var s = new AIPerceptionState();
            Spot(s, "u1", 1);

            s.RefreshContact("u1", P(6, 5), currentTurn: 2);

            Assert.AreEqual(SpottedLevel.Level1, s.LevelOf("u1"), "no level increment on refresh");
            Assert.AreEqual(P(6, 5), s.GetContact("u1").LastKnownPos);
            Assert.AreEqual(2, s.GetContact("u1").LastSeenTurn);
        }
    }
}
