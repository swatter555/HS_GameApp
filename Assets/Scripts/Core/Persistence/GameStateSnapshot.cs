using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HammerAndSickle.Persistence
{
    [DataContract]
    public sealed class GameStateSnapshot
    {
        [DataMember] public CampaignData Campaign { get; set; }
        [DataMember] public ScenarioData Scenario { get; set; }
        [DataMember] public JsonMapData MapData { get; set; } // Null for between-battle saves
        [DataMember] public Dictionary<string, CombatUnit> Units { get; set; } = new();
        [DataMember] public Dictionary<string, Leader> Leaders { get; set; } = new();
        [DataMember] public int SaveVersion { get; set; } = 1;
    }
}
