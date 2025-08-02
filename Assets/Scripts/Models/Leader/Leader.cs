using HammerAndSickle.Services;
using System;
using UnityEngine;

namespace HammerAndSickle.Models
{
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

        public string LeaderID { get; private set; }                               // Unique identifier for the officer
        public string Name { get; private set; }                             // Use random name generator
        public Side Side { get; private set; }                               // Player or AI
        public Nationality Nationality { get; private set; }                 // Nation of origin
        public CommandGrade CommandGrade { get; private set; }               // Rank of the officer
        public int ReputationPoints { get; private set; }                    // Points for promotions and skill upgrades
        public string FormattedRank { get { return GetFormattedRank(); } }   // Real-world rank of the officer
        public CommandAbility CombatCommand { get; private set; }            // Direct combat modifier
        public bool IsAssigned { get; internal set; }                         // Is the officer assigned to a unit?
        public string UnitID { get; internal set; }                           // UnitID of the unit assigned to the officer

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

                // Validate and set name
                if (string.IsNullOrWhiteSpace(name) ||
                    name.Length < CUConstants.MIN_LEADER_NAME_LENGTH ||
                    name.Length > CUConstants.MAX_LEADER_NAME_LENGTH)
                {
                    throw new ArgumentException($"Leader name must be between {CUConstants.MIN_LEADER_NAME_LENGTH} and {CUConstants.MAX_LEADER_NAME_LENGTH} characters");
                }

                Name = name.Trim();

                // Validate command ability
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

        #endregion // Constructors

        #region Initialization Helpers

        /// <summary>
        /// Initialize common properties shared by all constructors
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
        }

        /// <summary>
        /// Initialize the skill tree and wire up events
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
            string baseID = CUConstants.LEADER_ID_PREFIX;
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
                    name.Length < CUConstants.MIN_LEADER_NAME_LENGTH ||
                    name.Length > CUConstants.MAX_LEADER_NAME_LENGTH)
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
                        CommandGrade.JuniorGrade => "Lieutenant Colonel",
                        CommandGrade.SeniorGrade => "Colonel",
                        CommandGrade.TopGrade => "Major General",
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
        public void AwardReputationForAction(CUConstants.ReputationAction actionType, float contextMultiplier = 1.0f)
        {
            try
            {
                int baseREP = actionType switch
                {
                    CUConstants.ReputationAction.Move => CUConstants.REP_PER_MOVE_ACTION,
                    CUConstants.ReputationAction.MountDismount => CUConstants.REP_PER_MOUNT_DISMOUNT,
                    CUConstants.ReputationAction.IntelGather => CUConstants.REP_PER_INTEL_GATHER,
                    CUConstants.ReputationAction.Combat => CUConstants.REP_PER_COMBAT_ACTION,
                    CUConstants.ReputationAction.AirborneJump => CUConstants.REP_PER_AIRBORNE_JUMP,
                    CUConstants.ReputationAction.ForcedRetreat => CUConstants.REP_PER_FORCED_RETREAT,
                    CUConstants.ReputationAction.UnitDestroyed => CUConstants.REP_PER_UNIT_DESTROYED,
                    _ => 0
                };

                // Validate multiplier bounds
                contextMultiplier = Math.Clamp(contextMultiplier, CUConstants.MIN_REP_MULTIPLIER, CUConstants.MAX_REP_MULTIPLIER);

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

        #region Unit Assignment

        /// <summary>
        /// Assign this leader to a unit
        /// </summary>
        /// <param name="unitID">LeaderID of the unit to assign to</param>
        public void AssignToUnit(string unitID)
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
        public void UnassignFromUnit()
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
        /// Gets the skill tree for snapshot operations (internal access)
        /// </summary>
        /// <returns>The leader's skill tree</returns>
        internal LeaderSkillTree GetSkillTree()
        {
            return skillTree;
        }

        /// <summary>
        /// Sets the leader ID (for snapshot restoration only)
        /// </summary>
        /// <param name="leaderID">The leader ID to set</param>
        internal void SetLeaderID(string leaderID)
        {
            if (string.IsNullOrWhiteSpace(leaderID))
            {
                throw new ArgumentException("Leader ID cannot be null or empty");
            }
            LeaderID = leaderID;
        }

        /// <summary>
        /// Sets reputation points directly (for snapshot restoration)
        /// </summary>
        /// <param name="reputationPoints">Reputation points to set</param>
        internal void SetReputationPoints(int reputationPoints)
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
        internal void RestoreSkillTree(LeaderSkillTreeData skillTreeData)
        {
            if (skillTreeData == null)
            {
                throw new ArgumentNullException(nameof(skillTreeData));
            }

            try
            {
                skillTree = LeaderSkillTree.FromSnapshot(skillTreeData);
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