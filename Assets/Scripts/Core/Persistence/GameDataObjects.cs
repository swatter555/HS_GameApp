using HammerAndSickle.Models;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Persistence
{
    [Serializable]
    public class GameDataHeader
    {
        public int Version { get; set; } = 0;
        public DateTime SaveTime { get; set; } = DateTime.UtcNow;
        public string GameVersion { get; set; } = string.Empty;
        public int CombatUnitCount { get; set; } = 0;
        public int LeaderCount { get; set; } = 0;
        public string Checksum { get; set; } = string.Empty;
    }

    [Serializable]
    public class CampaignData
    {
        public string CampaignName { get; set; } = "Unnamed Campaign";
        public int CurrentScenarioIndex { get; set; } = 0;
        public List<string> CompletedScenarios { get; set; } = new List<string>();
        public int TotalVictoryPoints { get; set; } = 0;
        public int CampaignTurnsElapsed { get; set; } = 0;

        // Core force tracking (Panzer General style)
        public int CoreForcePrestige { get; set; } = 0;
        public Dictionary<string, int> UnitKillCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> UnitBattleCount { get; set; } = new Dictionary<string, int>();

        // Player progression
        public Dictionary<string, CombatUnit> PlayerUnits { get; set; } = new Dictionary<string, CombatUnit>();
        public Dictionary<string, Leader> PlayerLeaders { get; set; } = new Dictionary<string, Leader>();

        // Campaign branching
        public Dictionary<string, bool> CampaignFlags { get; set; } = new Dictionary<string, bool>();
        public List<string> UnlockedScenarios { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class ScenarioData
    {
        // Sample property.
        public int CurrentTurn { get; set; } = 0;

        // Sample property.
        public Dictionary<string, bool> Objectives { get; set; }
            = new Dictionary<string, bool>();

        // Sample property.
        public Dictionary<string, string> AiScratchPad { get; set; }
            = new Dictionary<string, string>();
    }
}