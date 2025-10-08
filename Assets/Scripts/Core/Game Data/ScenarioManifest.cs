using System;
using System.IO;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Core.GameData
{
    /// <summary>
    /// Serializable data structure representing a scenario manifest file.
    /// Lists all files required to load a scenario and provides metadata for UI display.
    /// </summary>
    [Serializable]
    public class ScenarioManifest
    {
        private const string CLASS_NAME = "ScenarioManifest";

        #region Serialized Fields

        [SerializeField] private string scenarioId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private string thumbnailFilename;
        [SerializeField] private string mapFilename;
        [SerializeField] private string oobFilename;
        [SerializeField] private string aiiFilename;
        [SerializeField] private string briefingFilename;
        [SerializeField] private int prestigePool;
        [SerializeField] private bool isCampaignScenario;

        #endregion // Serialized Fields

        #region Properties

        public string ScenarioId => scenarioId;
        public string DisplayName => displayName;
        public string Description => description;
        public string ThumbnailFilename => thumbnailFilename;
        public string MapFilename => mapFilename;
        public string OobFilename => oobFilename;
        public string AiiFilename => aiiFilename;
        public string BriefingFilename => briefingFilename;
        public int PrestigePool => prestigePool;
        public bool IsCampaignScenario => isCampaignScenario;

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Parameterless constructor for JSON deserialization.
        /// </summary>
        public ScenarioManifest()
        {
            scenarioId = string.Empty;
            displayName = string.Empty;
            description = string.Empty;
            thumbnailFilename = string.Empty;
            mapFilename = string.Empty;
            oobFilename = string.Empty;
            aiiFilename = string.Empty;
            briefingFilename = string.Empty;
            prestigePool = 0;
            isCampaignScenario = false;
        }

        /// <summary>
        /// Constructor with full parameters for creating manifests programmatically.
        /// </summary>
        public ScenarioManifest(string scenarioId, string displayName, string description,
            string thumbnailFilename, string mapFilename, string oobFilename,
            string aiiFilename, string briefingFilename, int prestigePool, bool isCampaignScenario)
        {
            this.scenarioId = scenarioId;
            this.displayName = displayName;
            this.description = description;
            this.thumbnailFilename = thumbnailFilename;
            this.mapFilename = mapFilename;
            this.oobFilename = oobFilename;
            this.aiiFilename = aiiFilename;
            this.briefingFilename = briefingFilename;
            this.prestigePool = prestigePool;
            this.isCampaignScenario = isCampaignScenario;
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Validates that the manifest contains all required data.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
                return false;

            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            if (string.IsNullOrWhiteSpace(mapFilename))
                return false;

            if (string.IsNullOrWhiteSpace(oobFilename))
                return false;

            if (prestigePool < 0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the path in the assets/resources folder to the thumbnail image. Must use resource load.
        /// </summary>
        public string GetThumbnailPath()
        {
            if (string.IsNullOrWhiteSpace(thumbnailFilename))
                return string.Empty;

            return "Art/Scenario Thumbs/" + thumbnailFilename;
        }

        /// <summary>
        /// Gets the full file system path to the map file.
        /// </summary>
        public string GetMapFilePath()
        {
            if (string.IsNullOrWhiteSpace(mapFilename))
                return string.Empty;

            return Path.Combine(AppService.MapPath, mapFilename);
        }

        /// <summary>
        /// Gets the full file system path to the OOB file.
        /// </summary>
        public string GetOobFilePath()
        {
            if (string.IsNullOrWhiteSpace(oobFilename))
                return string.Empty;

            return Path.Combine(AppService.OobPath, oobFilename);
        }

        /// <summary>
        /// Gets the full file system path to the AII file.
        /// </summary>
        public string GetAiiFilePath()
        {
            if (string.IsNullOrWhiteSpace(aiiFilename))
                return string.Empty;

            return Path.Combine(AppService.AiiPath, aiiFilename);
        }

        /// <summary>
        /// Gets the full file system path to the briefing file.
        /// </summary>
        public string GetBriefingFilePath()
        {
            if (string.IsNullOrWhiteSpace(briefingFilename))
                return string.Empty;

            return Path.Combine(AppService.BrfPath, briefingFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP (generated data path) map file, for campaign scenarios.
        /// </summary>
        public string GetMapFilePath_GDP()
        {             
            if (string.IsNullOrWhiteSpace(mapFilename))
                return string.Empty;
            
            return Path.Combine(AppService.GDP_MapPath, mapFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP OOB file, for campaign scenarios.
        /// </summary>
        public string GetOobFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(oobFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_OobPath, oobFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP AII file, for campaign scenarios.
        /// </summary>
        public string GetAiiFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(aiiFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_AiiPath, aiiFilename);
        }

        /// <summary>
        /// Retrieves the full file path for the GDP briefing file, for campaign scenarios.
        /// </summary>
        public string GetBriefingFilePath_GDP()
        {
            if (string.IsNullOrWhiteSpace(briefingFilename))
                return string.Empty;

            return Path.Combine(AppService.GDP_BrfPath, briefingFilename);
        }

        #endregion // Public Methods
    }
}