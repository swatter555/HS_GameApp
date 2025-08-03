using HammerAndSickle.Services;
using System;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Serializable data container for Leader persistence in the snapshot system.
    /// Contains all necessary data to reconstruct a Leader object during save/load operations.
    /// </summary>
    [Serializable]
    public class LeaderData
    {
        #region Core Identity

        public string LeaderID;
        public string Name;
        public Side Side;
        public Nationality Nationality;

        #endregion // Core Identity

        #region Command Properties

        public CommandGrade CommandGrade;
        public int ReputationPoints;
        public CommandAbility CombatCommand;

        #endregion // Command Properties

        #region Assignment State

        public bool IsAssigned;
        public string UnitID; // Can be null if not assigned

        #endregion // Assignment State

        #region Skill System

        public LeaderSkillTreeData SkillTreeData;

        #endregion // Skill System

        #region Constructor

        /// <summary>
        /// Parameterless constructor required for serialization
        /// </summary>
        public LeaderData()
        {
            // Initialize to prevent null reference issues
            LeaderID = string.Empty;
            Name = string.Empty;
            Side = Side.Player;
            Nationality = Nationality.USSR;
            CommandGrade = CommandGrade.JuniorGrade;
            ReputationPoints = 0;
            CombatCommand = CommandAbility.Average;
            IsAssigned = false;
            UnitID = null;
            SkillTreeData = new LeaderSkillTreeData();
        }

        #endregion // Constructor

        #region Validation

        /// <summary>
        /// Validates that the leader data is consistent and complete
        /// </summary>
        /// <returns>True if data is valid</returns>
        public bool IsValid()
        {
            // Check required fields
            if (string.IsNullOrWhiteSpace(LeaderID)) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;

            // Validate enum values
            if (!Enum.IsDefined(typeof(Side), Side)) return false;
            if (!Enum.IsDefined(typeof(Nationality), Nationality)) return false;
            if (!Enum.IsDefined(typeof(CommandGrade), CommandGrade)) return false;
            if (!Enum.IsDefined(typeof(CommandAbility), CombatCommand)) return false;

            // Validate assignment consistency
            if (IsAssigned && string.IsNullOrWhiteSpace(UnitID)) return false;
            if (!IsAssigned && !string.IsNullOrWhiteSpace(UnitID)) return false;

            // Validate reputation bounds
            if (ReputationPoints < 0) return false;

            // Validate skill tree data exists
            if (SkillTreeData == null) return false;

            return true;
        }

        #endregion // Validation
    }

    /// <summary>
    /// Extension methods for Leader class to support snapshot persistence
    /// </summary>
    public static class LeaderSnapshotExtensions
    {
        #region Snapshot Conversion

        /// <summary>
        /// Creates a snapshot of the current leader state for persistence
        /// </summary>
        /// <param name="leader">The leader to create a snapshot of</param>
        /// <returns>LeaderData containing all persistent state</returns>
        public static LeaderData ToSnapshot(this Leader leader)
        {
            if (leader == null)
            {
                throw new ArgumentNullException(nameof(leader));
            }

            try
            {
                var data = new LeaderData
                {
                    LeaderID = leader.LeaderID,
                    Name = leader.Name,
                    Side = leader.Side,
                    Nationality = leader.Nationality,
                    CommandGrade = leader.CommandGrade,
                    ReputationPoints = leader.ReputationPoints,
                    CombatCommand = leader.CombatCommand,
                    IsAssigned = leader.IsAssigned,
                    UnitID = leader.UnitID
                };

                // Get skill tree snapshot - will need to implement this in LeaderSkillTree
                if (leader.GetSkillTree() != null)
                {
                    data.SkillTreeData = leader.GetSkillTree().ToSnapshot();
                }
                else
                {
                    data.SkillTreeData = new LeaderSkillTreeData();
                }

                return data;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(LeaderSnapshotExtensions), nameof(ToSnapshot), e);
                throw;
            }
        }

        /// <summary>
        /// Creates a Leader from snapshot data
        /// </summary>
        /// <param name="data">The snapshot data to restore from</param>
        /// <returns>Reconstructed Leader object</returns>
        public static Leader FromSnapshot(LeaderData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!data.IsValid())
            {
                throw new ArgumentException("Invalid leader data provided to FromSnapshot");
            }

            try
            {
                // Create leader with the specified parameters
                var leader = new Leader(data.Name, data.Side, data.Nationality, data.CombatCommand);

                // Restore core properties that constructor doesn't set
                leader.SetLeaderID(data.LeaderID);
                leader.SetCommandGrade(data.CommandGrade);
                leader.SetReputationPoints(data.ReputationPoints);

                // Restore assignment state
                if (data.IsAssigned && !string.IsNullOrWhiteSpace(data.UnitID))
                {
                    leader.AssignToUnit(data.UnitID);
                }

                // Restore skill tree state
                if (data.SkillTreeData != null)
                {
                    leader.RestoreSkillTree(data.SkillTreeData);
                }

                return leader;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(LeaderSnapshotExtensions), nameof(FromSnapshot), e);
                throw;
            }
        }

        #endregion // Snapshot Conversion
    }
}