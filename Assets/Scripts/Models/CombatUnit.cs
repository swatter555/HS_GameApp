using System;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Defines the principal domains of unit operation: ground, air, or naval.
    /// </summary>
    public enum UnitType
    {
        LandUnitDF, // Land unit with direct fire capability
        LandUnitIF, // Land unit with indirect fire capability
        AirUnit,    // Air unit
        NavalUnitDF, // Naval unit with direct fire capability
        NavalUnitIF, // Naval unit with indirect fire capability
    }

    /// <summary>
    /// Descriptive categories that capture a unit's broader organizational or functional role.
    /// </summary>
    public enum UnitClassification
    {
        TANK,   // Tank
        MECH,   // Mechanized
        MOT,    // Motorized
        AB,     // Airborne
        MAB,    // Mechanized Airborne
        MAR,    // Marine
        MMAR,   // Mechanized Marine
        RECON,  // Reconnaissance
        AT,     // Anti-tank
        AM,     // Air Mobile
        MAM,    // Mechanized Air Mobile
        INF,    // Infantry
        SPECF,  // Special Forces Foot
        SPECM,  // Special Forces Mechanized
        SPECH,  // Special Forces Helicopter
        ART,    // Artillery
        SPA,    // Self-Propelled Artillery
        ROC,    // Rocket Artillery
        BM,     // Ballistic Missile
        SAM,    // Surface-to-Air Missile
        SPSAM,  // Self Proplled SAM
        AAA,    // Anti-Aircraft Artillery
        SPAAA,  // Self-Propelled Anti-Aircraft Artillery
        ENG,    // Engineer
        AHEL,   // Transport Helicopter
        THEL,   // Attack helicopter
        ASF,    // Air Superiority Fighter
        MRF,    // Multi-role Fighter
        ATT,    // Attack aircraft
        BMB,    // Bomber
        RCN,    // Recon Aircraft
        FWT,    // Fixed-wing transport
        FAC,    // Facility
        AIRB,   // Airbase
    }

    /// <summary>
    /// High-level roles that dictate a unit's primary in-game function and AI behavior.
    /// </summary>
    public enum UnitRole
    {
        GroundCombat,
        GroundCombatIndirect,
        GroundCombatStatic,    // Immobile (facility or airbase typically)
        GroundCombatRecon,
        AirDefenseArea,
        AirDefensePoint,
        AirSuperiority,
        AirMultirole,
        AirGroundAttack,
        AirStrategicAttack,
        AirRecon
    }

    /// <summary>
    /// Indicates whether the unit is controlled by the player or AI.
    /// </summary>
    public enum Side
    {
        Player,
        AI
    }

    /// <summary>
    /// National affiliation used for flag assets and faction logic.
    /// </summary>
    public enum Nationality
    {
        USSR,
        USA,
        FRG,
        UK,
        FRA,
        MJ,
        IR,
        IQ,
        SAUD
    }

    /// <summary>
    /// States representing whether a unit is riding in transport or deployed in various defensive postures.
    /// </summary>
    public enum CombatState
    {
        Mobile,        // Mounted on transport
        Deployed,      // Standard dismounted posture
        HastyDefense,  // Quick entrenchment, moderate defense boost
        Entrenched,    // Prepared defensive positions, stronger defense
        Fortified      // Maximum defensive preparations, highest defense
    }

    /// <summary>
    /// The unit's special movement capabilities.
    /// </summary>
    public enum StrategicMobility
    {
        Heavy,
        AirDrop,
        AirMobile,
        AirLift,
        Amphibious
    }

    /// <summary>
    /// The unit's special movement capabilities.
    /// </summary>
    public enum UnitCapability
    {
        NotApplicable,
        Low,
        Moderate,
        High
    }

    /// <summary>
    /// The experience level of a unit.
    /// </summary>
    public enum ExperienceLevel
    {
        Raw,
        Green,
        Trained,
        Experienced,
        Veteran,
        Elite
    }

    /// <summary>
    /// The experience points required to attain given proficiency level.
    /// </summary>
    public enum ExperiencePointLevels
    {
        Raw = 0,
        Green = 5,
        Trained = 12,
        Experienced = 22,
        Veteran = 33,
        Elite = 40
    }

    /// <summary>
    /// How ready is the unit for combat.
    /// </summary>
    public enum EfficiencyLevel
    {
        StaticOperations,
        DegradedOperations,
        Operational,
        FullyOperational,
        PeakOperational
    }

    /// <summary>
    /// How stealthy a unit is.
    /// </summary>
    public enum VisibilityProfile
    {
        Large,
        Medium,
        Small,
        Tiny
    }

    ///// <summary>
    ///// Represents a military unit with identification, base stats, and optional transport mounting.
    ///// </summary>
    //public class CombatUnit
    //{
    //    #region Constants

    //    /// <summary>
    //    /// The maximum possible hit points a unit can have, used as the baseline for calculating
    //    /// strength multipliers.
    //    /// </summary>
    //    public const int MaxPossibleHitPoints = 40;

    //    #endregion

    //    // Identification and metadata
    //    public string UnitName { get; set; }
    //    public UnitType UnitType { get; private set; }
    //    public UnitClassification Classification { get; private set; }
    //    public UnitRole Role { get; private set; }
    //    public Side Side { get; private set; }
    //    public Nationality Nationality { get; private set; }
    //    public bool IsTransportable { get; private set; }

    //    // Action counts per turn
    //    public int MoveActions { get; private set; }
    //    public int CombatActions { get; private set; }
    //    public int SpecialActions { get; private set; }

    //    // Combat profiles
    //    public WeaponSystemProfile DeployedProfile { get; private set; }
    //    public WeaponSystemProfile MountedProfile { get; private set; }

    //    // Informational profile
    //    public UnitProfile UnitProfile { get; private set; }

    //    // Is this unit currently mounted on a transport?
    //    public bool IsMounted { get; private set; }

    //    // Experience
    //    public int ExperiencePoints { get; private set; }
    //    public ExperienceLevel ExperienceLevel { get; private set; }

    //    // Combat readiness state gauge
    //    public EfficiencyLevel EfficiencyLevel { get; private set; }

    //    // Signals intelligence rating
    //    public UnitCapability SIGINT_Rating { get; private set; }

    //    // Nuclear, Biological, and Chemical (NBC) rating
    //    public UnitCapability NBC_Rating { get; private set; }

    //    // Special movement abilities
    //    public StrategicMobility StrategicMobility { get; private set; }

    //    // Night fighting capability (typically ground and aviation units)
    //    public UnitCapability NightFighting { get; private set; }

    //    // All weather capability (aircraft only)
    //    public UnitCapability AllWeather { get; private set; }

    //    // Visibility profile
    //    public VisibilityProfile VisibilityProfile { get; private set; }

    //    // Hit points
    //    public const int MaxHitPoints = 40;
    //    private int hitPoints;
    //    public int HitPoints => hitPoints;

    //    // Days of supply.
    //    public const float MaxDaysSupplyDepot = 100f;
    //    public const float MaxDaysSupplyUnit = 7f;
    //    public float CurrentDaysSupply = 5f;

    //    // Movement points
    //    public int MaxMovementPoints { get; private set; }
    //    public int CurrentMovementPoints { get; private set; }

    //    // Zone of control in hexes
    //    public int ZOC { get; private set; }

    //    // x,y location on map
    //    public Vector2 Position { get; private set; }

    //    // TODO: Unit Commander
    //    // TODO: Airbase structure
    //    // TODO: Facility structure



    //    // Constructor and methods will be added in subsequent iterations.
    //}


    /// <summary>
    /// Represents a military unit with identification, base stats, and optional transport mounting.
    /// Implements an event-driven design pattern for state changes.
    /// </summary>
    [Serializable]
    public class CombatUnit : ICloneable, ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        /// <summary>
        /// The maximum possible hit points a unit can have, used as the baseline for calculating
        /// strength multipliers.
        /// </summary>
        public const int MaxPossibleHitPoints = 40;

        // Movement constants for different unit types
        private const int MechanizedMovt = 12;
        private const int MotorizedMovt = 10;
        private const int NonMechanizedMovt = 8;
        private const int AirMovt = 100;
        private const int AviationMovt = 24;

        #endregion

        #region Events and Delegates

        /// <summary>
        /// Triggered when the unit's hit points change.
        /// </summary>
        public event Action<int, int> OnHitPointsChanged; // oldValue, newValue

        /// <summary>
        /// Triggered when the unit's position changes.
        /// </summary>
        public event Action<Vector2, Vector2> OnPositionChanged; // oldPosition, newPosition

        /// <summary>
        /// Triggered when the unit's movement points change.
        /// </summary>
        public event Action<int, int> OnMovementPointsChanged; // oldValue, newValue

        /// <summary>
        /// Triggered when the unit's experience level changes.
        /// </summary>
        public event Action<ExperienceLevel, ExperienceLevel> OnExperienceChanged; // oldLevel, newLevel

        /// <summary>
        /// Triggered when the unit's combat state changes.
        /// </summary>
        public event Action<CombatState, CombatState> OnCombatStateChanged; // oldState, newState

        /// <summary>
        /// Triggered when the unit's days of supply change.
        /// </summary>
        public event Action<float, float> OnSupplyChanged; // oldValue, newValue

        /// <summary>
        /// Triggered when the unit's efficiency level changes.
        /// </summary>
        public event Action<EfficiencyLevel, EfficiencyLevel> OnEfficiencyChanged; // oldLevel, newLevel

        /// <summary>
        /// Triggered when the unit's mounted state changes.
        /// </summary>
        public event Action<bool> OnMountedStateChanged; // isMounted

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

        // Action counts per turn
        public int MoveActions { get; private set; }
        public int CombatActions { get; private set; }
        public int SpecialActions { get; private set; }

        // Combat profiles
        public WeaponSystemProfile DeployedProfile { get; private set; }
        public WeaponSystemProfile MountedProfile { get; private set; }

        // Informational profile
        public UnitProfile UnitProfile { get; private set; }

        // Experience
        private int experiencePoints;
        public int ExperiencePoints
        {
            get => experiencePoints;
            private set
            {
                if (experiencePoints != value)
                {
                    experiencePoints = value;
                    UpdateExperienceLevel();
                }
            }
        }

        private ExperienceLevel experienceLevel;
        public ExperienceLevel ExperienceLevel
        {
            get => experienceLevel;
            private set
            {
                if (experienceLevel != value)
                {
                    var oldLevel = experienceLevel;
                    experienceLevel = value;
                    OnExperienceChanged?.Invoke(oldLevel, experienceLevel);
                }
            }
        }

        // Combat readiness state gauge
        private EfficiencyLevel efficiencyLevel;
        public EfficiencyLevel EfficiencyLevel
        {
            get => efficiencyLevel;
            private set
            {
                if (efficiencyLevel != value)
                {
                    var oldLevel = efficiencyLevel;
                    efficiencyLevel = value;
                    OnEfficiencyChanged?.Invoke(oldLevel, efficiencyLevel);
                }
            }
        }

        // Capability ratings
        public UnitCapability SIGINT_Rating { get; private set; }
        public UnitCapability NBC_Rating { get; private set; }
        public StrategicMobility StrategicMobility { get; private set; }
        public UnitCapability NightFighting { get; private set; }
        public UnitCapability AllWeather { get; private set; }
        public VisibilityProfile VisibilityProfile { get; private set; }

        // Is this unit currently mounted on a transport?
        private bool isMounted;
        public bool IsMounted
        {
            get => isMounted;
            private set
            {
                if (isMounted != value)
                {
                    isMounted = value;
                    OnMountedStateChanged?.Invoke(isMounted);
                }
            }
        }

        // Combat state
        private CombatState combatState;
        public CombatState CombatState
        {
            get => combatState;
            private set
            {
                if (combatState != value)
                {
                    var oldState = combatState;
                    combatState = value;
                    OnCombatStateChanged?.Invoke(oldState, combatState);
                }
            }
        }

        // Hit points
        public const int MaxHitPoints = 40;
        private int hitPoints;
        public int HitPoints
        {
            get => hitPoints;
            private set
            {
                if (hitPoints != value)
                {
                    int oldValue = hitPoints;
                    hitPoints = Math.Clamp(value, 0, MaxHitPoints);
                    OnHitPointsChanged?.Invoke(oldValue, hitPoints);

                    // Update unit profile representation
                    UnitProfile?.UpdateCurrentProfile(hitPoints);
                }
            }
        }

        // Supply
        public const float MaxDaysSupplyDepot = 100f;
        public const float MaxDaysSupplyUnit = 7f;
        private float currentDaysSupply = 5f;
        public float CurrentDaysSupply
        {
            get => currentDaysSupply;
            set
            {
                if (Math.Abs(currentDaysSupply - value) > 0.01f)
                {
                    float oldValue = currentDaysSupply;
                    currentDaysSupply = Math.Clamp(value, 0f, MaxDaysSupplyUnit);
                    OnSupplyChanged?.Invoke(oldValue, currentDaysSupply);

                    // Check if supply dropped to zero, which affects efficiency
                    if (oldValue > 0 && currentDaysSupply <= 0)
                    {
                        SupplyEfficiencyCheck();
                    }
                }
            }
        }

        // Movement
        public int MaxMovementPoints { get; private set; }
        private int currentMovementPoints;
        public int CurrentMovementPoints
        {
            get => currentMovementPoints;
            private set
            {
                if (currentMovementPoints != value)
                {
                    int oldValue = currentMovementPoints;
                    currentMovementPoints = Math.Clamp(value, 0, MaxMovementPoints);
                    OnMovementPointsChanged?.Invoke(oldValue, currentMovementPoints);
                }
            }
        }

        // Zone of control in hexes
        public int ZOC { get; private set; }

        // Position on map
        private Vector2 position;
        public Vector2 Position
        {
            get => position;
            private set
            {
                if (position != value)
                {
                    Vector2 oldPosition = position;
                    position = value;
                    OnPositionChanged?.Invoke(oldPosition, position);
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new combat unit with the specified parameters.
        /// </summary>
        public CombatUnit(
            string unitName,
            UnitType unitType,
            UnitClassification classification,
            UnitRole role,
            Side side,
            Nationality nationality,
            bool isTransportable,
            WeaponSystemProfile deployedProfile,
            WeaponSystemProfile mountedProfile = null,
            Vector2 initialPosition = default)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(unitName))
                    throw new ArgumentException("Unit name cannot be null or empty", nameof(unitName));

                if (deployedProfile == null)
                    throw new ArgumentNullException(nameof(deployedProfile), "Deployed profile cannot be null");

                if (isTransportable && mountedProfile == null)
                    throw new ArgumentException("Mounted profile must be provided for transportable units", nameof(mountedProfile));

                // Set basic properties
                UnitName = unitName;
                UnitID = Guid.NewGuid().ToString();
                UnitType = unitType;
                Classification = classification;
                Role = role;
                Side = side;
                Nationality = nationality;
                IsTransportable = isTransportable;
                DeployedProfile = deployedProfile;
                MountedProfile = mountedProfile;
                Position = initialPosition;

                // Set default action counts
                MoveActions = 1;
                CombatActions = 1;
                SpecialActions = 0;

                // Initialize state
                hitPoints = MaxHitPoints;
                currentDaysSupply = 5f;
                isMounted = false;
                combatState = CombatState.Deployed;
                experiencePoints = 0;
                experienceLevel = ExperienceLevel.Green;
                efficiencyLevel = EfficiencyLevel.Operational;

                // Default capabilities
                SIGINT_Rating = UnitCapability.Low;
                NBC_Rating = UnitCapability.Low;
                StrategicMobility = StrategicMobility.Heavy;
                NightFighting = UnitCapability.Low;
                AllWeather = UnitCapability.NotApplicable;
                VisibilityProfile = VisibilityProfile.Medium;

                // Set movement points based on profile
                MaxMovementPoints = CalculateMaxMovementPoints();
                CurrentMovementPoints = MaxMovementPoints;

                // Set ZOC based on profile and type
                ZOC = CalculateZOC();

                // Create a unit profile
                UnitProfile = new UnitProfile(unitName, nationality);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected CombatUnit(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Retrieve basic properties
                UnitName = info.GetString(nameof(UnitName));
                UnitID = info.GetString(nameof(UnitID));
                UnitType = (UnitType)info.GetValue(nameof(UnitType), typeof(UnitType));
                Classification = (UnitClassification)info.GetValue(nameof(Classification), typeof(UnitClassification));
                Role = (UnitRole)info.GetValue(nameof(Role), typeof(UnitRole));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                IsTransportable = info.GetBoolean(nameof(IsTransportable));

                // Retrieve profiles
                DeployedProfile = (WeaponSystemProfile)info.GetValue(nameof(DeployedProfile), typeof(WeaponSystemProfile));
                MountedProfile = (WeaponSystemProfile)info.GetValue(nameof(MountedProfile), typeof(WeaponSystemProfile));
                UnitProfile = (UnitProfile)info.GetValue(nameof(UnitProfile), typeof(UnitProfile));

                // Retrieve action counts
                MoveActions = info.GetInt32(nameof(MoveActions));
                CombatActions = info.GetInt32(nameof(CombatActions));
                SpecialActions = info.GetInt32(nameof(SpecialActions));

                // Retrieve state
                hitPoints = info.GetInt32(nameof(HitPoints));
                currentDaysSupply = info.GetSingle(nameof(CurrentDaysSupply));
                isMounted = info.GetBoolean(nameof(IsMounted));
                combatState = (CombatState)info.GetValue(nameof(CombatState), typeof(CombatState));
                experiencePoints = info.GetInt32(nameof(ExperiencePoints));
                experienceLevel = (ExperienceLevel)info.GetValue(nameof(ExperienceLevel), typeof(ExperienceLevel));
                efficiencyLevel = (EfficiencyLevel)info.GetValue(nameof(EfficiencyLevel), typeof(EfficiencyLevel));

                // Retrieve capabilities
                SIGINT_Rating = (UnitCapability)info.GetValue(nameof(SIGINT_Rating), typeof(UnitCapability));
                NBC_Rating = (UnitCapability)info.GetValue(nameof(NBC_Rating), typeof(UnitCapability));
                StrategicMobility = (StrategicMobility)info.GetValue(nameof(StrategicMobility), typeof(StrategicMobility));
                NightFighting = (UnitCapability)info.GetValue(nameof(NightFighting), typeof(UnitCapability));
                AllWeather = (UnitCapability)info.GetValue(nameof(AllWeather), typeof(UnitCapability));
                VisibilityProfile = (VisibilityProfile)info.GetValue(nameof(VisibilityProfile), typeof(VisibilityProfile));

                // Retrieve movement and position
                MaxMovementPoints = info.GetInt32(nameof(MaxMovementPoints));
                currentMovementPoints = info.GetInt32(nameof(CurrentMovementPoints));
                ZOC = info.GetInt32(nameof(ZOC));
                float posX = info.GetSingle("PositionX");
                float posY = info.GetSingle("PositionY");
                position = new Vector2(posX, posY);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion

        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this combat unit.
        /// </summary>
        public object Clone()
        {
            try
            {
                // Create a new unit with the same basic parameters
                var clone = new CombatUnit(
                    UnitName,
                    UnitType,
                    Classification,
                    Role,
                    Side,
                    Nationality,
                    IsTransportable,
                    DeployedProfile.Clone(),
                    MountedProfile?.Clone(),
                    Position
                );

                // Copy state values
                clone.hitPoints = hitPoints;
                clone.currentDaysSupply = currentDaysSupply;
                clone.isMounted = isMounted;
                clone.combatState = combatState;
                clone.experiencePoints = experiencePoints;
                clone.experienceLevel = experienceLevel;
                clone.efficiencyLevel = efficiencyLevel;

                // Copy capabilities
                clone.SIGINT_Rating = SIGINT_Rating;
                clone.NBC_Rating = NBC_Rating;
                clone.StrategicMobility = StrategicMobility;
                clone.NightFighting = NightFighting;
                clone.AllWeather = AllWeather;
                clone.VisibilityProfile = VisibilityProfile;

                // Copy movement and position
                clone.MaxMovementPoints = MaxMovementPoints;
                clone.currentMovementPoints = currentMovementPoints;
                clone.ZOC = ZOC;

                // Copy unit profile if it exists
                if (UnitProfile != null)
                {
                    clone.UnitProfile = UnitProfile.Clone();
                }

                // Generate a new unique ID
                clone.UnitID = Guid.NewGuid().ToString();

                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Serializes this combat unit.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Store basic properties
                info.AddValue(nameof(UnitName), UnitName);
                info.AddValue(nameof(UnitID), UnitID);
                info.AddValue(nameof(UnitType), UnitType);
                info.AddValue(nameof(Classification), Classification);
                info.AddValue(nameof(Role), Role);
                info.AddValue(nameof(Side), Side);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(IsTransportable), IsTransportable);

                // Store profiles
                info.AddValue(nameof(DeployedProfile), DeployedProfile);
                info.AddValue(nameof(MountedProfile), MountedProfile);
                info.AddValue(nameof(UnitProfile), UnitProfile);

                // Store action counts
                info.AddValue(nameof(MoveActions), MoveActions);
                info.AddValue(nameof(CombatActions), CombatActions);
                info.AddValue(nameof(SpecialActions), SpecialActions);

                // Store state
                info.AddValue(nameof(HitPoints), hitPoints);
                info.AddValue(nameof(CurrentDaysSupply), currentDaysSupply);
                info.AddValue(nameof(IsMounted), isMounted);
                info.AddValue(nameof(CombatState), combatState);
                info.AddValue(nameof(ExperiencePoints), experiencePoints);
                info.AddValue(nameof(ExperienceLevel), experienceLevel);
                info.AddValue(nameof(EfficiencyLevel), efficiencyLevel);

                // Store capabilities
                info.AddValue(nameof(SIGINT_Rating), SIGINT_Rating);
                info.AddValue(nameof(NBC_Rating), NBC_Rating);
                info.AddValue(nameof(StrategicMobility), StrategicMobility);
                info.AddValue(nameof(NightFighting), NightFighting);
                info.AddValue(nameof(AllWeather), AllWeather);
                info.AddValue(nameof(VisibilityProfile), VisibilityProfile);

                // Store movement and position
                info.AddValue(nameof(MaxMovementPoints), MaxMovementPoints);
                info.AddValue(nameof(CurrentMovementPoints), currentMovementPoints);
                info.AddValue(nameof(ZOC), ZOC);
                info.AddValue("PositionX", position.x);
                info.AddValue("PositionY", position.y);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Takes the specified amount of damage, reducing hit points.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            HitPoints = Math.Max(0, HitPoints - amount);
        }

        /// <summary>
        /// Repairs the unit, restoring hit points while deducting experience proportionally.
        /// When a unit is repaired, it loses experience as veteran crews are diluted with replacements.
        /// </summary>
        /// <param name="amount">The amount of hit points to repair</param>
        public void Repair(int amount)
        {
            if (amount <= 0) return;

            int oldHitPoints = hitPoints;
            HitPoints = Math.Min(MaxHitPoints, hitPoints + amount);
            int actualRepairAmount = HitPoints - oldHitPoints;

            // Only deduct experience if repairs actually occurred
            if (actualRepairAmount > 0 && experiencePoints > 0)
            {
                // Calculate experience loss - proportional to repair amount and current experience
                float repairRatio = (float)actualRepairAmount / MaxHitPoints;
                int expDeduction = (int)Math.Ceiling(experiencePoints * repairRatio);

                // Ensure we don't deduct more than current experience points
                expDeduction = Math.Min(expDeduction, experiencePoints);

                // Apply the deduction
                ExperiencePoints = Math.Max(0, experiencePoints - expDeduction);
            }
        }

        /// <summary>
        /// Moves the unit to a new position and decreases movement points.
        /// </summary>
        public bool MoveTo(Vector2 newPosition, int movementCost)
        {
            if (movementCost > CurrentMovementPoints)
                return false;

            Position = newPosition;
            CurrentMovementPoints -= movementCost;
            return true;
        }

        /// <summary>
        /// Adds the specified amount of experience points to the unit.
        /// </summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            ExperiencePoints += amount;
        }

        /// <summary>
        /// Toggles the unit's mounted state if possible.
        /// </summary>
        public bool ToggleMountedState()
        {
            if (!IsTransportable || MountedProfile == null)
                return false;

            IsMounted = !IsMounted;
            return true;
        }

        /// <summary>
        /// Updates the unit's combat state.
        /// </summary>
        public bool SetCombatState(CombatState newState)
        {
            if (IsMounted && newState != CombatState.Mobile)
                return false;

            CombatState = newState;
            return true;
        }

        /// <summary>
        /// Resupplies the unit to the specified level.
        /// </summary>
        public void Resupply(float daysOfSupply)
        {
            if (daysOfSupply <= 0) return;
            CurrentDaysSupply = Math.Min(MaxDaysSupplyUnit, CurrentDaysSupply + daysOfSupply);
        }

        /// <summary>
        /// Resets movement points to maximum at the start of a turn.
        /// </summary>
        public void ResetMovementPoints()
        {
            CurrentMovementPoints = MaxMovementPoints;
        }

        /// <summary>
        /// Consumes supplies based on an action or time passing.
        /// </summary>
        public void ConsumeSupplies(float amount)
        {
            if (amount <= 0) return;
            CurrentDaysSupply = Math.Max(0, CurrentDaysSupply - amount);
        }

        /// <summary>
        /// Upgrades a specific capability or attribute of this unit.
        /// Allows for runtime modification of unit capabilities such as SIGINT rating,
        /// NBC protection, night fighting capability, and other key attributes.
        /// This facilitates unit progression throughout a campaign.
        /// </summary>
        /// <param name="capabilityName">The name of the capability property to modify</param>
        /// <param name="newValue">The new value to set for the capability</param>
        /// <returns>True if the upgrade was successful, false otherwise</returns>
        public bool UpgradeCapability(string capabilityName, object newValue)
        {
            try
            {
                var propertyInfo = GetType().GetProperty(capabilityName);
                if (propertyInfo == null || !propertyInfo.CanWrite)
                    return false;

                propertyInfo.SetValue(this, newValue);
                return true;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpgradeCapability", e);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the maximum movement points based on unit classification.
        /// </summary>
        private int CalculateMaxMovementPoints()
        {
            int baseMovement;

            // Base movement by unit classification
            switch (Classification)
            {
                // Mechanized units
                case UnitClassification.TANK:
                case UnitClassification.MECH:
                case UnitClassification.SPA:
                case UnitClassification.SPSAM:
                case UnitClassification.SPAAA:
                case UnitClassification.SPECM:
                case UnitClassification.MAB:
                case UnitClassification.MAM:
                case UnitClassification.MMAR:
                    baseMovement = MechanizedMovt;
                    break;

                // Motorized units
                case UnitClassification.MOT:
                case UnitClassification.AT:
                case UnitClassification.ROC:
                case UnitClassification.ENG:
                case UnitClassification.RECON:
                case UnitClassification.ART:
                case UnitClassification.SAM:
                case UnitClassification.AAA:
                case UnitClassification.BM:
                    baseMovement = MotorizedMovt;
                    break;

                // Non-mechanized units
                case UnitClassification.INF:
                case UnitClassification.AB:
                case UnitClassification.AM:
                case UnitClassification.MAR:
                case UnitClassification.SPECF:
                
                    baseMovement = NonMechanizedMovt;
                    break;

                // Air units
                case UnitClassification.ASF:
                case UnitClassification.MRF:
                case UnitClassification.ATT:
                case UnitClassification.BMB:
                case UnitClassification.RCN:
                case UnitClassification.FWT:
                    baseMovement = AirMovt;
                    break;

                // Aviation units (helicopters)
                case UnitClassification.AHEL:
                case UnitClassification.THEL:
                case UnitClassification.SPECH:
                    baseMovement = AviationMovt;
                    break;

                // Facilities and airbases
                case UnitClassification.FAC:
                case UnitClassification.AIRB:
                    baseMovement = 0;
                    break;

                // Naval units - temporarily setting to 0
                default:
                    baseMovement = 0;
                    break;
            }

            // Apply profile modifier
            float modifier = IsMounted ?
                MountedProfile?.MovementModifier ?? 1.0f :
                DeployedProfile?.MovementModifier ?? 1.0f;

            return (int)Math.Round(baseMovement * modifier);
        }

        /// <summary>
        /// Calculates the zone of control based on unit type and profiles.
        /// </summary>
        private int CalculateZOC()
        {
            int baseZOC;

            // Base ZOC by unit type
            switch (UnitType)
            {
                case UnitType.LandUnitDF:
                    baseZOC = 1;
                    break;
                case UnitType.LandUnitIF:
                case UnitType.NavalUnitIF:
                    baseZOC = 1;
                    break;
                case UnitType.AirUnit:
                    baseZOC = 0;
                    break;
                case UnitType.NavalUnitDF:
                    baseZOC = 1;
                    break;
                default:
                    baseZOC = 0;
                    break;
            }

            // Apply profile modifier
            int modifier = IsMounted ?
                MountedProfile?.ZOCModifier ?? 0 :
                DeployedProfile?.ZOCModifier ?? 0;

            return baseZOC + modifier;
        }

        /// <summary>
        /// Updates the experience level based on current experience points.
        /// </summary>
        private void UpdateExperienceLevel()
        {
            ExperienceLevel newLevel;

            if (experiencePoints >= (int)ExperiencePointLevels.Elite)
                newLevel = ExperienceLevel.Elite;
            else if (experiencePoints >= (int)ExperiencePointLevels.Veteran)
                newLevel = ExperienceLevel.Veteran;
            else if (experiencePoints >= (int)ExperiencePointLevels.Experienced)
                newLevel = ExperienceLevel.Experienced;
            else if (experiencePoints >= (int)ExperiencePointLevels.Trained)
                newLevel = ExperienceLevel.Trained;
            else if (experiencePoints >= (int)ExperiencePointLevels.Green)
                newLevel = ExperienceLevel.Green;
            else
                newLevel = ExperienceLevel.Raw;

            ExperienceLevel = newLevel;
        }

        /// <summary>
        /// Checks if supply has dropped to zero and reduces efficiency level accordingly.
        /// This is a focused check specifically for when a unit runs out of supplies
        /// rather than a general efficiency update.
        /// </summary>
        private void SupplyEfficiencyCheck()
        {
            // Only reduce efficiency when supply hits zero
            if (currentDaysSupply <= 0)
            {
                // Drop the efficiency level by one if possible
                switch (efficiencyLevel)
                {
                    case EfficiencyLevel.PeakOperational:
                        EfficiencyLevel = EfficiencyLevel.FullyOperational;
                        break;
                    case EfficiencyLevel.FullyOperational:
                        EfficiencyLevel = EfficiencyLevel.Operational;
                        break;
                    case EfficiencyLevel.Operational:
                        EfficiencyLevel = EfficiencyLevel.DegradedOperations;
                        break;
                    case EfficiencyLevel.DegradedOperations:
                        EfficiencyLevel = EfficiencyLevel.StaticOperations;
                        break;
                        // Static operations is the lowest level, no further reduction
                }
            }
        }

        #endregion
    }
}