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
        //
        // ⚠ NO `contentVersion` — added then removed 2026-07-28 (Bob's call). Content ships INSIDE the
        // build, so `gameVersion` above already identifies it exactly; a content version could never
        // legitimately differ, and an always-empty field that looks meaningful is the mistake §7.1 records.
        // ⚠ REVISIT IF CONTENT EVER SHIPS SEPARATELY FROM THE EXE — e.g. hand-patching a rebalanced
        // campaign graph out to remote testers between builds. That is the one scenario where the two
        // versions genuinely diverge, and it is why the CAMPAIGN manifest's version is still an open
        // question (todo.md Phase 2.1) rather than settled the same way.
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

        // Prestige (SAVE_VERSION 7: setters opened so BattleManager.CaptureScenarioState can write —
        // these three were carried by the shape since v4 but never mapped from live state).
        [JsonPropertyName("currentPrestige")] public int CurrentPrestige { get; set; } = 0;
        [JsonPropertyName("prestigeEarned")] public int PrestigeEarned { get; set; } = 0;
        [JsonPropertyName("prestigeSpent")] public int PrestigeSpent { get; set; } = 0;

        // Victory scoring anchors (SAVE_VERSION 7, prestige pass Stage 5). Both are REAL HISTORY and
        // CANNOT be recomputed from the restored map: the starting share is unrecoverable after the
        // first flip (it is the §17.3 mirror anchor), and the high-water mark is the §18.2.2
        // anti-farm ratchet. ⚠ The VictoryLedger itself is deliberately NOT here — derived state
        // recomputes on load (V12.2); serialising it re-creates the drift the recompute design kills.
        [JsonPropertyName("startingPlayerShare")] public float StartingPlayerShare { get; set; } = 0f;
        [JsonPropertyName("highWaterVictoryValue")] public float HighWaterVictoryValue { get; set; } = 0f;

        // §18.2 income + §17.3 scoring knobs, mirrored from the manifest (V11.6): an in-battle save
        // must restore WITHOUT its manifest (§7.3 self-containment), and income/grading read these
        // every turn. The mission objectives need no mirror — they ride the embedded map's stamped
        // hex flags (C6).
        [JsonPropertyName("prestigeStipend")] public int PrestigeStipend { get; set; } = 0;
        [JsonPropertyName("prestigeIncomeRate")] public float PrestigeIncomeRate { get; set; } = 0f;
        [JsonPropertyName("prestigeProgressBonusRate")] public float PrestigeProgressBonusRate { get; set; } = 0f;
        [JsonPropertyName("earlyFinishMultiplier")] public float EarlyFinishMultiplier { get; set; } = 1.25f;
        [JsonPropertyName("victoryThresholdMinor")] public float VictoryThresholdMinor { get; set; } = 0f;
        [JsonPropertyName("victoryThresholdMajor")] public float VictoryThresholdMajor { get; set; } = 0f;
        [JsonPropertyName("victoryThresholdDecisive")] public float VictoryThresholdDecisive { get; set; } = 0f;
        [JsonPropertyName("requiredResult")] public BattleResult RequiredResult { get; set; } = BattleResult.MinorVictory;

        // C7 gate fraction (SAVE_VERSION 8), mirrored from the manifest for the same V11.6 reason as
        // the knobs above — the stamped objective FLAGS ride the embedded map, but the required
        // FRACTION has no other carrier, and the gate is evaluated every turn boundary. Default 1.0
        // (the C6 all-of-them rule) is what a pre-C7 save deserializes to.
        [JsonPropertyName("missionObjectiveFraction")] public float MissionObjectiveFraction { get; set; } = 1.0f;

        // Conditions
        [JsonPropertyName("weatherCondition")] public WeatherCondition WeatherCondition { get; set; } = WeatherCondition.Clear;
        [JsonInclude] [JsonPropertyName("currentPhase")] public BattlePhase CurrentPhase { get; private set; } = BattlePhase.NotStarted;
        [JsonInclude] [JsonPropertyName("currentResult")] public BattleResult CurrentResult { get; private set; } = BattleResult.Ongoing;

        // (The three objective-counter fields — objectiveHexesOccupied/Unoccupied/totalObjectiveHexes —
        // were DROPPED in SAVE_VERSION 7: the counters they mirrored retired with the recomputed
        // VictoryLedger + stamped mission-objective flags, prestige pass Stages 3–4.)

        // TODO: Need to add loss tracking.
    }
}
