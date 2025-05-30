using System;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Services;

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
        public LandBaseProfile LandBaseProfile { get; private set; }

        // The unit's leader.
        public Leader CommandingOfficer { get; private set; }

        // Action counts using StatsMaxCurrent
        public StatsMaxCurrent MoveActions { get; private set; }
        public StatsMaxCurrent CombatActions { get; private set; }
        public StatsMaxCurrent DeploymentActions { get; private set; }
        public StatsMaxCurrent OpportunityActions { get; private set; }
        public StatsMaxCurrent IntelGatheringActions { get; private set; }

        // State data using StatsMaxCurrent where appropriate
        public int ExperiencePoints { get; private set; }
        public ExperienceLevel _ExperienceLevel { get; private set; }
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
        /// <param name="unitID">Unique identifier for the unit</param>
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
            string unitID,
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
            LandBaseProfile landBaseProfile = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(unitName))
                    throw new ArgumentException("Unit name cannot be null or empty", nameof(unitName));

                if (string.IsNullOrEmpty(unitID))
                    throw new ArgumentException("Unit ID cannot be null or empty", nameof(unitID));

                if (deployedProfile == null)
                    throw new ArgumentNullException(nameof(deployedProfile), "Deployed profile is required");

                if (unitProfile == null)
                    throw new ArgumentNullException(nameof(unitProfile), "Unit profile is required");

                // Validate land base requirements
                if (isLandBase && landBaseProfile == null)
                    throw new ArgumentException("Land base profile is required when isLandBase is true", nameof(landBaseProfile));

                // Set identification and metadata
                UnitName = unitName;
                UnitID = unitID;
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
                LandBaseProfile = landBaseProfile;

                // Initialize leader (will be null until assigned)
                CommandingOfficer = null;

                // Initialize action counts based on unit type and classification
                InitializeActionCounts();

                // Initialize state with default values
                ExperiencePoints = 0;
                _ExperienceLevel = ExperienceLevel.Raw;
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

        // Deserialization constructor.
        protected CombatUnit(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Implement deserialization logic here
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
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
            IntelGatheringActions = new StatsMaxCurrent(intelActions);
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

                var previousLevel = _ExperienceLevel;
                ExperiencePoints += points;

                // Check if we've advanced to a new level
                var newLevel = CalculateExperienceLevel(ExperiencePoints);
                if (newLevel != previousLevel)
                {
                    _ExperienceLevel = newLevel;
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
                _ExperienceLevel = CalculateExperienceLevel(points);
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
            return _ExperienceLevel switch
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
                return $"{_ExperienceLevel} ({ExperiencePoints} XP, {pointsToNext} to next)";
            }
            else
            {
                return $"{_ExperienceLevel} (Max Level - {ExperiencePoints} XP)";
            }
        }

        /// <summary>
        /// Gets the experience progress as a percentage towards the next level (0.0 to 1.0).
        /// Returns 1.0 if at maximum level.
        /// </summary>
        /// <returns>Progress percentage towards next level</returns>
        public float GetExperienceProgress()
        {
            if (_ExperienceLevel == ExperienceLevel.Elite)
                return 1.0f;

            int currentLevelMin = GetMinPointsForLevel(_ExperienceLevel);
            int nextLevelMin = GetMinPointsForLevel(GetNextLevel(_ExperienceLevel));

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
            return _ExperienceLevel == ExperienceLevel.Veteran || _ExperienceLevel == ExperienceLevel.Elite;
        }

        /// <summary>
        /// Checks if the unit is considered elite level.
        /// </summary>
        /// <returns>True if unit is Elite</returns>
        public bool IsElite()
        {
            return _ExperienceLevel == ExperienceLevel.Elite;
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
            return _ExperienceLevel switch
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
            IntelGatheringActions.ResetToMax();
        }

        /// <summary>
        /// Refreshes movement points to maximum.
        /// Called at the start of each turn.
        /// </summary>
        public void RefreshMovementPoints()
        {
            MovementPoints.ResetToMax();
        }

        #endregion

        #region ICloneable Implementation

        public object Clone()
        {
            try
            {
                return null; // Implement deep copy logic here
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion

        #region ISerializable Implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Implement serialization logic here
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion
    }
}