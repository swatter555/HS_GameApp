using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Core.Campaign;

namespace HammerAndSickle.Persistence
{
    public class GameDataHeader
    {
        [JsonPropertyName("version")] public int Version { get; set; } = 0;
        [JsonPropertyName("saveTime")] public DateTime SaveTime { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("gameVersion")] public string GameVersion { get; set; } = string.Empty;
        [JsonPropertyName("combatUnitCount")] public int CombatUnitCount { get; set; } = 0;
        [JsonPropertyName("leaderCount")] public int LeaderCount { get; set; } = 0;
        [JsonPropertyName("checksum")] public string Checksum { get; set; } = string.Empty;
    }

    public class CampaignData
    {
        // Campaign tracking
        [JsonPropertyName("campaignName")] public string CampaignName { get; set; } = "Unnamed Campaign";
        [JsonPropertyName("currentScenarioIndex")] public CampaignScenario CurrentScenarioIndex { get; set; } = CampaignScenario.None;
        [JsonPropertyName("completedScenarios")] public List<CampaignScenario> CompletedScenarios { get; set; } = new List<CampaignScenario>();

        // Campaign date tracking
        [JsonPropertyName("campaignCalendar")] public CampaignDateCalendar CampaignCalendar { get; set; } = new CampaignDateCalendar(051981, 051989);

        // Core force tracking
        [JsonPropertyName("currentPrestige")] public int CurrentPrestige { get; set; } = 0;
        [JsonPropertyName("coreForcePrestige")] public int CoreForcePrestige { get; set; } = 0;
        [JsonPropertyName("playerUnits")] public Dictionary<string, CombatUnit> PlayerUnits { get; set; } = new Dictionary<string, CombatUnit>();
        [JsonPropertyName("playerLeaders")] public Dictionary<string, Leader> PlayerLeaders { get; set; } = new Dictionary<string, Leader>();
    }
    
    public sealed class ScenarioData
    {
        // General parameters
        [JsonPropertyName("scenarioId")] public string ScenarioId { get; set; } = string.Empty;
        [JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("thumbnailFilename")] public string ThumbnailFilename { get; set; } = string.Empty;
        [JsonPropertyName("mapFilename")] public string MapFilename { get; set; } = string.Empty;
        [JsonPropertyName("oobFilename")] public string OobFilename { get; set; } = string.Empty;
        [JsonPropertyName("aiiFilename")] public string AiiFilename { get; set; } = string.Empty;
        [JsonPropertyName("briefingFilename")] public string BriefingFilename { get; set; } = string.Empty;
        [JsonPropertyName("isCampaignScenario")] public bool IsCampaignScenario { get; set; } = false;
        [JsonPropertyName("mapTheme")] public MapTheme MapTheme { get; set; } = MapTheme.MiddleEast;
        [JsonPropertyName("difficultyLevel")] public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Colonel;

        // Turn data
        [JsonPropertyName("maxTurns")] public int MaxTurns { get; set; } = 0;
        [JsonPropertyName("currentTurn")] public int CurrentTurn { get; set; } = 0;

        // Max core units allowed
        [JsonPropertyName("maxCoreUnits")] public int MaxCoreUnits { get; set; } = 0;

        // Prestige
        [JsonPropertyName("currentPrestige")] public int CurrentPrestige { get; set; } = 0;
        [JsonInclude] [JsonPropertyName("prestigeEarned")] public int PrestigeEarned { get; private set; } = 0;
        [JsonInclude] [JsonPropertyName("prestigeSpent")] public int PrestigeSpent { get; private set; } = 0;

        // Conditions
        [JsonPropertyName("weatherCondition")] public WeatherCondition WeatherCondition { get; set; } = WeatherCondition.Clear;
        [JsonInclude] [JsonPropertyName("currentPhase")] public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotStarted;
        [JsonInclude] [JsonPropertyName("currentResult")] public BattleResult CurrentResult { get; private set; } = BattleResult.Ongoing;

        // Objective data
        [JsonInclude] [JsonPropertyName("objectiveHexesOccupied")] public int ObjectiveHexesOccupied { get; private set; } = 0;
        [JsonInclude] [JsonPropertyName("objectiveHexesUnoccupied")] public int ObjectiveHexesUnoccupied { get; private set; } = 0;
        [JsonInclude] [JsonPropertyName("totalObjectiveHexes")] public int TotalObjectiveHexes { get; private set; } = 0;

        // TODO: Need to add loss tracking.
    }
}
