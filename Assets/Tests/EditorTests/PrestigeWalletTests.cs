using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for PrestigeWallet (V8): Add credits balance AND tally together, TrySpend is an
    /// atomic check-and-debit that mutates nothing on refusal, Seed is the battle-start/reset state.
    /// The wallet is the headless-testable core of BattleManager's prestige surface.
    /// </summary>
    [TestFixture]
    public class PrestigeWalletTests : BaseTestFixture
    {
        #region Seed

        [Test]
        public void Seed_SetsPool_ZeroesTallies()
        {
            var wallet = new PrestigeWallet();
            wallet.Add(10);
            wallet.TrySpend(5);

            wallet.Seed(500);

            Assert.AreEqual(500, wallet.Current, "Seed puts the manifest pool in hand.");
            Assert.AreEqual(0, wallet.Earned, "Seed clears the earned tally — the pool is granted, not earned.");
            Assert.AreEqual(0, wallet.Spent, "Seed clears the spent tally.");
        }

        [Test]
        public void Seed_NegativePool_ClampsToZero()
        {
            var wallet = new PrestigeWallet();

            wallet.Seed(-100);

            Assert.AreEqual(0, wallet.Current, "A negative pool cannot open a debt.");
        }

        #endregion // Seed

        #region Add (V8.1)

        [Test]
        public void Add_CreditsBalanceAndTally_Together()
        {
            var wallet = new PrestigeWallet();
            wallet.Seed(100);

            int credited = wallet.Add(40);

            Assert.AreEqual(40, credited, "Add reports the true delta for the change event.");
            Assert.AreEqual(140, wallet.Current, "V8.1: the spendable balance is credited...");
            Assert.AreEqual(40, wallet.Earned, "...AND the earned tally — the old code fed only the tally.");
        }

        [Test]
        public void Add_NonPositive_IsNoOp()
        {
            var wallet = new PrestigeWallet();
            wallet.Seed(100);

            Assert.AreEqual(0, wallet.Add(0));
            Assert.AreEqual(0, wallet.Add(-25));
            Assert.AreEqual(100, wallet.Current, "Non-positive credits must not move the balance.");
            Assert.AreEqual(0, wallet.Earned);
        }

        #endregion // Add

        #region TrySpend (V8.2)

        [Test]
        public void TrySpend_SufficientFunds_DebitsAndTallies()
        {
            var wallet = new PrestigeWallet();
            wallet.Seed(100);

            bool ok = wallet.TrySpend(60);

            Assert.IsTrue(ok);
            Assert.AreEqual(40, wallet.Current);
            Assert.AreEqual(60, wallet.Spent);
        }

        [Test]
        public void TrySpend_InsufficientFunds_RefusesAndMutatesNothing()
        {
            var wallet = new PrestigeWallet();
            wallet.Seed(50);

            bool ok = wallet.TrySpend(51);

            Assert.IsFalse(ok, "The purchase flow relies on the atomic refusal (V8.2).");
            Assert.AreEqual(50, wallet.Current, "A refused spend must leave the balance untouched.");
            Assert.AreEqual(0, wallet.Spent, "A refused spend must leave the tally untouched.");
        }

        [Test]
        public void TrySpend_ExactBalance_Succeeds()
        {
            var wallet = new PrestigeWallet();
            wallet.Seed(75);

            Assert.IsTrue(wallet.TrySpend(75), "Spending to exactly zero is a legal purchase.");
            Assert.AreEqual(0, wallet.Current);
        }

        [Test]
        public void TrySpend_NonPositive_Refuses()
        {
            var wallet = new PrestigeWallet();
            wallet.Seed(100);

            Assert.IsFalse(wallet.TrySpend(0), "A zero spend is a caller bug, not a free success.");
            Assert.IsFalse(wallet.TrySpend(-10), "A negative spend must never credit.");
            Assert.AreEqual(100, wallet.Current);
            Assert.AreEqual(0, wallet.Spent);
        }

        #endregion // TrySpend
    }
}
