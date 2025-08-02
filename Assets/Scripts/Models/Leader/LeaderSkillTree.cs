using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Serializable data structure for saving/loading leader skill trees
    /// </summary>
    [Serializable]
    public class LeaderSkillTreeData
    {
        public int ReputationPoints;
        public CommandGrade CurrentGrade;
        public List<string> StartedBranches;
        public List<SkillReference> UnlockedSkills;
    }

    /// <summary>
    /// Serializable reference to a skill enum
    /// </summary>
    [Serializable]
    public class SkillReference
    {
        public string EnumType;  // IntelProfileID of the enum type
        public int EnumValue;    // Integer value of the enum
    }

    [Serializable]
    public class LeaderSkillTree
    {
        #region Fields

        // Dictionary to store all unlocked skills regardless of branch
        private readonly Dictionary<Enum, bool> unlockedSkills = new Dictionary<Enum, bool>();

        // Set to track which branches the leader has started
        private readonly HashSet<SkillBranch> startedBranches = new HashSet<SkillBranch>();

        // Cache for frequently accessed bonus values
        private readonly Dictionary<SkillBonusType, float> bonusCache = new Dictionary<SkillBonusType, float>();

        // Cache for boolean capabilities
        private readonly Dictionary<SkillBonusType, bool> capabilityCache = new Dictionary<SkillBonusType, bool>();

        // Flag to track if caches need to be cleared
        private bool isDirty = true;

        #endregion // Fields

        #region Properties

        // Reputation and Command Grade
        public int ReputationPoints { get; private set; }
        public CommandGrade CurrentGrade { get; private set; }

        // Promotion convenience properties
        public bool CanAffordSeniorPromotion =>
            ReputationPoints >= CUConstants.REP_COST_FOR_SENIOR_PROMOTION &&
            CurrentGrade == CommandGrade.JuniorGrade;
        public bool CanAffordTopPromotion =>
            ReputationPoints >= CUConstants.REP_COST_FOR_TOP_PROMOTION &&
            CurrentGrade == CommandGrade.SeniorGrade;

        // Branch-related properties
        public IReadOnlyCollection<SkillBranch> ActiveBranches => startedBranches;

        // Skill-count properties
        public int TotalSkillsUnlocked => unlockedSkills.Count(kv => kv.Value);

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Creates a new LeaderSkillTree with optional initial experience
        /// </summary>
        /// <param name="initialReputation">Starting experience points (default: 0)</param>
        public LeaderSkillTree(int initialReputation = 0)
        {
            ReputationPoints = initialReputation;
            CurrentGrade = CommandGrade.JuniorGrade;
            InitializeSkillDictionaries();
        }

        /// <summary>
        /// Initialize the skill dictionaries with all possible skills set to not unlocked
        /// Uses reflection to automatically discover all skill enum types
        /// </summary>
        private void InitializeSkillDictionaries()
        {
            var skillEnumTypes = GetAllSkillEnumTypes();

            foreach (var enumType in skillEnumTypes)
            {
                foreach (Enum skill in Enum.GetValues(enumType))
                {
                    // Skip "None" values (assume None = 0 for all skill enums)
                    if (Convert.ToInt32(skill) == 0) continue;

                    unlockedSkills[skill] = false;
                }
            }
        }

        /// <summary>
        /// Uses reflection to find all enum types that represent skill branches
        /// </summary>
        private static List<Type> GetAllSkillEnumTypes()
        {
            var skillEnumTypes = new List<Type>();

            // Get all enum types from the Models namespace that end with expected suffixes
            var modelTypes = typeof(LeadershipFoundation).Assembly.GetTypes()
                .Where(t => t.IsEnum && t.Namespace == "HammerAndSickle.Models");

            foreach (var type in modelTypes)
            {
                // Check if this enum represents a skill branch by checking naming conventions
                if (IsSkillEnumType(type))
                {
                    skillEnumTypes.Add(type);
                }
            }

            return skillEnumTypes;
        }

        /// <summary>
        /// Determines if an enum type represents a skill branch based on naming conventions
        /// </summary>
        private static bool IsSkillEnumType(Type enumType)
        {
            return enumType.GetCustomAttribute<SkillBranchEnumAttribute>() != null;
        }

        #endregion // Constructors


        #region Reputation Management

        /// <summary>
        /// Adds reputation points to the leader's pool.
        /// </summary>
        /// <param name="reputationAmount">Base experience to add</param>
        public void AddReputation(int reputationAmount)
        {
            if (reputationAmount <= 0) return;

            ReputationPoints += reputationAmount;
        }

        /// <summary>
        /// Spends experience points from the leader's pool
        /// </summary>
        /// <param name="reputationAmount">Amount to spend</param>
        /// <returns>True if the experience was successfully spent</returns>
        private bool SpendReputation(int reputationAmount)
        {
            if (reputationAmount <= 0 || ReputationPoints < reputationAmount) return false;

            ReputationPoints -= reputationAmount;
            return true;
        }

        #endregion // Reputation Management


        #region Skill Management

        /// <summary>
        /// Checks if a skill can be unlocked, considering prerequisites, branch exclusivity,
        /// command grade requirements, and available experience points
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill can be unlocked, false otherwise</returns>
        public bool CanUnlockSkill(Enum skillEnum)
        {
            if (Convert.ToInt32(skillEnum) == 0) return false; // 'None' enum value
            if (IsSkillUnlocked(skillEnum)) return false; // Already unlocked

            // Get the skill definition from the catalog
            if (!LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
            {
                return false; // Skill not found in catalog
            }

            // Check XP cost
            if (ReputationPoints < skillDef.REPCost) return false;

            // Check command grade requirement
            if (CurrentGrade < skillDef.RequiredGrade) return false;

            // Check branch exclusivity
            if (!IsBranchAvailable(skillDef.Branch)) return false;

            // Check prerequisites
            foreach (Enum prereqEnum in skillDef.Prerequisites)
            {
                if (!IsSkillUnlocked(prereqEnum)) return false;
            }

            // Check mutually exclusive skills
            foreach (Enum exclusiveEnum in skillDef.MutuallyExclusive)
            {
                if (IsSkillUnlocked(exclusiveEnum)) return false;
            }

            // Skill is available to unlock
            return true;
        }

        /// <summary>
        /// Attempts to unlock a skill, spending experience and applying effects
        /// </summary>
        /// <param name="skillEnum">The skill to unlock</param>
        /// <returns>True if the skill was successfully unlocked</returns>
        public bool UnlockSkill(Enum skillEnum)
        {
            if (!CanUnlockSkill(skillEnum)) return false;

            if (!LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
            {
                return false; // Should never happen if CanUnlockSkill returned true
            }

            // Handle promotion before spending reputation.
            var primaryEffect = skillDef.GetPrimaryEffect();
            bool isPromotion = primaryEffect?.BonusType == SkillBonusType.SeniorPromotion ||
                              primaryEffect?.BonusType == SkillBonusType.TopPromotion;
            if (isPromotion)
            {
                // Update grade first so reputation events fire with correct context
                if (primaryEffect.BonusType == SkillBonusType.SeniorPromotion)
                {
                    CurrentGrade = CommandGrade.SeniorGrade;
                }
                else if (primaryEffect.BonusType == SkillBonusType.TopPromotion)
                {
                    CurrentGrade = CommandGrade.TopGrade;
                }
            }

            // Spend the experience
            if (!SpendReputation(skillDef.REPCost)) return false;

            // Track the branch if this is the first skill in it
            startedBranches.Add(skillDef.Branch);

            // Mark the skill as unlocked
            unlockedSkills[skillEnum] = true;

            ClearBonusCaches();

            return true;
        }

        /// <summary>
        /// Checks if a specific skill is unlocked
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill is unlocked</returns>
        public bool IsSkillUnlocked(Enum skillEnum)
        {
            if (Convert.ToInt32(skillEnum) == 0) return false; // 'None' enum
            return unlockedSkills.TryGetValue(skillEnum, out bool isUnlocked) && isUnlocked;
        }

        /// <summary>
        /// Checks if the leader has started a specific branch
        /// </summary>
        /// <param name="branch">The branch to check</param>
        /// <returns>True if the leader has at least one skill in this branch</returns>
        public bool HasStartedBranch(SkillBranch branch)
        {
            return startedBranches.Contains(branch);
        }

        /// <summary>
        /// Checks if a branch is available to start, considering exclusivity rules
        /// </summary>
        /// <param name="branch">The branch to check</param>
        /// <returns>True if the branch can be started</returns>
        public bool IsBranchAvailable(SkillBranch branch)
        {
            // Defensive guard
            if (branch == SkillBranch.None)
            {
                Debug.LogError("LeaderSkillTree.IsBranchAvailable: SkillBranch.None is invalid input.");
                return false;
            }

            // Foundation branches can always be started (they stack with everything)
            if (branch.IsFoundation())
                return true;

            // For doctrine branches, check if any other doctrine has been started
            if (branch.IsDoctrine())
            {
                return !startedBranches.Any(b => b.IsDoctrine());
            }

            // For specialization branches, check if any other specialization has been started
            if (branch.IsSpecialization())
            {
                return !startedBranches.Any(b => b.IsSpecialization());
            }

            // This should never happen if all branches are properly attributed
            Debug.LogError($"LeaderSkillTree.IsBranchAvailable: Unknown branch type for {branch}");
            return false;
        }

        #endregion // Skill Management


        #region Validate Tree System

        /// <summary>
        /// Validates the skill tree system configuration. 
        /// Call this during initialization or in editor to verify everything is set up correctly.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void ValidateSkillTreeSystem()
        {
            try
            {
                Debug.Log("=== Skill Tree System Validation ===");

                // Validate branch classification
                SkillBranchExtensions.ValidateBranchClassification();

                // Test branch type queries
                var foundationCount = SkillBranchExtensions.GetFoundationBranches().Count();
                var doctrineCount = SkillBranchExtensions.GetDoctrineBranches().Count();
                var specializationCount = SkillBranchExtensions.GetSpecializationBranches().Count();

                Debug.Log($"Branch counts - Foundation: {foundationCount}, Doctrine: {doctrineCount}, Specialization: {specializationCount}");

                // Test some specific branches
                Debug.Log($"Test classifications:");
                Debug.Log($"  LeadershipFoundation.IsFoundation(): {SkillBranch.LeadershipFoundation.IsFoundation()}");
                Debug.Log($"  ArmoredDoctrine.IsDoctrine(): {SkillBranch.ArmoredDoctrine.IsDoctrine()}");
                Debug.Log($"  EngineeringSpecialization.IsSpecialization(): {SkillBranch.EngineeringSpecialization.IsSpecialization()}");

                Debug.Log("✓ Skill Tree System validation completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Skill Tree System validation failed: {ex.Message}");
            }
        }

        #endregion // Validate Tree System


        #region Bonus Calculations

        /// <summary>
        /// Gets the total bonus value for a specific bonus type from all unlocked skills
        /// </summary>
        /// <param name="bonusType">The type of bonus to calculate</param>
        /// <param name="onlyBoolean">If true, only consider boolean effects</param>
        /// <returns>The total bonus value (additive for most, multiplicative for some types)</returns>
        public float GetBonusValue(SkillBonusType bonusType, bool onlyBoolean = false)
        {
            if (!isDirty && bonusCache.TryGetValue(bonusType, out float cachedValue))
            {
                return cachedValue;
            }

            float totalBonus = IsMultiplicativeBonus(bonusType) ? 1.0f : 0f;

            ProcessUnlockedSkills(skillDef =>
            {
                foreach (var effect in skillDef.Effects)
                {
                    if (effect.BonusType != bonusType) continue;
                    if (onlyBoolean && !effect.IsBoolean) continue;

                    totalBonus = IsMultiplicativeBonus(bonusType)
                        ? totalBonus * effect.BonusValue
                        : totalBonus + effect.BonusValue;
                }
            });

            bonusCache[bonusType] = totalBonus;
            isDirty = false;
            return totalBonus;
        }

        /// <summary>
        /// Checks if the leader has a specific boolean capability
        /// </summary>
        /// <param name="bonusType">The capability to check for</param>
        /// <returns>True if the leader has this capability</returns>
        public bool HasCapability(SkillBonusType bonusType)
        {
            if (!isDirty && capabilityCache.TryGetValue(bonusType, out bool cachedValue))
            {
                return cachedValue;
            }

            bool hasCapability = false;

            ProcessUnlockedSkills(skillDef =>
            {
                foreach (var effect in skillDef.Effects)
                {
                    if (effect.BonusType == bonusType && effect.IsBoolean && effect.BonusValue > 0)
                    {
                        hasCapability = true;
                        break;
                    }
                }
            },
            stopEarlyIf: () => hasCapability);

            capabilityCache[bonusType] = hasCapability;
            isDirty = false;
            return hasCapability;
        }

        /// <summary>
        /// Helper method to determine if a bonus type should be calculated multiplicatively
        /// </summary>
        private bool IsMultiplicativeBonus(SkillBonusType bonusType)
        {
            // These bonus types stack multiplicatively
            return bonusType == SkillBonusType.SupplyConsumption ||
                   bonusType == SkillBonusType.PrestigeCost;
        }

        /// <summary>
        /// Clears all bonus caches, forcing recalculation on next access
        /// </summary>
        private void ClearBonusCaches()
        {
            bonusCache.Clear();
            capabilityCache.Clear();
            isDirty = true;
        }

        #endregion // Bonus Calculations


        #region Skill Reset (Respec)

        /// <summary>
        /// Resets all skills, refunding experience points
        /// </summary>
        /// <returns>True if any skills were reset</returns>
        public bool ResetAllSkills()
        {
            int refundedXP = 0;
            bool skillsWereReset = false;

            // Get all unlocked skills
            var unlockedSkillKeys = unlockedSkills.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

            foreach (Enum skillEnum in unlockedSkillKeys)
            {
                if (LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
                {
                    // Skip promotion skills - once promoted, always promoted
                    if (skillDef.GetPrimaryEffect()?.BonusType == SkillBonusType.SeniorPromotion ||
                        skillDef.GetPrimaryEffect()?.BonusType == SkillBonusType.TopPromotion)
                    {
                        continue;
                    }

                    refundedXP += skillDef.REPCost;
                    unlockedSkills[skillEnum] = false;
                    skillsWereReset = true;
                }
            }

            if (skillsWereReset)
            {
                // Reset branch tracking (except LeadershipFoundation which is tied to promotions)
                startedBranches.RemoveWhere(b =>
                    b != SkillBranch.LeadershipFoundation);

                // Add refunded XP
                ReputationPoints += refundedXP;

                // Clear caches
                ClearBonusCaches();
            }

            return skillsWereReset;
        }

        /// <summary>
        /// Resets all skills in a specific branch, refunding reputation points
        /// </summary>
        /// <param name="branch">The branch to reset</param>
        /// <returns>True if any skills were reset</returns>
        public bool ResetBranch(SkillBranch branch)
        {
            // Cannot reset LeadershipFoundation branch (contains promotions)
            if (branch == SkillBranch.LeadershipFoundation) return false;

            int refundedREP = 0;
            bool skillsWereReset = false;

            // Get all unlocked skills in this branch
            var branchSkills = unlockedSkills.Where(kvp => kvp.Value).Select(kvp => kvp.Key)
                .Where(key =>
                {
                    if (LeaderSkillCatalog.TryGetSkillDefinition(key, out SkillDefinition def))
                    {
                        return def.Branch == branch;
                    }
                    return false;
                })
                .ToList();

            foreach (Enum skillEnum in branchSkills)
            {
                if (LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
                {
                    refundedREP += skillDef.REPCost;
                    unlockedSkills[skillEnum] = false;
                    skillsWereReset = true;
                }
            }

            if (skillsWereReset)
            {
                // Remove branch from tracking
                startedBranches.Remove(branch);

                // Add refunded XP
                ReputationPoints += refundedREP;

                // Clear caches
                ClearBonusCaches();
            }

            return skillsWereReset;
        }

        /// <summary>
        /// Resets all skills except those in the LeadershipFoundation branch
        /// </summary>
        /// <returns>True if any skills were reset</returns>
        public bool ResetAllSkillsExceptLeadership()
        {
            int refundedXP = 0;
            bool skillsWereReset = false;

            // Get all unlocked skills not in LeadershipFoundation branch
            var nonLeadershipSkills = unlockedSkills.Where(kvp => kvp.Value).Select(kvp => kvp.Key)
                .Where(key =>
                {
                    if (LeaderSkillCatalog.TryGetSkillDefinition(key, out SkillDefinition def))
                    {
                        return def.Branch != SkillBranch.LeadershipFoundation;
                    }
                    return false;
                })
                .ToList();

            foreach (Enum skillEnum in nonLeadershipSkills)
            {
                if (LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
                {
                    refundedXP += skillDef.REPCost;
                    unlockedSkills[skillEnum] = false;
                    skillsWereReset = true;
                }
            }

            if (skillsWereReset)
            {
                // Reset branch tracking (except LeadershipFoundation)
                startedBranches.RemoveWhere(b =>
                    b != SkillBranch.LeadershipFoundation);

                // Add refunded XP
                ReputationPoints += refundedXP;

                // Clear caches
                ClearBonusCaches();
            }

            return skillsWereReset;
        }

        #endregion // Skill Reset


        #region Helper Methods

        /// <summary>
        /// Helper to iterate over all unlocked skills and apply an action
        /// </summary>
        /// <param name="action">Action to apply to each unlocked skill definition</param>
        /// <param name="stopEarlyIf">Optional function to check if iteration should stop early</param>
        private void ProcessUnlockedSkills(Action<SkillDefinition> action, Func<bool> stopEarlyIf = null)
        {
            // Get all unlocked skills
            var unlockedSkillKeys = unlockedSkills.Where(kvp => kvp.Value).Select(kvp => kvp.Key);

            foreach (Enum skillEnum in unlockedSkillKeys)
            {
                // Stop early if requested
                if (stopEarlyIf != null && stopEarlyIf()) return;

                if (LeaderSkillCatalog.TryGetSkillDefinition(skillEnum, out SkillDefinition skillDef))
                {
                    action(skillDef);
                }
            }
        }

        #endregion // Helper Methods
    }
}