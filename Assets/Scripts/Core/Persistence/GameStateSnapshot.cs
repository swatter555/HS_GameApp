using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Persistence
{
    public sealed class GameStateSnapshot
    {
        [JsonPropertyName("campaign")] public CampaignData Campaign { get; set; }
        [JsonPropertyName("scenario")] public ScenarioData Scenario { get; set; }
        [JsonPropertyName("mapData")] public JsonMapData MapData { get; set; } // Null for between-battle saves
        [JsonPropertyName("units")] public Dictionary<string, CombatUnit> Units { get; set; } = new();
        [JsonPropertyName("leaders")] public Dictionary<string, Leader> Leaders { get; set; } = new();
        [JsonPropertyName("saveVersion")] public int SaveVersion { get; set; } = 1;
    }
}
