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

        // ⚠ NO `contentVersion` HERE, DELIBERATELY (added then removed 2026-07-28, Bob's call).
        // A per-scenario content version cannot earn its place while content ships INSIDE the build:
        // StreamingAssets is replaced wholesale by a Steam patch, so content and exe move together and a
        // scenario's version could never legitimately disagree with the `gameVersion` already recorded in
        // the save header. Modding is designed out, so there is no foreign content to reconcile either,
        // and the .map header's editor-maintained `checksum` is a better content identity than a
        // hand-kept string — automatic and tamper-evident, where a forgotten version stamp asserts
        // something false. An always-empty field that LOOKS meaningful is the MapChecksumUtility mistake
        // (§7.1); this one was caught before it shipped a claim.
        // ⚠ The CAMPAIGN manifest is a genuinely different case and stays open — see todo.md Phase 2.1.

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

        // Per-scenario point budget the player may spend fielding units during Deployment
        // (§20.1 / §35.4). Replaces the RETIRED maxCoreUnits/maxDeployLand/maxDeployAir fields;
        // the campaign-wide ownership cap (coreForcePointCap) lives in the .cmp, not here.
        [JsonPropertyName("deploymentPointCap")]
        public int DeploymentPointCap { get; set; } = 0;

        [JsonPropertyName("mapWidth")]
        public int MapWidth { get; set; } = 0;

        [JsonPropertyName("mapHeight")]
        public int MapHeight { get; set; } = 0;

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
            int deploymentPointCap,
            int mapWidth = 0,
            int mapHeight = 0)
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
            DeploymentPointCap = deploymentPointCap;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
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

            // Dimensions must resolve to valid values (explicit or via MapConfig fallback)
            var dims = GetMapDimensions();
            if (dims.x < 10 || dims.y < 10)
                return false;

            return true;
        }

        /// <summary>
        /// Resolves map dimensions. Uses explicit MapWidth/MapHeight when present,
        /// otherwise falls back to MapConfig-derived defaults for legacy manifests.
        /// </summary>
        public UnityEngine.Vector2Int GetMapDimensions()
        {
            if (MapWidth >= 10 && MapHeight >= 10)
                return new UnityEngine.Vector2Int(MapWidth, MapHeight);

            // Backward compat: derive from MapConfig header in the .map file.
            // Callers that need dimensions before the map is loaded should ensure
            // manifests include explicit width/height fields.
            return new UnityEngine.Vector2Int(GameData.SmallHexWidth, GameData.SmallHexHeight);
        }

        /// <summary>
        /// Gets the path in the assets/resources folder to the thumbnail image. Must use resource load.
        /// </summary>
        public string GetThumbnailPath()
        {
            if (string.IsNullOrWhiteSpace(ThumbnailFilename))
                return string.Empty;
            
            return Path.Combine(AppService.ScenarioThumbnailPath, ThumbnailFilename);
        }

        // ────────────────────────────────────────────────────────────────────────────────────────────
        // CONTENT PATHS (rewritten 2026-07-27, content pipeline Phase 1).
        //
        // ⚠ This REPLACED two parallel method families — GetMapFilePath() resolving to Documents/My Games
        // and GetMapFilePath_GDP() resolving to Assets/Generated Data — which MapLoader and BattleManager
        // chose between on IsCampaignScenario. That welded a GAMEPLAY concept (campaign vs standalone) to
        // a STORAGE one (which folder), so a campaign could not be standalone-tested and the two copies
        // silently diverged. Now there is one family, and it resolves against the folder the manifest was
        // loaded from, so where a scenario lives is nobody's business but the loader's.
        // ────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Directory this manifest was loaded from — its scenario's self-contained content folder.
        /// Set by the loader at read time; transient, never serialized (the manifest must not carry a
        /// machine-specific absolute path into a save or into shipped content).
        /// </summary>
        [JsonIgnore]
        public string ContentRoot { get; set; } = string.Empty;

        /// <summary>Full path to the map file, or empty if unnamed.</summary>
        public string GetMapFilePath() => ResolveContentFile(MapFilename);

        /// <summary>Full path to the OOB file, or empty if unnamed.</summary>
        public string GetOobFilePath() => ResolveContentFile(OobFilename);

        /// <summary>
        /// Full path to the AII (AI hints) file, or empty if unnamed.
        /// ⚠ No .aii files exist yet — the AI pass will author them (Bob, 2026-07-27). Callers must treat
        /// a missing AII as a clean no-op, NOT an error.
        /// </summary>
        public string GetAiiFilePath() => ResolveContentFile(AiiFilename);

        /// <summary>Full path to the briefing file, or empty if unnamed.</summary>
        public string GetBriefingFilePath() => ResolveContentFile(BriefingFilename);

        /// <summary>
        /// Resolves a filename against this scenario's own content folder.
        /// Returns empty for an unnamed file so callers can treat "not specified" and "not found" alike.
        /// </summary>
        private string ResolveContentFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(ContentRoot))
                return string.Empty;

            return Path.Combine(ContentRoot, filename);
        }

        #endregion // Public Methods
    }
}
