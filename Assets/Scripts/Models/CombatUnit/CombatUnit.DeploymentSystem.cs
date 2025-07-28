using System;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
/*────────────────────────────────────────────────────────────────────────────
 CombatUnit.DeploymentStateMachine — simplified linear deployment state transitions
 ────────────────────────────────────────────────────────────────────────────────

Summary
═══════
Partial class extension that implements a simplified linear deployment state machine
for CombatUnit. Units progress through deployment states by incrementing or decrementing
enum values, with automatic resource consumption, profile switching, and unit-type
specific validation. Replaces the complex adjacency-based system with streamlined
linear progression while maintaining tactical depth through resource costs and
unit capabilities.

The system handles state transitions from lower enum values (defensive positions) to
higher enum values (mobile/embarked positions), with special logic for dis-entrenchment
and embarked operations requiring specific facility access.

Public Properties
═════════════════
public DeploymentPosition DeploymentPosition { get; } // Current position in state machine
public bool IsEmbarkable { get; private set; } // Can use air/naval transport
public bool IsMountable { get; private set; } // Can use ground transport (APC/IFV)

Public Method Signatures
═══════════════════════
public bool TryDeployUP(out string errorMsg, bool onAirbase = false, bool onPort = false) // Deploy up one state level with validation and resource consumption
public bool TryDeployDOWN(out string errorMsg) // Deploy down one state level (not yet implemented)

Private Method Signatures
════════════════════════
private bool SpecialEmbarkmentChecks(out string errorMsg, DeploymentPosition targetPos, bool onAirbase = false, bool onPort = false) // Validates unit-type specific embarked requirements
private void ApplyMobileBonus() // Adds +2 movement points when entering Mobile state
private void RemoveMobileBonus() // Clears mobile bonus flag when leaving Mobile state
private void UpdateMovementPointsForProfile() // Recalculates movement points based on active weapon profile
private bool CanChangeToState(DeploymentPosition targetState, out string errorMessage) // Validates state transition legality
private bool CanUnitTypeChangeStates() // Checks if unit classification allows state changes

Important Implementation Details
═══════════════════════════════
• **Linear Progression**: States progress via enum arithmetic (_deploymentPosition + 1 for up, -1 for down)
  eliminating complex adjacency rules while maintaining tactical resource management.

• **Automatic Profile Switching**: GetActiveWeaponSystemProfile() handles profile transitions automatically
  based on deployment state, with Embarked using transport profiles, Mobile using mounted profiles,
  and other states using deployed profiles.

• **Dis-entrenchment Logic**: Units in Fortified or Entrenched positions are forced to Deployed
  state before embarking, representing abandonment of defensive positions.

• **Resource Consumption**: Each transition consumes 1 deployment action, 0.25 days supply,
  and resets movement points based on the new active profile.

• **Unit-Type Restrictions**: Airborne units require airbases for embarked transitions,
  Marines require ports, Special Forces have flexible transport options, and fixed-wing
  aircraft/bases cannot change states.

• **Mobile Bonus System**: Units entering Mobile state receive +2 movement points bonus
  applied after profile-based movement point calculation, with bonus flag tracked for serialization.

• **Movement Point Gating**: All state transitions require sufficient movement points based
  on percentage costs of maximum movement, preventing exploitation while maintaining turn flow.

────────────────────────────────────────────────────────────────────────────── */
    public partial class CombatUnit
    {
        #region Fields

        private DeploymentPosition _deploymentPosition = DeploymentPosition.Deployed;
        private bool _mobileBonusApplied = false;                  // Persisted runtime flag, true when MOBILE_MOVEMENT_BONUS active.
        private const string SERIAL_KEY_MOBILE_BONUS = "mobBonus"; // Serialization identifier for _mobileBonusApplied

        #endregion


        #region Properties

        public DeploymentPosition DeploymentPosition { get => _deploymentPosition; }
        public bool IsEmbarkable { get; private set; } // Equipped with helicopter/airlift transport/naval.
        public bool IsMountable { get; private set; }  // Equipped with ground transport (e.g., trucks, APCs).

        #endregion //Properties


        #region Deployment State Machine

        /// <summary>
        /// Attempt to change the deployment state of the combat unit to a higher level.
        /// </summary>
        public bool TryDeployUP(out string errorMsg, bool onAirbase = false, bool onPort = false)
        {
            errorMsg = string.Empty;

            // Checks if up deployment is allowed based on current state.
            if (DeploymentPosition == DeploymentPosition.Embarked)
            {
                errorMsg = $"{UnitName} is embarked and cannot deploy up.";
                return false;
            }

            // Get the current deployment position.
            DeploymentPosition oldPosition = _deploymentPosition;

            // Compute target position.
            DeploymentPosition targetPosition = _deploymentPosition + 1;

            // Conduct comprehensive checks for state transition.
            if (!CanChangeToState(targetPosition, out errorMsg))
                return false;

            // Embarkment requires several specialized checks.
            if (!SpecialEmbarkmentChecks(out errorMsg, targetPosition, onAirbase, onPort)) 
                return false;

            // When fortified or entrenched, we must transition to deployed first.
            if (oldPosition == DeploymentPosition.Fortified || oldPosition == DeploymentPosition.Entrenched)
            {
                _deploymentPosition = DeploymentPosition.Deployed;
            }
            // Transition as normal.
            else _deploymentPosition = targetPosition;

            // Consume supplies.
            ConsumeSupplies(CUConstants.COMBAT_STATE_SUPPLY_TRANSITION_COST);

            // Decrement the deployment actions.
            DeploymentActions.DecrementCurrent();

            // Reset the movement points for the CombatUnit.
            UpdateMovementPointsForProfile();

            // Apply or remove the Mobile movement bonus.
            if (targetPosition == DeploymentPosition.Mobile) ApplyMobileBonus();
            else RemoveMobileBonus();
            
            return true;
        }

        public bool TryDeployDOWN()
        {
            return false;
        }

        /// <summary>
        /// Performs special checks to determine if a unit can be deployed to an embarked position.
        /// </summary>
        /// <remarks>This method checks various conditions based on the unit's classification and current
        /// location to determine if it can be deployed to an embarked position. Specific requirements include having a
        /// valid embarked profile and being located on an airbase or port, depending on the unit type.</remarks>
        /// </summary>
        private bool SpecialEmbarkmentChecks(out string errorMsg,
            DeploymentPosition targetPos,
            bool onAirbase = false,
            bool onPort = false)
        {
            errorMsg = string.Empty;

            // Conduct special embarkment checks.
            if (targetPos == DeploymentPosition.Embarked)
            {
                // Get the embarked profile for this unit.
                var embarkedProfile = GetEmbarkedProfile();

                // To embark, the unit must have a valid embarked profile.
                if (embarkedProfile == null)
                {
                    errorMsg = $"{UnitName} has no embarked profile and cannot deploy to Embarked position.";
                    return false;
                }

                // Airborne and mechanized airborne must be on an airbase to deploy to Embarked position.
                if (Classification == UnitClassification.AIRB || Classification == UnitClassification.MAB)
                {
                    if (!onAirbase)
                    {
                        errorMsg = $"{UnitName} must be on an airbase to deploy to Embarked position.";
                        return false;
                    }
                }

                // Special forces require their own check as they can be on either a helo or a transport aircraft.
                if (Classification == UnitClassification.SPECF)
                {
                    // Special forces with aircraft transport must be on an airbase.
                    if (embarkedProfile.WeaponSystemID == WeaponSystems.TRANSAIR_AN12 && !onAirbase)
                    {
                        errorMsg = $"{UnitName} must be on an airbase to deploy to Embarked position with AN-12 transport.";
                        return false;
                    }
                    // Or else they have helos and can embark from any hex.
                }

                // Marine units and mechanized marines must be on a port to deploy to Embarked position.
                if (Classification == UnitClassification.MAR || Classification == UnitClassification.MMAR)
                {
                    // Marine units must be on a port to deploy to Embarked position.
                    if (!onPort)
                    {
                        errorMsg = $"{UnitName} must be on a port to deploy to Embarked position.";
                        return false;
                    }
                }

                // Airmobile and mechanized airmobile units must have a valid helicopter transport profile to deploy to Embarked position.
                if (Classification == UnitClassification.AM || Classification == UnitClassification.MAM)
                {
                    if (embarkedProfile.WeaponSystemID != WeaponSystems.TRANSHELO_MI8)
                    {
                        errorMsg = $"{UnitName} must have a valid helicopter transport profile to deploy to Embarked position.";
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Apply a movement point bonus to the combat unit when it enters the Mobile state.
        /// </summary>
        private void ApplyMobileBonus()
        {
            // Compute and clamp new movement points.
            float newMax = Mathf.Max(0f, MovementPoints.Max + CUConstants.MOBILE_MOVEMENT_BONUS);
            float newCurrent = Mathf.Clamp(MovementPoints.Current + CUConstants.MOBILE_MOVEMENT_BONUS, 0f, newMax);

            // Set new movement points.
            MovementPoints.SetMax(newMax);
            MovementPoints.SetCurrent(newCurrent);

            // Set the mobile bonus flag to true.
            _mobileBonusApplied = true;
        }

        /// <summary>
        /// Removes the mobile bonus from the movement points if it has been applied.
        /// </summary>
        private void RemoveMobileBonus()
        {
            // Set the mobile bonus flag to false.
            _mobileBonusApplied = false;
        }

        /// <summary>
        /// Updates the movement points for the active weapon system profile.
        /// </summary>
        /// <remarks>This method retrieves the active weapon system profile and updates the maximum
        /// movement points based on the profile's settings. The current movement points are adjusted proportionally to
        /// maintain the same percentage of the new maximum. If no active profile is available, an <see
        /// cref="InvalidOperationException"/> is thrown.</remarks>
        private void UpdateMovementPointsForProfile()
        {
            try
            {
                // Get the active weapon system profile for the combat unit.
                var activeProfile = GetActiveWeaponSystemProfile();
                if (activeProfile == null)
                    throw new InvalidOperationException("No active weapon system profile available");

                // Set the new maximum movement points based on the active profile.
                int newMaxMovement = activeProfile.MovementPoints;

                // Calculate current movement as percentage of old max
                float movementPercentage = MovementPoints.Max > 0
                    ? MovementPoints.Current / MovementPoints.Max
                    : 0f;

                // Set new max and scale current proportionally
                MovementPoints.SetMax(newMaxMovement);
                float newCurrent = newMaxMovement * movementPercentage;
                MovementPoints.SetCurrent(newCurrent);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateMovementPointsForProfile", e);
            }
        }

        /// <summary>
        /// Checks if the unit can transition to the specified combat state.
        /// Validates unit type restrictions, adjacency rules, and resource requirements.
        /// </summary>
        /// <param name="targetState">The desired combat state</param>
        /// <returns>True if transition is allowed</returns>
        private bool CanChangeToState(DeploymentPosition targetState, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Same state - no change needed
            if (DeploymentPosition == targetState)
            {
                errorMessage = $"Already in target state {targetState}";
                return false;
            }

            // Check if the unit is destroyed
            if (IsDestroyed())
            {
                errorMessage = $"{UnitName} is destroyed and cannot change states";
                throw new InvalidOperationException(errorMessage);
            }

            // Air units and bases cannot change states
            if (!CanUnitTypeChangeStates())
            {
                errorMessage = $"{UnitName} cannot change combat states (unit type: {Classification})";
                return false;
            }

            // Check if the unit has critical supply levels
            if (DaysSupply.Current <= CUConstants.CRITICAL_SUPPLY_THRESHOLD)
            {
                errorMessage = $"Cannot change state with critical supply levels ({DaysSupply.Current:F1} days remaining)";
                return false;
            }

            // Only limited DeploymentStatus transitions are allowed based on efficiency level
            if (EfficiencyLevel == EfficiencyLevel.StaticOperations)
            {
                // Static Operations allow only certain transitions
                if (DeploymentPosition == DeploymentPosition.Fortified ||
                    DeploymentPosition == DeploymentPosition.Entrenched ||
                    DeploymentPosition == DeploymentPosition.HastyDefense)
                {
                    errorMessage = $"Cannot change from defensive states in Static Operations (current efficiency: {EfficiencyLevel})";
                    return false;
                }

                // Static Operations cannot transition to Mobile or InTransit states
                if (targetState == DeploymentPosition.Mobile)
                {
                    errorMessage = $"Cannot change to Mobile state in Static Operations (current efficiency: {EfficiencyLevel})";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if this unit classification can change combat states.
        /// Fixed-wing aircraft and bases cannot change states.
        /// </summary>
        /// <returns>True if unit can change states</returns>
        private bool CanUnitTypeChangeStates()
        {
            // Fixed-wing aircraft cannot change states
            if (Classification == UnitClassification.ASF ||
                Classification == UnitClassification.MRF ||
                Classification == UnitClassification.ATT ||
                Classification == UnitClassification.BMB ||
                Classification == UnitClassification.RECONA)
            {
                return false;
            }

            // Bases cannot change states
            if (Classification == UnitClassification.HQ ||
                Classification == UnitClassification.DEPOT ||
                Classification == UnitClassification.AIRB)
            {
                return false;
            }

            // All other units (including helicopters) can change states
            return true;
        }

        #endregion //Deployment State Machine
    }
}