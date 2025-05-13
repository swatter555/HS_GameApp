namespace HammerAndSickle.Models
{
    //=======================
    //====== CombatUnit======
    //=======================

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
        AIRB,   // AirbaseSubProfile
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
    public enum NBC_Rating
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
    public enum SIGINT_Rating
    {
        UnitLevel,
        HQLevel,
        SpecializedLevel
    }


    //=====================
    //====== Leaders ======
    //=====================

    /// <summary>
    /// Represents the military rank grade of a commander.
    /// </summary>
    public enum CommandGrade
    {
        JuniorGrade,    // Lieutenant Colonel equivalent
        SeniorGrade,    // Colonel equivalent
        TopGrade        // Major General equivalent
    }

    /// <summary>
    /// The command ability of an officer.
    /// </summary>
    public enum CommandAbility
    {
        Poor = -2,
        BelowAverage = -1,
        Average = 0,
        Good = 1,
        Superior = 2,
        Genius = 3
    }

    //================================
    //====== Leader Skill Paths ======
    //================================

    /// <summary>
    /// Represents leadership skill path.
    /// </summary>
    public enum LeadershipPath 
    {
        None, 
        ShockFormation, 
        IronDiscipline, 
        MaskirovkaMaster, 
        TacticalGenius, 
        PoliticalOfficer, 
        OperationalArt, 
        HeroOfSovietUnion, 
        DeepBattleTheorist 
    }

    /// <summary>
    /// Represents the rear area skill path.
    /// </summary>
    public enum RearAreaSkillPath 
    { 
        None, 
        SupplyEconomy, 
        FieldWorkshop, 
        PartyConnections, 
        StrategicAirlift, 
        ArmoredSupplyColumn 
    }

    /// <summary>
    /// Represents battlefield skill path.
    /// </summary>
    public enum BattleDoctrineSkillPath
    {
        None, 
        ArmoredWarfare, 
        HullDownExpert, 
        ShockTankCorps, 
        NightFightingSpecialist,
        DefenseInDepth, 
        HedgehogDefense, 
        FortificationEngineer, 
        TrenchWarfareExpert,
        QueenOfBattle, 
        ForwardObservationPost, 
        IntegratedAirDefenseSystem, 
        PrecisionTargetting
    }

    /// <summary>
    /// Represents the combat operations skill path.
    /// </summary>
    public enum CombatOperationsSkillPath 
    { 
        None, 
        OffensiveDoctrine, 
        ManeuverDoctrine, 
        PursuitDoctrine, 
        SpecialistCorps, 
        ReconnaissanceInForce, 
        CombinedArmsWarfare 
    }

    //===============================
    //====== Skill Bonus Types ======
    //===============================

    // Enum to define the type of bonus a skill provides
    public enum SkillBonusType
    {
        None,
        UnitXP, // Value is a float multiplier (e.g., 0.25 for +25%)
        Command,        // Value is an int (e.g., 1 for +1)
        Initiative,           // Value is an int (e.g., 1 for +1)
        Detection,  // Value is an int (e.g., 1 for +1 detection)
        SupplyConsumption, // Value is a float total multiplier (e.g., 0.67 for 33% reduction)
        PrestigeCost,      // Value is a float total multiplier (e.g., 0.67 for 33% reduction)
        EmergencyResupply,    // Boolean, value not directly used, presence of skill implies capability
        Entrenchment,        // Boolean, skip defensive and go straight to entrenched.
        HardAttack,
        HardDefense,
        SoftAttack,
        SoftDefense,

        DetectionRange,
        NightFighting,

        IndirectRange,
        AirDefense,
        SupplyPenetration,      // Boolean, supply can pierce 1 enemy ZOC
        CombatActionBonus,      // Bonus combat action
        MovementActionBonus,    // Bonus move action
        DeploymentActionBonus,  // Bonus deployment action
        BreakthroughCapability, // Boolean
    }

}
