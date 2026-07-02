using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// WP5 validation (pure layer): AOB type-flip + the §11.1.1a target pre-filter + the §11.7 ASB mission map.
    /// The operative class is authoritative for the box type; the drop target narrows the legal set (RB legal
    /// everywhere; AAB/AEWB OpenHex-only); the two compose in IsOperativeLegalForTarget. Pure enum logic.
    /// </summary>
    [TestFixture]
    public class AOBMissionResolverTests
    {
        #region Box type by operative (§11.1.1)

        [Test]
        public void ResolveBoxType_OperativeClassMapsToBox()
        {
            Assert.AreEqual(AOBType.ASB, AOBMissionResolver.ResolveBoxType(UnitClassification.BMB, false), "BMB → ASB");
            Assert.AreEqual(AOBType.ASB, AOBMissionResolver.ResolveBoxType(UnitClassification.ATT, false), "ATT → ASB");
            Assert.AreEqual(AOBType.RB, AOBMissionResolver.ResolveBoxType(UnitClassification.RECONA, false), "RECONA → RB");
            Assert.AreEqual(AOBType.AEWB, AOBMissionResolver.ResolveBoxType(UnitClassification.AWACS, false), "AWACS → AEWB");
        }

        [Test]
        public void ResolveBoxType_TransportDisambiguatedByPayload()
        {
            // §11.9.7 — para aboard → AAB, supply load (no para) → SB. NOT target-driven.
            Assert.AreEqual(AOBType.AAB, AOBMissionResolver.ResolveBoxType(UnitClassification.TRN, true), "TRN + para → AAB");
            Assert.AreEqual(AOBType.SB, AOBMissionResolver.ResolveBoxType(UnitClassification.TRN, false), "TRN + supply → SB");
        }

        [Test]
        public void ResolveBoxType_NonOperativeFlipsNothing()
        {
            Assert.AreEqual(AOBType.None, AOBMissionResolver.ResolveBoxType(UnitClassification.FGT, false), "escort/interceptor");
            Assert.AreEqual(AOBType.None, AOBMissionResolver.ResolveBoxType(UnitClassification.WW, false), "Wild Weasel");
            Assert.AreEqual(AOBType.None, AOBMissionResolver.ResolveBoxType(UnitClassification.HELO, false), "helo is never an AOB operative");
        }

        #endregion // Box type

        #region Pre-filter matrix (§11.1.1a)

        [Test]
        public void CandidateBoxTypes_MatchMatrix()
        {
            CollectionAssert.AreEquivalent(new[] { AOBType.ASB, AOBType.RB }, AOBMissionResolver.CandidateBoxTypes(AOBTargetCategory.EnemyUnit));
            CollectionAssert.AreEquivalent(new[] { AOBType.ASB, AOBType.RB }, AOBMissionResolver.CandidateBoxTypes(AOBTargetCategory.EnemyBase));
            CollectionAssert.AreEquivalent(new[] { AOBType.ASB, AOBType.RB }, AOBMissionResolver.CandidateBoxTypes(AOBTargetCategory.Bridge));
            CollectionAssert.AreEquivalent(new[] { AOBType.SB, AOBType.RB }, AOBMissionResolver.CandidateBoxTypes(AOBTargetCategory.FriendlyUnit));
            CollectionAssert.AreEquivalent(new[] { AOBType.AAB, AOBType.AEWB, AOBType.RB }, AOBMissionResolver.CandidateBoxTypes(AOBTargetCategory.OpenHex));
        }

        [Test]
        public void CandidateBoxTypes_InvalidTargetIsEmpty()
        {
            Assert.AreEqual(0, AOBMissionResolver.CandidateBoxTypes(AOBTargetCategory.Invalid).Count);
        }

        #endregion // Pre-filter matrix

        #region Composed legality (§11.1.1a)

        [Test]
        public void IsLegal_StrikeOnlyOnEnemyTargets()
        {
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.BMB, false, AOBTargetCategory.EnemyUnit), "BMB strikes an enemy unit");
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.BMB, false, AOBTargetCategory.EnemyBase), "BMB strikes a base");
            Assert.IsFalse(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.BMB, false, AOBTargetCategory.FriendlyUnit), "can't strike a friendly unit");
            Assert.IsFalse(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.BMB, false, AOBTargetCategory.OpenHex), "can't ASB an empty hex (no target)");
        }

        [Test]
        public void IsLegal_SupplyOnlyOnFriendlyUnit()
        {
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.TRN, false, AOBTargetCategory.FriendlyUnit), "SB resupplies a friendly unit");
            Assert.IsFalse(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.TRN, false, AOBTargetCategory.EnemyUnit), "can't supply an enemy");
        }

        [Test]
        public void IsLegal_AssaultAndAewbOpenHexOnly()
        {
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.TRN, true, AOBTargetCategory.OpenHex), "AAB drops on open ground");
            Assert.IsFalse(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.TRN, true, AOBTargetCategory.EnemyUnit), "AAB can't drop on an enemy hex");
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.AWACS, false, AOBTargetCategory.OpenHex), "AEWB operates over open ground");
            Assert.IsFalse(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.AWACS, false, AOBTargetCategory.EnemyUnit), "AEWB not over an enemy unit");
        }

        [Test]
        public void IsLegal_ReconLegalEverywhere()
        {
            // RB judgment call: recon can search any hex.
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.RECONA, false, AOBTargetCategory.EnemyUnit));
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.RECONA, false, AOBTargetCategory.FriendlyUnit));
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.RECONA, false, AOBTargetCategory.OpenHex));
            Assert.IsTrue(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.RECONA, false, AOBTargetCategory.Bridge));
        }

        [Test]
        public void IsLegal_NonOperativeNeverLegal()
        {
            Assert.IsFalse(AOBMissionResolver.IsOperativeLegalForTarget(UnitClassification.FGT, false, AOBTargetCategory.OpenHex));
        }

        #endregion // Composed legality

        #region ASB mission sub-type (§11.7)

        [Test]
        public void ResolveAsbMission_TargetSelectsMission()
        {
            Assert.AreEqual(AsbMissionType.GroundStrike, AOBMissionResolver.ResolveAsbMission(AOBTargetCategory.EnemyUnit), "§11.7.1");
            Assert.AreEqual(AsbMissionType.BaseStrike, AOBMissionResolver.ResolveAsbMission(AOBTargetCategory.EnemyBase), "§11.7.2/§11.7.5");
            Assert.AreEqual(AsbMissionType.BridgeStrike, AOBMissionResolver.ResolveAsbMission(AOBTargetCategory.Bridge), "§11.7.3");
            Assert.AreEqual(AsbMissionType.None, AOBMissionResolver.ResolveAsbMission(AOBTargetCategory.OpenHex), "no ASB on open ground");
        }

        #endregion // ASB mission sub-type
    }
}
