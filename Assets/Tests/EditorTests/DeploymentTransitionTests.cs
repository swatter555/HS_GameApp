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

        /// <summary>Foot infantry whose ONLY transport is airborne — the Spetsnaz / air-mobile shape.</summary>
        private static CombatUnit MakeFootWithHelosOnly() =>
            new CombatUnit("Spetsnaz", UnitClassification.SPECF, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, RegimentProfileType.DEP_EMB_HELO, WeaponType.INF_SPEC_SV,
                isMountable: false, WeaponType.NONE, isEmbarkable: true, WeaponType.HEL_MI8T_SV);

        /// <summary>Foot infantry with BOTH a ground carrier and helicopters — the air-assault shape.</summary>
        private static CombatUnit MakeFootWithCarriersAndHelos() =>
            new CombatUnit("Air Assault", UnitClassification.MAM, UnitRole.GroundCombat, Side.Player,
                Nationality.USSR, RegimentProfileType.DEP_MOB_EMB_HELO, WeaponType.INF_AM_SV,
                isMountable: true, WeaponType.APC_MTLB_SV, isEmbarkable: true, WeaponType.HEL_MI8T_SV);

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
    }
}
