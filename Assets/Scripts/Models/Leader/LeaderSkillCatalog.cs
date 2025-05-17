using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Defines a leader skill effect, including the type and value of the bonus.
    /// </summary>
    public class SkillEffect
    {
        /// <summary>
        /// The type of bonus this effect provides.
        /// </summary>
        public SkillBonusType BonusType { get; }

        /// <summary>
        /// The value of the bonus. Interpretation depends on BonusType:
        /// - For numeric bonuses (like Command, Attack), this is the addition amount
        /// - For multiplier bonuses (like SupplyConsumption), this is the multiplier
        /// - For boolean effects, true = 1.0f, false = 0.0f
        /// </summary>
        public float BonusValue { get; }

        /// <summary>
        /// Description of this specific effect
        /// </summary>
        public string EffectDescription { get; }

        /// <summary>
        /// Whether this bonus represents a boolean capability rather than a numeric value
        /// </summary>
        public bool IsBoolean =>
            BonusType == SkillBonusType.Breakthrough ||
            BonusType == SkillBonusType.OpportunityAction ||
            BonusType == SkillBonusType.ShootAndScoot ||
            BonusType == SkillBonusType.AirMobile ||
            BonusType == SkillBonusType.AirMobileAssault ||
            BonusType == SkillBonusType.AirMobileElite ||
            BonusType == SkillBonusType.ImpromptuPlanning ||
            BonusType == SkillBonusType.AirborneAssault ||
            BonusType == SkillBonusType.AirborneElite ||
            BonusType == SkillBonusType.UndergroundBunker ||
            BonusType == SkillBonusType.RiverCrossing ||
            BonusType == SkillBonusType.RiverAssault ||
            BonusType == SkillBonusType.BridgeBuilding ||
            BonusType == SkillBonusType.FieldFortification ||
            BonusType == SkillBonusType.TerrainMastery ||
            BonusType == SkillBonusType.InfiltrationMovement ||
            BonusType == SkillBonusType.ConcealedPositions ||
            BonusType == SkillBonusType.AmbushTactics ||
            BonusType == SkillBonusType.SignalDecryption ||
            BonusType == SkillBonusType.SpaceAssets ||
            BonusType == SkillBonusType.ElectronicWarfare ||
            BonusType == SkillBonusType.PatternRecognition ||
            BonusType == SkillBonusType.UrbanCombat ||
            BonusType == SkillBonusType.RoughTerrain ||
            BonusType == SkillBonusType.NightCombat ||
            BonusType == SkillBonusType.EmergencyResupply ||
            BonusType == SkillBonusType.NVG ||
            BonusType == SkillBonusType.SeniorPromotion ||
            BonusType == SkillBonusType.TopPromotion;

        public SkillEffect(SkillBonusType bonusType, float bonusValue, string effectDescription = null)
        {
            BonusType = bonusType;
            BonusValue = bonusValue;
            EffectDescription = effectDescription ?? GetDefaultDescription();
        }

        /// <summary>
        /// Generate a default description based on the bonus type and value
        /// </summary>
        private string GetDefaultDescription()
        {
            if (IsBoolean && BonusValue > 0)
            {
                return $"Grants {BonusType} capability.";
            }
            else if (BonusType == SkillBonusType.SupplyConsumption ||
                    BonusType == SkillBonusType.PrestigeCost)
            {
                // These are reduction multipliers
                float reductionPercent = (1.0f - BonusValue) * 100f;
                return $"Reduces {BonusType} by {reductionPercent:0}%.";
            }
            else
            {
                return $"+{BonusValue} to {BonusType}.";
            }
        }
    }

    

    /// <summary>
    /// Defines a leader skill with all its attributes, requirements, and effects
    /// </summary>
    public class SkillDefinition
    {
        // Core identification
        public Enum SkillEnumValue { get; }
        public string Name { get; }
        public int XPCost { get; }
        public string Description { get; }

        // Classification
        public SkillBranch Branch { get; }
        public SkillTier Tier { get; }
        public CommandGrade RequiredGrade { get; }

        // Requirements and restrictions
        public List<Enum> Prerequisites { get; }
        public List<Enum> MutuallyExclusive { get; }

        // Effects
        public List<SkillEffect> Effects { get; }

        // Visual/UI
        public string IconPath { get; }

        /// <summary>
        /// Creates a comprehensive skill definition
        /// </summary>
        public SkillDefinition(
            Enum skillEnumValue,
            string name,
            int xpCost,
            SkillBranch branch,
            SkillTier tier,
            string description,
            CommandGrade requiredGrade = CommandGrade.JuniorGrade,
            List<Enum> prerequisites = null,
            List<Enum> mutuallyExclusive = null,
            List<SkillEffect> effects = null,
            string iconPath = null)
        {
            SkillEnumValue = skillEnumValue;
            Name = name;
            XPCost = xpCost;
            Branch = branch;
            Tier = tier;
            Description = description;
            RequiredGrade = requiredGrade;
            Prerequisites = prerequisites ?? new List<Enum>();
            MutuallyExclusive = mutuallyExclusive ?? new List<Enum>();
            Effects = effects ?? new List<SkillEffect>();
            IconPath = iconPath;
        }

        /// <summary>
        /// Convenience constructor for a skill with a single effect
        /// </summary>
        public SkillDefinition(
            Enum skillEnumValue,
            string name,
            int xpCost,
            SkillBranch branch,
            SkillTier tier,
            string description,
            SkillBonusType bonusType,
            float bonusValue,
            CommandGrade requiredGrade = CommandGrade.JuniorGrade,
            List<Enum> prerequisites = null,
            List<Enum> mutuallyExclusive = null,
            string iconPath = null)
            : this(
                skillEnumValue,
                name,
                xpCost,
                branch,
                tier,
                description,
                requiredGrade,
                prerequisites,
                mutuallyExclusive,
                new List<SkillEffect> { new SkillEffect(bonusType, bonusValue) },
                iconPath)
        {
        }

        /// <summary>
        /// Convenience constructor for a boolean capability skill
        /// </summary>
        public SkillDefinition(
            Enum skillEnumValue,
            string name,
            int xpCost,
            SkillBranch branch,
            SkillTier tier,
            string description,
            SkillBonusType booleanCapability,
            CommandGrade requiredGrade = CommandGrade.JuniorGrade,
            List<Enum> prerequisites = null,
            List<Enum> mutuallyExclusive = null,
            string iconPath = null)
            : this(
                skillEnumValue,
                name,
                xpCost,
                branch,
                tier,
                description,
                booleanCapability,
                1.0f, // 1.0 = true for boolean effects
                requiredGrade,
                prerequisites,
                mutuallyExclusive,
                iconPath)
        {
        }

        /// <summary>
        /// Gets a complete description of the skill including all effects
        /// </summary>
        public string GetFullDescription()
        {
            string result = $"{Description}\n\nEffects:";
            foreach (var effect in Effects)
            {
                result += $"\n• {effect.EffectDescription}";
            }

            if (RequiredGrade > CommandGrade.JuniorGrade)
            {
                result += $"\n\nRequires {RequiredGrade} rank.";
            }

            if (Prerequisites.Count > 0)
            {
                result += "\n\nPrerequisites:";
                foreach (var prereq in Prerequisites)
                {
                    string prereqName = LeaderSkillCatalog.GetSkillName(prereq);
                    result += $"\n• {prereqName}";
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the primary (first) effect of this skill, or null if no effects
        /// </summary>
        public SkillEffect GetPrimaryEffect()
        {
            return Effects.Count > 0 ? Effects[0] : null;
        }
    }

    /// <summary>
    /// LeaderSkillCatalog is a static repository of all skill definitions in the game.
    /// 
    /// This class provides the following functionality:
    /// - Central storage of all skill definitions
    /// - Methods to retrieve skills by enum value
    /// - Initialization of all skill tree branches
    /// - Utility methods for skill descriptions and properties
    /// 
    /// The catalog is organized into distinct skill branches, each with its own initialization method.
    /// Skills are tiered from 1-4 with increasing XP costs and prerequisites.
    /// Each skill provides one or more effects or bonuses to unit capabilities.
    /// </summary>
    public static class LeaderSkillCatalog
    {
        #region Fields

        // Main storage for all skill definitions
        private static readonly Dictionary<Enum, SkillDefinition> AllSkills = new Dictionary<Enum, SkillDefinition>();

        #endregion

        #region Initialization

        /// <summary>
        /// Static constructor to initialize all skill trees when first accessed
        /// </summary>
        static LeaderSkillCatalog()
        {
            InitializeLeadershipSkills();
            InitializeArmoredWarfareSkills();
            InitializeInfantryDoctrineSkills();
            InitializeArtilleryDoctrineSkills();
            InitializeAirDefenseDoctrineSkills();
            InitializeAirborneDoctrineSkills();
            InitializeAirMobileDoctrineSkills();
            InitializeIntelligenceDoctrine();
            InitializeSignalIntelligenceSpecialization();
            InitializeEngineeringSkills();
            InitializeSpecialForcesDoctrineSkills();
            InitializePoliticallyConnectedSkills();
        }

        /// <summary>
        /// Initialize leadership skills - command progression and promotions
        /// </summary>
        private static void InitializeLeadershipSkills()
        {
            // Tier 1: Junior Officer Training
            AddSkill(new SkillDefinition(
                LeadershipFoundation.JuniorOfficerTraining_CommandTier1,
                "Junior Officer Training",
                CUConstants.TIER1_XP_COST,
                SkillBranch.LeadershipFoundation,
                SkillTier.Tier1,
                "Basic command training for junior officers enhances leadership capabilities.",
                SkillBonusType.CommandTier1,
                CUConstants.COMMAND_BONUS_VAL
            ));

            // Tier 2: Promotion to Senior Grade
            AddSkill(new SkillDefinition(
                LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion,
                "Promotion to Senior Grade",
                CUConstants.XP_COST_FOR_SENIOR_PROMOTION, // Special cost
                SkillBranch.LeadershipFoundation,
                SkillTier.Tier2,
                "Promotion to Senior Grade increases command authority and unlocks advanced skills.",
                SkillBonusType.SeniorPromotion, // Boolean capability
                CommandGrade.JuniorGrade,
                new List<Enum> { LeadershipFoundation.JuniorOfficerTraining_CommandTier1 }
            ));

            // Tier 3: Senior Officer Training
            AddSkill(new SkillDefinition(
                LeadershipFoundation.SeniorOfficerTraining_CommandTier2,
                "Senior Officer Training",
                CUConstants.TIER3_XP_COST,
                SkillBranch.LeadershipFoundation,
                SkillTier.Tier3,
                "Advanced command training for senior officers substantially improves leadership abilities.",
                SkillBonusType.CommandTier2,
                CUConstants.COMMAND_BONUS_VAL,
                CommandGrade.SeniorGrade,
                new List<Enum> { LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion }
            ));

            // Tier 4: Promotion to Top Grade
            AddSkill(new SkillDefinition(
                LeadershipFoundation.PromotionToTopGrade_TopPromotion,
                "Promotion to Top Grade",
                CUConstants.XP_COST_FOR_TOP_PROMOTION, // Special cost
                SkillBranch.LeadershipFoundation,
                SkillTier.Tier4,
                "Promotion to Top Grade grants the highest level of command authority and strategic influence.",
                SkillBonusType.TopPromotion, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { LeadershipFoundation.SeniorOfficerTraining_CommandTier2 }
            ));

            // Tier 5: General Staff Training
            AddSkill(new SkillDefinition(
                LeadershipFoundation.GeneralStaffTraining_CommandTier3,
                "General Staff Training",
                CUConstants.TIER4_XP_COST,
                SkillBranch.LeadershipFoundation,
                SkillTier.Tier5,
                "Elite command training for top officers maximizes leadership effectiveness and strategic capability.",
                SkillBonusType.CommandTier3,
                CUConstants.COMMAND_BONUS_VAL,
                CommandGrade.TopGrade,
                new List<Enum> { LeadershipFoundation.PromotionToTopGrade_TopPromotion }
            ));
        }

        /// <summary>
        /// Initialize armored warfare skills - tank/mechanized combat specialization
        /// </summary>
        private static void InitializeArmoredWarfareSkills()
        {
            // Tier 1: Shock Tank Corps - Hard Attack bonus
            AddSkill(new SkillDefinition(
                ArmoredDoctrine.ShockTankCorps_HardAttack,
                "Shock Tank Corps",
                CUConstants.TIER1_XP_COST,
                SkillBranch.ArmoredDoctrine,
                SkillTier.Tier1,
                "Specialization in armored assault tactics increases hard target attack effectiveness.",
                SkillBonusType.HardAttack,
                CUConstants.HARD_ATTACK_BONUS_VAL
            ));

            // Tier 2: Hull Down Expert - Hard Defense bonus
            AddSkill(new SkillDefinition(
                ArmoredDoctrine.HullDownExpert_HardDefense,
                "Hull Down Expert",
                CUConstants.TIER2_XP_COST,
                SkillBranch.ArmoredDoctrine,
                SkillTier.Tier2,
                "Training in defensive positioning techniques increases armor protection and survivability.",
                SkillBonusType.HardDefense,
                CUConstants.HARD_DEFENSE_BONUS_VAL,
                CommandGrade.JuniorGrade,
                new List<Enum> { ArmoredDoctrine.ShockTankCorps_HardAttack }
            ));

            // Tier 3: Pursuit Doctrine - Breakthrough capability
            AddSkill(new SkillDefinition(
                ArmoredDoctrine.PursuitDoctrine_Breakthrough,
                "Pursuit Doctrine",
                CUConstants.TIER3_XP_COST,
                SkillBranch.ArmoredDoctrine,
                SkillTier.Tier3,
                "Specialization in rapid exploitation of breakthroughs allows bonus movement after enemy retreats.",
                SkillBonusType.Breakthrough, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { ArmoredDoctrine.HullDownExpert_HardDefense }
            ));

            // Tier 4: Night Fighting Specialist - Night vision capability
            AddSkill(new SkillDefinition(
                ArmoredDoctrine.NightFightingSpecialist_NVG,
                "Night Fighting Specialist",
                CUConstants.TIER4_XP_COST,
                SkillBranch.ArmoredDoctrine,
                SkillTier.Tier4,
                "Elite training in night operations with advanced equipment allows effective combat in darkness.",
                SkillBonusType.NVG, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { ArmoredDoctrine.PursuitDoctrine_Breakthrough }
            ));
        }

        /// <summary>
        /// Initialize infantry doctrine skills - soft-target and tactical infantry operations
        /// </summary>
        private static void InitializeInfantryDoctrineSkills()
        {
            // Tier 1: Infantry Assault Tactics - Soft Attack bonus
            AddSkill(new SkillDefinition(
                InfantryDoctrine.InfantryAssaultTactics_SoftAttack,
                "Infantry Assault Tactics",
                CUConstants.TIER1_XP_COST,
                SkillBranch.InfantryDoctrine,
                SkillTier.Tier1,
                "Specialized infantry assault training increases effectiveness against soft targets.",
                SkillBonusType.SoftAttack,
                CUConstants.SOFT_ATTACK_BONUS_VAL
            ));

            // Tier 2: Defensive Doctrine - Soft Defense bonus
            AddSkill(new SkillDefinition(
                InfantryDoctrine.DefensiveDoctrine_SoftDefense,
                "Defensive Doctrine",
                CUConstants.TIER2_XP_COST,
                SkillBranch.InfantryDoctrine,
                SkillTier.Tier2,
                "Training in defensive tactics and entrenchment improves infantry survivability.",
                SkillBonusType.SoftDefense,
                CUConstants.SOFT_DEFENSE_BONUS_VAL,
                CommandGrade.JuniorGrade,
                new List<Enum> { InfantryDoctrine.InfantryAssaultTactics_SoftAttack }
            ));

            // Tier 3: Urban Combat Specialist - Urban combat bonus
            AddSkill(new SkillDefinition(
                InfantryDoctrine.UrbanCombatSpecialist_UrbanCombat,
                "Urban Combat Specialist",
                CUConstants.TIER3_XP_COST,
                SkillBranch.InfantryDoctrine,
                SkillTier.Tier3,
                "Advanced training in built-up area operations significantly improves combat effectiveness in urban terrain.",
                SkillBonusType.UrbanCombat, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { InfantryDoctrine.DefensiveDoctrine_SoftDefense }
            ));

            // Tier 4: Rough Terrain Operations - Rough terrain bonus
            AddSkill(new SkillDefinition(
                InfantryDoctrine.RoughTerrainOperations_RoughTerrain,
                "Rough Terrain Operations",
                CUConstants.TIER4_XP_COST,
                SkillBranch.InfantryDoctrine,
                SkillTier.Tier4,
                "Elite training in difficult terrain operations enhances combat effectiveness in mountains, forests, and swamps.",
                SkillBonusType.RoughTerrain, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { InfantryDoctrine.UrbanCombatSpecialist_UrbanCombat }
            ));
        }

        /// <summary>
        /// Initialize artillery doctrine skills - indirect fire and precision targeting
        /// </summary>
        private static void InitializeArtilleryDoctrineSkills()
        {
            // Tier 1: Precision Targeting - Range bonus
            AddSkill(new SkillDefinition(
                ArtilleryDoctrine.PrecisionTargeting_IndirectRange,
                "Precision Targeting",
                CUConstants.TIER1_XP_COST,
                SkillBranch.ArtilleryDoctrine,
                SkillTier.Tier1,
                "Improved artillery targeting techniques extend effective range of indirect fire weapons.",
                SkillBonusType.IndirectRange,
                CUConstants.INDIRECT_RANGE_BONUS_VAL
            ));

            // Tier 2: Mobile Artillery Doctrine - Shoot and scoot capability
            AddSkill(new SkillDefinition(
                ArtilleryDoctrine.MobileArtilleryDoctrine_ShootAndScoot,
                "Mobile Artillery Doctrine",
                CUConstants.TIER2_XP_COST,
                SkillBranch.ArtilleryDoctrine,
                SkillTier.Tier2,
                "Training in rapid redeployment allows artillery to fire and then move in the same turn.",
                SkillBonusType.ShootAndScoot, // Boolean capability
                CommandGrade.JuniorGrade,
                new List<Enum> { ArtilleryDoctrine.PrecisionTargeting_IndirectRange }
            ));

            // Tier 3: Fire Mission Specialist - Advanced targeting capability
            AddSkill(new SkillDefinition(
                ArtilleryDoctrine.FireMissionSpecialist_AdvancedTargetting,
                "Fire Mission Specialist",
                CUConstants.TIER3_XP_COST,
                SkillBranch.ArtilleryDoctrine,
                SkillTier.Tier3,
                "Expert artillery coordination allows targeting multiple enemy units in one turn at increased supply cost.",
                SkillBonusType.AdvancedTargetting, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { ArtilleryDoctrine.MobileArtilleryDoctrine_ShootAndScoot }
            ));
        }

        /// <summary>
        /// Initialize air defense doctrine skills - anti-air and opportunity fire
        /// </summary>
        private static void InitializeAirDefenseDoctrineSkills()
        {
            // Tier 1: Offensive Air Defense - Air Attack bonus
            AddSkill(new SkillDefinition(
                AirDefenseDoctrine.OffensiveAirDefense_AirAttack,
                "Offensive Air Defense",
                CUConstants.TIER1_XP_COST,
                SkillBranch.AirDefenseDoctrine,
                SkillTier.Tier1,
                "Aggressive anti-aircraft tactics increase effectiveness against air targets.",
                SkillBonusType.AirAttack,
                CUConstants.AIR_ATTACK_BONUS_VAL
            ));

            // Tier 2: Integrated Air Defense System - Air Defense bonus
            AddSkill(new SkillDefinition(
                AirDefenseDoctrine.IntegratedAirDefenseSystem_AirDefense,
                "Integrated Air Defense System",
                CUConstants.TIER2_XP_COST,
                SkillBranch.AirDefenseDoctrine,
                SkillTier.Tier2,
                "Coordinated air defense network improves survivability against air attacks.",
                SkillBonusType.AirDefense,
                CUConstants.AIR_DEFENSE_BONUS_VAL,
                CommandGrade.JuniorGrade,
                new List<Enum> { AirDefenseDoctrine.OffensiveAirDefense_AirAttack }
            ));

            // Tier 3: Ready Response Protocol - Opportunity Action capability
            AddSkill(new SkillDefinition(
                AirDefenseDoctrine.ReadyResponseProtocol_OpportunityAction,
                "Ready Response Protocol",
                CUConstants.TIER3_XP_COST,
                SkillBranch.AirDefenseDoctrine,
                SkillTier.Tier3,
                "Improved reaction time allows additional opportunity fire actions against enemy movement.",
                SkillBonusType.OpportunityAction, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { AirDefenseDoctrine.IntegratedAirDefenseSystem_AirDefense }
            ));
        }

        /// <summary>
        /// Initialize airborne doctrine skills - paratrooper operations
        /// </summary>
        private static void InitializeAirborneDoctrineSkills()
        {
            // Tier 1: Rapid Deployment Planning - Impromptu Planning capability
            AddSkill(new SkillDefinition(
                AirborneDoctrine.RapidDeploymentPlanning_ImpromptuPlanning,
                "Rapid Deployment Planning",
                CUConstants.TIER1_XP_COST,
                SkillBranch.AirborneDoctrine,
                SkillTier.Tier1,
                "Streamlined planning procedures allow boarding aircraft without spending an action.",
                SkillBonusType.ImpromptuPlanning // Boolean capability
            ));

            // Tier 2: Combat Drop Doctrine - Airborne Assault capability
            AddSkill(new SkillDefinition(
                AirborneDoctrine.CombatDropDoctrine_AirborneAssault,
                "Combat Drop Doctrine",
                CUConstants.TIER2_XP_COST,
                SkillBranch.AirborneDoctrine,
                SkillTier.Tier2,
                "Advanced combat drop training reduces suppression impact for paratroopers after landing.",
                SkillBonusType.AirborneAssault, // Boolean capability
                CommandGrade.JuniorGrade,
                new List<Enum> { AirborneDoctrine.RapidDeploymentPlanning_ImpromptuPlanning }
            ));

            // Tier 3: Elite Paratrooper Corps - Airborne Elite capability
            AddSkill(new SkillDefinition(
                AirborneDoctrine.EliteParatrooperCorps_AirborneElite,
                "Elite Paratrooper Corps",
                CUConstants.TIER3_XP_COST,
                SkillBranch.AirborneDoctrine,
                SkillTier.Tier3,
                "Elite airborne units can still conduct combat operations immediately after a jump.",
                SkillBonusType.AirborneElite, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { AirborneDoctrine.CombatDropDoctrine_AirborneAssault }
            ));
        }

        /// <summary>
        /// Initialize air mobile doctrine skills - helicopter operations
        /// </summary>
        private static void InitializeAirMobileDoctrineSkills()
        {
            // Tier 1: Rapid Redeployment - Air Mobile capability
            AddSkill(new SkillDefinition(
                AirMobileDoctrine.RapidRedeployment_AirMobile,
                "Rapid Redeployment",
                CUConstants.TIER1_XP_COST,
                SkillBranch.AirMobileDoctrine,
                SkillTier.Tier1,
                "Improved helicopter operations allow units to move after air landing.",
                SkillBonusType.AirMobile // Boolean capability
            ));

            // Tier 2: Heliborne Strike Force - Air Mobile Assault capability
            AddSkill(new SkillDefinition(
                AirMobileDoctrine.HeliborneStrikeForce_AirMobileAssault,
                "Heliborne Strike Force",
                CUConstants.TIER2_XP_COST,
                SkillBranch.AirMobileDoctrine,
                SkillTier.Tier2,
                "Combat-focused air mobile operations allow units to still have a combat action after landing.",
                SkillBonusType.AirMobileAssault, // Boolean capability
                CommandGrade.JuniorGrade,
                new List<Enum> { AirMobileDoctrine.RapidRedeployment_AirMobile }
            ));

            // Tier 3: Elite Air Mobile Operations - Air Mobile Elite capability
            AddSkill(new SkillDefinition(
                AirMobileDoctrine.EliteAirMobileOperations_AirMobileElite,
                "Elite Air Mobile Operations",
                CUConstants.TIER3_XP_COST,
                SkillBranch.AirMobileDoctrine,
                SkillTier.Tier3,
                "Elite training significantly reduces vulnerability to enemy fire when mounted in helicopters.",
                SkillBonusType.AirMobileElite, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { AirMobileDoctrine.HeliborneStrikeForce_AirMobileAssault }
            ));
        }

        /// <summary>
        /// Initialize intelligence skills - reconnaissance and target acquisition
        /// </summary>
        private static void InitializeIntelligenceDoctrine()
        {
            // Tier 1: Enhanced IntelligenceDoctrine Collection - Intel actions bonus
            AddSkill(new SkillDefinition(
                IntelligenceDoctrine.EnhancedIntelligenceCollection_ImprovedGathering,
                "Enhanced Intelligence Collection",
                CUConstants.TIER1_XP_COST,
                SkillBranch.IntelligenceDoctrine,
                SkillTier.Tier1,
                "Improved intelligence gathering techniques provide additional intel actions each turn.",
                SkillBonusType.ImprovedGathering,
                CUConstants.DEPLOYMENT_ACTION_BONUS_VAL
            ));

            // Tier 2: Concealed Operations Base - Underground Bunker capability
            AddSkill(new SkillDefinition(
                IntelligenceDoctrine.ConcealedOperationsBase_UndergroundBunker,
                "Concealed Operations Base",
                CUConstants.TIER2_XP_COST,
                SkillBranch.IntelligenceDoctrine,
                SkillTier.Tier2,
                "Specialized camouflage and concealment techniques significantly reduce unit visibility on the battlefield.",
                SkillBonusType.UndergroundBunker, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { IntelligenceDoctrine.EnhancedIntelligenceCollection_ImprovedGathering }
            ));

            // Tier 3: SatelliteIntelligence - Satellite Intel capability
            AddSkill(new SkillDefinition(
                IntelligenceDoctrine.SatelliteIntelligence_SpaceAssets,
                "Satellite Reconnaissance",
                CUConstants.TIER3_XP_COST,
                SkillBranch.IntelligenceDoctrine,
                SkillTier.Tier3,
                "Satellite intelligence provides a chance to spot enemy units anywhere on the map.",
                SkillBonusType.SpaceAssets, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { IntelligenceDoctrine.ConcealedOperationsBase_UndergroundBunker }
            ));
        }

        /// <summary>
        /// Initialize signal intelligence skills - electronic warfare and analysis
        /// </summary>
        private static void InitializeSignalIntelligenceSpecialization()
        {
            // Tier 4: Communications Decryption - Signal Decryption capability
            AddSkill(new SkillDefinition(
                SignalIntelligenceSpecialization.CommunicationsDecryption_SignalDecryption,
                "Communications Decryption",
                CUConstants.TIER4_XP_COST,
                SkillBranch.SignalIntelligenceSpecialization,
                SkillTier.Tier4,
                "Signals intelligence capability reveals more detailed information about spotted enemy units.",
                SkillBonusType.SignalDecryption, 
                CommandGrade.TopGrade // Boolean capability
            ));

            // Tier 4: Electronic Surveillance Network - Detection Range bonus
            AddSkill(new SkillDefinition(
                SignalIntelligenceSpecialization.ElectronicSurveillanceNetwork_SpottingRange,
                "Electronic Surveillance Network",
                CUConstants.TIER4_XP_COST,
                SkillBranch.SignalIntelligenceSpecialization,
                SkillTier.Tier4,
                "Deployable electronic surveillance systems dramatically increase detection range.",
                SkillBonusType.SpottingRange,
                CUConstants.LARGE_SPOTTING_RANGE_BONUS_VAL,
                CommandGrade.TopGrade,
                new List<Enum> { SignalIntelligenceSpecialization.CommunicationsDecryption_SignalDecryption }
            ));

            // Tier 4: Radio Electronic Combat - Electronic Warfare capability
            AddSkill(new SkillDefinition(
                SignalIntelligenceSpecialization.RadioElectronicCombat_ElectronicWarfare,
                "Radio Electronic Combat",
                CUConstants.TIER4_XP_COST,
                SkillBranch.SignalIntelligenceSpecialization,
                SkillTier.Tier4,
                "Advanced jamming techniques provide a chance to immobilize enemy units when gathering intelligence.",
                SkillBonusType.ElectronicWarfare, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { SignalIntelligenceSpecialization.ElectronicSurveillanceNetwork_SpottingRange }
            ));

            // Tier 4: Enemy Behavior Analysis - Pattern Recognition capability
            AddSkill(new SkillDefinition(
                SignalIntelligenceSpecialization.EnemyBehaviorAnalysis_PatternRecognition,
                "Enemy Behavior Analysis",
                CUConstants.TIER4_XP_COST,
                SkillBranch.SignalIntelligenceSpecialization,
                SkillTier.Tier4,
                "Advanced analysis of enemy movement patterns reveals likely movement paths for spotted units.",
                SkillBonusType.PatternRecognition, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { SignalIntelligenceSpecialization.RadioElectronicCombat_ElectronicWarfare}
            ));
        }

        /// <summary>
        /// Initialize engineering skills - terrain manipulation and river crossing
        /// </summary>
        private static void InitializeEngineeringSkills()
        {
            // Tier 1: River Crossing Operations - River Crossing capability
            AddSkill(new SkillDefinition(
                EngineeringSpecialization.RiverCrossingOperations_RiverCrossing,
                "River Crossing Operations",
                CUConstants.TIER1_XP_COST,
                SkillBranch.EngineeringSpecialization,
                SkillTier.Tier1,
                "Engineering expertise significantly reduces movement costs when crossing rivers.",
                SkillBonusType.RiverCrossing // Boolean capability
            ));

            // Tier 2: Amphibious Assault Tactics - River Assault capability
            AddSkill(new SkillDefinition(
                EngineeringSpecialization.AmphibiousAssaultTactics_RiverAssault,
                "Amphibious Assault Tactics",
                CUConstants.TIER2_XP_COST,
                SkillBranch.EngineeringSpecialization,
                SkillTier.Tier2,
                "Specialized assault river crossing techniques reduce combat penalties when attacking across rivers.",
                SkillBonusType.RiverAssault, // Boolean capability
                CommandGrade.JuniorGrade,
                new List<Enum> { EngineeringSpecialization.RiverCrossingOperations_RiverCrossing }
            ));

            // Tier 3: Combat EngineeringSpecialization Corps - Bridge Building capability
            AddSkill(new SkillDefinition(
                EngineeringSpecialization.CombatEngineeringCorps_BridgeBuilding,
                "Combat Engineering Corps",
                CUConstants.TIER3_XP_COST,
                SkillBranch.EngineeringSpecialization,
                SkillTier.Tier3,
                "Advanced military engineering allows construction of tactical bridges over rivers in a single turn.",
                SkillBonusType.BridgeBuilding, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { EngineeringSpecialization.AmphibiousAssaultTactics_RiverAssault }
            ));

            // Tier 4: Field Fortification Expert - Field Fortification capability
            AddSkill(new SkillDefinition(
                EngineeringSpecialization.FieldFortificationExpert_FieldFortification,
                "Field Fortification Expert",
                CUConstants.TIER4_XP_COST,
                SkillBranch.EngineeringSpecialization,
                SkillTier.Tier4,
                "Expert engineering skills allow construction of permanent defensive fortifications.",
                SkillBonusType.FieldFortification, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { EngineeringSpecialization.CombatEngineeringCorps_BridgeBuilding }
            ));
        }

        /// <summary>
        /// Initialize special forces doctrine skills - unconventional warfare
        /// </summary>
        private static void InitializeSpecialForcesDoctrineSkills()
        {
            // Tier 1: Special Terrain Mastery - Terrain Mastery capability
            AddSkill(new SkillDefinition(
                SpecialForcesSpecialization.SpecialTerrainMastery_TerrainMastery,
                "Special Terrain Mastery",
                CUConstants.TIER1_XP_COST,
                SkillBranch.SpecialForcesSpecialization,
                SkillTier.Tier1,
                "Advanced training in difficult terrain reduces movement costs in rough terrain, forests, and mountains.",
                SkillBonusType.TerrainMastery // Boolean capability
            ));

            // Tier 2: Infiltration Tactics - Infiltration Movement capability
            AddSkill(new SkillDefinition(
                SpecialForcesSpecialization.InfiltrationTactics_InfiltrationMovement,
                "Infiltration Tactics",
                CUConstants.TIER2_XP_COST,
                SkillBranch.SpecialForcesSpecialization,
                SkillTier.Tier2,
                "Specialized movement techniques allow easier passage through enemy zones of control.",
                SkillBonusType.InfiltrationMovement, // Boolean capability
                CommandGrade.JuniorGrade,
                new List<Enum> { SpecialForcesSpecialization.SpecialTerrainMastery_TerrainMastery }
            ));

            // Tier 3: Superior Camouflage - Concealed Positions capability
            AddSkill(new SkillDefinition(
                SpecialForcesSpecialization.SuperiorCamouflage_ConcealedPositions,
                "Superior Camouflage",
                CUConstants.TIER3_XP_COST,
                SkillBranch.SpecialForcesSpecialization,
                SkillTier.Tier3,
                "Advanced concealment techniques reduce unit visibility on the battlefield.",
                SkillBonusType.ConcealedPositions, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { SpecialForcesSpecialization.InfiltrationTactics_InfiltrationMovement }
            ));

            // Tier 4: Ambush Tactics - Ambush Tactics capability
            AddSkill(new SkillDefinition(
                SpecialForcesSpecialization.AmbushTactics_AmbushTactics,
                "Ambush Tactics",
                CUConstants.TIER4_XP_COST,
                SkillBranch.SpecialForcesSpecialization,
                SkillTier.Tier4,
                "Specialized ambush training grants a significant combat bonus for the first attack from concealment.",
                SkillBonusType.AmbushTactics, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { SpecialForcesSpecialization.SuperiorCamouflage_ConcealedPositions }
            ));

            // Tier 5: Night Combat Operations - Night Combat capability
            AddSkill(new SkillDefinition(
                SpecialForcesSpecialization.NightCombatOperations_NightCombat,
                "Night Combat Operations",
                CUConstants.TIER4_XP_COST,
                SkillBranch.SpecialForcesSpecialization,
                SkillTier.Tier5,
                "Elite training in night operations provides combat bonuses during nighttime turns.",
                SkillBonusType.NightCombat, // Boolean capability
                CommandGrade.TopGrade,
                new List<Enum> { SpecialForcesSpecialization.AmbushTactics_AmbushTactics }
            ));
        }

        /// <summary>
        /// Initialize politically connected skills - special political bonuses
        /// This branch can be combined with other specializations
        /// </summary>
        private static void InitializePoliticallyConnectedSkills()
        {
            // Tier 1: Emergency Air Drop - Emergency Resupply capability
            AddSkill(new SkillDefinition(
                PoliticallyConnectedFoundation.EmergencyAirDrop_EmergencyResupply,
                "Emergency Air Drop",
                CUConstants.TIER1_XP_COST,
                SkillBranch.PoliticallyConnectedFoundation,
                SkillTier.Tier1,
                "Political connections provide access to emergency air resupply operations once per scenario.",
                SkillBonusType.EmergencyResupply // Boolean capability
            ));

            // Tier 2: Direct Line To HQ - Supply Consumption reduction
            AddSkill(new SkillDefinition(
                PoliticallyConnectedFoundation.DirectLineToHQ_SupplyConsumption,
                "Direct Line To HQ",
                CUConstants.TIER2_XP_COST,
                SkillBranch.PoliticallyConnectedFoundation,
                SkillTier.Tier2,
                $"Priority supply allocation reduces consumption by {(1.0f - CUConstants.SUPPLY_ECONOMY_REDUCTION_VAL) * 100}%.",
                SkillBonusType.SupplyConsumption,
                CUConstants.SUPPLY_ECONOMY_REDUCTION_VAL,
                CommandGrade.JuniorGrade,
                new List<Enum> { PoliticallyConnectedFoundation.EmergencyAirDrop_EmergencyResupply }
            ));

            // Tier 3: Foreign Technology - Night Vision capability
            AddSkill(new SkillDefinition(
                PoliticallyConnectedFoundation.ForeignTechnology_NVG,
                "Foreign Technology",
                CUConstants.TIER3_XP_COST,
                SkillBranch.PoliticallyConnectedFoundation,
                SkillTier.Tier3,
                "Access to cutting-edge foreign night vision equipment enhances operations in darkness.",
                SkillBonusType.NVG, // Boolean capability
                CommandGrade.SeniorGrade,
                new List<Enum> { PoliticallyConnectedFoundation.DirectLineToHQ_SupplyConsumption }
            ));

            // Tier 4: Better Replacements - Replacement XP bonus
            AddSkill(new SkillDefinition(
                PoliticallyConnectedFoundation.BetterReplacements_ReplacementXP,
                "Better Replacements",
                CUConstants.TIER4_XP_COST,
                SkillBranch.PoliticallyConnectedFoundation,
                SkillTier.Tier4,
                $"Political influence ensures better trained replacements, increasing experience gain by {CUConstants.EXPERIENCE_BONUS_VAL * 100}%.",
                SkillBonusType.ReplacementXP,
                CUConstants.EXPERIENCE_BONUS_VAL,
                CommandGrade.SeniorGrade,
                new List<Enum> { PoliticallyConnectedFoundation.ForeignTechnology_NVG }
            ));

            // Tier 5: Connections At The Top - Prestige cost reduction
            AddSkill(new SkillDefinition(
                PoliticallyConnectedFoundation.ConnectionsAtTheTop_PrestigeCost,
                "Connections At The Top",
                CUConstants.TIER5_XP_COST,
                SkillBranch.PoliticallyConnectedFoundation,
                SkillTier.Tier5,
                $"High-level political connections reduce equipment costs by {(1.0f - CUConstants.PRESTIGE_COST_REDUCTION_VAL) * 100}%.",
                SkillBonusType.PrestigeCost,
                CUConstants.PRESTIGE_COST_REDUCTION_VAL,
                CommandGrade.TopGrade,
                new List<Enum> { PoliticallyConnectedFoundation.BetterReplacements_ReplacementXP }
            ));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Add a skill definition to the catalog
        /// </summary>
        /// <param name="skillDef">The skill definition to add</param>
        private static void AddSkill(SkillDefinition skillDef)
        {
            if (AllSkills.ContainsKey(skillDef.SkillEnumValue))
            {
                // Handle error: duplicate skill enum value
                Debug.LogError($"Skill {skillDef.Name} (Enum: {skillDef.SkillEnumValue}) is already defined.");
                return;
            }
            AllSkills[skillDef.SkillEnumValue] = skillDef;
        }

        /// <summary>
        /// Try to get a skill definition by its enum value
        /// </summary>
        /// <param name="skillEnum">The enum value of the skill</param>
        /// <param name="definition">The output skill definition if found</param>
        /// <returns>True if the skill was found, false otherwise</returns>
        public static bool TryGetSkillDefinition(Enum skillEnum, out SkillDefinition definition)
        {
            return AllSkills.TryGetValue(skillEnum, out definition);
        }

        /// <summary>
        /// Get a skill's name by its enum value
        /// </summary>
        /// <param name="skillEnum">The enum value of the skill</param>
        /// <returns>The skill name, or "Unknown Skill" if not found</returns>
        public static string GetSkillName(Enum skillEnum)
        {
            if (TryGetSkillDefinition(skillEnum, out var def))
            {
                return def.Name;
            }
            return "Unknown Skill";
        }

        /// <summary>
        /// Get a full description of a skill, including requirements and effects
        /// </summary>
        /// <param name="skillEnum">The enum value of the skill</param>
        /// <returns>The full description, or "Unknown skill." if not found</returns>
        public static string GetFullSkillDescription(Enum skillEnum)
        {
            if (TryGetSkillDefinition(skillEnum, out var def))
            {
                return def.GetFullDescription();
            }
            return "Unknown skill.";
        }

        /// <summary>
        /// Get all skills in a specific branch
        /// </summary>
        /// <param name="branch">The branch to get skills for</param>
        /// <returns>A list of skill definitions in the branch</returns>
        public static List<SkillDefinition> GetSkillsInBranch(SkillBranch branch)
        {
            return AllSkills.Values
                .Where(def => def.Branch == branch)
                .OrderBy(def => def.Tier)
                .ToList();
        }

        /// <summary>
        /// Get all skills of a specific tier within a branch
        /// </summary>
        /// <param name="branch">The branch to get skills from</param>
        /// <param name="tier">The tier to filter by</param>
        /// <returns>A list of skill definitions matching the branch and tier</returns>
        public static List<SkillDefinition> GetSkillsInBranchByTier(SkillBranch branch, SkillTier tier)
        {
            return AllSkills.Values
                .Where(def => def.Branch == branch && def.Tier == tier)
                .ToList();
        }

        #endregion
    }
}