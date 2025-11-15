using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Core.Campaign;

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
        // Campaign tracking
        public string CampaignName { get; set; } = "Unnamed Campaign";
        public CampaignScenario CurrentScenarioIndex { get; set; } = CampaignScenario.None;
        public List<CampaignScenario> CompletedScenarios { get; set; } = new List<CampaignScenario>();

        // Campaign date tracking
        public CampaignDateCalendar CampaignCalendar { get; set; } = new CampaignDateCalendar(051981, 051989);

        // Core force tracking
        public int CurrentPrestige { get; set; } = 0;
        public int CoreForcePrestige { get; set; } = 0;
        public Dictionary<string, CombatUnit> PlayerUnits { get; set; } = new Dictionary<string, CombatUnit>();
        public Dictionary<string, Leader> PlayerLeaders { get; set; } = new Dictionary<string, Leader>();
    }
    
    [Serializable]
    public sealed class ScenarioData
    {
        // General parameters
        public string ScenarioId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailFilename { get; set; } = string.Empty;
        public string MapFilename { get; set; } = string.Empty;
        public string OobFilename { get; set; } = string.Empty;
        public string AiiFilename { get; set; } = string.Empty;
        public string BriefingFilename { get; set; } = string.Empty;
        public bool IsCampaignScenario { get; set; } = false;
        public MapTheme MapTheme { get; set; } = MapTheme.MiddleEast;
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Colonel;

        // Turn data
        public int MaxTurns { get; set; } = 0;
        public int CurrentTurn { get; set; } = 0;

        // Max core units allowed
        public int MaxCoreUnits { get; set; } = 0;

        // Prestige
        public int CurrentPrestige { get; set; } = 0;
        public int PrestigeEarned { get; private set; } = 0;
        public int PrestigeSpent { get; private set; } = 0;

        // Conditions
        public WeatherCondition WeatherCondition { get; set; } = WeatherCondition.Clear;
        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotStarted;
        public BattleResult CurrentResult { get; private set; } = BattleResult.Ongoing;

        // Objective data
        public int ObjectiveHexesOccupied { get; private set; } = 0;
        public int ObjectiveHexesUnoccupied { get; private set; } = 0;
        public int TotalObjectiveHexes { get; private set; } = 0;

        // TODO: Need to add loss tracking.
    }
}
