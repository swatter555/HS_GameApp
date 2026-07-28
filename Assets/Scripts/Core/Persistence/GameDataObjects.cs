using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Core.Campaign;

namespace HammerAndSickle.Persistence
{
    /// <summary>
    /// PROVENANCE block on a save: what wrote this file, and against what content (§3.1).
    ///
    /// ⚠ This class existed before 2026-07-28 but was referenced by NOTHING — it was not on
    /// <see cref="GameStateSnapshot"/> and no save ever carried it. It is wired in now.
    ///
    /// ⚠ IT DELIBERATELY CARRIES NO VERSION FIELD. <see cref="GameStateSnapshot.SaveVersion"/> is the ONE
    /// authority on save format version, because the migration ladder keys off it; a second `version` here
    /// could disagree with it, and a save that reports two different versions is worse than one that
    /// reports none.
    ///
    /// ⚠ AND NO CHECKSUM. The old field was never computed and never validated — precisely the shape that
    /// produced the false "MapLoader checksum-validates" claim and the phase built on top of it (§7.1). If
    /// save-integrity checking is ever wanted, it lands together with the code that verifies it, never as
    /// a field that merely looks like it means something.
    /// </summary>
    public class GameDataHeader
    {
        [JsonPropertyName("saveTime")] public DateTime SaveTime { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("gameVersion")] public string GameVersion { get; set; } = string.Empty;

        // What content this save was made against. ScenarioId is empty for a pure between-battle campaign
        // save; CampaignId is empty for a standalone scenario. Both empty is legal (a bare roster save).
        [JsonPropertyName("contentVersion")] public string ContentVersion { get; set; } = string.Empty;
        [JsonPropertyName("scenarioId")] public string ScenarioId { get; set; } = string.Empty;
        [JsonPropertyName("campaignId")] public string CampaignId { get; set; } = string.Empty;

        [JsonPropertyName("combatUnitCount")] public int CombatUnitCount { get; set; } = 0;
        [JsonPropertyName("leaderCount")] public int LeaderCount { get; set; } = 0;
    }

    public class CampaignData
    {
        // Campaign tracking.
        //
        // ⚠ POSITION BY ID, NEVER BY ENUM (§3.2 / principle P2, rewritten 2026-07-28). These two fields
        // were typed `CampaignScenario` — a 23-member enum of hard-coded mission names living in
        // GameData.cs. That put the campaign's structure in the EXECUTABLE and addressed a player's
        // progress by an ordinal: inserting a mission mid-campaign shifted every later member, so every
        // existing save silently pointed at the wrong scenario, and adding a mission at all was a code
        // change. Strings are the scenario's real identity (its folder name, §7.1) and are stable under
        // insertion, reordering and patching. The enum is DELETED.
        [JsonPropertyName("campaignId")] public string CampaignId { get; set; } = string.Empty;
        [JsonPropertyName("campaignName")] public string CampaignName { get; set; } = "Unnamed Campaign";
        [JsonPropertyName("currentScenarioId")] public string CurrentScenarioId { get; set; } = string.Empty;
        [JsonPropertyName("completedScenarioIds")] public List<string> CompletedScenarioIds { get; set; } = new List<string>();

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
        [JsonPropertyName("mapTheme")] public MapTheme MapTheme { get; set; } = MapTheme.MiddleEast;
        [JsonPropertyName("difficultyLevel")] public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Colonel;

        // Turn data
        [JsonPropertyName("maxTurns")] public int MaxTurns { get; set; } = 0;
        [JsonPropertyName("currentTurn")] public int CurrentTurn { get; set; } = 0;

        // Per-scenario Deployment fielding budget (§20.1 / §35.4), mirroring the manifest field of the
        // same name. ⚠ REPLACES `maxCoreUnits`, which is RETIRED — ScenarioManifest dropped it already
        // and leaving it here would have kept a dead concept alive in the save format.
        [JsonPropertyName("deploymentPointCap")] public int DeploymentPointCap { get; set; } = 0;

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
