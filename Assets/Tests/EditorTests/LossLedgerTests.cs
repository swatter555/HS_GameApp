using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.UI;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The equipment loss ledger (printer P5) — HP lost converted into weapon systems lost.
    ///
    /// THE MODEL: a unit's `RegimentProfile.TotalIntelStats` is its FULL-STRENGTH roster of weapon systems,
    /// and §12.2.6 scales that by currentHP/maxHP for display. So a unit that loses X% of its hit points has
    /// lost X% of its equipment, and `CombatUnit.TakeDamage` books it.
    ///
    /// The tests that matter here are the arithmetic ones. A loss ledger fails QUIETLY — it under-reports,
    /// and nobody notices until someone reads a battle report that says a destroyed tank regiment lost no
    /// tanks. Each case below pins one way that can happen.
    /// </summary>
    [TestFixture]
    public class LossLedgerTests
    {

        #region Setup

        [SetUp]
        public void SetUp() => GameDataManager.ClearLossLedger();

        [TearDown]
        public void TearDown() => GameDataManager.ClearLossLedger();

        /// <summary>
        /// A unit whose full-strength roster is exactly the supplied weapon systems, at full HP.
        /// Built through the real CombatUnit/RegimentProfile path so the test exercises the same
        /// TotalIntelStats the game reads, not a stand-in.
        /// </summary>
        private static CombatUnit MakeUnit(Side side, params (WeaponType type, int count)[] roster)
        {
            var unit = new CombatUnit("TestUnit", UnitClassification.INF, UnitRole.GroundCombat,
                side, Nationality.USSR);

            unit.HitPoints.SetCurrent(unit.HitPoints.Max);

            // Several cases below use absolute damage figures (10 of 40, 20 of 40) whose expected results
            // only hold at the documented mobile-unit MAX_HP. Assert it once here so a changed constant
            // fails with its real cause instead of looking like broken ledger arithmetic.
            Assert.That(unit.HitPoints.Max, Is.EqualTo((float)GameData.MAX_HP).Within(0.001f),
                "these tests assume the documented mobile-unit MAX_HP");

            // TotalIntelStats is set directly rather than built from real WeaponProfiles: the ledger's
            // contract is "whatever the full-strength roster says", and a hand-set roster makes the
            // expected arithmetic obvious instead of coupling these tests to WeaponProfileDB's contents.
            var stats = new Dictionary<WeaponType, int>();
            foreach ((WeaponType type, int count) in roster)
                stats[type] = count;

            unit.RegimentProfile.TotalIntelStats = stats;
            return unit;
        }

        private static float LossOf(Side side, WeaponType type)
        {
            var ledger = GameDataManager.GetLossLedger(side);
            return ledger.TryGetValue(type, out float v) ? v : 0f;
        }

        #endregion // Setup

        #region Proportional arithmetic

        [Test]
        public void HalfHitPointsLost_BooksHalfTheEquipment()
        {
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 30));

            unit.TakeDamage(unit.HitPoints.Max / 2f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(15f).Within(0.001f));
        }

        [Test]
        public void DamageAccumulatesAcrossSeparateEvents()
        {
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40));

            unit.TakeDamage(10f);
            unit.TakeDamage(10f);

            // 20 of 40 HP across two events = half the roster, exactly as one 20-point blow would give.
            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void EachWeaponTypeIsBookedIndependently()
        {
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 30), (WeaponType.APC_BTR70_SV, 10));

            unit.TakeDamage(unit.HitPoints.Max / 2f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(15f).Within(0.001f));
            Assert.That(LossOf(Side.Player, WeaponType.APC_BTR70_SV), Is.EqualTo(5f).Within(0.001f));
        }

        #endregion // Proportional arithmetic

        #region The rounding trap — the reason values are float

        [Test]
        public void SmallDamageIsNotLostToRounding()
        {
            // 3 tanks, 1 HP of 40 = 0.075 of a tank. Under an int ledger this rounds to ZERO and the
            // contribution disappears — the exact defect the float ledger exists to prevent.
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 3));
            float expected = 3f * (1f / unit.HitPoints.Max);

            unit.TakeDamage(1f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.GreaterThan(0f),
                "a small hit must contribute a FRACTION, not be rounded away");
            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ManySmallHitsKillingAUnit_BookItsEntireRoster()
        {
            // The failure this guards: a unit ground down 1 HP at a time reporting zero losses because
            // every individual event rounded to nothing. Forty 1-HP hits must total the whole roster.
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 3));

            for (int i = 0; i < (int)unit.HitPoints.Max; i++)
                unit.TakeDamage(1f);

            Assert.That(unit.HitPoints.Current, Is.EqualTo(0f).Within(0.001f));
            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(3f).Within(0.001f));
        }

        #endregion // The rounding trap

        #region Overkill

        [Test]
        public void Overkill_BooksOnlyWhatTheUnitStillHad()
        {
            // A unit on 3 HP hit for 20 loses 3 HP of equipment, not 20. Booking the requested damage
            // instead of the HP actually removed would over-report losses on every single kill.
            const int roster = 40;
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, roster));
            unit.HitPoints.SetCurrent(3f);
            float expected = roster * (3f / unit.HitPoints.Max);

            unit.TakeDamage(20f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(expected).Within(0.001f),
                "only the 3 HP the unit still had may be booked, not the 20 requested");
        }

        #endregion // Overkill

        #region Sides

        [Test]
        public void LossesAreBookedAgainstTheOwningSide()
        {
            var player = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40));
            var ai = MakeUnit(Side.AI, (WeaponType.TANK_T72A_SV, 40));

            player.TakeDamage(10f);
            ai.TakeDamage(20f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(10f).Within(0.001f));
            Assert.That(LossOf(Side.AI, WeaponType.TANK_T72A_SV), Is.EqualTo(20f).Within(0.001f));
        }

        #endregion // Sides

        #region Non-losses and lifecycle

        [Test]
        public void ZeroDamage_BooksNothing()
        {
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40));

            unit.TakeDamage(0f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(0f));
        }

        [Test]
        public void Repair_DoesNotUnbookLosses()
        {
            // Losses are cumulative history, not a function of current strength: replacements restore the
            // regiment but do not un-destroy the equipment already reported to HQ.
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40));

            unit.TakeDamage(20f);
            unit.Repair(20f);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void SurrenderedUnit_BooksItsRemainingEquipment()
        {
            // §7.9.6a: a unit lost WITHOUT being damaged to zero. No damage event fires, so its surviving
            // equipment would vanish from the report unless booked explicitly.
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40));
            unit.HitPoints.SetCurrent(unit.HitPoints.Max / 2f);

            GameDataManager.RecordRemainingEquipmentAsLost(unit);

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void ClearLossLedger_EmptiesBothSides()
        {
            MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40)).TakeDamage(10f);
            MakeUnit(Side.AI, (WeaponType.TANK_T72A_SV, 40)).TakeDamage(10f);

            GameDataManager.ClearLossLedger();

            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(0f));
            Assert.That(LossOf(Side.AI, WeaponType.TANK_T72A_SV), Is.EqualTo(0f));
        }

        #endregion // Non-losses and lifecycle

        #region Loss report rollup (printer P6)

        /// <summary>Finds the report row starting with the given label.</summary>
        private static string RowFor(PrinterMessage report, string label)
        {
            foreach (string line in report.Lines)
            {
                if (line.StartsWith(label))
                    return line;
            }

            return null;
        }

        private static PrinterMessage BuildReport() => PrinterMessage.CreateLossReport(
            GameDataManager.GetLossLedger(Side.Player),
            GameDataManager.GetLossLedger(Side.AI));

        [Test]
        public void Report_ShowsAllSixRatifiedRows()
        {
            // ⚠ Needs a loss on the books first: since the height fix, an EMPTY report replaces the table
            // with a one-line notice, so there are deliberately no rows to find in that case. The contract
            // being pinned here is "when there ARE losses, all six ratified rows appear" — including the
            // ones sitting at zero, so the report always has the same shape.
            MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 10)).TakeDamage(40f);

            var report = BuildReport();

            foreach (string label in new[] { "Men", "Tanks", "AFVs", "Guns", "Aircraft", "Helicopters" })
                Assert.That(RowFor(report, label), Is.Not.Null, $"missing ratified row '{label}'");
        }

        [Test]
        public void Report_WithNoLosses_SaysSoRatherThanShowingSixZeroes()
        {
            var report = BuildReport();

            Assert.That(report.FullText, Does.Contain("No losses reported"));

            // The notice must be INSIDE the table, not appended under six zero rows — there it fell off
            // the bottom of the CRT and the message said nothing at all.
            Assert.That(RowFor(report, "Helicopters"), Is.Null,
                "an empty report replaces the table rather than trailing below it");
        }

        [Test]
        public void Report_FitsTheCrtHeightBudget()
        {
            // ⚠ MEASURED, NOT GUESSED: the panel shows about ten lines including the frame's
            // "turn: Message from …" header, and the first version overran it (Bob, in play 2026-07-28).
            // Nine total is the budget with a line in hand. This test exists because the overrun was
            // invisible from here — it clips silently, it does not throw.
            MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40)).TakeDamage(40f);
            MakeUnit(Side.AI, (WeaponType.TANK_T72A_SV, 40)).TakeDamage(40f);

            int renderedLines = BuildReport().FullText.Split('\n').Length;

            Assert.That(renderedLines, Is.LessThanOrEqualTo(LOSS_REPORT_LINE_BUDGET),
                $"the loss report must fit {LOSS_REPORT_LINE_BUDGET} rendered lines including the frame header");
        }

        /// <summary>Frame header + heading/column row + six equipment rows.</summary>
        private const int LOSS_REPORT_LINE_BUDGET = 9;

        [Test]
        public void Report_RollsIfvApcAndReconIntoAFVs()
        {
            // AFVs is a COMBINED row; a unit losing IFVs and APCs must show their sum, not either alone.
            var unit = MakeUnit(Side.Player,
                (WeaponType.IFV_BMP1_SV, 10),
                (WeaponType.APC_BTR70_SV, 10));

            unit.TakeDamage(unit.HitPoints.Max);   // total loss → whole roster

            Assert.That(RowFor(BuildReport(), "AFVs"), Does.Contain("20"));
        }

        [Test]
        public void Report_SumsBeforeRounding()
        {
            // THE ROUNDING RULE, end to end. Two contributions of 0.4 of a vehicle each: rounded
            // individually they are 0 + 0 = 0; summed first they are 0.8 → 1. The row must read 1.
            // This is the render-side half of the trap the float ledger guards.
            var ifv = MakeUnit(Side.Player, (WeaponType.IFV_BMP1_SV, 1));
            var apc = MakeUnit(Side.Player, (WeaponType.APC_BTR70_SV, 1));

            ifv.TakeDamage(ifv.HitPoints.Max * 0.4f);
            apc.TakeDamage(apc.HitPoints.Max * 0.4f);

            Assert.That(RowFor(BuildReport(), "AFVs"), Does.Contain("1"),
                "0.4 + 0.4 must round to 1, not to 0");
        }

        [Test]
        public void Report_KeepsTheTwoSidesInSeparateColumns()
        {
            MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 10)).TakeDamage(40f);
            MakeUnit(Side.AI, (WeaponType.TANK_T72A_SV, 20)).TakeDamage(40f);

            string tanks = RowFor(BuildReport(), "Tanks");

            // Ours = 10, enemy = 20, in that column order.
            Assert.That(tanks.IndexOf("10"), Is.LessThan(tanks.IndexOf("20")),
                "OURS must precede ENEMY on the row");
        }

        #endregion // Loss report rollup

        #region Daily ledger

        private static float DailyLossOf(Side side, WeaponType type)
        {
            var ledger = GameDataManager.GetDailyLossLedger(side);
            return ledger.TryGetValue(type, out float v) ? v : 0f;
        }

        [Test]
        public void DailyLedger_TracksAlongsideCumulative()
        {
            MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40)).TakeDamage(10f);

            Assert.That(DailyLossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(10f).Within(0.001f));
            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void StartNewDailyLossPeriod_ClearsDailyButNotCumulative()
        {
            MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40)).TakeDamage(10f);

            GameDataManager.StartNewDailyLossPeriod();

            Assert.That(DailyLossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(0f),
                "a new turn resets the daily tally");
            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(10f).Within(0.001f),
                "the cumulative tally must survive the turn boundary — that is the whole distinction");
        }

        [Test]
        public void DailyLedger_AccumulatesOnlyWithinTheCurrentPeriod()
        {
            var unit = MakeUnit(Side.Player, (WeaponType.TANK_T72A_SV, 40));

            unit.TakeDamage(10f);                          // turn 1
            GameDataManager.StartNewDailyLossPeriod();     // turn boundary
            unit.TakeDamage(4f);                           // turn 2

            Assert.That(DailyLossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(4f).Within(0.001f));
            Assert.That(LossOf(Side.Player, WeaponType.TANK_T72A_SV), Is.EqualTo(14f).Within(0.001f));
        }

        [Test]
        public void DailyReport_IsLabelledForTheTurnAndReportsItsOwnEmptyState()
        {
            var daily = PrinterMessage.CreateLossReport(
                GameDataManager.GetDailyLossLedger(Side.Player),
                GameDataManager.GetDailyLossLedger(Side.AI),
                dailyOnly: true);

            Assert.That(daily.FullText, Does.Contain("TURN LOSSES"));
            Assert.That(daily.FullText, Does.Contain("No losses this turn"));
        }

        #endregion // Daily ledger

        #region Transport aircraft

        [Test]
        public void TransportAircraftLosses_AppearUnderAircraft()
        {
            // The An-12 is the ONLY TRN-bucket profile carrying intel stats, and it is a fixed-wing
            // transport. Absent from every row it would vanish silently — a destroyed transport regiment
            // printing "Aircraft 0".
            var unit = MakeUnit(Side.Player, (WeaponType.TRN_AN8_SV, 48));

            unit.TakeDamage(unit.HitPoints.Max);

            Assert.That(RowFor(BuildReport(), "Aircraft"), Does.Contain("48"));
        }

        #endregion // Transport aircraft
    }
}
