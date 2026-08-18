using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for VictoryLedger.Compute (V5): sums by controller over EVERY hex (no flag gate),
    /// neutral value stays in the denominator, zero-total and null maps are legitimate zero ledgers,
    /// negative authored values are ignored (MapLoader warns; scoring treats them as 0).
    /// </summary>
    [TestFixture]
    public class VictoryLedgerTests : BaseTestFixture
    {
        private const float Eps = 1e-4f;

        #region Degenerate inputs (V5.4 / V5.5)

        [Test]
        public void Compute_NullMap_YieldsZeroLedger()
        {
            var ledger = VictoryLedger.Compute(null);

            Assert.AreEqual(0f, ledger.TotalValue, Eps, "A null map must yield the zero ledger, not throw.");
            Assert.AreEqual(0f, ledger.PlayerShare, Eps, "PlayerShare must be 0 when TotalValue is 0 — no division.");
        }

        [Test]
        public void Compute_MapWithNoValue_IsLegitimateZero()
        {
            // V5.4: every currently shipped map is in this state — it must be quiet and safe.
            var map = MapFixtures.UniformMap();

            var ledger = VictoryLedger.Compute(map);

            Assert.AreEqual(0f, ledger.TotalValue, Eps, "An unscored map is legitimate, not exceptional.");
            Assert.AreEqual(0f, ledger.PlayerShare, Eps);
        }

        [Test]
        public void DefaultStruct_IsZeroLedger()
        {
            var ledger = default(VictoryLedger);

            Assert.AreEqual(0f, ledger.TotalValue, Eps);
            Assert.AreEqual(0f, ledger.PlayerShare, Eps, "The default struct must be safe to read before the first Compute.");
        }

        #endregion // Degenerate inputs

        #region Sums and shares

        [Test]
        public void Compute_SumsByController_NoFlagGate()
        {
            var map = MapFixtures.UniformMap();
            MapFixtures.SetVictory(map, 1, 1, 30f, TileControl.Red);
            MapFixtures.SetVictory(map, 2, 1, 20f, TileControl.Red);
            MapFixtures.SetVictory(map, 3, 1, 40f, TileControl.Blue);
            MapFixtures.SetVictory(map, 4, 1, 10f, TileControl.Grey);

            var ledger = VictoryLedger.Compute(map);

            Assert.AreEqual(50f, ledger.PlayerValue, Eps, "Red hexes sum for the player — no IsObjective gate (Bob's ruling).");
            Assert.AreEqual(40f, ledger.EnemyValue, Eps);
            Assert.AreEqual(10f, ledger.NeutralValue, Eps);
            Assert.AreEqual(100f, ledger.TotalValue, Eps);
            Assert.AreEqual(0.5f, ledger.PlayerShare, Eps);
        }

        [Test]
        public void Compute_NeutralValue_StaysInDenominator()
        {
            // V5.2: neutral ground credits nobody but still dilutes both shares — a map with real
            // neutral value correctly starts both sides below 50%.
            var map = MapFixtures.UniformMap();
            MapFixtures.SetVictory(map, 1, 1, 50f, TileControl.Red);
            MapFixtures.SetVictory(map, 2, 2, 50f, TileControl.Grey);

            var ledger = VictoryLedger.Compute(map);

            Assert.AreEqual(0.5f, ledger.PlayerShare, Eps,
                "Holding 50 of 100 total is a 0.5 share even though the other 50 is neutral, not enemy.");
        }

        [Test]
        public void Compute_NoneControl_CountsAsNeutral()
        {
            var map = MapFixtures.UniformMap();
            MapFixtures.SetVictory(map, 1, 1, 25f, TileControl.None);

            var ledger = VictoryLedger.Compute(map);

            Assert.AreEqual(25f, ledger.NeutralValue, Eps, "None (authoring placeholder) buckets with Grey.");
            Assert.AreEqual(0f, ledger.PlayerValue, Eps);
        }

        [Test]
        public void Compute_NegativeValue_IgnoredAsZero()
        {
            // Ruled 2026-08-17 (todo_prestige Stage 1): MapLoader warns at load; scoring skips.
            var map = MapFixtures.UniformMap();
            MapFixtures.SetVictory(map, 1, 1, 30f, TileControl.Red);
            MapFixtures.SetVictory(map, 2, 1, -50f, TileControl.Red);

            var ledger = VictoryLedger.Compute(map);

            Assert.AreEqual(30f, ledger.PlayerValue, Eps, "A negative authored value must not subtract.");
            Assert.AreEqual(30f, ledger.TotalValue, Eps);
        }

        #endregion // Sums and shares
    }
}
