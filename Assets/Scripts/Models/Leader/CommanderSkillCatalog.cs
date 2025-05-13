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

    public static class CommanderSkillCatalog
    {
        // --- XP Cost Tiers (can be used for default costs if not specified per skill) ---
        private const int TIER1_XP_COST = 50;
        private const int TIER2_XP_COST = 75;
        private const int TIER3_XP_COST = 100;
        private const int TIER4_XP_COST = 150;

        // --- Bonus Value Constants (used in SkillDefinitions) ---
        private const float EXPERIENCE_BONUS_VAL = 0.25f;
        private const int MASKIROVKA_DETECTION_BONUS_VAL = 1;
        private const float SUPPLY_ECONOMY_REDUCTION_VAL = 0.33f; // Reduction, so final multiplier is 1 - value
        private const float PRESTIGE_COST_REDUCTION_VAL = 0.33f;  // Reduction
        private const int GENERIC_STAT_BONUS_VAL = 1; // For Command, Initiative, NVGCapability etc.
        private const int DEFENSE_ATTACK_BONUS_VAL = 5;
        private const int DETECTION_RANGE_BONUS_VAL = 1;
        private const int ENTRENCHMENT_BONUS_VAL = 1;
        private const int INDIRECT_RANGE_BONUS_VAL = 1;
        private const int AIR_DEFENSE_BONUS_VAL = 5;

        private static readonly Dictionary<Enum, SkillDefinition> AllSkills = new();

        static CommanderSkillCatalog()
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
                LeadershipPath.ShockFormation, "Shock Formation", TIER1_XP_COST,
                $"Increases experience gain by {EXPERIENCE_BONUS_VAL * 100}%. Mutually exclusive with Maskirovka Master.",
                mutuallyExclusive: new List<Enum> { LeadershipPath.MaskirovkaMaster },
                primaryBonusType: SkillBonusType.UnitXP, primaryBonusValue: EXPERIENCE_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                LeadershipPath.MaskirovkaMaster, "Maskirovka Master", TIER1_XP_COST,
                $"Increases Detection Range by +{MASKIROVKA_DETECTION_BONUS_VAL}. Mutually exclusive with Shock Formation.",
                mutuallyExclusive: new List<Enum> { LeadershipPath.ShockFormation },
                primaryBonusType: SkillBonusType.Detection, primaryBonusValue: MASKIROVKA_DETECTION_BONUS_VAL
            ));

            // Tier 2
            AddSkill(new SkillDefinition(
                LeadershipPath.IronDiscipline, "Iron Discipline", TIER2_XP_COST,
                "Increases Command Rating by +1.",
                prerequisites: new List<Enum> { LeadershipPath.ShockFormation },
                primaryBonusType: SkillBonusType.Command, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                LeadershipPath.TacticalGenius, "Tactical Genius", TIER2_XP_COST,
                "Increases Initiative by +1.",
                prerequisites: new List<Enum> { LeadershipPath.MaskirovkaMaster },
                primaryBonusType: SkillBonusType.Initiative, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));

            // Tier 3 - Senior Grade
            AddSkill(new SkillDefinition(
                LeadershipPath.PoliticalOfficer, "Political Officer", TIER3_XP_COST,
                "Increases Command Rating by +1.", CommandGrade.SeniorGrade,
                prerequisites: new List<Enum> { LeadershipPath.IronDiscipline },
                primaryBonusType: SkillBonusType.Command, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                LeadershipPath.OperationalArt, "Operational Art", TIER3_XP_COST,
                "Increases Initiative by +1.", CommandGrade.SeniorGrade,
                prerequisites: new List<Enum> { LeadershipPath.TacticalGenius },
                primaryBonusType: SkillBonusType.Initiative, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));

            // Tier 4 - Top Grade
            AddSkill(new SkillDefinition(
                LeadershipPath.HeroOfSovietUnion, "Hero Of Soviet Union", TIER4_XP_COST,
                "Increases Command Rating by +1.", CommandGrade.TopGrade,
                prerequisites: new List<Enum> { LeadershipPath.PoliticalOfficer },
                primaryBonusType: SkillBonusType.Command, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                LeadershipPath.DeepBattleTheorist, "Deep Battle Theorist", TIER4_XP_COST,
                "Increases Initiative by +1.", CommandGrade.TopGrade,
                prerequisites: new List<Enum> { LeadershipPath.OperationalArt },
                primaryBonusType: SkillBonusType.Initiative, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
        }

        private static void InitializeRearAreaSkills()
        {
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.SupplyEconomy, "Supply Economy", TIER1_XP_COST,
                $"Reduces supply consumption by {SUPPLY_ECONOMY_REDUCTION_VAL * 100}%.",
                primaryBonusType: SkillBonusType.SupplyConsumption, primaryBonusValue: 1.0f - SUPPLY_ECONOMY_REDUCTION_VAL
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.FieldWorkshop, "Field Workshop", TIER1_XP_COST,
                $"Reduces repair costs by {PRESTIGE_COST_REDUCTION_VAL * 100}%.",
                primaryBonusType: SkillBonusType.PrestigeCost, primaryBonusValue: 1.0f - PRESTIGE_COST_REDUCTION_VAL
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.PartyConnections, "Party Connections", TIER2_XP_COST,
                "Enables access to equipment upgrades.",
                prerequisites: new List<Enum> { RearAreaSkillPath.SupplyEconomy, RearAreaSkillPath.FieldWorkshop }
                // This skill might have an effect handled elsewhere, or a specific boolean flag bonus type
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.StrategicAirlift, "Strategic Airlift", TIER3_XP_COST,
                "Enables emergency resupply operations.",
                prerequisites: new List<Enum> { RearAreaSkillPath.PartyConnections },
                primaryBonusType: SkillBonusType.EmergencyResupply
            ));
            AddSkill(new SkillDefinition(
                RearAreaSkillPath.ArmoredSupplyColumn, "Armored Supply Column", TIER4_XP_COST,
                "Depots can penetrate one enemy zone of control.",
                prerequisites: new List<Enum> { RearAreaSkillPath.StrategicAirlift },
                primaryBonusType: SkillBonusType.SupplyPenetration
            ));
        }

        private static void InitializeBattleDoctrineSkills()
        {
            // Armored Branch
            AddSkill(new SkillDefinition(BattleDoctrineSkillPath.ArmoredWarfare, "Armored Warfare", TIER1_XP_COST, "Specializes in armored operations."));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.HullDownExpert, "Hull Down Expert", TIER2_XP_COST,
                $"Increases Hard Defense by +{DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.ArmoredWarfare },
                primaryBonusType: SkillBonusType.HardDefense, primaryBonusValue: DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.ShockTankCorps, "Shock Tank Corps", TIER2_XP_COST, // Tier 2 as per original, could be T3
                $"Increases Hard Attack by +{DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.HullDownExpert },
                primaryBonusType: SkillBonusType.HardAttack, primaryBonusValue: DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
               BattleDoctrineSkillPath.NightFightingSpecialist, "Night Fighting Specialist", TIER3_XP_COST,
               $"Increases Night Fighting capability by +{GENERIC_STAT_BONUS_VAL}.",
               prerequisites: new List<Enum> { BattleDoctrineSkillPath.ShockTankCorps },
               primaryBonusType: SkillBonusType.NightFighting, primaryBonusValue: GENERIC_STAT_BONUS_VAL
           ));

            // Defensive Branch
            AddSkill(new SkillDefinition(BattleDoctrineSkillPath.DefenseInDepth, "Defense In Depth", TIER1_XP_COST, "Specializes in defensive operations."));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.HedgehogDefense, "Hedgehog Defense", TIER2_XP_COST,
                $"Increases Soft Defense by +{DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.DefenseInDepth },
                primaryBonusType: SkillBonusType.SoftDefense, primaryBonusValue: DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.FortificationEngineer, "Fortification Engineer", TIER2_XP_COST, // Tier 2 as per original
                $"Increases Hard Defense by +{DEFENSE_ATTACK_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.HedgehogDefense },
                primaryBonusType: SkillBonusType.HardDefense, primaryBonusValue: DEFENSE_ATTACK_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.TrenchWarfareExpert, "Trench Warfare Expert", TIER3_XP_COST,
                $"Increases Entrenchment by +{ENTRENCHMENT_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.FortificationEngineer },
                primaryBonusType: SkillBonusType.Entrenchment, primaryBonusValue: ENTRENCHMENT_BONUS_VAL
            ));

            // Artillery/Support Branch
            AddSkill(new SkillDefinition(BattleDoctrineSkillPath.QueenOfBattle, "Queen Of Battle", TIER1_XP_COST, "Specializes in artillery operations."));
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.IntegratedAirDefenseSystem, "Integrated Air Defense", TIER2_XP_COST,
                $"Increases Air Defense by +{AIR_DEFENSE_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.QueenOfBattle },
                primaryBonusType: SkillBonusType.AirDefense, primaryBonusValue: AIR_DEFENSE_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
               BattleDoctrineSkillPath.ForwardObservationPost, "Forward Observation Post", TIER3_XP_COST,
               $"Increases Detection Range by +{DETECTION_RANGE_BONUS_VAL}.",
               prerequisites: new List<Enum> { BattleDoctrineSkillPath.IntegratedAirDefenseSystem },
               primaryBonusType: SkillBonusType.DetectionRange, primaryBonusValue: DETECTION_RANGE_BONUS_VAL
           ));
            
            AddSkill(new SkillDefinition(
                BattleDoctrineSkillPath.PrecisionTargetting, "Precision Targetting", TIER4_XP_COST,
                $"Increases Indirect Range by +{INDIRECT_RANGE_BONUS_VAL}.",
                prerequisites: new List<Enum> { BattleDoctrineSkillPath.ForwardObservationPost },
                primaryBonusType: SkillBonusType.IndirectRange, primaryBonusValue: INDIRECT_RANGE_BONUS_VAL
            ));
        }

        private static void InitializeCombatOperationsSkills()
        {
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.PursuitDoctrine, "Pursuit Doctrine", TIER4_XP_COST,
                "Enables breakthrough following a retreat.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.BreakthroughCapability
            ));
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.OffensiveDoctrine, "Offensive Doctrine", TIER4_XP_COST,
                $"Increases Combat Actions by +{GENERIC_STAT_BONUS_VAL}.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.CombatActionBonus, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.ManeuverDoctrine, "Maneuver Doctrine", TIER4_XP_COST,
                $"Increases Movement Actions by +{GENERIC_STAT_BONUS_VAL}.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.MovementActionBonus, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
            
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.SpecialistCorps, "Specialist Corps", TIER4_XP_COST,
                $"Increases Special operations by +{GENERIC_STAT_BONUS_VAL}.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.DeploymentActionBonus, primaryBonusValue: GENERIC_STAT_BONUS_VAL
            ));
            AddSkill(new SkillDefinition(
                CombatOperationsSkillPath.ReconnaissanceInForce, "Reconnaissance In Force", TIER4_XP_COST,
                "Enhances reconnaissance capabilities.",
                requiredGrade: CommandGrade.TopGrade,
                primaryBonusType: SkillBonusType.DetectionRange, primaryBonusValue: DETECTION_RANGE_BONUS_VAL
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