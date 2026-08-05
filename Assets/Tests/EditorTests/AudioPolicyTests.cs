using System;
using HammerAndSickle.Audio;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The two audio policies ratified in HS_DesignDoc §27.7.4 (fog gating) and §27.7.5 (family mapping).
    ///
    /// Both fail SILENTLY in play, which is why they are worth pinning. A fog leak is inaudible as a bug —
    /// the player simply learns something they should not have, and nobody sees a stack trace. A family
    /// mis-map is worse than silence: it plays a tank gun for an artillery piece, which is confidently
    /// wrong rather than obviously missing.
    /// </summary>
    [TestFixture]
    public class AudioPolicyTests
    {
        #region Helpers

        private static CombatUnit MakeUnit(Side side, SpottedLevel level)
        {
            var unit = new CombatUnit("TestUnit", UnitClassification.INF, UnitRole.GroundCombat,
                side, Nationality.USSR);
            unit.SetSpottedLevel(level);
            return unit;
        }

        #endregion // Helpers

        #region §27.7.4 — fog gating

        [Test]
        public void PlayerUnits_AreAlwaysAudible_AtEverySpottedLevel()
        {
            // SpottedLevel describes what the PLAYER knows about an ENEMY; it carries no meaning for an
            // owned unit. A friendly unit sitting at Level0 must not be silenced by it.
            foreach (SpottedLevel level in Enum.GetValues(typeof(SpottedLevel)))
            {
                var friendly = MakeUnit(Side.Player, level);
                Assert.That(AudioFogPolicy.CanHear(friendly), Is.True,
                    $"friendly unit must be audible at {level}");
            }
        }

        [Test]
        public void UnspottedEnemy_IsSilent()
        {
            var hidden = MakeUnit(Side.AI, SpottedLevel.Level0);
            Assert.That(AudioFogPolicy.CanHear(hidden), Is.False,
                "an unspotted enemy firing would reveal itself through audio, bypassing the §12 ladder");
        }

        [Test]
        public void EnemyBecomesAudible_AtLevel1_AndStaysAudibleAbove()
        {
            // §27.7.4.3: the threshold is Level1 because §24.3.2.1 already shows unit art from Level1 —
            // the sound leaks nothing the icon has not. If someone "hardens" this to Level2+, this fails.
            foreach (SpottedLevel level in Enum.GetValues(typeof(SpottedLevel)))
            {
                var enemy = MakeUnit(Side.AI, level);
                bool expected = level >= SpottedLevel.Level1;
                Assert.That(AudioFogPolicy.CanHear(enemy), Is.EqualTo(expected),
                    $"enemy audibility at {level}");
            }
        }

        [Test]
        public void NullSource_FailsClosed()
        {
            // Deliberately the opposite of DefaultDialog_Scene1.IsScreenPointOverUI, which fails OPEN and
            // shipped a live defect. For a fog gate, silence is the safe direction.
            Assert.That(AudioFogPolicy.CanHear(null), Is.False);
        }

        [Test]
        public void UnseenFirer_HittingPlayerUnit_SuppressesTheGunButNotTheImpact()
        {
            // The §27.7.4.2 attribution case, and the whole reason no "generic substitute sound" concept
            // is needed: the player hears themselves being shelled without learning what shelled them.
            var hiddenBattery = MakeUnit(Side.AI, SpottedLevel.Level0);
            var ourRegiment = MakeUnit(Side.Player, SpottedLevel.Level0);

            var (fire, impact) = AudioFogPolicy.CanHearEngagement(hiddenBattery, ourRegiment);

            Assert.That(fire, Is.False, "the gun report identifies an enemy the player has not spotted");
            Assert.That(impact, Is.True, "the player always hears their own unit being hit");
        }

        #endregion // §27.7.4 — fog gating

        #region §27.7.5 — family mapping

        [Test]
        public void EveryEquipmentBucket_HasAFamilyDecision()
        {
            // Guards the switch's default arm: a bucket added later silently lands on None. That is the
            // correct FAILURE direction, but it should be a decision, not an oversight — so this test
            // exists to fail loudly when the bucket set grows.
            var armedBuckets = new[]
            {
                EquipmentBucket.Personnel, EquipmentBucket.TANK, EquipmentBucket.IFV, EquipmentBucket.APC,
                EquipmentBucket.RCN, EquipmentBucket.ART, EquipmentBucket.ROC, EquipmentBucket.SAM,
                EquipmentBucket.AAA, EquipmentBucket.AT, EquipmentBucket.HEL, EquipmentBucket.FGT,
                EquipmentBucket.ATT, EquipmentBucket.BMB
            };

            foreach (var bucket in armedBuckets)
                Assert.That(WeaponSoundClassifier.FamilyFor(bucket), Is.Not.EqualTo(WeaponSoundFamily.None),
                    $"{bucket} is an armed class and must map to a firing sound");

            Assert.That(Enum.GetValues(typeof(EquipmentBucket)).Length, Is.EqualTo(armedBuckets.Length + 4),
                "EquipmentBucket changed — the four unarmed members are None/AWACS/TRN/RCNA; " +
                "a new bucket needs a deliberate WeaponSoundFamily decision");
        }

        [Test]
        public void UnarmedClasses_AreSilent()
        {
            foreach (var bucket in new[] { EquipmentBucket.AWACS, EquipmentBucket.TRN,
                                           EquipmentBucket.RCNA, EquipmentBucket.None })
                Assert.That(WeaponSoundClassifier.FamilyFor(bucket), Is.EqualTo(WeaponSoundFamily.None),
                    $"{bucket} never initiates fire");
        }

        [Test]
        public void FamilyIsDerivedFromTheSharedClassifier_NotASecondPrefixList()
        {
            // The contract that keeps audio from disagreeing with the intel and loss reports: whatever
            // ClassifyWeaponType says a weapon is, the sound family follows. Asserted over EVERY
            // WeaponType so a future member cannot quietly diverge.
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                var viaType = WeaponSoundClassifier.FamilyFor(type);
                var viaBucket = WeaponSoundClassifier.FamilyFor(RegimentProfile.ClassifyWeaponType(type));
                Assert.That(viaType, Is.EqualTo(viaBucket), $"{type} disagrees with the shared classifier");
            }
        }

        [Test]
        public void RepresentativeWeapons_MapToTheExpectedFamily()
        {
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.TANK_T72A_SV),
                Is.EqualTo(WeaponSoundFamily.TankGun));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.IFV_BMP2_SV),
                Is.EqualTo(WeaponSoundFamily.Autocannon));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.INF_REG_SV),
                Is.EqualTo(WeaponSoundFamily.SmallArms));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.ART_LIGHT_SV),
                Is.EqualTo(WeaponSoundFamily.ArtilleryGun));
        }

        [Test]
        public void UnitWithNoWeaponProfile_IsSilent_RatherThanThrowing()
        {
            // The 5-argument CombatUnit ctor installs no weapon profiles. Audio must tolerate that:
            // a profile-less unit is a fixture artefact, not a reason to throw inside a sound call.
            var bare = MakeUnit(Side.Player, SpottedLevel.Level0);
            Assert.That(WeaponSoundClassifier.FamilyFor(bare), Is.EqualTo(WeaponSoundFamily.None));
            Assert.That(WeaponSoundClassifier.FamilyFor((CombatUnit)null), Is.EqualTo(WeaponSoundFamily.None));
        }

        #endregion // §27.7.5 — family mapping
    }
}
