using System;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    public class CommanderSkillTree
    {
        #region Constants
        public const int XP_PER_BATTLE = 25;
        public const int XP_FOR_JUNIOR_GRADE = 0;
        public const int XP_FOR_SENIOR_GRADE = 150;
        public const int XP_FOR_TOP_GRADE = 400;

        // Skill costs by tier
        public const int TIER1_SKILL_COST = 1;  // Junior Grade starting skills
        public const int TIER2_SKILL_COST = 2;  // Junior Grade advanced skills
        public const int TIER3_SKILL_COST = 3;  // Senior Grade skills
        public const int TIER4_SKILL_COST = 4;  // Top Grade skills

        // Combat effectiveness bonuses
        public const float EXPERIENCE_BONUS = 0.25f;
        public const float COMMAND_RATING_BONUS = 0.1f;
        public const float MASKIROVKA_DETECTION_BONUS = 1.0f;  // +1 detection range
        public const float INITIATIVE_BONUS = 0.1f;

        // Supply and logistics bonuses
        public const float SUPPLY_ECONOMY_BONUS = 0.33f;
        public const float PRESTIGE_COST_REDUCTION = 0.33f;

        // Combat bonuses
        public const int HARD_DEFENSE_BONUS = 5;
        public const int SOFT_DEFENSE_BONUS = 5;
        public const int HARD_ATTACK_BONUS = 5;
        public const int DETECTION_RANGE_BONUS = 3;
        public const int NIGHT_FIGHT_BONUS = 1;
        public const int ENTRENCHMENT_BONUS = 1;
        public const int INDIRECT_RANGE_BONUS = 1;
        public const int AIR_DEFENSE_BONUS = 5;

        // Combat operation bonuses
        public const int COMBAT_BONUS = 1;
        public const int MOVEMENT_BONUS = 1;
        public const int SPECIAL_BONUS = 1;
        #endregion

        #region Enums
        public enum CommandGrade { JuniorGrade, SeniorGrade, TopGrade }

        public enum CommandSkillPath
        {
            None,
            ShockFormation,      // Tier 1 (junior) - player must choose this OR Maskirovka
            IronDiscipline,      // Tier 2 (junior) - requires ShockFormation
            MaskirovkaMaster,    // Tier 1 (junior) - player must choose this OR ShockFormation
            TacticalGenius,      // Tier 2 (junior) - requires MaskirovkaMaster
            PoliticalOfficer,    // Tier 3 (senior) - requires IronDiscipline and SeniorGrade
            OperationalArt,      // Tier 3 (senior) - requires TacticalGenius and SeniorGrade
            HeroOfSovietUnion,   // Tier 4 (top) - requires PoliticalOfficer and TopGrade
            DeepBattleTheorist   // Tier 4 (top) - requires OperationalArt and TopGrade
        }

        public enum RearAreaSkillPath
        {
            None,
            SupplyEconomy,
            FieldWorkshop,
            PartyConnections,
            StrategicAirlift
        }

        public enum BattleDoctrineSkillPath
        {
            None,
            ArmoredWarfare,
            T72Regiment,
            ShockTankCorps,
            NightFightingSpecialist,
            DefenseInDepth,
            HedgehogDefense,
            FortificationEngineer,
            TrenchWarfareExpert,
            RocketArtillery,
            ForwardObservationPost,
            IntegratedAirDefenseSystem,
            KatyushaBarrage
        }

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
        #endregion

        #region Fields and Properties
        public int ExperiencePoints { get; private set; }
        public int AvailableSkillPoints { get; private set; }
        public CommandGrade CurrentGrade { get; private set; }
        public bool IsSeniorPromotionAvailable => ExperiencePoints >= XP_FOR_SENIOR_GRADE && CurrentGrade == CommandGrade.JuniorGrade;
        public bool IsTopPromotionAvailable => ExperiencePoints >= XP_FOR_TOP_GRADE && CurrentGrade == CommandGrade.SeniorGrade;
        public Dictionary<CommandSkillPath, bool> CommandSkills { get; private set; }
        public Dictionary<RearAreaSkillPath, bool> RearAreaSkills { get; private set; }
        public Dictionary<BattleDoctrineSkillPath, bool> BattleDoctrineSkills { get; private set; }
        public Dictionary<CombatOperationsSkillPath, bool> CombatOperationsSkills { get; private set; }

        public event Action<string, string> OnSkillUnlocked;
        public event Action<CommandGrade> OnGradeChanged;
        public event Action<int, int> OnExperienceGained;
        public event Action<int> OnSkillPointsAwarded;
        public event Action OnPromotionAvailable;
        #endregion

        public CommanderSkillTree()
        {
            ExperiencePoints = 0;
            AvailableSkillPoints = TIER1_SKILL_COST; // Start with enough for first tier skill
            CurrentGrade = CommandGrade.JuniorGrade;
            InitializeSkillDictionaries();
        }

        private void InitializeSkillDictionaries()
        {
            CommandSkills = new Dictionary<CommandSkillPath, bool>();
            foreach (CommandSkillPath skill in Enum.GetValues(typeof(CommandSkillPath)))
                if (skill != CommandSkillPath.None)
                    CommandSkills[skill] = false;

            RearAreaSkills = new Dictionary<RearAreaSkillPath, bool>();
            foreach (RearAreaSkillPath skill in Enum.GetValues(typeof(RearAreaSkillPath)))
                if (skill != RearAreaSkillPath.None)
                    RearAreaSkills[skill] = false;

            BattleDoctrineSkills = new Dictionary<BattleDoctrineSkillPath, bool>();
            foreach (BattleDoctrineSkillPath skill in Enum.GetValues(typeof(BattleDoctrineSkillPath)))
                if (skill != BattleDoctrineSkillPath.None)
                    BattleDoctrineSkills[skill] = false;

            CombatOperationsSkills = new Dictionary<CombatOperationsSkillPath, bool>();
            foreach (CombatOperationsSkillPath skill in Enum.GetValues(typeof(CombatOperationsSkillPath)))
                if (skill != CombatOperationsSkillPath.None)
                    CombatOperationsSkills[skill] = false;
        }

        public void AddExperience(int experienceAmount)
        {
            if (experienceAmount <= 0) return;

            int oldExperience = ExperiencePoints;
            ExperiencePoints += experienceAmount;

            // Check if promotion is available
            UpdateGrade();

            // Notify about experience gain
            OnExperienceGained?.Invoke(experienceAmount, ExperiencePoints);
        }

        private void UpdateGrade()
        {
            CommandGrade oldGrade = CurrentGrade;

            // We don't automatically promote - promotions require explicit calls to PromoteToNextGrade
            // Just check if promotion is available and notify
            if (CurrentGrade == CommandGrade.JuniorGrade && ExperiencePoints >= XP_FOR_SENIOR_GRADE)
            {
                OnPromotionAvailable?.Invoke();
            }
            else if (CurrentGrade == CommandGrade.SeniorGrade && ExperiencePoints >= XP_FOR_TOP_GRADE)
            {
                OnPromotionAvailable?.Invoke();
            }
        }

        public bool PromoteToNextGrade()
        {
            // Check if promotion is possible
            if (CurrentGrade == CommandGrade.JuniorGrade && ExperiencePoints >= XP_FOR_SENIOR_GRADE)
            {
                // Promote to Senior Grade
                CurrentGrade = CommandGrade.SeniorGrade;

                // Award skill points
                AvailableSkillPoints += TIER3_SKILL_COST; // Give enough points for a Tier 3 skill

                // Notify
                OnGradeChanged?.Invoke(CurrentGrade);
                OnSkillPointsAwarded?.Invoke(AvailableSkillPoints);

                return true;
            }
            else if (CurrentGrade == CommandGrade.SeniorGrade && ExperiencePoints >= XP_FOR_TOP_GRADE)
            {
                // Promote to Top Grade
                CurrentGrade = CommandGrade.TopGrade;

                // Award skill points
                AvailableSkillPoints += TIER4_SKILL_COST; // Give enough points for a Tier 4 skill

                // Notify
                OnGradeChanged?.Invoke(CurrentGrade);
                OnSkillPointsAwarded?.Invoke(AvailableSkillPoints);

                return true;
            }

            return false;
        }

        public bool UnlockCommandSkill(CommandSkillPath skill)
        {
            if (skill == CommandSkillPath.None || CommandSkills[skill])
                return false;

            if (!CanUnlockCommandSkill(skill))
                return false;

            // Get the skill cost based on its tier
            int skillCost = GetSkillCost(skill);

            if (AvailableSkillPoints < skillCost)
                return false;

            CommandSkills[skill] = true;
            AvailableSkillPoints -= skillCost;

            OnSkillUnlocked?.Invoke(skill.ToString(), GetSkillDescription(skill));

            return true;
        }

        private int GetSkillCost(CommandSkillPath skill)
        {
            switch (skill)
            {
                case CommandSkillPath.ShockFormation:
                case CommandSkillPath.MaskirovkaMaster:
                    return TIER1_SKILL_COST;

                case CommandSkillPath.IronDiscipline:
                case CommandSkillPath.TacticalGenius:
                    return TIER2_SKILL_COST;

                case CommandSkillPath.PoliticalOfficer:
                case CommandSkillPath.OperationalArt:
                    return TIER3_SKILL_COST;

                case CommandSkillPath.HeroOfSovietUnion:
                case CommandSkillPath.DeepBattleTheorist:
                    return TIER4_SKILL_COST;

                default:
                    return TIER1_SKILL_COST;
            }
        }

        public bool UnlockRearAreaSkill(RearAreaSkillPath skill)
        {
            if (skill == RearAreaSkillPath.None || RearAreaSkills[skill])
                return false;

            if (!CanUnlockRearAreaSkill(skill))
                return false;

            // Get skill tier cost based on its position in the tree
            int skillCost = skill switch
            {
                RearAreaSkillPath.SupplyEconomy => TIER1_SKILL_COST,
                RearAreaSkillPath.FieldWorkshop => TIER1_SKILL_COST,
                RearAreaSkillPath.PartyConnections => TIER2_SKILL_COST,
                RearAreaSkillPath.StrategicAirlift => TIER3_SKILL_COST,
                _ => 1
            };

            if (AvailableSkillPoints < skillCost)
                return false;

            RearAreaSkills[skill] = true;
            AvailableSkillPoints -= skillCost;

            OnSkillUnlocked?.Invoke(skill.ToString(), GetSkillDescription(skill));

            return true;
        }

        public bool UnlockBattleDoctrineSkill(BattleDoctrineSkillPath skill)
        {
            if (skill == BattleDoctrineSkillPath.None || BattleDoctrineSkills[skill])
                return false;

            if (!CanUnlockBattleDoctrineSkill(skill))
                return false;

            // Get skill tier cost based on its position in the tree
            int skillCost = skill switch
            {
                BattleDoctrineSkillPath.ArmoredWarfare => TIER1_SKILL_COST,
                BattleDoctrineSkillPath.DefenseInDepth => TIER1_SKILL_COST,

                BattleDoctrineSkillPath.T72Regiment => TIER2_SKILL_COST,
                BattleDoctrineSkillPath.HedgehogDefense => TIER2_SKILL_COST,

                BattleDoctrineSkillPath.ShockTankCorps => TIER2_SKILL_COST,
                BattleDoctrineSkillPath.FortificationEngineer => TIER2_SKILL_COST,

                BattleDoctrineSkillPath.NightFightingSpecialist => TIER3_SKILL_COST,
                BattleDoctrineSkillPath.TrenchWarfareExpert => TIER3_SKILL_COST,
                BattleDoctrineSkillPath.RocketArtillery => TIER3_SKILL_COST,

                BattleDoctrineSkillPath.ForwardObservationPost => TIER3_SKILL_COST,
                BattleDoctrineSkillPath.IntegratedAirDefenseSystem => TIER4_SKILL_COST,
                BattleDoctrineSkillPath.KatyushaBarrage => TIER4_SKILL_COST,
                _ => 1
            };

            if (AvailableSkillPoints < skillCost)
                return false;

            BattleDoctrineSkills[skill] = true;
            AvailableSkillPoints -= skillCost;

            OnSkillUnlocked?.Invoke(skill.ToString(), GetSkillDescription(skill));

            return true;
        }

        public bool UnlockCombatOperationsSkill(CombatOperationsSkillPath skill)
        {
            if (skill == CombatOperationsSkillPath.None || CombatOperationsSkills[skill])
                return false;

            if (!CanUnlockCombatOperationsSkill(skill))
                return false;

            // Get skill tier cost based on its position in the tree
            int skillCost = skill switch
            {
                CombatOperationsSkillPath.OffensiveDoctrine => TIER1_SKILL_COST,
                CombatOperationsSkillPath.ManeuverDoctrine => TIER1_SKILL_COST,
                CombatOperationsSkillPath.PursuitDoctrine => TIER1_SKILL_COST,
                CombatOperationsSkillPath.SpecialistCorps => TIER1_SKILL_COST,

                CombatOperationsSkillPath.ReconnaissanceInForce => TIER2_SKILL_COST,
                CombatOperationsSkillPath.CombinedArmsWarfare => TIER3_SKILL_COST,
                _ => 1
            };

            if (AvailableSkillPoints < skillCost)
                return false;

            CombatOperationsSkills[skill] = true;
            AvailableSkillPoints -= skillCost;

            OnSkillUnlocked?.Invoke(skill.ToString(), GetSkillDescription(skill));

            return true;
        }

        private bool CanUnlockCommandSkill(CommandSkillPath skill)
        {
            // First check for mutually exclusive skills in Tier 1
            if (skill == CommandSkillPath.ShockFormation && CommandSkills[CommandSkillPath.MaskirovkaMaster])
                return false; // Can't choose ShockFormation if already chose MaskirovkaMaster

            if (skill == CommandSkillPath.MaskirovkaMaster && CommandSkills[CommandSkillPath.ShockFormation])
                return false; // Can't choose MaskirovkaMaster if already chose ShockFormation

            // Check grade requirements for higher tier skills
            switch (skill)
            {
                case CommandSkillPath.PoliticalOfficer:
                case CommandSkillPath.OperationalArt:
                    if (CurrentGrade < CommandGrade.SeniorGrade)
                        return false;
                    break;

                case CommandSkillPath.HeroOfSovietUnion:
                case CommandSkillPath.DeepBattleTheorist:
                    if (CurrentGrade < CommandGrade.TopGrade)
                        return false;
                    break;
            }

            // Check prerequisites in the skill tree
            switch (skill)
            {
                case CommandSkillPath.IronDiscipline:
                    return CommandSkills[CommandSkillPath.ShockFormation];

                case CommandSkillPath.TacticalGenius:
                    return CommandSkills[CommandSkillPath.MaskirovkaMaster];

                case CommandSkillPath.PoliticalOfficer:
                    return CommandSkills[CommandSkillPath.IronDiscipline] && CurrentGrade >= CommandGrade.SeniorGrade;

                case CommandSkillPath.OperationalArt:
                    return CommandSkills[CommandSkillPath.TacticalGenius] && CurrentGrade >= CommandGrade.SeniorGrade;

                case CommandSkillPath.HeroOfSovietUnion:
                    return CommandSkills[CommandSkillPath.PoliticalOfficer] && CurrentGrade >= CommandGrade.TopGrade;

                case CommandSkillPath.DeepBattleTheorist:
                    return CommandSkills[CommandSkillPath.OperationalArt] && CurrentGrade >= CommandGrade.TopGrade;

                case CommandSkillPath.ShockFormation:
                case CommandSkillPath.MaskirovkaMaster:
                    return true; // Tier 1 skills are always available (except for mutual exclusivity, checked above)

                default:
                    return false;
            }
        }

        private bool CanUnlockRearAreaSkill(RearAreaSkillPath skill)
        {
            switch (skill)
            {
                case RearAreaSkillPath.PartyConnections:
                    return RearAreaSkills[RearAreaSkillPath.SupplyEconomy] &&
                           RearAreaSkills[RearAreaSkillPath.FieldWorkshop];
                case RearAreaSkillPath.StrategicAirlift:
                    return RearAreaSkills[RearAreaSkillPath.PartyConnections];
                case RearAreaSkillPath.SupplyEconomy:
                case RearAreaSkillPath.FieldWorkshop:
                    return true;
                default:
                    return false;
            }
        }

        private bool CanUnlockBattleDoctrineSkill(BattleDoctrineSkillPath skill)
        {
            switch (skill)
            {
                case BattleDoctrineSkillPath.T72Regiment:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.ArmoredWarfare];
                case BattleDoctrineSkillPath.ShockTankCorps:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.T72Regiment];
                case BattleDoctrineSkillPath.NightFightingSpecialist:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.ShockTankCorps];
                case BattleDoctrineSkillPath.HedgehogDefense:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.DefenseInDepth];
                case BattleDoctrineSkillPath.FortificationEngineer:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.HedgehogDefense];
                case BattleDoctrineSkillPath.TrenchWarfareExpert:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.FortificationEngineer];
                case BattleDoctrineSkillPath.RocketArtillery:
                    return (BattleDoctrineSkills[BattleDoctrineSkillPath.NightFightingSpecialist] ||
                            BattleDoctrineSkills[BattleDoctrineSkillPath.TrenchWarfareExpert]);
                case BattleDoctrineSkillPath.ForwardObservationPost:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.RocketArtillery];
                case BattleDoctrineSkillPath.IntegratedAirDefenseSystem:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.ForwardObservationPost];
                case BattleDoctrineSkillPath.KatyushaBarrage:
                    return BattleDoctrineSkills[BattleDoctrineSkillPath.IntegratedAirDefenseSystem];
                case BattleDoctrineSkillPath.ArmoredWarfare:
                case BattleDoctrineSkillPath.DefenseInDepth:
                    return true;
                default:
                    return false;
            }
        }

        private bool CanUnlockCombatOperationsSkill(CombatOperationsSkillPath skill)
        {
            switch (skill)
            {
                case CombatOperationsSkillPath.ReconnaissanceInForce:
                    return (CombatOperationsSkills[CombatOperationsSkillPath.OffensiveDoctrine] ||
                            CombatOperationsSkills[CombatOperationsSkillPath.ManeuverDoctrine] ||
                            CombatOperationsSkills[CombatOperationsSkillPath.PursuitDoctrine] ||
                            CombatOperationsSkills[CombatOperationsSkillPath.SpecialistCorps]);
                case CombatOperationsSkillPath.CombinedArmsWarfare:
                    return CombatOperationsSkills[CombatOperationsSkillPath.ReconnaissanceInForce];
                case CombatOperationsSkillPath.OffensiveDoctrine:
                case CombatOperationsSkillPath.ManeuverDoctrine:
                case CombatOperationsSkillPath.PursuitDoctrine:
                case CombatOperationsSkillPath.SpecialistCorps:
                    return true;
                default:
                    return false;
            }
        }

        // Bonus calculation methods
        public float GetExperienceBonus() => CommandSkills[CommandSkillPath.ShockFormation] ? 1.0f + EXPERIENCE_BONUS : 1.0f;

        public int GetMaskirovkaDetectionBonus() => CommandSkills[CommandSkillPath.MaskirovkaMaster] ? (int)MASKIROVKA_DETECTION_BONUS : 0;

        public int GetCommandRatingBonus()
        {
            int bonus = 0;
            if (CommandSkills[CommandSkillPath.IronDiscipline]) bonus += 1;
            if (CommandSkills[CommandSkillPath.PoliticalOfficer]) bonus += 1;
            if (CommandSkills[CommandSkillPath.HeroOfSovietUnion]) bonus += 1;
            return bonus;
        }

        public int GetInitiativeBonus()
        {
            int bonus = 0;
            if (CommandSkills[CommandSkillPath.TacticalGenius]) bonus += 1;
            if (CommandSkills[CommandSkillPath.OperationalArt]) bonus += 1;
            if (CommandSkills[CommandSkillPath.DeepBattleTheorist]) bonus += 1;
            return bonus;
        }

        public float GetSupplyConsumptionMultiplier() =>
            RearAreaSkills[RearAreaSkillPath.SupplyEconomy] ? Math.Max(0.1f, 1.0f - SUPPLY_ECONOMY_BONUS) : 1.0f;

        public float GetPrestigeCostMultiplier() =>
            RearAreaSkills[RearAreaSkillPath.FieldWorkshop] ? Math.Max(0.1f, 1.0f - PRESTIGE_COST_REDUCTION) : 1.0f;

        public bool HasEmergencyResupply() => RearAreaSkills[RearAreaSkillPath.StrategicAirlift];

        public int GetHardDefenseBonus()
        {
            int bonus = 0;
            if (BattleDoctrineSkills[BattleDoctrineSkillPath.T72Regiment]) bonus += HARD_DEFENSE_BONUS;
            if (BattleDoctrineSkills[BattleDoctrineSkillPath.FortificationEngineer]) bonus += HARD_DEFENSE_BONUS;
            return bonus;
        }

        public int GetSoftDefenseBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.HedgehogDefense] ? SOFT_DEFENSE_BONUS : 0;

        public int GetHardAttackBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.ShockTankCorps] ? HARD_ATTACK_BONUS : 0;

        public int GetDetectionRangeBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.ForwardObservationPost] ? DETECTION_RANGE_BONUS : 0;

        public int GetNightFightingBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.NightFightingSpecialist] ? NIGHT_FIGHT_BONUS : 0;

        public int GetEntrenchmentBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.TrenchWarfareExpert] ? ENTRENCHMENT_BONUS : 0;

        public int GetIndirectRangeBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.KatyushaBarrage] ? INDIRECT_RANGE_BONUS : 0;

        public int GetAirDefenseBonus() =>
            BattleDoctrineSkills[BattleDoctrineSkillPath.IntegratedAirDefenseSystem] ? AIR_DEFENSE_BONUS : 0;

        public int GetCombatBonus() =>
            CombatOperationsSkills[CombatOperationsSkillPath.OffensiveDoctrine] ? COMBAT_BONUS : 0;

        public int GetMovementBonus() =>
            CombatOperationsSkills[CombatOperationsSkillPath.ManeuverDoctrine] ? MOVEMENT_BONUS : 0;

        public int GetSpecialBonus() =>
            CombatOperationsSkills[CombatOperationsSkillPath.SpecialistCorps] ? SPECIAL_BONUS : 0;

        public bool HasBreakthroughCapability() =>
            CombatOperationsSkills[CombatOperationsSkillPath.PursuitDoctrine];

        public bool HasCombinedArmsWarfare() =>
            CombatOperationsSkills[CombatOperationsSkillPath.CombinedArmsWarfare];

        private string GetSkillDescription(object skill)
        {
            if (skill is CommandSkillPath commandSkill)
                return GetCommandSkillDescription(commandSkill);
            else if (skill is RearAreaSkillPath rearAreaSkill)
                return GetRearAreaSkillDescription(rearAreaSkill);
            else if (skill is BattleDoctrineSkillPath battleDoctrineSkill)
                return GetBattleDoctrineSkillDescription(battleDoctrineSkill);
            else if (skill is CombatOperationsSkillPath combatOperationsSkill)
                return GetCombatOperationsSkillDescription(combatOperationsSkill);

            return "Unknown skill";
        }

        private string GetCommandSkillDescription(CommandSkillPath skill)
        {
            switch (skill)
            {
                case CommandSkillPath.ShockFormation:
                    return $"Tier 1 ({TIER1_SKILL_COST}pt): Increases experience gain by {EXPERIENCE_BONUS * 100}%. You must choose this OR Maskirovka Master.";
                case CommandSkillPath.IronDiscipline:
                    return $"Tier 2 ({TIER2_SKILL_COST}pt): Increases Command Rating by +1. Requires Shock Formation.";
                case CommandSkillPath.MaskirovkaMaster:
                    return $"Tier 1 ({TIER1_SKILL_COST}pt): Increases Detection Range by +{MASKIROVKA_DETECTION_BONUS}. You must choose this OR Shock Formation.";
                case CommandSkillPath.TacticalGenius:
                    return $"Tier 2 ({TIER2_SKILL_COST}pt): Increases Initiative by +1. Requires Maskirovka Master.";
                case CommandSkillPath.PoliticalOfficer:
                    return $"Tier 3 ({TIER3_SKILL_COST}pt): Increases Command Rating by +1. Requires Iron Discipline and Senior Grade promotion.";
                case CommandSkillPath.OperationalArt:
                    return $"Tier 3 ({TIER3_SKILL_COST}pt): Increases Initiative by +1. Requires Tactical Genius and Senior Grade promotion.";
                case CommandSkillPath.HeroOfSovietUnion:
                    return $"Tier 4 ({TIER4_SKILL_COST}pt): Increases Command Rating by +1. Requires Political Officer and Top Grade promotion.";
                case CommandSkillPath.DeepBattleTheorist:
                    return $"Tier 4 ({TIER4_SKILL_COST}pt): Increases Initiative by +1. Requires Operational Art and Top Grade promotion.";
                default:
                    return "Unknown Command Doctrine skill";
            }
        }

        private string GetRearAreaSkillDescription(RearAreaSkillPath skill)
        {
            switch (skill)
            {
                case RearAreaSkillPath.SupplyEconomy:
                    return $"Reduces supply consumption to {(1 - SUPPLY_ECONOMY_BONUS) * 100}%.";
                case RearAreaSkillPath.FieldWorkshop:
                    return $"Reduces repair costs to {(1 - PRESTIGE_COST_REDUCTION) * 100}%.";
                case RearAreaSkillPath.PartyConnections:
                    return "Enables access to equipment upgrades.";
                case RearAreaSkillPath.StrategicAirlift:
                    return "Enables emergency resupply operations.";
                default:
                    return "Unknown Rear Area Operations skill";
            }
        }

        private string GetBattleDoctrineSkillDescription(BattleDoctrineSkillPath skill)
        {
            switch (skill)
            {
                case BattleDoctrineSkillPath.ArmoredWarfare:
                    return "Specializes in armored operations.";
                case BattleDoctrineSkillPath.T72Regiment:
                    return $"Increases Hard Defense by +{HARD_DEFENSE_BONUS}.";
                case BattleDoctrineSkillPath.ShockTankCorps:
                    return $"Increases Hard Attack by +{HARD_ATTACK_BONUS}.";
                case BattleDoctrineSkillPath.NightFightingSpecialist:
                    return $"Increases Night Fighting capability by +{NIGHT_FIGHT_BONUS}.";
                case BattleDoctrineSkillPath.DefenseInDepth:
                    return "Specializes in defensive operations.";
                case BattleDoctrineSkillPath.HedgehogDefense:
                    return $"Increases Soft Defense by +{SOFT_DEFENSE_BONUS}.";
                case BattleDoctrineSkillPath.FortificationEngineer:
                    return $"Increases Hard Defense by +{HARD_DEFENSE_BONUS}.";
                case BattleDoctrineSkillPath.TrenchWarfareExpert:
                    return $"Increases Entrenchment by +{ENTRENCHMENT_BONUS}.";
                case BattleDoctrineSkillPath.RocketArtillery:
                    return "Enables advanced artillery operations.";
                case BattleDoctrineSkillPath.ForwardObservationPost:
                    return $"Increases Detection Range by +{DETECTION_RANGE_BONUS}.";
                case BattleDoctrineSkillPath.IntegratedAirDefenseSystem:
                    return $"Increases Air Defense by +{AIR_DEFENSE_BONUS}.";
                case BattleDoctrineSkillPath.KatyushaBarrage:
                    return $"Increases Indirect Range by +{INDIRECT_RANGE_BONUS}.";
                default:
                    return "Unknown Battle Doctrine skill";
            }
        }

        private string GetCombatOperationsSkillDescription(CombatOperationsSkillPath skill)
        {
            switch (skill)
            {
                case CombatOperationsSkillPath.OffensiveDoctrine:
                    return $"Increases Combat by +{COMBAT_BONUS}.";
                case CombatOperationsSkillPath.ManeuverDoctrine:
                    return $"Increases Movement by +{MOVEMENT_BONUS}.";
                case CombatOperationsSkillPath.PursuitDoctrine:
                    return "Enables breakthrough after combat.";
                case CombatOperationsSkillPath.SpecialistCorps:
                    return $"Increases Special operations by +{SPECIAL_BONUS}.";
                case CombatOperationsSkillPath.ReconnaissanceInForce:
                    return "Enhances reconnaissance capabilities.";
                case CombatOperationsSkillPath.CombinedArmsWarfare:
                    return "Master of coordinated operations across all branches.";
                default:
                    return "Unknown Combat Operations skill";
            }
        }

        public bool ResetAllSkills()
        {
            int unlockedSkillCount = 0;

            foreach (var pair in CommandSkills)
                if (pair.Value) unlockedSkillCount++;

            foreach (var pair in RearAreaSkills)
                if (pair.Value) unlockedSkillCount++;

            foreach (var pair in BattleDoctrineSkills)
                if (pair.Value) unlockedSkillCount++;

            foreach (var pair in CombatOperationsSkills)
                if (pair.Value) unlockedSkillCount++;

            if (unlockedSkillCount == 0)
                return false;

            foreach (CommandSkillPath skill in Enum.GetValues(typeof(CommandSkillPath)))
                if (skill != CommandSkillPath.None)
                    CommandSkills[skill] = false;

            foreach (RearAreaSkillPath skill in Enum.GetValues(typeof(RearAreaSkillPath)))
                if (skill != RearAreaSkillPath.None)
                    RearAreaSkills[skill] = false;

            foreach (BattleDoctrineSkillPath skill in Enum.GetValues(typeof(BattleDoctrineSkillPath)))
                if (skill != BattleDoctrineSkillPath.None)
                    BattleDoctrineSkills[skill] = false;

            foreach (CombatOperationsSkillPath skill in Enum.GetValues(typeof(CombatOperationsSkillPath)))
                if (skill != CombatOperationsSkillPath.None)
                    CombatOperationsSkills[skill] = false;

            AvailableSkillPoints += unlockedSkillCount;
            OnSkillPointsAwarded?.Invoke(AvailableSkillPoints);

            return true;
        }
    }
}