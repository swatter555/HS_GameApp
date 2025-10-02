using System;
using System.Text.Json.Serialization;
using HammerAndSickle.Services;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// JSON-serializable map header containing metadata and data integrity information.
    /// Used for map identification, versioning, and checksum validation.
    /// </summary>
    [Serializable]
    public class JsonMapHeader
    {
        #region Constants
        private const string CLASS_NAME = nameof(JsonMapHeader);
        #endregion

        #region Properties
        /// <summary>
        /// Display name of the map.
        /// </summary>
        [JsonInclude]
        public string MapName { get; set; }

        /// <summary>
        /// Map size configuration (Small, Large, None).
        /// </summary>
        [JsonInclude]
        public MapConfig MapConfiguration { get; set; }

        /// <summary>
        /// Save format version for compatibility tracking.
        /// </summary>
        [JsonInclude]
        public int SaveVersion { get; set; }

        /// <summary>
        /// Data integrity checksum for validation.
        /// </summary>
        [JsonInclude]
        public string Checksum { get; set; }

        /// <summary>
        /// Creation timestamp for the JSON file.
        /// </summary>
        [JsonInclude]
        public DateTime CreatedAt { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Parameterless constructor for JSON serialization.
        /// </summary>
        [JsonConstructor]
        public JsonMapHeader()
        {
            MapName = string.Empty;
            MapConfiguration = MapConfig.None;
            SaveVersion = HexMapConstants.CurrentMapDataVersion;
            Checksum = string.Empty;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new JsonMapHeader with specified values.
        /// </summary>
        /// <param name="mapName">Name of the map</param>
        /// <param name="mapConfiguration">Size configuration of the map</param>
        /// <param name="checksum">Data integrity checksum</param>
        public JsonMapHeader(string mapName, MapConfig mapConfiguration, string checksum)
        {
            try
            {
                MapName = mapName ?? throw new ArgumentNullException(nameof(mapName));
                MapConfiguration = mapConfiguration;
                SaveVersion = HexMapConstants.CurrentMapDataVersion;
                Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
                CreatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(JsonMapHeader), ex);
                throw;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Validates the header data for consistency and completeness.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MapName))
                {
                    AppService.CaptureUiMessage("JsonMapHeader validation failed: MapName is null or empty");
                    return false;
                }

                if (!Enum.IsDefined(typeof(MapConfig), MapConfiguration))
                {
                    AppService.CaptureUiMessage($"JsonMapHeader validation failed: Invalid MapSize value {MapConfiguration}");
                    return false;
                }

                if (SaveVersion <= 0)
                {
                    AppService.CaptureUiMessage($"JsonMapHeader validation failed: Invalid SaveVersion {SaveVersion}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Checksum))
                {
                    AppService.CaptureUiMessage("JsonMapHeader validation failed: Checksum is null or empty");
                    return false;
                }

                if (CreatedAt == default)
                {
                    AppService.CaptureUiMessage("JsonMapHeader validation failed: CreatedAt is default value");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsValid), ex);
                return false;
            }
        }

        /// <summary>
        /// Updates the checksum value.
        /// </summary>
        /// <param name="newChecksum">New checksum value</param>
        public void UpdateChecksum(string newChecksum)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newChecksum))
                {
                    throw new ArgumentException("Checksum cannot be null or empty", nameof(newChecksum));
                }

                Checksum = newChecksum;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateChecksum), ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the current save version constant.
        /// </summary>
        /// <returns>Current save version</returns>
        public static int GetCurrentSaveVersion()
        {
            return HexMapConstants.CurrentMapDataVersion;
        }

        /// <summary>
        /// Checks if this header version is compatible with the current version.
        /// </summary>
        /// <returns>True if compatible, false otherwise</returns>
        public bool IsCompatibleVersion()
        {
            try
            {
                // For now, only exact version matches are compatible
                // Future versions can implement backward compatibility logic
                return SaveVersion == HexMapConstants.CurrentMapDataVersion;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsCompatibleVersion), ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a summary string of the header information.
        /// </summary>
        /// <returns>Header summary</returns>
        public string GetSummary()
        {
            try
            {
                return $"'{MapName}' ({MapConfiguration}) v{SaveVersion} created {CreatedAt:yyyy-MM-dd HH:mm} UTC";
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetSummary), ex);
                return "JsonMapHeader: Error generating summary";
            }
        }
        #endregion
    }
}