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
    /// `IntelReportStats` at all. That is the whole argument for these two tests: they are the only
    /// place either mistake becomes visible before it ships inside a `.oob`.
    ///
    /// Added 2026-08-12 with the Lowlands unit-DB expansion, on the scenario editor agent's suggestion.
    /// </summary>
    [TestFixture]
    public class CensusIntegrityTests : BaseTestFixture
    {
        #region Exemptions

        /// <summary>
        /// Profiles that legitimately declare NO census, documented at `PrinterMessage.cs` §loss rows.
        ///
        /// ⚠ TRUCKS ARE ABSENT FROM THE INTEL MODEL BY DESIGN (Bob, 2026-07-28), not by oversight — no
        /// truck profile declares intel stats, so trucks are unreportable because there is no data, and
        /// adding a census to one would silently file it under the AIRCRAFT loss row (the TRN bucket
        /// catches both the TRN_ and TRK_ prefixes). `TRN_NAVAL` is the shared sealift draw and holds
        /// nothing of its own for the same reason.
        ///
        /// ⚠ THIS LIST IS AN ALLOW-LIST, NOT A TODO. Adding a member here asserts "this profile is
        /// deliberately equipment-less"; it must never be used to quiet a profile whose census was
        /// simply forgotten.
        /// </summary>
        private static readonly HashSet<WeaponType> CensusExempt = new HashSet<WeaponType>
        {
            WeaponType.TRK_GEN_SV,
            WeaponType.TRK_GEN_ARAB,
            WeaponType.TRK_WEST,
            WeaponType.TRN_NAVAL,
        };

        /// <summary>Every WeaponType that actually resolves to a registered profile.</summary>
        private static IEnumerable<WeaponType> RegisteredProfileTypes() =>
            Enum.GetValues(typeof(WeaponType))
                .Cast<WeaponType>()
                .Where(t => t != WeaponType.NONE)
                .Where(WeaponProfileDB.HasWeaponProfile);

        #endregion // Exemptions

        #region Tests

        /// <summary>
        /// Every registered profile declares a non-empty census, bar the documented truck/sealift
        /// exemptions. Catches the silent-empty case: a unit that dies without appearing in the report.
        /// </summary>
        [Test]
        public void EveryRegisteredProfile_DeclaresANonEmptyCensus()
        {
            var offenders = RegisteredProfileTypes()
                .Where(t => !CensusExempt.Contains(t))
                .Where(t =>
                {
                    var stats = WeaponProfileDB.GetWeaponProfile(t)?.IntelReportStats;
                    return stats == null || stats.Count == 0;
                })
                .ToList();

            Assert.IsEmpty(offenders,
                "These profiles declare no equipment, so a regiment holding one can be destroyed and " +
                "contribute nothing to the §24.8 loss report:\n  " +
                string.Join("\n  ", offenders.Select(t => t.ToString())) +
                "\nEither give the profile a census, or add it to CensusExempt with the reason it holds nothing.");
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

        #endregion // Tests
    }
}
