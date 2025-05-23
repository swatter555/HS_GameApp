using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HammerAndSickle.Models;

namespace HammerAndSickle.Testing
{
    public class SkillSystemTester : MonoBehaviour
    {
        [Header("Skill System Testing")]
        [SerializeField] private bool showTestButton = true;
        [SerializeField] private bool runTestsOnStart = false;

        private int testsRun = 0;
        private int testsPassed = 0;
        private int testsFailed = 0;

        void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        void OnGUI()
        {
            if (!showTestButton) return;

            // Create a simple button in the top-left corner
            if (GUI.Button(new Rect(10, 10, 200, 40), "Run Skill System Tests"))
            {
                RunAllTests();
            }

            // Show test results if any tests have been run
            if (testsRun > 0)
            {
                GUI.Label(new Rect(10, 60, 300, 20), $"Tests: {testsRun} | Passed: {testsPassed} | Failed: {testsFailed}");

                if (testsFailed > 0)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(10, 80, 300, 20), "⚠ FAILURES DETECTED - Check Console!");
                    GUI.color = Color.white;
                }
                else if (testsRun > 0)
                {
                    GUI.color = Color.green;
                    GUI.Label(new Rect(10, 80, 300, 20), "✓ All Tests Passed!");
                    GUI.color = Color.white;
                }
            }
        }

        public void RunAllTests()
        {
            Debug.Log("=== SKILL SYSTEM TESTS STARTING ===");
            testsRun = 0;
            testsPassed = 0;
            testsFailed = 0;

            // Run all our test methods
            Test_InitializeSkillDictionaries_ShouldRegisterAllKnownSkills();
            Test_AllSkillEnumsInCatalog_ShouldBeRegisteredInSkillTree();
            Test_GetAllSkillEnumTypes_ShouldFindAllExpectedTypes();
            Test_NoMissingSkillsInSerialization();
            Test_SkillTreeBasicFunctionality();

            Debug.Log($"=== TESTS COMPLETED: {testsRun} run, {testsPassed} passed, {testsFailed} failed ===");

            if (testsFailed == 0)
            {
                Debug.Log("<color=green>🎉 ALL TESTS PASSED! Your skill system is working correctly.</color>");
            }
            else
            {
                Debug.LogError($"❌ {testsFailed} TEST(S) FAILED! Check the logs above for details.");
            }
        }

        // TEST 1: Check that all skills are registered
        private void Test_InitializeSkillDictionaries_ShouldRegisterAllKnownSkills()
        {
            StartTest("All Known Skills Registration");

            try
            {
                var skillTree = new LeaderSkillTree();
                var expectedSkillCount = GetExpectedTotalSkillCount();
                var actualSkillCount = GetRegisteredSkillCount(skillTree);

                if (expectedSkillCount == actualSkillCount)
                {
                    PassTest($"✓ Correctly registered {actualSkillCount} skills");
                }
                else
                {
                    FailTest($"Expected {expectedSkillCount} skills, but found {actualSkillCount}. " +
                            "A skill enum may have been added without updating registration logic.");
                }
            }
            catch (Exception ex)
            {
                FailTest($"Exception during skill registration test: {ex.Message}");
            }
        }

        // TEST 2: Check that catalog and tree are in sync
        private void Test_AllSkillEnumsInCatalog_ShouldBeRegisteredInSkillTree()
        {
            StartTest("Catalog-Tree Synchronization");

            try
            {
                var skillTree = new LeaderSkillTree();
                var catalogSkills = GetAllSkillsFromCatalog();
                var missingSkills = new List<string>();

                foreach (var skill in catalogSkills)
                {
                    if (!IsSkillRegistered(skillTree, skill))
                    {
                        missingSkills.Add(skill.ToString());
                    }
                }

                if (missingSkills.Count == 0)
                {
                    PassTest($"✓ All {catalogSkills.Count} catalog skills are registered in skill tree");
                }
                else
                {
                    FailTest($"Skills in catalog but not in tree: {string.Join(", ", missingSkills)}");
                }
            }
            catch (Exception ex)
            {
                FailTest($"Exception during catalog-tree sync test: {ex.Message}");
            }
        }

        // TEST 3: Check reflection-based enum discovery
        private void Test_GetAllSkillEnumTypes_ShouldFindAllExpectedTypes()
        {
            StartTest("Enum Type Discovery");

            try
            {
                var expectedTypes = new[]
                {
                    typeof(LeadershipFoundation),
                    typeof(PoliticallyConnectedFoundation),
                    typeof(ArmoredDoctrine),
                    typeof(InfantryDoctrine),
                    typeof(ArtilleryDoctrine),
                    typeof(AirDefenseDoctrine),
                    typeof(AirborneDoctrine),
                    typeof(AirMobileDoctrine),
                    typeof(IntelligenceDoctrine),
                    typeof(CombinedArmsSpecialization),
                    typeof(SignalIntelligenceSpecialization),
                    typeof(EngineeringSpecialization),
                    typeof(SpecialForcesSpecialization)
                };

                var actualTypes = GetAllSkillEnumTypes();
                var missingTypes = expectedTypes.Where(et => !actualTypes.Contains(et)).ToList();
                var extraTypes = actualTypes.Where(at => !expectedTypes.Contains(at)).ToList();

                if (missingTypes.Count == 0 && extraTypes.Count == 0)
                {
                    PassTest($"✓ Found all {expectedTypes.Length} expected skill enum types");
                }
                else
                {
                    string errorMsg = "";
                    if (missingTypes.Count > 0)
                        errorMsg += $"Missing types: {string.Join(", ", missingTypes.Select(t => t.Name))}. ";
                    if (extraTypes.Count > 0)
                        errorMsg += $"Extra types: {string.Join(", ", extraTypes.Select(t => t.Name))}. ";

                    FailTest(errorMsg.Trim());
                }
            }
            catch (Exception ex)
            {
                FailTest($"Exception during enum discovery test: {ex.Message}");
            }
        }

        // TEST 4: Check serialization robustness
        private void Test_NoMissingSkillsInSerialization()
        {
            StartTest("Serialization Robustness");

            try
            {
                var skillTree = new LeaderSkillTree();

                // Add some REP and unlock a few skills
                skillTree.AddReputation(200);
                skillTree.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
                skillTree.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack);

                // Serialize and deserialize
                var data = skillTree.ToSerializableData();
                var newSkillTree = new LeaderSkillTree();
                newSkillTree.FromSerializableData(data);

                // Check that skills persisted
                bool skill1Preserved = newSkillTree.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
                bool skill2Preserved = newSkillTree.IsSkillUnlocked(ArmoredDoctrine.ShockTankCorps_HardAttack);
                bool repPreserved = newSkillTree.ReputationPoints == skillTree.ReputationPoints;

                if (skill1Preserved && skill2Preserved && repPreserved)
                {
                    PassTest("✓ Serialization correctly preserves skill tree state");
                }
                else
                {
                    FailTest($"Serialization failed: Skill1={skill1Preserved}, Skill2={skill2Preserved}, REP={repPreserved}");
                }
            }
            catch (Exception ex)
            {
                FailTest($"Exception during serialization test: {ex.Message}");
            }
        }

        // TEST 5: Basic functionality check
        private void Test_SkillTreeBasicFunctionality()
        {
            StartTest("Basic Skill Tree Operations");

            try
            {
                var skillTree = new LeaderSkillTree();
                bool allGood = true;
                string errors = "";

                // Test 1: Can add reputation
                skillTree.AddReputation(100);
                if (skillTree.ReputationPoints != 100)
                {
                    allGood = false;
                    errors += "REP addition failed. ";
                }

                // Test 2: Can unlock skill
                bool unlockSuccess = skillTree.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
                if (!unlockSuccess)
                {
                    allGood = false;
                    errors += "Skill unlock failed. ";
                }

                // Test 3: Skill is recognized as unlocked
                bool isUnlocked = skillTree.IsSkillUnlocked(LeadershipFoundation.JuniorOfficerTraining_CommandTier1);
                if (!isUnlocked)
                {
                    allGood = false;
                    errors += "Skill not recognized as unlocked. ";
                }

                // Test 4: REP was spent
                if (skillTree.ReputationPoints != 50) // 100 - 50 for the skill
                {
                    allGood = false;
                    errors += $"REP not spent correctly (should be 50, is {skillTree.ReputationPoints}). ";
                }

                if (allGood)
                {
                    PassTest("✓ Basic skill tree operations work correctly");
                }
                else
                {
                    FailTest($"Basic operations failed: {errors.Trim()}");
                }
            }
            catch (Exception ex)
            {
                FailTest($"Exception during basic functionality test: {ex.Message}");
            }
        }

        // Helper methods for testing framework
        private void StartTest(string testName)
        {
            testsRun++;
            Debug.Log($"🧪 Running Test: {testName}");
        }

        private void PassTest(string message)
        {
            testsPassed++;
            Debug.Log($"   <color=green>{message}</color>");
        }

        private void FailTest(string message)
        {
            testsFailed++;
            Debug.LogError($"   <color=red>❌ FAIL: {message}</color>");
        }

        // Helper methods for accessing private data (using reflection)
        private int GetExpectedTotalSkillCount()
        {
            int count = 0;
            count += Enum.GetValues(typeof(LeadershipFoundation)).Length - 1; // -1 for None
            count += Enum.GetValues(typeof(PoliticallyConnectedFoundation)).Length - 1;
            count += Enum.GetValues(typeof(ArmoredDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(InfantryDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(ArtilleryDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(AirDefenseDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(AirborneDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(AirMobileDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(IntelligenceDoctrine)).Length - 1;
            count += Enum.GetValues(typeof(CombinedArmsSpecialization)).Length - 1;
            count += Enum.GetValues(typeof(SignalIntelligenceSpecialization)).Length - 1;
            count += Enum.GetValues(typeof(EngineeringSpecialization)).Length - 1;
            count += Enum.GetValues(typeof(SpecialForcesSpecialization)).Length - 1;
            return count;
        }

        private int GetRegisteredSkillCount(LeaderSkillTree skillTree)
        {
            var field = typeof(LeaderSkillTree).GetField("unlockedSkills", BindingFlags.NonPublic | BindingFlags.Instance);
            var unlockedSkills = (Dictionary<Enum, bool>)field.GetValue(skillTree);
            return unlockedSkills.Count;
        }

        private List<Enum> GetAllSkillsFromCatalog()
        {
            var catalogType = typeof(LeaderSkillCatalog);
            var allSkillsField = catalogType.GetField("AllSkills", BindingFlags.NonPublic | BindingFlags.Static);
            var allSkills = (Dictionary<Enum, SkillDefinition>)allSkillsField.GetValue(null);
            return allSkills.Keys.ToList();
        }

        private bool IsSkillRegistered(LeaderSkillTree skillTree, Enum skill)
        {
            var field = typeof(LeaderSkillTree).GetField("unlockedSkills", BindingFlags.NonPublic | BindingFlags.Instance);
            var unlockedSkills = (Dictionary<Enum, bool>)field.GetValue(skillTree);
            return unlockedSkills.ContainsKey(skill);
        }

        private List<Type> GetAllSkillEnumTypes()
        {
            var skillEnumTypes = new List<Type>();

            var modelTypes = typeof(LeadershipFoundation).Assembly.GetTypes()
                .Where(t => t.IsEnum && t.Namespace == "HammerAndSickle.Models");

            foreach (var type in modelTypes)
            {
                if (IsSkillEnumType(type))
                {
                    skillEnumTypes.Add(type);
                }
            }

            return skillEnumTypes;
        }

        private bool IsSkillEnumType(Type enumType)
        {
            var typeName = enumType.Name;
            return typeName == nameof(LeadershipFoundation) ||
                   typeName == nameof(PoliticallyConnectedFoundation) ||
                   typeName.EndsWith("Doctrine") ||
                   typeName.EndsWith("Specialization");
        }
    }
}