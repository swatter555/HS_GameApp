using HammerAndSickle.Core.GameData;
using HammerAndSickle.Persistence;
using NUnit.Framework;
using System.Text.Json;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the V11 manifest schema: a pre-V11 manifest (no scoring keys) loads with safe
    /// defaults and stays valid — the property that keeps both shipped manifests loading unchanged —
    /// the new fields round-trip by NAME through JsonPolicy.Content, and IsValid enforces the V11.3
    /// economy/threshold gates.
    /// </summary>
    [TestFixture]
    public class ScenarioManifestTests : BaseTestFixture
    {
        #region Helpers

        /// <summary>A pre-V11 manifest body — the 16 original keys only, mirroring shipped content.</summary>
        private const string PreV11Json = @"{
            ""scenarioId"": ""Mission_Test"",
            ""displayName"": ""Test Scenario"",
            ""description"": ""desc"",
            ""thumbnailFilename"": ""t.png"",
            ""mapFilename"": ""t.map"",
            ""oobFilename"": ""t.oob"",
            ""aiiFilename"": ""t.aii"",
            ""briefingFilename"": ""t.brf"",
            ""prestigePool"": 500,
            ""isCampaignScenario"": false,
            ""mapTheme"": ""MiddleEast"",
            ""difficultyLevel"": ""MjGeneral"",
            ""maxTurns"": 21,
            ""deploymentPointCap"": 1000,
            ""mapWidth"": 32,
            ""mapHeight"": 21
        }";

        /// <summary>A valid manifest with every field set, for round-trip and IsValid variations.</summary>
        private static ScenarioManifest ValidManifest() => new ScenarioManifest
        {
            ScenarioId = "Mission_Test",
            DisplayName = "Test Scenario",
            MapFilename = "t.map",
            OobFilename = "t.oob",
            PrestigePool = 500,
            MaxTurns = 21,
            DeploymentPointCap = 1000,
            MapWidth = 32,
            MapHeight = 21,
            PrestigeStipend = 20,
            PrestigeIncomeRate = 0.05f,
            PrestigeProgressBonusRate = 0.5f,
            EarlyFinishMultiplier = 1.25f,
            VictoryThresholdMinor = 0.55f,
            VictoryThresholdMajor = 0.65f,
            VictoryThresholdDecisive = 0.8f,
            RequiredResult = BattleResult.MajorVictory
        };

        #endregion // Helpers

        #region Schema — absent keys take defaults (V11.1 / Q6)

        [Test]
        public void Deserialize_PreV11Manifest_TakesDefaults_AndIsValid()
        {
            var m = JsonSerializer.Deserialize<ScenarioManifest>(PreV11Json, JsonPolicy.Content);

            Assert.IsNotNull(m);
            // The parameterless-ctor pattern (V11.1a): STJ populates via setters, so an absent key IS
            // its property default — no silent 16-parameter constructor mismatch possible.
            Assert.AreEqual(0, m.PrestigeStipend);
            Assert.AreEqual(0f, m.PrestigeIncomeRate);
            Assert.AreEqual(0f, m.PrestigeProgressBonusRate);
            Assert.AreEqual(1.25f, m.EarlyFinishMultiplier, 1e-5f, "The one non-zero default.");
            Assert.AreEqual(0f, m.VictoryThresholdMinor);
            Assert.AreEqual(0f, m.VictoryThresholdMajor);
            Assert.AreEqual(0f, m.VictoryThresholdDecisive);
            Assert.AreEqual(BattleResult.MinorVictory, m.RequiredResult);
            Assert.AreEqual(500, m.PrestigePool, "The original 16 keys still bind.");
            Assert.AreEqual(MapTheme.MiddleEast, m.MapTheme);

            Assert.IsTrue(m.IsValid(),
                "All-thresholds-zero is VALID — 'declares no scoring'. Refusing it would stop both shipped manifests loading.");
        }

        [Test]
        public void Roundtrip_ScoringFields_SurviveByName()
        {
            var original = ValidManifest();

            string json = JsonSerializer.Serialize(original, JsonPolicy.Content);
            var restored = JsonSerializer.Deserialize<ScenarioManifest>(json, JsonPolicy.Content);

            StringAssert.Contains("MajorVictory", json,
                "requiredResult must persist BY NAME (JsonPolicy.Content string-enum rule) — a number here regresses to the ordinal fragility.");
            Assert.AreEqual(original.PrestigeStipend, restored.PrestigeStipend);
            Assert.AreEqual(original.PrestigeIncomeRate, restored.PrestigeIncomeRate, 1e-5f);
            Assert.AreEqual(original.PrestigeProgressBonusRate, restored.PrestigeProgressBonusRate, 1e-5f);
            Assert.AreEqual(original.EarlyFinishMultiplier, restored.EarlyFinishMultiplier, 1e-5f);
            Assert.AreEqual(original.VictoryThresholdMinor, restored.VictoryThresholdMinor, 1e-5f);
            Assert.AreEqual(original.VictoryThresholdMajor, restored.VictoryThresholdMajor, 1e-5f);
            Assert.AreEqual(original.VictoryThresholdDecisive, restored.VictoryThresholdDecisive, 1e-5f);
            Assert.AreEqual(original.RequiredResult, restored.RequiredResult);
        }

        #endregion // Schema

        #region IsValid — economy gates (V11.3)

        [Test]
        public void IsValid_RefusesNegativeEconomy()
        {
            var m = ValidManifest();
            m.PrestigeStipend = -1;
            Assert.IsFalse(m.IsValid(), "Negative stipend refused.");

            m = ValidManifest();
            m.PrestigeIncomeRate = -0.1f;
            Assert.IsFalse(m.IsValid(), "Negative income rate refused.");

            m = ValidManifest();
            m.PrestigeProgressBonusRate = -0.5f;
            Assert.IsFalse(m.IsValid(), "Negative progress bonus refused.");
        }

        [Test]
        public void IsValid_RefusesSubUnityEarlyFinishMultiplier()
        {
            var m = ValidManifest();
            m.EarlyFinishMultiplier = 0.9f;

            Assert.IsFalse(m.IsValid(),
                "Below 1.0, sitting to the turn limit dominates cashing out — the exact incentive V10.2 exists to invert.");
        }

        #endregion // IsValid — economy gates

        #region IsValid — threshold ladder (V11.3)

        [Test]
        public void IsValid_AllZeroThresholds_IsValid_NoScoringDeclared()
        {
            var m = ValidManifest();
            m.VictoryThresholdMinor = 0f;
            m.VictoryThresholdMajor = 0f;
            m.VictoryThresholdDecisive = 0f;

            Assert.IsTrue(m.IsValid(), "All-zero = 'declares no scoring' and must remain valid (Q6).");
        }

        [Test]
        public void IsValid_AscendingLadder_IsValid()
        {
            Assert.IsTrue(ValidManifest().IsValid(), "0.55 < 0.65 < 0.8 is a proper ladder.");
        }

        [Test]
        public void IsValid_NonAscendingLadder_Refused()
        {
            var m = ValidManifest();
            m.VictoryThresholdMajor = 0.55f; // == minor

            Assert.IsFalse(m.IsValid(), "The ladder must be STRICTLY ascending.");

            m = ValidManifest();
            m.VictoryThresholdDecisive = 0.6f; // < major

            Assert.IsFalse(m.IsValid());
        }

        [Test]
        public void IsValid_PartialThresholds_Refused()
        {
            // A zero minor cut with non-zero others would grade ANY share a MinorVictory — the same
            // degenerate shape the C1 scoring guard keeps out at runtime.
            var m = ValidManifest();
            m.VictoryThresholdMinor = 0f;

            Assert.IsFalse(m.IsValid(), "Partial declaration is ambiguous and refused.");
        }

        [Test]
        public void IsValid_ThresholdAboveOne_Refused()
        {
            var m = ValidManifest();
            m.VictoryThresholdDecisive = 1.1f;

            Assert.IsFalse(m.IsValid(), "A share can never exceed 1 — an unreachable rung is authoring garbage.");
        }

        [Test]
        public void IsValid_NegativeThreshold_Refused()
        {
            var m = ValidManifest();
            m.VictoryThresholdMinor = -0.2f;

            Assert.IsFalse(m.IsValid());
        }

        #endregion // IsValid — threshold ladder
    }
}
