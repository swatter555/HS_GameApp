using System.Collections.Generic;
using UnityEngine;

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
        MAR,    // Marine
        RECON,  // Reconnaissance
        AT,     // Anti-tank
        AM,     // Air Mobile
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

    /// <summary>
    /// Represents a military unit with identification, base stats, and optional transport mounting.
    /// </summary>
    public class CombatUnit
    {
        #region Constants

        /// <summary>
        /// The maximum possible hit points a unit can have, used as the baseline for calculating
        /// strength multipliers.
        /// </summary>
        public const int MaxPossibleHitPoints = 40;

        #endregion

        // Identification and metadata
        public string UnitName { get; set; }
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

        // Is this unit currently mounted on a transport?
        public bool IsMounted { get; private set; }

        // Experience
        public int ExperiencePoints { get; private set; }
        public ExperienceLevel ExperienceLevel { get; private set; }
        
        // Combat readiness state gauge
        public EfficiencyLevel EfficiencyLevel { get; private set; }

        // Signals intelligence rating
        public UnitCapability SIGINT_Rating { get; private set; }

        // Nuclear, Biological, and Chemical (NBC) rating
        public UnitCapability NBC_Rating { get; private set; }

        // Special movement abilities
        public StrategicMobility StrategicMobility { get; private set; }

        // Night fighting capability (typically ground and aviation units)
        public UnitCapability NightFighting { get; private set; }

        // All weather capability (aircraft only)
        public UnitCapability AllWeather { get; private set; }

        // Visibility profile
        public VisibilityProfile VisibilityProfile { get; private set; }

        // Hit points
        public const int MaxHitPoints = 40;
        private int hitPoints;
        public int HitPoints => hitPoints;

        // Days of supply.
        public const float MaxDaysSupplyDepot = 100f;
        public const float MaxDaysSupplyUnit = 7f;
        public float CurrentDaysSupply = 5f;

        // Movement points
        public int MaxMovementPoints { get; private set; }
        public int CurrentMovementPoints { get; private set; }

        // Zone of control in hexes
        public int ZOC { get; private set; }

        // x,y location on map
        public Vector2 Position { get; private set; }

        // TODO: Unit Commander
        // TODO: Airbase structure
        // TODO: Facility structure



        // Constructor and methods will be added in subsequent iterations.
    }
}