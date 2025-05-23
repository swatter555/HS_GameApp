using System.Xml.Linq;
using UnityEngine;

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
    /// Signals IntelligenceDoctrine (SIGINT) rating.
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
    /// LeadershipFoundation path tied to officer promotions, providing increasing command ability.
    /// </summary>
    public enum LeadershipFoundation
    {
        None,
        JuniorOfficerTraining_CommandTier1,         // Junior Officer Training (+1 command)
        PromotionToSeniorGrade_SeniorPromotion,     // Promotion to Senior Grade (costs XP)
        SeniorOfficerTraining_CommandTier2,         // Senior Officer Training (+1 additional command)
        PromotionToTopGrade_TopPromotion,           // Promotion to Top Grade (costs XP)
        GeneralStaffTraining_CommandTier3           // General Staff Training (+1 additional command)
    }

    /// <summary>
    /// Special bonus path, only tier requirements apply.
    /// </summary>
    public enum PoliticallyConnectedFoundation
    {
        None,
        EmergencyAirDrop_EmergencyResupply,                  // Emergency Air Drop
        DirectLineToHQ_SupplyConsumption,                    // Direct Line To HQ
        ForeignTechnology_NVG,                               // Foreign Technology
        BetterReplacements_ReplacementXP,                    // Better Replacements
        ConnectionsAtTheTop_PrestigeCost                     // Connections At The Top
    }

    /// <summary>
    /// Armored warfare skills.
    /// </summary>
    public enum ArmoredDoctrine
    {
        None,
        ShockTankCorps_HardAttack,             // Shock Tank Corps
        HullDownExpert_HardDefense,            // Hull Down Expert
        PursuitDoctrine_Breakthrough,          // Pursuit Doctrine
    }

    /// <summary>
    /// Infantry and soft-target focused skills.
    /// </summary>
    public enum InfantryDoctrine
    {
        None,
        InfantryAssaultTactics_SoftAttack,      // Infantry Assault Tactics
        DefensiveDoctrine_SoftDefense,          // Defensive Doctrine
        TerrainAdvantage_RoughTerrainMovt       // Move more easily in rough terrain.
    }

    /// <summary>
    /// Artillery and indirect fire skills.
    /// </summary>
    public enum ArtilleryDoctrine
    {
        None,
        PrecisionTargeting_IndirectRange,        // Precision Targeting
        MobileArtilleryDoctrine_ShootAndScoot,   // Mobile Artillery Doctrine
        FireMissionSpecialist_AdvancedTargetting // Fire Mission Specialist
    }

    /// <summary>
    /// Air defense and anti-air skills.
    /// </summary>
    public enum AirDefenseDoctrine
    {
        None,
        OffensiveAirDefense_AirAttack,            // Offensive Air Defense
        IntegratedAirDefenseSystem_AirDefense,    // Integrated Air Defense System
        ReadyResponseProtocol_OpportunityAction   // Ready Response Protocol
    }

    /// <summary>
    /// VDV Airborne skills.
    /// </summary>
    public enum AirborneDoctrine
    {
        None,
        RapidDeploymentPlanning_ImpromptuPlanning,    // Rapid Deployment Planning
        CombatDropDoctrine_AirborneAssault,           // Combat Drop Doctrine
        EliteParatrooperCorps_AirborneElite           // Elite Paratrooper Corps
    }

    /// <summary>
    /// Air Mobile and helicopter operation skills.
    /// </summary>
    public enum AirMobileDoctrine
    {
        None,
        RapidRedeployment_AirMobile,                  // Rapid Redeployment
        HeliborneStrikeForce_AirMobileAssault,        // Heliborne Strike Force
        EliteAirMobileOperations_AirMobileElite       // Elite Air Mobile Operations
    }

    /// <summary>
    /// IntelligenceDoctrine and reconnaissance skills.
    /// </summary>
    public enum IntelligenceDoctrine
    {
        None,
        EnhancedIntelligenceCollection_ImprovedGathering,    // Enhanced IntelligenceDoctrine Collection
        ConcealedOperationsBase_UndergroundBunker,           // Concealed Operations Base
        SatelliteIntelligence_SpaceAssets,                   // Priority Target Designation
    }

    /// <summary>
    /// CombinedArmsSpecialization skills.
    /// </summary>
    public enum CombinedArmsSpecialization
    {
        None,
        AviationAssets_SpottingRange,       // +1 spotting range
        ExpertStaff_MovementAction,         // +1 move action
        TacticalGenius_CombatAction,        // +1 combat action
        NightCombatOperations_NightCombat   // Night combat bonus
    }

    /// <summary>
    /// SIGINT (Signals IntelligenceDoctrine) skills.
    /// </summary>
    public enum SignalIntelligenceSpecialization
    {
        None,
        CommunicationsDecryption_SignalDecryption,           // Communications Decryption
        ElectronicSurveillanceNetwork_SpottingRange,         // Electronic Surveillance Network
        RadioElectronicCombat_ElectronicWarfare,             // Radio Electronic Combat
        EnemyBehaviorAnalysis_PatternRecognition             // Enemy Behavior Analysis
    }

    /// <summary>
    /// EngineeringSpecialization skills.
    /// </summary>
    public enum EngineeringSpecialization
    {
        None,
        RiverCrossingOperations_RiverCrossing,               // River Crossing Operations
        AmphibiousAssaultTactics_RiverAssault,               // Amphibious Assault Tactics
        CombatEngineeringCorps_BridgeBuilding,               // Combat EngineeringSpecialization Corps
        FieldFortificationExpert_FieldFortification          // Field Fortification Expert
    }

    /// <summary>
    /// Special forces and irregular warfare skills.
    /// </summary>
    public enum SpecialForcesSpecialization
    {
        None,
        TerrainExpert_TerrainMastery,                        // Terrain Expert
        InfiltrationTactics_InfiltrationMovement,            // Infiltration Tactics
        SuperiorCamouflage_ConcealedPositions,               // Superior Camouflage
        AmbushTactics_AmbushTactics,                         // Ambush Tactics
    }

    //===============================
    //====== Skill Bonus Types ======
    //===============================

    // Enum to define the type of bonus a skill provides
    public enum SkillBonusType
    {
        None,

        // LeadershipFoundation
        CommandTier1,         //  +1 to Command
        SeniorPromotion,      // Boolean, promotion to SeniorGrade.
        CommandTier2,         // +1 additional command
        TopPromotion,         // Boolean, promotion to TopGrade
        CommandTier3,         // +1 additional command

        // PoliticallyConnectedFoundation
        EmergencyResupply,    // Boolean, one free emergency resupply/replacement action per scenario.
        SupplyConsumption,    // Value is a float total multiplier (e.g., 0.67 for 33% reduction)
        NVG,                  // Upgrade unit to latest gen NVG
        ReplacementXP,        // Replacements recieved are more experienced.
        PrestigeCost,         // Value is a float total multiplier (e.g., 0.67 for 33% reduction)

        // ArmoredDoctine
        HardAttack,           // Bonus to HardAttack
        HardDefense,          // Bonus to HardDefense
        Breakthrough,         // Boolean, enemy retreat gives bonus move action.

        // InfanrtyDoctrine
        SoftAttack,           // Bonus to SoftAttack
        SoftDefense,          // Bonus to SoftDefense
        RoughTerrainMovt,     // Boolean, rough terrain movement bonus (Rough, Forest, Marsh).

        // AirDefenseDoctrine
        AirAttack,            // Bonus to AirAttack
        AirDefense,           // Bonus to AirDefense
        OpportunityAction,    // Boolean, gets extra opportunity action(attack).

        // ArtilleryDoctrine
        ShootAndScoot,        // Boolean, may move after ranged attack.
        AdvancedTargetting,   // Boolean, Indirect attack two different units in one turn, uses 4 supply.
        IndirectRange,        // Indirect fire range +1

        // AirMobileDoctrine
        AirMobile,            // Boolean, may move after air landing.
        AirMobileAssault,     // Boolean, still have combat action after landing.
        AirMobileElite,       // Boolean, significantly reduces vulnerability to enemy fire when mounted.

        // AirborneDoctrine
        ImpromptuPlanning,     // Boolean, boarding aircraft doesn't cost an action.
        AirborneAssault,       // Boolean, 1/2 suppression cost for a jump.
        AirborneElite,         // Boolean, still have combat action after jump.

        // IntelligenceDoctrine
        ImprovedGathering,    // +1 Intel gathering actions
        UndergroundBunker,    // Much smaller unit silouette.
        SpaceAssets,          // Boolean, has a chance to spot enemy units anywhere on the map.

        // CombinedArmsSpecialization
        SpottingRange,        // +1 spotting range
        NightCombat,          // Boolean, night combat bonus.
        MovementAction,       // +1 move action
        CombatAction,         // +1 combat action

        // EngineeringSpecialization
        RiverCrossing,        // Boolean, river crossing move bonus.
        RiverAssault,         // Boolean, amphibious assault combat bonus.
        BridgeBuilding,       // Boolean, may build bridges over rivers in one turn.
        FieldFortification,   // Boolean, may build a persistent static fortification.

        // SpecialForcesSpecialization
        TerrainMastery,         // Boolean, combat bonuses in Rough, Forest, Mountain, Urban.
        InfiltrationMovement,   // Boolean, can move through enemy ZOC more easily
        ConcealedPositions,     // Boolean, units have reduced silhouette.
        AmbushTactics,          // Boolean, first attack from concealment gets +50% combat bonus

        // SIGINTSpecialization
        SignalDecryption,      // Boolean, reveals more detailed unit information when spotted
        ElectronicWarfare,     // Boolean, randomly makes an enemy unit immobile when gathering intel.
        PatternRecognition,    // Boolean, shows likely enemy movement paths for some spotted units.
    }

    /// <summary>
    /// Enum representing the major skill branches
    /// </summary>
    public enum SkillBranch
    {
        None,
        LeadershipFoundation,
        PoliticallyConnectedFoundation,
        ArmoredDoctrine,
        InfantryDoctrine,
        ArtilleryDoctrine,
        AirDefenseDoctrine,
        AirborneDoctrine,
        AirMobileDoctrine,
        IntelligenceDoctrine,
        CombinedArmsSpecialization,
        SignalIntelligenceSpecialization,
        EngineeringSpecialization,
        SpecialForcesSpecialization
    }

    /// <summary>
    /// Skill tiers representing progression and requirements
    /// </summary>
    public enum SkillTier
    {
        None = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5
    }

    //================================
    //====== WeaponSystem Enums ======
    //================================

    /// <summary>
    /// Weapon systems in the game.
    /// </summary>
    public enum WeaponSystems
    {
        // Soviet weapon systems
        TANK_T55A,
        TANK_T64A,
        TANK_T64B,
        TANK_T72A,
        TANK_T72B,
        TANK_T80B,
        TANK_T80U,
        TANK_T80BV,
        APC_MTLB,
        APC_BTR70,
        APC_BTR80,
        IFV_BMP1,
        IFV_BMP2,
        IFV_BMP3,
        IFV_BMD2,
        IFV_BMD3,
        RCN_BRDM2,
        RCN_BRDM2AT,
        TRUCK_RED,
        SPA_2S1,
        SPA_2S3,
        SPA_2S5,
        SPA_2S19,
        ART_D20,
        ART_D30,
        ROC_BM21,
        ROC_BM27,
        ROC_BM30,
        SSM_SCUD,
        SPAAA_ZSU57,
        SPAAA_ZSU23,
        SPAAA_2K22,
        SPSAM_9K31,
        SAM_S75,
        SAM_S125,
        SAM_S300,
        HEL_MI8T,
        HEL_MI8AT,
        HEL_MI24D,
        HEL_MI24V,
        HEL_MI28,
        AWACS_A50,
        TRAN_AN8,
        ASF_MIG21,
        ASF_MIG23,
        ASF_MIG25,
        ASF_MIG29,
        ASF_MIG31,
        ASF_SU27,
        ASF_SU47,
        MRF_MIG27,
        ATT_SU25,
        ATT_SU25B,
        BMB_SU24,
        BMB_TU16,
        BMB_TU22,
        BMB_TU22M3,
        RCN_MIG25R,

        // US
        TANK_M1,
        IFV_M2,
        IFV_M3,
        APC_M113,
        APC_LVTP7,
        SPA_M109,
        ROC_MLRS,
        SPAAA_M163,
        SPSAM_CHAP,
        SAM_HAWK,
        HEL_AH64,
        ASF_F15,
        ASF_F4,
        MRF_F16,
        ATT_A10,
        BMB_F111,
        BMB_F117,
        RCN_SR71,

        // West Germany
        TANK_LEOPARD1,
        TANK_LEOPARD2,
        IFV_MARDER,
        SPAAA_GEPARD,
        HEL_BO105,
        MRF_TornadoIDS,

        // UK
        TANK_CHALLENGER1,
        IFV_WARRIOR,

        // France
        TANK_AMX30,
        SPAAA_ROLAND,
        ASF_MIRAGE2000,
        ATT_JAGUAR,

        // Types of infantry
        SV_REG_INF,
        SV_AB_INF,
        SV_AM_INF,
        SV_MAR_INF,
        SV_SPEC_INF,
        SV_ENG_INF,

        US_REG_INF,
        US_AB_INF,
        US_AM_INF,
        US_MAR_INF,
        US_SPEC_INF,
        US_ENG_INF,

        FRG_REG_INF,
        FRG_AB_INF,
        FRG_AM_INF,
        FRG_MAR_INF,
        FRG_SPEC_INF,
        FRG_ENG_INF,

        UK_REG_INF,
        UK_AB_INF,
        UK_AM_INF,
        UK_MAR_INF,
        UK_SPEC_INF,
        UK_ENG_INF,

        FRA_REG_INF,
        FRA_AB_INF,
        FRA_AM_INF,
        FRA_MAR_INF,
        FRA_SPEC_INF,
        FRA_ENG_INF,

        ARAB_REG_INF,
        ARAB_AB_INF,
        ARAB_AM_INF,
        ARAB_MAR_INF,
        ARAB_SPEC_INF,
        ARAB_ENG_INF,

        AIRBASE,
        SUPPLY_DEPOT,

        EAST_SIGINT,
        WEST_SIGINT
    }

    /// <summary>
    /// The type of upgrade a unit can receive.
    /// </summary>
    public enum UpgradeType
    {
        None,
        AFV,
        IFV,
        APC,
        RECON,
        SPART,
        ART,
        ROC,
        SSM,
        SAM,
        SPSAM,
        AAA,
        SPAAA,
        Fighter,
        Attack,
        Bomber
    }

    //============================
    //====== LandBase Enums ======
    //============================

    /// <summary>
    /// Represents the operational capacity of a land-based facility based on damage level.
    /// </summary>
    public enum OperationalCapacity
    {
        Full,
        SlightlyDegraded,
        ModeratelyDegraded,
        HeavilyDegraded,
        OutOfOperation
    }

    /// <summary>
    /// Defines the size of a supply depot and its maximum storage capacity.
    /// </summary>
    public enum DepotSize
    {
        Small,   // 30 days of supply
        Medium,  // 50 days of supply
        Large,   // 80 days of supply
        Huge     // 110 days of supply
    }

    /// <summary>
    /// Defines the supply generation rate of a depot.
    /// </summary>
    public enum SupplyGenerationRate
    {
        Minimal,        // 0.5 days of supply per turn
        Basic,          // 1.0 days of supply per turn
        Standard,       // 1.5 days of supply per turn
        Enhanced,       // 2.5 days of supply per turn
        Industrial      // 4.0 days of supply per turn
    }

    /// <summary>
    /// Defines how far a depot can project supplies effectively.
    /// </summary>
    public enum SupplyProjection
    {
        Local,          // 2 hex radius
        Extended,       // 4 hex radius
        Regional,       // 6 hex radius
        Strategic,      // 9 hex radius
        Theater         // 12 hex radius
    }

    /// <summary>
    /// Defines the category of a depot.
    /// </summary>
    public enum DepotCategory
    {
        Main,       // Primary depot with special abilities
        Secondary   // Standard field depot
    }
}