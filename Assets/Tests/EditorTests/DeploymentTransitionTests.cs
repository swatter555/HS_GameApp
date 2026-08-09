using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The deployment transitions reworked on 2026-08-04: airborne deploy-up for regiments with no ground
    /// transport, and proportional movement-point rescaling across a posture change.
    /// </summary>
    /// <remarks>
    /// ⚠ Both defects here were INVISIBLE AS ERRORS. A Spetsnaz regiment "mounted up" into helicopters as
    /// its ground posture and a foot regiment boarding helicopters flew two hexes — neither threw, neither
    /// logged, and both looked like ordinary gameplay until someone watched closely.
    /// </remarks>
    [TestFixture]
    public class DeploymentTransitionTests
    {
        #region Helpers

        /// <summary>Foot infantry whose ONLY transport is airborne — the Spetsnaz / air-mobile shape.
        /// (P1 2026-08-08: no declared shape, no flags — the shape IS Mobile=NONE + a populated Embarked bay.)</summary>
        private static CombatUnit MakeFootWithHelosOnly() =>
            new CombatUnit("Spetsnaz", UnitClassification.SPECF, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: WeaponType.INF_SPEC_SV,
                mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.HEL_MI8T_SV);

        /// <summary>Foot infantry with BOTH a ground carrier and helicopters — the air-assault shape.</summary>
        private static CombatUnit MakeFootWithCarriersAndHelos() =>
            new CombatUnit("Air Assault", UnitClassification.MAM, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: WeaponType.INF_AM_SV,
                mobileProfile: WeaponType.APC_MTLB_SV, embarkedProfile: WeaponType.HEL_MI8T_SV);

        #endregion // Helpers

        #region Airborne deploy-up

        [Test]
        public void FootUnitWithOnlyHelos_DeploysUpStraightToEmbarked()
        {
            /* ⚠ THE BUG BOB SAW. This regiment has nothing on the ground to mount, so Deployed must aim
             * at Embarked. It used to land on Mobile, which only "worked" because the helicopter had been
             * authored into the Mobile slot — the unit rode aircraft as its ground posture. */
            var spetsnaz = MakeFootWithHelosOnly();
            spetsnaz.SetDeploymentPosition(DeploymentPosition.Deployed);

            bool moved = spetsnaz.TryDeployUP(out string error, onAirbase: true, onPort: false);

            Assert.That(moved, Is.True, error);
            Assert.That(spetsnaz.DeploymentPosition, Is.EqualTo(DeploymentPosition.Embarked),
                "with no ground transport there is no Mobile posture to stop at");
            Assert.That(MovementModeService.CurrentMedium(spetsnaz), Is.EqualTo(MovementMedium.Helo));
            Assert.That(MovementModeService.IsAirborneNow(spetsnaz), Is.True);
        }

        [Test]
        public void FootUnitWithCarriersAndHelos_StillPassesThroughMobile()
        {
            // The other half of the rule: a regiment that DOES have ground transport must not skip it.
            // Dismount from the carriers, then board the helicopters — two distinct postures.
            var airAssault = MakeFootWithCarriersAndHelos();
            airAssault.SetDeploymentPosition(DeploymentPosition.Deployed);

            Assert.That(airAssault.TryDeployUP(out string error, onAirbase: true, onPort: false), Is.True, error);
            Assert.That(airAssault.DeploymentPosition, Is.EqualTo(DeploymentPosition.Mobile),
                "it has MT-LBs, so Mobile is a real posture for it");
            Assert.That(MovementModeService.CurrentMedium(airAssault), Is.EqualTo(MovementMedium.Tracked));
        }

        [Test]
        public void ComingDownFromEmbarked_LandsOnDeployed_NotMobile()
        {
            // Correct already, pinned so it stays that way: dismounting from flight puts men on the
            // ground, never into a ground vehicle they may not even have.
            var spetsnaz = MakeFootWithHelosOnly();
            spetsnaz.SetDeploymentPosition(DeploymentPosition.Embarked);

            Assert.That(spetsnaz.TryDeployDOWN(out string error), Is.True, error);
            Assert.That(spetsnaz.DeploymentPosition, Is.EqualTo(DeploymentPosition.Deployed));
            Assert.That(MovementModeService.CurrentMedium(spetsnaz), Is.EqualTo(MovementMedium.Foot));
        }

        #endregion // Airborne deploy-up

        #region Movement points scale across a posture change

        [Test]
        public void BoardingHelicopters_ScalesRemainingPoints_RatherThanCarryingTheNumber()
        {
            /* ⚠ THE DEFECT THIS RULING FIXES. Movement points mean different things either side of a
             * posture change: two points is half a foot regiment's day and one twelfth of a helicopter
             * lift. The old code kept the ABSOLUTE leftover and clamped it, so a full-strength regiment
             * paid its transition cost and then flew a couple of hexes on a 24-point profile. */
            var spetsnaz = MakeFootWithHelosOnly();
            spetsnaz.SetDeploymentPosition(DeploymentPosition.Deployed);

            float footMax = spetsnaz.MovementPoints.Max;
            Assert.That(spetsnaz.TryDeployUP(out string error, onAirbase: true, onPort: false), Is.True, error);

            float heloMax = spetsnaz.MovementPoints.Max;
            Assert.That(heloMax, Is.GreaterThan(footMax), "helicopters must raise the ceiling");

            // The transition costs half the pre-move budget, so half the new ceiling should remain.
            Assert.That(spetsnaz.MovementPoints.Current, Is.EqualTo(heloMax * 0.5f).Within(0.01f),
                "half the foot budget spent must mean half the air budget remains");
            Assert.That(spetsnaz.MovementPoints.Current, Is.GreaterThan(footMax),
                "the old bug: a regiment that boarded helicopters could still only travel on foot legs");
        }

        [Test]
        public void ScalingIsPureAndSymmetric()
        {
            // The rule itself, independent of any cost constant — which is what lets it survive the
            // action/movement rebalance without re-tuning.
            Assert.That(MovementModeService.ScaleMovementPoints(2f, 4f, 24f), Is.EqualTo(12f).Within(0.001f),
                "half of foot becomes half of air");
            Assert.That(MovementModeService.ScaleMovementPoints(12f, 24f, 4f), Is.EqualTo(2f).Within(0.001f),
                "and back again");
            Assert.That(MovementModeService.ScaleMovementPoints(0f, 4f, 24f), Is.EqualTo(0f),
                "spent is spent, whatever you board");
            Assert.That(MovementModeService.ScaleMovementPoints(4f, 4f, 24f), Is.EqualTo(24f).Within(0.001f),
                "untouched is untouched");
        }

        [Test]
        public void ScalingSurvivesADegenerateOldMaximum()
        {
            // A static or profile-less unit reports a zero ceiling; there is no fraction to preserve, so
            // the new ceiling stands rather than producing a divide-by-zero.
            Assert.That(MovementModeService.ScaleMovementPoints(0f, 0f, 24f), Is.EqualTo(24f));
        }

        #endregion // Movement points scale across a posture change

        #region P2 — naval transient state + the rewritten embark gates (2026-08-08)

        /// <summary>Plain foot infantry: no transport of any kind — the universal-sealift base case.</summary>
        private static CombatUnit MakePlainFoot() =>
            new CombatUnit("Rifles", UnitClassification.MOT, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: WeaponType.INF_REG_SV,
                mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.NONE);

        /// <summary>Mounted marines — the shape whose sealift the P1 template change un-owned.</summary>
        private static CombatUnit MakeMountedMarines() =>
            new CombatUnit("Marines", UnitClassification.MMAR, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: WeaponType.INF_MAR_SV,
                mobileProfile: WeaponType.APC_BTR70_SV, embarkedProfile: WeaponType.NONE);

        /// <summary>Paratroopers with their An-12s — the fixed-wing gate case.</summary>
        private static CombatUnit MakeParatroopersWithAn12() =>
            new CombatUnit("VDV", UnitClassification.AB, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, deployedProfile: WeaponType.INF_AB_SV,
                mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.TRN_AN8_SV);

        [Test]
        public void FootUnit_AtAFriendlyPort_SealiftsToNavalEmbark()
        {
            // §9.4.7: ANY ground unit at a friendly port skips Mobile and embarks on the shared
            // flotilla. Nothing is owned — the profile is drawn, the state is set.
            var rifles = MakePlainFoot();

            Assert.That(rifles.TryDeployUP(out string err, onPort: true), Is.True, err);
            Assert.That(rifles.DeploymentPosition, Is.EqualTo(DeploymentPosition.Embarked));
            Assert.That(rifles.IsNavalEmbarked, Is.True);
            Assert.That(rifles.GetActiveWeaponProfile()?.WeaponType, Is.EqualTo(WeaponType.TRN_NAVAL));
            Assert.That(MovementModeService.CurrentMedium(rifles), Is.EqualTo(MovementMedium.Naval));
        }

        [Test]
        public void FootUnit_OffPort_WithNothingToMount_IsRefusedNotSilentlyMobile()
        {
            // D3: the pre-P2 code let this unit "mount" nothing — it paid full transition costs and
            // silently kept its deployed profile at the Mobile position.
            var rifles = MakePlainFoot();
            float mpBefore = rifles.MovementPoints.Current;
            float actionsBefore = rifles.DeploymentActions.Current;

            Assert.That(rifles.TryDeployUP(out string err), Is.False);
            Assert.That(err, Is.Not.Empty);
            Assert.That(rifles.DeploymentPosition, Is.EqualTo(DeploymentPosition.Deployed));
            Assert.That(rifles.MovementPoints.Current, Is.EqualTo(mpBefore), "a refusal charges nothing");
            Assert.That(rifles.DeploymentActions.Current, Is.EqualTo(actionsBefore));
        }

        [Test]
        public void MountedMarines_ReachSealift_ThroughMobile()
        {
            // A unit WITH ground transport still passes through Mobile; from there, +1 at a port is the
            // naval route (no owned lift in the bay).
            var marines = MakeMountedMarines();

            Assert.That(marines.TryDeployUP(out string e1, onPort: true), Is.True, e1);
            Assert.That(marines.DeploymentPosition, Is.EqualTo(DeploymentPosition.Mobile),
                "owning carriers means mounting them first — the port does not skip a real Mobile bay");

            marines.RefreshAllActions();                        // one deployment action per turn
            marines.MovementPoints.ResetToMax();
            Assert.That(marines.TryDeployUP(out string e2, onPort: true), Is.True, e2);
            Assert.That(marines.DeploymentPosition, Is.EqualTo(DeploymentPosition.Embarked));
            Assert.That(marines.IsNavalEmbarked, Is.True);
        }

        [Test]
        public void OrganicLift_WinsOverNaval_AtAPort()
        {
            // Owned equipment beats the shared flotilla: a Spetsnaz at a port boards its OWN Mi-8s.
            var spetsnaz = MakeFootWithHelosOnly();

            Assert.That(spetsnaz.TryDeployUP(out string err, onPort: true), Is.True, err);
            Assert.That(spetsnaz.IsNavalEmbarked, Is.False);
            Assert.That(spetsnaz.GetActiveWeaponProfile()?.WeaponType, Is.EqualTo(WeaponType.HEL_MI8T_SV));
        }

        [Test]
        public void FixedWingLift_NeedsTheAirbase_HeloLiftDoesNot()
        {
            /* The rewritten gate keys on WHAT is boarded, never who boards: the AB unit's airbase rule
             * survives as a consequence of its An-12s — and now also covers the FW-lifted SPECF the old
             * classification list missed (defect D8). */
            var vdv = MakeParatroopersWithAn12();
            Assert.That(vdv.TryDeployUP(out _, onAirbase: false), Is.False,
                "fixed-wing lift operates from an airbase");
            Assert.That(vdv.TryDeployUP(out string err, onAirbase: true), Is.True, err);
            Assert.That(vdv.IsNavalEmbarked, Is.False);

            var spetsnaz = MakeFootWithHelosOnly();
            Assert.That(spetsnaz.TryDeployUP(out string err2), Is.True, err2);
        }

        [Test]
        public void NavalDebark_PortForEveryone_BeachheadForMarinesOnly()
        {
            var marines = MakeMountedMarines();
            marines.TryDeployUP(out _, onPort: true);           // Mobile
            marines.RefreshAllActions();
            marines.MovementPoints.ResetToMax();
            marines.TryDeployUP(out _, onPort: true);           // Embarked (naval)
            marines.RefreshAllActions();                        // fresh deployment actions for the debark
            marines.MovementPoints.ResetToMax();

            Assert.That(marines.TryDeployDOWN(out _), Is.False, "mid-ocean is not a debark site");
            Assert.That(marines.TryDeployDOWN(out string err, onBeachhead: true), Is.True, err);
            Assert.That(marines.DeploymentPosition, Is.EqualTo(DeploymentPosition.Deployed),
                "naval debark lands Deployed (§9.5.2), bypassing Mobile");
            Assert.That(marines.IsNavalEmbarked, Is.False, "the state clears on debark");

            var rifles = MakePlainFoot();
            rifles.TryDeployUP(out _, onPort: true);
            rifles.RefreshAllActions();
            rifles.MovementPoints.ResetToMax();
            Assert.That(rifles.TryDeployDOWN(out _, onBeachhead: true), Is.False,
                "the beachhead is the marines' ONE naval privilege (§9.10.6.1)");
            Assert.That(rifles.TryDeployDOWN(out string err2, onPort: true), Is.True, err2);
        }

        [Test]
        public void DeployUp_AtEmbarked_IsRefused_NotUndefined()
        {
            // D2: +1 from the top used to write the undefined enum value 6 and charge full costs.
            var spetsnaz = MakeFootWithHelosOnly();
            spetsnaz.TryDeployUP(out _);                        // Embarked
            spetsnaz.RefreshAllActions();
            spetsnaz.MovementPoints.ResetToMax();

            Assert.That(spetsnaz.TryDeployUP(out string err), Is.False);
            Assert.That(err, Is.Not.Empty);
            Assert.That(spetsnaz.DeploymentPosition, Is.EqualTo(DeploymentPosition.Embarked));
        }

        [Test]
        public void NavalEmbark_ScalesMovementPoints_OntoTheFlotillaCeiling()
        {
            // The naval flag is written BEFORE costs, so the rescale lands on TRN_NAVAL's ceiling —
            // written after, the unit would board ships on its foot budget.
            var rifles = MakePlainFoot();
            float footMax = rifles.MovementPoints.Max;

            rifles.TryDeployUP(out _, onPort: true);

            float navalMax = rifles.MovementPoints.Max;
            Assert.That(navalMax, Is.GreaterThan(footMax), "the flotilla is faster than boots");
            Assert.That(rifles.MovementPoints.Current, Is.EqualTo(navalMax * 0.5f).Within(0.01f),
                "half the foot budget spent means half the sea budget remains");
        }

        #endregion // P2 — naval transient state + the rewritten embark gates
    }
}
