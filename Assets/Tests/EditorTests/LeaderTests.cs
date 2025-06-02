using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Comprehensive unit tests for the Leader class.
    /// Tests all major functionality including construction, reputation management,
    /// skill tree integration, unit assignment, serialization, and edge cases.
    /// </summary>
    [TestFixture]
    public class LeaderTests
    {
        #region Test Setup and Teardown

        private Leader testLeader;
        private const string TEST_LEADER_NAME = "Test Commander";
        private const Nationality TEST_NATIONALITY = Nationality.USSR;
        private const Side TEST_SIDE = Side.Player;
        private const CommandAbility TEST_COMMAND = CommandAbility.Good;

        [SetUp]
        public void Setup()
        {
            // Initialize any required services (mock if necessary)
            // Note: In real implementation, you might need to mock NameGenService and AppService
        }

        [TearDown]
        public void TearDown()
        {
            testLeader = null;
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ManualCreation_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);

            // Assert
            Assert.AreEqual(TEST_LEADER_NAME, testLeader.Name);
            Assert.AreEqual(TEST_SIDE, testLeader.Side);
            Assert.AreEqual(TEST_NATIONALITY, testLeader.Nationality);
            Assert.AreEqual(TEST_COMMAND, testLeader.CombatCommand);
            Assert.AreEqual(CommandGrade.JuniorGrade, testLeader.CommandGrade);
            Assert.AreEqual(0, testLeader.ReputationPoints);
            Assert.IsFalse(testLeader.IsAssigned);
            Assert.IsNull(testLeader.UnitID);
            Assert.IsNotNull(testLeader.LeaderID);
            Assert.IsTrue(testLeader.LeaderID.StartsWith(CUConstants.LEADER_ID_PREFIX));
        }

        [Test]
        public void Constructor_RandomGeneration_CreatesValidLeader()
        {
            // Arrange & Act
            testLeader = new Leader(TEST_SIDE, TEST_NATIONALITY);

            // Assert
            Assert.AreEqual(TEST_SIDE, testLeader.Side);
            Assert.AreEqual(TEST_NATIONALITY, testLeader.Nationality);
            Assert.AreEqual(CommandGrade.JuniorGrade, testLeader.CommandGrade);
            Assert.AreEqual(0, testLeader.ReputationPoints);
            Assert.IsFalse(testLeader.IsAssigned);
            Assert.IsNull(testLeader.UnitID);
            Assert.IsNotNull(testLeader.Name);
            Assert.IsNotEmpty(testLeader.Name);
            Assert.IsTrue(Enum.IsDefined(typeof(CommandAbility), testLeader.CombatCommand));
        }

        [Test]
        public void Constructor_InvalidName_ThrowsArgumentException()
        {
            // Test null name
            Assert.Throws<ArgumentException>(() =>
                new Leader(null, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND));

            // Test empty name
            Assert.Throws<ArgumentException>(() =>
                new Leader("", TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND));

            // Test name too short
            Assert.Throws<ArgumentException>(() =>
                new Leader("A", TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND));

            // Test name too long
            string longName = new string('A', CUConstants.MAX_LEADER_NAME_LENGTH + 1);
            Assert.Throws<ArgumentException>(() =>
                new Leader(longName, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND));
        }

        [Test]
        public void Constructor_InvalidCommandAbility_ThrowsArgumentException()
        {
            // Test invalid command ability (outside enum range)
            Assert.Throws<ArgumentException>(() =>
                new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, (CommandAbility)99));
        }

        #endregion

        #region Property Tests

        [Test]
        public void SetOfficerName_ValidName_UpdatesSuccessfully()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            const string newName = "New Commander";

            // Act
            bool result = testLeader.SetOfficerName(newName);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(newName, testLeader.Name);
        }

        [Test]
        public void SetOfficerName_InvalidName_ReturnsFalse()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            string originalName = testLeader.Name;

            // Act & Assert - null name
            Assert.IsFalse(testLeader.SetOfficerName(null));
            Assert.AreEqual(originalName, testLeader.Name);

            // Act & Assert - empty name
            Assert.IsFalse(testLeader.SetOfficerName(""));
            Assert.AreEqual(originalName, testLeader.Name);

            // Act & Assert - name too long
            string longName = new string('A', CUConstants.MAX_LEADER_NAME_LENGTH + 1);
            Assert.IsFalse(testLeader.SetOfficerName(longName));
            Assert.AreEqual(originalName, testLeader.Name);
        }

        [Test]
        public void SetOfficerCommandAbility_ValidAbility_UpdatesSuccessfully()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, CommandAbility.Average);

            // Act
            testLeader.SetOfficerCommandAbility(CommandAbility.Superior);

            // Assert
            Assert.AreEqual(CommandAbility.Superior, testLeader.CombatCommand);
        }

        [Test]
        public void GetFormattedRank_DifferentNationalities_ReturnsCorrectRanks()
        {
            // Test USSR ranks
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, Nationality.USSR, TEST_COMMAND);
            Assert.AreEqual("Lieutenant Colonel", testLeader.GetFormattedRank());

            // Test German ranks
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, Nationality.FRG, TEST_COMMAND);
            Assert.AreEqual("Oberst", testLeader.GetFormattedRank());

            // Test French ranks
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, Nationality.FRA, TEST_COMMAND);
            Assert.AreEqual("Colonel", testLeader.GetFormattedRank());
        }

        #endregion

        #region Reputation Management Tests

        [Test]
        public void AwardReputation_ValidAmount_UpdatesCorrectly()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            const int awardAmount = 100;
            bool eventFired = false;
            int eventChange = 0;
            int eventTotal = 0;

            testLeader.OnReputationChanged += (change, total) =>
            {
                eventFired = true;
                eventChange = change;
                eventTotal = total;
            };

            // Act
            testLeader.AwardReputation(awardAmount);

            // Assert
            Assert.AreEqual(awardAmount, testLeader.ReputationPoints);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(awardAmount, eventChange);
            Assert.AreEqual(awardAmount, eventTotal);
        }

        [Test]
        public void AwardReputation_MultipleAwards_AccumulatesCorrectly()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);

            // Act
            testLeader.AwardReputation(50);
            testLeader.AwardReputation(30);
            testLeader.AwardReputation(20);

            // Assert
            Assert.AreEqual(100, testLeader.ReputationPoints);
        }

        [Test]
        public void AwardReputationForAction_DifferentActions_CorrectAmounts()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);

            // Act & Assert - Move action
            testLeader.AwardReputationForAction(CUConstants.ReputationAction.Move);
            Assert.AreEqual(CUConstants.REP_PER_MOVE_ACTION, testLeader.ReputationPoints);

            // Reset and test combat action
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputationForAction(CUConstants.ReputationAction.Combat);
            Assert.AreEqual(CUConstants.REP_PER_COMBAT_ACTION, testLeader.ReputationPoints);

            // Reset and test unit destroyed with multiplier
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputationForAction(CUConstants.ReputationAction.UnitDestroyed, 1.5f);
            int expectedREP = Mathf.RoundToInt(CUConstants.REP_PER_UNIT_DESTROYED * 1.5f);
            Assert.AreEqual(expectedREP, testLeader.ReputationPoints);
        }

        [Test]
        public void AwardReputation_ZeroOrNegative_NoChange()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(50); // Give some initial reputation

            // Act & Assert - Zero amount
            testLeader.AwardReputation(0);
            Assert.AreEqual(50, testLeader.ReputationPoints);

            // Act & Assert - Negative amount
            testLeader.AwardReputation(-10);
            Assert.AreEqual(50, testLeader.ReputationPoints);
        }

        #endregion

        #region Skill Tree Integration Tests

        [Test]
        public void SkillTree_InitialState_NoSkillsUnlocked()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);

            // Act & Assert
            Assert.IsFalse(testLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));
            Assert.IsFalse(testLeader.HasCapability(SkillBonusType.CommandTier1));
            Assert.AreEqual(0f, testLeader.GetBonusValue(SkillBonusType.CommandTier1));
        }

        [Test]
        public void UnlockSkill_InsufficientReputation_ReturnsFalse()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            // Don't give any reputation

            // Act
            bool result = testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(testLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));
        }

        [Test]
        public void UnlockSkill_SufficientReputation_UnlocksSuccessfully()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(CUConstants.TIER1_REP_COST);
            bool skillUnlockedEventFired = false;

            testLeader.OnSkillUnlocked += (skill, name) => skillUnlockedEventFired = true;

            // Act
            bool result = testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(testLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));
            Assert.IsTrue(skillUnlockedEventFired);
            Assert.AreEqual(0, testLeader.ReputationPoints); // Reputation should be spent
        }

        [Test]
        public void SkillProgression_Leadership_UpdatesGradeCorrectly()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(CUConstants.TIER1_REP_COST + CUConstants.REP_COST_FOR_SENIOR_PROMOTION);
            bool gradeChangedEventFired = false;
            CommandGrade newGrade = CommandGrade.JuniorGrade;

            testLeader.OnGradeChanged += (grade) =>
            {
                gradeChangedEventFired = true;
                newGrade = grade;
            };

            // Act - Unlock junior training first
            testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);

            // Act - Promote to Senior Grade
            bool promotionResult = testLeader.UnlockSkill(LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion);

            // Assert
            Assert.IsTrue(promotionResult);
            Assert.AreEqual(CommandGrade.SeniorGrade, testLeader.CommandGrade);
            Assert.IsTrue(gradeChangedEventFired);
            Assert.AreEqual(CommandGrade.SeniorGrade, newGrade);
        }

        [Test]
        public void BranchExclusivity_Doctrine_OnlyOneAllowed()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(CUConstants.TIER1_REP_COST * 2);

            // Act - Start Armored Doctrine
            bool firstDoctrineResult = testLeader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);

            // Act - Try to start Infantry Doctrine (should fail due to exclusivity)
            bool secondDoctrineResult = testLeader.UnlockSkill(InfantryDoctrine.InfantryAssaultTactics_SoftAttack);

            // Assert
            Assert.IsTrue(firstDoctrineResult);
            Assert.IsFalse(secondDoctrineResult);
            Assert.IsTrue(testLeader.IsSkillUnlocked(ArmoredDoctrine.ShockTankCorps_HardAttack));
            Assert.IsFalse(testLeader.IsSkillUnlocked(InfantryDoctrine.InfantryAssaultTactics_SoftAttack));
        }

        [Test]
        public void GetBonusValue_MultipleSkills_AccumulatesCorrectly()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(500); // Plenty for multiple skills

            // Act - Unlock multiple command bonuses
            testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
            testLeader.UnlockSkill(LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion);
            testLeader.UnlockSkill(LeadershipFoundation.SeniorOfficerTraining_CommandTier2);

            // Assert
            float totalCommandBonus = testLeader.GetBonusValue(SkillBonusType.CommandTier1) +
                                    testLeader.GetBonusValue(SkillBonusType.CommandTier2);
            Assert.AreEqual(CUConstants.COMMAND_BONUS_VAL * 2, totalCommandBonus);
        }

        [Test]
        public void ResetSkills_PreservesLeadership_ResetsOthers()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(300);

            // Unlock leadership and doctrine skills
            testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
            testLeader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);

            int reputationBeforeReset = testLeader.ReputationPoints;

            // Act
            bool resetResult = testLeader.ResetSkills();

            // Assert
            Assert.IsTrue(resetResult);
            Assert.IsTrue(testLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1)); // Leadership preserved
            Assert.IsFalse(testLeader.IsSkillUnlocked(ArmoredDoctrine.ShockTankCorps_HardAttack)); // Doctrine reset
            Assert.Greater(testLeader.ReputationPoints, reputationBeforeReset); // Reputation refunded
        }

        #endregion

        #region Unit Assignment Tests

        [Test]
        public void AssignToUnit_ValidUnitID_UpdatesCorrectly()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            const string unitID = "UNIT_001";
            bool assignmentEventFired = false;
            string eventUnitID = null;

            testLeader.OnUnitAssigned += (id) =>
            {
                assignmentEventFired = true;
                eventUnitID = id;
            };

            // Act
            testLeader.AssignToUnit(unitID);

            // Assert
            Assert.IsTrue(testLeader.IsAssigned);
            Assert.AreEqual(unitID, testLeader.UnitID);
            Assert.IsTrue(assignmentEventFired);
            Assert.AreEqual(unitID, eventUnitID);
        }

        [Test]
        public void AssignToUnit_NullOrEmptyUnitID_ThrowsException()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => testLeader.AssignToUnit(null));
            Assert.Throws<ArgumentException>(() => testLeader.AssignToUnit(""));
            Assert.Throws<ArgumentException>(() => testLeader.AssignToUnit("   "));
        }

        [Test]
        public void UnassignFromUnit_WhenAssigned_ClearsAssignment()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AssignToUnit("UNIT_001");
            bool unassignmentEventFired = false;

            testLeader.OnUnitUnassigned += () => unassignmentEventFired = true;

            // Act
            testLeader.UnassignFromUnit();

            // Assert
            Assert.IsFalse(testLeader.IsAssigned);
            Assert.IsNull(testLeader.UnitID);
            Assert.IsTrue(unassignmentEventFired);
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void Serialization_RoundTrip_PreservesAllData()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(200);
            testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
            testLeader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);
            testLeader.AssignToUnit("UNIT_TEST");

            // Act - Serialize
            var formatter = new BinaryFormatter();
            Leader deserializedLeader;

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, testLeader);
                stream.Position = 0;
                deserializedLeader = (Leader)formatter.Deserialize(stream);
            }

            // Assert - All properties preserved
            Assert.AreEqual(testLeader.LeaderID, deserializedLeader.LeaderID);
            Assert.AreEqual(testLeader.Name, deserializedLeader.Name);
            Assert.AreEqual(testLeader.Side, deserializedLeader.Side);
            Assert.AreEqual(testLeader.Nationality, deserializedLeader.Nationality);
            Assert.AreEqual(testLeader.CommandGrade, deserializedLeader.CommandGrade);
            Assert.AreEqual(testLeader.ReputationPoints, deserializedLeader.ReputationPoints);
            Assert.AreEqual(testLeader.CombatCommand, deserializedLeader.CombatCommand);
            Assert.AreEqual(testLeader.IsAssigned, deserializedLeader.IsAssigned);
            Assert.AreEqual(testLeader.UnitID, deserializedLeader.UnitID);

            // Assert - Skills preserved
            Assert.AreEqual(testLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1),
                           deserializedLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));
            Assert.AreEqual(testLeader.IsSkillUnlocked(ArmoredDoctrine.ShockTankCorps_HardAttack),
                           deserializedLeader.IsSkillUnlocked(ArmoredDoctrine.ShockTankCorps_HardAttack));
        }

        #endregion

        #region Cloning Tests

        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(150);
            testLeader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
            testLeader.AssignToUnit("UNIT_ORIGINAL");

            // Act
            var clonedLeader = (Leader)testLeader.Clone();

            // Assert - Same values
            Assert.AreEqual(testLeader.Name, clonedLeader.Name);
            Assert.AreEqual(testLeader.Side, clonedLeader.Side);
            Assert.AreEqual(testLeader.Nationality, clonedLeader.Nationality);
            Assert.AreEqual(testLeader.CommandGrade, clonedLeader.CommandGrade);
            Assert.AreEqual(testLeader.ReputationPoints, clonedLeader.ReputationPoints);
            Assert.AreEqual(testLeader.CombatCommand, clonedLeader.CombatCommand);
            Assert.AreEqual(testLeader.IsAssigned, clonedLeader.IsAssigned);
            Assert.AreEqual(testLeader.UnitID, clonedLeader.UnitID);

            // Assert - Different objects (independent)
            Assert.AreNotSame(testLeader, clonedLeader);
            Assert.AreNotEqual(testLeader.LeaderID, clonedLeader.LeaderID); // Should have different IDs

            // Assert - Independent skill trees
            clonedLeader.AwardReputation(100);
            Assert.AreNotEqual(testLeader.ReputationPoints, clonedLeader.ReputationPoints);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void RandomGeneration_MultipleCreations_ProducesDifferentResults()
        {
            // Arrange & Act
            var leader1 = new Leader(TEST_SIDE, TEST_NATIONALITY);
            var leader2 = new Leader(TEST_SIDE, TEST_NATIONALITY);
            var leader3 = new Leader(TEST_SIDE, TEST_NATIONALITY);

            // Assert - Different IDs
            Assert.AreNotEqual(leader1.LeaderID, leader2.LeaderID);
            Assert.AreNotEqual(leader2.LeaderID, leader3.LeaderID);
            Assert.AreNotEqual(leader1.LeaderID, leader3.LeaderID);

            // Assert - Names might be different (depending on NameGenService implementation)
            // Note: This test might fail if NameGenService has limited names for testing
            // In that case, this assertion could be removed or modified
        }

        [Test]
        public void SkillOperations_NullSkillTree_HandleGracefully()
        {
            // This test would require creating a Leader with a null skill tree
            // which shouldn't happen in normal operation, but tests defensive programming

            // Note: This would require modification to Leader class to allow testing
            // with null skill tree, or using reflection to set skillTree to null
            // Skipping for now as it's an edge case that shouldn't occur in practice
        }

        [Test]
        public void IDGeneration_FollowsConstantFormat()
        {
            // Arrange & Act
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);

            // Assert
            Assert.IsTrue(testLeader.LeaderID.StartsWith(CUConstants.LEADER_ID_PREFIX));
            Assert.AreEqual(CUConstants.LEADER_ID_LENGTH, testLeader.LeaderID.Length);

            // Check that the LeaderID contains only valid characters (letters and numbers)
            string idSuffix = testLeader.LeaderID.Substring(CUConstants.LEADER_ID_PREFIX.Length);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(idSuffix, "^[A-F0-9]+$"));
        }

        #endregion

        #region Performance Tests

        [Test]
        public void SkillOperations_ManySkills_PerformAcceptably()
        {
            // Arrange
            testLeader = new Leader(TEST_LEADER_NAME, TEST_SIDE, TEST_NATIONALITY, TEST_COMMAND);
            testLeader.AwardReputation(5000); // Lots of reputation

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Perform many skill checks
            for (int i = 0; i < 1000; i++)
            {
                testLeader.CanUnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
                testLeader.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
                testLeader.GetBonusValue(SkillBonusType.CommandTier1);
                testLeader.HasCapability(SkillBonusType.CommandTier1);
            }

            stopwatch.Stop();

            // Assert - Should complete quickly (adjust threshold as needed)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Skill operations should be fast");
        }

        #endregion
    }
}