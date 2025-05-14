using System;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    // Struct to hold all static data for a single skill
    public readonly struct SkillDefinition
    {
        public Enum SkillEnumValue { get; } // The actual enum value (e.g., LeadershipPath.ShockFormation)
        public string Name { get; }
        public int XPCost { get; }
        public string Description { get; }
        public CommandGrade RequiredGrade { get; }
        public List<Enum> Prerequisites { get; } // List of other skill enum values
        public List<Enum> MutuallyExclusive { get; } // List of skills this one cannot be taken with
        public SkillBonusType PrimaryBonusType { get; }
        public float PrimaryBonusValue { get; } // Use float to accommodate multipliers and integers
        // Add secondary bonus if a skill can have more than one distinct effect, or handle complex effects differently

        public SkillDefinition(Enum skillEnumValue, string name, int xpCost, string description,
                               CommandGrade requiredGrade = CommandGrade.JuniorGrade,
                               List<Enum> prerequisites = null, List<Enum> mutuallyExclusive = null,
                               SkillBonusType primaryBonusType = SkillBonusType.None, float primaryBonusValue = 0)
        {
            SkillEnumValue = skillEnumValue;
            Name = name;
            XPCost = xpCost;
            Description = description;
            RequiredGrade = requiredGrade;
            Prerequisites = prerequisites ?? new List<Enum>();
            MutuallyExclusive = mutuallyExclusive ?? new List<Enum>();
            PrimaryBonusType = primaryBonusType;
            PrimaryBonusValue = primaryBonusValue;
        }
    }

    public static class LeaderSkillCatalog
    {
        private static readonly Dictionary<Enum, SkillDefinition> AllSkills = new();

        static LeaderSkillCatalog()
        {
            InitializeCommandSkills();
            InitializeRearAreaSkills();
            InitializeBattleDoctrineSkills();
            InitializeCombatOperationsSkills();
        }

        private static void InitializeCommandSkills()
        {
            // Tier 1
            AddSkill(new SkillDefinition(
                LeadershipPath.ShockFormation, "Shock Formation", CUConstants.TIER1_XP_COST,
                $"Increases experience gain by {CUConstants.EXPERIENCE_BONUS_VAL * 100}%. Mutually exclusive with Maskirovka Master.",
                mutuallyExclusive: new List<Enum> { LeadershipPath.MaskirovkaMaster },
                primaryBonusType: SkillBonusType.ReplacementXP, primaryBonusValue: CUConstants.EXPERIENCE_BONUS_VAL
            ));
            //AddSkill(new SkillDefinition(
            //    LeadershipPath.MaskirovkaMaster, "Maskirovka Master", TIER1_XP_COST,
            //    $"Increases Detection Range by +{MASKIROVKA_DETECTION_BONUS_VAL}. Mutually exclusive with Shock Formation.",
            //    mutuallyExclusive: new List<Enum> { LeadershipPath.ShockFormation },
            //    primaryBonusType: SkillBonusType.Detection, primaryBonusValue: MASKIROVKA_DETECTION_BONUS_VAL
            //));

            // Tier 2
            AddSkill(new SkillDefinition(
                LeadershipPath.IronDiscipline, "Iron Discipline", CUConstants.TIER2_XP_COST,
                "Increases Command Rating by +1.",
                prerequisites: new List<Enum> { LeadershipPath.ShockFormation },
                primaryBonusType: SkillBonusType.Command, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
            ));
            //AddSkill(new SkillDefinition(
            //    LeadershipPath.TacticalGenius, "Tactical Genius", TIER2_XP_COST,
            //    "Increases Initiative by +1.",
            //    prerequisites: new List<Enum> { LeadershipPath.MaskirovkaMaster },
            //    primaryBonusType: SkillBonusType.Initiative, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            //));

            // Tier 3 - Senior Grade
            AddSkill(new SkillDefinition(
                LeadershipPath.PoliticalOfficer, "Political Officer", CUConstants.TIER3_XP_COST,
                "Increases Command Rating by +1.", CommandGrade.SeniorGrade,
                prerequisites: new List<Enum> { LeadershipPath.IronDiscipline },
                primaryBonusType: SkillBonusType.Command, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
            ));
            //AddSkill(new SkillDefinition(
            //    LeadershipPath.OperationalArt, "Operational Art", TIER3_XP_COST,
            //    "Increases Initiative by +1.", CommandGrade.SeniorGrade,
            //    prerequisites: new List<Enum> { LeadershipPath.TacticalGenius },
            //    primaryBonusType: SkillBonusType.Initiative, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            //));

            // Tier 4 - Top Grade
            AddSkill(new SkillDefinition(
                LeadershipPath.HeroOfSovietUnion, "Hero Of Soviet Union", CUConstants.TIER4_XP_COST,
                "Increases Command Rating by +1.", CommandGrade.TopGrade,
                prerequisites: new List<Enum> { LeadershipPath.PoliticalOfficer },
                primaryBonusType: SkillBonusType.Command, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
            ));
            //AddSkill(new SkillDefinition(
            //    LeadershipPath.DeepBattleTheorist, "Deep Battle Theorist", TIER4_XP_COST,
            //    "Increases Initiative by +1.", CommandGrade.TopGrade,
            //    prerequisites: new List<Enum> { LeadershipPath.OperationalArt },
            //    primaryBonusType: SkillBonusType.Initiative, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            //));
        }

        private static void InitializeRearAreaSkills()
        {
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.SupplyEconomy, "Supply Economy", CUConstants.TIER1_XP_COST,
                $"Reduces supply consumption by {CUConstants.SUPPLY_ECONOMY_REDUCTION_VAL * 100}%.",
                primaryBonusType: SkillBonusType.SupplyConsumption, primaryBonusValue: 1.0f - CUConstants.SUPPLY_ECONOMY_REDUCTION_VAL
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.FieldWorkshop, "Field Workshop", CUConstants.TIER1_XP_COST,
                $"Reduces repair costs by {CUConstants.PRESTIGE_COST_REDUCTION_VAL * 100}%.",
                primaryBonusType: SkillBonusType.PrestigeCost, primaryBonusValue: 1.0f - CUConstants.PRESTIGE_COST_REDUCTION_VAL
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.PartyConnections, "Party Connections", CUConstants.TIER2_XP_COST,
                "Enables access to equipment upgrades.",
                prerequisites: new List<Enum> { RearAreaSkillPath.SupplyEconomy, RearAreaSkillPath.FieldWorkshop }
                // This skill might have an effect handled elsewhere, or a specific boolean flag bonus type
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.StrategicAirlift, "Strategic Airlift", CUConstants.TIER3_XP_COST,
                "Enables emergency resupply operations.",
                prerequisites: new List<Enum> { RearAreaSkillPath.PartyConnections },
                primaryBonusType: SkillBonusType.EmergencyResupply
            ));
            //AddSkill(new SkillDefinition(
            //    RearAreaSkillPath.ArmoredSupplyColumn, "Armored Supply Column", CUConstants.TIER4_XP_COST,
            //    "Depots can penetrate one enemy zone of control.",
            //    prerequisites: new List<Enum> { RearAreaSkillPath.StrategicAirlift },
            //    primaryBonusType: SkillBonusType.SupplyPenetration
            //));
        }

        private static void InitializeBattleDoctrineSkills()
        {
            // Armored Branch
            AddSkill(new SkillDefinition(BattleDoctrineSkillPath.ArmoredWarfare, "Armored Warfare", CUConstants.TIER1_XP_COST, "Specializes in armored operations."));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.HullDownExpert, "Hull Down Expert", CUConstants.TIER2_XP_COST,
                $"Increases Hard Defense by +{CUConstants.DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.ArmoredWarfare },
                primaryBonusType: SkillBonusType.HardDefense, primaryBonusValue: CUConstants.DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.ShockTankCorps, "Shock Tank Corps", CUConstants.TIER2_XP_COST, // Tier 2 as per original, could be T3
                $"Increases Hard Attack by +{CUConstants.DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.HullDownExpert },
                primaryBonusType: SkillBonusType.HardAttack, primaryBonusValue: CUConstants.DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
               BattleDoctrineSkillPath.NightFightingSpecialist, "Night Fighting Specialist", CUConstants.TIER3_XP_COST,
               $"Increases Night Fighting capability by +{CUConstants.GENERIC_STAT_BONUS_VAL}.",
               prerequisites: new List<Enum> { BattleDoctrineSkillPath.ShockTankCorps },
               primaryBonusType: SkillBonusType.NVG, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
           ));

            // Defensive Branch
            AddSkill(new SkillDefinition(BattleDoctrineSkillPath.DefenseInDepth, "Defense In Depth", CUConstants.TIER1_XP_COST, "Specializes in defensive operations."));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.HedgehogDefense, "Hedgehog Defense", CUConstants.TIER2_XP_COST,
                $"Increases Soft Defense by +{CUConstants.DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.DefenseInDepth },
                primaryBonusType: SkillBonusType.SoftDefense, primaryBonusValue: CUConstants.DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.FortificationEngineer, "Fortification Engineer", CUConstants.TIER2_XP_COST, // Tier 2 as per original
                $"Increases Hard Defense by +{CUConstants.DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.HedgehogDefense },
                primaryBonusType: SkillBonusType.HardDefense, primaryBonusValue: CUConstants.DEFENSE_ATTACK_BONUS_VAL
            ));
            //AddSkill(new SkillDefinition(
            //    BattleDoctrineSkillPath.TrenchWarfareExpert, "Trench Warfare Expert", CUConstants.TIER3_XP_COST,
            //    $"Increases Entrenchment by +{CUConstants.ENTRENCHMENT_BONUS_VAL}.",
            //    prerequisites: new List<Enum> { BattleDoctrineSkillPath.FortificationEngineer },
            //    primaryBonusType: SkillBonusType.Entrenchment, primaryBonusValue: CUConstants.ENTRENCHMENT_BONUS_VAL
            //));

            // Artillery/Support Branch
            AddSkill(new SkillDefinition(BattleDoctrineSkillPath.QueenOfBattle, "Queen Of Battle", CUConstants.TIER1_XP_COST, "Specializes in artillery operations."));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.IntegratedAirDefenseSystem, "Integrated Air Defense", CUConstants.TIER2_XP_COST,
                $"Increases Air Defense by +{CUConstants.AIR_DEFENSE_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.QueenOfBattle },
                primaryBonusType: SkillBonusType.AirDefense, primaryBonusValue: CUConstants.AIR_DEFENSE_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
               BattleDoctrineSkillPath.ForwardObservationPost, "Forward Observation Post", CUConstants.TIER3_XP_COST,
               $"Increases Detection Range by +{CUConstants.DETECTION_RANGE_BONUS_VAL}.",
               prerequisites: new List<Enum> { BattleDoctrineSkillPath.IntegratedAirDefenseSystem },
               primaryBonusType: SkillBonusType.SpottingRange, primaryBonusValue: CUConstants.DETECTION_RANGE_BONUS_VAL
           ));
            
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.PrecisionTargetting, "Precision Targetting", CUConstants.TIER4_XP_COST,
                $"Increases Indirect Range by +{CUConstants.INDIRECT_RANGE_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.ForwardObservationPost },
                primaryBonusType: SkillBonusType.IndirectRange, primaryBonusValue: CUConstants.INDIRECT_RANGE_BONUS_VAL
            ));
        }

        private static void InitializeCombatOperationsSkills()
        {
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.PursuitDoctrine, "Pursuit Doctrine", CUConstants.TIER4_XP_COST,
                "Enables breakthrough following a retreat.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.Breakthrough
            ));
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.OffensiveDoctrine, "Offensive Doctrine", CUConstants.TIER4_XP_COST,
                $"Increases Combat Actions by +{CUConstants.GENERIC_STAT_BONUS_VAL}.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.CombatAction, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.ManeuverDoctrine, "Maneuver Doctrine", CUConstants.TIER4_XP_COST,
                $"Increases Movement Actions by +{CUConstants.GENERIC_STAT_BONUS_VAL}.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.MovementAction, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
            ));
            
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.SpecialistCorps, "Specialist Corps", CUConstants.TIER4_XP_COST,
                $"Increases Special operations by +{CUConstants.GENERIC_STAT_BONUS_VAL}.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.DeploymentAction, primaryBonusValue: CUConstants.GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.ReconnaissanceInForce, "Reconnaissance In Force", CUConstants.TIER4_XP_COST,
                "Enhances reconnaissance capabilities.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.SpottingRange, primaryBonusValue: CUConstants.DETECTION_RANGE_BONUS_VAL
            ));   
        }

        private static void AddSkill(SkillDefinition skillDef)
        {
            if (AllSkills.ContainsKey(skillDef.SkillEnumValue))
            {
                // Handle error: duplicate skill enum value
                throw new ArgumentException($"Skill {skillDef.Name} (Enum: {skillDef.SkillEnumValue}) is already defined.");
            }
            AllSkills[skillDef.SkillEnumValue] = skillDef;
        }

        public static bool TryGetSkillDefinition(Enum skillEnum, out SkillDefinition definition)
        {
            return AllSkills.TryGetValue(skillEnum, out definition);
        }

        public static string GetFullSkillDescription(Enum skillEnum)
        {
            if (TryGetSkillDefinition(skillEnum, out var def))
            {
                return $"Cost: {def.XPCost} XP. {def.Description}";
            }
            return "Unknown skill.";
        }












    }
}