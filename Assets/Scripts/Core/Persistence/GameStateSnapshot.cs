using HammerAndSickle.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HammerAndSickle.Persistence
{
    [DataContract]
    public sealed class GameStateSnapshot
    {
        [DataMember] public CampaignData Campaign { get; set; }
        [DataMember] public ScenarioData Scenario { get; set; }
        [DataMember] public Dictionary<string, CombatUnit> Units { get; set; } = new();
        [DataMember] public Dictionary<string, Leader> Leaders { get; set; } = new();
        public int SaveVersion { get; set; } = 1;
    }
}