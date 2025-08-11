using System;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    public partial class CombatUnit
    {
        #region Fields

        private DeploymentPosition _deploymentPosition = DeploymentPosition.Deployed;

        #endregion


        #region Properties

        public DeploymentPosition DeploymentPosition { get => _deploymentPosition; }
        public bool IsEmbarkable { get; private set; } // Equipped with helicopter/airlift transport/naval.
        public bool IsMountable { get; private set; }  // Equipped with ground transport (e.g., trucks, APCs).

        #endregion //Properties


        #region Initialization

        /// <summary>
        /// Intializes the deployment system for the combat unit.
        /// </summary>
        /// <param name="embarkable"></param>
        /// <param name="mountable"></param>
        private void InitializeDeploymentSystem(bool embarkable, bool mountable)
        {
            IsEmbarkable = embarkable;
            IsMountable = mountable;
            _deploymentPosition = DeploymentPosition.Deployed;
        }

        #endregion

        #region Deployment State Machine

        /// <summary>
        /// Attempt to change the deployment state of the combat unit to a higher level.
        /// </summary>
        public bool TryDeployUP(out string errorMsg, bool onAirbase = false, bool onPort = false)
        {
            errorMsg = string.Empty;

            // Check for an invalid profile.
            if (MovementPoints.Max <= 0f)
            {
                errorMsg = "Unit has invalid movement profile; cannot deploy.";
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

            // Consume movement points for the deployment action BEFORE profile change
            float movementCost = CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST * MovementPoints.Max;
            MovementPoints.SetCurrent(MovementPoints.Current - movementCost);

            // Reset the movement points for the CombatUnit, preserves used movt points.
            UpdateMovementPointsForProfile();

            // Apply the Mobile movement bonus if applicable.
            if (_deploymentPosition == DeploymentPosition.Mobile) ApplyMobileBonus();
            
            return true;
        }

        /// <summary>
        /// Attempt to change the deployment state of the combat unit to a lower level (more defensive).
        /// </summary>
        /// <param name="errorMsg">Error message if deployment fails</param>
        /// <param name="isBeachhead">True if Marines are debarking from sea to land</param>
        /// <returns>True if deployment succeeded, false otherwise</returns>
        public bool TryDeployDOWN(out string errorMsg, bool isBeachhead = false)
        {
            errorMsg = string.Empty;

            // Check for an invalid profile.
            if (MovementPoints.Max <= 0f)
            {
                errorMsg = "Unit has invalid movement profile; cannot deploy.";
                return false;
            }

            // Check if we're already at the lowest deployment level
            if (DeploymentPosition == DeploymentPosition.Fortified)
            {
                errorMsg = $"{UnitName} is already at minimum deployment level (Fortified).";
                return false;
            }

            // Get the current deployment position
            DeploymentPosition oldPosition = _deploymentPosition;

            // Determine target position based on special rules
            DeploymentPosition targetPosition = GetDownwardTargetPosition(oldPosition);

            // Conduct comprehensive checks for state transition
            if (!CanChangeToState(targetPosition, out errorMsg))
                return false;

            // Special checks for debarking from Embarked state
            if (!SpecialDebarkmentChecks(out errorMsg, oldPosition, isBeachhead))
                return false;

            // Execute the state transition
            _deploymentPosition = targetPosition;

            // Consume supplies
            ConsumeSupplies(CUConstants.COMBAT_STATE_SUPPLY_TRANSITION_COST);

            // Decrement the deployment actions
            DeploymentActions.DecrementCurrent();

            // Consume movement points for the deployment action BEFORE profile change
            float movementCost = CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST * MovementPoints.Max;
            MovementPoints.SetCurrent(MovementPoints.Current - movementCost);

            // Reset the movement points for the CombatUnit, preserves used movement points
            UpdateMovementPointsForProfile();

            // Apply the Mobile movement bonus if applicable.
            if (_deploymentPosition == DeploymentPosition.Mobile) ApplyMobileBonus();

            return true;
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
                if (Classification == UnitClassification.AB || Classification == UnitClassification.MAB)
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
                    if (embarkedProfile.WeaponSystemID == WeaponSystems.TRA_AN12 && !onAirbase)
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
                    if (embarkedProfile.WeaponSystemID != WeaponSystems.HEL_MI8T)
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

            // Check if there are enough movement points for the transition
            if (MovementPoints.Current < CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST * MovementPoints.Max)
            {
                errorMessage = $"{UnitName} does not have enough movement points to change states ({MovementPoints.Current:F1} available, {CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST} required)";
                return false;
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

        /// <summary>
        /// Determines the target deployment position when deploying down.
        /// Handles the special case where Embarked always goes to Deployed.
        /// </summary>
        /// <param name="currentPosition">Current deployment position</param>
        /// <returns>Target deployment position</returns>
        private DeploymentPosition GetDownwardTargetPosition(DeploymentPosition currentPosition)
        {
            // Special case: Embarked always goes to Deployed (bypasses Mobile)
            if (currentPosition == DeploymentPosition.Embarked)
            {
                return DeploymentPosition.Deployed;
            }

            // All other positions go down one step linearly
            return currentPosition - 1;
        }

        /// <summary>
        /// Performs special validation checks for debarking from Embarked state.
        /// </summary>
        /// <param name="errorMsg">Error message if checks fail</param>
        /// <param name="currentPosition">Current deployment position</param>
        /// <param name="isBeachhead">True if Marines are debarking from sea to land</param>
        /// <returns>True if checks pass, false otherwise</returns>
        private bool SpecialDebarkmentChecks(out string errorMsg, DeploymentPosition currentPosition, bool isBeachhead)
        {
            errorMsg = string.Empty;

            // Only need special checks when debarking from Embarked state
            if (currentPosition != DeploymentPosition.Embarked)
                return true;

            // Marine units have special debarking requirements
            if (Classification == UnitClassification.MAR || Classification == UnitClassification.MMAR)
            {
                // Marines can debark on beachheads (from sea to land)
                // Other hex legality is handled by external classes
                if (!isBeachhead)
                {
                    // This might be a land-based debarkation, which should be valid
                    // We'll allow it and let other systems validate hex legality
                }
            }

            // Airborne units debarking (parachute landing)
            if (Classification == UnitClassification.AB || Classification == UnitClassification.MAB)
            {
                // Airborne units can debark anywhere their transport can legally be
                // Hex legality validation happens elsewhere
            }

            // Air Mobile units debarking (helicopter landing)
            if (Classification == UnitClassification.AM || Classification == UnitClassification.MAM)
            {
                // Air Mobile units can debark anywhere helicopters can land
                // Hex legality validation happens elsewhere
            }

            // Special Forces have flexible transport options
            if (Classification == UnitClassification.SPECF)
            {
                // Special Forces can debark from any transport type
                // Hex legality validation happens elsewhere
            }

            // All checks passed
            return true;
        }

        #endregion //Deployment State Machine

        #region Utility Methods

        /// <summary>
        /// Direct change of combat state for debugging purposes.
        /// </summary>
        /// <param name="newPosition"></param>
        public void SetDeploymentPosition(DeploymentPosition newPosition)
        {
            _deploymentPosition = newPosition;
        }

        #endregion
    }
}