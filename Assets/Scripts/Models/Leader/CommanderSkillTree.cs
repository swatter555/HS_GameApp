using System;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Models
{
    public class CommanderSkillTree
    {
        #region Properties

        public int ExperiencePoints { get; private set; }
        public CommandGrade CurrentGrade { get; private set; }

        public bool CanAffordSeniorPromotion => ExperiencePoints >= CUConstants.XP_COST_FOR_SENIOR_PROMOTION && CurrentGrade == CommandGrade.JuniorGrade;
        public bool CanAffordTopPromotion => ExperiencePoints >= CUConstants.XP_COST_FOR_TOP_PROMOTION && CurrentGrade == CommandGrade.SeniorGrade;

        // Dictionaries to store the unlocked status of each skill for this commander
        public Dictionary<LeadershipPath, bool> UnlockedCommandSkills { get; private set; }
        public Dictionary<RearAreaSkillPath, bool> UnlockedRearAreaSkills { get; private set; }
        public Dictionary<BattleDoctrineSkillPath, bool> UnlockedBattleDoctrineSkills { get; private set; }
        public Dictionary<CombatOperationsSkillPath, bool> UnlockedCombatOperationsSkills { get; private set; }

        // Events
        public event Action<string, string> OnSkillUnlocked; // (skillName, fullSkillDescription)
        public event Action<CommandGrade> OnGradeChanged;
        public event Action<int, int> OnExperienceChanged; // (changeAmount, newTotalExperience)
        public event Action<CommandGrade> OnPromotionAvailable; // (targetPromotionGrade)

        #endregion // Properties

        public CommanderSkillTree(int initialExperience = 0)
        {
            ExperiencePoints = initialExperience;
            CurrentGrade = CommandGrade.JuniorGrade;
            InitializeSkillDictionaries();
        }

        private void InitializeSkillDictionaries()
        {
            UnlockedCommandSkills = Enum.GetValues(typeof(LeadershipPath)).Cast<LeadershipPath>()
                                     .Where(s => s != LeadershipPath.None)
                                     .ToDictionary(s => s, s => false);
            UnlockedRearAreaSkills = Enum.GetValues(typeof(RearAreaSkillPath)).Cast<RearAreaSkillPath>()
                                     .Where(s => s != RearAreaSkillPath.None)
                                     .ToDictionary(s => s, s => false);
            UnlockedBattleDoctrineSkills = Enum.GetValues(typeof(BattleDoctrineSkillPath)).Cast<BattleDoctrineSkillPath>()
                                     .Where(s => s != BattleDoctrineSkillPath.None)
                                     .ToDictionary(s => s, s => false);
            UnlockedCombatOperationsSkills = Enum.GetValues(typeof(CombatOperationsSkillPath)).Cast<CombatOperationsSkillPath>()
                                     .Where(s => s != CombatOperationsSkillPath.None)
                                     .ToDictionary(s => s, s => false);
        }

        public void AddExperience(int experienceAmount)
        {
            if (experienceAmount <= 0) return;

            float currentExperienceBonus = 1.0f;
            if (IsSkillUnlocked(LeadershipPath.ShockFormation))
            {
                if (LeaderSkillCatalog.TryGetSkillDefinition(LeadershipPath.ShockFormation, out var skillDef))
                {
                    currentExperienceBonus += skillDef.PrimaryBonusValue; // Assumes bonus value is the additive part (0.25)
                }
            }

            int finalExperienceToAdd = (int)(experienceAmount * currentExperienceBonus);
            ExperiencePoints += finalExperienceToAdd;
            OnExperienceChanged?.Invoke(finalExperienceToAdd, ExperiencePoints);
            CheckPromotionAvailability();
        }

        private void SpendExperience(int experienceAmount)
        {
            if (experienceAmount <= 0 || ExperiencePoints < experienceAmount) return; // Ensure enough XP
            ExperiencePoints -= experienceAmount;
            OnExperienceChanged?.Invoke(-experienceAmount, ExperiencePoints);
            CheckPromotionAvailability();
        }

        private void CheckPromotionAvailability()
        {
            if (CanAffordSeniorPromotion)
            {
                OnPromotionAvailable?.Invoke(CommandGrade.SeniorGrade);
            }
            else if (CanAffordTopPromotion)
            {
                OnPromotionAvailable?.Invoke(CommandGrade.TopGrade);
            }
        }

        public bool PromoteToNextGrade()
        {
            if (CurrentGrade == CommandGrade.JuniorGrade && CanAffordSeniorPromotion)
            {
                SpendExperience(CUConstants.XP_COST_FOR_SENIOR_PROMOTION);
                CurrentGrade = CommandGrade.SeniorGrade;
                OnGradeChanged?.Invoke(CurrentGrade);
                CheckPromotionAvailability(); // Check if Top Grade is now affordable
                return true;
            }
            else if (CurrentGrade == CommandGrade.SeniorGrade && CanAffordTopPromotion)
            {
                SpendExperience(CUConstants.XP_COST_FOR_TOP_PROMOTION);
                CurrentGrade = CommandGrade.TopGrade;
                OnGradeChanged?.Invoke(CurrentGrade);
                return true;
            }
            return false;
        }

        public bool UnlockSkill(Enum skillEnum)
        {
            if (Convert.ToInt32(skillEnum) == 0) return false; // 'None' enum
            if (IsSkillUnlocked(skillEnum)) return false;

            if (!LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
            {
                // Skill not found in catalog, should not happen if enums and catalog are synced
                return false;
            }

            if (!CanUnlockSkill(skillEnum, skillDef)) return false;

            if (ExperiencePoints < skillDef.XPCost) return false;

            SpendExperience(skillDef.XPCost);
            SetSkillUnlockedStatus(skillEnum, true);
            OnSkillUnlocked?.Invoke(skillDef.Name, LeaderSkillCatalog.GetFullSkillDescription(skillEnum));
            return true;
        }

        private bool CanUnlockSkill(Enum skillEnum, SkillDefinition skillDef)
        {
            // 1. Check Grade Requirement
            if (CurrentGrade < skillDef.RequiredGrade) return false;

            // 2. Check Prerequisites
            foreach (Enum prereqEnum in skillDef.Prerequisites)
            {
                if (!IsSkillUnlocked(prereqEnum)) return false;
            }

            // 3. Check Mutually Exclusive Skills
            foreach (Enum exclusiveEnum in skillDef.MutuallyExclusive)
            {
                if (IsSkillUnlocked(exclusiveEnum)) return false;
            }

            // 4. Special Cases from original logic (if any not covered by SkillDefinition structure)
            // Example: QueenOfBattle OR logic, ReconnaissanceInForce OR logic
            if (skillEnum is BattleDoctrineSkillPath bdSkill && bdSkill == BattleDoctrineSkillPath.QueenOfBattle)
            {
                bool nightFighting = IsSkillUnlocked(BattleDoctrineSkillPath.NightFightingSpecialist);
                bool trenchWarfare = IsSkillUnlocked(BattleDoctrineSkillPath.TrenchWarfareExpert);
                if (!nightFighting && !trenchWarfare) return false; // Needs at least one
            }
            if (skillEnum is CombatOperationsSkillPath coSkill && coSkill == CombatOperationsSkillPath.ReconnaissanceInForce)
            {
                bool hasTier1CO = IsSkillUnlocked(CombatOperationsSkillPath.OffensiveDoctrine) ||
                                  IsSkillUnlocked(CombatOperationsSkillPath.ManeuverDoctrine) ||
                                  IsSkillUnlocked(CombatOperationsSkillPath.PursuitDoctrine) ||
                                  IsSkillUnlocked(CombatOperationsSkillPath.SpecialistCorps);
                if (!hasTier1CO) return false;
            }

            return true;
        }

        public bool IsSkillUnlocked(Enum skillEnum)
        {
            return skillEnum switch
            {
                LeadershipPath csp => UnlockedCommandSkills.TryGetValue(csp, out bool unlocked) && unlocked,
                RearAreaSkillPath rasp => UnlockedRearAreaSkills.TryGetValue(rasp, out bool unlocked) && unlocked,
                BattleDoctrineSkillPath bdsp => UnlockedBattleDoctrineSkills.TryGetValue(bdsp, out bool unlocked) && unlocked,
                CombatOperationsSkillPath cosp => UnlockedCombatOperationsSkills.TryGetValue(cosp, out bool unlocked) && unlocked,
                _ => false
            };
        }

        private void SetSkillUnlockedStatus(Enum skillEnum, bool isUnlocked)
        {
            switch (skillEnum)
            {
                case LeadershipPath csp: UnlockedCommandSkills[csp] = isUnlocked; break;
                case RearAreaSkillPath rasp: UnlockedRearAreaSkills[rasp] = isUnlocked; break;
                case BattleDoctrineSkillPath bdsp: UnlockedBattleDoctrineSkills[bdsp] = isUnlocked; break;
                case CombatOperationsSkillPath cosp: UnlockedCombatOperationsSkills[cosp] = isUnlocked; break;
            }
        }

        public bool ResetAllSkills()
        {
            int refundedXP = 0;
            bool skillsWereReset = false;

            void ResetCategory<TEnum>(Dictionary<TEnum, bool> skillsDict) where TEnum : Enum
            {
                var keys = skillsDict.Keys.ToList(); // Avoid modification during iteration
                foreach (TEnum skillKey in keys)
                {
                    if (skillsDict[skillKey]) // If skill is unlocked
                    {
                        if (LeaderSkillCatalog.TryGetSkillDefinition(skillKey, out var skillDef))
                        {
                            refundedXP += skillDef.XPCost;
                        }
                        skillsDict[skillKey] = false;
                        skillsWereReset = true;
                    }
                }
            }

            ResetCategory(UnlockedCommandSkills);
            ResetCategory(UnlockedRearAreaSkills);
            ResetCategory(UnlockedBattleDoctrineSkills);
            ResetCategory(UnlockedCombatOperationsSkills);

            if (skillsWereReset)
            {
                ExperiencePoints += refundedXP; // Directly add XP back
                OnExperienceChanged?.Invoke(refundedXP, ExperiencePoints);
                CheckPromotionAvailability();
            }
            // Note: Does not reset CommandGrade. Promotions are persistent.
            return skillsWereReset;
        }

        // --- Bonus Calculation Methods ---
        // These methods now iterate unlocked skills and sum bonuses based on definitions in LeaderSkillCatalog

        public float GetTotalExperienceBonusMultiplier() // Returns total multiplier, e.g., 1.25 for +25%
        {
            float totalMultiplier = 1.0f;
            ProcessUnlockedSkills((skillDef) => {
                if (skillDef.PrimaryBonusType == SkillBonusType.UnitXP)
                {
                    totalMultiplier += skillDef.PrimaryBonusValue; // Assumes value is additive (0.25)
                }
            });
            return totalMultiplier;
        }

        public int GetTotalCommandRatingBonus()
        {
            int totalBonus = 0;
            ProcessUnlockedSkills(skillDef => {
                if (skillDef.PrimaryBonusType == SkillBonusType.Command) totalBonus += (int)skillDef.PrimaryBonusValue;
            });
            return totalBonus;
        }

        public float GetTotalSupplyConsumptionMultiplier() // e.g., 0.67 for 33% reduction
        {
            float finalMultiplier = 1.0f;
            ProcessUnlockedSkills(skillDef => {
                if (skillDef.PrimaryBonusType == SkillBonusType.SupplyConsumption)
                {
                    // If multiple skills provide this, they should stack multiplicatively
                    finalMultiplier *= skillDef.PrimaryBonusValue;
                }
            });
            return Math.Max(0.1f, finalMultiplier); // Ensure it doesn't go below a certain threshold
        }

        public float GetTotalPrestigeCostMultiplier() // e.g., 0.67 for 33% reduction
        {
            float finalMultiplier = 1.0f;
            ProcessUnlockedSkills(skillDef => {
                if (skillDef.PrimaryBonusType == SkillBonusType.PrestigeCost)
                {
                    finalMultiplier *= skillDef.PrimaryBonusValue;
                }
            });
            return Math.Max(0.1f, finalMultiplier);
        }

        public bool HasEmergencyResupply()
        {
            bool hasSkill = false;
            ProcessUnlockedSkills(skillDef => {
                if (skillDef.PrimaryBonusType == SkillBonusType.EmergencyResupply) hasSkill = true;
            }, stopEarlyIf: () => hasSkill);
            return hasSkill;
        }

        // Add other GetTotal...Bonus methods similarly for HardDefense, SoftDefense, etc.
        public int GetTotalHardDefenseBonus()
        {
            int totalBonus = 0;
            ProcessUnlockedSkills(skillDef => {
                if (skillDef.PrimaryBonusType == SkillBonusType.HardDefense) totalBonus += (int)skillDef.PrimaryBonusValue;
            });
            return totalBonus;
        }
        // ... and so on for all other specific bonuses.

        public bool HasBreakthroughCapability()
        {
            bool hasSkill = false;
            ProcessUnlockedSkills(skillDef => {
                if (skillDef.PrimaryBonusType == SkillBonusType.Breakthrough) hasSkill = true;
            }, stopEarlyIf: () => hasSkill);
            return hasSkill;
        }

        /// <summary>
        /// Helper to iterate over all unlocked skills of all types and apply an action.
        /// </summary>
        private void ProcessUnlockedSkills(Action<SkillDefinition> action, Func<bool> stopEarlyIf = null)
        {
            IEnumerable<Enum> allSkillEnums =
                UnlockedCommandSkills.Where(kvp => kvp.Value).Select(kvp => (Enum)kvp.Key)
                .Concat(UnlockedRearAreaSkills.Where(kvp => kvp.Value).Select(kvp => (Enum)kvp.Key))
                .Concat(UnlockedBattleDoctrineSkills.Where(kvp => kvp.Value).Select(kvp => (Enum)kvp.Key))
                .Concat(UnlockedCombatOperationsSkills.Where(kvp => kvp.Value).Select(kvp => (Enum)kvp.Key));

            foreach (Enum skillEnum in allSkillEnums)
            {
                if (stopEarlyIf != null && stopEarlyIf()) return;

                if (LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
                {
                    action(skillDef);
                }
            }
        }
    }
}
