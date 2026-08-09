using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Coverage guard for <see cref="MovementMedium"/> — the one fact the project never recorded:
    /// how a weapon profile physically moves.
    /// </summary>
    /// <remarks>
    /// ⚠ THIS TEST IS THE WHOLE SAFETY NET FOR AN UNDECLARED MEDIUM. `None` is the deliberate safe
    /// failure — a profile nobody classified goes SILENT rather than confidently sounding like the wrong
    /// vehicle — but silence is also the correct behaviour for a sound with no clip yet, so the two are
    /// indistinguishable in play. Nobody would ever notice an undeclared APC by ear. This test is where
    /// it gets noticed.
    ///
    /// ⚠ It walks the REAL databases rather than a fixture list, because the failure it guards against is
    /// precisely "someone added a profile and forgot". A hand-maintained list of profiles to check would
    /// have the same gap as the thing it is checking.
    /// </remarks>
    [TestFixture]
    public class MovementMediumTests
    {
        #region Coverage

        [Test]
        public void EveryProfileAUnitCanMoveOn_DeclaresAMedium()
        {
            /* Walks every template's three movement slots. Failures are COLLECTED and reported together
             * rather than thrown on the first one — a new family of profiles tends to be missed as a
             * group, and fixing them one test-run at a time would be miserable. */
            var missing = new List<string>();

            foreach (string templateId in CombatUnitDB.GetAllTemplateIds())
            {
                CombatUnit template = CombatUnitDB.GetUnitTemplate(templateId);
                if (template == null) continue;

                CheckSlot(missing, templateId, "Deployed", template.GetDeployedProfile());
                CheckSlot(missing, templateId, "Mobile", template.GetMobileProfile());
                CheckSlot(missing, templateId, "Embarked", template.GetEmbarkedProfile());
            }

            Assert.That(missing, Is.Empty,
                "These profiles can carry a unit but do not say how they move, so they would be silent " +
                "and — once the movement rules read the medium — would also be treated as neither " +
                "airborne nor ground. Set it on the family archetype if the whole family agrees, or per " +
                "profile via SetMovementMedium if the family is mixed (APC and Recon are):\n  " +
                string.Join("\n  ", missing));
        }

        private static void CheckSlot(List<string> missing, string templateId, string slot,
                                      WeaponProfile profile)
        {
            // An empty slot is normal — most units have no embarked profile, and a self-propelled unit
            // has no mobile one. Only a PRESENT profile is required to declare itself.
            if (profile == null) return;

            if (profile.MovementMedium == MovementMedium.None)
                missing.Add($"{templateId} [{slot}] -> {profile.WeaponType}");
        }

        [Test]
        public void NoAirTransport_SitsInAGroundTransportSlot()
        {
            /* ⚠ THE GUARD FOR A MISTAKE THAT SHIPPED. The Spetsnaz (GRU) regiment carried HEL_MI8T_SV as
             * its MOBILE profile with an empty embarked slot, so deploying up put the regiment aboard
             * helicopters as its GROUND posture: it paid ground terrain costs and stopped for zones of
             * control while flying. Nothing threw. It took a play test to notice.
             *
             * The Mobile slot means "ground transport". A profile that declares itself a helo or
             * fixed-wing transport belongs in Embarked, and a regiment with no ground transport at all
             * simply leaves Mobile = NONE (P1 2026-08-08: there is no declared profile type anymore —
             * the shape IS the slot contents). */
            var offenders = new List<string>();

            foreach (string templateId in CombatUnitDB.GetAllTemplateIds())
            {
                CombatUnit template = CombatUnitDB.GetUnitTemplate(templateId);
                WeaponProfile mobile = template?.GetMobileProfile();
                if (mobile == null || mobile.TransportCategory == TransportCategory.None) continue;

                offenders.Add($"{templateId} -> {mobile.WeaponType} ({mobile.TransportCategory})");
            }

            Assert.That(offenders, Is.Empty,
                "Air transports in a GROUND transport slot. In Mobile the unit rides them as its ground " +
                "posture — terrain costs, zones of control, ground ambush — while airborne:\n  " +
                string.Join("\n  ", offenders));
        }

        #endregion // Coverage

        #region Family defaults

        [Test]
        public void UnanimousFamilies_CarryTheirMediumOnTheArchetype()
        {
            // These are declared once on the family rather than repeated per profile. If someone adds a
            // family member that disagrees, the family default is no longer safe and the family must be
            // demoted to per-profile like APC and Recon were.
            Assert.That(FamilyArchetypes.Infantry.Medium, Is.EqualTo(MovementMedium.Foot));
            Assert.That(FamilyArchetypes.Ifv.Medium, Is.EqualTo(MovementMedium.Tracked));
            Assert.That(FamilyArchetypes.Truck.Medium, Is.EqualTo(MovementMedium.Wheeled));
            Assert.That(FamilyArchetypes.Helicopter.Medium, Is.EqualTo(MovementMedium.Helo));
            Assert.That(FamilyArchetypes.Facility.Medium, Is.EqualTo(MovementMedium.Static));
            Assert.That(TankArchetypes.Gen1.Medium, Is.EqualTo(MovementMedium.Tracked));
            Assert.That(TankArchetypes.Gen4.Medium, Is.EqualTo(MovementMedium.Tracked));
            Assert.That(FamilyArchetypes.FighterLate.Medium, Is.EqualTo(MovementMedium.FixedWing));
        }

        [Test]
        public void TowedGuns_AreFoot_AndSelfPropelledOnesAreNot()
        {
            /* ⚠ ASSERTS PROFILES, NOT ARCHETYPE DEFAULTS, and the difference is why the earlier version of
             * this test was worthless. It used to check that Artillery/Aaa/Sam defaulted to Foot — which
             * pinned the MECHANISM, passed happily while all 31 self-propelled guns silently inherited
             * "walking infantry", and then had to be deleted the moment the mechanism changed. What
             * actually matters is that a towed gun and a self-propelled one sound different, and that
             * holds however the medium is sourced.
             *
             * These four also cover every chassis kind the artillery families contain, so a bulk edit
             * that quietly fails to apply — as one did — is caught here rather than by ear. */
            Assert.That(Medium(WeaponType.ART_LIGHT_SV), Is.EqualTo(MovementMedium.Foot),
                "a towed gun is emplaced and crew-manhandled — exactly what its MMP 4 has always said");
            Assert.That(Medium(WeaponType.SPA_2S1_SV), Is.EqualTo(MovementMedium.Tracked),
                "a 2S1 carries its own gun on tracks");
            Assert.That(Medium(WeaponType.ROC_BM21_SV), Is.EqualTo(MovementMedium.Wheeled),
                "a BM-21 is a launcher on a truck");
            Assert.That(Medium(WeaponType.SPSAM_9K31_SV), Is.EqualTo(MovementMedium.Wheeled),
                "Strela-1 rides a BRDM-2 hull — self-propelled does not imply tracked");
        }

        private static MovementMedium Medium(WeaponType type) =>
            WeaponProfileDB.GetWeaponProfile(type).MovementMedium;

        [Test]
        public void MixedFamilies_CarryNoDefault_SoAMemberMustDecide()
        {
            /* ⚠ THE LOAD-BEARING ASSERTION OF THE WHOLE DESIGN. Five families are genuinely mixed. A
             * default on any of them makes the WRONG answer the silent one, and a new profile inherits it
             * without anyone deciding. None forces the decision, and the coverage test above enforces it.
             *
             * ⚠ THE ARTILLERY THREE ARE HERE BECAUSE THEY PROVED THE RULE THE HARD WAY. They briefly
             * carried Foot — true of the towed baseline the archetype is named for — and that silently
             * made all 31 self-propelled guns sound like walking infantry. The coverage test could not
             * catch it: those profiles HAD a medium, it was just wrong. A wrong default is confident and
             * invisible; None is silent and testable. */
            Assert.That(FamilyArchetypes.Apc.Medium, Is.EqualTo(MovementMedium.None),
                "APC holds the tracked MT-LB/M113/LVTP-7 alongside the wheeled BTR/HMMWV");
            Assert.That(FamilyArchetypes.Recon.Medium, Is.EqualTo(MovementMedium.None),
                "recon holds the tracked M3/FV105 alongside the wheeled BRDM/ERC-90/Luchs");
            Assert.That(FamilyArchetypes.Artillery.Medium, Is.EqualTo(MovementMedium.None),
                "artillery holds towed guns, tracked SP guns AND truck-mounted rocket launchers");
            Assert.That(FamilyArchetypes.Aaa.Medium, Is.EqualTo(MovementMedium.None),
                "AAA holds towed guns and tracked SPAAA");
            Assert.That(FamilyArchetypes.Sam.Medium, Is.EqualTo(MovementMedium.None),
                "SAM holds emplaced sites, tracked SPSAM and wheeled Strela-1/Crotale");
        }

        #endregion // Family defaults

        #region The pair that started this

        [Test]
        public void MtlbIsTracked_AndBtr70IsWheeled()
        {
            /* The two profiles that proved the fact did not exist. They are identical on every axis the
             * code had: same APC_ prefix, same Apc archetype, same AMPHIBIOUS trait, same UpgradePath,
             * same MMP 8, same EquipmentBucket. Only this field can tell them apart. */
            Assert.That(WeaponProfileDB.GetWeaponProfile(WeaponType.APC_MTLB_SV).MovementMedium,
                Is.EqualTo(MovementMedium.Tracked));
            Assert.That(WeaponProfileDB.GetWeaponProfile(WeaponType.APC_BTR70_SV).MovementMedium,
                Is.EqualTo(MovementMedium.Wheeled));
        }

        [Test]
        public void FrenchVab_IsTracked_DeliberatelyAndNotByMistake()
        {
            /* ⚠ The real VAB is a wheeled 6x6. This is ratified as Tracked (Bob, 2026-08-04) because no
             * VAB sprite exists — the profile draws FR_M113_*, a French motor rifle regiment carries it
             * as its mobile profile, and the player therefore SEES tracks. Sight and sound must agree.
             * This test exists so that a later reader who "corrects" it to Wheeled gets a failure that
             * points at the reasoning instead of a silent mismatch nobody notices for months. */
            Assert.That(WeaponProfileDB.GetWeaponProfile(WeaponType.APC_VAB_FR).MovementMedium,
                Is.EqualTo(MovementMedium.Tracked),
                "VAB is deliberately Tracked to match its M113 art — see the ruling at the profile. " +
                "Flip it to Wheeled only when real VAB art is authored.");
        }

        #endregion // The pair that started this
    }
}
