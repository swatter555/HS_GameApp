using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    [TestFixture]
    public class ActionEconomyRegressionTests
    {
        private static CombatUnit AirMobile() => new CombatUnit("AM", UnitClassification.MAM,
            UnitRole.GroundCombat, Side.Player, Nationality.USSR, deployedProfile: WeaponType.INF_AM_SV,
            mobileProfile: WeaponType.APC_MTLB_SV, embarkedProfile: WeaponType.HEL_MI8T_SV);

        private static CombatUnit Airborne() => new CombatUnit("AB", UnitClassification.AB,
            UnitRole.GroundCombat, Side.Player, Nationality.USSR, deployedProfile: WeaponType.INF_AB_SV,
            mobileProfile: WeaponType.NONE, embarkedProfile: WeaponType.TRN_AN8_SV);

        [Test]
        public void CombatAtZeroMp_PreservesExtraCombatAndOpportunity_ClosesOtherOrders()
        {
            var unit = AirMobile();
            unit.MovementPoints.SetCurrent(0);
            unit.CombatActions.SetCurrent(3);
            unit.OpportunityActions.SetCurrent(2);
            unit.IntelActions.SetCurrent(2);
            Assert.That(unit.PerformCombatAction(), Is.True);
            Assert.That(unit.MovementPoints.Current, Is.Zero);
            Assert.That(unit.CombatActions.Current, Is.EqualTo(2));
            Assert.That(unit.OpportunityActions.Current, Is.EqualTo(2));
            Assert.That(unit.MoveActions.Current, Is.Zero);
            Assert.That(unit.DeploymentActions.Current, Is.Zero);
            Assert.That(unit.IntelActions.Current, Is.Zero);
            Assert.That(unit.PerformCombatAction(), Is.True, "extra voluntary attacks remain available");
        }

        [Test]
        public void CombatLock_CannotBeBypassedByRestoringCountersAndBoarding()
        {
            var unit = AirMobile();
            Assert.That(unit.PerformCombatAction(), Is.True);
            unit.DeploymentActions.SetCurrent(1);
            unit.MoveActions.SetCurrent(1);
            Assert.That(unit.TryAirEmbark(out _), Is.False);
            Assert.That(unit.TryDeployUP(out _), Is.False);
            Assert.That(unit.BeginMoveOrder(), Is.False);
            Assert.That(unit.DeductMovementCost(1), Is.False);
            unit.RefreshAllActions();
            Assert.That(unit.BeginMoveOrder(), Is.True, "own turn refresh clears the lock");
        }

        [Test]
        public void FailedCombatPayment_LeavesOrdersAndMovementUntouched()
        {
            var unit = AirMobile();
            unit.DaysSupply.SetCurrent(0);
            Assert.That(unit.PerformCombatAction(), Is.False);
            Assert.That(unit.HasInitiatedCombatThisTurn, Is.False);
            Assert.That(unit.MoveActions.Current, Is.EqualTo(1));
            Assert.That(unit.DeploymentActions.Current, Is.EqualTo(1));
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(3));
        }

        [Test]
        public void DefensiveFire_DoesNotApplyOffensiveLock()
        {
            var unit = AirMobile();
            unit.OpportunityActions.SetCurrent(1);
            Assert.That(unit.PerformOpportunityAction(), Is.True);
            unit.MarkFoughtThisTurn();
            Assert.That(unit.BeginMoveOrder(), Is.True);
            Assert.That(unit.TryAirEmbark(out _), Is.True);
        }

        [Test]
        public void GroundMountAndDirectAirBoarding_AreSeparateCommandsWithSharedBudget()
        {
            var mount = AirMobile();
            Assert.That(mount.TryDeployUP(out _), Is.True);
            Assert.That(mount.DeploymentPosition, Is.EqualTo(DeploymentPosition.Mobile));
            Assert.That(mount.MovementPoints.Current, Is.EqualTo(4));
            Assert.That(mount.TryAirEmbark(out _), Is.False, "mounting used the shared deployment action");
            var air = AirMobile();
            Assert.That(air.TryAirEmbark(out _), Is.True);
            Assert.That(air.DeploymentPosition, Is.EqualTo(DeploymentPosition.Embarked));
            Assert.That(air.MovementPoints.Current, Is.EqualTo(24));
            Assert.That(air.TryAirDisembark(out _), Is.False, "ground-start board plus land needs another turn");
        }

        [TestCase(0f)]
        [TestCase(0.375f)]
        [TestCase(1.625f)]
        [TestCase(3f)]
        public void OrdinaryAirTransitions_PreserveFractionWithoutRoundingOrLandingRefill(float remaining)
        {
            var unit = AirMobile();
            unit.MovementPoints.SetCurrent(remaining);
            unit.MoveActions.SetCurrent(0);
            Assert.That(unit.TryAirEmbark(out _), Is.True);
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(remaining * 8).Within(0.00001));
            Assert.That(unit.MoveActions.Current, Is.EqualTo(1));
            unit.DeploymentActions.SetCurrent(1); // explicit future-extra-action fixture, not a baseline grant
            unit.MoveActions.SetCurrent(0);
            Assert.That(unit.TryAirDisembark(out _), Is.True);
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(remaining).Within(0.00001));
            Assert.That(unit.MoveActions.Current, Is.Zero);
            Assert.That(unit.DeploymentPosition, Is.EqualTo(DeploymentPosition.Deployed));
        }

        [Test]
        public void BoardingHelo_SetsOneMoveExactly_AndTurnStartProvidesTwo()
        {
            var unit = AirMobile();
            unit.MoveActions.SetCurrent(3);
            Assert.That(unit.TryAirEmbark(out _), Is.True);
            Assert.That(unit.MoveActions.Current, Is.EqualTo(1));
            unit.RefreshAllActions();
            Assert.That(unit.MoveActions.Current, Is.EqualTo(2));
        }

        [Test]
        public void StartingAboard_FlyLandAttack_IsAllowedWithoutMovementRefill()
        {
            var unit = AirMobile();
            unit.SetDeploymentPosition(DeploymentPosition.Embarked);
            unit.RefreshMovementPointsForPosture();
            unit.RefreshAllActions();
            Assert.That(unit.MoveActions.Current, Is.EqualTo(2));
            Assert.That(unit.BeginMoveOrder(), Is.True);
            Assert.That(unit.DeductMovementCost(6), Is.True);
            Assert.That(unit.TryAirDisembark(out _), Is.True);
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(2.25f));
            Assert.That(unit.MoveActions.Current, Is.EqualTo(1));
            Assert.That(unit.PerformCombatAction(), Is.True);
            Assert.That(unit.BeginMoveOrder(), Is.False);
        }

        [Test]
        public void ForcedHaltAndKnownZoc_PreventBoardingWithoutCharges()
        {
            var halted = AirMobile();
            halted.MarkMovementHalted();
            Assert.That(halted.TryAirEmbark(out _), Is.False);
            Assert.That(halted.DeploymentActions.Current, Is.EqualTo(1));
            var inZoc = AirMobile();
            Assert.That(inZoc.TryAirEmbark(out _, inEnemyZoc: true), Is.False);
            Assert.That(inZoc.MovementPoints.Current, Is.EqualTo(3));
        }

        [Test]
        public void Airborne_RequiresFullMpAndAirbase_BoardsZero_NextTurnFull_LandsZero_CanAttack()
        {
            var unit = Airborne();
            Assert.That(unit.TryAirEmbark(out _), Is.False);
            unit.MovementPoints.SetCurrent(2.99f);
            Assert.That(unit.TryAirEmbark(out _, onAirbase: true), Is.False);
            unit.MovementPoints.ResetToMax();
            Assert.That(unit.TryAirEmbark(out _, onAirbase: true), Is.True);
            Assert.That(unit.MovementPoints.Max, Is.EqualTo(100));
            Assert.That(unit.MovementPoints.Current, Is.Zero);
            unit.RefreshMovementPoints();
            unit.RefreshAllActions();
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(100));
            Assert.That(unit.TryAirDisembark(out _), Is.True);
            Assert.That(unit.MovementPoints.Current, Is.Zero);
            Assert.That(unit.MovementPoints.Max, Is.EqualTo(3));
            Assert.That(unit.DeploymentPosition, Is.EqualTo(DeploymentPosition.Deployed));
            Assert.That(unit.PerformCombatAction(), Is.True);
        }

        [TestCase(1.49f, false)]
        [TestCase(1.5f, true)]
        [TestCase(3f, true)]
        public void Entrench_RequiresHalfBudget_ConsumesAllMp(float remaining, bool allowed)
        {
            var unit = AirMobile();
            unit.MovementPoints.SetCurrent(remaining);
            Assert.That(unit.TryDeployDOWN(out _), Is.EqualTo(allowed));
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(allowed ? 0 : remaining));
        }

        [Test]
        public void IntelEligibility_IsHqBaseOnly_WithNoMpFee()
        {
            var ground = AirMobile();
            ground.IntelActions.SetCurrent(2);
            Assert.That(ground.PerformIntelAction(), Is.False);
            var hq = new CombatUnit("HQ", UnitClassification.HQ, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            hq.MovementPoints.SetCurrent(0);
            Assert.That(hq.PerformIntelAction(), Is.True);
            Assert.That(hq.MovementPoints.Current, Is.Zero);
            var depot = new CombatUnit("Depot", UnitClassification.DEPOT, UnitRole.GroundCombat, Side.Player, Nationality.USSR);
            depot.IntelActions.SetCurrent(2);
            Assert.That(depot.PerformIntelAction(), Is.False);
        }

        [Test]
        public void ProfileFactory_RejectsPermanentMovementDeltas_AndAllowsExplicitZero()
        {
            Assert.Throws<ArgumentException>(() => WeaponProfile.FromProfileDef("bad", "bad", WeaponType.INF_AM_SV,
                GameData.MMP_WALKING, new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int> { [ProfileStat.MMP] = 1 }, Array.Empty<WeaponTrait>())));
            var immobile = WeaponProfile.FromProfileDef("zero", "zero", WeaponType.INF_AM_SV,
                GameData.MMP_IMMOBILE, new ProfileDef(FamilyArchetypes.Infantry,
                    new Dictionary<ProfileStat, int>(), Array.Empty<WeaponTrait>()));
            Assert.That(immobile.MaxMovementPoints, Is.Zero);
        }
        [Test]
        public void SaveRoundTrip_PreservesOffensiveLockAndFractionalLiftBudget()
        {
            var unit = AirMobile();
            Assert.That(unit.TryAirEmbark(out _), Is.True);
            unit.RefreshAllActions();
            unit.MovementPoints.SetCurrent(13.5f);
            Assert.That(unit.PerformCombatAction(), Is.True);
            string json = System.Text.Json.JsonSerializer.Serialize(unit, HammerAndSickle.Persistence.JsonPolicy.Save);
            var restored = System.Text.Json.JsonSerializer.Deserialize<CombatUnit>(json, HammerAndSickle.Persistence.JsonPolicy.Save);
            Assert.That(restored.HasInitiatedCombatThisTurn, Is.True);
            Assert.That(restored.MovementPoints.Max, Is.EqualTo(24));
            Assert.That(restored.MovementPoints.Current, Is.EqualTo(13.5f));
            restored.DeploymentActions.SetCurrent(1);
            Assert.That(restored.TryAirDisembark(out _), Is.False);
        }

        [Test]
        public void AttackHelo_HasTwoMovesAtConstructionAndTurnStart()
        {
            var unit = new CombatUnit("Helo", UnitClassification.HELO, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR);
            Assert.That(unit.MoveActions.Current, Is.EqualTo(2));
            unit.RefreshAllActions();
            Assert.That(unit.MoveActions.Current, Is.EqualTo(2));
        }

    }
}
