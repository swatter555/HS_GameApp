using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents a military unit with identification, base stats, and optional transport mounting.
    /// Implements an event-driven design pattern for state changes.
    /// Uses StatsMaxCurrent for paired max/current value management.
    /// </summary>
    [Serializable]
    public class CombatUnit : ICloneable, ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        #endregion

        #region Fields

        // Temporary fields for deserialization reference resolution
        private string unresolvedDeployedProfileID = "";
        private string unresolvedMountedProfileID = "";
        private string unresolvedUnitProfileID = "";
        private string unresolvedLandBaseProfileID = "";
        private string unresolvedLeaderID = "";

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
        public bool IsLandBase { get; private set; }

        // Profiles contain unit stats and capabilities.
        public WeaponSystemProfile DeployedProfile { get; private set; }
        public WeaponSystemProfile MountedProfile { get; private set; }
        public UnitProfile UnitProfile { get; private set; }
        public LandBaseFacility LandBaseFacility { get; private set; }

        // The unit's leader.
        public Leader CommandingOfficer { get; private set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelActions { get; private set; }

        // State data using StatsMaxCurrent where appropriate
        public int ExperiencePoints { get; private set; }
        public ExperienceLevel ExperienceLevel { get; private set; }
        public EfficiencyLevel EfficiencyLevel { get; private set; }
        public bool IsMounted { get; private set; }
        public CombatState CombatState { get; private set; }
        public StatsMaxCurrent HitPoints { get; private set; }
        public StatsMaxCurrent DaysSupply { get; private set; }
        public StatsMaxCurrent MovementPoints { get; private set; }
        public Vector2 MapPos { get; private set; }

        #endregion


        #region Constructors

        public CombatUnit()
        {

        }

        /// <summary>
        /// Creates a new CombatUnit with the specified core properties.
        /// </summary>
        /// <param name="unitName">Display name of the unit</param>
        /// <param name="unitType">Type of unit (land, air, naval)</param>
        /// <param name="classification">Unit classification (tank, infantry, etc.)</param>
        /// <param name="role">Primary role of the unit</param>
        /// <param name="side">Which side controls this unit</param>
        /// <param name="nationality">National affiliation</param>
        /// <param name="deployedProfile">Combat profile when deployed</param>
        /// <param name="mountedProfile">Combat profile when mounted (can be null)</param>
        /// <param name="unitProfile">Organizational profile for tracking losses</param>
        /// <param name="isTransportable">Whether this unit can be transported</param>
        /// <param name="isLandBase">Whether this unit is a land-based facility</param>
        /// <param name="landBaseProfile">Land base profile if applicable (can be null)</param>
        public CombatUnit(
            string unitName,
            UnitType unitType,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            WeaponSystemProfile deployedProfile,
            WeaponSystemProfile mountedProfile,
            UnitProfile unitProfile,
            bool isTransportable,
            bool isLandBase = false,
            LandBaseFacility landBaseProfile = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(unitName))
                    throw new ArgumentException("Unit name cannot be null or empty", nameof(unitName));

                if (deployedProfile == null)
                    throw new ArgumentNullException(nameof(deployedProfile), "Deployed profile is required");

                if (unitProfile == null)
                    throw new ArgumentNullException(nameof(unitProfile), "Unit profile is required");

                // Validate land base requirements
                if (isLandBase && landBaseProfile == null)
                    throw new ArgumentException("Land base profile is required when isLandBase is true", nameof(landBaseProfile));

                // Set identification and metadata
                UnitName = unitName;
                UnitID = Guid.NewGuid().ToString();
                UnitType = unitType;
                Classification = classification;
                Role = role;
                Side = side;
                Nationality = nationality;
                IsTransportable = isTransportable;
                IsLandBase = isLandBase;

                // Set profiles
                DeployedProfile = deployedProfile;
                MountedProfile = mountedProfile;
                UnitProfile = unitProfile;
                LandBaseFacility = landBaseProfile;

                // Initialize leader (will be null until assigned)
                CommandingOfficer = null;

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize state with default values
                ExperiencePoints = 0;
                ExperienceLevel = ExperienceLevel.Raw;
                EfficiencyLevel = EfficiencyLevel.FullyOperational;
                IsMounted = false;
                CombatState = CombatState.Deployed;

                // Initialize StatsMaxCurrent properties
                HitPoints = new StatsMaxCurrent(CUConstants.MAX_HP);
                DaysSupply = new StatsMaxCurrent(CUConstants.MaxDaysSupplyUnit);

                // Initialize movement based on unit classification
                InitializeMovementPoints();

                // Initialize position to origin (will be set when placed on map)
                MapPos = Vector2.zero;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

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
                IsLandBase = info.GetBoolean(nameof(IsLandBase));

                // Store profile IDs for later resolution (don't resolve objects yet)
                unresolvedDeployedProfileID = info.GetString("DeployedProfileID");
                unresolvedMountedProfileID = info.GetString("MountedProfileID");
                unresolvedUnitProfileID = info.GetString("UnitProfileID");
                unresolvedLandBaseProfileID = info.GetString("LandBaseProfileID");
                unresolvedLeaderID = info.GetString("LeaderID");

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
                MapPos = (Vector2)info.GetValue(nameof(MapPos), typeof(Vector2));

                // Leave all object references null - they will be resolved later
                DeployedProfile = null;
                MountedProfile = null;
                UnitProfile = null;
                LandBaseFacility = null;
                CommandingOfficer = null;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, nameof(CombatUnit), e);
                throw;
            }
        }

        #endregion


        #region Initialization Methods

        /// <summary>
        /// Initializes action counts based on unit type and classification.
        /// Most units get standard action counts, with variations for special cases.
        /// </summary>
        private void InitializeActionCounts()
        {
            // Standard action counts for most units
            int moveActions = 2;
            int combatActions = 1;
            int deploymentActions = 1;
            int opportunityActions = 1;
            int intelActions = 1;

            // Adjust based on unit classification
            switch (Classification)
            {
                case UnitClassification.RECON:
                    moveActions = 3; // Recon units are more mobile
                    intelActions = 2; // Better at intelligence gathering
                    break;

                case UnitClassification.ART:
                case UnitClassification.SPA:
                case UnitClassification.ROC:
                    moveActions = 1; // Artillery is less mobile
                    break;

                case UnitClassification.SAM:
                case UnitClassification.SPSAM:
                case UnitClassification.AAA:
                case UnitClassification.SPAAA:
                    opportunityActions = 2; // Air defense gets more opportunity actions
                    break;

                case UnitClassification.SPECF:
                case UnitClassification.SPECM:
                case UnitClassification.SPECH:
                    intelActions = 2; // Special forces are better at intel
                    break;

                case UnitClassification.BASE:
                case UnitClassification.DEPOT:
                case UnitClassification.AIRB:
                    moveActions = 0; // Bases don't move
                    combatActions = 0; // Bases don't attack
                    deploymentActions = 0; // Bases don't deploy
                    opportunityActions = 0; // Bases don't have opportunity actions
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
                UnitClassification.TANK or UnitClassification.MECH or UnitClassification.RECON or UnitClassification.SPA or UnitClassification.SPAAA or UnitClassification.SPSAM => CUConstants.MECH_MOV,
                UnitClassification.MOT or UnitClassification.MAB or UnitClassification.MMAR or UnitClassification.AM or UnitClassification.MAM or UnitClassification.SPECM or UnitClassification.ROC => CUConstants.MOT_MOV,
                UnitClassification.INF or UnitClassification.AB or UnitClassification.MAR or UnitClassification.AT or UnitClassification.SPECF or UnitClassification.ART or UnitClassification.SAM or UnitClassification.AAA or UnitClassification.ENG => CUConstants.FOOT_MOV,
                UnitClassification.ASF or UnitClassification.MRF or UnitClassification.ATT or UnitClassification.BMB or UnitClassification.RCN or UnitClassification.FWT => CUConstants.FIXEDWING_MOV,
                UnitClassification.AHEL or UnitClassification.THEL or UnitClassification.SPECH => CUConstants.HELO_MOV,
                UnitClassification.BASE or UnitClassification.DEPOT or UnitClassification.AIRB => 0,// Bases don't move
                _ => CUConstants.FOOT_MOV,// Default to foot movement
            };
            MovementPoints = new StatsMaxCurrent(maxMovement);
        }

        #endregion


        #region Public Methods

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

        #endregion // Public Methods


        #region Experience System Methods

        /// <summary>
        /// Adds experience points to the unit and checks for level advancement.
        /// Returns true if the unit leveled up.
        /// </summary>
        /// <param name="points">Experience points to add</param>
        /// <returns>True if the unit advanced to a new experience level</returns>
        public bool AddExperience(int points)
        {
            try
            {
                if (points <= 0)
                    return false;

                var previousLevel = ExperienceLevel;
                ExperiencePoints += points;

                // Check if we've advanced to a new level
                var newLevel = CalculateExperienceLevel(ExperiencePoints);
                if (newLevel != previousLevel)
                {
                    ExperienceLevel = newLevel;
                    OnExperienceLevelChanged(previousLevel, newLevel);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AddExperience", e);
                return false;
            }
        }

        /// <summary>
        /// Sets the unit's experience points directly and updates the level accordingly.
        /// Used for loading saved games or manual experience setting.
        /// </summary>
        /// <param name="points">Total experience points</param>
        public void SetExperience(int points)
        {
            try
            {
                if (points < 0)
                    points = 0;

                ExperiencePoints = points;
                ExperienceLevel = CalculateExperienceLevel(points);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetExperience", e);
            }
        }

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
        /// Gets the experience level as a human-readable string with progress information.
        /// </summary>
        /// <returns>Formatted string showing level and progress</returns>
        public string GetExperienceDisplayString()
        {
            var pointsToNext = GetPointsToNextLevel();
            if (pointsToNext > 0)
            {
                return $"{ExperienceLevel} ({ExperiencePoints} XP, {pointsToNext} to next)";
            }
            else
            {
                return $"{ExperienceLevel} (Max Level - {ExperiencePoints} XP)";
            }
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
        /// Checks if the unit is considered experienced (Veteran or Elite).
        /// Used for various game mechanics that benefit experienced units.
        /// </summary>
        /// <returns>True if unit is Veteran or Elite</returns>
        public bool IsExperienced()
        {
            return ExperienceLevel == ExperienceLevel.Veteran || ExperienceLevel == ExperienceLevel.Elite;
        }

        /// <summary>
        /// Checks if the unit is considered elite level.
        /// </summary>
        /// <returns>True if unit is Elite</returns>
        public bool IsElite()
        {
            return ExperienceLevel == ExperienceLevel.Elite;
        }

        /// <summary>
        /// Called when the unit's experience level changes.
        /// Can be overridden or used to trigger events/notifications.
        /// </summary>
        /// <param name="previousLevel">The previous experience level</param>
        /// <param name="newLevel">The new experience level</param>
        protected virtual void OnExperienceLevelChanged(ExperienceLevel previousLevel, ExperienceLevel newLevel)
        {
            // Could trigger events, sound effects, UI notifications, etc.
            // For now, just log the advancement
            Debug.Log($"{UnitName} advanced from {previousLevel} to {newLevel}!");
        }

        /// <summary>
        /// Gets the combat effectiveness multiplier based on experience level.
        /// Used to modify combat values based on unit experience.
        /// </summary>
        /// <returns>Multiplier for combat effectiveness (1.0 = normal)</returns>
        public float GetExperienceMultiplier()
        {
            return ExperienceLevel switch
            {
                ExperienceLevel.Raw => CUConstants.RAW_XP_MODIFIER,// -20% effectiveness
                ExperienceLevel.Green => CUConstants.GREEN_XP_MODIFIER,// -10% effectiveness
                ExperienceLevel.Trained => CUConstants.TRAINED_XP_MODIFIER,// Normal effectiveness
                ExperienceLevel.Experienced => CUConstants.EXPERIENCED_XP_MODIFIER,// +10% effectiveness
                ExperienceLevel.Veteran => CUConstants.VETERAN_XP_MODIFIER,// +20% effectiveness
                ExperienceLevel.Elite => CUConstants.ELITE_XP_MODIFIER,// +30% effectiveness
                _ => 1.0f,
            };
        }

        #endregion // Experience System Methods


        #region Leader Assignment System

        /// <summary>
        /// Assigns a leader to command this unit.
        /// Validates that the leader can command this unit type and handles state management.
        /// </summary>
        /// <param name="leader">The leader to assign to this unit</param>
        public void AssignLeader(Leader leader)
        {
            try
            {
                // TODO: This method will need to interact with the game state manager
                // to handle leader assignment validation and state updates
                throw new NotImplementedException("CombatUnit leader assignment not yet implemented");
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AssignLeader", e);
                throw;
            }
        }

        /// <summary>
        /// Removes the commanding officer from this unit.
        /// Handles state management and cleanup.
        /// </summary>
        public void RemoveLeader()
        {
            try
            {
                // TODO: This method will need to interact with the game state manager
                // to handle leader removal validation and state updates
                throw new NotImplementedException("CombatUnit leader removal not yet implemented");
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RemoveLeader", e);
                throw;
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
                // Return empty dictionary if no leader assigned
                if (CommandingOfficer == null)
                {
                    return bonuses;
                }

                // Iterate through all skill bonus types and get non-zero values
                foreach (SkillBonusType bonusType in (SkillBonusType[])Enum.GetValues(typeof(SkillBonusType)))
                {
                    if (bonusType == SkillBonusType.None) continue;

                    float bonusValue = CommandingOfficer.GetBonusValue(bonusType);
                    if (bonusValue != 0f)
                    {
                        bonuses[bonusType] = bonusValue;
                    }
                }

                return bonuses;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderBonuses", e);
                return bonuses; // Return empty dictionary on error
            }
        }

        /// <summary>
        /// Checks if the unit has a specific leader capability/bonus.
        /// </summary>
        /// <param name="bonusType">The bonus type to check for</param>
        /// <returns>True if the leader provides this capability</returns>
        public bool HasLeaderCapability(SkillBonusType bonusType)
        {
            try
            {
                if (CommandingOfficer == null)
                {
                    return false;
                }

                return CommandingOfficer.HasCapability(bonusType);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "HasLeaderCapability", e);
                return false;
            }
        }

        /// <summary>
        /// Gets a specific leader bonus value.
        /// Returns 0 if no leader assigned or bonus not present.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve</param>
        /// <returns>The bonus value, or 0 if not present</returns>
        public float GetLeaderBonus(SkillBonusType bonusType)
        {
            try
            {
                if (CommandingOfficer == null || bonusType == SkillBonusType.None)
                {
                    return 0f;
                }

                return CommandingOfficer.GetBonusValue(bonusType);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderBonus", e);
                return 0f;
            }
        }

        /// <summary>
        /// Checks if a leader is currently assigned to this unit.
        /// </summary>
        /// <returns>True if a leader is assigned</returns>
        public bool HasLeader()
        {
            return CommandingOfficer != null;
        }

        /// <summary>
        /// Gets the leader's name for display purposes.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Leader name or empty string</returns>
        public string GetLeaderName()
        {
            try
            {
                return CommandingOfficer?.Name ?? "";
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderName", e);
                return "";
            }
        }

        /// <summary>
        /// Gets the leader's command grade for display and bonus calculations.
        /// Returns JuniorGrade if no leader assigned.
        /// </summary>
        /// <returns>Leader's command grade</returns>
        public CommandGrade GetLeaderGrade()
        {
            try
            {
                return CommandingOfficer?.CommandGrade ?? CommandGrade.JuniorGrade;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderGrade", e);
                return CommandGrade.JuniorGrade;
            }
        }

        /// <summary>
        /// Gets the leader's reputation points for display purposes.
        /// Returns 0 if no leader assigned.
        /// </summary>
        /// <returns>Leader's reputation points</returns>
        public int GetLeaderReputation()
        {
            try
            {
                return CommandingOfficer?.ReputationPoints ?? 0;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderReputation", e);
                return 0;
            }
        }

        /// <summary>
        /// Gets the leader's formatted rank based on nationality.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Formatted rank string</returns>
        public string GetLeaderRank()
        {
            try
            {
                return CommandingOfficer?.FormattedRank ?? "";
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderRank", e);
                return "";
            }
        }

        /// <summary>
        /// Gets the leader's combat command ability modifier.
        /// Returns Average if no leader assigned.
        /// </summary>
        /// <returns>Leader's combat command ability</returns>
        public CommandAbility GetLeaderCommandAbility()
        {
            try
            {
                return CommandingOfficer?.CombatCommand ?? CommandAbility.Average;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetLeaderCommandAbility", e);
                return CommandAbility.Average;
            }
        }

        /// <summary>
        /// Checks if the leader has unlocked a specific skill.
        /// Returns false if no leader assigned.
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill is unlocked</returns>
        public bool HasLeaderSkill(Enum skillEnum)
        {
            try
            {
                if (CommandingOfficer == null)
                {
                    return false;
                }

                return CommandingOfficer.IsSkillUnlocked(skillEnum);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "HasLeaderSkill", e);
                return false;
            }
        }

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
                if (CommandingOfficer == null)
                {
                    return;
                }

                CommandingOfficer.AwardReputationForAction(actionType, contextMultiplier);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
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
                if (CommandingOfficer == null || amount <= 0)
                {
                    return;
                }

                CommandingOfficer.AwardReputation(amount);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        #endregion // Leader Assignment System


        #region Action Consumption System

        /// <summary>
        /// Consumes one move action if available.
        /// </summary>
        /// <returns>True if a move action was consumed, false if none available</returns>
        public bool ConsumeMoveAction()
        {
            try
            {
                if (MoveActions.Current >= 1f)
                {
                    MoveActions.SetCurrent(MoveActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeMoveAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one combat action if available.
        /// </summary>
        /// <returns>True if a combat action was consumed, false if none available</returns>
        public bool ConsumeCombatAction()
        {
            try
            {
                if (CombatActions.Current >= 1f)
                {
                    CombatActions.SetCurrent(CombatActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeCombatAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes movement points if available.
        /// </summary>
        /// <param name="points">Number of movement points to consume</param>
        /// <returns>True if movement points were consumed, false if insufficient</returns>
        public bool ConsumeMovementPoints(float points)
        {
            try
            {
                if (points <= 0f)
                {
                    throw new ArgumentException("Movement points must be positive", nameof(points));
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
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeMovementPoints", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one deployment action if available.
        /// </summary>
        /// <returns>True if a deployment action was consumed, false if none available</returns>
        public bool ConsumeDeploymentAction()
        {
            try
            {
                if (DeploymentActions.Current >= 1f)
                {
                    DeploymentActions.SetCurrent(DeploymentActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeDeploymentAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one opportunity action if available.
        /// </summary>
        /// <returns>True if an opportunity action was consumed, false if none available</returns>
        public bool ConsumeOpportunityAction()
        {
            try
            {
                if (OpportunityActions.Current >= 1f)
                {
                    OpportunityActions.SetCurrent(OpportunityActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeOpportunityAction", e);
                return false;
            }
        }

        /// <summary>
        /// Consumes one intelligence action if available.
        /// </summary>
        /// <returns>True if an intelligence action was consumed, false if none available</returns>
        public bool ConsumeIntelAction()
        {
            try
            {
                if (IntelActions.Current >= 1f)
                {
                    IntelActions.SetCurrent(IntelActions.Current - 1f);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeIntelAction", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if the unit can consume a move action.
        /// </summary>
        /// <returns>True if at least one move action is available</returns>
        public bool CanConsumeMoveAction()
        {
            return MoveActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume a combat action.
        /// </summary>
        /// <returns>True if at least one combat action is available</returns>
        public bool CanConsumeCombatAction()
        {
            return CombatActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume the specified movement points.
        /// </summary>
        /// <param name="points">Number of movement points needed</param>
        /// <returns>True if sufficient movement points are available</returns>
        public bool CanConsumeMovementPoints(float points)
        {
            if (points <= 0f) return false;
            return MovementPoints.Current >= points;
        }

        /// <summary>
        /// Checks if the unit can consume a deployment action.
        /// </summary>
        /// <returns>True if at least one deployment action is available</returns>
        public bool CanConsumeDeploymentAction()
        {
            return DeploymentActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume an opportunity action.
        /// </summary>
        /// <returns>True if at least one opportunity action is available</returns>
        public bool CanConsumeOpportunityAction()
        {
            return OpportunityActions.Current >= 1f;
        }

        /// <summary>
        /// Checks if the unit can consume an intelligence action.
        /// </summary>
        /// <returns>True if at least one intelligence action is available</returns>
        public bool CanConsumeIntelAction()
        {
            return IntelActions.Current >= 1f;
        }

        /// <summary>
        /// Gets the number of available actions of each type.
        /// </summary>
        /// <returns>Dictionary mapping action types to available counts</returns>
        public Dictionary<string, float> GetAvailableActions()
        {
            return new Dictionary<string, float>
            {
                ["Move"] = MoveActions.Current,
                ["Combat"] = CombatActions.Current,
                ["Deployment"] = DeploymentActions.Current,
                ["Opportunity"] = OpportunityActions.Current,
                ["Intelligence"] = IntelActions.Current,
                ["MovementPoints"] = MovementPoints.Current
            };
        }

        #endregion // Action Consumption System


        #region Position and Movement Management

        /// <summary>
        /// Sets the unit's position on the map.
        /// </summary>
        /// <param name="newPos">The new position coordinates</param>
        public void SetPosition(Vector2 newPos)
        {
            try
            {
                // Use reflection to set MapPos since it has a private setter
                var unitType = typeof(CombatUnit);
                var mapPosProperty = unitType.GetProperty("MapPos");
                mapPosProperty?.SetValue(this, newPos);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetPosition", e);
                throw;
            }
        }

        /// <summary>
        /// Checks if the unit can move to the specified position.
        /// This is a basic validation - full movement rules will be implemented later.
        /// </summary>
        /// <param name="targetPos">The target position to validate</param>
        /// <returns>True if movement appears valid</returns>
        public bool CanMoveTo(Vector2 targetPos)
        {
            try
            {
                // Basic validations - will be expanded when terrain system is implemented

                // Check if unit has movement capability
                if (MovementPoints.Max <= 0)
                {
                    return false; // Immobile units (bases, etc.)
                }

                // Check if unit has movement points available
                if (MovementPoints.Current <= 0)
                {
                    return false; // No movement points left
                }

                // Check if target is different from current position
                if (Vector2.Distance(MapPos, targetPos) < 0.01f)
                {
                    return false; // Already at target position
                }

                // TODO: Add terrain validation, enemy ZOC checks, pathfinding, etc.
                // when those systems are implemented

                return true; // Basic validation passed
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CanMoveTo", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the distance between this unit and a target position in Unity units.
        /// </summary>
        /// <param name="targetPos">The target position</param>
        /// <returns>Distance in Unity units</returns>
        public float GetDistanceTo(Vector2 targetPos)
        {
            try
            {
                return Vector2.Distance(MapPos, targetPos);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetDistanceTo", e);
                return float.MaxValue;
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
                if (otherUnit == null)
                {
                    throw new ArgumentNullException(nameof(otherUnit));
                }

                return GetDistanceTo(otherUnit.MapPos);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetDistanceTo", e);
                return float.MaxValue;
            }
        }

        /// <summary>
        /// Checks if the unit is at the specified position (within tolerance).
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="tolerance">Distance tolerance (default 0.01f)</param>
        /// <returns>True if unit is at the position</returns>
        public bool IsAtPosition(Vector2 position, float tolerance = 0.01f)
        {
            try
            {
                return Vector2.Distance(MapPos, position) <= tolerance;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "IsAtPosition", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the unit's current map position.
        /// </summary>
        /// <returns>Current position on the map</returns>
        public Vector2 GetPosition()
        {
            return MapPos;
        }

        #endregion // Position and Movement Management


        #region Damage and Supply Systems

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
                float newHitPoints = Mathf.Max(0f, HitPoints.Current - damage);
                HitPoints.SetCurrent(newHitPoints);

                // Update unit profile to reflect current strength
                if (UnitProfile != null)
                {
                    UnitProfile.UpdateCurrentProfile((int)HitPoints.Current);
                }

                // Update efficiency level based on damage
                UpdateEfficiencyFromDamage();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "TakeDamage", e);
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

                // Update unit profile to reflect current strength
                if (UnitProfile != null)
                {
                    UnitProfile.UpdateCurrentProfile((int)HitPoints.Current);
                }

                // Update efficiency level based on new damage state
                UpdateEfficiencyFromDamage();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Repair", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "ConsumeSupplies", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "ReceiveSupplies", e);
                return 0f;
            }
        }

        /// <summary>
        /// Checks if the unit is destroyed (no hit points remaining).
        /// </summary>
        /// <returns>True if the unit is destroyed</returns>
        public bool IsDestroyed()
        {
            return HitPoints.Current <= 0f;
        }

        /// <summary>
        /// Checks if the unit can operate effectively (has minimum supplies and hit points).
        /// </summary>
        /// <returns>True if the unit can perform operations</returns>
        public bool CanOperate()
        {
            try
            {
                // Must have hit points to operate
                if (IsDestroyed())
                {
                    return false;
                }

                // Must have some supplies for most operations
                // Allow emergency operations with very low supplies
                if (DaysSupply.Current < 0.1f)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CanOperate", e);
                return false;
            }
        }

        /// <summary>
        /// Gets the combat effectiveness as a percentage based on current hit points.
        /// </summary>
        /// <returns>Combat effectiveness from 0.0 to 1.0</returns>
        public float GetCombatEffectiveness()
        {
            try
            {
                if (HitPoints.Max <= 0f)
                {
                    return 0f;
                }

                return HitPoints.Current / HitPoints.Max;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetCombatEffectiveness", e);
                return 0f;
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
                AppService.Instance.HandleException(CLASS_NAME, "GetSupplyStatus", e);
                return 0f;
            }
        }

        /// <summary>
        /// Checks if the unit has adequate supplies for operations.
        /// </summary>
        /// <param name="threshold">Supply threshold percentage (default 0.25 = 25%)</param>
        /// <returns>True if supplies are above threshold</returns>
        public bool HasAdequateSupplies(float threshold = 0.25f)
        {
            return GetSupplyStatus() >= threshold;
        }

        /// <summary>
        /// Updates efficiency level based on current damage state.
        /// </summary>
        private void UpdateEfficiencyFromDamage()
        {
            try
            {
                float effectiveness = GetCombatEffectiveness();

                // Use reflection to set EfficiencyLevel since it has private setter
                var unitType = typeof(CombatUnit);
                var efficiencyProperty = unitType.GetProperty("EfficiencyLevel");

                EfficiencyLevel newEfficiency = effectiveness switch
                {
                    >= 0.9f => EfficiencyLevel.FullyOperational,
                    >= 0.7f => EfficiencyLevel.Operational,
                    >= 0.4f => EfficiencyLevel.DegradedOperations,
                    > 0f => EfficiencyLevel.StaticOperations,
                    _ => EfficiencyLevel.StaticOperations
                };

                efficiencyProperty?.SetValue(this, newEfficiency);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpdateEfficiencyFromDamage", e);
            }
        }

        #endregion // Damage and Supply Systems


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
                    this.DeployedProfile,      // Shared reference
                    this.MountedProfile,       // Shared reference  
                    this.UnitProfile,          // Shared reference
                    this.IsTransportable,
                    this.IsLandBase,
                    this.LandBaseFacility      // Shared reference
                );

                // Deep copy all StatsMaxCurrent objects by reconstructing them
                // This overwrites the default values set by the constructor
                clone.HitPoints = new StatsMaxCurrent(this.HitPoints.Max, this.HitPoints.Current);
                clone.DaysSupply = new StatsMaxCurrent(this.DaysSupply.Max, this.DaysSupply.Current);
                clone.MovementPoints = new StatsMaxCurrent(this.MovementPoints.Max, this.MovementPoints.Current);
                clone.MoveActions = new StatsMaxCurrent(this.MoveActions.Max, this.MoveActions.Current);
                clone.CombatActions = new StatsMaxCurrent(this.CombatActions.Max, this.CombatActions.Current);
                clone.DeploymentActions = new StatsMaxCurrent(this.DeploymentActions.Max, this.DeploymentActions.Current);
                clone.OpportunityActions = new StatsMaxCurrent(this.OpportunityActions.Max, this.OpportunityActions.Current);
                clone.IntelActions = new StatsMaxCurrent(this.IntelActions.Max, this.IntelActions.Current);

                // Copy per-unit state data
                clone.SetExperience(this.ExperiencePoints); // This also sets ExperienceLevel correctly

                // Copy properties with private setters using reflection
                var cloneType = typeof(CombatUnit);

                // Copy CommandingOfficer (shared reference)
                cloneType.GetProperty("CommandingOfficer")
                    ?.SetValue(clone, this.CommandingOfficer);

                // Copy state properties
                cloneType.GetProperty("EfficiencyLevel")
                    ?.SetValue(clone, this.EfficiencyLevel);
                cloneType.GetProperty("IsMounted")
                    ?.SetValue(clone, this.IsMounted);
                cloneType.GetProperty("CombatState")
                    ?.SetValue(clone, this.CombatState);
                cloneType.GetProperty("MapPos")
                    ?.SetValue(clone, this.MapPos);

                // Copy unresolved reference fields (should be empty in normal cloning scenarios)
                cloneType.GetField("unresolvedDeployedProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedDeployedProfileID);
                cloneType.GetField("unresolvedMountedProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedMountedProfileID);
                cloneType.GetField("unresolvedUnitProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedUnitProfileID);
                cloneType.GetField("unresolvedLandBaseProfileID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedLandBaseProfileID);
                cloneType.GetField("unresolvedLeaderID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(clone, this.unresolvedLeaderID);

                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion // ICloneable Implementation


        #region ISerializable Implementation

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
                info.AddValue(nameof(IsLandBase), IsLandBase);

                // Serialize profile references as IDs/names (not the objects themselves)
                info.AddValue("DeployedProfileID", DeployedProfile?.WeaponSystemID ?? "");
                info.AddValue("MountedProfileID", MountedProfile?.WeaponSystemID ?? "");
                info.AddValue("UnitProfileID", UnitProfile?.UnitProfileID ?? "");
                info.AddValue("LandBaseFacilityID", LandBaseFacility?.BaseID ?? "");
                info.AddValue("LeaderID", CommandingOfficer?.LeaderID ?? "");

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
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, nameof(GetObjectData), e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region Serialization Support Methods

        /// <summary>
        /// Gets the list of unresolved unit IDs from deserialization.
        /// Used to check if unit references need to be resolved.
        /// </summary>
        public IReadOnlyList<string> GetUnresolvedUnitIDs()
        {
            var unresolvedIDs = new List<string>();

            // Currently CombatUnit doesn't store references to other CombatUnits
            // This method exists for interface consistency and future expansion

            return unresolvedIDs.AsReadOnly();
        }

        /// <summary>
        /// Resolves unit references after deserialization.
        /// Called by game state manager with reconstructed unit objects.
        /// </summary>
        /// <param name="unitLookup">Dictionary mapping unit IDs to CombatUnit instances</param>
        public void ResolveUnitReferences(Dictionary<string, CombatUnit> unitLookup)
        {
            try
            {
                // Currently CombatUnit doesn't store references to other CombatUnits
                // This method exists for interface consistency and future expansion
                throw new NotImplementedException("CombatUnit unit reference resolution not yet implemented");
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ResolveUnitReferences", e);
                throw;
            }
        }

        /// <summary>
        /// Resolves profile references after deserialization.
        /// Called by game state manager with reconstructed profile objects.
        /// </summary>
        /// <param name="weaponProfileLookup">Dictionary mapping weapon system IDs to WeaponSystemProfile instances</param>
        /// <param name="unitProfileLookup">Dictionary mapping unit profile IDs to UnitProfile instances</param>
        /// <param name="landBaseLookup">Dictionary mapping land base IDs to LandBaseFacility instances</param>
        public void ResolveProfileReferences(
            Dictionary<string, WeaponSystemProfile> weaponProfileLookup,
            Dictionary<string, UnitProfile> unitProfileLookup,
            Dictionary<string, LandBaseFacility> landBaseLookup)
        {
            try
            {
                // Resolve WeaponSystemProfile references
                if (!string.IsNullOrEmpty(unresolvedDeployedProfileID))
                {
                    if (weaponProfileLookup.TryGetValue(unresolvedDeployedProfileID, out WeaponSystemProfile deployedProfile))
                    {
                        DeployedProfile = deployedProfile;
                        unresolvedDeployedProfileID = "";
                    }
                    else
                    {
                        AppService.Instance.HandleException(CLASS_NAME, "ResolveProfileReferences",
                            new KeyNotFoundException($"Deployed profile {unresolvedDeployedProfileID} not found in lookup dictionary"));
                    }
                }

                if (!string.IsNullOrEmpty(unresolvedMountedProfileID))
                {
                    if (weaponProfileLookup.TryGetValue(unresolvedMountedProfileID, out WeaponSystemProfile mountedProfile))
                    {
                        MountedProfile = mountedProfile;
                        unresolvedMountedProfileID = "";
                    }
                    else
                    {
                        AppService.Instance.HandleException(CLASS_NAME, "ResolveProfileReferences",
                            new KeyNotFoundException($"Mounted profile {unresolvedMountedProfileID} not found in lookup dictionary"));
                    }
                }

                // Resolve UnitProfile reference
                if (!string.IsNullOrEmpty(unresolvedUnitProfileID))
                {
                    if (unitProfileLookup.TryGetValue(unresolvedUnitProfileID, out UnitProfile unitProfile))
                    {
                        UnitProfile = unitProfile;
                        unresolvedUnitProfileID = "";
                    }
                    else
                    {
                        AppService.Instance.HandleException(CLASS_NAME, "ResolveProfileReferences",
                            new KeyNotFoundException($"Unit profile {unresolvedUnitProfileID} not found in lookup dictionary"));
                    }
                }

                // Resolve LandBaseFacility reference
                if (!string.IsNullOrEmpty(unresolvedLandBaseProfileID))
                {
                    if (landBaseLookup.TryGetValue(unresolvedLandBaseProfileID, out LandBaseFacility landBase))
                    {
                        LandBaseFacility = landBase;
                        unresolvedLandBaseProfileID = "";
                    }
                    else
                    {
                        AppService.Instance.HandleException(CLASS_NAME, "ResolveProfileReferences",
                            new KeyNotFoundException($"Land base profile {unresolvedLandBaseProfileID} not found in lookup dictionary"));
                    }
                }

                throw new NotImplementedException("CombatUnit profile reference resolution not yet implemented");
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ResolveProfileReferences", e);
                throw;
            }
        }

        /// <summary>
        /// Resolves leader references after deserialization.
        /// Called by game state manager with reconstructed leader objects.
        /// </summary>
        /// <param name="leaderLookup">Dictionary mapping leader IDs to Leader instances</param>
        public void ResolveLeaderReferences(Dictionary<string, Leader> leaderLookup)
        {
            try
            {
                if (!string.IsNullOrEmpty(unresolvedLeaderID))
                {
                    if (leaderLookup.TryGetValue(unresolvedLeaderID, out Leader leader))
                    {
                        CommandingOfficer = leader;
                        unresolvedLeaderID = "";
                    }
                    else
                    {
                        AppService.Instance.HandleException(CLASS_NAME, "ResolveLeaderReferences",
                            new KeyNotFoundException($"Leader {unresolvedLeaderID} not found in lookup dictionary"));
                    }
                }

                throw new NotImplementedException("CombatUnit leader reference resolution not yet implemented");
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ResolveLeaderReferences", e);
                throw;
            }
        }

        /// <summary>
        /// Checks if there are unresolved references that need to be resolved.
        /// </summary>
        /// <returns>True if any resolution methods need to be called</returns>
        public bool HasUnresolvedReferences()
        {
            return !string.IsNullOrEmpty(unresolvedDeployedProfileID) ||
                   !string.IsNullOrEmpty(unresolvedMountedProfileID) ||
                   !string.IsNullOrEmpty(unresolvedUnitProfileID) ||
                   !string.IsNullOrEmpty(unresolvedLandBaseProfileID) ||
                   !string.IsNullOrEmpty(unresolvedLeaderID);
        }

        /// <summary>
        /// Gets the list of profile IDs that need resolution for external reference tracking.
        /// Used by game state manager to track dependencies.
        /// </summary>
        public Dictionary<string, string> GetUnresolvedProfileIDs()
        {
            var unresolvedProfiles = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(unresolvedDeployedProfileID))
                unresolvedProfiles["DeployedProfile"] = unresolvedDeployedProfileID;

            if (!string.IsNullOrEmpty(unresolvedMountedProfileID))
                unresolvedProfiles["MountedProfile"] = unresolvedMountedProfileID;

            if (!string.IsNullOrEmpty(unresolvedUnitProfileID))
                unresolvedProfiles["UnitProfile"] = unresolvedUnitProfileID;

            if (!string.IsNullOrEmpty(unresolvedLandBaseProfileID))
                unresolvedProfiles["LandBaseFacility"] = unresolvedLandBaseProfileID;

            return unresolvedProfiles;
        }

        /// <summary>
        /// Gets the unresolved leader ID for external reference tracking.
        /// </summary>
        public string GetUnresolvedLeaderID()
        {
            return unresolvedLeaderID;
        }

        #endregion // Serialization Support Methods
    }
}