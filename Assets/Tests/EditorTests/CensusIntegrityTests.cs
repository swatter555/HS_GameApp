using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Guards the CENSUS — a weapon profile's <c>IntelReportStats</c>, the roster of equipment a
    /// full-strength regiment holding that profile is made of.
    ///
    /// ⚠ WHY THIS FIXTURE EXISTS. A census is not decoration: it is the multiplicand of the §24.8 loss
    /// ledger. `GameDataManager.BookLosses` computes `lost[type] = TotalIntelStats[type] × (hpLost /
    /// HitPoints.Max)`, so a profile with an EMPTY census produces a unit that can be annihilated and
    /// contribute nothing to the loss report, and a census token with an unrecognised prefix is dropped
    /// by `EquipmentBays.ClassifyWeaponType` from BOTH the intel report and the loss report.
    ///
    /// ⚠ BOTH FAILURES ARE SILENT AND IDENTICAL IN PLAY — the unit dies and the report says nothing.
    /// Neither throws, neither logs, and before this fixture nothing under Assets/Tests referenced
    /// `IntelReportStats` at all. That is the whole argument for this fixture: it is the only place
    /// these mistakes become visible before they ship inside a `.oob`.
    ///
    /// Added 2026-08-12 with the Lowlands unit-DB expansion, on the scenario editor agent's suggestion.
    /// Extended 2026-08-13 (census pass Block 6) with the four DOCTRINE GUARDS — carrier-clean,
    /// lift-empty, truck-empty, classification coherence — which machine-check census doctrine v2
    /// (todo_census.md §0) over every authored template. On a failure here: FIX THE DATA, NEVER THE
    /// GUARD — a failure names a profile the fix pass missed.
    /// </summary>
    [TestFixture]
    public class CensusIntegrityTests : BaseTestFixture
    {
        #region Setup + Exemptions

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // BaseTestFixture reaches GameDataManager.Instance, which initializes both DBs — but the
            // template-driven guards below pass VACUOUSLY over an empty CombatUnitDB, so make the
            // dependency explicit rather than inherited by accident.
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized)
                CombatUnitDB.Initialize();
        }

        /// <summary>
        /// The two RULE-BASED census exemptions (doctrine v2, census pass 2026-08-13). These replaced
        /// the old name-based `CensusExempt` allow-list: a rule cannot silently rot the way a hand-kept
        /// name list can, and a new lift or truck profile is exempt the day it is authored.
        ///
        /// ⚠ RULE 4 — LIFT CARRIES NO CENSUS, EVER: lift losses are unreported by design (a debarked
        /// VDV regiment must not lose transport planes in ground combat).
        /// ⚠ RULE 5 — TRUCKS ARE NEVER COUNTED (Bob, 2026-07-28, permanent law): a census on a TRK_/TRN_
        /// profile would silently file under the AIRCRAFT loss row (the TRN bucket catches both
        /// prefixes). `TRN_NAVAL` is the shared sealift draw and holds nothing for the same reason.
        /// </summary>
        private static bool IsLiftProfile(WeaponType type) =>
            (WeaponProfileDB.GetWeaponProfile(type)?.TransportCategory ?? TransportCategory.None)
                != TransportCategory.None;

        private static bool IsTruckOrTransportProfile(WeaponType type) =>
            type.ToString().StartsWith("TRK_", StringComparison.Ordinal) ||
            type.ToString().StartsWith("TRN_", StringComparison.Ordinal);

        private static bool HasEmptyCensus(WeaponType type)
        {
            var stats = WeaponProfileDB.GetWeaponProfile(type)?.IntelReportStats;
            return stats == null || stats.Count == 0;
        }

        /// <summary>Every WeaponType that actually resolves to a registered profile.</summary>
        private static IEnumerable<WeaponType> RegisteredProfileTypes() =>
            Enum.GetValues(typeof(WeaponType))
                .Cast<WeaponType>()
                .Where(t => t != WeaponType.NONE)
                .Where(WeaponProfileDB.HasWeaponProfile);

        /// <summary>Every authored template, materialized once per assertion.</summary>
        private static IEnumerable<CombatUnit> AllTemplates() =>
            CombatUnitDB.GetAllTemplateIds()
                .Select(CombatUnitDB.GetUnitTemplate)
                .Where(t => t != null);

        #endregion // Setup + Exemptions

        #region Tests

        /// <summary>
        /// Every registered profile declares a non-empty census, bar the documented truck/sealift
        /// exemptions. Catches the silent-empty case: a unit that dies without appearing in the report.
        /// </summary>
        [Test]
        public void EveryRegisteredProfile_DeclaresANonEmptyCensus()
        {
            var offenders = RegisteredProfileTypes()
                .Where(t => !IsLiftProfile(t) && !IsTruckOrTransportProfile(t))
                .Where(HasEmptyCensus)
                .ToList();

            Assert.IsEmpty(offenders,
                "These profiles declare no equipment, so a regiment holding one can be destroyed and " +
                "contribute nothing to the §24.8 loss report:\n  " +
                string.Join("\n  ", offenders.Select(t => t.ToString())) +
                "\nEither give the profile a census, or — if it is genuinely equipment-less — it should " +
                "be a lift (TransportCategory) or TRK_/TRN_ profile, which the rules exempt.");
        }

        /// <summary>
        /// Every token appearing in any census classifies to a real equipment bucket.
        /// </summary>
        /// <remarks>
        /// ⚠ `ClassifyWeaponType` is PREFIX-MATCHED and falls through to `EquipmentBucket.None` for an
        /// unrecognised name — silently, from both the intel report and the loss report. So a typo'd or
        /// novel-prefixed token in a census is invisible today: the equipment simply does not exist as
        /// far as either report is concerned. This is the test that makes it visible.
        /// </remarks>
        [Test]
        public void EveryCensusToken_ClassifiesToARealBucket()
        {
            var offenders = RegisteredProfileTypes()
                .SelectMany(profileType =>
                    (WeaponProfileDB.GetWeaponProfile(profileType)?.IntelReportStats
                        ?? new Dictionary<WeaponType, int>())
                    .Keys
                    .Where(token => EquipmentBays.ClassifyWeaponType(token) == EquipmentBucket.None)
                    .Select(token => $"{profileType} lists {token}"))
                .ToList();

            Assert.IsEmpty(offenders,
                "These census entries classify to EquipmentBucket.None, so they are dropped from BOTH " +
                "the intel report and the loss report without any error:\n  " +
                string.Join("\n  ", offenders) +
                "\nEither the token is misspelled, or its prefix needs an arm in EquipmentBays.ClassifyWeaponType.");
        }

        /// <summary>
        /// DOCTRINE RULE 2 — a carrier's census lists its OWN platform count and nothing else.
        /// </summary>
        /// <remarks>
        /// A carrier is any profile a template mounts in its Mobile bay. Because `BuildIntelStats`
        /// bay-SUMS, anything else a carrier lists is silently added to whatever formation mounts it —
        /// the §7.2 double-count disease this pass cured (a BMP-1 carrier used to add 40 tanks to every
        /// MRR that rode it, on top of the base profile's own 40).
        /// </remarks>
        [Test]
        public void EveryMobileBayCarrier_CensusListsOnlyItsOwnType()
        {
            var offenders = AllTemplates()
                .Select(t => t.EquipmentBays?.Mobile ?? WeaponType.NONE)
                .Where(mt => mt != WeaponType.NONE)
                .Distinct()
                .SelectMany(mt =>
                    (WeaponProfileDB.GetWeaponProfile(mt)?.IntelReportStats
                        ?? new Dictionary<WeaponType, int>())
                    .Keys
                    .Where(k => k != mt)
                    .Select(k => $"{mt} lists {k}"))
                .ToList();

            Assert.IsEmpty(offenders,
                "These Mobile-bay carrier censuses list equipment beyond the carrier's own platform, " +
                "which bay-summing double-counts into every formation that mounts them (rule 2):\n  " +
                string.Join("\n  ", offenders) +
                "\nMove the equipment to the base (deployed) profile's census.");
        }

        /// <summary>
        /// DOCTRINE RULE 4 — lift (`TransportCategory != None`) carries NO census, ever. Lift losses
        /// are unreported by design: a debarked air-mobile regiment must not lose its transport
        /// aircraft in ground combat.
        /// </summary>
        [Test]
        public void EveryLiftProfile_DeclaresAnEmptyCensus()
        {
            var offenders = RegisteredProfileTypes()
                .Where(IsLiftProfile)
                .Where(t => !HasEmptyCensus(t))
                .ToList();

            Assert.IsEmpty(offenders,
                "These lift profiles declare a census; lift is never censused (rule 4):\n  " +
                string.Join("\n  ", offenders.Select(t => t.ToString())));
        }

        /// <summary>
        /// DOCTRINE RULE 5 — trucks are never counted (permanent law, Bob 2026-07-28). A TRK_/TRN_
        /// census would silently file under the AIRCRAFT loss row via the TRN bucket.
        /// </summary>
        [Test]
        public void EveryTruckAndTransportProfile_DeclaresAnEmptyCensus()
        {
            var offenders = RegisteredProfileTypes()
                .Where(IsTruckOrTransportProfile)
                .Where(t => !HasEmptyCensus(t))
                .ToList();

            Assert.IsEmpty(offenders,
                "These TRK_/TRN_ profiles declare a census; trucks and transports are never counted " +
                "(rule 5):\n  " + string.Join("\n  ", offenders.Select(t => t.ToString())));
        }

        /// <summary>
        /// DOCTRINE RULE 1 coherence — a template's bay-summed census must contain the equipment
        /// bucket its Classification implies: a MECH regiment with no tanks, or an artillery regiment
        /// with no guns, is a roster contradicting its own counter.
        /// </summary>
        /// <remarks>
        /// The mapping is deliberately SMALL — only classifications whose implication is unambiguous.
        /// Facilities, air, HQ and ENG carry no implied ground roster and are simply absent from the
        /// table; Mujahideen templates are exempt as irregulars (their formations are deliberately
        /// ragged). MECH/MOT are the special case: they imply BOTH organic tanks AND a carrier
        /// (APC or IFV) somewhere in the bay-sum.
        /// </remarks>
        [Test]
        public void EveryTemplate_BaySummedCensus_ContainsItsClassificationsBucket()
        {
            // Classification → the single bucket its bay-sum must contain (MECH/MOT special-cased).
            var implies = new Dictionary<UnitClassification, EquipmentBucket>
            {
                { UnitClassification.TANK,  EquipmentBucket.TANK },
                { UnitClassification.ART,   EquipmentBucket.ART },
                { UnitClassification.SPA,   EquipmentBucket.ART },
                { UnitClassification.SAM,   EquipmentBucket.SAM },
                { UnitClassification.SPSAM, EquipmentBucket.SAM },
                { UnitClassification.AAA,   EquipmentBucket.AAA },
                { UnitClassification.SPAAA, EquipmentBucket.AAA },
                { UnitClassification.RECON, EquipmentBucket.RCN },
            };

            var templates = AllTemplates().ToList();
            Assert.IsNotEmpty(templates,
                "CombatUnitDB returned no templates — this test would pass vacuously.");

            var offenders = new List<string>();
            foreach (var template in templates)
            {
                if (template.Nationality == Nationality.MJ)
                    continue; // irregulars — deliberately ragged formations

                bool isMech = template.Classification == UnitClassification.MECH
                           || template.Classification == UnitClassification.MOT;
                if (!isMech && !implies.ContainsKey(template.Classification))
                    continue; // no unambiguous implication — exempt by absence from the table

                var buckets = new HashSet<EquipmentBucket>(new[]
                    {
                        template.EquipmentBays?.Deployed ?? WeaponType.NONE,
                        template.EquipmentBays?.Mobile ?? WeaponType.NONE,
                        template.EquipmentBays?.Embarked ?? WeaponType.NONE,
                    }
                    .Where(slot => slot != WeaponType.NONE)
                    .SelectMany(slot =>
                        (WeaponProfileDB.GetWeaponProfile(slot)?.IntelReportStats
                            ?? new Dictionary<WeaponType, int>()).Keys)
                    .Select(EquipmentBays.ClassifyWeaponType));

                bool ok = isMech
                    ? buckets.Contains(EquipmentBucket.TANK)
                        && (buckets.Contains(EquipmentBucket.APC) || buckets.Contains(EquipmentBucket.IFV))
                    : buckets.Contains(implies[template.Classification]);

                if (!ok)
                {
                    offenders.Add($"[{template.Classification}] {template.UnitName} — buckets: " +
                        string.Join(", ", buckets.OrderBy(b => b)));
                }
            }

            Assert.IsEmpty(offenders,
                "These templates' bay-summed censuses lack the bucket their Classification implies " +
                "(rule 1 coherence):\n  " + string.Join("\n  ", offenders) +
                "\nFix the DATA (the base profile's census), never this guard.");
        }

        #endregion // Tests
    }
}
