using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Persistence
{
    public sealed class GameStateSnapshot
    {
        /// <summary>
        /// Provenance: what wrote this save and against what content (§3.1). Never null on a save written
        /// by this build; may be a default instance when reading one written before headers existed.
        /// </summary>
        [JsonPropertyName("header")] public GameDataHeader Header { get; set; } = new();

        [JsonPropertyName("campaign")] public CampaignData Campaign { get; set; }
        [JsonPropertyName("scenario")] public ScenarioData Scenario { get; set; }

        /// <summary>
        /// The battle map, embedded. NULL for a between-battle save.
        ///
        /// ⚠ THIS NULL/NON-NULL IS THE SAVE'S CONTRACT (principle P5, formalised 2026-07-28), not an
        /// implementation detail: an IN-BATTLE save is SELF-CONTAINED — it carries its own map, so a
        /// content patch cannot corrupt a battle in progress, and it stays loadable even if its scenario
        /// was removed from the install. A BETWEEN-BATTLE save is CONTENT-INDEPENDENT — it holds only the
        /// core roster, results and a scenarioId, so it needs that scenario to still exist.
        /// <see cref="SnapshotMapper"/> keys its missing-content handling off exactly this distinction.
        /// </summary>
        [JsonPropertyName("mapData")] public JsonMapData MapData { get; set; }

        [JsonPropertyName("units")] public Dictionary<string, CombatUnit> Units { get; set; } = new();
        [JsonPropertyName("leaders")] public Dictionary<string, Leader> Leaders { get; set; } = new();

        /// <summary>
        /// THE single authority on save format version — the migration ladder keys off this and nothing
        /// else. Deliberately not duplicated onto <see cref="GameDataHeader"/>.
        /// </summary>
        [JsonPropertyName("saveVersion")] public int SaveVersion { get; set; } = 1;
    }
}
