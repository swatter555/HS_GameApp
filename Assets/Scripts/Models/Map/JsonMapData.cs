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
        /// <summary>
        /// Map header containing metadata and integrity information.
        /// </summary>
        [JsonInclude]
        public JsonMapHeader Header { get; set; }

        /// <summary>
        /// Array of all hex tiles in the map.
        /// </summary>
        [JsonInclude]
        public GameHex[] Hexes { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Parameterless constructor for JSON serialization.
        /// </summary>
        [JsonConstructor]
        public JsonMapData()
        {
            Header = new JsonMapHeader();
            Hexes = Array.Empty<GameHex>();
        }

        /// <summary>
        /// Creates a new JsonMapData instance with header and hex data.
        /// </summary>
        /// <param name="header">Map header information</param>
        /// <param name="hexes">Array of hex tiles</param>
        public JsonMapData(JsonMapHeader header, GameHex[] hexes)
        {
            try
            {
                Header = header ?? throw new ArgumentNullException(nameof(header));
                Hexes = hexes ?? throw new ArgumentNullException(nameof(hexes));
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(JsonMapData), ex);
                throw;
            }
        }
        #endregion

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

                // Validate each hex
                for (int i = 0; i < Hexes.Length; i++)
                {
                    if (Hexes[i] == null)
                    {
                        AppService.CaptureUiMessage($"JsonMapData validation failed: Hex at index {i} is null");
                        return false;
                    }

                    if (!Hexes[i].ValidateHex())
                    {
                        AppService.CaptureUiMessage($"JsonMapData validation failed: Hex at index {i} failed validation");
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

                return $"Map: {Header.MapName}, Size: {Header.MapSize}, Hexes: {GetHexCount()}, Version: {Header.SaveVersion}";
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetSummary), ex);
                return "JsonMapData: Error generating summary";
            }
        }
        #endregion
    }
}