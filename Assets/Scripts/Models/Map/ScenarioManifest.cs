using System;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Represents a scenario manifest describing required files and metadata.
    /// Used by ScenarioLoadingService to understand what files are needed for each scenario.
    /// </summary>
    [Serializable]
    public class ScenarioManifest
    {
        #region Properties

        /// <summary>
        /// Unique identifier for the scenario.
        /// </summary>
        public string ScenarioId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable display name for the scenario.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Description of the scenario for briefings or selection screens.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Name of the map file (.map) required for this scenario.
        /// </summary>
        public string MapFile { get; set; } = string.Empty;

        /// <summary>
        /// Name of the player Order of Battle file (.oob).
        /// Optional for campaign scenarios where player OOB comes from campaign save.
        /// </summary>
        public string PlayerOobFile { get; set; } = string.Empty;

        /// <summary>
        /// Name of the AI Order of Battle file (.oob).
        /// </summary>
        public string AiOobFile { get; set; } = string.Empty;

        /// <summary>
        /// Name of the briefing file (.brf) containing scenario objectives and narrative.
        /// </summary>
        public string BriefingFile { get; set; } = string.Empty;

        /// <summary>
        /// Name of the AI instruction file (.aii) containing AI behavior scripts.
        /// </summary>
        public string AiInstructionFile { get; set; } = string.Empty;

        /// <summary>
        /// Author or creator of the scenario.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Version number of the scenario manifest format.
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Indicates if this scenario is part of a campaign.
        /// Campaign scenarios may have different loading requirements.
        /// </summary>
        public bool IsCampaignScenario { get; set; } = false;

        #endregion // Properties

        #region Public Methods

        /// <summary>
        /// Validates that all required fields are properly filled.
        /// </summary>
        /// <returns>True if manifest is valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ScenarioId)) return false;
                if (string.IsNullOrWhiteSpace(DisplayName)) return false;
                if (string.IsNullOrWhiteSpace(MapFile)) return false;
                if (string.IsNullOrWhiteSpace(AiOobFile)) return false;
                if (string.IsNullOrWhiteSpace(BriefingFile)) return false;
                if (string.IsNullOrWhiteSpace(AiInstructionFile)) return false;

                // Player OOB is optional for campaign scenarios
                if (!IsCampaignScenario && string.IsNullOrWhiteSpace(PlayerOobFile)) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a summary string of the manifest for debugging.
        /// </summary>
        /// <returns>Summary information</returns>
        public string GetSummary()
        {
            try
            {
                string scenarioType = IsCampaignScenario ? "Campaign" : "Standalone";
                return $"{DisplayName} ({ScenarioId}) - {scenarioType} by {Author} v{Version}";
            }
            catch
            {
                return $"ScenarioManifest ({ScenarioId})";
            }
        }

        #endregion // Public Methods
    }
}