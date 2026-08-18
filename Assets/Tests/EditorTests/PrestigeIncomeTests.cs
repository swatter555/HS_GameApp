using HammerAndSickle.Controllers;
using HammerAndSickle.Models.Map;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the §18.2 per-turn income arithmetic (V7) — BattleManager.ComputeIncome, the
    /// static pure helper ProcessUpkeep calls: stipend floor, rate × held value, the high-water
    /// progress bonus that pays exactly once, and single end-of-sum rounding.
    /// </summary>
    [TestFixture]
    public class PrestigeIncomeTests : BaseTestFixture
    {
        private static VictoryLedger Held(float playerValue) => new VictoryLedger(playerValue, 0f, 0f);

        [Test]
        public void Income_StipendAlone_IsTheFloor()
        {
            // V7.1: the anti-death-spiral floor — a player holding nothing still gets the stipend.
            int paid = BattleManager.ComputeIncome(Held(0f), stipend: 20, incomeRate: 0.05f,
                progressBonusRate: 0.5f, highWater: 0f, out float hw);

            Assert.AreEqual(20, paid, "No held value → stipend only.");
            Assert.AreEqual(0f, hw, 1e-4f, "High water unchanged at zero.");
        }

        [Test]
        public void Income_RatePaysOnHeldValue()
        {
            // 20 + 100 × 0.05 = 25; no bonus because nothing exceeds the high-water mark.
            int paid = BattleManager.ComputeIncome(Held(100f), stipend: 20, incomeRate: 0.05f,
                progressBonusRate: 0.5f, highWater: 100f, out float hw);

            Assert.AreEqual(25, paid);
            Assert.AreEqual(100f, hw, 1e-4f);
        }

        [Test]
        public void Income_ProgressBonus_PaysOnlyOnNewGround_AndRatchets()
        {
            // Turn 1: 40 above the mark → bonus 40 × 0.5 = 20 on top of 20 + 140 × 0.05 = 27 → 47.
            int paid1 = BattleManager.ComputeIncome(Held(140f), 20, 0.05f, 0.5f,
                highWater: 100f, out float hw1);
            Assert.AreEqual(47, paid1, "20 stipend + 7 rate + 20 progress bonus.");
            Assert.AreEqual(140f, hw1, 1e-4f, "The mark ratchets up to the new holding.");

            // Turn 2, same holding: the SAME value pays no bonus again (V7.2 — this is the anti-farm).
            int paid2 = BattleManager.ComputeIncome(Held(140f), 20, 0.05f, 0.5f,
                highWater: hw1, out float hw2);
            Assert.AreEqual(27, paid2, "Held ground pays rate income only — the bonus was a one-time thump.");
            Assert.AreEqual(140f, hw2, 1e-4f);
        }

        [Test]
        public void Income_LoseThenRetake_PaysNoSecondBonus()
        {
            // Drop from 140 to 60: mark must NOT follow the value down...
            BattleManager.ComputeIncome(Held(60f), 0, 0f, 0.5f, highWater: 140f, out float hwAfterLoss);
            Assert.AreEqual(140f, hwAfterLoss, 1e-4f, "The high-water mark never ratchets down.");

            // ...so retaking the same 140 pays nothing extra (V7.2 — lose-and-retake farming is dead).
            int paid = BattleManager.ComputeIncome(Held(140f), 0, 0f, 0.5f,
                highWater: hwAfterLoss, out float hw);
            Assert.AreEqual(0, paid, "Retaken ground is not NEW ground.");
            Assert.AreEqual(140f, hw, 1e-4f);
        }

        [Test]
        public void Income_RoundsOnce_AtTheEnd()
        {
            // 9 × 0.05 = 0.45 rate + 9 × 0.05 = 0.45 bonus = 0.9 → rounds to 1.
            // Per-component rounding would truncate both halves to 0 — the V7.3 failure.
            int paid = BattleManager.ComputeIncome(Held(9f), stipend: 0, incomeRate: 0.05f,
                progressBonusRate: 0.05f, highWater: 0f, out _);

            Assert.AreEqual(1, paid, "Components sum in double and round ONCE — 0.45 + 0.45 must pay 1, not 0.");
        }

        [Test]
        public void Income_AllZeroKnobs_PaysNothing()
        {
            int paid = BattleManager.ComputeIncome(Held(500f), 0, 0f, 0f, highWater: 0f, out float hw);

            Assert.AreEqual(0, paid, "A scenario declaring no economy pays nothing.");
            Assert.AreEqual(500f, hw, 1e-4f, "The mark still tracks holdings so a later knob change cannot re-pay old gains.");
        }
    }
}
