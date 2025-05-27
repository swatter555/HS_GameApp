// FILE: SkillSystemUnitTests.cs
// LOCATION: HammerAndSickle/Tests/SkillSystemUnitTests.cs
// USAGE: Attach to a GameObject in your test scene, tests run automatically on Start()

using System;
using System.Linq;
using UnityEngine;
using HammerAndSickle.Models;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Comprehensive unit test suite for the Leader Skill System.
    /// Tests branch classification, skill tree logic, and integration between components.
    /// 
    /// Attach this script to a GameObject in a test scene to run all tests on Start().
    /// Results are logged to Unity console with clear pass/fail indicators.
    /// </summary>
    public class SkillSystemUnitTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private bool stopOnFirstFailure = false;

        private int totalTests = 0;
        private int passedTests = 0;
        private int failedTests = 0;

        void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== SKILL SYSTEM UNIT TESTS STARTING ===");
            totalTests = 0;
            passedTests = 0;
            failedTests = 0;

            try
            {
                // Test 1: Branch Classification System
                TestBranchClassificationSystem();

                // Test 2: Extension Methods
                TestExtensionMethods();

                // Test 3: Skill Tree Creation and Basic Operations
                TestSkillTreeBasics();

                // Test 4: Branch Availability Logic
                TestBranchAvailabilityLogic();

                // Test 5: Skill Unlocking with Branch Constraints
                TestSkillUnlockingWithBranches();

                // Test 6: Edge Cases and Error Handling
                TestEdgeCases();

                // Test 7: Serialization (if implemented)
                // TestSerialization();

                // Final Results
                LogTestSummary();
            }
            catch (Exception ex)
            {
                Debug.LogError($"CRITICAL TEST FAILURE: {ex.Message}\n{ex.StackTrace}");
                failedTests++;
            }
        }

        #region Test Categories

        void TestBranchClassificationSystem()
        {
            LogTestCategory("Branch Classification System");

            // Validate the classification system is working
            TestAssert("ValidateBranchClassification runs without error", () =>
            {
                SkillBranchExtensions.ValidateBranchClassification();
                return true;
            });

            // Test expected branch counts
            TestAssert("Foundation branch count is 2", () =>
                SkillBranchExtensions.GetBranchCountByType(BranchType.Foundation) == 2);

            TestAssert("Doctrine branch count is 7", () =>
                SkillBranchExtensions.GetBranchCountByType(BranchType.Doctrine) == 7);

            TestAssert("Specialization branch count is 4", () =>
                SkillBranchExtensions.GetBranchCountByType(BranchType.Specialization) == 4);

            // Test specific branch classifications
            TestAssert("LeadershipFoundation is Foundation", () =>
                SkillBranch.LeadershipFoundation.IsFoundation());

            TestAssert("PoliticallyConnectedFoundation is Foundation", () =>
                SkillBranch.PoliticallyConnectedFoundation.IsFoundation());

            TestAssert("ArmoredDoctrine is Doctrine", () =>
                SkillBranch.ArmoredDoctrine.IsDoctrine());

            TestAssert("InfantryDoctrine is Doctrine", () =>
                SkillBranch.InfantryDoctrine.IsDoctrine());

            TestAssert("CombinedArmsSpecialization is Specialization", () =>
                SkillBranch.CombinedArmsSpecialization.IsSpecialization());

            TestAssert("EngineeringSpecialization is Specialization", () =>
                SkillBranch.EngineeringSpecialization.IsSpecialization());

            // Test None branch
            TestAssert("None branch is Foundation (default)", () =>
                SkillBranch.None.IsFoundation());
        }

        void TestExtensionMethods()
        {
            LogTestCategory("Extension Methods");

            // Test GetBranchesByType
            var foundationBranches = SkillBranchExtensions.GetFoundationBranches().ToList();
            TestAssert("GetFoundationBranches returns correct branches", () =>
                foundationBranches.Contains(SkillBranch.LeadershipFoundation) &&
                foundationBranches.Contains(SkillBranch.PoliticallyConnectedFoundation) &&
                foundationBranches.Count == 2);

            var doctrineBranches = SkillBranchExtensions.GetDoctrineBranches().ToList();
            TestAssert("GetDoctrineBranches returns 7 branches", () =>
                doctrineBranches.Count == 7);

            TestAssert("GetDoctrineBranches contains ArmoredDoctrine", () =>
                doctrineBranches.Contains(SkillBranch.ArmoredDoctrine));

            var specializationBranches = SkillBranchExtensions.GetSpecializationBranches().ToList();
            TestAssert("GetSpecializationBranches returns 4 branches", () =>
                specializationBranches.Count == 4);

            // Test mutual exclusivity of results
            TestAssert("No branch appears in multiple categories", () =>
            {
                var allFoundation = foundationBranches.ToHashSet();
                var allDoctrine = doctrineBranches.ToHashSet();
                var allSpecialization = specializationBranches.ToHashSet();

                return !allFoundation.Overlaps(allDoctrine) &&
                       !allFoundation.Overlaps(allSpecialization) &&
                       !allDoctrine.Overlaps(allSpecialization);
            });
        }

        void TestSkillTreeBasics()
        {
            LogTestCategory("Skill Tree Basics");

            // Test skill tree creation
            var skillTree = new LeaderSkillTree(1000);
            TestAssert("Skill tree creates with initial reputation", () =>
                skillTree.ReputationPoints == 1000);

            TestAssert("Skill tree starts with JuniorGrade", () =>
                skillTree.CurrentGrade == CommandGrade.JuniorGrade);

            TestAssert("Skill tree starts with no skills unlocked", () =>
                skillTree.TotalSkillsUnlocked == 0);

            TestAssert("Skill tree starts with no active branches", () =>
                skillTree.ActiveBranches.Count == 0);

            // Test reputation management
            skillTree.AddReputation(500);
            TestAssert("AddReputation increases total", () =>
                skillTree.ReputationPoints == 1500);

            // Test specific skill checks
            TestAssert("No skills unlocked initially", () =>
                !skillTree.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));
        }

        void TestBranchAvailabilityLogic()
        {
            LogTestCategory("Branch Availability Logic");

            var skillTree = new LeaderSkillTree(2000);

            // Test initial availability - all should be available
            TestAssert("LeadershipFoundation available initially", () =>
                skillTree.IsBranchAvailable(SkillBranch.LeadershipFoundation));

            TestAssert("PoliticallyConnectedFoundation available initially", () =>
                skillTree.IsBranchAvailable(SkillBranch.PoliticallyConnectedFoundation));

            TestAssert("ArmoredDoctrine available initially", () =>
                skillTree.IsBranchAvailable(SkillBranch.ArmoredDoctrine));

            TestAssert("InfantryDoctrine available initially", () =>
                skillTree.IsBranchAvailable(SkillBranch.InfantryDoctrine));

            TestAssert("CombinedArmsSpecialization available initially", () =>
                skillTree.IsBranchAvailable(SkillBranch.CombinedArmsSpecialization));

            TestAssert("EngineeringSpecialization available initially", () =>
                skillTree.IsBranchAvailable(SkillBranch.EngineeringSpecialization));

            // Test None branch (should return false)
            TestAssert("None branch is not available", () =>
                !skillTree.IsBranchAvailable(SkillBranch.None));
        }

        void TestSkillUnlockingWithBranches()
        {
            LogTestCategory("Skill Unlocking with Branch Constraints");

            var skillTree = new LeaderSkillTree(3000);

            // Test unlocking first skill in Leadership
            bool unlocked = skillTree.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
            TestAssert("Can unlock first Leadership skill", () => unlocked);

            TestAssert("Leadership branch is now active", () =>
                skillTree.HasStartedBranch(SkillBranch.LeadershipFoundation));

            // Test unlocking first skill in Armored Doctrine
            unlocked = skillTree.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);
            TestAssert("Can unlock first Armored skill", () => unlocked);

            TestAssert("Armored branch is now active", () =>
                skillTree.HasStartedBranch(SkillBranch.ArmoredDoctrine));

            // Test that other doctrines are now blocked
            TestAssert("Infantry doctrine is now blocked", () =>
                !skillTree.IsBranchAvailable(SkillBranch.InfantryDoctrine));

            TestAssert("Artillery doctrine is now blocked", () =>
                !skillTree.IsBranchAvailable(SkillBranch.ArtilleryDoctrine));

            // Test that foundations are still available
            TestAssert("Political foundation still available", () =>
                skillTree.IsBranchAvailable(SkillBranch.PoliticallyConnectedFoundation));

            // Test that specializations are still available
            TestAssert("Engineering specialization still available", () =>
                skillTree.IsBranchAvailable(SkillBranch.EngineeringSpecialization));

            // Test unlocking first specialization
            unlocked = skillTree.UnlockSkill(CombinedArmsSpecialization.AviationAssets_SpottingRange);
            TestAssert("Can unlock first Combined Arms skill", () => unlocked);

            // Test that other specializations are now blocked
            TestAssert("Engineering specialization now blocked", () =>
                !skillTree.IsBranchAvailable(SkillBranch.EngineeringSpecialization));

            TestAssert("Special Forces specialization now blocked", () =>
                !skillTree.IsBranchAvailable(SkillBranch.SpecialForcesSpecialization));

            // Test branch tracking using existing methods
            TestAssert("HasStartedBranch returns true for Armored", () =>
                skillTree.HasStartedBranch(SkillBranch.ArmoredDoctrine));

            TestAssert("HasStartedBranch returns true for CombinedArms", () =>
                skillTree.HasStartedBranch(SkillBranch.CombinedArmsSpecialization));

            TestAssert("HasStartedBranch returns false for Infantry", () =>
                !skillTree.HasStartedBranch(SkillBranch.InfantryDoctrine));
        }

        void TestEdgeCases()
        {
            LogTestCategory("Edge Cases and Error Handling");

            var skillTree = new LeaderSkillTree(100); // Low reputation

            // Test insufficient reputation
            TestAssert("Cannot unlock expensive skill with low reputation", () =>
                !skillTree.CanUnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));

            // Test with sufficient reputation
            skillTree.AddReputation(500);
            TestAssert("Can unlock skill with sufficient reputation", () =>
                skillTree.CanUnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));

            // Test unlocking already unlocked skill
            skillTree.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
            TestAssert("Cannot unlock already unlocked skill", () =>
                !skillTree.CanUnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1));

            // Test available branches after starting multiple branches
            // Check that multiple doctrine branches are initially available
            var doctrineBranches = SkillBranchExtensions.GetDoctrineBranches().ToList();
            int availableDoctrineCount = doctrineBranches.Count(b => skillTree.IsBranchAvailable(b));
            TestAssert("Multiple doctrine branches available initially", () =>
                availableDoctrineCount > 1);

            // Start a doctrine
            skillTree.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);
            availableDoctrineCount = doctrineBranches.Count(b => skillTree.IsBranchAvailable(b));
            TestAssert("No doctrine branches available after starting one", () =>
                availableDoctrineCount == 0);

            var specializationBranches = SkillBranchExtensions.GetSpecializationBranches().ToList();
            int availableSpecCount = specializationBranches.Count(b => skillTree.IsBranchAvailable(b));
            TestAssert("All specialization branches still available", () =>
                availableSpecCount == 4);
        }

        #endregion

        #region Test Utilities

        void TestAssert(string testName, Func<bool> testFunction)
        {
            totalTests++;
            try
            {
                bool result = testFunction();
                if (result)
                {
                    passedTests++;
                    if (verboseLogging)
                    {
                        Debug.Log($"✓ PASS: {testName}");
                    }
                }
                else
                {
                    failedTests++;
                    Debug.LogError($"✗ FAIL: {testName}");

                    if (stopOnFirstFailure)
                    {
                        throw new Exception($"Test failed: {testName}");
                    }
                }
            }
            catch (Exception ex)
            {
                failedTests++;
                Debug.LogError($"✗ ERROR: {testName} - {ex.Message}");

                if (stopOnFirstFailure)
                {
                    throw;
                }
            }
        }

        void LogTestCategory(string categoryName)
        {
            Debug.Log($"\n--- {categoryName} ---");
        }

        void LogTestSummary()
        {
            Debug.Log($"\n=== SKILL SYSTEM TEST RESULTS ===");
            Debug.Log($"Total Tests: {totalTests}");
            Debug.Log($"Passed: {passedTests}");
            Debug.Log($"Failed: {failedTests}");
            Debug.Log($"Success Rate: {(passedTests * 100.0f / totalTests):F1}%");

            if (failedTests == 0)
            {
                Debug.Log($"🎉 ALL TESTS PASSED! Skill system is working correctly.");
            }
            else
            {
                Debug.LogWarning($"⚠️ {failedTests} tests failed. Review errors above.");
            }

            Debug.Log("=== END OF TESTS ===\n");
        }

        #endregion

        #region Manual Test Triggers

        [ContextMenu("Test Branch Classification Only")]
        public void TestBranchClassificationOnly()
        {
            totalTests = 0; passedTests = 0; failedTests = 0;
            TestBranchClassificationSystem();
            LogTestSummary();
        }

        [ContextMenu("Test Extension Methods Only")]
        public void TestExtensionMethodsOnly()
        {
            totalTests = 0; passedTests = 0; failedTests = 0;
            TestExtensionMethods();
            LogTestSummary();
        }

        [ContextMenu("Test Skill Tree Only")]
        public void TestSkillTreeOnly()
        {
            totalTests = 0; passedTests = 0; failedTests = 0;
            TestSkillTreeBasics();
            TestBranchAvailabilityLogic();
            TestSkillUnlockingWithBranches();
            LogTestSummary();
        }

        [ContextMenu("Quick Validation Test")]
        public void QuickValidationTest()
        {
            Debug.Log("=== QUICK VALIDATION ===");

            try
            {
                SkillBranchExtensions.ValidateBranchClassification();

                var skillTree = new LeaderSkillTree(5000);
                bool canUnlock = skillTree.CanUnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);

                Debug.Log($"✓ System validation passed");
                Debug.Log($"✓ Can unlock basic skill: {canUnlock}");
                Debug.Log($"✓ Foundation branches: {SkillBranchExtensions.GetBranchCountByType(BranchType.Foundation)}");
                Debug.Log($"✓ Doctrine branches: {SkillBranchExtensions.GetBranchCountByType(BranchType.Doctrine)}");
                Debug.Log($"✓ Specialization branches: {SkillBranchExtensions.GetBranchCountByType(BranchType.Specialization)}");

                Debug.Log("🎉 Quick validation successful!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Quick validation failed: {ex.Message}");
            }
        }

        #endregion
    }
}