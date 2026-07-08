using System;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.AI
{
    /// <summary>
    /// L1 board analysis container (AI-Design-Supplement Part 4) — the static terrain study every AI
    /// layer reads: region graph + chokepoints now; avenues of approach, defensive-trace ladder, and
    /// the ambush-site catalog land in the AI1b slice and join this container.
    /// REBUILD TRIGGERS (Part 4.4): bridge destroyed or pontoon built (§11.7.3/§21.1) and fort
    /// construction (§9.8.6) invalidate the analysis — the owner (AI turn driver, AI2/AI3 wiring)
    /// calls Build again on those events; everything here is derived data, never serialized.
    /// </summary>
    public sealed class BoardAnalysis
    {
        private const string CLASS_NAME = nameof(BoardAnalysis);

        public RegionGraph Regions { get; private set; } = new RegionGraph();
        public ChokepointAnalysis Chokepoints { get; private set; } = new ChokepointAnalysis();

        public static BoardAnalysis Build(HexMap map)
        {
            var analysis = new BoardAnalysis();
            try
            {
                if (map == null) throw new ArgumentNullException(nameof(map));
                analysis.Regions = RegionGraph.Build(map);
                analysis.Chokepoints = ChokepointAnalysis.Build(map);
                return analysis;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Build), e);
                return analysis;
            }
        }
    }
}
