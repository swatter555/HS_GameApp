using System;
using System.Text.Json.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// JSON-serializable container for complete map data with header and hex information.
    /// Used for persisting maps in JSON format with data integrity validation.
    /// </summary>
    [Serializable]
    public class JsonMapData
    {
        #region Constants

        private const string CLASS_NAME = nameof(JsonMapData);

        #endregion

        #region Properties

        // Map header containing metadata and integrity information.
        [JsonPropertyName("header")]
        public JsonMapHeader Header { get; set; }

        // Array of all hex tiles in the map.
        [JsonPropertyName("hexes")]
        public HexTile[] Hexes { get; set; }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// JSON deserialization constructor with explicit parameters for all serializable properties.
        /// System.Text.Json uses this constructor to create objects with all data available at construction time.
        /// </summary>
        [JsonConstructor]
        public JsonMapData(JsonMapHeader header, HexTile[] hexes)
        {
            Header = header;
            Hexes = hexes;
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Validates the map data for consistency and completeness.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            try
            {
                if (Header == null)
                {
                    AppService.CaptureUiMessage("JsonMapData validation failed: Header is null");
                    return false;
                }

                if (Hexes == null)
                {
                    AppService.CaptureUiMessage("JsonMapData validation failed: Hexes array is null");
                    return false;
                }

                if (!Header.IsValid())
                {
                    AppService.CaptureUiMessage("JsonMapData validation failed: Header validation failed");
                    return false;
                }

                // Skip individual hex validation during initial load.
                // Hexes will be properly initialized and validated when added to HexMap.
                // This validation only checks for null hexes to ensure the array is properly populated.
                for (int i = 0; i < Hexes.Length; i++)
                {
                    if (Hexes[i] == null)
                    {
                        AppService.CaptureUiMessage($"JsonMapData validation failed: Hex at index {i} is null");
                        return false;
                    }
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
        /// Gets the total number of hexes in the map.
        /// </summary>
        /// <returns>Number of hexes</returns>
        public int GetHexCount()
        {
            try
            {
                return Hexes?.Length ?? 0;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetHexCount), ex);
                return 0;
            }
        }

        /// <summary>
        /// Creates a summary string of the map data.
        /// </summary>
        /// <returns>Summary information</returns>
        public string GetSummary()
        {
            try
            {
                if (Header == null)
                {
                    return "JsonMapData: Header is null";
                }

                return $"Map: {Header.MapName}, Config: {Header.MapConfiguration}, Hexes: {GetHexCount()}, Version: {Header.SaveVersion}";
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetSummary), ex);
                return "JsonMapData: Error generating summary";
            }
        }

        #endregion // Public Methods
    }
}
