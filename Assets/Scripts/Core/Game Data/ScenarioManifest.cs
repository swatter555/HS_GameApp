using HammerAndSickle.Services;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Core.GameData
{
    /// <summary>
    /// Serializable data structure representing a scenario manifest file.
    /// Lists all files required to load a scenario and provides metadata for UI display.
    /// Uses System.Text.Json for serialization/deserialization.
    /// </summary>
    [Serializable]
    public class ScenarioManifest
    {
        #region JSON Properties

        [JsonPropertyName("scenarioId")]
        public string ScenarioId { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("thumbnailFilename")]
        public string ThumbnailFilename { get; set; } = string.Empty;

        [JsonPropertyName("mapFilename")]
        public string MapFilename { get; set; } = string.Empty;

        [JsonPropertyName("oobFilename")]
        public string OobFilename { get; set; } = string.Empty;

        [JsonPropertyName("aiiFilename")]
        public string AiiFilename { get; set; } = string.Empty;

        [JsonPropertyName("briefingFilename")]
        public string BriefingFilename { get; set; } = string.Empty;

        [JsonPropertyName("prestigePool")]
        public int PrestigePool { get; set; } = 0;

        [JsonPropertyName("isCampaignScenario")]
        public bool IsCampaignScenario { get; set; } = false;

        [JsonPropertyName("mapTheme")]
        public MapTheme MapTheme { get; set; } = MapTheme.MiddleEast;

        [JsonPropertyName("difficultyLevel")]
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Colonel;

        [JsonPropertyName("maxTurns")]
        public int MaxTurns { get; set; } = 0;

        [JsonPropertyName("maxCoreUnits")]
        public int MaxCoreUnits { get; set; } = 0;

        #endregion // JSON Properties

        #region Constructors

        /// <summary>
        /// JSON deserialization constructor with explicit parameters for all serializable properties.
        /// System.Text.Json uses this constructor to create objects with all data available at construction time.
        /// Also used for creating manifest copies programmatically.
        /// </summary>
        [JsonConstructor]
        public ScenarioManifest(
            string scenarioId,
            string displayName,
            string description,
            string thumbnailFilename,
            string mapFilename,
            string oobFilename,
            string aiiFilename,
            string briefingFilename,
            int prestigePool,
            bool isCampaignScenario,
            MapTheme mapTheme,
            DifficultyLevel difficultyLevel,
            int maxTurns,
            int maxCoreUnits)
        {
            ScenarioId = scenarioId;
            DisplayName = displayName;
            Description = description;
            ThumbnailFilename = thumbnailFilename;
            MapFilename = mapFilename;
            OobFilename = oobFilename;
            AiiFilename = aiiFilename;
            BriefingFilename = briefingFilename;
            PrestigePool = prestigePool;
            IsCampaignScenario = isCampaignScenario;
            MapTheme = mapTheme;
            DifficultyLevel = difficultyLevel;
            MaxTurns = maxTurns;
            MaxCoreUnits = maxCoreUnits;
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Validates that the manifest contains all required data.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ScenarioId))
                return false;

            if (string.IsNullOrWhiteSpace(DisplayName))
                return false;

            if (string.IsNullOrWhiteSpace(MapFilename))
                return false;

            if (string.IsNullOrWhiteSpace(OobFilename))
                return false;

            if (PrestigePool < 0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the path in the assets/resources folder to the thumbnail image. Must use resource load.
        /// </summary>
        public string GetThumbnailPath()
        {
            if (string.IsNullOrWhiteSpace(ThumbnailFilename))
                return string.Empty;

            return "Art/Scenario Thumbs/" + ThumbnailFilename;
        }

        /// <summary>
        /// Gets the full file system path to the map file.
        /// </summary>
        public string GetMapFilePath()
        {
            if (string.IsNullOrWhiteSpace(MapFilename))
                return string.Empty;

            return Path.Combine(AppService.MapPath, MapFilename);
        }

        /// <summary>
        /// Gets the full file system path to the OOB file.
        /// </summary>
        public string GetOobFilePath()
        {
            if (string.IsNullOrWhiteSpace(OobFilename))
                return string.Empty;

            return Path.Combine(AppService.OobPath, OobFilename);
        }

        /// <summary>
        /// Gets the full file system path to the AII file.
        /// </summary>
        public string GetAiiFilePath()
        {
            if (string.IsNullOrWhiteSpace(AiiFilename))
                return string.Empty;

            return Path.Combine(AppService.AiiPath, AiiFilename);
        }

        /// <summary>
        /// Gets the full file system path to the briefing file.
        /// </summary>
        public string GetBriefingFilePath()
        {
            if (string.IsNullOrWhiteSpace(BriefingFilename))
                return string.Empty;

            return Path.Combine(AppService.BrfPath, BriefingFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP (generated data path) map file, for campaign scenarios.
        /// </summary>
        public string GetMapFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(MapFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_MapPath, MapFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP OOB file, for campaign scenarios.
        /// </summary>
        public string GetOobFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(OobFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_OobPath, OobFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP AII file, for campaign scenarios.
        /// </summary>
        public string GetAiiFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(AiiFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_AiiPath, AiiFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP briefing file, for campaign scenarios.
        /// </summary>
        public string GetBriefingFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(BriefingFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_BrfPath, BriefingFilename);
        }

        #endregion // Public Methods
    }
}
