using System;

namespace HammerAndSickle.Models
{
    #region CombatUnit Enums

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
        ASF,    // Air Superiority ASF
        MRF,    // Multi-role ASF
        ATT,    // ATT aircraft
        BMB,    // BMB
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
        Embarked = CUConstants.Embarked,
        Mobile = CUConstants.Mobile,
        Deployed = CUConstants.Deployed,
        HastyDefense = CUConstants.HastyDefense,
        Entrenched = CUConstants.Entrenched,
        Fortified = CUConstants.Fortified
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
        Average   = 0,
        Good      = 1,
        Superior  = 2,
        Genius    = 3
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
        CommandTier1,         // +x to Command
        SeniorPromotion,      // Boolean, promotion to SeniorGrade.
        CommandTier2,         // +x additional command
        TopPromotion,         // Boolean, promotion to TopGrade
        CommandTier3,         // +x additional command

        // PoliticallyConnectedFoundation
        EmergencyResupply,    // Boolean, one free emergency resupply per scenario.
        SupplyConsumption,    // Supplies are consumed at reduced rate.
        NVG,                  // Boolean, upgrade unit to latest gen NVG.
        ReplacementXP,        // Unit gets better replacements (x bonus levels).
        PrestigeCost,         // Unit is cheaper to upgrade (x% discount).

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
        HEL_MI8T,   // Transport Helicopter
        HEL_MI8AT,
        HEL_MI24D,
        HEL_MI24V,
        HEL_MI28,
        TRA_AN12,   // Transport Aircraft
        AWACS_A50,
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
        RCNA_MIG25R,
        
        TRA_NAVAL,   // Transport Naval

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
        AWACS_E3,
        ASF_F15,
        ASF_F4,
        MRF_F16,
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
        MRF_TORNADO_IDS,

        // UK
        TANK_CHALLENGER1,
        IFV_WARRIOR,
        APC_FV432,
        RCN_SCIMITAR,
        SAM_RAPIER,
        HEL_LYNX,
        MRF_TORNADO_GR1,

        // France
        TANK_AMX30,
        IFV_AMX10P,
        APC_VAB,
        RCN_ERC90,
        SPA_AUF1,
        SPSAM_ROLAND,
        ASF_MIRAGE2000,
        ATT_JAGUAR,

        // Generic types
        GENERIC_AAA,
        GENERIC_ART_LIGHT,
        GENERIC_ART_HEAVY,
        GENERIC_MANPAD,
        GENERIC_ATGM,
        GENERIC_LANDBASE,
        GENERIC_AIRBASE,
        GENERIC_SUPPLYDEPOT,

        // Shared profiles for infantry
        INF_REG,    // Regular INF
        INF_AB,     // Airborne INF
        INF_AM,     // Air Mobile INF
        INF_MAR,    // Marine INF
        INF_SPEC,   // Special Forces INF
        INF_ENG,    // Engineer INF

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
        ASF,
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

        SV_NAV_T55,     // Naval INF Regiment
        SV_NAV_T72,     // Naval INF Regiment
        SV_NAV_T80,     // Naval INF Regiment

        SV_ENG,         // Engineer Regiment

        SV_RCR,         // Reconnaissance Regiment
        SV_RCR_AT,      // Anti-Tank Regiment

        SV_ADR_AAA,     // Air Defense Regiment, generic
        SV_ADR_ZSU57,   // Air Defense Regiment
        SV_ADR_ZSU23,   // Air Defense Regiment
        SV_ADR_2K22,    // Air Defense Regiment

        SV_SAM_S75,     // Surface-to-Air Missile Regiment
        SV_SAM_S125,    // Surface-to-Air Missile Regiment
        SV_SAM_S300,    // Surface-to-Air Missile Regiment

        SV_HEL_MI8AT,    // Helicopter Regiment
        SV_HEL_MI24D,    // Helicopter Regiment
        SV_HEL_MI24V,    // Helicopter Regiment
        SV_HEL_MI28,     // Helicopter Regiment

        SV_GRU,    // Spetsnaz Regiment

        SV_FR_MIG21,     // ASF Regiment
        SV_FR_MIG23,     // ASF Regiment
        SV_FR_MIG25,     // ASF Regiment
        SV_FR_MIG29,     // ASF Regiment
        SV_FR_MIG31,     // ASF Regiment
        SV_FR_SU27,      // ASF Regiment
        SV_FR_SU47,      // ASF Regiment
        
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
}