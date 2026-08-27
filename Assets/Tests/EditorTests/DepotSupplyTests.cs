using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// ONE SUPPLY NUMBER PER UNIT (SUP-1, Bob's ruling 2026-08-24; plan `Planning Docs/Supply
    /// Unification.md`). `DaysSupply` is THE pool everything deducts from — only the caps differ:
    /// regiment 5 · airbase 30 · depot 30/50/80/110 by size · **fixed-wing 0** (no own supply; the
    /// launching airbase pays, §10.3.1/§15.1.2). These pins guard the cap ladder at construction, the
    /// pool arithmetic the §15 pass will call, the upgrade refill, and the two rules that fail SILENTLY
    /// if lost: a fixed-wing must not be supply-gated out of movement (the `Max > 0` guard in `CanMove`),
    /// and a helicopter is NOT fixed-wing and keeps its 5. Replaces `DepotStockpileTests`, which pinned
    /// the deleted dual-number model.
    /// </summary>
    [TestFixture]
    public class DepotSupplyTests
    {
        #region Helpers

        private static CombatUnit MakeDepot(DepotSize size = DepotSize.Small) =>
            new CombatUnit("TestDepot", UnitClassification.DEPOT, UnitRole.GroundCombatStatic,
                Side.AI, Nationality.MJ, category: DepotCategory.Secondary, size: size);

        private static CombatUnit Make(UnitClassification cls, UnitRole role = UnitRole.GroundCombat) =>
            new CombatUnit("TestUnit", cls, role, Side.Player, Nationality.USSR);

        #endregion // Helpers

        #region The cap ladder at construction

        [Test]
        public void Caps_FollowTheLadder_Regiment5_Airbase30_DepotBySize()
        {
            Assert.That(Make(UnitClassification.INF).DaysSupply.Max, Is.EqualTo(GameData.MaxDaysSupplyUnit),
                "a line regiment caps at 5");
            Assert.That(Make(UnitClassification.AIRB, UnitRole.GroundCombatStatic).DaysSupply.Max,
                Is.EqualTo(GameData.MaxDaysSupplyAirbase), "an airbase caps at 30 — its pool IS its sortie stockpile");
            Assert.That(MakeDepot(DepotSize.Small).DaysSupply.Max,
                Is.EqualTo(GameData.MaxStockpileBySize[DepotSize.Small]), "a Small depot caps at 30");
            Assert.That(MakeDepot(DepotSize.Large).DaysSupply.Max,
                Is.EqualTo(GameData.MaxStockpileBySize[DepotSize.Large]), "a Large depot caps at 80");
        }

        [Test]
        public void Depot_ConstructsFull_ToItsSizeCap()
        {
            var depot = MakeDepot(DepotSize.Large);
            Assert.That(depot.DaysSupply.Current, Is.EqualTo(depot.DaysSupply.Max),
                "a scenario depot starts full — an .oob without an authored value relies on this default");
        }

        [Test]
        public void FixedWing_CarriesNoSupply_MaxZero()
        {
            var jet = Make(UnitClassification.ATT, UnitRole.AirGroundAttack);
            Assert.That(jet.DaysSupply.Max, Is.EqualTo(0f),
                "fixed-wing carry NO own supply (§10.3.1) — the launching airbase pays per §11.2.3");
            Assert.That(jet.DaysSupply.Current, Is.EqualTo(0f));
        }

        [Test]
        public void Helicopter_IsNotFixedWing_AndKeepsItsFive()
        {
            Assert.That(Make(UnitClassification.HELO, UnitRole.GroundCombat).DaysSupply.Max,
                Is.EqualTo(GameData.MaxDaysSupplyUnit),
                "§15.1.2 caps ground combat units INCLUDING helicopters at 5 — a helo is a ground-domain regiment");
        }

        #endregion // The cap ladder at construction

        #region The fixed-wing movement guard — fails silently if lost

        [Test]
        public void FixedWing_AtZeroSupply_IsNotSupplyGatedOutOfMovement()
        {
            var jet = Make(UnitClassification.ATT, UnitRole.AirGroundAttack);

            Assert.That(jet.CanMove(), Is.True,
                "a unit with NO supply store (Max 0) must not be supply-gated — without the Max > 0 guard " +
                "no aircraft could ever be ordered to move and unit-cycling would skip every plane");
        }

        [Test]
        public void GroundUnit_AtZeroSupply_IsStillSupplyGated()
        {
            var inf = Make(UnitClassification.INF);
            inf.DaysSupply.SetCurrent(0f);

            Assert.That(inf.CanMove(), Is.False,
                "the guard exempts ONLY empty-store units — a starving regiment (Max 5, Current 0) still cannot move");
        }

        #endregion // The fixed-wing movement guard — fails silently if lost

        #region Pool arithmetic — what the §15 pass will call

        [Test]
        public void ReceiveSupplies_ClampsAtTheCap()
        {
            var depot = MakeDepot(DepotSize.Small);
            depot.DaysSupply.SetCurrent(25f);

            float received = depot.ReceiveSupplies(20f);

            Assert.That(received, Is.EqualTo(5f), "only the headroom to the 30-day cap is accepted");
            Assert.That(depot.DaysSupply.Current, Is.EqualTo(depot.DaysSupply.Max));
        }

        [Test]
        public void ConsumeSupplies_DeductsFromTheOnePool()
        {
            var depot = MakeDepot(DepotSize.Large);

            bool ok = depot.ConsumeSupplies(12.5f);

            Assert.That(ok, Is.True);
            Assert.That(depot.DaysSupply.Current, Is.EqualTo(67.5f),
                "distribution and consumption come out of the SAME number — that is the whole ruling");
        }

        [Test]
        public void UpgradeDepotSize_RaisesTheCapAndRefills()
        {
            var depot = MakeDepot(DepotSize.Small);
            depot.ConsumeSupplies(10f); // 20/30 before the upgrade

            bool ok = depot.UpgradeDepotSize();

            Assert.That(ok, Is.True);
            Assert.That(depot.DaysSupply.Max, Is.EqualTo(GameData.MaxStockpileBySize[DepotSize.Medium]));
            Assert.That(depot.DaysSupply.Current, Is.EqualTo(depot.DaysSupply.Max),
                "upgrade refills to the new cap — the pre-unification refill-on-upgrade behavior, preserved");
        }

        #endregion // Pool arithmetic — what the §15 pass will call
    }
}
