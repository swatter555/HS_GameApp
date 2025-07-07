using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

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


        #region Fields

        private bool _mobileBonusApplied = false; // Persisted runtime flag, true when MOBILE_MOVEMENT_BONUS active.
        private const string SERIAL_KEY_MOBILE_BONUS = "mobBonus"; // Serialization identifier for _mobileBonusApplied
        
        #endregion // Fields


        #region Properties

        // Identification and metadata
        public string UnitName { get; set; }
        public string UnitID { get; private set; }
        public UnitType UnitType { get; private set; }
        public UnitClassification Classification { get; private set; }
        public UnitRole Role { get; private set; }
        public Side Side { get; private set; }
        public Nationality Nationality { get; private set; }
        public bool IsTransportable { get; private set; }
        public bool IsBase => Classification.IsBaseType();

        // Profile IDs
        public WeaponSystems DeployedProfileID { get; private set; }
        public WeaponSystems MountedProfileID { get; private set; }
        public IntelProfileTypes IntelProfileType { get; internal set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelActions { get; private set; }

        // State data using StatsMaxCurrent where appropriate
        public int ExperiencePoints { get; internal set; }
        public ExperienceLevel ExperienceLevel { get; internal set; }
        public EfficiencyLevel EfficiencyLevel { get; internal set; }
        public bool IsMounted { get; internal set; }
        public CombatState CombatState { get; internal set; }
        public StatsMaxCurrent HitPoints { get; private set; }
        public StatsMaxCurrent DaysSupply { get; private set; }
        public StatsMaxCurrent MovementPoints { get; private set; }
        public Coordinate2D MapPos { get; internal set; }
        public SpottedLevel SpottedLevel { get; private set; }

        // The ID of unit's Leader, if assigned.
        public string LeaderID { get; internal set; }
        public bool IsLeaderAssigned => !string.IsNullOrEmpty(LeaderID);
        public Leader UnitLeader
        {
            get
            {
                try
                {
                    if (!IsLeaderAssigned)
                        throw new InvalidOperationException("No leader assigned to this unit.");

                    return GameDataManager.Instance.GetLeader(LeaderID);
                }
                catch (Exception e)
                {
                    AppService.HandleException(CLASS_NAME, "UnitLeader.get", e, ExceptionSeverity.Minor);
                    return null;
                }
            }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Creates a new CombatUnit with the specified core properties.
        /// </summary>
        /// <param name="unitName">Display name of the unit</param>
        /// <param name="unitType">Type of unit (land, air, naval)</param>
        /// <param name="classification">Unit classification (tank, infantry, etc.)</param>
        /// <param name="role">Primary role of the unit</param>
        /// <param name="side">Which side controls this unit</param>
        /// <param name="nationality">National affiliation</param>
        /// <param name="deployedProfileID">Deployed weapon system profile ID</param>
        /// <param name="mountedProfileID">Mounted weapon system profile ID (can be DEFAULT for none)</param>
        /// <param name="intelProfileType">Intel profile type for intelligence reports</param>
        /// <param name="isTransportable">Whether this unit can be transported</param>
        /// <param name="isBase">Whether this unit is a land-based facility</param>
        public CombatUnit(string unitName,
            UnitType unitType,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            WeaponSystems deployedProfileID,
            WeaponSystems mountedProfileID,
            IntelProfileTypes intelProfileType,
            bool isTransportable,
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
                if (mountedProfileID != WeaponSystems.DEFAULT)
                {
                    if (WeaponSystemsDatabase.GetWeaponSystemProfile(mountedProfileID) == null)
                        throw new ArgumentException($"Mounted profile ID {mountedProfileID} not found in database", nameof(mountedProfileID));
                }

                // Validate intel profile type
                if (!Enum.IsDefined(typeof(IntelProfileTypes), intelProfileType))
                    throw new ArgumentException("Invalid intel profile type", nameof(intelProfileType));

                // Set identification and metadata

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
                IsTransportable = isTransportable;

                // Set profile IDs
                DeployedProfileID = deployedProfileID;
                MountedProfileID = mountedProfileID;
                IntelProfileType = intelProfileType;

                // Initialise facility data when appropriate
                if (IsBase)
                {
                    InitializeFacility(category, size);
                }

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize state with default values
                ExperiencePoints = 0;
                ExperienceLevel = ExperienceLevel.Raw;
                EfficiencyLevel = EfficiencyLevel.FullyOperational;
                IsMounted = false;
                CombatState = CombatState.Deployed;
                SpottedLevel = SpottedLevel.Level1;
                LeaderID = null;

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

        #endregion


        #region Core

        /// <summary>
        /// Retrieves the weapon‑system profile the unit employs in its <em>deployed</em> (dismounted) state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="DeployedProfileID"/> equals <see cref="WeaponSystems.DEFAULT"/>, the unit has no
        /// organic deployed weapon system and the method returns <c>null</c> without logging an error.
        /// </para>
        /// <para>
        /// When the ID is not <c>DEFAULT</c> but the profile cannot be located in the database, the situation
        /// indicates stale or corrupt data. The problem is logged via
        /// <see cref="AppService.HandleException(string,string,System.Exception)"/>, and the method returns
        /// <c>null</c> so that callers can handle the absence gracefully.
        /// </para>
        /// </remarks>
        /// <returns>The corresponding <see cref="WeaponSystemProfile"/> or <c>null</c> when none is available.</returns>
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
        /// Retrieves the weapon‑system profile of the transport (mount) currently carrying the unit, if any.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A return value of <c>null</c> can mean one of two things:
        /// </para>
        /// <list type="bullet">
        ///   <item>The unit is not mounted &mdash; <see cref="MountedProfileID"/> equals <see cref="WeaponSystems.DEFAULT"/>.</item>
        ///   <item>The ID points to a profile that no longer exists in the database (logged as an error).</item>
        /// </list>
        /// <para>
        /// By returning <c>null</c> in both cases the method provides a simple “no usable profile” contract, while
        /// still ensuring data‑integrity problems are captured in the log.
        /// </para>
        /// </remarks>
        /// <returns>The <see cref="WeaponSystemProfile"/> representing the transport, or <c>null</c> when unmounted/invalid.</returns>
        public WeaponSystemProfile GetMountedProfile()
        {
            try
            {
                // DEFAULT means the unit is foot‑mobile (no transport profile).
                if (MountedProfileID == WeaponSystems.DEFAULT)
                    return null;

                var profile = WeaponSystemsDatabase.GetWeaponSystemProfile(MountedProfileID);
                if (profile == null)
                    throw new InvalidOperationException($"Mounted profile ID '{MountedProfileID}' not found in weapon‑systems database.");

                return profile;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(GetMountedProfile), ex);
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
                    CombatState,
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
            // Return the active profile based on mounted state
            return IsMounted ? GetMountedProfile(): GetDeployedProfile();
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

                // Create a new temporary profile for combat calculations
                var tempProfile = new WeaponSystemProfile(
                    activeProfile.Name + "_Combat",
                    activeProfile.Nationality,
                    WeaponSystems.COMBAT);

                // Compute all modifiers that can affect a combat rating
                float finalModifier = GetFinalCombatRatingModifier();

                // Copy and apply modifiers to combat ratings
                tempProfile.LandHard.SetAttack(Mathf.CeilToInt(activeProfile.LandHard.Attack * finalModifier));
                tempProfile.LandHard.SetDefense(Mathf.CeilToInt(activeProfile.LandHard.Defense * finalModifier));

                tempProfile.LandSoft.SetAttack(Mathf.CeilToInt(activeProfile.LandSoft.Attack * finalModifier));
                tempProfile.LandSoft.SetDefense(Mathf.CeilToInt(activeProfile.LandSoft.Defense * finalModifier));

                tempProfile.LandAir.SetAttack(Mathf.CeilToInt(activeProfile.LandAir.Attack * finalModifier));
                tempProfile.LandAir.SetDefense(Mathf.CeilToInt(activeProfile.LandAir.Defense * finalModifier));

                tempProfile.Air.SetAttack(Mathf.CeilToInt(activeProfile.Air.Attack * finalModifier));
                tempProfile.Air.SetDefense(Mathf.CeilToInt(activeProfile.Air.Defense * finalModifier));

                tempProfile.AirGround.SetAttack(Mathf.CeilToInt(activeProfile.AirGround.Attack * finalModifier));
                tempProfile.AirGround.SetDefense(Mathf.CeilToInt(activeProfile.AirGround.Defense * finalModifier));

                // Copy other relevant properties that might be needed for combat calculations
                tempProfile.SetPrimaryRange(activeProfile.PrimaryRange);
                tempProfile.SetIndirectRange(activeProfile.IndirectRange);
                tempProfile.SetSpottingRange(activeProfile.SpottingRange);
                tempProfile.SetMovementModifier(activeProfile.MovementModifier);

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

        #endregion // Core


        #region Experience System

        /// <summary>
        /// Adds experience points to the unit and returns true if successful.
        /// </summary>
        /// <param name="points">Experience points to add</param>
        /// <returns>Returns true if successful</returns>
        public bool AddExperience(int points)
        {
            try
            {
                // Units cannot gain negative experience.
                if (points <= 0)
                    return false;

                // Validate points do not exceed maximum gain per action.
                if (points > CUConstants.MAX_EXP_GAIN_PER_ACTION)
                {
                    points = CUConstants.MAX_EXP_GAIN_PER_ACTION;
                }
                    
                // Add experience points to total.
                ExperiencePoints += points;

                // Cap at Elite level.
                if (ExperiencePoints > (int)ExperiencePointLevels.Elite)
                {
                    ExperiencePoints = (int)ExperiencePointLevels.Elite;
                }

                // Store the previous level for comparison.
                var previousLevel = ExperienceLevel;

                // Get the new experience level based on updated points.
                var newLevel = CalculateExperienceLevel(ExperiencePoints);

                // If the level has changed, update and notify.
                if (newLevel != previousLevel)
                {
                    ExperienceLevel = newLevel;
                    OnExperienceLevelChanged(previousLevel, newLevel);
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddExperience", e);
                return false;
            }
        }

        /// <summary>
        /// Safely sets the unit’s cumulative experience points (XP) and keeps
        /// <see cref="ExperienceLevel"/> tightly synchronized.
        /// </summary>
        /// <param name="points">The new total XP to apply.  Values outside
        /// <c>0 … CUConstants.EXPERIENCE_MAX</c> are automatically clamped.</param>
        /// <returns>The clamped XP value that was actually stored.</returns>
        /// <remarks>
        /// <para>
        /// Both <see cref="ExperiencePoints"/> and the derived
        /// <see cref="ExperienceLevel"/> are updated from the same clamped value,
        /// eliminating drift.  The method never throws for invalid input; any
        /// unexpected runtime errors are logged and the prior XP value is
        /// preserved.
        /// </para>
        /// </remarks>
        public int SetExperience(int points)
        {
            try
            {
                // 1) Constrain to legal range.
                int clamped = Math.Clamp(points, 0, (int)ExperiencePointLevels.Elite);

                // 2) Skip work if nothing changed – avoids redundant UI refresh.
                if (clamped == ExperiencePoints)
                    return clamped;

                // 3) Persist XP and recompute level from the same source value.
                ExperiencePoints = clamped;
                ExperienceLevel = CalculateExperienceLevel(clamped);

                return clamped;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(SetExperience), ex);
                return ExperiencePoints; // return state that survived the error
            }
        }

        /// <summary>
        /// Gets the experience points required for the next level.
        /// Returns 0 if already at maximum level (Elite).
        /// </summary>
        /// <returns>Points needed for next level, or 0 if at max level</returns>
        public int GetPointsToNextLevel()
        {
            return ExperienceLevel switch
            {
                ExperienceLevel.Raw => (int)ExperiencePointLevels.Green - ExperiencePoints,
                ExperienceLevel.Green => (int)ExperiencePointLevels.Trained - ExperiencePoints,
                ExperienceLevel.Trained => (int)ExperiencePointLevels.Experienced - ExperiencePoints,
                ExperienceLevel.Experienced => (int)ExperiencePointLevels.Veteran - ExperiencePoints,
                ExperienceLevel.Veteran => (int)ExperiencePointLevels.Elite - ExperiencePoints,
                ExperienceLevel.Elite => 0,// Already at max level
                _ => 0,
            };
        }

        /// <summary>
        /// Gets the experience progress as a percentage towards the next level (0.0 to 1.0).
        /// Returns 1.0 if at maximum level.
        /// </summary>
        /// <returns>Progress percentage towards next level</returns>
        public float GetExperienceProgress()
        {
            if (ExperienceLevel == ExperienceLevel.Elite)
                return 1.0f;

            int currentLevelMin = GetMinPointsForLevel(ExperienceLevel);
            int nextLevelMin = GetMinPointsForLevel(GetNextLevel(ExperienceLevel));

            if (nextLevelMin == currentLevelMin)
                return 1.0f;

            float progress = (float)(ExperiencePoints - currentLevelMin) / (nextLevelMin - currentLevelMin);
            return Mathf.Clamp01(progress);
        }

        #endregion // Experience System


        #region Leader System

        /// <summary>
        /// Assigns a leader to this unit by their ID, removing any existing leader.
        /// </summary>
        /// <param name="leaderID">ID of the new leader</param>
        /// <returns>success/failure</returns>
        public bool AssignLeader(string leaderID)
        {
            try
            {
                // Validate the incoming LeaderID.
                Leader newLeader = GameDataManager.Instance.GetLeader(leaderID);
                if (newLeader == null)
                    throw new ArgumentException($"Leader with ID {leaderID} does not exist in the game data.", nameof(leaderID));

                // Check if the new leader is already assigned to another unit.
                if (newLeader.IsAssigned)
                    throw new InvalidOperationException($"Leader {newLeader.Name} is already assigned to another unit.");

                // If there is already a leader assigned, we must remove them first.
                if (IsLeaderAssigned)
                {
                    // Make sure current leader is valid before proceeding.
                    if (UnitLeader == null)
                        throw new InvalidOperationException("Current leader is null, cannot unassign.");

                    // Capture UI message about the leader being unassigned.
                    AppService.CaptureUiMessage($"{UnitLeader.FormattedRank} {UnitLeader.Name} has been unassigned from {UnitName}.");

                    // Reach in and let the Leader know it isn't assigned to this unit anymore.
                    UnitLeader.UnassignFromUnit();
                }

                // Assign the new leader ID.
                LeaderID = leaderID;

                // Now reach into new leader and assign him from there.
                newLeader.AssignToUnit(UnitID);

                // Capture UI message about the new leader being assigned.
                AppService.CaptureUiMessage($"{newLeader.FormattedRank} {newLeader.Name} has been assigned to command {UnitName}.");

                return true;
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "AssignLeader", e);
                AppService.CaptureUiMessage("Leader assignment failed due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Removes the current leader from this unit, if one is assigned.
        /// </summary>
        /// <returns>Success/Failure</returns>
        public bool RemoveLeader()
        {
            try
            {
                // Check if there is actually a leader to remove.
                if (!IsLeaderAssigned)
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have a commanding officer to remove.");
                    return false;
                }

                // Capture UI message about the leader being removed.
                AppService.CaptureUiMessage($"{UnitLeader.FormattedRank} {UnitLeader.Name} has been relieved of command of {UnitName} and is now available for reassignment.");

                // Reach in and let the Leader know it isn't assigned to this unit anymore.
                UnitLeader.UnassignFromUnit();

                // Clear our reference to the leader.
                LeaderID = null;

                return true;
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "RemoveLeader", e);
                AppService.CaptureUiMessage("Leader removal failed due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Gets all bonuses provided by the commanding officer's skills.
        /// Returns an empty dictionary if no leader is assigned.
        /// </summary>
        /// <returns>Dictionary mapping skill bonus types to their values</returns>
        public Dictionary<SkillBonusType, float> GetLeaderBonuses()
        {
            var bonuses = new Dictionary<SkillBonusType, float>();

            try
            {
                // Check if there is a leader assigned
                if (!IsLeaderAssigned)
                    return bonuses;

                // Iterate through all skill bonus types and get non-zero values
                foreach (SkillBonusType bonusType in (SkillBonusType[])Enum.GetValues(typeof(SkillBonusType)))
                {
                    if (bonusType == SkillBonusType.None) continue;

                    float bonusValue = UnitLeader.GetBonusValue(bonusType);
                    if (bonusValue != 0f)
                    {
                        bonuses[bonusType] = bonusValue;
                    }
                }

                return bonuses;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetLeaderBonuses", e);
                return bonuses; // Return empty dictionary on error
            }
        }

        /// <summary>
        /// Checks if the unit has a specific leader capability/bonus.
        /// </summary>
        /// <param name="bonusType">The bonus type to check for</param>
        /// <returns>True if the leader provides this capability</returns>
        public bool HasLeaderCapability(SkillBonusType bonusType) =>
            UnitLeader != null && UnitLeader.HasCapability(bonusType);

        /// <summary>
        /// Gets a specific leader bonus value.
        /// Returns 0 if no leader assigned or bonus not present.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve</param>
        /// <returns>The bonus value, or 0 if not present</returns>
        public float GetLeaderBonus(SkillBonusType bonusType) =>
            UnitLeader != null && bonusType != SkillBonusType.None
            ? UnitLeader.GetBonusValue(bonusType)
            : 0f;

        /// <summary>
        /// Gets the leader's name for display purposes.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Leader name or empty string</returns>
        public string GetLeaderName() => UnitLeader?.Name ?? string.Empty;

        /// <summary>
        /// Gets the leader's command grade for display and bonus calculations.
        /// Returns JuniorGrade if no leader assigned.
        /// </summary>
        /// <returns>Leader's command grade</returns>
        public CommandGrade GetLeaderGrade() => UnitLeader?.CommandGrade ?? CommandGrade.JuniorGrade;

        /// <summary>
        /// Gets the leader's reputation points for display purposes.
        /// Returns 0 if no leader assigned.
        /// </summary>
        /// <returns>Leader's reputation points</returns>
        public int GetLeaderReputation() => UnitLeader?.ReputationPoints ?? 0;

        /// <summary>
        /// Gets the leader's formatted rank based on nationality.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Formatted rank string</returns>
        public string GetLeaderRank() => UnitLeader?.FormattedRank ?? "";

        /// <summary>
        /// Gets the leader's combat command ability modifier.
        /// Returns Average if no leader assigned.
        /// </summary>
        /// <returns>Leader's combat command ability</returns>
        public CommandAbility GetLeaderCommandAbility() =>
            UnitLeader?.CombatCommand ?? CommandAbility.Average;

        /// <summary>
        /// Checks if the leader has unlocked a specific skill.
        /// Returns false if no leader assigned.
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill is unlocked</returns>
        public bool HasLeaderSkill(Enum skill) =>
            UnitLeader != null && UnitLeader.IsSkillUnlocked(skill);

        /// <summary>
        /// Awards reputation to the leader for unit actions.
        /// Does nothing if no leader assigned.
        /// </summary>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="contextMultiplier">Context-based multiplier (default 1.0)</param>
        public void AwardLeaderReputation(CUConstants.ReputationAction actionType, float contextMultiplier = 1.0f)
        {
            try
            {
                if (UnitLeader == null)
                {
                    return;
                }

                UnitLeader.AwardReputationForAction(actionType, contextMultiplier);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        /// <summary>
        /// Awards reputation points directly to the leader.
        /// Does nothing if no leader assigned.
        /// </summary>
        /// <param name="amount">Amount of reputation to award</param>
        public void AwardLeaderReputation(int amount)
        {
            try
            {
                if (UnitLeader == null || amount <= 0)
                {
                    return;
                }

                UnitLeader.AwardReputation(amount);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        #endregion // Leader System


        #region CombatUnit Actions

        /// <summary>
        /// Attempts to deploy the combat unit to a higher deployment level.
        /// </summary>
        /// <remarks>This method checks whether the combat unit has sufficient deployment actions,
        /// movement points, and supplies to perform the operation. If the deployment is successful, the required
        /// resources are consumed. If the deployment fails, an appropriate message is captured for the user
        /// interface.</remarks>
        /// <returns><see langword="true"/> if the deployment to a higher level is successful; otherwise, <see
        /// langword="false"/>.</returns>
        public bool DeployUpOneLevel()
        {
            try
            {
                // Check actions and movement points before deploying.
                if (GetDeployActions() >= 1 && MovementPoints.Current >= GetDeployMovementCost())
                {
                    // Atempt to deploy up one level.
                    bool result = TryDeployUpOneState();

                    // If we have made it this far, we can consume supplies and actions.
                    if (result)
                    {
                        ConsumeSupplies(CUConstants.COMBAT_STATE_SUPPLY_TRANSITION_COST);
                        DeploymentActions.DecrementCurrent();
                        return true;
                    }
                    else
                    {
                        // Deployment failed, notify the user.
                        AppService.CaptureUiMessage($"{UnitName} cannot deploy up one level due to insufficient conditions.");
                        return false; // Deployment failed
                    }
                }
                // Not enough actions or movement points to deploy
                AppService.CaptureUiMessage($"{UnitName} does not have enough deployment actions or movement points to deploy up one level.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeployUpOneLevel", e);
                return false;
            }
        }

        /// <summary>
        /// Attempts to deploy the combat unit to a lower deployment level.
        /// </summary>
        /// <remarks>This method checks whether the combat unit has sufficient deployment actions,
        /// movement points, and supplies to perform the operation. If the deployment is successful, the required
        /// resources are consumed. If the deployment fails, an appropriate message is captured for the user
        /// interface.</remarks>
        /// <returns><see langword="true"/> if the deployment to a lower level is successful; otherwise, <see
        /// langword="false"/>.</returns>
        public bool DeployDownOneLevel()
        {
            try
            {
                // Check actions and movement points before deploying.
                if (GetDeployActions() >= 1 && MovementPoints.Current >= GetDeployMovementCost())
                {
                    // Atempt to deploy down one level.
                    bool result = TryEntrenchDownOneState();

                    // If we have made it this far, we can consume supplies and actions.
                    if (result)
                    {
                        ConsumeSupplies(CUConstants.COMBAT_STATE_SUPPLY_TRANSITION_COST);
                        DeploymentActions.DecrementCurrent();
                        return true;
                    }
                    else
                    {
                        // Deployment failed, notify the user.
                        AppService.CaptureUiMessage($"{UnitName} cannot deploy down one level due to insufficient conditions.");
                        return false; // Deployment failed
                    }
                }
                // Not enough actions or movement points to deploy
                AppService.CaptureUiMessage($"{UnitName} does not have enough deployment actions or movement points to deploy down one level.");
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeployDownOneLevel", e);
                return false;
            }
        }

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
                    ConsumeSupplies((movtCost * CUConstants.MOVE_ACTION_SUPPLY_COST) + CUConstants.MOVE_ACTION_SUPPLY_THRESHOLD);

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
                    ConsumeSupplies(CUConstants.OPPORTUNITY_ACTION_SUPPLY_COST + CUConstants.OPPORTUNITY_ACTION_SUPPLY_THRESHOLD);

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


        #region Core Helpers

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
                case UnitClassification.SPECM:
                case UnitClassification.SPECH:
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
            var maxMovement = Classification switch
            {
                UnitClassification.TANK or
                UnitClassification.MECH or
                UnitClassification.RECON or
                UnitClassification.MAB or
                UnitClassification.MAM or
                UnitClassification.MMAR or
                UnitClassification.SPECM or
                UnitClassification.SPA or
                UnitClassification.SPAAA or
                UnitClassification.SPSAM => CUConstants.MECH_MOV,

                UnitClassification.AT or
                UnitClassification.MOT or
                UnitClassification.ROC => CUConstants.MOT_MOV,

                UnitClassification.INF or
                UnitClassification.AB or
                UnitClassification.AM or
                UnitClassification.MAR or
                UnitClassification.ART or
                UnitClassification.SAM or
                UnitClassification.AAA or
                UnitClassification.SPECF or
                UnitClassification.ENG => CUConstants.FOOT_MOV,

                UnitClassification.ASF or
                UnitClassification.MRF or
                UnitClassification.ATT or
                UnitClassification.BMB or
                UnitClassification.RECONA => CUConstants.FIXEDWING_MOV,

                UnitClassification.HELO or
                UnitClassification.SPECH => CUConstants.HELO_MOV,

                UnitClassification.HQ or
                UnitClassification.DEPOT or
                UnitClassification.AIRB => 0,// Bases don't move

                _ => CUConstants.FOOT_MOV,// Default to foot movement
            };
            MovementPoints = new StatsMaxCurrent(maxMovement);
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
            return CombatState switch
            {
                CombatState.Mobile => CUConstants.COMBAT_MOD_MOBILE,
                CombatState.Deployed => CUConstants.COMBAT_MOD_DEPLOYED,
                CombatState.HastyDefense => CUConstants.COMBAT_MOD_HASTY_DEFENSE,
                CombatState.Entrenched => CUConstants.COMBAT_MOD_ENTRENCHED,
                CombatState.Fortified => CUConstants.COMBAT_MOD_FORTIFIED,
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

        #endregion // Core Helpers


        #region CombatUnit Actions Helpers

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

        /// <summary>
        /// Attempts to move the unit <em>one step toward <c>Mobile</c></em> on the posture ladder.
        /// </summary>
        /// <remarks>
        /// <para>
        /// **“Deploy <u>up</u> one state” means *becoming more mobile*.**  Internally that is a <strong>decrement</strong>
        /// of the underlying enum index:<br/>
        /// <c>Fortified (4) → Entrenched (3) → HastyDefense (2) → Deployed (1) → Mobile (0)</c>.
        /// </para>
        /// <para>
        /// If the unit is already at the most‑mobile posture (<c>Mobile</c>), no transition occurs and the method returns
        /// <c>false</c>.  All legality checks (e.g., suppression, casualties, supply) are delegated to
        /// <see cref="CanChangeToState(CombatState)"/>.
        /// </para>
        /// </remarks>
        /// <returns><c>true</c> if the posture actually changed; otherwise <c>false</c>.</returns>
        private bool TryDeployUpOneState()
        {
            try
            {
                CombatState currentState = CombatState;

                // Check if already at maximum mobility (Mobile state)
                if (currentState == CombatState.Mobile)
                {
                    AppService.CaptureUiMessage($"{UnitName} is already in Mobile state - cannot move to higher mobility.");
                    return false;
                }

                // Calculate target state (one step toward Mobile)
                int targetIndex = (int)currentState - 1;
                CombatState targetState = (CombatState)targetIndex;

                // Comprehensive check for valid state transition.
                if (!CanChangeToState(targetState))
                    return false;

                // Save previous state for comparison.
                var previousState = CombatState;

                // Set the new combat state
                CombatState = targetState;

                // Update the unit's mobility state based on the new combat state
                UpdateMobilityState(targetState, previousState);

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpOneState", e);
                return false;
            }
        }

        /// <summary>
        /// Attempts to move the unit <em>one step toward <c>Fortified</c></em> on the posture ladder.
        /// </summary>
        /// <remarks>
        /// <para>
        /// **“Entrench <u>down</u> one state” means *digging in further* (less mobile, more protection).**  This is a
        /// <strong>increment</strong> of the enum index:<br/>
        /// <c>Mobile (0) → Deployed (1) → HastyDefense (2) → Entrenched (3) → Fortified (4)</c>.
        /// </para>
        /// <para>
        /// Attempting the action while already at <c>Fortified</c> simply returns <c>false</c>.  All other gating rules
        /// are handled by <see cref="CanChangeToState(CombatState)"/>.
        /// </para>
        /// </remarks>
        /// <returns><c>true</c> if the posture deepened; otherwise <c>false</c>.</returns>
        private bool TryEntrenchDownOneState()
        {
            try
            {
                CombatState currentState = CombatState;

                // Check if already at maximum entrenchment (Fortified state)
                if (currentState == CombatState.Fortified)
                {
                    AppService.CaptureUiMessage($"{UnitName} is already in Fortified state - cannot entrench further.");
                    return false;
                }

                // Calculate target state (one step toward fortified)
                int targetIndex = (int)currentState + 1;
                CombatState targetState = (CombatState)targetIndex;

                // Comprehensive check for valid state transition.
                if (!CanChangeToState(targetState))
                    return false;

                // Save previous state for comparison.
                var previousState = CombatState;

                // Set the new combat state
                CombatState = targetState;

                // Update the unit's mobility state based on the new combat state
                UpdateMobilityState(targetState, previousState);

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(TryEntrenchDownOneState), ex);
                return false;
            }
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
        /// Determines whether two <see cref="CombatState"/> values are <em>adjacent rungs</em> on the posture ladder.
        /// </summary>
        /// <remarks>
        /// The method assumes the enum values are contiguous integers in mobility order.  If that ordering ever changes
        /// (e.g., inserting a new state or switching to bit‑flags), this helper must be updated accordingly.
        /// </remarks>
        private bool IsAdjacentStateTransition(CombatState currentState, CombatState targetState)
        {
            int currentIndex = (int)currentState;
            int targetIndex = (int)targetState;

            // Adjacent means difference of exactly 1
            return Math.Abs(currentIndex - targetIndex) == 1;
        }

        /// <summary>
        /// Adjusts <see cref="IsMounted"/> and the <em>mobile‑state movement
        /// bonus</em> when the unit changes combat state.
        /// </summary>
        /// <param name="newState">The state the unit is entering.</param>
        /// <param name="previousState">The state the unit is leaving.</param>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><term>Entering <see cref="CombatState.Mobile"/></term>
        ///         <description>
        ///           • If the unit has a mounted‑profile, it becomes <c>IsMounted = true</c> and <strong>does not</strong>
        ///             receive the foot‑mobile bonus.<br/>
        ///           • Otherwise it is foot‑mobile (<c>IsMounted = false</c>) and gains
        ///             <c>+MOBILE_MOVEMENT_BONUS</c> – applied only once per entry.
        ///         </description></item>
        ///   <item><term>Leaving <c>Mobile</c></term>
        ///         <description>
        ///           • Mounted units simply mark <c>IsMounted = false</c> (no bonus to remove).<br/>
        ///           • Foot‑mobile units have the bonus removed exactly once.
        ///         </description></item>
        /// </list>
        /// Preconditions:
        /// • <see cref="GetDeployedProfile"/> must return a non‑null profile.
        /// • The method never throws for missing mounted profiles – they are
        ///   treated as “not mounted.”
        /// </remarks>
        private void UpdateMobilityState(CombatState newState, CombatState previousState)
        {
            try
            {
                // ----- Preconditions --------------------------------------------------
                if (GetDeployedProfile() == null)
                    throw new InvalidOperationException("Unit lacks a deployed weapon profile – cannot change mobility state.");

                bool enteringMobile = newState == CombatState.Mobile && previousState != CombatState.Mobile;
                bool leavingMobile = newState != CombatState.Mobile && previousState == CombatState.Mobile;

                // Presence of a transport profile determines mount capability.
                bool hasTransport = GetMountedProfile() != null;

                // ---------------------------------------------------------------------
                // ENTERING Mobile
                // ---------------------------------------------------------------------
                if (enteringMobile)
                {
                    if (hasTransport)
                    {
                        // Ride the transport – no foot bonus.
                        IsMounted = true;

                        if (_mobileBonusApplied)
                        {
                            ApplyMovementBonus(-CUConstants.MOBILE_MOVEMENT_BONUS);
                            _mobileBonusApplied = false;
                        }
                    }
                    else // Foot‑mobile
                    {
                        IsMounted = false;

                        if (!_mobileBonusApplied)
                        {
                            ApplyMovementBonus(+CUConstants.MOBILE_MOVEMENT_BONUS);
                            _mobileBonusApplied = true;
                        }
                    }
                }
                // ---------------------------------------------------------------------
                // LEAVING Mobile
                // ---------------------------------------------------------------------
                else if (leavingMobile)
                {
                    // All units end up dismounted outside the Mobile state.
                    IsMounted = false;

                    if (_mobileBonusApplied)
                    {
                        ApplyMovementBonus(-CUConstants.MOBILE_MOVEMENT_BONUS);
                        _mobileBonusApplied = false;
                    }
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(nameof(CombatUnit), nameof(UpdateMobilityState), ex);
            }
        }

        /// <summary>
        /// Adjusts Max and Current movement points by <paramref name="delta"/>, clamping current to
        /// the [0, newMax] range.
        /// </summary>
        private void ApplyMovementBonus(float delta)
        {
            float newMax = MovementPoints.Max + delta;
            float newCurrent = Mathf.Clamp(MovementPoints.Current + delta, 0f, newMax);

            MovementPoints.SetMax(newMax);
            MovementPoints.SetCurrent(newCurrent);
        }

        /// <summary>
        /// Checks if the unit can transition to the specified combat state.
        /// Validates unit type restrictions, adjacency rules, and resource requirements.
        /// </summary>
        /// <param name="targetState">The desired combat state</param>
        /// <returns>True if transition is allowed</returns>
        private bool CanChangeToState(CombatState targetState)
        {
            try
            {
                // Capture the UI message if needed.
                string errorMessage = $"Cannot change from {CombatState} to {targetState}: ";

                // Same state - no change needed
                if (CombatState == targetState)
                {
                    errorMessage += "Already in target state.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // Check if the unit is destroyed
                if (IsDestroyed())
                    throw new InvalidOperationException($"{UnitName} is destroyed and cannot change states.");

                // Air units and bases cannot change states.
                if (!CanUnitTypeChangeStates())
                {
                    errorMessage += "Unit type cannot change combat states.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // Check if transition is adjacent
                if (!IsAdjacentStateTransition(CombatState, targetState))
                {
                    errorMessage += $"Transition from {CombatState} to {targetState} is not adjacent.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // Check if the unit has critical supply levels
                if (DaysSupply.Current <= CUConstants.CRITICAL_SUPPLY_THRESHOLD)
                {
                    errorMessage += "Cannot change state with critical supply levels.";
                    AppService.CaptureUiMessage(errorMessage);
                    return false;
                }

                // Only limited CombatState transitions are allowed based on efficiency level.
                if (EfficiencyLevel == EfficiencyLevel.StaticOperations)
                {
                    if (CombatState == CombatState.Fortified || CombatState == CombatState.Entrenched || CombatState == CombatState.HastyDefense)
                    {
                        errorMessage += "Cannot change from defensive states in Static Operations.";
                        AppService.CaptureUiMessage(errorMessage);
                        return false; // Cannot change from defensive states in static operations
                    }

                    if (targetState == CombatState.Mobile)
                    {
                        errorMessage += "Cannot change to Mobile state in Static Operations.";
                        AppService.CaptureUiMessage(errorMessage);
                        return false; // Cannot change to Mobile state in static operations
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanChangeToState", e);
                return false;
            }
        }

        #endregion // CombatUnit Actions Helper Methods


        #region Experience System Helpers

        /// <summary>
        /// Calculates the experience level based on total experience points.
        /// </summary>
        /// <param name="totalPoints">Total experience points</param>
        /// <returns>The appropriate experience level</returns>
        private ExperienceLevel CalculateExperienceLevel(int totalPoints)
        {
            if (totalPoints >= (int)ExperiencePointLevels.Elite)
                return ExperienceLevel.Elite;
            else if (totalPoints >= (int)ExperiencePointLevels.Veteran)
                return ExperienceLevel.Veteran;
            else if (totalPoints >= (int)ExperiencePointLevels.Experienced)
                return ExperienceLevel.Experienced;
            else if (totalPoints >= (int)ExperiencePointLevels.Trained)
                return ExperienceLevel.Trained;
            else if (totalPoints >= (int)ExperiencePointLevels.Green)
                return ExperienceLevel.Green;
            else
                return ExperienceLevel.Raw;
        }

        /// <summary>
        /// Gets the minimum experience points required for a specific level.
        /// </summary>
        /// <param name="level">The experience level</param>
        /// <returns>Minimum points required for that level</returns>
        private int GetMinPointsForLevel(ExperienceLevel level)
        {
            return level switch
            {
                ExperienceLevel.Raw => (int)ExperiencePointLevels.Raw,
                ExperienceLevel.Green => (int)ExperiencePointLevels.Green,
                ExperienceLevel.Trained => (int)ExperiencePointLevels.Trained,
                ExperienceLevel.Experienced => (int)ExperiencePointLevels.Experienced,
                ExperienceLevel.Veteran => (int)ExperiencePointLevels.Veteran,
                ExperienceLevel.Elite => (int)ExperiencePointLevels.Elite,
                _ => 0,
            };
        }

        /// <summary>
        /// Gets the next experience level after the specified level.
        /// Returns Elite if already at Elite.
        /// </summary>
        /// <param name="currentLevel">Current experience level</param>
        /// <returns>Next experience level</returns>
        private ExperienceLevel GetNextLevel(ExperienceLevel currentLevel)
        {
            return currentLevel switch
            {
                ExperienceLevel.Raw => ExperienceLevel.Green,
                ExperienceLevel.Green => ExperienceLevel.Trained,
                ExperienceLevel.Trained => ExperienceLevel.Experienced,
                ExperienceLevel.Experienced => ExperienceLevel.Veteran,
                ExperienceLevel.Veteran => ExperienceLevel.Elite,
                ExperienceLevel.Elite => ExperienceLevel.Elite,// Already at max
                _ => ExperienceLevel.Green,
            };
        }

        /// <summary>
        /// Called when the unit's experience level changes.
        /// Can be overridden or used to trigger events/notifications.
        /// </summary>
        /// <param name="previousLevel">The previous experience level</param>
        /// <param name="newLevel">The new experience level</param>
        protected virtual void OnExperienceLevelChanged(ExperienceLevel previousLevel, ExperienceLevel newLevel)
        {
            AppService.CaptureUiMessage($"{UnitName} has advanced from {previousLevel} to {newLevel}!");
        }

        /// <summary>
        /// Gets the combat effectiveness multiplier based on experience level.
        /// Used to modify combat values based on unit experience.
        /// </summary>
        /// <returns>Multiplier for combat effectiveness (1.0 = normal)</returns>
        private float GetExperienceMultiplier()
        {
            return ExperienceLevel switch
            {
                ExperienceLevel.Raw => CUConstants.RAW_XP_MODIFIER,                // -20% effectiveness
                ExperienceLevel.Green => CUConstants.GREEN_XP_MODIFIER,            // -10% effectiveness
                ExperienceLevel.Trained => CUConstants.TRAINED_XP_MODIFIER,        // Normal effectiveness
                ExperienceLevel.Experienced => CUConstants.EXPERIENCED_XP_MODIFIER,// +10% effectiveness
                ExperienceLevel.Veteran => CUConstants.VETERAN_XP_MODIFIER,        // +20% effectiveness
                ExperienceLevel.Elite => CUConstants.ELITE_XP_MODIFIER,            // +30% effectiveness
                _ => 1.0f,
            };
        }

        #endregion // Experience System Helpers


        #region Debugging

        /// <summary>
        /// Direct change of combat state for debugging purposes.
        /// </summary>
        /// <param name="newState"></param>
        public void DebugSetCombatState(CombatState newState)
        {
            CombatState = newState;
        }

        /// <summary>
        /// Sets the mounted state of the object for debugging purposes.
        /// </summary>
        /// <param name="isMounted">A value indicating whether the object should be marked as mounted.  <see langword="true"/> to mark the
        /// object as mounted; otherwise, <see langword="false"/>.</param>
        public void DebugSetMounted(bool isMounted)
        {
            IsMounted = isMounted;
        }

        #endregion // Debugging


        #region ICloneable Implementation

        public object Clone()
        {
            try
            {
                // Create new unit using constructor with same core properties
                // This ensures proper initialization and generates a new UnitID
                var clone = new CombatUnit(
                    this.UnitName,
                    this.UnitType,
                    this.Classification,
                    this.Role,
                    this.Side,
                    this.Nationality,
                    this.DeployedProfileID,      // Direct enum value copy
                    this.MountedProfileID,       // Direct enum value copy
                    this.IntelProfileType,       // Static enum value - no reference resolution needed
                    this.IsTransportable,
                    this.DepotCategory,          // Copy depot category for base units
                    this.DepotSize               // Copy depot size for base units
                );

                // Deep copy all StatsMaxCurrent objects by reconstructing them
                clone.HitPoints = new StatsMaxCurrent(this.HitPoints.Max, this.HitPoints.Current);
                clone.DaysSupply = new StatsMaxCurrent(this.DaysSupply.Max, this.DaysSupply.Current);
                clone.MovementPoints = new StatsMaxCurrent(this.MovementPoints.Max, this.MovementPoints.Current);
                clone.MoveActions = new StatsMaxCurrent(this.MoveActions.Max, this.MoveActions.Current);
                clone.CombatActions = new StatsMaxCurrent(this.CombatActions.Max, this.CombatActions.Current);
                clone.DeploymentActions = new StatsMaxCurrent(this.DeploymentActions.Max, this.DeploymentActions.Current);
                clone.OpportunityActions = new StatsMaxCurrent(this.OpportunityActions.Max, this.OpportunityActions.Current);
                clone.IntelActions = new StatsMaxCurrent(this.IntelActions.Max, this.IntelActions.Current);

                // Copy per-unit state data
                clone.SetExperience(this.ExperiencePoints);
                clone.EfficiencyLevel = this.EfficiencyLevel;
                clone.IsMounted = this.IsMounted;
                clone.CombatState = this.CombatState;
                clone.MapPos = this.MapPos;
                clone._mobileBonusApplied = this._mobileBonusApplied; // Copy mobile bonus state
                clone.SpottedLevel = this.SpottedLevel;

                // NOTE: LeaderID is NOT copied - templates should never have leaders assigned
                // Leaders must be assigned manually after cloning
                clone.LeaderID = null;

                // Clone facility data for base units
                if (this.IsBase)
                {
                    // Copy common facility properties
                    clone.BaseDamage = this.BaseDamage;
                    clone.OperationalCapacity = this.OperationalCapacity;
                    clone.FacilityType = this.FacilityType;

                    // Copy supply depot properties
                    clone.DepotSize = this.DepotSize;
                    clone.StockpileInDays = this.StockpileInDays;
                    clone.GenerationRate = this.GenerationRate;
                    clone.SupplyProjection = this.SupplyProjection;
                    clone.SupplyPenetration = this.SupplyPenetration;
                    clone.DepotCategory = this.DepotCategory;

                    // NOTE: Air unit attachments are NEVER cloned for templates
                    // Templates should be clean and ready for fresh assignments
                    // _airUnitsAttached remains empty in the clone
                    clone.AirUnitsAttached = clone._airUnitsAttached.AsReadOnly();
                }

                return clone;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
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
            try
            {
                // Load basic properties
                UnitName = info.GetString(nameof(UnitName));
                UnitID = info.GetString(nameof(UnitID));
                UnitType = (UnitType)info.GetValue(nameof(UnitType), typeof(UnitType));
                Classification = (UnitClassification)info.GetValue(nameof(Classification), typeof(UnitClassification));
                Role = (UnitRole)info.GetValue(nameof(Role), typeof(UnitRole));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                IsTransportable = info.GetBoolean(nameof(IsTransportable));
                SpottedLevel = (SpottedLevel)info.GetValue(nameof(SpottedLevel), typeof(SpottedLevel));

                // Load IntelProfileType directly as enum value
                IntelProfileType = (IntelProfileTypes)info.GetValue(nameof(IntelProfileType), typeof(IntelProfileTypes));

                // Load profile IDs directly as enum values (no reference resolution needed)
                DeployedProfileID = (WeaponSystems)info.GetValue(nameof(DeployedProfileID), typeof(WeaponSystems));
                MountedProfileID = (WeaponSystems)info.GetValue(nameof(MountedProfileID), typeof(WeaponSystems));

                // Load leader ID directly - no temporary storage needed
                LeaderID = info.GetString("LeaderID");
                if (string.IsNullOrEmpty(LeaderID))
                    LeaderID = null;

                // Deserialize owned StatsMaxCurrent objects
                HitPoints = new StatsMaxCurrent(
                    info.GetSingle("HitPoints_Max"),
                    info.GetSingle("HitPoints_Current")
                );

                DaysSupply = new StatsMaxCurrent(
                    info.GetSingle("DaysSupply_Max"),
                    info.GetSingle("DaysSupply_Current")
                );

                MovementPoints = new StatsMaxCurrent(
                    info.GetSingle("MovementPoints_Max"),
                    info.GetSingle("MovementPoints_Current")
                );

                MoveActions = new StatsMaxCurrent(
                    info.GetSingle("MoveActions_Max"),
                    info.GetSingle("MoveActions_Current")
                );

                CombatActions = new StatsMaxCurrent(
                    info.GetSingle("CombatActions_Max"),
                    info.GetSingle("CombatActions_Current")
                );

                DeploymentActions = new StatsMaxCurrent(
                    info.GetSingle("DeploymentActions_Max"),
                    info.GetSingle("DeploymentActions_Current")
                );

                OpportunityActions = new StatsMaxCurrent(
                    info.GetSingle("OpportunityActions_Max"),
                    info.GetSingle("OpportunityActions_Current")
                );

                IntelActions = new StatsMaxCurrent(
                    info.GetSingle("IntelActions_Max"),
                    info.GetSingle("IntelActions_Current")
                );

                // Load simple properties
                ExperiencePoints = info.GetInt32(nameof(ExperiencePoints));
                ExperienceLevel = (ExperienceLevel)info.GetValue(nameof(ExperienceLevel), typeof(ExperienceLevel));
                EfficiencyLevel = (EfficiencyLevel)info.GetValue(nameof(EfficiencyLevel), typeof(EfficiencyLevel));
                IsMounted = info.GetBoolean(nameof(IsMounted));
                CombatState = (CombatState)info.GetValue(nameof(CombatState), typeof(CombatState));
                MapPos = (Coordinate2D)info.GetValue(nameof(MapPos), typeof(Coordinate2D));
                _mobileBonusApplied = info.GetBoolean(SERIAL_KEY_MOBILE_BONUS);

                // Deserialize facility data if this is a base unit
                if (IsBase)
                {
                    // Initialize facility collections first
                    _airUnitsAttached.Clear();
                    _attachedUnitIDs.Clear();

                    // Load common facility properties
                    BaseDamage = info.GetInt32(nameof(BaseDamage));
                    OperationalCapacity = (OperationalCapacity)info.GetValue(nameof(OperationalCapacity), typeof(OperationalCapacity));
                    FacilityType = (FacilityType)info.GetValue(nameof(FacilityType), typeof(FacilityType));

                    // Load supply depot properties
                    DepotSize = (DepotSize)info.GetValue(nameof(DepotSize), typeof(DepotSize));
                    StockpileInDays = info.GetSingle(nameof(StockpileInDays));
                    GenerationRate = (SupplyGenerationRate)info.GetValue(nameof(GenerationRate), typeof(SupplyGenerationRate));
                    SupplyProjection = (SupplyProjection)info.GetValue(nameof(SupplyProjection), typeof(SupplyProjection));
                    SupplyPenetration = info.GetBoolean(nameof(SupplyPenetration));
                    DepotCategory = (DepotCategory)info.GetValue(nameof(DepotCategory), typeof(DepotCategory));

                    // Load air unit attachments for later resolution
                    int airUnitCount = info.GetInt32("AirUnitCount");
                    for (int i = 0; i < airUnitCount; i++)
                    {
                        string unitID = info.GetString($"AirUnitID_{i}");
                        if (!string.IsNullOrEmpty(unitID))
                        {
                            _attachedUnitIDs.Add(unitID);
                        }
                    }

                    // Initialize readonly property
                    AirUnitsAttached = _airUnitsAttached.AsReadOnly();
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CombatUnit), e);
                throw;
            }
        }

        /// <summary>
        /// Serializes CombatUnit data for saving to file.
        /// </summary>
        /// <param name="info">Serialization info to store data</param>
        /// <param name="context">Streaming context for serialization</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Serialize basic properties
                info.AddValue(nameof(UnitName), UnitName);
                info.AddValue(nameof(UnitID), UnitID);
                info.AddValue(nameof(UnitType), UnitType);
                info.AddValue(nameof(Classification), Classification);
                info.AddValue(nameof(Role), Role);
                info.AddValue(nameof(Side), Side);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(IsTransportable), IsTransportable);
                info.AddValue(nameof(IsBase), IsBase);
                info.AddValue(nameof(SpottedLevel), SpottedLevel);

                // Serialize IntelProfileType directly as enum value
                info.AddValue(nameof(IntelProfileType), IntelProfileType);

                // Serialize profile IDs directly as enum values (no reference resolution needed)
                info.AddValue(nameof(DeployedProfileID), DeployedProfileID);
                info.AddValue(nameof(MountedProfileID), MountedProfileID);

                // Serialize leader reference as ID (simple string)
                info.AddValue("LeaderID", LeaderID ?? "");

                // Serialize owned StatsMaxCurrent objects as Max/Current pairs
                info.AddValue("HitPoints_Max", HitPoints.Max);
                info.AddValue("HitPoints_Current", HitPoints.Current);
                info.AddValue("DaysSupply_Max", DaysSupply.Max);
                info.AddValue("DaysSupply_Current", DaysSupply.Current);
                info.AddValue("MovementPoints_Max", MovementPoints.Max);
                info.AddValue("MovementPoints_Current", MovementPoints.Current);
                info.AddValue("MoveActions_Max", MoveActions.Max);
                info.AddValue("MoveActions_Current", MoveActions.Current);
                info.AddValue("CombatActions_Max", CombatActions.Max);
                info.AddValue("CombatActions_Current", CombatActions.Current);
                info.AddValue("DeploymentActions_Max", DeploymentActions.Max);
                info.AddValue("DeploymentActions_Current", DeploymentActions.Current);
                info.AddValue("OpportunityActions_Max", OpportunityActions.Max);
                info.AddValue("OpportunityActions_Current", OpportunityActions.Current);
                info.AddValue("IntelActions_Max", IntelActions.Max);
                info.AddValue("IntelActions_Current", IntelActions.Current);

                // Serialize simple properties
                info.AddValue(nameof(ExperiencePoints), ExperiencePoints);
                info.AddValue(nameof(ExperienceLevel), ExperienceLevel);
                info.AddValue(nameof(EfficiencyLevel), EfficiencyLevel);
                info.AddValue(nameof(IsMounted), IsMounted);
                info.AddValue(nameof(CombatState), CombatState);
                info.AddValue(nameof(MapPos), MapPos);
                info.AddValue(SERIAL_KEY_MOBILE_BONUS, _mobileBonusApplied);

                // Serialize facility data if this is a base unit
                if (IsBase)
                {
                    // Serialize common facility properties
                    info.AddValue(nameof(BaseDamage), BaseDamage);
                    info.AddValue(nameof(OperationalCapacity), OperationalCapacity);
                    info.AddValue(nameof(FacilityType), FacilityType);

                    // Serialize supply depot properties
                    info.AddValue(nameof(DepotSize), DepotSize);
                    info.AddValue(nameof(StockpileInDays), StockpileInDays);
                    info.AddValue(nameof(GenerationRate), GenerationRate);
                    info.AddValue(nameof(SupplyProjection), SupplyProjection);
                    info.AddValue(nameof(SupplyPenetration), SupplyPenetration);
                    info.AddValue(nameof(DepotCategory), DepotCategory);

                    // Serialize air unit attachments as Unit IDs to avoid circular references
                    info.AddValue("AirUnitCount", _airUnitsAttached.Count);
                    for (int i = 0; i < _airUnitsAttached.Count; i++)
                    {
                        info.AddValue($"AirUnitID_{i}", _airUnitsAttached[i].UnitID);
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetObjectData), e);
                throw;
            }
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

            try
            {
                // Validate leader assignment consistency
                if (IsLeaderAssigned && UnitLeader == null)
                {
                    errors.Add($"Unit {UnitName} has IsLeaderAssigned=true but no CommandingOfficer");
                }
                else if (!IsLeaderAssigned && UnitLeader != null)
                {
                    errors.Add($"Unit {UnitName} has CommandingOfficer but IsLeaderAssigned=false");
                }

                // Validate facility consistency for base units
                if (IsBase)
                {
                    // Validate facility type matches classification
                    switch (Classification)
                    {
                        case UnitClassification.HQ:
                            if (FacilityType != FacilityType.HQ)
                                errors.Add($"HQ unit {UnitName} has incorrect facility type: {FacilityType}");
                            break;
                        case UnitClassification.DEPOT:
                            if (FacilityType != FacilityType.SupplyDepot)
                                errors.Add($"Depot unit {UnitName} has incorrect facility type: {FacilityType}");
                            break;
                        case UnitClassification.AIRB:
                            if (FacilityType != FacilityType.Airbase)
                                errors.Add($"Airbase unit {UnitName} has incorrect facility type: {FacilityType}");
                            break;
                    }

                    // Validate airbase attachments
                    if (FacilityType == FacilityType.Airbase)
                    {
                        foreach (var attachedUnit in AirUnitsAttached)
                        {
                            if (attachedUnit.UnitType != UnitType.AirUnit)
                            {
                                errors.Add($"Airbase {UnitName} has non-air unit {attachedUnit.UnitName} attached");
                            }
                        }

                        if (AirUnitsAttached.Count > CUConstants.MAX_AIR_UNITS)
                        {
                            errors.Add($"Airbase {UnitName} has {AirUnitsAttached.Count} attached units, exceeding maximum of {CUConstants.MAX_AIR_UNITS}");
                        }
                    }
                }
                else if (!IsBase && FacilityType != FacilityType.HQ) // HQ can be on non-base units
                {
                    errors.Add($"Non-base unit {UnitName} has FacilityType set to {FacilityType}");
                }

                // Validate profile references
                if (GetDeployedProfile() == null)
                {
                    errors.Add($"Unit {UnitName} has invalid DeployedProfileID: {DeployedProfileID}");
                }

                if (IsMounted && MountedProfileID != WeaponSystems.DEFAULT && GetMountedProfile() == null)
                {
                    errors.Add($"Unit {UnitName} is mounted but has invalid MountedProfileID: {MountedProfileID}");
                }

                // Validate state consistency
                if (HitPoints.Current > HitPoints.Max)
                {
                    errors.Add($"Unit {UnitName} has current HP ({HitPoints.Current}) greater than max HP ({HitPoints.Max})");
                }

                if (MovementPoints.Current > MovementPoints.Max)
                {
                    errors.Add($"Unit {UnitName} has current movement points ({MovementPoints.Current}) greater than max ({MovementPoints.Max})");
                }

                // Validate action counts
                if (MoveActions.Current > MoveActions.Max)
                {
                    errors.Add($"Unit {UnitName} has current move actions ({MoveActions.Current}) greater than max ({MoveActions.Max})");
                }

                if (CombatActions.Current > CombatActions.Max)
                {
                    errors.Add($"Unit {UnitName} has current combat actions ({CombatActions.Current}) greater than max ({CombatActions.Max})");
                }

            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ValidateInternalConsistency", e);
                errors.Add($"Validation failed with exception: {e.Message}");
            }

            return errors;
        }

        #endregion // Validation Methods
    }
}