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
        BASE,   // Land base of some type (e.g., HQ, depot, airbase, etc.)
        DEPOT,  // Supply Depot
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
    public enum UnitSilhouette
    {
        Large,
        Medium,
        Small,
        Tiny
    }

    /// <summary>
    /// Night Vision Gear rating.
    /// </summary>
    public enum NVG_Rating
    {
        None,
        Gen1,
        Gen2
    }

    /// <summary>
    /// Nuclear, Biological, and Chemical (NBC) protection rating.
    /// </summary>
    public enum  NBC_Rating
    {
        None,
        Gen1,
        Gen2
    }

    /// <summary>
    /// Aircraft's ability to fly in various conditions.
    /// </summary>
    public enum AllWeatherRating
    {
        Day,
        Night,
        AllWeather
    }

    /// <summary>
    /// Signals Intelligence (SIGINT) rating.
    /// </summary>
    public enum  SIGINT_Rating
    {
        UnitLevel,
        HQLevel,
        SpecializedLevel
    }

    /// <summary>
    /// Represents a military unit with identification, base stats, and optional transport mounting.
    /// Implements an event-driven design pattern for state changes.
    /// </summary>
    [Serializable]
    public class CombatUnit : ICloneable, ISerializable
    {
        #region TODOs
        // TODO: Unit Commander
        #endregion

        #region Constants

        private const string CLASS_NAME = nameof(CombatUnit);

        /// <summary>
        /// The maximum possible hit points a unit can have, used as the baseline for calculating
        /// strength multipliers.
        /// </summary>
        public const int MaxPossibleHitPoints = 40;

        public const float MaxDaysSupplyDepot = 100f;
        public const float MaxDaysSupplyUnit = 7f;

        public const int ZOCRange = 1; // Zone of Control range for all units

        // Movement constants for different unit types
        private const int MechanizedMovt = 12;
        private const int MotorizedMovt = 10;
        private const int NonMechanizedMovt = 8;
        private const int AirMovt = 100;
        private const int AviationMovt = 24;

        #endregion

        //====== Fields ======
        #region Fields
        //====================

        // Managing unit states.
        private int experiencePoints;
        private ExperienceLevel experienceLevel;
        private EfficiencyLevel efficiencyLevel;
        private bool isMounted;
        private CombatState combatState;
        private int currentHitPoints;
        private float currentDaysSupply;
        private int maxMovementPoints;
        private int currentMovementPoints;
        private Vector2 mapPos;
        #endregion

        //====== Properties ======
        #region Properties
        //========================

        // Identification and metadata
        public string UnitName { get; set; }
        public string UnitID { get; private set; }
        public UnitType UnitType { get; private set; }
        public UnitClassification Classification { get; private set; }
        public UnitRole Role { get; private set; }
        public Side Side { get; private set; }
        public Nationality Nationality { get; private set; }
        public bool IsTransportable { get; private set; }

        // Action counts per turn TODO: Make actions a struct or class with a max and current count.
        public int MoveActions { get; private set; }
        public int CombatActions { get; private set; }
        public int DeploymentActions { get; private set; }

        // Combat profiles
        public WeaponSystemProfile DeployedProfile { get; private set; }
        public WeaponSystemProfile MountedProfile { get; private set; }

        // Informational profile
        public UnitProfile UnitProfile { get; private set; }

        // Properties related to unit's that are a base.
        public bool IsLandBase { get; private set; }
        public LandBase LandBase { get; private set; }
        public bool IsAirbase => LandBase is Airbase;
        public bool IsSupplyDepot => LandBase is SupplyDepot;
        public bool IsFacility => LandBase is not Airbase && LandBase is not SupplyDepot;

        // State management properties.
        public int ExperiencePoints => experiencePoints;
        public ExperienceLevel ExperienceLevel => experienceLevel;
        public EfficiencyLevel EfficiencyLevel => efficiencyLevel;
        public bool IsMounted => isMounted;
        public CombatState CombatState => combatState;
        public int CurrentHitPoints => currentHitPoints;
        public float CurrentDaysSupply => currentDaysSupply;
        public int MaxMovementPoints => maxMovementPoints;
        public int CurrentMovementPoints => currentMovementPoints;
        public Vector2 MapPos => mapPos;

        #endregion


        //====== Constructors ======
        #region Constructors
        //==========================

        public CombatUnit()
        {

        }

        private void InitializeFacility()
        {
            // Initialize the facility base based on the unit's classification
            LandBase = Classification switch
            {
                UnitClassification.AIRB => new Airbase(),
                UnitClassification.DEPOT => new SupplyDepot(UnitName, Side, DepotSize.Medium),
                UnitClassification.BASE => new LandBase(),// For general facilities, use the base LandBase class
                _ => null,// Non-facility units don't have a LandBase
            };
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
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

        //====== ICloneable Implementation ======
        #region ICloneable Implementation
        //=======================================

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

        //====== ISerializable Implementation ======
        #region ISerializable Implementation
        //==========================================

        /// <summary>
        /// Serializes this combat unit.
        /// </summary>
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

    //====== Public Methods ======
    #region Public Methods
    //============================


    #endregion

    //====== Private Methods ======
    #region Private Methods
    //=============================


    #endregion
}