using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Core.GameData
{
    #region CombatUnit Enums

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
        CAV,    // Horse Cavalry
        AT,     // Anti-tank
        AM,     // Air Mobile
        MAM,    // Mechanized Air Mobile
        INF,    // INF
        SPECF,  // Special Forces
        ART,    // Artillery
        SPA,    // Self-Propelled Artillery
        ROC,    // Rocket Artillery
        BM,     // Ballistic Missile
        SAM,    // Surface-to-Air Missile
        SPSAM,  // Self Proplled SAM
        AAA,    // Anti-Aircraft Artillery
        SPAAA,  // Self-Propelled Anti-Aircraft Artillery
        ENG,    // Engineer
        HELO,   // ATT helicopter
        FGT,    // Fighter aircraft
        ATT,    // ATT aircraft
        AWACS,  // AWACS aircraft
        BMB,    // Bomber aircraft
        RECONA, // Recon Aircraft
        HQ,     // HQ facility
        DEPOT,  // Supply Depot
        AIRB    // Airbase
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
        AirSuperiority,
        AirMultirole,
        AirGroundAttack,
        AirStrategicAttack,
        AirRecon,
        AirborneEarlyWarning,
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
        SAUD,
        GENERIC
    }

    /// <summary>
    /// The types of actions a CombatUnit can perform.
    /// </summary>
    public enum ActionTypes
    {
        MoveAction,         // Move the unit to a new location
        CombatAction,       // Engage in combat with another unit
        DeployAction,       // Deploy the unit into a specific formation or position
        OpportunityAction,  // Perform an action based on an opportunity (e.g., ambush)
        IntelAction         // Gather intelligence or perform reconnaissance
    }

    /// <summary>
    /// The unit's special movement capabilities.
    /// </summary>
    public enum StrategicMobility
    {
        Heavy,
        AirLift,
        NavalAssault,
        AirDrop,
        AirMobile,
        Aviation,
        Aircraft
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
        Green = 50,
        Trained = 120,
        Experienced = 220,
        Veteran = 330,
        Elite = 400
    }

    /// <summary>
    /// How ready is the unit for combat.
    /// </summary>
    public enum EfficiencyLevel
    {
        StaticOperations,
        DegradedOperations,
        NormalOperations,
        CombatOperations,
        FullOperations
    }

    /// <summary>
    /// How stealthy a unit is.
    /// </summary>
    public enum UnitSilhouette
    {
        Tiny,
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Night Vision Gear rating.
    /// </summary>
    public enum NVG_Rating
    {
        None,
        Gen1,
        Gen2,
        Gen3
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
        GroundUnit,
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

    /// <summary>
    /// The intelligence info on enemy units as they are spotted on the map.
    /// </summary>
    public enum SpottedLevel
    {
        Level0, // Not spotted
        Level1, // Unit name visible
        Level2, // Above plus DeploymentStatus and an error rate of about 30%
        Level3, // Above plus EXP and EFF levels, and a 10% error rate.
        Level4, // Above plus no error rate.
    }

    #endregion // CombatUnit Enums

    #region DeploymentStateMachine

    public enum DeploymentPosition
    {
        Embarked     = 5,
        Mobile       = 4,
        Deployed     = 3,
        HastyDefense = 2,
        Entrenched   = 1,
        Fortified    = 0
    }

    #endregion // DeploymentStateMachine

    #region LeaderEnums

    /// <summary>
    /// Represents the military rank grade of a commander.
    /// </summary>
    public enum CommandGrade
    {
        JuniorGrade = 1,    // Lieutenant Colonel equivalent
        SeniorGrade = 2,    // Colonel equivalent
        TopGrade = 3     // Major General equivalent
    }

    /// <summary>
    /// The command ability of an officer.
    /// </summary>
    public enum CommandAbility
    {
        Average = 0,
        Good = 1,
        Superior = 2,
        Genius = 3
    }

    /// <summary>
    /// Marks an enum as representing a skill branch for automatic registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class SkillBranchEnumAttribute : Attribute
    {
        public SkillBranch Branch { get; }

        public SkillBranchEnumAttribute(SkillBranch branch)
        {
            Branch = branch;
        }
    }

    /// <summary>
    /// LeadershipFoundation path tied to officer promotions, providing increasing command ability.
    /// </summary>
    [SkillBranchEnum(SkillBranch.LeadershipFoundation)]
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
    [SkillBranchEnum(SkillBranch.PoliticallyConnectedFoundation)]
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
    [SkillBranchEnum(SkillBranch.ArmoredDoctrine)]
    public enum ArmoredDoctrine
    {
        None,
        ShockTankCorps_HardAttack,             // Shock Tank Corps
        HullDownExpert_HardDefense,            // Hull Down Expert
        PursuitDoctrine_Breakthrough,          // Pursuit Doctrine
    }

    /// <summary>
    /// INF and soft-target focused skills.
    /// </summary>
    [SkillBranchEnum(SkillBranch.InfantryDoctrine)]
    public enum InfantryDoctrine
    {
        None,
        InfantryAssaultTactics_SoftAttack,      // INF Assault Tactics
        DefensiveDoctrine_SoftDefense,          // Defensive Doctrine
        RoughTerrainOperations_RTO              // Move more easily in rough terrain.
    }

    /// <summary>
    /// Artillery and indirect fire skills.
    /// </summary>
    [SkillBranchEnum(SkillBranch.ArtilleryDoctrine)]
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
    [SkillBranchEnum(SkillBranch.AirDefenseDoctrine)]
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
    [SkillBranchEnum(SkillBranch.AirborneDoctrine)]
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
    [SkillBranchEnum(SkillBranch.AirMobileDoctrine)]
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
    [SkillBranchEnum(SkillBranch.IntelligenceDoctrine)]
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
    [SkillBranchEnum(SkillBranch.CombinedArmsSpecialization)]
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
    [SkillBranchEnum(SkillBranch.SignalIntelligenceSpecialization)]
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
    [SkillBranchEnum(SkillBranch.EngineeringSpecialization)]
    public enum EngineeringSpecialization
    {
        None,
        RiverCrossingOperations_RiverCrossing,               // River Crossing Operations
        AmphibiousAssaultTactics_RiverAssault,               // NavalAssault Assault Tactics
        CombatEngineeringCorps_BridgeBuilding,               // Combat EngineeringSpecialization Corps
        FieldFortificationExpert_FieldFortification          // Field Fortification Expert
    }

    /// <summary>
    /// Special forces and irregular warfare skills.
    /// </summary>
    [SkillBranchEnum(SkillBranch.SpecialForcesSpecialization)]
    public enum SpecialForcesSpecialization
    {
        None,
        TerrainExpert_TerrainMastery,                        // Terrain Expert
        InfiltrationTactics_InfiltrationMovement,            // Infiltration Tactics
        SuperiorCamouflage_ConcealedPositions,               // Superior Camouflage
        AmbushTactics_AmbushTactics,                         // Ambush Tactics
    }

    // Enum to define the type of bonus a skill provides
    public enum SkillBonusType
    {
        None,

        // LeadershipFoundation
        CommandTier1,         // +X to Command
        SeniorPromotion,      // Boolean, promotion to SeniorGrade.
        CommandTier2,         // +X additional command
        TopPromotion,         // Boolean, promotion to TopGrade
        CommandTier3,         // +X additional command

        // PoliticallyConnectedFoundation
        EmergencyResupply,    // Boolean, one free emergency resupply per scenario.
        SupplyConsumption,    // Supplies are consumed at reduced rate.
        NVG,                  // Boolean, upgrade unit to latest gen NVG.
        ReplacementXP,        // Unit gets better replacements (X bonus levels).
        PrestigeCost,         // Unit is cheaper to upgrade (X% discount).

        // ArmoredDoctine
        HardAttack,           // Bonus to HardAttack
        HardDefense,          // Bonus to HardDefense
        Breakthrough,         // Boolean, enemy retreat gives refreshes movement points to max and +1 move actions.

        // InfanrtyDoctrine
        SoftAttack,           // Bonus to SoftAttack
        SoftDefense,          // Bonus to SoftDefense
        RTO,                  // Bonus, Rough Terrain Operations (Forest, Marsh, Rough) 

        // AirDefenseDoctrine
        AirAttack,            // Bonus to AirAttack
        AirDefense,           // Bonus to AirDefense
        OpportunityAction,    // Bonus to opportunity actions.

        // ArtilleryDoctrine
        ShootAndScoot,        // Boolean, may move after ranged attack.
        AdvancedTargetting,   // Boolean, Indirect attack two different units in one turn, uses 4 supply.
        IndirectRange,        // Indirect fire range +1

        // AirMobileDoctrine
        AirMobile,            // Boolean, unit has 1 move action after landing.
        AirMobileAssault,     // Boolean, unit has 1 combat action after landing.
        AirMobileElite,       // Boolean, significantly reduces vulnerability to enemy fire when mounted.

        // AirborneDoctrine
        ImpromptuPlanning,     // Boolean, boarding aircraft doesn't cost an action.
        AirborneAssault,       // Boolean, only lose 1 level of efficiency after jump.
        AirborneElite,         // Boolean, unit will retain one combat action after jump.

        // IntelligenceDoctrine
        ImprovedGathering,    // +1 Intel gathering actions
        UndergroundBunker,    // Much smaller unit silouette.
        SpaceAssets,          // Boolean, has a chance to spot enemy units anywhere on the map.

        // CombinedArmsSpecialization
        SpottingRange,        // +1 spotting range
        NightCombat,          // Bonus to night combat operations.
        MovementAction,       // +1 move action
        CombatAction,         // +1 combat action

        // EngineeringSpecialization
        RiverCrossing,        // River crossing move bonus.
        RiverAssault,         // NavalAssault assault combat bonus.
        BridgeBuilding,       // Boolean, may build bridges over rivers in one turn.
        FieldFortification,   // Boolean, may build a persistent static fortification.

        // SpecialForcesSpecialization
        TerrainMastery,         // Move more easily through all non-clear terrain.
        InfiltrationMovement,   // Move more easily through enemy ZOCs.
        ConcealedPositions,     // Smaller silhouette.
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
        None = 0,

        // Foundation branches starts at 1.
        [BranchType(BranchType.Foundation)]
        LeadershipFoundation = 1,

        [BranchType(BranchType.Foundation)]
        PoliticallyConnectedFoundation = 2,

        // Doctrine branches starts at 10.
        [BranchType(BranchType.Doctrine)]
        ArmoredDoctrine = 10,

        [BranchType(BranchType.Doctrine)]
        InfantryDoctrine = 11,

        [BranchType(BranchType.Doctrine)]
        ArtilleryDoctrine = 12,

        [BranchType(BranchType.Doctrine)]
        AirDefenseDoctrine = 13,

        [BranchType(BranchType.Doctrine)]
        AirborneDoctrine = 14,

        [BranchType(BranchType.Doctrine)]
        AirMobileDoctrine = 15,

        [BranchType(BranchType.Doctrine)]
        IntelligenceDoctrine = 16,

        //Specialization branches start at 20.
        [BranchType(BranchType.Specialization)]
        CombinedArmsSpecialization = 20,

        [BranchType(BranchType.Specialization)]
        SignalIntelligenceSpecialization = 21,

        [BranchType(BranchType.Specialization)]
        EngineeringSpecialization = 22,

        [BranchType(BranchType.Specialization)]
        SpecialForcesSpecialization = 23
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

    /// <summary>
    /// The types of branches that a skill can belong to.
    /// </summary>
    public enum BranchType
    {
        Foundation = 1,
        Doctrine = 2,
        Specialization = 3
    }

    // Used to maintain branch type information for skill branches
    [AttributeUsage(AttributeTargets.Field)]
    public class BranchTypeAttribute : Attribute
    {
        public BranchType Type { get; }
        public BranchTypeAttribute(BranchType type) => Type = type;
    }

    #endregion // LeaderEnums

    #region WeaponSystem Enums

    /// <summary>
    /// Represents the general category of weapon system.
    /// </summary>
    public enum WeaponSystemType
    {
        Hard,
        Soft,
        Air
    }

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
        IFV_BMD1,
        IFV_BMD2,
        IFV_BMD3,
        RCN_BRDM2,
        RCN_BRDM2AT,
        SPA_2S1,
        SPA_2S3,
        SPA_2S5,
        SPA_2S19,
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
        HEL_MI8T,        // This is a transport helicopter for air mobile troops, for mobile profile.
        HEL_MI8AT,
        HEL_MI24D,
        HEL_MI24V,
        HEL_MI28,
        AWACS_A50,
        FGT_MIG21,
        FGT_MIG23,
        FGT_MIG25,
        FGT_MIG29,
        FGT_MIG31,
        FGT_SU27,
        FGT_SU47,
        FGT_MIG27,
        ATT_SU25,
        ATT_SU25B,
        BMB_SU24,
        BMB_TU16,
        BMB_TU22,
        BMB_TU22M3,
        RCNA_MIG25R,
        Transport_AIR,   // Embarkment profile transport for paratroopers. Player side only.
        Transport_NAVAL, // Embarkment profile transport for maritime operations. Player side only.

        // USA
        TANK_M1,
        TANK_M60A3,
        TANK_M551,
        IFV_M2,
        IFV_M3,
        APC_M113,
        APC_LVTP7,
        SPA_M109,
        ROC_MLRS,
        SPAAA_M163,
        SPSAM_CHAP,
        SAM_HAWK,
        HEL_OH58,
        HEL_AH64,
        HEL_UH60,        // This is a transport helicopter for air mobile troops, for mobile profile.
        AWACS_E3,
        FGT_F15,
        FGT_F4,
        FGT_F16,
        ATT_A10,
        BMB_F111,
        BMB_F117,
        RCNA_SR71,

        // West Germany (FRG)
        TANK_LEOPARD1,
        TANK_LEOPARD2,
        IFV_MARDER,
        RCN_LUCHS,
        SPAAA_GEPARD,
        HEL_BO105,
        FGT_TORNADO_IDS,

        // UK
        TANK_CHALLENGER1,
        IFV_WARRIOR,
        APC_FV432,
        RCN_SCIMITAR,
        SAM_RAPIER,
        HEL_LYNX,
        FGT_TORNADO_GR1,

        // France
        TANK_AMX30,
        IFV_AMX10P,
        APC_VAB,
        RCN_ERC90,
        SPA_AUF1,
        SPSAM_ROLAND,
        FGT_MIRAGE2000,
        ATT_JAGUAR,

        // Generic types
        AAA_GENERIC,
        ART_LIGHT_GENERIC,
        ART_HEAVY_GENERIC,
        MANPAD_GENERIC,
        ATGM_GENERIC,
        AT_RPG7,
        MORTAR_81MM,
        MORTAR_120MM,
        RR_RECOILLESS_RIFLE,
        CAVALRY_GENERIC,
        TRUCK_GENERIC,

        // Shared profiles for infantry
        INF_REG,    // Regular INF
        INF_AB,     // Airborne INF
        INF_AM,     // Air Mobile INF
        INF_MAR,    // Marine INF
        INF_SPEC,   // Special Forces INF
        INF_ENG,    // Engineer INF

        // Bases
        LANDBASE_GENERIC,
        AIRBASE_GENERIC,
        SUPPLYDEPOT_GENERIC,

        COMBAT, // Used for generic combat calculations
        DEFAULT // Fallback value
    }

    /// <summary>
    /// The type of upgrade a unit can receive.
    /// </summary>
    public enum UpgradeType
    {
        None,
        INF,
        AFV,
        IFV,
        APC,
        RECON,
        SPA,
        ART,
        ROC,
        SSM,
        SAM,
        SPSAM,
        AAA,
        SPAAA,
        ATGM,
        FGT,
        ATT,
        BMB,
        RCN,
        AWACS,
        ATTHELO,
        TRNHELO,
        TRNAIR,
        TRNNAVAL,
        BASE
    }

    #endregion // WeaponSystem Enums

    #region Intel Enums

    /// <summary>
    /// Type of UnitProfiles
    /// </summary>
    public enum IntelProfileTypes
    {
        // Soviet profiles
        SV_MRR_BTR70,   // Motor Rifle Regiment
        SV_MRR_BTR80,   // Motor Rifle Regiment
        SV_MRR_BMP1,    // Motor Rifle Regiment (BMP)
        SV_MRR_BMP2,    // Motor Rifle Regiment (BMP)
        SV_MRR_BMP3,    // Motor Rifle Regiment (BMP)

        SV_TR_T55,      // Tank Regiment T-55
        SV_TR_T64A,     // Tank Regiment T-72
        SV_TR_T64B,     // Tank Regiment T-72
        SV_TR_T72A,     // Tank Regiment T-72
        SV_TR_T72B,     // Tank Regiment T-72
        SV_TR_T80B,     // Tank Regiment T-80
        SV_TR_T80U,     // Tank Regiment T-80
        SV_TR_T80BV,    // Tank Regiment T-80

        SV_AR_HVY,     // Artillery Regiment
        SV_AR_LGT,     // Artillery Regiment
        SV_AR_2S1,     // Self-Propelled Artillery Regiment
        SV_AR_2S3,     // Self-Propelled Artillery Regiment
        SV_AR_2S5,     // Self-Propelled Artillery Regiment
        SV_AR_2S19,    // Self-Propelled Artillery Regiment

        SV_ROC_BM21,    // Rocket Artillery Regiment
        SV_ROC_BM27,    // Rocket Artillery Regiment
        SV_ROC_BM30,    // Rocket Artillery Regiment
        SV_BM_SCUDB,    // Ballistic Missile Regiment

        SV_AAR_MTLB,    // Air Assault Regiment
        SV_AAR_BMD1,    // Air Assault Regiment
        SV_AAR_BMD2,    // Air Assault Regiment
        SV_AAR_BMD3,    // Air Assault Regiment

        SV_VDV_BMD1,    // Airborne Regiment
        SV_VDV_BMD2,    // Airborne Regiment
        SV_VDV_BMD3,    // Airborne Regiment
        SV_VDV_ART,     // Airborne Artillery Regiment
        SV_VDV_SUP,     // Airborne Support Regiment

        SV_NAV_BTR70,   // Naval INF Regiment
        SV_NAV_BTR80,   // Naval INF Regiment

        SV_ENG,         // Engineer Regiment

        SV_RCR,         // Reconnaissance Regiment
        SV_RCR_AT,      // Anti-Tank Regiment

        SV_ADR_AAA,     // Air Defense Regiment, generic
        SV_ADR_ZSU57,   // Air Defense Regiment
        SV_ADR_ZSU23,   // Air Defense Regiment
        SV_ADR_2K22,    // Air Defense Regiment

        SV_SPSAM_9K31,  // Self-Propelled SAM Regiment
        SV_SAM_S75,     // Surface-to-Air Missile Regiment
        SV_SAM_S125,    // Surface-to-Air Missile Regiment
        SV_SAM_S300,    // Surface-to-Air Missile Regiment

        SV_HEL_MI8AT,    // Helicopter Regiment
        SV_HEL_MI24D,    // Helicopter Regiment
        SV_HEL_MI24V,    // Helicopter Regiment
        SV_HEL_MI28,     // Helicopter Regiment

        SV_GRU,    // Spetsnaz Regiment

        SV_FR_MIG21,     // FGT Regiment
        SV_FR_MIG23,     // FGT Regiment
        SV_FR_MIG25,     // FGT Regiment
        SV_FR_MIG29,     // FGT Regiment
        SV_FR_MIG31,     // FGT Regiment
        SV_FR_SU27,      // FGT Regiment
        SV_FR_SU47,      // FGT Regiment

        SV_MR_MIG27,     // Multirole Regiment

        SV_AR_SU25,     // ATT Regiment
        SV_AR_SU25B,    // ATT Regiment

        SV_AWACS_A50,   // AWACS Regiment
        SV_BR_SU24,     // BMB Regiment
        SV_BR_TU16,     // BMB Regiment
        SV_BR_TU22,     // BMB Regiment
        SV_BR_TU22M3,   // BMB Regiment

        SV_RR_MIG25R,     // Reconnaissance Regiment (Air)

        SV_BASE,   // Base
        SV_AIRB,   // Airbase
        SV_DEPOT,  // Supply Depot

        // US profiles
        US_ARMORED_BDE_M1,         // US Armored Brigade with M1A1 Abrams
        US_ARMORED_BDE_M60A3,      // US Armored Brigade with M60A3 Patton
        US_HEAVY_MECH_BDE_M1,      // US Heavy Mechanized Brigade with M1A1 Abrams
        US_HEAVY_MECH_BDE_M60A3,   // US Heavy Mechanized Brigade with M60A3 Patton
        US_PARA_BDE_82ND,          // US Airborne Brigade (82nd Airborne)
        US_AIR_ASSAULT_BDE_101ST,  // US Air Assault Brigade (101st Airborne)

        US_AVIATION_ATTACK_BDE,    // US Aviation Attack Brigade
        US_ENGINEER_BDE,           // US Engineer Brigade
        US_ARMORED_CAV_SQDN,       // US Armored Cavalry Squadron
        US_ARTILLERY_BDE_M109,     // US Division Artillery
        US_ARTILLERY_BDE_MLRS,     // US Division Rocket Artillery
        US_AIR_DEFENSE_BDE_HAWK,        // US Air Defense Brigade
        US_AIR_DEFENSE_BDE_CHAPARRAL,   // US Air Defense Brigade
        US_FIGHTER_WING_F15,      // US Fighter Wing with F-15C Eagle
        US_FIGHTER_WING_F4,       // US Fighter Wing with F-4 Phantom II
        US_FIGHTER_WING_F16,      // US Multirole Wing with F-16 Fighting Falcon
        US_TACTICAL_WING_A10,     // US Tactical Wing with A-10 Thunderbolt II
        US_BOMBER_WING_F111,      // US Bomber Wing with F-111 Aardvark
        US_BOMBER_WING_F117,      // US Bomber Wing with F-117 Nighthawk
        US_RECON_SQDN_SR71,       // US Reconnaissance Squadron with SR-71 Blackbird
        US_AWACS_E3,              // US AWACS Squadron with E-3 Sentry

        // Federal Republic of Germany (FRG) profiles
        FRG_PANZER_BDE_LEO2,         // FRG Panzer Brigade with Leopard 2
        FRG_PANZER_BDE_LEO1,         // FRG Panzer Brigade with Leopard 1
        FRG_PZGREN_BDE_MARDER,       // FRG Panzergrenadier Brigade
        FRG_ARTILLERY_BDE_M109,      // FRG Artillery Brigade
        FRG_ARTILLERY_BDE_MLRS,      // FRG Rocket Artillery Brigade
        FRG_LUFTLANDE_BDE,           // FRG Luftlande Brigade (Airborne)
        FRG_MOUNTAIN_BDE,            // FRG Mountain Infantry Brigade
        FRG_AIR_DEFENSE_BDE_HAWK,    // FRG Air Defense Brigade
        FRG_AIR_DEFENSE_BDE_ROLAND,  // FRG Air Defense Brigade
        FRG_AIR_DEFENSE_BDE_GEPARD,  // FRG Air Defense Brigade
        FRG_AVIATION_BDE_BO105,      // FRG Aviation Brigade
        FRG_FIGHTER_WING_TORNADO_IDS, // FRG Fighter Wing Tornado IDS

        // UK profiles
        UK_ARMOURED_BDE_CHALLENGER,   //UK Armored Brigade with Challenger 1
        UK_MECHANISED_BDE_WARRIOR,    //UK Mechanized Brigade with Warrior IFV
        UK_INFANTRY_BDE_FV432,        //UK Infantry Brigade with FV432 APC
        UK_AIRMOBILE_BDE,             //UK Airmobile Brigade
        UK_ARTILLERY_BDE,             //UK Artillery Brigade
        UK_AIR_DEFENSE_BDE,           //UK Air Defense Brigade

        // FRA profiles
        FR_BRIGADE_BLINDEE_AMX30,    // French Armored Division with AMX-30
        FR_BRIGADE_INF_MECA_AMX10P,  // French Mechanized Infantry Division with AMX-10P
        FR_BRIGADE_INF_MOTO_VAB,     // French Motorized Infantry Division with VAB
        FR_BRIGADE_PARACHUTISTE,     // French 11e Division Parachutiste
        FR_REGIMENT_ARTILLERIE,      // French Artillery Regiment
        FR_REGIMENT_DEFENSE_AA,      // French Air Defense Regiment
        FR_FIGHTER_WING_MIRAGE2000,  // French Fighter Wing with Mirage 2000
        FR_ATTACK_WING_JAGUAR,       // French Attack Wing with Jaguar

        // Mujahideen profiles
        MJ_INF_GUERRILLA,        // Mujahideen Guerrilla INF
        MJ_SPEC_COMMANDO,        // Mujahideen Special Forces Commando INF
        MJ_CAV_HORSE,            // Mujahideen Horse Cavalry
        MJ_AA,                   // Mujahideen anti-aircraft unit
        MJ_ART_LIGHT_MORTAR,     // Mujahideen Light Mortar unit
        MJ_ART_HEAVY_MORTAR,     // Mujahideen Heavy Mortar unit

        // Regular Arab profiles
        ARAB_TANK_REG_T55,         // Regular Arab Tank Regiment T-55
        ARAB_TANK_REG_T72,         // Regular Arab Tank Regiment T-72
        ARAB_TANK_REG_M60A3,       // Regular Arab Tank Regiment M60A3
        ARAB_TANK_REG_M1,          // Regular Arab Tank Regiment M1
        ARAB_MECH_REG_BMP1,        // Regular Arab Mechanized Infantry Regiment BMP-1
        ARAB_MECH_REG_BTR70,       // Regular Arab Mechanized Infantry Regiment BTR-70
        ARAB_MECH_REG_M2,          // Regular Arab Mechanized Infantry Regiment M2 Bradley
        ARAB_MECH_REG_M113,        // Regular Arab Mechanized Infantry Regiment M113
        ARAB_REG_MOT,              // Regular Arab Motorized Infantry Regiment
        ARAB_REG_INF,              // Regular Arab Infantry Regiment
        ARAB_REG_HVY_ART,          // Regular Arab Heavy Artillery Regiment
        ARAB_REG_LGT_ART,          // Regular Arab Light Artillery Regiment
        ARAB_SPA_REG_2S1,          // Regular Arab Self-Propelled Artillery Regiment 2S1
        ARAB_SPA_REG_M109,         // Regular Arab Self-Propelled Artillery Regiment M109
        ARAB_ROC_REG_BM21,         // Regular Arab Rocket Artillery Regiment BM-21
        ARAB_ROC_REG_MLRS,         // Regular Arab Rocket Artillery Regiment MLRS
        ARAB_RCN_REG_BRDM,         // Regular Arab Reconnaissance Regiment BRDM
        ARAB_RCN_REG_M3,           // Regular Arab Reconnaissance Regiment M3 Cavalry
        ARAB_SPAAA_REG_ZSU23,      // Regular Arab SPAAA Regiment ZSU-23
        ARAB_SAM_REG_S75,          // Regular Arab SAM Regiment S-75
        ARAB_SAM_REG_S125,         // Regular Arab SAM Regiment S-125
        ARAB_SAM_REG_HAWK,         // Regular Arab SAM Regiment HAWK
    }

    #endregion

    #region Facility Enums

    /// <summary>
    /// Represents the type of a base facility in a military or logistical context.
    /// </summary>
    /// <remarks>This enumeration defines the various types of facilities that can exist within a base,  such
    /// as land bases, airbases, and supply depots. It is used to categorize and identify  the purpose or function of a
    /// facility.</remarks>
    public enum FacilityType
    {
        HQ,
        Airbase,
        SupplyDepot
    }

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

    #endregion // Facility Enums

    #region Campaign and Scenario Enums

    /// <summary>
    /// List of scenarios available in the campaign
    /// </summary>
    public enum CampaignScenario
    {
        None,
        Khost_Valley_branch1,
        Panjshir_Valley_branch1,
        Tabriz_branch2,
        Tehran_branch2,
        Bandar_Abbas_branch2,
        Iraq_branch2,
        Saudi_Arabia_branch2,
        Hamburg_branch3,
        Breman_branch3,
        Low_Countries_branch3,
        France_branch3,
        BritishIsles_branch3,
        Yugoslavia_branch4,
        Po_Valley_branch4,
        Central_Italy_branch4,
        Southern_Italy_branch4,
        Harbin_branch5,
        Shenyang_branch5,
        Beijing_branch5,
        South_Korea_branch5,
        Kyushu_branch5,
        Inland_Sea_branch5,
        Tokyo_branch5
    }

    /// <summary>
    /// Weather conditions affecting combat operations and visibility.
    /// </summary>
    public enum WeatherCondition
    {
        Clear = 0,
        Overcast = 1,
        Storm = 2,
        Snow = 3,
        Blizzard = 4,
        Sandstorm = 5
    }

    /// <summary>
    /// Current phase of the battle turn.
    /// </summary>
    public enum BattlePhase
    {
        NotStarted = 0,
        PlayerTurn = 1,
        AITurn = 2,
        EndTurnProcessing = 3,
        BattleComplete = 4
    }

    /// <summary>
    /// Final result of the battle scenario.
    /// </summary>
    public enum BattleResult
    {
        Ongoing = 0,
        DecisiveVictory = 1,
        MajorVictory = 2,
        MinorVictory = 3,
        Draw = 4,
        MinorDefeat = 5,
        MajorDefeat = 6,
        DecisiveDefeat = 7
    }

    /// <summary>
    /// Represents the overall difficulty level of scenarios
    /// </summary>
    public enum DifficultyLevel
    {
        Major,
        Colonel,
        General
    }

    #endregion // Campaign and Scenario Enums

    #region HexMap Enums

    /// <summary>
    /// Pointy-Top hex directions.
    /// </summary>
    public enum HexDirection
    {
        NE,
        E,
        SE,
        SW,
        W,
        NW
    }

    /// <summary>
    /// HexTile border types.
    /// </summary>
    public enum BorderType
    {
        None,
        River,
        Bridge,
        DestroyedBridge,
        PontoonBridge
    }

    /// <summary>
    /// Types of bridges.
    /// </summary>
    public enum BridgeType
    {
        Regular,
        DamagedRegular,
        Pontoon
    }

    /// <summary>
    /// Types of map icons.
    /// </summary>
    public enum MapIconType
    {
        Airbase,
        Fort,
        UrbanSprawl
    }

    /// <summary>
    /// The terrain BaseUnitValueType of the hex.
    /// </summary>
    public enum TerrainType
    {
        Water,
        Clear,
        Forest,
        Rough,
        Marsh,
        Mountains,
        MinorCity,
        MajorCity,
        Impassable
    }

    /// <summary>
    /// Movement cost crossing a hex, based on 20 movement points.
    /// </summary>
    public enum HexMovementCost
    {
        Impassable = 0,
        Water = 1,
        Plains = 1,
        Forest = 2,
        Rough = 3,
        Marsh = 4,
        Mountains = 5,
        MinorCity = 1,
        MajorCity = 1
    }

    /// <summary>
    /// Defense bonus provided by the hex terrain.
    /// </summary>
    public enum HexDefenseBonus
    {
        Water = 0,
        Plains = 0,
        Forest = 1,
        Rough = 2,
        Marsh = 3,
        Mountains = 4,
        MinorCity = 1,
        MajorCity = 3
    }

    /// <summary>
    /// Describes which side control a tile.
    /// </summary>
    public enum TileControl
    {
        Red,  // Friendly.
        Blue, // OPFOR.
        Grey, // Neutral.
        None  // No control.
    }

    /// <summary>
    /// Helps track nationality control within a faction.
    /// </summary>
    public enum DefaultTileControl
    {
        None,
        BE, // Belgium
        DE, // Denmark
        FR, // France
        MJ, // Mujahideen
        NE, // Netherlands
        SV, // Soviet Union
        UK, // United Kingdom
        US, // United States
        GE, // Germany
        CH, // China
        IR, // Iran
        IQ, // Iraq
        SA, // Saudi Arabia
        KW  // Kuwait
    }

    /// <summary>
    /// The types of map configurations.
    /// </summary>
    public enum MapConfig
    {
        Small,
        Large,
        None
    }

    /// <summary>
    /// This enum contains the map themes used in the game.
    /// </summary>
    public enum MapTheme
    {
        MiddleEast,
        Europe,
        China
    }

    /// <summary>
    /// The types of hex outlines.
    /// </summary>
    public enum HexOutlineColor
    {
        Black,
        White,
        Grey
    }

    /// <summary>
    /// The color types for map text elements.
    /// </summary>
    public enum TextColor
    {
        Black,
        White,
        Gold,
        Red,
        Blue,
        Grey,
        Yellow,
        Green,
        Teal
    }

    /// <summary>
    /// The text size for the map element
    /// </summary>
    public enum TextSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// The font weight for the map elelemt
    /// </summary>
    public enum FontWeight
    {
        Light,
        Medium,
        Bold
    }

    #endregion // HexMap Enums

    #region Scene IDs

    public enum SceneID
    {
        MainMenu = 0,
        Scenario_Khost = 1,
        Campaign_Khost = 2
    }

    #endregion // Enumerations

    public class GameData : MonoBehaviour
    {
        #region File Constants

        public const string MANIFEST_EXTENSION = ".manifest";
        public const string MAP_EXTENSION = ".map";
        public const string OOB_EXTENSION = ".oob";
        public const string AII_EXTENSION = ".aii";
        public const string BRF_EXTENSION = ".brf";
        public const string CMP_EXTENSION = ".cmp";

        #endregion // File Constants

        #region Scenario ID Constants

        public const string SCENARIO_ID_MISSION_KHOST = "Mission_Khost";
        public const string SCENARIO_ID_CAMPAIGN_KHOST = "Campaign_Khost";

        #endregion // Scenario ID Constants

        #region Menu/Dialog IDs

        // Main startup scene menu/dialog IDs
        public const int DefaultID = 0;
        public const int MainScene_CoreInterface_ID = 1;
        public const int MainScene_ContinueDialog_ID = 2;
        public const int MainScene_CampaignDialog_ID = 3;
        public const int MainScene_ScenarioDialog_ID = 4;
        public const int MainScene_OptionsDialog_ID = 5;
        public const int MainScene_ExitDialog_ID = 6;

        // Khost scenario scene menu/dialog IDs
        public const int KhostScene_CoreInterface_ID = 7;
        public const int KhostScene_OrdersDialog_ID = 8;

        #endregion

        #region General Constants

        public const int SAVE_VERSION = 1;

        #endregion

        #region CombatUnit Constants

        // Individual Combat Modifiers for unit types.
        public const float ICM_MIN = 0.1f;
        public const float ICM_MAX = 10.0f;
        public const float ICM_DEFAULT = 1.0f;
        public const float ICM_LARGE_UNIT = 1.25f;
        public const float ICM_SMALL_UNIT = 0.75f;

        // CombatUnit constants.
        public const int MAX_HP = 40; // Maximum hit points for a CombatUnit
        public const int MIN_HP = 1;  // Minimum hit points for a CombatUnit
        public const int ZOC_RANGE = 1;  // Zone of Control Range
        public const int MAX_EXP_GAIN_PER_ACTION = 10; // Max XP gain per action

        // Experience level modifiers.
        public const float RAW_XP_MODIFIER = 0.8f; // -20% effectiveness
        public const float GREEN_XP_MODIFIER = 0.9f; // -10% effectiveness
        public const float TRAINED_XP_MODIFIER = 1.0f; // Normal effectiveness
        public const float EXPERIENCED_XP_MODIFIER = 1.1f; // +10% effectiveness
        public const float VETERAN_XP_MODIFIER = 1.2f; // +20% effectiveness
        public const float ELITE_XP_MODIFIER = 1.3f; // +30% effectiveness

        public const float MOBILE_MOVEMENT_BONUS = 2.0f;  // Movement point bonus for Mobile units without MountedProfile
        public const float DEPLOYMENT_ACTION_MOVEMENT_COST = 0.5f;  // Deployment actions cost 50% of max movement
        public const float COMBAT_ACTION_MOVEMENT_COST = 0.25f; // Combat actions cost 25% of max movement
        public const float INTEL_ACTION_MOVEMENT_COST = 0.15f; // Intel actions cost 15% of max movement

        public const float COMBAT_MOD_MOBILE = 0.9f;  // Mobile units get 10% combat malus
        public const float COMBAT_MOD_DEPLOYED = 1.0f;  // Deployed units have no combat modifier
        public const float COMBAT_MOD_HASTY_DEFENSE = 1.1f;  // Hasty defense gives +10% combat bonus
        public const float COMBAT_MOD_ENTRENCHED = 1.2f;  // Entrenched units get +20% combat bonus
        public const float COMBAT_MOD_FORTIFIED = 1.3f;  // Fortified units get +30% combat bonus

        public const float STRENGTH_MOD_FULL = 1.15f; // Full strength units get +15% combat bonus
        public const float STRENGTH_MOD_DEPLETED = 0.75f; // Depleted strength units get -25% combat malus
        public const float STRENGTH_MOD_LOW = 0.4f;  // Low strength units get -60% combat malus

        public const float EFFICIENCY_MOD_STATIC = 0.5f; // Static units get 50% combat malus
        public const float EFFICIENCY_MOD_DEGRADED = 0.7f; // Degraded units get 30% combat malus
        public const float EFFICIENCY_MOD_OPERATIONAL = 0.8f; // NormalOperations units get 20% combat malus
        public const float EFFICIENCY_MOD_FULL = 0.9f; // Full efficiency units get 10% combat malus
        public const float EFFICIENCY_MOD_PEAK = 1.0f; // Peak efficiency units have no combat modifier

        public const float FULL_STRENGTH_FLOOR = 0.8f; // Minimum strength for full effectiveness
        public const float DEPLETED_STRENGTH_FLOOR = 0.5f; // Minimum strength for depleted effectiveness

        // Combat action defaults
        public const int DEFAULT_MOVE_ACTIONS = 1;
        public const int DEFAULT_COMBAT_ACTIONS = 1;
        public const int DEFAULT_INTEL_ACTIONS = 1;
        public const int DEFAULT_DEPLOYMENT_ACTIONS = 1;
        public const int DEFAULT_OPPORTUNITY_ACTIONS = 1;

        // Unit supply constants.
        public const float LOW_SUPPLY_THRESHOLD = 1f;    // Threshold for low supply warning
        public const float CRITICAL_SUPPLY_THRESHOLD = 0.5f;  // Threshold for critical supply warning
        public const float COMBAT_STATE_SUPPLY_TRANSITION_COST = 0.25f; // Supply cost for state transitions.
        public const float COMBAT_ACTION_SUPPLY_COST = 1f;    // Supply cost for combat actions.
        public const float COMBAT_ACTION_SUPPLY_THRESHOLD = 2f;    // Threshold for combat action supply cost.
        public const float MOVE_ACTION_SUPPLY_THRESHOLD = 1.5f;  // Threshold for move action supply cost.
        public const float MOVE_ACTION_SUPPLY_COST = 0.2f;  // Supply cost for move actions.
        public const float INTEL_ACTION_SUPPLY_COST = 0.25f; // Supply cost for intel actions.
        public const float OPPORTUNITY_ACTION_SUPPLY_THRESHOLD = 1.5f;  // Threshold for opportunity action supply cost.
        public const float OPPORTUNITY_ACTION_SUPPLY_COST = 0.5f;  // Supply cost for opportunity actions.

        // Intel error margins
        public const float MIN_INTEL_ERROR = 4f;   // Minimum intel error margin
        public const float MODERATE_INTEL_ERROR = 8f;   // Maximum intel error margin
        public const float MAX_INTEL_ERROR = 12f;  // Maximum intel error margin

        #endregion // CombatUnit Constants

        #region WeaponSystem Constants

        /* Note: Attacker targets defenders hardness/softness type. Defender targets attackers hardness/softness type */

        // WeaponSystem constants.
        public const int MAX_COMBAT_VALUE = 25;
        public const int MIN_COMBAT_VALUE = 1;
        public const float MAX_RANGE = 100.0f;
        public const float MIN_RANGE = 0.0f;

        // Movement constants for WeaponSystems,in movement points.
        public const int STATIC_UNIT = 0;
        public const int FOOT_UNIT = 4;
        public const int MOT_UNIT = 8;
        public const int MECH_UNIT = 10;
        public const int CAVALRY_UNIT = 10;
        public const int NAVAL_UNIT = 10;
        public const int HELO_UNIT = 24;
        public const int FIXEDWING_UNIT = 100;

        // Ground defense against air attack.
        public const int GROUND_DEFENSE_LIGHTARMOR = 5;  // Base ground defense for APCs units
        public const int GROUND_DEFENSE_INFANTRY = 6;  // Base ground defense for Infantry units
        public const int GROUND_DEFENSE_ARMOR = 8;  // Base ground defense for tank units
        public const int GROUND_DEFENSE_SAM = 8;  // Base ground defense for SAM units
        public const int GROUND_DEFENSE_HELO = 10;  // Base air defense for helo units
        public const int GROUND_DEFENSE_AAA = 12;  // Base ground defense for AAA units


        // Standard spotting range values
        public const float BASE_UNIT_SPOTTING_RANGE = 2;
        public const float RECON_UNIT_SPOTTING_RANGE = 3;
        public const float BASE_AAA_SPOTTING_RANGE = 3;
        public const float FACILITY_SPOTTING_RANGE = 4;
        public const float BASE_SAM_SPOTTING_RANGE = 6;
        public const float INTEL_UNIT_SPOTTING_RANGE = 6;

        // Standard primary range values.
        public const float PRIMARY_RANGE_DEFAULT = 1;

        // Standard indirect range values.
        public const float INDIRECT_RANGE_DEFAULT = 0;
        public const float INDIRECT_RANGE_120MM = 4;
        public const float INDIRECT_RANGE_155MM = 5;
        public const float INDIRECT_RANGE_203MM = 6;
        public const float INDIRECT_RANGE_ROC_SR = 4;
        public const float INDIRECT_RANGE_ROC_MR = 6;
        public const float INDIRECT_RANGE_ROC_LR = 10;

        // Standard anti-air range values.
        public const float INDIRECT_RANGE_AAA = 3;
        public const float INDIRECT_RANGE_SAM = 6;

        // Standard infantry values
        public const int BASE_INF_HARD_ATTACK = 5;
        public const int BASE_INF_HARD_DEFENSE = 7;

        public const int BASE_INF_SOFT_ATTACK = 7;
        public const int BASE_INF_SOFT_DEFENSE = 8;

        // Standard APC values
        public const int BASE_APC_HARD_ATTACK = 3;
        public const int BASE_APC_HARD_DEFENSE = 4;
        public const int BASE_APC_SOFT_ATTACK = 6;
        public const int BASE_APC_SOFT_DEFENSE = 7;

        // Standard IFV values
        public const int BASE_IFV_HARD_ATTACK = 4;
        public const int BASE_IFV_HARD_DEFENSE = 4;
        public const int BASE_IFV_SOFT_ATTACK = 7;
        public const int BASE_IFV_SOFT_DEFENSE = 7;

        // Standard tank soft combat values
        public const int BASE_TANK_SOFT_ATTACK = 8;
        public const int BASE_TANK_SOFT_DEFENSE = 6;

        // Gen1 standard tank values
        public const int GEN1_TANK_HARD_ATTACK = 7;
        public const int GEN1_TANK_HARD_DEFENSE = 5;

        // Gen2 standard tank values
        public const int GEN2_TANK_HARD_ATTACK = 10;
        public const int GEN2_TANK_HARD_DEFENSE = 8;

        // Gen3 standard tank values
        public const int GEN3_TANK_HARD_ATTACK = 13;
        public const int GEN3_TANK_HARD_DEFENSE = 11;

        // Gen4 standard tank values
        public const int GEN4_TANK_HARD_ATTACK = 16;
        public const int GEN4_TANK_HARD_DEFENSE = 14;

        // Standard artillery values
        public const int BASE_ARTY_HARD_ATTACK = 5;
        public const int BASE_ARTY_HARD_DEFENSE = 5;
        public const int BASE_ARTY_SOFT_ATTACK = 9;
        public const int BASE_ARTY_SOFT_DEFENSE = 5;

        // AAA standard values
        public const int BASE_AAA_HARD_ATTACK = 4;
        public const int BASE_AAA_HARD_DEFENSE = 4;
        public const int BASE_AAA_SOFT_ATTACK = 9;
        public const int BASE_AAA_SOFT_DEFENSE = 6;
        public const int BASE_AAA_GROUND_AIR_ATTACK = 9;

        // SAM standard values
        public const int BASE_SAM_HARD_ATTACK = 1;
        public const int BASE_SAM_HARD_DEFENSE = 3;
        public const int BASE_SAM_SOFT_ATTACK = 1;
        public const int BASE_SAM_SOFT_DEFENSE = 3;
        public const int BASE_SAM_GROUND_AIR_ATTACK = 10;

        // Standard helo values
        public const int BASE_HEL_HARD_ATTACK = 7;
        public const int BASE_HEL_HARD_DEFENSE = 6;
        public const int BASE_HEL_SOFT_ATTACK = 10;
        public const int BASE_HEL_SOFT_DEFENSE = 7;

        // Standard fixed wing values, early generation
        public const int EARLY_FGT_DOGFIGHT = 8;
        public const int EARLY_FGT_MANEUVER = 9;
        public const int EARLY_FGT_TOPSPEED = 10;
        public const int EARLY_FGT_SURVIVE = 6;

        // Standard fixed wing values, mid generation
        public const int MID_FGT_DOGFIGHT = 10;
        public const int MID_FGT_MANEUVER = 11;
        public const int MID_FGT_TOPSPEED = 10;
        public const int MID_FGT_SURVIVE = 7;

        // Standard fixed wing values, late generation
        public const int LATE_FGT_DOGFIGHT = 12;
        public const int LATE_FGT_MANEUVER = 12;
        public const int LATE_FGT_TOPSPEED = 10;
        public const int LATE_FGT_SURVIVE = 9;

        // Ordinance Loads
        public const int SMALL_AC_LOAD = 6;  // Small air-to-ground load
        public const int MEDIUM_AC_LOAD = 9;  // Medium air-to-ground load
        public const int LARGE_AC_LOAD = 12;  // Large air-to-ground load
        public const int XLARGE_AC_LOAD = 16;  // Extra large air-to-ground load

        // Spotting in the AC context express ability for long range engagements
        public const int AC_SPOTTING_BASIC = 1;
        public const int AC_SPOTTING_ENHANCED = 2;
        public const int AC_SPOTTING_ADVANCED = 3;
        public const int AC_SPOTTING_SUPERIOR = 4;

        // Air unit prestige costs
        public const int PRESTIGE_TIER_FREE = 1;
        public const int PRESTIGE_TIER_0 = 25;
        public const int PRESTIGE_TIER_1 = 50;
        public const int PRESTIGE_TIER_2 = 75;
        public const int PRESTIGE_TIER_3 = 100;
        public const int PRESTIGE_TIER_4 = 125;
        public const int PRESTIGE_TIER_5 = 150;

        // Standard attack aircraft values
        public const int AC_ATTACK_DOGFIGHT = 4;
        public const int AC_ATTACK_MANEUVER = 4;
        public const int AC_ATTACK_TOPSPEED = 7;
        public const int AC_ATTACK_SURVIVE = 10;

        // Standard bomber values
        public const int AC_BOMBER_DOGFIGHT = 1;
        public const int AC_BOMBER_MANEUVER = 3;
        public const int AC_BOMBER_TOPSPEED = 10;
        public const int AC_BOMBER_SURVIVE = 8;

        // High mach aircraft speeds
        public const int AC_HIGHSPEED_RUSSIAN = 17;
        public const int AC_HIGHSPEED_WESTERN = 21;

        // Standard fixed wing values, attack aircraft
        public const int GROUND_ATTACK_NA = 0;
        public const int GROUND_ATTACK_TIER_0 = 6;
        public const int GROUND_ATTACK_TIER_1 = 9;
        public const int GROUND_ATTACK_TIER_2 = 12;
        public const int GROUND_ATTACK_TIER_3 = 15;

        // Standard values for truck transport units
        public const int TRUCK_HARD_ATTACK = 3;
        public const int TRUCK_HARD_DEFENSE = 3;
        public const int TRUCK_SOFT_ATTACK = 3;
        public const int TRUCK_SOFT_DEFENSE = 3;

        // Standard values for facilities
        public const int BASE_HARD_ATTACK = 4;
        public const int BASE_HARD_DEFENSE = 6;
        public const int BASE_SOFT_ATTACK = 6;
        public const int BASE_SOFT_DEFENSE = 7;

        #endregion

        #region Leader Constants

        // Leader LeaderID generation
        public const string LEADER_ID_PREFIX = "LDR";
        public const int LEADER_ID_LENGTH = 8; // LDR + 5 random chars

        // Leader validation bounds
        public const int MIN_REPUTATION = 0;
        public const int MAX_REPUTATION = 9999;
        public const int MAX_LEADER_NAME_LENGTH = 50;
        public const int MIN_LEADER_NAME_LENGTH = 2;

        // Reputation constants.
        public const int REP_COST_FOR_SENIOR_PROMOTION = 100;
        public const int REP_COST_FOR_TOP_PROMOTION = 250;

        // Tiered skill XP costs.
        public const int TIER1_REP_COST = 60;
        public const int TIER2_REP_COST = 80;
        public const int TIER3_REP_COST = 120;
        public const int TIER4_REP_COST = 180;
        public const int TIER5_REP_COST = 260;

        // Skill cost validation bounds
        public const int MIN_SKILL_REP_COST = 50;
        public const int MAX_SKILL_REP_COST = 500;

        // Command and Operation bonuses (typically +1 for actions)
        public const int COMMAND_BONUS_VAL = 1;
        public const int DEPLOYMENT_ACTION_BONUS_VAL = 1;
        public const int MOVEMENT_ACTION_BONUS_VAL = 1;
        public const int COMBAT_ACTION_BONUS_VAL = 1;
        public const int OPPORTUNITY_ACTION_BONUS_VAL = 1;

        // Combat rating bonuses.
        public const int HARD_ATTACK_BONUS_VAL = 5;
        public const int HARD_DEFENSE_BONUS_VAL = 5;
        public const int SOFT_ATTACK_BONUS_VAL = 5;
        public const int SOFT_DEFENSE_BONUS_VAL = 5;
        public const int AIR_ATTACK_BONUS_VAL = 5;
        public const int AIR_DEFENSE_BONUS_VAL = 5;

        // Bonus value validation bounds
        public const int MIN_COMBAT_BONUS = 1;
        public const int MAX_COMBAT_BONUS = 10;
        public const int MIN_ACTION_BONUS = 1;
        public const int MAX_ACTION_BONUS = 3;

        // Spotting and range bonuses.
        public const int SMALL_SPOTTING_RANGE_BONUS_VAL = 1;
        public const int MEDIUM_SPOTTING_RANGE_BONUS_VAL = 2;
        public const int LARGE_SPOTTING_RANGE_BONUS_VAL = 3;
        public const int INDIRECT_RANGE_BONUS_VAL = 1;

        // Silouette bonuses.
        public const int SMALL_SILHOUETTE_REDUCTION_VAL = 1;
        public const int MEDIUM_SILHOUETTE_REDUCTION_VAL = 2;
        public const int MAX_SILHOUETTE_REDUCTION_VAL = 3;

        // General multiplier bounds (for any positive effect)
        public const float MIN_MULTIPLIER = 0.01f;    // 1% of original value (extreme reduction)
        public const float MAX_MULTIPLIER = 10.0f;    // 10x original value (extreme boost)

        // Common decrease modifiers (what you multiply by to get the reduction)
        public const float TINY_DECREASE_MULT = 0.99f;     // 1% decrease (keep 99%)
        public const float SMALL_DECREASE_MULT = 0.90f;    // 10% decrease (keep 90%) 
        public const float MEDIUM_DECREASE_MULT = 0.80f;   // 20% decrease (keep 80%)
        public const float LARGE_DECREASE_MULT = 0.50f;    // 50% decrease (keep 50%)
        public const float HUGE_DECREASE_MULT = 0.01f;     // 99% decrease (keep 1%)

        // Common increase modifiers (what you multiply by to get the boost)
        public const float TINY_INCREASE_MULT = 1.01f;     // 1% increase (101% of original)
        public const float SMALL_INCREASE_MULT = 1.10f;    // 10% increase (110% of original)
        public const float MEDIUM_INCREASE_MULT = 1.25f;   // 25% increase (125% of original)
        public const float LARGE_INCREASE_MULT = 1.50f;    // 50% increase (150% of original)
        public const float HUGE_INCREASE_MULT = 2.00f;     // 100% increase (200% of original)

        // Validation: ensure multipliers stay within sane bounds
        public static bool IsValidMultiplier(float multiplier)
        {
            return multiplier >= MIN_MULTIPLIER && multiplier <= MAX_MULTIPLIER;
        }

        // Helper: convert percentage to multiplier
        public static float PercentToMultiplier(float percent)
        {
            return 1.0f + (percent / 100.0f);
        }

        // Helper: convert multiplier to percentage change
        public static float MultiplierToPercent(float multiplier)
        {
            return (multiplier - 1.0f) * 100.0f;
        }

        // INF doctrine multiplier.
        public const float RTO_MOVE_MULT = 0.8f;           // 20% movement cost reduction for RTOs.

        // Politically connected bonuses and multipliers.
        public const int REPLACEMENT_XP_LEVEL_VAL = 1;    // Replacements get +1 XP level.
        public const float SUPPLY_ECONOMY_MULT = 0.8f; // Supply consumption gets 20% cost reduction.
        public const float PRESTIGE_COST_MULT = 0.7f; // Unit upgrades get 30% price reduction.

        // EngineeringSpecialization specific
        public const float RIVER_CROSSING_MOVE_MULT = 0.5f; // X% movement cost reduction
        public const float RIVER_ASSAULT_MULT = 1.4f; // X% combat bonus when attacking across a river.

        // Special forces bonuses
        public const float TMASTERY_MOVE_MULT = 0.8f; // X% movement cost reduction in non-clear terrain.
        public const float INFILTRATION_MULT = 0.5f; // X% ZOC penalty reduction
        public const float AMBUSH_BONUS_MULT = 1.5f; // X% combat bonus

        // Combined arms bonus.
        public const float NIGHT_COMBAT_MULT = 1.25f;// X% combat bonus at night

        /// <summary>
        /// Types of actions that can award reputation to leaders
        /// </summary>
        public enum ReputationAction
        {
            Move,
            MountDismount,
            IntelGather,
            Combat,
            AirborneJump,
            ForcedRetreat,
            UnitDestroyed
        }

        // Base REP gain per action type
        public const int REP_PER_MOVE_ACTION = 1;              // Routine movement
        public const int REP_PER_MOUNT_DISMOUNT = 1;           // Mounting/dismounting transport
        public const int REP_PER_INTEL_GATHER = 2;             // Intelligence gathering (requires positioning)
        public const int REP_PER_COMBAT_ACTION = 3;            // Attacking (risk involved)
        public const int REP_PER_AIRBORNE_JUMP = 3;            // Paratrooper insertion (high risk)
        public const int REP_PER_FORCED_RETREAT = 5;           // Causing enemy to retreat (tactical success)
        public const int REP_PER_UNIT_DESTROYED = 8;           // Destroying enemy unit (major victory)

        // REP action validation bounds
        public const int MIN_REP_PER_ACTION = 1;
        public const int MAX_REP_PER_ACTION = 15;

        // Bonus REP multipliers
        public const float REP_EXPERIENCE_MULTIPLIER = 1.5f;   // Veteran/Elite units gain more REP
        public const float REP_ELITE_DIFFICULTY_BONUS = 2.0f;  // Bonus for destroying elite enemy units

        // REP multiplier bounds
        public const float MIN_REP_MULTIPLIER = 1.0f;
        public const float MAX_REP_MULTIPLIER = 3.0f;

        #endregion // Leader Constants

        #region Facility Constants

        // Maximum stockpile capacities by depot size
        public static readonly Dictionary<DepotSize, float> MaxStockpileBySize = new()
        {
            { DepotSize.Small, 30f },
            { DepotSize.Medium, 50f },
            { DepotSize.Large, 80f },
            { DepotSize.Huge, 110f }
        };

        // Supply generation rates by level
        public static readonly Dictionary<SupplyGenerationRate, float> GenerationRateValues = new()
        {
            { SupplyGenerationRate.Minimal, 10.0f },
            { SupplyGenerationRate.Basic, 20.0f },
            { SupplyGenerationRate.Standard, 40.0f },
            { SupplyGenerationRate.Enhanced, 80.0f }
        };

        // Supply projection ranges in hexes
        public static readonly Dictionary<SupplyProjection, int> ProjectionRangeValues = new()
        {
            { SupplyProjection.Local, 4 },
            { SupplyProjection.Extended, 8 },
            { SupplyProjection.Regional, 12 },
            { SupplyProjection.Strategic, 16 }
        };

        // Amount any unit can stockpile
        public const float MaxDaysSupplyDepot = 100f;       // Max supply a depot can carry
        public const float MaxDaysSupplyUnit = 7f;          // Max supply a unit can carry

        // Supply efficiency multipliers
        public const float DISTANCE_EFF_MULT = 0.4f;
        public const float ZOC_EFF_MULT = 0.3f;

        // Constants for special abilities
        public const int AirSupplyMaxRange = 16;
        public const int NavalSupplyMaxRange = 12;

        // Efficientcy multipliers for base operations, both Airbase and Supply Depot
        public const float BASE_CAPACITY_LVL5 = 1f;    // Full operations capacity of an airbase
        public const float BASE_CAPACITY_LVL4 = 0.75f; // 75% operations capacity
        public const float BASE_CAPACITY_LVL3 = 0.5f;  // 50% operations capacity
        public const float BASE_CAPACITY_LVL2 = 0.25f; // 25% operations capacity
        public const float BASE_CAPACITY_LVL1 = 0f;    // 0% operations capacity

        // Base damage constants
        public const int MAX_DAMAGE = 100;
        public const int MIN_DAMAGE = 0;

        // Airbase constants
        public const int MAX_AIR_UNITS = 4;        // Max air units that can be attached to an airbase.

        #endregion // Facility Constants

        #region HexMap Constants

        // Hex grid orientation.
        public const bool IsPointyTop = true;

        // Hex size constant.
        public const int HexSize = 256;

        // Rendering constants.
        public const float PixelScaleX = 1;
        public const float PixelScaleY = 1;
        public const int SpritePPU = 256;

        // Hex grid size constants.
        public const int SmallHexWidth = 32;
        public const int SmallHexHeight = 21;
        public const int LargeHexWidth = 32;
        public const int LargeHexHeight = 42;

        /// <summary>
        /// Gets vertical spacing for the hex grid.
        /// </summary>
        /// <returns></returns>
        public static float GetVerticalSpacing()
        {
            return HexSize * 0.75f;
        }

        // Version of the .map file format.
        public const int CurrentMapDataVersion = 1;

        #endregion // HexMap Constants
    }
}
