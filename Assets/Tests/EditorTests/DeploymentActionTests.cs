using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Deploy up / down (§8.2 action economy, §21.3.1 airborne embarkation).
    ///
    /// These drive `CombatUnit.TryDeployUP` / `TryDeployDOWN` directly rather than through
    /// `MovementController`, because that is where the RULES live — the controller only supplies map
    /// context (airbase adjacency, port hex) and publishes the result. Anything the model refuses here is
    /// refused however the request arrives.
    ///
    /// The cases that matter are the ECONOMY ones: a deployment change that costs nothing, or that charges
    /// on a refusal, is invisible in play until someone notices a unit fortifying every turn for free.
    /// </summary>
    [TestFixture]
    public class DeploymentActionTests
    {
        #region Helpers

        /// <summary>
        /// A supplied, full-strength ground unit able to change states.
        ///
        /// ⚠ THE REGIMENT PROFILE IS NOT OPTIONAL HERE. A completing transition ends in
        /// `ApplyDeploymentTransitionCosts` → `UpdateMovementPointsForProfile`, which throws
        /// "No active weapon system profile available" if the unit has no weapon profiles — and the
        /// five-argument constructor does NOT install any (only the eleven-argument one calls
        /// `InitializeEquipmentBays`). Without this the REFUSAL cases still pass, because they return
        /// before reaching the cost step, so a bare unit hides the problem in exactly half the suite.
        /// Matches the fixture idiom used by the combat suites.
        /// </summary>
        private static CombatUnit MakeUnit(DeploymentPosition start = DeploymentPosition.Deployed)
        {
            var unit = new CombatUnit("TestUnit", UnitClassification.INF, UnitRole.GroundCombat,
                Side.Player, Nationality.USSR);

            unit.EquipmentBays.InitializeEquipmentBays("TestUnit",
                WeaponType.INF_REG_SV, WeaponType.NONE, WeaponType.NONE);

            unit.HitPoints.SetCurrent(unit.HitPoints.Max);
            unit.DaysSupply.SetCurrent(unit.DaysSupply.Max);
            unit.MovementPoints.SetCurrent(unit.MovementPoints.Max);
            unit.DeploymentActions.ResetToMax();
            unit.SetDeploymentPosition(start);

            return unit;
        }

        #endregion // Helpers

        #region The action-economy gate (D1)

        [Test]
        public void DeployDown_SpendsExactlyOneDeploymentAction()
        {
            var unit = MakeUnit();
            float before = unit.DeploymentActions.Current;

            Assert.That(unit.TryDeployDOWN(out _), Is.True);
            Assert.That(unit.DeploymentActions.Current, Is.EqualTo(before - 1f).Within(0.001f));
        }

        [Test]
        public void WithNoDeploymentActionsLeft_TheChangeIsRefused()
        {
            // ⚠ THE DEFECT THIS PINS: CanChangeToState checked supply, efficiency and movement points but
            // NOT whether an action remained, while ApplyDeploymentTransitionCosts decremented one
            // unconditionally. A unit could dig in and un-dig every turn for free.
            var unit = MakeUnit();
            unit.DeploymentActions.SetCurrent(0f);

            Assert.That(unit.TryDeployDOWN(out string error), Is.False);
            Assert.That(error, Does.Contain("deployment actions"));
        }

        [Test]
        public void ARefusedChange_LeavesStateAndCostsUntouched()
        {
            // A refusal must be free. Charging for a rejected order is worse than rejecting it.
            var unit = MakeUnit();
            unit.DeploymentActions.SetCurrent(0f);

            DeploymentPosition position = unit.DeploymentPosition;
            float supply = unit.DaysSupply.Current;
            float movement = unit.MovementPoints.Current;

            Assert.That(unit.TryDeployDOWN(out _), Is.False);

            Assert.That(unit.DeploymentPosition, Is.EqualTo(position));
            Assert.That(unit.DaysSupply.Current, Is.EqualTo(supply).Within(0.001f));
            Assert.That(unit.MovementPoints.Current, Is.EqualTo(movement).Within(0.001f));
            Assert.That(unit.DeploymentActions.Current, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DeploymentAlsoCostsSupplyAndMovement()
        {
            var unit = MakeUnit();
            float supply = unit.DaysSupply.Current;
            float movement = unit.MovementPoints.Current;

            Assert.That(unit.TryDeployDOWN(out _), Is.True);

            Assert.That(unit.DaysSupply.Current, Is.LessThan(supply), "a transition consumes supply");

            // ⚠ Compared against the PRE-TRANSITION current, not against Max: the transition re-maxes
            // movement points from the newly active profile, so Max is a moving target and a
            // "Current < Max" assertion can pass or fail on profile MMP rather than on the cost.
            Assert.That(unit.MovementPoints.Current, Is.LessThan(movement),
                "a transition costs 50% of max movement points");
        }

        #endregion // The action-economy gate

        #region Direction and the Fortified collapse

        [Test]
        public void DeployDown_DigsInOneStep()
        {
            var unit = MakeUnit(DeploymentPosition.Deployed);

            Assert.That(unit.TryDeployDOWN(out _), Is.True);
            Assert.That(unit.DeploymentPosition, Is.EqualTo(DeploymentPosition.HastyDefense));
        }

        [Test]
        public void DeployDown_FromFortified_IsRefusedAsAlreadyMinimum()
        {
            var unit = MakeUnit(DeploymentPosition.Fortified);

            Assert.That(unit.TryDeployDOWN(out string error), Is.False);
            Assert.That(error, Does.Contain("minimum"));
        }

        [Test]
        public void DeployUp_FromADugInState_CollapsesStraightToDeployed()
        {
            // §7.9.5.2 shape: breaking out of any dug-in tier returns to bare Deployed in ONE action
            // rather than unwinding Fortified → Entrenched → Hasty a step at a time.
            var unit = MakeUnit(DeploymentPosition.Entrenched);

            Assert.That(unit.TryDeployUP(out _), Is.True);
            Assert.That(unit.DeploymentPosition, Is.EqualTo(DeploymentPosition.Deployed));
        }

        #endregion // Direction and the Fortified collapse

        #region Supply floor

        [Test]
        public void AtCriticalSupply_DeploymentIsRefused()
        {
            var unit = MakeUnit();
            unit.DaysSupply.SetCurrent(GameData.CRITICAL_SUPPLY_THRESHOLD);

            Assert.That(unit.TryDeployDOWN(out string error), Is.False);
            Assert.That(error, Does.Contain("supply"));
        }

        #endregion // Supply floor
    }
}
