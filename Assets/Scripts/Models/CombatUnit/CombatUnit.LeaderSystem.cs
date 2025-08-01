using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using HammerAndSickle.Controllers;

namespace HammerAndSickle.Models
{
    public partial class CombatUnit
    {
        #region Properties

        public string LeaderID { get; internal set; }
        public bool IsLeaderAssigned => !string.IsNullOrEmpty(LeaderID);
        public Leader UnitLeader
        {
            get
            {
                try
                {
                    if (!IsLeaderAssigned)
                        throw new InvalidOperationException("No leader assigned to this unit.");

                    return GameDataManager.Instance.GetLeader(LeaderID);
                }
                catch (Exception e)
                {
                    AppService.HandleException(CLASS_NAME, "UnitLeader.get", e, ExceptionSeverity.Minor);
                    return null;
                }
            }
        }

        #endregion // Properties


        #region Initialization

        /// <summary>
        /// Initializes the leader system for the combat unit.
        /// </summary>
        private void InitializeLeaderSystem()
        {
            LeaderID = null; // No leader assigned by default
        }

        #endregion


        #region Leader System

        /// <summary>
        /// Assigns a leader to this unit by their ID, removing any existing leader.
        /// </summary>
        /// <param name="leaderID">ID of the new leader</param>
        /// <returns>success/failure</returns>
        public bool AssignLeader(string leaderID)
        {
            try
            {
                // Validate the incoming LeaderID.
                Leader newLeader = GameDataManager.Instance.GetLeader(leaderID);
                if (newLeader == null)
                {
                    AppService.HandleException(CLASS_NAME, "AssignLeader", new Exception("Missing leader: " + leaderID));
                    LeaderID = null;
                    return false;
                }

                // Check if the new leader is already assigned to another unit.
                if (newLeader.IsAssigned)
                {
                    AppService.CaptureUiMessage($"{newLeader.FormattedRank} {newLeader.Name} is already assigned to another unit.");
                    return false;
                }

                // If there is already a leader assigned, we must remove them first.
                if (IsLeaderAssigned)
                {
                    // Make sure current leader is valid before proceeding.
                    if (UnitLeader == null)
                    {
                        AppService.HandleException(CLASS_NAME, "AssignLeader", new Exception("Current leader is null when trying to assign a new leader."));
                        return false;
                    }

                    // Capture UI message about the leader being unassigned.
                    AppService.CaptureUiMessage($"{UnitLeader.FormattedRank} {UnitLeader.Name} has been unassigned from {UnitName}.");

                    // Reach in and let the Leader know it isn't assigned to this unit anymore.
                    UnitLeader.UnassignFromUnit();
                }

                // Assign the new leader ID.
                LeaderID = leaderID;

                // Now reach into new leader and assign him from there.
                newLeader.AssignToUnit(UnitID);

                // Capture UI message about the new leader being assigned.
                AppService.CaptureUiMessage($"{newLeader.FormattedRank} {newLeader.Name} has been assigned to command {UnitName}.");

                return true;
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "AssignLeader", e);
                AppService.CaptureUiMessage("Leader assignment failed due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Removes the current leader from this unit, if one is assigned.
        /// </summary>
        /// <returns>Success/Failure</returns>
        public bool RemoveLeader()
        {
            try
            {
                // Check if there is actually a leader to remove.
                if (!IsLeaderAssigned)
                {
                    AppService.CaptureUiMessage($"{UnitName} does not have a commanding officer to remove.");
                    return false;
                }

                // Capture UI message about the leader being removed.
                AppService.CaptureUiMessage($"{UnitLeader.FormattedRank} {UnitLeader.Name} has been relieved of command of {UnitName} and is now available for reassignment.");

                // Reach in and let the Leader know it isn't assigned to this unit anymore.
                UnitLeader.UnassignFromUnit();

                // Clear our reference to the leader.
                LeaderID = null;

                return true;
            }
            catch (Exception e)
            {
                // Handle any unexpected errors
                AppService.HandleException(CLASS_NAME, "RemoveLeader", e);
                AppService.CaptureUiMessage("Leader removal failed due to an unexpected error.");
                return false;
            }
        }

        /// <summary>
        /// Gets all bonuses provided by the commanding officer's skills.
        /// Returns an empty dictionary if no leader is assigned.
        /// </summary>
        /// <returns>Dictionary mapping skill bonus types to their values</returns>
        public Dictionary<SkillBonusType, float> GetLeaderBonuses()
        {
            var bonuses = new Dictionary<SkillBonusType, float>();

            try
            {
                // Check if there is a leader assigned
                if (!IsLeaderAssigned)
                    return bonuses;

                // Iterate through all skill bonus types and get non-zero values
                foreach (SkillBonusType bonusType in (SkillBonusType[])Enum.GetValues(typeof(SkillBonusType)))
                {
                    if (bonusType == SkillBonusType.None) continue;

                    float bonusValue = UnitLeader.GetBonusValue(bonusType);
                    if (bonusValue != 0f)
                    {
                        bonuses[bonusType] = bonusValue;
                    }
                }

                return bonuses;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetLeaderBonuses", e);
                return bonuses; // Return empty dictionary on error
            }
        }

        /// <summary>
        /// Checks if the unit has a specific leader capability/bonus.
        /// </summary>
        /// <param name="bonusType">The bonus type to check for</param>
        /// <returns>True if the leader provides this capability</returns>
        public bool HasLeaderCapability(SkillBonusType bonusType) =>
            UnitLeader != null && UnitLeader.HasCapability(bonusType);

        /// <summary>
        /// Gets a specific leader bonus value.
        /// Returns 0 if no leader assigned or bonus not present.
        /// </summary>
        /// <param name="bonusType">The type of bonus to retrieve</param>
        /// <returns>The bonus value, or 0 if not present</returns>
        public float GetLeaderBonus(SkillBonusType bonusType) =>
            UnitLeader != null && bonusType != SkillBonusType.None
            ? UnitLeader.GetBonusValue(bonusType)
            : 0f;

        /// <summary>
        /// Gets the leader's name for display purposes.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Leader name or empty string</returns>
        public string GetLeaderName() => UnitLeader?.Name ?? string.Empty;

        /// <summary>
        /// Gets the leader's command grade for display and bonus calculations.
        /// Returns JuniorGrade if no leader assigned.
        /// </summary>
        /// <returns>Leader's command grade</returns>
        public CommandGrade GetLeaderGrade() => UnitLeader?.CommandGrade ?? CommandGrade.JuniorGrade;

        /// <summary>
        /// Gets the leader's reputation points for display purposes.
        /// Returns 0 if no leader assigned.
        /// </summary>
        /// <returns>Leader's reputation points</returns>
        public int GetLeaderReputation() => UnitLeader?.ReputationPoints ?? 0;

        /// <summary>
        /// Gets the leader's formatted rank based on nationality.
        /// Returns empty string if no leader assigned.
        /// </summary>
        /// <returns>Formatted rank string</returns>
        public string GetLeaderRank() => UnitLeader?.FormattedRank ?? "";

        /// <summary>
        /// Gets the leader's combat command ability modifier.
        /// Returns Average if no leader assigned.
        /// </summary>
        /// <returns>Leader's combat command ability</returns>
        public CommandAbility GetLeaderCommandAbility() =>
            UnitLeader?.CombatCommand ?? CommandAbility.Average;

        /// <summary>
        /// Checks if the leader has unlocked a specific skill.
        /// Returns false if no leader assigned.
        /// </summary>
        /// <param name="skillEnum">The skill to check</param>
        /// <returns>True if the skill is unlocked</returns>
        public bool HasLeaderSkill(Enum skill) =>
            UnitLeader != null && UnitLeader.IsSkillUnlocked(skill);

        /// <summary>
        /// Awards reputation to the leader for unit actions.
        /// Does nothing if no leader assigned.
        /// </summary>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="contextMultiplier">Context-based multiplier (default 1.0)</param>
        public void AwardLeaderReputation(CUConstants.ReputationAction actionType, float contextMultiplier = 1.0f)
        {
            try
            {
                if (UnitLeader == null)
                {
                    return;
                }

                UnitLeader.AwardReputationForAction(actionType, contextMultiplier);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        /// <summary>
        /// Awards reputation points directly to the leader.
        /// Does nothing if no leader assigned.
        /// </summary>
        /// <param name="amount">Amount of reputation to award</param>
        public void AwardLeaderReputation(int amount)
        {
            try
            {
                if (UnitLeader == null || amount <= 0)
                {
                    return;
                }

                UnitLeader.AwardReputation(amount);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AwardLeaderReputation", e);
            }
        }

        #endregion // Leader System
    }
}