using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Persistence;
using NUnit.Framework;
using System.Text.Json;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Editor tests for the SAVE_VERSION 7 shape (prestige pass Stage 5): the wallet + scoring
    /// anchors + manifest-knob mirror round-trip through ScenarioData, the dropped counter fields are
    /// really gone from the wire, PrestigeWallet.Restore reinstates verbatim, and a stamped
    /// mission-objective flag survives the hex round-trip (the C6 save-self-containment claim).
    /// </summary>
    [TestFixture]
    public class PrestigePersistenceTests : BaseTestFixture
    {
        [Test]
        public void ScenarioData_PrestigeAndScoringSlice_RoundTrips()
        {
            var data = new ScenarioData
            {
                ScenarioId = "Mission_Test",
                CurrentPrestige = 340,
                PrestigeEarned = 190,
                PrestigeSpent = 350,
                StartingPlayerShare = 0.42f,
                HighWaterVictoryValue = 137.5f,
                PrestigeStipend = 20,
                PrestigeIncomeRate = 0.05f,
                PrestigeProgressBonusRate = 0.5f,
                EarlyFinishMultiplier = 1.25f,
                VictoryThresholdMinor = 0.55f,
                VictoryThresholdMajor = 0.65f,
                VictoryThresholdDecisive = 0.8f,
                RequiredResult = BattleResult.Draw,
                MissionObjectiveFraction = 0.5f   // ⚠ NON-default: a field that defaults correctly
                                                  // would round-trip "successfully" while being
                                                  // dropped on the wire (editor C7 §2.7)
            };

            string json = JsonSerializer.Serialize(data, JsonPolicy.Save);
            var restored = JsonSerializer.Deserialize<ScenarioData>(json, JsonPolicy.Save);

            Assert.AreEqual(340, restored.CurrentPrestige);
            Assert.AreEqual(190, restored.PrestigeEarned, "The earned tally maps now — it was carried-but-never-written since v4.");
            Assert.AreEqual(350, restored.PrestigeSpent);
            Assert.AreEqual(0.42f, restored.StartingPlayerShare, 1e-5f, "The mirror anchor cannot be recomputed — it must survive.");
            Assert.AreEqual(137.5f, restored.HighWaterVictoryValue, 1e-4f, "The anti-farm ratchet must survive — a reload must not re-pay old gains.");
            Assert.AreEqual(20, restored.PrestigeStipend, "Knobs mirror because an in-battle save restores without a manifest (§7.3).");
            Assert.AreEqual(0.05f, restored.PrestigeIncomeRate, 1e-6f);
            Assert.AreEqual(1.25f, restored.EarlyFinishMultiplier, 1e-6f);
            Assert.AreEqual(0.8f, restored.VictoryThresholdDecisive, 1e-6f);
            Assert.AreEqual(BattleResult.Draw, restored.RequiredResult, "A defensive scenario's demand survives the save.");
            Assert.AreEqual(0.5f, restored.MissionObjectiveFraction, 1e-6f,
                "C7 (SAVE_VERSION 8): the gate fraction has no other carrier — the stamped flags ride the map, the fraction rides here.");
            StringAssert.Contains("\"Draw\"", json, "requiredResult persists BY NAME in saves too (JsonPolicy.Save string-enum rule).");
        }

        [Test]
        public void ScenarioData_DroppedCounterFields_AreGoneFromTheWire()
        {
            string json = JsonSerializer.Serialize(new ScenarioData(), JsonPolicy.Save);

            StringAssert.DoesNotContain("objectiveHexesOccupied", json, "SAVE_VERSION 7 dropped the counter trio.");
            StringAssert.DoesNotContain("objectiveHexesUnoccupied", json);
            StringAssert.DoesNotContain("totalObjectiveHexes", json);
        }

        [Test]
        public void Wallet_Restore_ReinstatesVerbatim_UnlikeSeed()
        {
            var wallet = new PrestigeWallet();

            wallet.Restore(current: 340, earned: 190, spent: 350);

            Assert.AreEqual(340, wallet.Current);
            Assert.AreEqual(190, wallet.Earned, "Restore keeps the tallies — Seed would zero them, losing battle history.");
            Assert.AreEqual(350, wallet.Spent);
        }

        [Test]
        public void Wallet_Restore_ClampsNegatives()
        {
            var wallet = new PrestigeWallet();

            wallet.Restore(current: -50, earned: -1, spent: -2);

            Assert.AreEqual(0, wallet.Current, "A hand-edited save cannot open a debt.");
            Assert.AreEqual(0, wallet.Earned);
            Assert.AreEqual(0, wallet.Spent);
        }

        [Test]
        public void StampedObjectiveFlag_SurvivesTheHexRoundTrip()
        {
            // The C6 architecture's save half: the gate reads stamped flags, and the flags ride the
            // embedded map — so a serialized hex must carry IsObjective through the save shape.
            var hex = new HexTile(new Vector2Int(4, 4));
            hex.SetTerrain(TerrainType.MinorCity);
            hex.IsObjective = true;   // as MapLoader's stamp leaves it

            string json = JsonSerializer.Serialize(hex, JsonPolicy.Save);
            var restored = JsonSerializer.Deserialize<HexTile>(json, JsonPolicy.Save);

            Assert.IsTrue(restored.IsObjective, "A stamped objective must survive save/load — restore never re-stamps (§7.3).");
            Assert.IsTrue(restored.IsStronghold, "And the stronghold derivation holds on the restored hex.");
        }
    }
}
