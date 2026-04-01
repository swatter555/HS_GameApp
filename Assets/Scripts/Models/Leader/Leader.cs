using HammerAndSickle.Services;
using System;
using System.Text.Json.Serialization;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents a military leader with attributes such as name, nationality, rank, and command abilities.
    /// </summary>
    /// <remarks>A <see cref="Leader"/> can be assigned to a unit, gain reputation points, and unlock skills
    /// through a skill tree. Leaders are created with either default or specified attributes, and their properties and
    /// methods allow for managing their state, reputation, and skills. This class is designed to support both
    /// player-controlled and AI-controlled leaders.</remarks>
    [Serializable]
    public class Leader
    {
        #region Constants

        private const string CLASS_NAME = nameof(Leader);

        #endregion // Constants

        #region Fields

        private LeaderSkillTree skillTree;

        #endregion // Fields

        #region Properties

        [JsonInclude] [JsonPropertyName("name")]
        public string Name { get; private set; }                             // Use random name generator
        [JsonInclude] [JsonPropertyName("side")]
        public Side Side { get; private set; }                               // Player or AI
        [JsonInclude] [JsonPropertyName("nationality")]
        public Nationality Nationality { get; private set; }                 // Nation of origin
        [JsonInclude] [JsonPropertyName("commandGrade")]
        public CommandGrade CommandGrade { get; private set; }               // Rank of the officer
        [JsonInclude] [JsonPropertyName("reputationPoints")]
        public int ReputationPoints { get; private set; }                    // Points for promotions and skill upgrades
        [JsonInclude] [JsonPropertyName("combatCommand")]
        public CommandAbility CombatCommand { get; private set; }            // Direct combat modifier
        [JsonInclude] [JsonPropertyName("isAssigned")]
        public bool IsAssigned { get; internal set; }                        // Is the officer assigned to a unit?
        [JsonInclude] [JsonPropertyName("leaderID")]
        public string LeaderID { get; private set; }                         // Unique identifier for the officer
        [JsonInclude] [JsonPropertyName("unitID")]
        public string UnitID { get; internal set; }                          // UnitID of the unit assigned to the officer

        [JsonInclude] [JsonPropertyName("portraitId")]
        public string PortraitId { get; private set; }                       // Sprite constant for the officer's portrait

        [JsonInclude] [JsonPropertyName("skillTreeData")]
        public LeaderSkillTreeData SkillTreeData { get; private set; }

        [JsonIgnore]
        public string FormattedRank { get { return GetFormattedRank(); } }   // Real-world rank of the officer

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Creates a new leader with random name based on nationality, setup with default parameters.
        /// </summary>
        /// <param name="side">Player or AI side</param>
        /// <param name="nationality">Nation of origin for name generation and rank formatting</param>
        public Leader(Side side, Nationality nationality)
        {
            try
            {
                InitializeCommonProperties(side, nationality);
                GenerateRandomNameBasedOnNationality();
                InitializeSkillTree();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new leader with specified name and command ability
        /// </summary>
        /// <param name="name">Leader's name</param>
        /// <param name="side">Player or AI side</param>
        /// <param name="nationality">Nation of origin</param>
        /// <param name="command">Command ability level</param>
        public Leader(string name, Side side, Nationality nationality, CommandAbility command)
        {
            try
            {
                InitializeCommonProperties(side, nationality);

                // PrepareBattle and set name
                if (string.IsNullOrWhiteSpace(name) ||
                    name.Length < GameData.MIN_LEADER_NAME_LENGTH ||
                    name.Length > GameData.MAX_LEADER_NAME_LENGTH)
                {
                    throw new ArgumentException($"Leader name must be between {GameData.MIN_LEADER_NAME_LENGTH} and {GameData.MAX_LEADER_NAME_LENGTH} characters");
                }

                Name = name.Trim();

                // PrepareBattle command ability
                if (!Enum.IsDefined(typeof(CommandAbility), command))
                {
                    throw new ArgumentException($"Invalid command ability: {command}");
                }

                CombatCommand = command;
                InitializeSkillTree();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Parameterless constructor for JSON deserialization.
        /// Properties will be set after construction by the JSON deserializer.
        /// </summary>
        [JsonConstructor]
        public Leader()
        {
            try
            {
                // PrepareBattle with safe defaults - JSON will overwrite these
                LeaderID = GenerateUniqueID();
                Name = string.Empty;
                Side = Side.Player;
                Nationality = Nationality.USSR;
                CommandGrade = CommandGrade.JuniorGrade;
                CombatCommand = CommandAbility.Average;
                ReputationPoints = 0;
                IsAssigned = false;
                UnitID = null;
                PortraitId = string.Empty;

                // Skill tree left null — RestoreFromDeserialization() will rebuild it
                // from the deserialized SkillTreeData. All skill tree methods use null-conditional
                // delegation (skillTree?.Method()) so null is safe here.
                skillTree = null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "JsonConstructor", e);
                throw;
            }
        }

        #endregion // Constructors

        #region Initialization Helpers

        /// <summary>
        /// PrepareBattle common properties shared by all constructors
        /// </summary>
        private void InitializeCommonProperties(Side side, Nationality nationality)
        {
            LeaderID = GenerateUniqueID();
            Side = side;
            Nationality = nationality;
            CommandGrade = CommandGrade.JuniorGrade;
            CombatCommand = CommandAbility.Average;
            ReputationPoints = 0;
            IsAssigned = false;
            UnitID = null;
            PortraitId = string.Empty;
        }

        /// <summary>
        /// PrepareBattle the skill tree and wire up events
        /// </summary>
        private void InitializeSkillTree()
        {
            skillTree = new LeaderSkillTree(ReputationPoints);
        }

        /// <summary>
        /// Generate a unique LeaderID for the leader
        /// </summary>
        private string GenerateUniqueID()
        {
            string baseID = GameData.LEADER_ID_PREFIX;
            string randomPart = Guid.NewGuid().ToString("N")[..5].ToUpper();
            return $"{baseID}{randomPart}";
        }

        #endregion // Initialization Helpers

        #region Public Methods

        /// <summary>
        /// Manually set the officer's command ability (for testing or special cases)
        /// </summary>
        /// <param name="command">New command ability level</param>
        public void SetOfficerCommandAbility(CommandAbility command)
        {
            try
            {
                if (!Enum.IsDefined(typeof(CommandAbility), command))
                {
                    throw new ArgumentException($"Invalid command ability: {command}");
                }

                CombatCommand = command;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetOfficerCommandAbility", e);
                throw;
            }
        }

        /// <summary>
        /// Set the officer's name with validation
        /// </summary>
        /// <param name="name">New name for the officer</param>
        /// <returns>True if name was successfully set</returns>
        public bool SetOfficerName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) ||
                    name.Length < GameData.MIN_LEADER_NAME_LENGTH ||
                    name.Length > GameData.MAX_LEADER_NAME_LENGTH)
                {
                    return false;
                }

                Name = name.Trim();
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetOfficerName", e);
                return false;
            }
        }

        /// <summary>
        /// Get formatted rank based on nationality and command grade
        /// </summary>
        /// <returns>Localized rank string</returns>
        public string GetFormattedRank()
        {
            try
            {
                return Nationality switch
                {
                    Nationality.USSR => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Colonel",
                        CommandGrade.SeniorGrade => "Major General",
                        CommandGrade.TopGrade => "Lieutenant General",
                        _ => "Officer",
                    },
                    Nationality.USA or Nationality.UK or Nationality.IQ or Nationality.IR or Nationality.SAUD => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Lieutenant Colonel",
                        CommandGrade.SeniorGrade => "Colonel",
                        CommandGrade.TopGrade => "Brigadier General",
                        _ => "Officer",
                    },
                    Nationality.FRG => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Oberst",
                        CommandGrade.SeniorGrade => "Generalmajor",
                        CommandGrade.TopGrade => "Generalleutnant",
                        _ => "Offizier",
                    },
                    Nationality.FRA => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Colonel",
                        CommandGrade.SeniorGrade => "Général de Brigade",
                        CommandGrade.TopGrade => "Général de Division",
                        _ => "Officier",
                    },
                    Nationality.MJ => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Amir al-Fawj",
                        CommandGrade.SeniorGrade => "Amir al-Mintaqa",
                        CommandGrade.TopGrade => "Amir al-Jihad",
                        _ => "Commander",
                    },
                    _ => CommandGrade.ToString(),
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetFormattedRank", e);
                return "Officer";
            }
        }

        /// <summary>
        /// Set the command grade of the officer.
        /// </summary>
        /// <param name="grade"></param>
        public void SetCommandGrade(CommandGrade grade)
        {
            try
            {
                if (!Enum.IsDefined(typeof(CommandGrade), grade))
                {
                    throw new ArgumentException($"Invalid command grade: {grade}");
                }
                CommandGrade = grade;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetCommandGrade", e);
                throw;
            }
        }

        /// <summary>
        /// Set the unique identifier for this leader.
        /// </summary>
        /// <param name="id"></param>
        public void SetLeaderID(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Leader ID cannot be null or empty");
                }
                LeaderID = id.Trim();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetLeaderID", e);
                throw;
            }
        }

        /// <summary>
        /// Set the portrait identifier for this leader
        /// </summary>
        /// <param name="portraitId">Sprite constant identifying the portrait</param>
        public void SetPortraitId(string portraitId)
        {
            try
            {
                PortraitId = portraitId ?? string.Empty;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetPortraitId", e);
                throw;
            }
        }

        #endregion // Public Methods

        #region Helpers

        /// <summary>
        /// Generates a random name for the officer based on the specified nationality.
        /// </summary>
        /// <remarks>If the generated name is null or empty, a fallback name in the format "Officer-XXXX"
        /// (where XXXX is a random GUID segment) will be used.</remarks>
        /// <param name="nationality">The nationality to use when generating the name. This determines the cultural context for the generated
        /// name.</param>
        private void GenerateRandomNameBasedOnNationality()
        {
            try
            {
                var random = new System.Random();
                if (NameGenService.Instance == null)
                {
                    throw new InvalidOperationException("NameGenService is not available");
                }

                // Generate a random name based on nationality
                Name = NameGenService.Instance.GenerateMaleName(Nationality);

                // Ensure name is valid
                if (string.IsNullOrEmpty(Name))
                {
                    Name = $"Officer-{Guid.NewGuid().ToString()[..8]}";
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RandomlyGenerateMe", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a random low-tier Soviet general with a random name, portrait, lowest rank, no skills, and 60 reputation.
        /// </summary>
        /// <param name="nationality">Must be USSR; other nationalities are not yet supported.</param>
        /// <returns>A new Leader configured as a junior-grade Soviet officer</returns>
        public static Leader GenerateRandomJuniorLeader(Nationality nationality)
        {
            try
            {
                if (nationality != Nationality.USSR)
                {
                    throw new ArgumentException($"Nationality {nationality} is not supported. Only USSR is currently implemented.");
                }

                var leader = new Leader(Side.Player, nationality);

                // Assign 60 starting reputation directly (bypasses skill tree sync since no skills are unlocked)
                leader.SetReputationPoints(60);

                // Assign a random portrait from the Soviet portrait pool
                var random = new System.Random();
                int portraitIndex = random.Next(1, GameData.NUM_PORTRAITS_SOVIET + 1);
                leader.PortraitId = $"Russian{portraitIndex:D2}";

                return leader;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GenerateRandomJuniorLeader), e);
                throw;
            }
        }

        #endregion // Helpers

        #region Reputation Management

        /// <summary>
        /// Award reputation points to the leader
        /// </summary>
        /// <param name="amount">Amount of reputation to award</param>
        public void AwardReputation(int amount)
        {
            try
            {
                if (amount <= 0) return;

                ReputationPoints += amount;
                skillTree.AddReputation(amount);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardReputation", e);
                throw;
            }
        }

        /// <summary>
        /// Award reputation based on specific action type with context modifiers
        /// </summary>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="contextMultiplier">Additional multiplier based on context (e.g., difficulty, unit experience)</param>
        public void AwardReputationForAction(GameData.ReputationAction actionType, float contextMultiplier = 1.0f)
        {
            try
            {
                int baseREP = actionType switch
                {
                    GameData.ReputationAction.Move => GameData.REP_PER_MOVE_ACTION,
                    GameData.ReputationAction.MountDismount => GameData.REP_PER_MOUNT_DISMOUNT,
                    GameData.ReputationAction.IntelGather => GameData.REP_PER_INTEL_GATHER,
                    GameData.ReputationAction.Combat => GameData.REP_PER_COMBAT_ACTION,
                    GameData.ReputationAction.AirborneJump => GameData.REP_PER_AIRBORNE_JUMP,
                    GameData.ReputationAction.ForcedRetreat => GameData.REP_PER_FORCED_RETREAT,
                    GameData.ReputationAction.UnitDestroyed => GameData.REP_PER_UNIT_DESTROYED,
                    _ => 0
                };

                // PrepareBattle multiplier bounds
                contextMultiplier = Math.Clamp(contextMultiplier, GameData.MIN_REP_MULTIPLIER, GameData.MAX_REP_MULTIPLIER);

                int finalREP = Mathf.RoundToInt(baseREP * contextMultiplier);

                if (finalREP > 0)
                {
                    AwardReputation(finalREP);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardReputationForAction", e);
                throw;
            }
        }

        #endregion // Reputation Management

        #region Skill Tree Interface

        /// <summary>
        /// Check if a skill can be unlocked
        /// </summary>
        /// <param name="skillEnum">Skill to check</param>
        /// <returns>True if skill can be unlocked</returns>
        public bool CanUnlockSkill(Enum skillEnum)
        {
            try
            {
                return skillTree?.CanUnlockSkill(skillEnum) ?? false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanUnlockSkill", e);
                return false;
            }
        }

        /// <summary>
        /// Attempt to unlock a skill
        /// </summary>
        /// <param name="skillEnum">Skill to unlock</param>
        /// <returns>True if skill was successfully unlocked</returns>
        public bool UnlockSkill(Enum skillEnum)
        {
            try
            {
                return skillTree?.UnlockSkill(skillEnum) ?? false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UnlockSkill", e);
                return false;
            }
        }

        /// <summary>
        /// Check if a specific skill is unlocked
        /// </summary>
        /// <param name="skillEnum">Skill to check</param>
        /// <returns>True if skill is unlocked</returns>
        public bool IsSkillUnlocked(Enum skillEnum)
        {
            try
            {
                return skillTree?.IsSkillUnlocked(skillEnum) ?? false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "IsSkillUnlocked", e);
                return false;
            }
        }

        /// <summary>
        /// Check if leader has a specific capability
        /// </summary>
        /// <param name="bonusType">Capability to check for</param>
        /// <returns>True if leader has this capability</returns>
        public bool HasCapability(SkillBonusType bonusType)
        {
            try
            {
                return skillTree?.HasCapability(bonusType) ?? false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "HasCapability", e);
                return false;
            }
        }

        /// <summary>
        /// Get the total bonus value for a specific bonus type
        /// </summary>
        /// <param name="bonusType">Type of bonus to calculate</param>
        /// <returns>Total bonus value</returns>
        public float GetBonusValue(SkillBonusType bonusType)
        {
            try
            {
                return skillTree?.GetBonusValue(bonusType) ?? 0f;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetBonusValue", e);
                return 0f;
            }
        }

        /// <summary>
        /// Check if a skill branch is available to start
        /// </summary>
        /// <param name="branch">Branch to check</param>
        /// <returns>True if branch can be started</returns>
        public bool IsBranchAvailable(SkillBranch branch)
        {
            try
            {
                return skillTree?.IsBranchAvailable(branch) ?? false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "IsBranchAvailable", e);
                return false;
            }
        }

        /// <summary>
        /// Reset all skills except leadership (respec functionality)
        /// </summary>
        /// <returns>True if any skills were reset</returns>
        public bool ResetSkills()
        {
            try
            {
                return skillTree?.ResetAllSkillsExceptLeadership() ?? false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResetSkills", e);
                return false;
            }
        }

        #endregion // Skill Tree Interface

        #region Skill Convenience Properties

        // Leadership Foundation
        [JsonIgnore] public float CommandTier1Bonus => GetBonusValue(SkillBonusType.CommandTier1);
        [JsonIgnore] public float CommandTier2Bonus => GetBonusValue(SkillBonusType.CommandTier2);
        [JsonIgnore] public float CommandTier3Bonus => GetBonusValue(SkillBonusType.CommandTier3);
        [JsonIgnore] public bool HasSeniorPromotion => HasCapability(SkillBonusType.SeniorPromotion);
        [JsonIgnore] public bool HasTopPromotion => HasCapability(SkillBonusType.TopPromotion);

        // Politically Connected Foundation
        [JsonIgnore] public bool HasEmergencyResupply => HasCapability(SkillBonusType.EmergencyResupply);
        [JsonIgnore] public float SupplyConsumptionModifier => GetBonusValue(SkillBonusType.SupplyConsumption);
        [JsonIgnore] public bool HasNVG => HasCapability(SkillBonusType.NVG);
        [JsonIgnore] public float ReplacementXPBonus => GetBonusValue(SkillBonusType.ReplacementXP);
        [JsonIgnore] public float PrestigeCostModifier => GetBonusValue(SkillBonusType.PrestigeCost);

        // Armored Doctrine
        [JsonIgnore] public float HardAttackBonus => GetBonusValue(SkillBonusType.HardAttack);
        [JsonIgnore] public float HardDefenseBonus => GetBonusValue(SkillBonusType.HardDefense);
        [JsonIgnore] public bool HasBreakthrough => HasCapability(SkillBonusType.Breakthrough);

        // Infantry Doctrine
        [JsonIgnore] public float SoftAttackBonus => GetBonusValue(SkillBonusType.SoftAttack);
        [JsonIgnore] public float SoftDefenseBonus => GetBonusValue(SkillBonusType.SoftDefense);
        [JsonIgnore] public float RTOModifier => GetBonusValue(SkillBonusType.RTO);

        // Artillery Doctrine
        [JsonIgnore] public float IndirectRangeBonus => GetBonusValue(SkillBonusType.IndirectRange);
        [JsonIgnore] public bool HasShootAndScoot => HasCapability(SkillBonusType.ShootAndScoot);
        [JsonIgnore] public bool HasAdvancedTargeting => HasCapability(SkillBonusType.AdvancedTargetting);

        // Air Defense Doctrine
        [JsonIgnore] public float AirAttackBonus => GetBonusValue(SkillBonusType.AirAttack);
        [JsonIgnore] public float AirDefenseBonus => GetBonusValue(SkillBonusType.AirDefense);
        [JsonIgnore] public float OpportunityActionBonus => GetBonusValue(SkillBonusType.OpportunityAction);

        // Airborne Doctrine
        [JsonIgnore] public bool HasImpromptuPlanning => HasCapability(SkillBonusType.ImpromptuPlanning);
        [JsonIgnore] public bool HasAirborneAssault => HasCapability(SkillBonusType.AirborneAssault);
        [JsonIgnore] public bool IsAirborneElite => HasCapability(SkillBonusType.AirborneElite);

        // Air Mobile Doctrine
        [JsonIgnore] public bool IsAirMobile => HasCapability(SkillBonusType.AirMobile);
        [JsonIgnore] public bool HasAirMobileAssault => HasCapability(SkillBonusType.AirMobileAssault);
        [JsonIgnore] public bool IsAirMobileElite => HasCapability(SkillBonusType.AirMobileElite);

        // Intelligence Doctrine
        [JsonIgnore] public float IntelActionBonus => GetBonusValue(SkillBonusType.ImprovedGathering);
        [JsonIgnore] public float SilhouetteReduction => GetBonusValue(SkillBonusType.UndergroundBunker);
        [JsonIgnore] public bool HasSpaceAssets => HasCapability(SkillBonusType.SpaceAssets);

        // Combined Arms Specialization (SpottingRangeBonus also aggregates Signal Intel contributions)
        [JsonIgnore] public float SpottingRangeBonus => GetBonusValue(SkillBonusType.SpottingRange);
        [JsonIgnore] public float MovementActionBonus => GetBonusValue(SkillBonusType.MovementAction);
        [JsonIgnore] public float CombatActionBonus => GetBonusValue(SkillBonusType.CombatAction);
        [JsonIgnore] public float NightCombatModifier => GetBonusValue(SkillBonusType.NightCombat);

        // Signal Intelligence Specialization
        [JsonIgnore] public bool HasSignalDecryption => HasCapability(SkillBonusType.SignalDecryption);
        [JsonIgnore] public bool HasElectronicWarfare => HasCapability(SkillBonusType.ElectronicWarfare);
        [JsonIgnore] public bool HasPatternRecognition => HasCapability(SkillBonusType.PatternRecognition);

        // Engineering Specialization
        [JsonIgnore] public float RiverCrossingModifier => GetBonusValue(SkillBonusType.RiverCrossing);
        [JsonIgnore] public float RiverAssaultModifier => GetBonusValue(SkillBonusType.RiverAssault);
        [JsonIgnore] public bool CanBuildBridges => HasCapability(SkillBonusType.BridgeBuilding);
        [JsonIgnore] public bool CanBuildFortifications => HasCapability(SkillBonusType.FieldFortification);

        // Special Forces Specialization
        [JsonIgnore] public float TerrainMasteryModifier => GetBonusValue(SkillBonusType.TerrainMastery);
        [JsonIgnore] public float InfiltrationModifier => GetBonusValue(SkillBonusType.InfiltrationMovement);
        [JsonIgnore] public float ConcealedPositionsReduction => GetBonusValue(SkillBonusType.ConcealedPositions);
        [JsonIgnore] public bool HasAmbushTactics => HasCapability(SkillBonusType.AmbushTactics);

        // Tier counts
        /// <summary>
        /// Gets the count of unlocked skills at the specified tier
        /// </summary>
        public int GetUnlockedSkillCountByTier(SkillTier tier)
        {
            return skillTree?.GetUnlockedSkillCountByTier(tier) ?? 0;
        }

        #endregion // Skill Convenience Properties

        #region Unit Assignment

        /// <summary>
        /// Assign this leader to a unit
        /// </summary>
        /// <param name="unitID">LeaderID of the unit to assign to</param>
        internal void AssignToUnit(string unitID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(unitID))
                {
                    throw new ArgumentException("Unit ID cannot be null or empty");
                }

                UnitID = unitID;
                IsAssigned = true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AssignToUnit", e);
                throw;
            }
        }

        /// <summary>
        /// Unassign this leader from their current unit
        /// </summary>
        internal void UnassignFromUnit()
        {
            try
            {
                UnitID = null;
                IsAssigned = false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UnassignFromUnit", e);
                throw;
            }
        }

        #endregion // Unit Assignment

        #region Snapshot Support Methods

        /// <summary>
        /// Captures skill tree state into the serializable SkillTreeData property.
        /// Must be called before JSON serialization.
        /// </summary>
        public void PrepareForSerialization()
        {
            try
            {
                SkillTreeData = skillTree?.ToSnapshot() ?? new LeaderSkillTreeData();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(PrepareForSerialization), e);
                throw;
            }
        }

        /// <summary>
        /// Restores skill tree from the deserialized SkillTreeData property.
        /// Must be called after JSON deserialization.
        /// </summary>
        public void RestoreFromDeserialization()
        {
            try
            {
                if (SkillTreeData != null)
                {
                    RestoreSkillTree(SkillTreeData);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RestoreFromDeserialization), e);
                throw;
            }
        }

        /// <summary>
        /// Gets the skill tree for snapshot operations (internal access)
        /// </summary>
        /// <returns>The leader's skill tree</returns>
        public LeaderSkillTree GetSkillTree()
        {
            return skillTree;
        }

        /// <summary>
        /// Sets reputation points directly (for snapshot restoration)
        /// </summary>
        /// <param name="reputationPoints">Reputation points to set</param>
        public void SetReputationPoints(int reputationPoints)
        {
            if (reputationPoints < 0)
            {
                throw new ArgumentException("Reputation points cannot be negative");
            }
            ReputationPoints = reputationPoints;
        }

        /// <summary>
        /// Restores skill tree from snapshot data
        /// </summary>
        /// <param name="skillTreeData">Skill tree data to restore</param>
        public void RestoreSkillTree(LeaderSkillTreeData skillTreeData)
        {
            if (skillTreeData == null)
            {
                throw new ArgumentNullException(nameof(skillTreeData));
            }

            try
            {
                skillTree = LeaderSkillTreeSnapshotExtensions.FromSnapshot(skillTreeData);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RestoreSkillTree", e);
                throw;
            }
        }

        #endregion // Snapshot Support Methods
    }
}
