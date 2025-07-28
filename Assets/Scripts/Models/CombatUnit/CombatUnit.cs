using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Extension helpers for UnitClassification.
    /// </summary>
    public static class UnitClassificationExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if the classification represents a fixed facility (HQ, DEPOT, AIRB).
        /// </summary>
        public static bool IsBaseType(this UnitClassification classification) =>
            classification == UnitClassification.HQ ||
            classification == UnitClassification.DEPOT ||
            classification == UnitClassification.AIRB;
    }

   
    [Serializable]
    public partial class CombatUnit : ICloneable, ISerializable, IResolvableReferences
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        #endregion


        #region Properties

        // Identification and metadata
        public string UnitName { get; set; }
        public string UnitID { get; private set; }
        public UnitType UnitType { get; private set; }
        public UnitClassification Classification { get; private set; }
        public UnitRole Role { get; private set; }
        public Side Side { get; private set; }
        public Nationality Nationality { get; private set; }
        public bool IsBase => Classification.IsBaseType();

        // Profile IDs
        // Profile for air, helo, naval transport, if available.
        public WeaponSystems EmbarkedProfileID { get; private set; }

        // Profile for units that have organic transport (Truck/APC/IFV), if available.
        public WeaponSystems MobileProfileID { get; private set; }

        // The default, core profile that all units have.
        public WeaponSystems DeployedProfileID { get; private set; }

        // The profile used to generate intelligence reports.
        public IntelProfileTypes IntelProfileType { get; internal set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelActions { get; private set; }

        // State data
        public StatsMaxCurrent HitPoints { get; private set; }
        public StatsMaxCurrent DaysSupply { get; private set; }
        public StatsMaxCurrent MovementPoints { get; private set; }
        public Coordinate2D MapPos { get; internal set; }
        public SpottedLevel SpottedLevel { get; private set; }

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public CombatUnit(string unitName,
            UnitType unitType,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            IntelProfileTypes intelProfileType,
            WeaponSystems deployedProfileID,
            bool isMountable = false,
            WeaponSystems mobileProfileID = WeaponSystems.DEFAULT,
            bool isEmbarkable = false,
            WeaponSystems embarkProfileID = WeaponSystems.DEFAULT,
            DepotCategory category = DepotCategory.Secondary,
            DepotSize size = DepotSize.Small)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(unitName))
                    throw new ArgumentException("Unit name cannot be null or empty", nameof(unitName));

                // Deployed profile ID must point to a valid profile.
                if (deployedProfileID == WeaponSystems.DEFAULT)
                    throw new ArgumentException("Deployed profile ID cannot be DEFAULT", nameof(deployedProfileID));

                // Check that deployed profile exists in database.
                if (WeaponSystemsDatabase.GetWeaponSystemProfile(deployedProfileID) == null)
                    throw new ArgumentException($"Deployed profile ID {deployedProfileID} not found in database", nameof(deployedProfileID));

                // When not DEFAULT, mounted profile ID must point to a valid profile.
                if (isMountable && mobileProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(mobileProfileID) == null)
                        throw new ArgumentException($"Mounted profile ID {mobileProfileID} not found in database", nameof(mobileProfileID));
                }

                // When not DEFAULT, transport profile ID must point to a valid profile.
                if (isEmbarkable && embarkProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(embarkProfileID) == null)
                        throw new ArgumentException($"Transport profile ID {embarkProfileID} not found in database", nameof(embarkProfileID));
                }

                // Validate intel profile type
                if (!Enum.IsDefined(typeof(IntelProfileTypes), intelProfileType))
                    throw new ArgumentException("Invalid intel profile type", nameof(intelProfileType));

                //---------------------------------
                // Set identification and metadata
                //---------------------------------

                // Basic argument validation
                if (string.IsNullOrWhiteSpace(unitName))
                    throw new ArgumentException("Unit name cannot be null or whitespace", nameof(unitName));
                UnitName = unitName.Trim();
                UnitID = Guid.NewGuid().ToString();
                UnitType = unitType;
                Classification = classification;
                Role = role;
                Side = side;
                Nationality = nationality;

                // Initialize deployment position based on embarkable and mountable states
                InitializeDeploymentSystem(isEmbarkable, isMountable);

                // Set profile IDs
                EmbarkedProfileID = embarkProfileID;
                DeployedProfileID = deployedProfileID;
                MobileProfileID = mobileProfileID;
                IntelProfileType = intelProfileType;

                // Initialise facility data when appropriate
                if (IsBase)
                {
                    InitializeFacility(category, size);
                }

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize the leader system
                InitializeLeaderSystem();

                // Set the initial spotted level
                SpottedLevel = SpottedLevel.Level1;

                // Initialize experience system
                InitializeExperienceSystem();

                // Initialize StatsMaxCurrent properties
                HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
                DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

                // Initialize movement based on unit classification
                InitializeMovementPoints();

                // Initialize position to origin (will be set when placed on map)
                MapPos = Coordinate2D.Zero;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Core

        /// <summary>
        /// Retrieves the weapon‑system profile the unit employs in its deployed state.
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetDeployedProfile()
        {
            try
            {
                // "No profile" is a valid state.
                if (DeployedProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(DeployedProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Deployed profile ID '{DeployedProfileID}' not found in weapon‑systems database.");

                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetDeployedProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the weapon‑system profile for the mobile state.
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetMobileProfile()
        {
            try
            {
                // DEFAULT means the unit is foot‑mobile (no transport profile).
                if (MobileProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(MobileProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Mobile profile ID '{MobileProfileID}' not found in weapon‑systems database.");

                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetMobileProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the weapon‑system profile the unit employs when in transport (airlift/sea).
        /// </summary>
        /// <returns></returns>
        public WeaponSystemProfile GetEmbarkedProfile()
        {
            try
            {
                // DEFAULT means the unit has no transport profile.
                if (EmbarkedProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(EmbarkedProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Embarked profile ID '{EmbarkedProfileID}' not found in weapon‑systems database.");
                
                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetEmbarkedProfile), ex);
                return null;
            }
        }

        /// <summary>
        /// Generates an intelligence report for this unit based on the specified spotted level.
        /// Uses the static IntelProfile system to create fog-of-war filtered intelligence data.
        /// </summary>
        /// <param name="spottedLevel">Intelligence accuracy level (default Level1)</param>
        /// <returns>IntelReport with unit intelligence data, or null if not spotted</returns>
        public IntelReport GenerateIntelReport(SpottedLevel spottedLevel = SpottedLevel.Level1)
        {
            try
            {
                return IntelProfile.GenerateIntelReport(
                    IntelProfileType,
                    UnitName,
                    (int)HitPoints.Current,
                    Nationality,
                    DeploymentPosition,
                    ExperienceLevel,
                    EfficiencyLevel,
                    spottedLevel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateIntelReport", e);
                return null;
            }
        }

        /// <summary>
        /// Refreshes all action counts to their maximum values.
        /// Called at the start of each turn.
        /// </summary>
        public void RefreshAllActions()
        {
            MoveActions.ResetToMax();
            CombatActions.ResetToMax();
            DeploymentActions.ResetToMax();
            OpportunityActions.ResetToMax();
            IntelActions.ResetToMax();
        }

        /// <summary>
        /// Refreshes movement points to maximum.
        /// Called at the start of each turn.
        /// </summary>
        public void RefreshMovementPoints()
        {
            MovementPoints.ResetToMax();
        }

        /// <summary>
        /// Sets the spotted level for the current instance.
        /// </summary>
        /// <param name="spottedLevel">The new spotted level to assign.</param>
        public void SetSpottedLevel(SpottedLevel spottedLevel)
        {
            SpottedLevel = spottedLevel;
        }

        /// <summary>
        /// Retrieves the active weapon system profile based on the current mounted state.
        /// </summary>
        /// <returns>The active <see cref="WeaponSystemProfile"/>. Returns the <see cref="MountedProfile"/> if the system is
        /// mounted; otherwise, returns the <see cref="DeployedProfile"/>.</returns>
        public WeaponSystemProfile GetActiveWeaponSystemProfile()
        {
            return DeploymentPosition switch
            {
                DeploymentPosition.Embarked => GetEmbarkedProfile() ?? GetDeployedProfile(),
                DeploymentPosition.Mobile => GetMobileProfile() ?? GetDeployedProfile(),
                _=> GetDeployedProfile()
            };
        }

        /// <summary>
        /// Get the adjusted combat strength based on current mounted state and all applicable modifiers.
        /// </summary>
        /// <returns>Temporary WeaponSystemProfile with modified combat values for immediate calculation</returns>
        public WeaponSystemProfile GetCurrentCombatStrength()
        {
            try
            {
                // Ensure we have a valid active weapon system profile
                var activeProfile = GetActiveWeaponSystemProfile();
                if (activeProfile == null)
                    throw new InvalidOperationException("Active weapon system profile is null");

                // Modify the constructor call to specify the correct parameters for WeaponSystemProfile
                var tempProfile = new WeaponSystemProfile(
                    activeProfile.Name + "_Combat",
                    activeProfile.Nationality,
                    WeaponSystems.COMBAT,
                    0
                );

                // Compute all modifiers that can affect a combat rating
                float finalModifier = GetFinalCombatRatingModifier();
                float finalModifier_Air = GetFinalCombatRatingModifier_Aircraft();

                // Copy and apply modifiers to combat ratings for ground units.
                tempProfile.SetHardAttack(Mathf.CeilToInt(activeProfile.HardAttack * finalModifier));
                tempProfile.SetHardDefense(Mathf.CeilToInt(activeProfile.HardDefense * finalModifier));
                tempProfile.SetSoftAttack(Mathf.CeilToInt(activeProfile.SoftAttack * finalModifier));
                tempProfile.SetSoftDefense(Mathf.CeilToInt(activeProfile.SoftDefense * finalModifier));
                tempProfile.SetGroundAirAttack(Mathf.CeilToInt(activeProfile.GroundAirAttack * finalModifier));
                tempProfile.SetGroundAirDefense(Mathf.CeilToInt(activeProfile.GroundAirDefense * finalModifier));

                // Copy and apply modifiers to combat ratings for air units.
                tempProfile.SetDogfighting(Mathf.CeilToInt(activeProfile.Dogfighting * finalModifier_Air));
                tempProfile.SetManeuverability(activeProfile.Maneuverability);
                tempProfile.SetTopSpeed(activeProfile.TopSpeed);
                tempProfile.SetSurvivability(activeProfile.Survivability);
                tempProfile.SetGroundAttack(activeProfile.GroundAttack);
                tempProfile.SetOrdinanceLoad(Mathf.CeilToInt(activeProfile.OrdinanceLoad * finalModifier_Air));
                tempProfile.SetStealth(activeProfile.Stealth);

                return tempProfile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetCurrentCombatStrength", e);
                return null;
            }
        }

        /// <summary>
        /// Applies damage to the unit, reducing hit points and updating combat effectiveness.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        public void TakeDamage(float damage)
        {
            try
            {
                if (damage < 0f)
                {
                    throw new ArgumentException("Damage cannot be negative", nameof(damage));
                }

                if (damage == 0f)
                {
                    return; // No damage to apply
                }

                // Apply damage to hit points
                float newHitPoints = Mathf.Max(CUConstants.MIN_HP, HitPoints.Current - damage);
                HitPoints.SetCurrent(newHitPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "TakeDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Repairs damage to the unit, restoring hit points.
        /// </summary>
        /// <param name="repairAmount">Amount of damage to repair</param>
        public void Repair(float repairAmount)
        {
            try
            {
                if (repairAmount < 0f)
                {
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));
                }

                if (repairAmount == 0f)
                {
                    return; // No repair to apply
                }

                // Apply repair to hit points (clamped to maximum)
                float newHitPoints = Mathf.Min(HitPoints.Max, HitPoints.Current + repairAmount);
                HitPoints.SetCurrent(newHitPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Repair", e);
                throw;
            }
        }

        /// <summary>
        /// Consumes supplies for unit operations.
        /// </summary>
        /// <param name="amount">Amount of supplies to consume</param>
        /// <returns>True if supplies were consumed, false if insufficient</returns>
        public bool ConsumeSupplies(float amount)
        {
            try
            {
                if (amount < 0f)
                {
                    throw new ArgumentException("Supply amount cannot be negative", nameof(amount));
                }

                if (amount == 0f)
                {
                    return true; // No supplies to consume
                }

                if (DaysSupply.Current >= amount)
                {
                    DaysSupply.SetCurrent(DaysSupply.Current - amount);
                    return true;
                }

                return false; // Insufficient supplies
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeSupplies", e);
                return false;
            }
        }

        /// <summary>
        /// Receives supplies from external source (depot, transport, etc.).
        /// </summary>
        /// <param name="amount">Amount of supplies offered</param>
        /// <returns>Actual amount of supplies received (may be less than offered due to capacity)</returns>
        public float ReceiveSupplies(float amount)
        {
            try
            {
                if (amount < 0f)
                {
                    throw new ArgumentException("Supply amount cannot be negative", nameof(amount));
                }

                if (amount == 0f)
                {
                    return 0f; // No supplies offered
                }

                // Calculate how much we can actually receive
                float availableCapacity = DaysSupply.Max - DaysSupply.Current;
                float actualAmount = Mathf.Min(amount, availableCapacity);

                // Add supplies
                DaysSupply.SetCurrent(DaysSupply.Current + actualAmount);

                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ReceiveSupplies", e);
                return 0f;
            }
        }

        /// <summary>
        /// Checks if the unit is destroyed (no hit points remaining).
        /// </summary>
        /// <returns>True if the unit is destroyed</returns>
        public bool IsDestroyed()
        {
            return HitPoints.Current <= 1f;
        }

        /// <summary>
        /// Checks if the unit can move based on various factors.
        /// </summary>
        /// <returns></returns>
        public bool CanMove()
        {
            try
            {
                // Must have hit points to operate
                if (IsDestroyed())
                {
                    return false;
                }

                // Must have some supplies for most operations.
                if (DaysSupply.Current < 1f)
                {
                    return false;
                }

                // Low efficiency level units cannot move.
                if (EfficiencyLevel == EfficiencyLevel.StaticOperations)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanMove", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the supply status as a percentage of maximum capacity.
        /// </summary>
        /// <returns>Supply status from 0.0 to 1.0</returns>
        public float GetSupplyStatus()
        {
            try
            {
                if (DaysSupply.Max <= 0f)
                {
                    return 0f;
                }

                return DaysSupply.Current / DaysSupply.Max;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSupplyStatus", e);
                return 0f;
            }
        }

        /// <summary>
        /// Sets the efficiency level for the application.
        /// </summary>
        /// <remarks>This method updates the application's efficiency level to the specified value. 
        /// Ensure that the provided <paramref name="level"/> is a valid enumeration value to avoid
        /// exceptions.</remarks>
        /// <param name="level">The desired efficiency level to set. Must be a valid value of the <see cref="EfficiencyLevel"/> enumeration.</param>
        public void SetEfficiencyLevel(EfficiencyLevel level)
        {
            try
            {
                // Validate the new level
                if (!Enum.IsDefined(typeof(EfficiencyLevel), level))
                {
                    throw new ArgumentOutOfRangeException(nameof(level), "Invalid efficiency level");
                }
                // Set the new efficiency level
                EfficiencyLevel = level;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetEfficiencyLevel", e);
            }
        }

        /// <summary>
        /// Decreases the efficiency level by 1, if possible.
        /// </summary>
        public void DecreaseEfficiencyLevelBy1()
        {
            try
            {
                if (EfficiencyLevel > EfficiencyLevel.StaticOperations)
                    EfficiencyLevel--;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DecreaseEfficiencyLevel", e);
            }
        }

        /// <summary>
        /// Increases the efficiency level by 1, if possible.
        /// </summary>
        public void IncreaseEfficiencyLevelBy1()
        {
            try
            {
                if (EfficiencyLevel < EfficiencyLevel.PeakOperational)
                    EfficiencyLevel++;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "IncreaseEfficiencyLevel", e);
            }
        }

        /// <summary>
        /// Initializes action counts based on unit type and classification.
        /// Most units get standard action counts, with variations for special cases.
        /// </summary>
        private void InitializeActionCounts()
        {
            // Standard action counts for most units
            int moveActions = CUConstants.DEFAULT_MOVE_ACTIONS;
            int combatActions = CUConstants.DEFAULT_COMBAT_ACTIONS;
            int deploymentActions = CUConstants.DEFAULT_DEPLOYMENT_ACTIONS;
            int opportunityActions = CUConstants.DEFAULT_OPPORTUNITY_ACTIONS;
            int intelActions = CUConstants.DEFAULT_INTEL_ACTIONS;


            switch (Classification)
            {
                case UnitClassification.TANK:
                case UnitClassification.MECH:
                case UnitClassification.MOT:
                case UnitClassification.AB:
                case UnitClassification.MAB:
                case UnitClassification.MAR:
                case UnitClassification.MMAR:
                case UnitClassification.AT:
                case UnitClassification.INF:
                case UnitClassification.ART:
                case UnitClassification.SPA:
                case UnitClassification.ROC:
                case UnitClassification.BM:
                case UnitClassification.ENG:
                case UnitClassification.HELO:
                    break;

                case UnitClassification.RECON:
                    moveActions += 1;
                    break;

                case UnitClassification.AM:
                case UnitClassification.MAM:
                    deploymentActions += 1;
                    break;

                case UnitClassification.SPECF:
                    intelActions += 1;
                    break;

                case UnitClassification.SAM:
                case UnitClassification.SPSAM:
                case UnitClassification.AAA:
                case UnitClassification.SPAAA:
                    opportunityActions += 1;
                    break;

                case UnitClassification.ASF:
                case UnitClassification.MRF:
                case UnitClassification.ATT:
                case UnitClassification.BMB:
                case UnitClassification.RECONA:
                    moveActions += 2;
                    deploymentActions = 0;
                    break;

                case UnitClassification.HQ:
                    intelActions += 1;
                    break;

                case UnitClassification.DEPOT:
                case UnitClassification.AIRB:
                    moveActions = 0;
                    combatActions = 0;
                    deploymentActions = 0;
                    opportunityActions = 0;
                    intelActions = 0;
                    break;

                default:
                    break;
            }

            // Create StatsMaxCurrent instances
            MoveActions = new StatsMaxCurrent(moveActions);
            CombatActions = new StatsMaxCurrent(combatActions);
            DeploymentActions = new StatsMaxCurrent(deploymentActions);
            OpportunityActions = new StatsMaxCurrent(opportunityActions);
            IntelActions = new StatsMaxCurrent(intelActions);
        }

        /// <summary>
        /// Initializes movement points based on unit classification.
        /// </summary>
        private void InitializeMovementPoints()
        {
            try
            {
                // Start with deployed profile movement points
                var deployedProfile = GetDeployedProfile();
                if (deployedProfile == null)
                    throw new InvalidOperationException("Unit must have a valid deployed profile");

                MovementPoints = new StatsMaxCurrent(deployedProfile.MovementPoints);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "InitializeMovementPoints", e);
                // Fallback to foot movement if profile access fails
                MovementPoints = new StatsMaxCurrent(CUConstants.FOOT_UNIT);
            }
        }

        /// <summary>
        /// Calculates the final combat rating modifier by combining various contributing factors.
        /// </summary>
        /// <remarks>This method aggregates multiple modifiers, including strength, combat state,
        /// efficiency, and experience, to compute the final combat rating modifier. If an error occurs during
        /// calculation, the method returns a default neutral modifier of 1.0.</remarks>
        /// <returns>A <see cref="float"/> representing the final combat rating modifier. The value is the product of all
        /// contributing modifiers, or 1.0 if an error occurs.</returns>
        private float GetFinalCombatRatingModifier()
        {
            try
            {
                // Calculate the final combat rating modifier based on all factors.
                float strengthModifier = GetStrengthModifier();
                float combatStateModifier = GetCombatStateModifier();
                float efficiencyModifier = GetEfficiencyModifier();
                float experienceModifier = GetExperienceMultiplier();

                // Combine all modifiers
                return strengthModifier * combatStateModifier * efficiencyModifier * experienceModifier;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetFinalCombatRatingModifier", e);
                return 1.0f; // Default to neutral modifier on error
            }
        }

        /// <summary>
        /// Calculates the final combat rating modifier specifically for aircraft units.
        /// </summary>
        /// <returns>Multiplier</returns>
        private float GetFinalCombatRatingModifier_Aircraft()
        {
            try
            {
                // Calculate the final combat rating modifier based on all factors.
                float strengthModifier = GetStrengthModifier();
                float efficiencyModifier = GetEfficiencyModifier();
                float experienceModifier = GetExperienceMultiplier();
                // Combine all modifiers
                return strengthModifier * efficiencyModifier * experienceModifier;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetFinalCombatRatingModifier_Aircraft", e);
                return 1.0f; // Default to neutral modifier on error
            }
        }

        /// <summary>
        /// Gets the combat effectiveness as a percentage based on current hit points.
        /// </summary>
        /// <returns>Combat effectiveness from 0.0 to 1.0</returns>
        private float GetStrengthModifier()
        {
            try
            {
                // Compute the combat strength multiplier based on hit points.
                if (HitPoints.Current >= (HitPoints.Max * CUConstants.FULL_STRENGTH_FLOOR))
                {
                    return CUConstants.STRENGTH_MOD_FULL;
                }
                else if (HitPoints.Current >= (HitPoints.Max * CUConstants.DEPLETED_STRENGTH_FLOOR))
                {
                    return CUConstants.STRENGTH_MOD_DEPLETED;
                }
                else
                {
                    return CUConstants.STRENGTH_MOD_LOW;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetStrengthModifier", e);
                return CUConstants.STRENGTH_MOD_LOW;
            }
        }

        /// <summary>
        /// Calculates the combat state modifier based on the current combat state.
        /// </summary>
        /// <returns>A <see cref="float"/> representing the combat state multiplier. The value corresponds to the current combat
        /// state, with predefined modifiers for specific states. Returns <c>1.0f</c> if the combat state is not
        /// recognized.</returns>
        private float GetCombatStateModifier()
        {
            // Returns the combat state multiplier based on current combat state.
            return DeploymentPosition switch
            {
                DeploymentPosition.Mobile => CUConstants.COMBAT_MOD_MOBILE,
                DeploymentPosition.Deployed => CUConstants.COMBAT_MOD_DEPLOYED,
                DeploymentPosition.HastyDefense => CUConstants.COMBAT_MOD_HASTY_DEFENSE,
                DeploymentPosition.Entrenched => CUConstants.COMBAT_MOD_ENTRENCHED,
                DeploymentPosition.Fortified => CUConstants.COMBAT_MOD_FORTIFIED,
                _ => 1.0f, // Default multiplier for other states
            };
        }

        /// <summary>
        /// Calculates the efficiency modifier based on the current efficiency level.
        /// </summary>
        /// <remarks>The returned modifier is determined by the current <c>EfficiencyLevel</c> and maps to
        /// specific  constants defined in <c>CUConstants</c>. If the efficiency level is unrecognized, a default 
        /// static modifier is returned.</remarks>
        /// <returns>A <see cref="float"/> representing the efficiency modifier. The value corresponds to the current 
        /// operational state, with predefined constants for each efficiency level.</returns>
        private float GetEfficiencyModifier()
        {
            // Returns the efficiency modifier based on current efficiency level.
            return EfficiencyLevel switch
            {
                EfficiencyLevel.PeakOperational => CUConstants.EFFICIENCY_MOD_PEAK,
                EfficiencyLevel.FullyOperational => CUConstants.EFFICIENCY_MOD_FULL,
                EfficiencyLevel.Operational => CUConstants.EFFICIENCY_MOD_OPERATIONAL,
                EfficiencyLevel.DegradedOperations => CUConstants.EFFICIENCY_MOD_DEGRADED,
                _ => CUConstants.EFFICIENCY_MOD_STATIC, // Default multiplier for other states
            };
        }

        #endregion // Core


        #region CombatUnit Actions

        /// <summary>
        /// Consume actions, movement points, and supplies to perform a combat action.
        /// </summary>
        /// <returns>Success/Failure</returns>
        public bool PerformCombatAction()
        {
            try
            {
                // Make sure the unit has enough combat actions, movement points, and supplies to perform a combat action.
                if (CombatActions.Current >= 1 && 
                    MovementPoints.Current >= GetCombatMovementCost() &&
                    DaysSupply.Current >= CUConstants.COMBAT_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    // Deduct one combat action
                    CombatActions.DecrementCurrent();

                    // Consume movement points
                    ConsumeMovementPoints(GetCombatMovementCost());

                    // Consume supplies for the combat action
                    ConsumeSupplies(CUConstants.COMBAT_ACTION_SUPPLY_COST);

                    return true; // Combat action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough combat actions, movement points, or supplies to perform a combat action.");
                    return false; // Not enough resources to perform combat action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformCombatAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consume the required actions, movement points, and supplies to perform a move action.
        /// </summary>
        /// <param name="movtCost"></param>
        /// <returns>Success/Failure</returns>
        public bool PerformMoveAction(int movtCost)
        {
            try
            {
                // Make sure the unit has enough move actions, movement points, and supplies to perform a move action.
                if (MoveActions.Current >= 1 &&
                    MovementPoints.Current >= movtCost &&
                    DaysSupply.Current >= (movtCost * CUConstants.MOVE_ACTION_SUPPLY_COST) + CUConstants.MOVE_ACTION_SUPPLY_THRESHOLD &&
                    !IsBase)
                {
                    // Decrement move actions
                    MoveActions.DecrementCurrent();

                    // Consume movement points
                    ConsumeMovementPoints(movtCost);

                    // Consume supplies for the move action
                    ConsumeSupplies(movtCost * CUConstants.MOVE_ACTION_SUPPLY_COST);

                    return true; // Move action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough move actions, movement points, or supplies to perform a move action.");
                    return false; // Not enough resources to perform move action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformMoveAction", e);
                return false;
            }
        }
       
       /// <summary>
       /// Attempts to perform an intel action for the unit, consuming the required resources.
       /// </summary>
       /// <remarks>This method checks whether the unit has sufficient intel actions, movement points, and
       /// supplies to perform an intel action. If the required resources are available, they are consumed accordingly,
       /// and the action is performed. If the resources are insufficient, the method logs a message and returns <see
       /// langword="false"/>.</remarks>
       /// <returns><see langword="true"/> if the intel action is successfully performed; otherwise, <see langword="false"/>.</returns>
       public bool PerformIntelAction()
        {
            try
            {
                // Make sure the unit has enough intel actions, movement points, and supplies to perform an intel action.
                if (IntelActions.Current >= 1 &&
                    MovementPoints.Current >= GetIntelMovementCost() &&
                    DaysSupply.Current >= CUConstants.INTEL_ACTION_SUPPLY_COST)
                {
                    // Decrement intel actions
                    IntelActions.DecrementCurrent();

                    // Consume movement points
                    ConsumeMovementPoints(GetIntelMovementCost());

                    // Consume supplies for the intel action
                    ConsumeSupplies(CUConstants.INTEL_ACTION_SUPPLY_COST);

                    return true; // Intel action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough intel actions, movement points, or supplies to perform an intel action.");
                    return false; // Not enough resources to perform intel action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformIntelAction", e);
                return false;
            }
        }

       /// <summary>
       /// Attempts to perform an opportunity action for the unit.
       /// </summary>
       /// <remarks>An opportunity action can only be performed if the unit has sufficient opportunity
       /// actions, adequate supply levels, and is not designated as a base. If the action is successfully performed,
       /// the unit's opportunity actions are decremented, and the required supplies are consumed.</remarks>
       /// <returns><see langword="true"/> if the opportunity action was successfully performed; otherwise, <see
       /// langword="false"/>.</returns>
       public bool PerformOpportunityAction()
       {
            try
            {
                // Make sure the unit has enough opportunity actions and is not a base.
                if (OpportunityActions.Current >= 1 &&
                    DaysSupply.Current >= CUConstants.OPPORTUNITY_ACTION_SUPPLY_COST + CUConstants.OPPORTUNITY_ACTION_SUPPLY_THRESHOLD && 
                    !IsBase)
                {
                    // Decrement opportunity actions
                    OpportunityActions.DecrementCurrent();

                    // Consume supplies for the opportunity action
                    ConsumeSupplies(CUConstants.OPPORTUNITY_ACTION_SUPPLY_COST);

                    return true; // Opportunity action performed successfully
                }
                else
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have enough opportunity actions to perform an opportunity action.");
                    return false; // Not enough resources to perform opportunity action
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformOpportunityAction", e);
                return false;
            }
       }

        /// <summary>
        /// Returns a dictionary mapping each action type to the number of **truly** available tokens
        /// after validating both action counters and movement‑point prerequisites.
        /// </summary>
        public Dictionary<ActionTypes, float> GetAvailableActions()
        {
            // Move – must have a token and at least 1 movement point remaining.
            float moveAvailable = (MoveActions.Current >= 1 && MovementPoints.Current > 0f)
                ? MoveActions.Current : 0f;

            // Combat – existing validation already checks movement cost.
            float combatAvailable = CombatActions.Current >= 1 ? CombatActions.Current : 0f;

            // Opportunity – purely reactive, no validation.
            float opportunityAvailable = OpportunityActions.Current;

            // Intel – existing validation already handles base / movement logic.
            float intelAvailable = IntelActions.Current >= 1 ? IntelActions.Current : 0f;

            // Deployment – not available for bases, needs movement points.
            float deploymentAvailable = 0f;
            if (IsBase)
            {
                deploymentAvailable = 0f;
            }
            else
            {
                if (MovementPoints.Current >= GetDeployMovementCost() && DeploymentActions.Current >= 1)
                    deploymentAvailable = DeploymentActions.Current;
            }

            return new Dictionary<ActionTypes, float>
            {
                [ActionTypes.MoveAction] = moveAvailable,
                [ActionTypes.CombatAction] = combatAvailable,
                [ActionTypes.DeployAction] = deploymentAvailable,
                [ActionTypes.OpportunityAction] = opportunityAvailable,
                [ActionTypes.IntelAction] = intelAvailable
            };
        }

        /// <summary>
        /// Determines the number of deployment actions available for the unit.
        /// </summary>
        /// <remarks>This method checks whether the unit has sufficient movement points to perform a
        /// deployment. If the movement points are greater than or equal to the deployment movement cost, the current
        /// deployment actions are returned; otherwise, the method returns 0.</remarks>
        /// <returns>The number of deployment actions available. Returns <see langword="0"/> if the unit does not have enough
        /// movement points to deploy.</returns>
        public float GetDeployActions()
        {
            // If the unit is a base, it cannot deploy.
            if (!CanUnitTypeChangeStates())
                return 0;

            // Check if the unit can deploy based on movement points
            if (MovementPoints.Current >= GetDeployMovementCost())
                return DeploymentActions.Current;
            else return 0f;
        }

        /// <summary>
        /// Determines the number of combat actions available for the unit based on its current movement points.
        /// </summary>
        /// <returns>The number of combat actions</returns>
        public float GetCombatActions()
        {
            // If the unit is a base, it cannot perform combat actions.
            if (IsBase)
                return 0;

            // Check if the unit can perform combat actions based on movement points
            if (MovementPoints.Current >= GetCombatMovementCost())
                return CombatActions.Current;
            else return 0;
        }

        /// <summary>
        /// Determines the number of move actions available for the unit.
        /// </summary>
        /// <remarks>A unit's ability to perform move actions depends on its current movement points. If
        /// the unit is a base,  it cannot move under any circumstances.</remarks>
        /// <returns>The number of move actions the unit can perform. Returns <see langword="0"/> if the unit is a base  or if
        /// the unit has no remaining movement points.</returns>
        public float GetMoveActions()
        {
            // If the unit is a base, it cannot move.
            if (IsBase)
                return 0;

            // Check if the unit can perform move actions based on movement points
            if (MovementPoints.Current > 0)
                return MoveActions.Current;
            else
                return 0;
        }

        /// <summary>
        /// Retrieves the current number of opportunity actions available for the unit.
        /// </summary>
        /// <remarks>Opportunity actions represent actions that the unit can perform in specific
        /// situations. If the unit is a base, it cannot perform opportunity actions, and the method returns
        /// 0.</remarks>
        /// <returns>The current number of opportunity actions available for the unit. Returns 0 if the unit is a base.</returns>
        public float GetOpportunityActions()
        {
            // If the unit is a base, it cannot perform opportunity actions.
            if (IsBase)
                return 0;

            return OpportunityActions.Current;
        }

        /// <summary>
        /// Retrieves the number of available intelligence actions for the unit.
        /// </summary>
        /// <remarks>The number of intelligence actions returned depends on whether the unit has
        /// sufficient movement points  to perform an intelligence action. If the unit's current movement points are
        /// less than the required  movement cost, the method returns 0.</remarks>
        /// <returns>The current number of intelligence actions available if the unit has sufficient movement points; otherwise,
        /// 0.</returns>
        public float GetIntelActions()
        {
            // Check if the unit has enough movement points to perform an intel action.
            if (MovementPoints.Current >= GetIntelMovementCost())
                return IntelActions.Current;
            else
                return 0; // Not enough movement points for intel action
        }

        /// <summary>
        /// Consumes movement points if available.
        /// </summary>
        /// <param name="points">Number of movement points to consume</param>
        /// <returns>True if movement points were consumed, false if insufficient</returns>
        private bool ConsumeMovementPoints(float points)
        {
            try
            {
                if (points <= 0f)
                {
                    return true; // No points to consume
                }

                if (MovementPoints.Current >= points)
                {
                    MovementPoints.SetCurrent(MovementPoints.Current - points);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ConsumeMovementPoints", e);
                return false;
            }
        }

        /// <summary>
        /// Calculates the movement-point (MP) cost for this unit to perform a <b>Deploy</b> action.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="MovementPoints.Max"/> is <c>0</c> the method returns <c>0</c>.  
        /// This indicates the unit is <em>immobile</em>, <u>not</u> that the action is free.  
        /// Callers must still verify that <see cref="MovementPoints.Remaining"/> is at least
        /// the value returned before enabling the action.
        /// </para>
        /// </remarks>
        /// <returns>The whole-number MP cost for deploying.  Always non-negative.</returns>
        private float GetDeployMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.DEPLOYMENT_ACTION_MOVEMENT_COST);
        }

        /// <summary>
        /// Calculates the MP cost required to execute a <b>Combat</b> (attack) action.
        /// </summary>
        /// <remarks>
        /// Returns <c>0</c> when the unit’s maximum MPs are zero, signalling that the unit
        /// cannot pay any MP cost at all.  Client code should still check
        /// <see cref="MovementPoints.Remaining"/> ≥ returned value before allowing the attack.
        /// </remarks>
        /// <returns>The MP cost as an integer.</returns>
        private float GetCombatMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.COMBAT_ACTION_MOVEMENT_COST);
        }

        /// <summary>
        /// Calculates the MP cost for an <b>Intel</b> (recon/spotting) action.
        /// </summary>
        /// <remarks>
        /// A result of <c>0</c> means the unit is immobile (<see cref="MovementPoints.Max"/> == 0);
        /// the action is <em>not</em> considered gratis.  
        /// UI or AI callers must independently confirm there are enough MPs remaining.
        /// </remarks>
        /// <returns>Integer MP cost for the intel action.</returns>
        private float GetIntelMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.INTEL_ACTION_MOVEMENT_COST);
        }

        #endregion // CombatUnit Actions


        #region Position and Movement

        /// <summary>
        /// Sets the unit's position on the map.
        /// </summary>
        /// <param name="newPos">The new position coordinates</param>
        public void SetPosition(Coordinate2D newPos)
        {
            try
            {
                MapPos = newPos; // Direct assignment instead of reflection
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetPosition", e);
                throw;
            }
        }

        /// <summary>
        /// Checks if the unit can move to the specified position.
        /// This is a basic validation - full movement rules will be implemented later.
        /// </summary>
        /// <param name="targetPos">The target position to validate</param>
        /// <returns>True if movement appears valid</returns>
        public bool CanMoveTo(Coordinate2D targetPos)
        {
            try
            {
                throw new NotImplementedException("Movement validation logic not implemented yet.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanMoveTo", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the distance between this unit and a target position in Unity units.
        /// </summary>
        /// <param name="targetPos">The target position</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(Coordinate2D targetPos)
        {
            try
            {
                throw new NotImplementedException("Distance calculation logic not implemented yet.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetDistanceTo", e);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the distance between this unit and another unit.
        /// </summary>
        /// <param name="otherUnit">The other unit</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(CombatUnit otherUnit)
        {
            try
            {
                throw new NotImplementedException("Distance calculation logic not implemented yet.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetDistanceTo", e);
                return 0f;
            }
        }

        #endregion // Position and Movement


        #region Debugging

        /// <summary>
        /// Direct change of combat state for debugging purposes.
        /// </summary>
        /// <param name="newPosition"></param>
        public void DebugSetCombatState(DeploymentPosition newPosition)
        {
            _deploymentPosition = newPosition;
        }

        /// <summary>
        /// Calculates the movement cost for a combat action.
        /// </summary>
        /// <returns>The movement cost as a floating-point number, representing the ceiling of the maximum movement points
        /// multiplied by the combat action movement cost constant.</returns>
        public float DebugGetCombatMovementCost()
        {
            return Mathf.CeilToInt(MovementPoints.Max * CUConstants.COMBAT_ACTION_MOVEMENT_COST);
        }

        #endregion // Debugging


        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this CombatUnit with a new unique ID.
        /// Used for spawning fresh units from templates - never used on live state objects.
        /// Leaders are not cloned and must be assigned separately.
        /// </summary>
        /// <returns>A new CombatUnit instance with identical properties but unique ID</returns>
        public object Clone()
        {
            return null;
        }

        #endregion // ICloneable Implementation


        #region ISerializable Implementation

        /// <summary>
        /// Deserialization constructor for loading CombatUnit from saved data.
        /// </summary>
        /// <param name="info">Serialization info containing saved data</param>
        /// <param name="context">Streaming context for deserialization</param>
        protected CombatUnit(SerializationInfo info, StreamingContext context)
        {
            
        }

        /// <summary>
        /// Serializes CombatUnit data for saving to file.
        /// </summary>
        /// <param name="info">Serialization info to store data</param>
        /// <param name="context">Streaming context for serialization</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
        }

        /// <summary>
        /// Checks if there are unresolved references that need to be resolved.
        /// </summary>
        /// <returns>True if any resolution methods need to be called</returns>
        public bool HasUnresolvedReferences()
        {
            // Only facilities attach other units
            return IsBase && _attachedUnitIDs.Count > 0;
        }

        #endregion // ISerializable Implementation


        #region IResolvableReferences

        /// <summary>
        /// Gets the list of unresolved reference IDs that need to be resolved.
        /// </summary>
        /// <returns>Collection of object IDs that this object references</returns>
        public IReadOnlyList<string> GetUnresolvedReferenceIDs()
        {
            var unresolvedIDs = new List<string>();

            // Include facility's unresolved references
            if (IsBase)
            {
                unresolvedIDs.AddRange(_attachedUnitIDs.Select(unitID => $"AirUnit:{unitID}"));
            }

            return unresolvedIDs.AsReadOnly();
        }

        /// <summary>
        /// Resolves object references using the provided data manager.
        /// Called after all objects have been deserialized.
        /// </summary>
        /// <param name="manager">Game data manager containing all loaded objects</param>
        public void ResolveReferences(GameDataManager manager)
        {
            try
            {
                // Leader resolution is no longer needed - handled via GameDataManager lookup

                // Resolve facility references if this is a base unit
                if (IsBase && FacilityType == FacilityType.Airbase)
                {
                    _airUnitsAttached.Clear();

                    foreach (string unitID in _attachedUnitIDs)
                    {
                        var unit = manager.GetCombatUnit(unitID);
                        if (unit != null)
                        {
                            if (unit.UnitType == UnitType.AirUnit)
                            {
                                _airUnitsAttached.Add(unit);
                            }
                            else
                            {
                                AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                    new InvalidOperationException($"Unit {unitID} is not an air unit (Type: {unit.UnitType})"),
                                    ExceptionSeverity.Minor);
                            }
                        }
                        else
                        {
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new KeyNotFoundException($"Air unit {unitID} not found in game data manager"),
                                ExceptionSeverity.Minor);
                        }
                    }

                    _attachedUnitIDs.Clear(); // Clean up temporary storage
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResolveReferences", e);
                throw;
            }
        }

        #endregion // IResolvableReferences


        #region Validation Methods

        /// <summary>
        /// Validates internal consistency of the unit, including facility relationships.
        /// </summary>
        /// <returns>List of validation errors found</returns>
        public List<string> ValidateInternalConsistency()
        {
            var errors = new List<string>();

           
            return errors;
        }

        #endregion // Validation Methods
    }
}