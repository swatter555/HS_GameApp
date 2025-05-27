using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// LeaderSkillTree manages a leader's skills, reputation points, and command grade progression.
    /// 
    /// SKILL SYSTEM DESIGN:
    /// The skill system is built around strategic specialization with universal gating mechanisms:
    /// 
    /// BRANCH TYPES:
    /// • Foundation Branches: Universal branches available to all leaders
    ///   - LeadershipFoundation: Required for progression, contains promotions that gate access to higher tiers
    ///   - PoliticallyConnectedFoundation: Optional utility branch with logistical bonuses
    /// 
    /// • Doctrine Branches: Combat specializations (MUTUALLY EXCLUSIVE - choose only 1)
    ///   - ArmoredDoctrine, InfantryDoctrine, ArtilleryDoctrine, AirDefenseDoctrine
    ///   - AirborneDoctrine, AirMobileDoctrine, IntelligenceDoctrine
    /// 
    /// • Specialization Branches: Advanced capabilities (MUTUALLY EXCLUSIVE - choose only 1)
    ///   - CombinedArmsSpecialization, SignalIntelligenceSpecialization
    ///   - EngineeringSpecialization, SpecialForcesSpecialization
    /// 
    /// PROGRESSION GATING:
    /// Leadership acts as the universal progression gate through command grade promotions:
    /// • JuniorGrade (starting): Access to Tier1 skills in any branch
    /// • SeniorGrade: Unlocks Tier2-3 skills across all branches
    /// • TopGrade: Unlocks Tier4-5 specialization skills
    /// 
    /// This design forces meaningful REP investment decisions - a fair amount of a leader's REP 
    /// must go to Leadership promotions to access higher-tier abilities, preventing early 
    /// specialization abuse while ensuring every leader has strong command capabilities.
    /// 
    /// STRATEGIC CHOICE ARCHITECTURE:
    /// • Every leader progresses: Leadership + Optional PoliticallyConnected
    /// • Each leader chooses: 1 Doctrine + 1 Specialization
    /// • Result: Distinct leader archetypes (Tank Commander, Artillery Specialist, etc.)
    /// • No "master of everything" builds possible due to exclusivity rules
    /// 
    /// CORE FUNCTIONALITY:
    /// - Storage and management of which skills a leader has unlocked
    /// - Enforcement of branch exclusivity rules (1 doctrine, 1 specialization max)
    /// - Command grade promotion management and tier unlocking
    /// - Calculation of all skill-based bonuses for combat and other systems
    /// - Validation of skill prerequisites and command grade requirements
    /// - Support for resetting skills (respec functionality)
    /// - Event notifications for UI and other systems
    /// 
    /// KEY METHODS:
    /// - AddReputation: Adds REP to the leader's pool
    /// - CanUnlockSkill: Validates prerequisites, grade requirements, and branch exclusivity
    /// - UnlockSkill: Unlocks skills, handles promotions, and applies effects
    /// - IsSkillUnlocked: Checks if a specific skill is unlocked
    /// - GetBonusValue: Retrieves total bonus for a specific bonus type
    /// - HasCapability: Checks if the leader has a specific boolean capability
    /// - ResetAllSkills/ResetBranch: Refunds reputation from skills (respects promotions)
    /// 
    /// USAGE EXAMPLE - Typical Leader Progression:
    /// ```csharp
    /// var skillTree = new LeaderSkillTree();
    /// skillTree.AddReputation(200);  // Award REP after battles
    /// 
    /// // Start with Leadership foundation
    /// skillTree.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
    /// 
    /// // Choose a doctrine specialization  
    /// skillTree.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);
    /// 
    /// // Promote to access higher tiers (costs 200 XP but unlocks Tier2-3 globally)
    /// skillTree.UnlockSkill(LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion);
    /// 
    /// // Now can access Tier2 armored skills
    /// skillTree.UnlockSkill(ArmoredDoctrine.HullDownExpert_HardDefense);
    /// 
    /// // Use bonuses in combat calculations
    /// int commandBonus = (int)skillTree.GetBonusValue(SkillBonusType.CommandTier1);
    /// bool hasBreakthrough = skillTree.HasCapability(SkillBonusType.Breakthrough);
    /// 
    /// // Check branch availability (will return false for other doctrines)
    /// bool canStartInfantry = skillTree.IsBranchAvailable(SkillBranch.InfantryDoctrine);
    /// ```
    /// </summary>
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

        #endregion

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

        #endregion

        #region Events

        // Notification events for UI and other systems
        public event Action<Enum, string, string> OnSkillUnlocked; // (skillEnum, skillName, fullSkillDescription)
        public event Action<CommandGrade> OnGradeChanged;
        public event Action<int, int> OnReputationChanged; // (changeAmount, newTotalReputation)
        public event Action<CommandGrade> OnPromotionAvailable; // (targetPromotionGrade)
        public event Action<SkillBranch, SkillTier> OnBranchTierUnlocked; // (branch, tier)
        public event Action<SkillBonusType> OnCapabilityUnlocked; // (bonusType)

        #endregion


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

        #endregion


        #region Reputation Management

        /// <summary>
        /// Adds reputation points to the leader's pool.
        /// </summary>
        /// <param name="reputationAmount">Base experience to add</param>
        public void AddReputation(int reputationAmount)
        {
            if (reputationAmount <= 0) return;

            ReputationPoints += reputationAmount;

            OnReputationChanged?.Invoke(reputationAmount, ReputationPoints);
            CheckPromotionAvailability();
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
            OnReputationChanged?.Invoke(-reputationAmount, ReputationPoints);
            CheckPromotionAvailability();
            return true;
        }

        /// <summary>
        /// Checks if a promotion is available and fires the appropriate event
        /// </summary>
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

        #endregion


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

                // Fire grade change event first
                OnGradeChanged?.Invoke(CurrentGrade);
            }

            // Spend the experience
            if (!SpendReputation(skillDef.REPCost)) return false;

            // Track the branch if this is the first skill in it
            startedBranches.Add(skillDef.Branch);

            // Mark the skill as unlocked
            unlockedSkills[skillEnum] = true;

            ClearBonusCaches();

            // Fire events
            OnSkillUnlocked?.Invoke(skillEnum, skillDef.Name, skillDef.GetFullDescription());
            OnBranchTierUnlocked?.Invoke(skillDef.Branch, skillDef.Tier);

            // Fire capability events for each effect that is boolean
            foreach (var effect in skillDef.Effects)
            {
                if (effect.IsBoolean)
                {
                    OnCapabilityUnlocked?.Invoke(effect.BonusType);
                }
            }

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
            // ---- Defensive guard ----------------------------------------------------
            if (branch == SkillBranch.None)
            {
                Debug.LogError("LeaderSkillTree.IsBranchAvailable: SkillBranch.None is invalid input.");
                return false;
            }

            // ---- Foundation branches stack with everything -------------------------
            if (branch == SkillBranch.LeadershipFoundation ||
                branch == SkillBranch.PoliticallyConnectedFoundation)
                return true;

            bool isDoctrine = branch.IsDoctrine();
            bool isSpecialization = branch.IsSpecialization();

            // ---- Doctrine / specialization exclusivity -----------------------------
            if (isDoctrine)
                return !startedBranches.Any(b => b.IsDoctrine());

            if (isSpecialization)
                return !startedBranches.Any(b => b.IsSpecialization());

            // ---- Unknown branch category -------------------------------------------
            Debug.LogError($"LeaderSkillTree.IsBranchAvailable: Unhandled branch type: {branch}");
            return false;
        }

        #endregion


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

        #endregion


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
                OnReputationChanged?.Invoke(refundedXP, ReputationPoints);

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
                OnReputationChanged?.Invoke(refundedREP, ReputationPoints);

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
                OnReputationChanged?.Invoke(refundedXP, ReputationPoints);

                // Clear caches
                ClearBonusCaches();
            }

            return skillsWereReset;
        }

        #endregion


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

        #endregion


        #region Serialization Support

        /// <summary>
        /// Saves the current state to a serializable data object
        /// </summary>
        /// <returns>Serializable skill tree data</returns>
        public LeaderSkillTreeData ToSerializableData()
        {
            var data = new LeaderSkillTreeData
            {
                ReputationPoints = this.ReputationPoints,
                CurrentGrade = this.CurrentGrade,
                StartedBranches = this.startedBranches.Select(b => (int)b).ToList(),
                UnlockedSkills = new List<SkillReference>()
            };

            foreach (var kvp in unlockedSkills.Where(kvp => kvp.Value))
            {
                // Only save skills that are actually unlocked
                var skillRef = new SkillReference
                {
                    EnumType = kvp.Key.GetType().Name,
                    EnumValue = Convert.ToInt32(kvp.Key)
                };

                data.UnlockedSkills.Add(skillRef);
            }

            return data;
        }

        /// <summary>
        /// Loads state from serialized data
        /// </summary>
        /// <param name="data">The serialized skill tree data</param>
        public void FromSerializableData(LeaderSkillTreeData data)
        {
            if (data == null) return;

            // Reset current state
            unlockedSkills.Clear();
            startedBranches.Clear();
            InitializeSkillDictionaries();

            // Load basic properties
            ReputationPoints = data.ReputationPoints;
            CurrentGrade = data.CurrentGrade;

            // Load branches
            foreach (int branchId in data.StartedBranches)
            {
                startedBranches.Add((SkillBranch)branchId);
            }

            // Load skills
            foreach (var skillRef in data.UnlockedSkills)
            {
                Enum skillEnum = DeserializeSkillEnum(skillRef);
                if (skillEnum != null)
                {
                    unlockedSkills[skillEnum] = true;
                }
            }

            // Clear caches
            ClearBonusCaches();
        }

        /// <summary>
        /// Deserializes a skill enum from a skill reference
        /// </summary>
        private Enum DeserializeSkillEnum(SkillReference skillRef)
        {
            try
            {
                // Convert enum type name to Type
                Type enumType = null;
                switch (skillRef.EnumType)
                {
                    case nameof(LeadershipFoundation): enumType = typeof(LeadershipFoundation); break;
                    case nameof(ArmoredDoctrine): enumType = typeof(ArmoredDoctrine); break;
                    case nameof(InfantryDoctrine): enumType = typeof(InfantryDoctrine); break;
                    case nameof(ArtilleryDoctrine): enumType = typeof(ArtilleryDoctrine); break;
                    case nameof(AirDefenseDoctrine): enumType = typeof(AirDefenseDoctrine); break;
                    case nameof(AirborneDoctrine): enumType = typeof(AirborneDoctrine); break;
                    case nameof(AirMobileDoctrine): enumType = typeof(AirMobileDoctrine); break;
                    case nameof(IntelligenceDoctrine): enumType = typeof(IntelligenceDoctrine); break;
                    case nameof(CombinedArmsSpecialization): enumType = typeof(CombinedArmsSpecialization); break;
                    case nameof(SignalIntelligenceSpecialization): enumType = typeof(SignalIntelligenceSpecialization); break;
                    case nameof(EngineeringSpecialization): enumType = typeof(EngineeringSpecialization); break;
                    case nameof(SpecialForcesSpecialization): enumType = typeof(SpecialForcesSpecialization); break;
                    case nameof(PoliticallyConnectedFoundation): enumType = typeof(PoliticallyConnectedFoundation); break;
                    default: return null;
                }

                // Convert int value to enum
                return (Enum)Enum.ToObject(enumType, skillRef.EnumValue);
            }
            catch
            {
                // If any error occurs, return null
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Serializable data structure for saving/loading leader skill trees
    /// </summary>
    [Serializable]
    public class LeaderSkillTreeData
    {
        public int ReputationPoints;
        public CommandGrade CurrentGrade;
        public List<int> StartedBranches;
        public List<SkillReference> UnlockedSkills;
    }

    /// <summary>
    /// Serializable reference to a skill enum
    /// </summary>
    [Serializable]
    public class SkillReference
    {
        public string EnumType;  // Name of the enum type
        public int EnumValue;    // Integer value of the enum
    }

    /// <summary>
    /// Extension helpers that classify <see cref="SkillBranch"/> values.
    /// Keep these in sync with the enum definition.
    /// </summary>
    public static class SkillBranchExtensions
    {
        /// <summary>
        /// Returns <c>true</c> when the branch is a Doctrine (mutually exclusive group).
        /// </summary>
        public static bool IsDoctrine(this SkillBranch branch)
        {
            return branch switch
            {
                SkillBranch.ArmoredDoctrine 
                or SkillBranch.InfantryDoctrine 
                or SkillBranch.ArtilleryDoctrine 
                or SkillBranch.AirDefenseDoctrine 
                or SkillBranch.AirborneDoctrine 
                or SkillBranch.AirMobileDoctrine 
                or SkillBranch.IntelligenceDoctrine => true,
                _ => false,
            };
        }

        /// <summary>
        /// Returns <c>true</c> when the branch is a Specialization (mutually exclusive group).
        /// </summary>
        public static bool IsSpecialization(this SkillBranch branch)
        {
            return branch switch
            {
                SkillBranch.SignalIntelligenceSpecialization 
                or SkillBranch.CombinedArmsSpecialization 
                or SkillBranch.EngineeringSpecialization 
                or SkillBranch.SpecialForcesSpecialization => true,
                _ => false,
            };
        }
    }
}