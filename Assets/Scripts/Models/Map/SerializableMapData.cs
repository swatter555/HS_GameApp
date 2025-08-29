using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Legacy serializable map data structure for binary deserialization.
    /// Used only for converting old binary map files to JSON format.
    /// </summary>
    [Serializable]
    public class SerializableMapData
    {
        #region Constants
        private const string CLASS_NAME = nameof(SerializableMapData);
        #endregion

        #region Properties

        /// <summary>
        /// Map header containing metadata and configuration.
        /// </summary>
        public SerializableMapHeader Header { get; set; }

        /// <summary>
        /// Collection of all hex tiles in the map.
        /// </summary>
        public List<SerializableHex> Hexes { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for binary deserialization.
        /// </summary>
        public SerializableMapData()
        {
            Header = new SerializableMapHeader();
            Hexes = new List<SerializableHex>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates the map data for consistency and completeness.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateMapData()
        {
            try
            {
                if (Header == null)
                {
                    AppService.CaptureUiMessage("Map header is null");
                    return false;
                }

                if (Hexes == null || Hexes.Count == 0)
                {
                    AppService.CaptureUiMessage("Map has no hex data");
                    return false;
                }

                // Validate header
                if (!Header.ValidateHeader())
                {
                    return false;
                }

                // Check for duplicate positions
                var positions = new HashSet<Vector2Int>();
                foreach (var hex in Hexes)
                {
                    var pos = new Vector2Int(hex.X, hex.Y);
                    if (positions.Contains(pos))
                    {
                        AppService.CaptureUiMessage($"Duplicate hex position found: {pos}");
                        return false;
                    }
                    positions.Add(pos);
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateMapData), ex);
                return false;
            }
        }

        /// <summary>
        /// Converts this legacy map data to the new JSON-compatible format.
        /// </summary>
        /// <returns>List of JSON-compatible Hex objects</returns>
        public List<Hex> ConvertToJsonHexes()
        {
            try
            {
                var jsonHexes = new List<Hex>();

                foreach (var serializableHex in Hexes)
                {
                    var jsonHex = serializableHex.ConvertToJsonHex();
                    if (jsonHex != null)
                    {
                        jsonHexes.Add(jsonHex);
                    }
                }

                AppService.CaptureUiMessage($"Successfully converted {jsonHexes.Count} hexes to JSON format");
                return jsonHexes;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertToJsonHexes), ex);
                return new List<Hex>();
            }
        }

        #endregion
    }

    /// <summary>
    /// Legacy serializable map header for binary deserialization.
    /// Contains map metadata and configuration information.
    /// </summary>
    [Serializable]
    public class SerializableMapHeader
    {
        #region Constants
        private const string CLASS_NAME = nameof(SerializableMapHeader);
        #endregion

        #region Properties

        /// <summary>
        /// Map configuration type (Small/Large).
        /// </summary>
        public MapConfig MapConfiguration { get; set; }

        /// <summary>
        /// Map theme (Europe/MiddleEast/China).
        /// </summary>
        public MapTheme Theme { get; set; }

        /// <summary>
        /// Map name or identifier.
        /// </summary>
        public string MapName { get; set; }

        /// <summary>
        /// Map width in hexes.
        /// </summary>
        public int MapWidth { get; set; }

        /// <summary>
        /// Map height in hexes.
        /// </summary>
        public int MapHeight { get; set; }

        /// <summary>
        /// Version of the map format.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for binary deserialization.
        /// </summary>
        public SerializableMapHeader()
        {
            MapConfiguration = MapConfig.None;
            Theme = MapTheme.None;
            MapName = string.Empty;
            MapWidth = 0;
            MapHeight = 0;
            Version = 1;
            CreatedDate = DateTime.MinValue;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates the header data for consistency.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateHeader()
        {
            try
            {
                if (string.IsNullOrEmpty(MapName))
                {
                    AppService.CaptureUiMessage("Map name is empty");
                    return false;
                }

                if (MapWidth <= 0 || MapHeight <= 0)
                {
                    AppService.CaptureUiMessage($"Invalid map dimensions: {MapWidth}x{MapHeight}");
                    return false;
                }

                if (!Enum.IsDefined(typeof(MapConfig), MapConfiguration))
                {
                    AppService.CaptureUiMessage($"Invalid map configuration: {MapConfiguration}");
                    return false;
                }

                if (!Enum.IsDefined(typeof(MapTheme), Theme))
                {
                    AppService.CaptureUiMessage($"Invalid map theme: {Theme}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateHeader), ex);
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Legacy serializable hex data for binary deserialization.
    /// Contains all hex properties in a format compatible with old binary files.
    /// </summary>
    [Serializable]
    public class SerializableHex
    {
        #region Constants
        private const string CLASS_NAME = nameof(SerializableHex);
        #endregion

        #region Properties

        // Position
        public int X { get; set; }
        public int Y { get; set; }

        // Terrain
        public TerrainType Terrain { get; set; }

        // Infrastructure
        public bool IsRail { get; set; }
        public bool IsRoad { get; set; }
        public bool IsFort { get; set; }
        public bool IsAirbase { get; set; }

        // Game State
        public bool IsObjective { get; set; }
        public bool IsVisible { get; set; }
        public TileControl TileControl { get; set; }
        public DefaultTileControl DefaultTileControl { get; set; }

        // Labels and Display
        public string TileLabel { get; set; }
        public string LargeTileLabel { get; set; }
        public TextSize LabelSize { get; set; }
        public FontWeight LabelWeight { get; set; }
        public TextColor LabelColor { get; set; }
        public float LabelOutlineThickness { get; set; }

        // Victory and Damage
        public float VictoryValue { get; set; }
        public float AirbaseDamage { get; set; }
        public int UrbanDamage { get; set; }

        // Border Features (stored as binary strings)
        public string RiverBorderString { get; set; }
        public string BridgeBorderString { get; set; }
        public string PontoonBridgeBorderString { get; set; }
        public string DamagedBridgeBorderString { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for binary deserialization.
        /// </summary>
        public SerializableHex()
        {
            X = 0;
            Y = 0;
            Terrain = TerrainType.Clear;
            IsRail = false;
            IsRoad = false;
            IsFort = false;
            IsAirbase = false;
            IsObjective = false;
            IsVisible = false;
            TileControl = TileControl.None;
            DefaultTileControl = DefaultTileControl.None;
            TileLabel = string.Empty;
            LargeTileLabel = string.Empty;
            LabelSize = TextSize.Small;
            LabelWeight = FontWeight.Medium;
            LabelColor = TextColor.Blue;
            LabelOutlineThickness = 0.1f;
            VictoryValue = 0f;
            AirbaseDamage = 0f;
            UrbanDamage = 0;
            RiverBorderString = "000000";
            BridgeBorderString = "000000";
            PontoonBridgeBorderString = "000000";
            DamagedBridgeBorderString = "000000";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts this legacy hex data to the new JSON-compatible Hex format.
        /// </summary>
        /// <returns>New Hex object with converted data</returns>
        public Hex ConvertToJsonHex()
        {
            try
            {
                // Create new JSON-compatible hex
                var jsonHex = new Hex(new Vector2Int(X, Y));

                // Copy basic properties
                jsonHex.SetTerrain(Terrain);
                jsonHex.SetIsRail(IsRail);
                jsonHex.SetIsRoad(IsRoad);
                jsonHex.SetIsFort(IsFort);
                jsonHex.SetIsAirbase(IsAirbase);
                jsonHex.SetIsObjective(IsObjective);
                jsonHex.SetIsVisible(IsVisible);

                // Copy control and labels
                jsonHex.TileControl = TileControl;
                jsonHex.DefaultTileControl = DefaultTileControl;
                jsonHex.TileLabel = TileLabel ?? string.Empty;
                jsonHex.LargeTileLabel = LargeTileLabel ?? string.Empty;
                jsonHex.LabelSize = LabelSize;
                jsonHex.LabelWeight = LabelWeight;
                jsonHex.LabelColor = LabelColor;
                jsonHex.LabelOutlineThickness = LabelOutlineThickness;

                // Copy victory and damage values
                jsonHex.VictoryValue = VictoryValue;
                jsonHex.AirbaseDamage = AirbaseDamage;
                jsonHex.UrbanDamage = UrbanDamage;

                // Convert border strings to FeatureBorders objects
                jsonHex.RiverBorders = new FeatureBorders(RiverBorderString ?? "000000", BorderType.River);
                jsonHex.BridgeBorders = new FeatureBorders(BridgeBorderString ?? "000000", BorderType.Bridge);
                jsonHex.PontoonBridgeBorders = new FeatureBorders(PontoonBridgeBorderString ?? "000000", BorderType.PontoonBridge);
                jsonHex.DamagedBridgeBorders = new FeatureBorders(DamagedBridgeBorderString ?? "000000", BorderType.DestroyedBridge);

                return jsonHex;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertToJsonHex), ex);
                return null;
            }
        }

        /// <summary>
        /// Applies this hex data to an existing Hex object (legacy compatibility method).
        /// </summary>
        /// <param name="targetHex">The hex object to update</param>
        public void ApplyToHex(Hex targetHex)
        {
            try
            {
                if (targetHex == null)
                {
                    throw new ArgumentNullException(nameof(targetHex));
                }

                // Set position and terrain
                targetHex.SetPosition(new Vector2Int(X, Y));
                targetHex.SetTerrain(Terrain);

                // Set infrastructure features
                targetHex.SetIsRail(IsRail);
                targetHex.SetIsRoad(IsRoad);
                targetHex.SetIsFort(IsFort);
                targetHex.SetIsAirbase(IsAirbase);

                // Set game state
                targetHex.SetIsObjective(IsObjective);
                targetHex.SetIsVisible(IsVisible);

                // Set control and labels directly (no setter methods for these)
                targetHex.TileControl = TileControl;
                targetHex.DefaultTileControl = DefaultTileControl;
                targetHex.TileLabel = TileLabel ?? string.Empty;
                targetHex.LargeTileLabel = LargeTileLabel ?? string.Empty;
                targetHex.LabelSize = LabelSize;
                targetHex.LabelWeight = LabelWeight;
                targetHex.LabelColor = LabelColor;
                targetHex.LabelOutlineThickness = LabelOutlineThickness;

                // Set victory and damage values
                targetHex.VictoryValue = VictoryValue;
                targetHex.AirbaseDamage = AirbaseDamage;
                targetHex.UrbanDamage = UrbanDamage;

                // Convert and set border features
                targetHex.RiverBorders = new FeatureBorders(RiverBorderString ?? "000000", BorderType.River);
                targetHex.BridgeBorders = new FeatureBorders(BridgeBorderString ?? "000000", BorderType.Bridge);
                targetHex.PontoonBridgeBorders = new FeatureBorders(PontoonBridgeBorderString ?? "000000", BorderType.PontoonBridge);
                targetHex.DamagedBridgeBorders = new FeatureBorders(DamagedBridgeBorderString ?? "000000", BorderType.DestroyedBridge);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyToHex), ex);
                throw;
            }
        }

        /// <summary>
        /// Validates this hex data for consistency.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateHexData()
        {
            try
            {
                // Check for valid terrain type
                if (!Enum.IsDefined(typeof(TerrainType), Terrain))
                {
                    AppService.CaptureUiMessage($"Invalid terrain type at ({X}, {Y}): {Terrain}");
                    return false;
                }

                // Check for mutually exclusive features
                if (IsFort && IsAirbase)
                {
                    AppService.CaptureUiMessage($"Hex at ({X}, {Y}): Fort and Airbase cannot both be true");
                    return false;
                }

                // Validate border strings
                if (!ValidateBorderString(RiverBorderString, "River"))
                    return false;
                if (!ValidateBorderString(BridgeBorderString, "Bridge"))
                    return false;
                if (!ValidateBorderString(PontoonBridgeBorderString, "PontoonBridge"))
                    return false;
                if (!ValidateBorderString(DamagedBridgeBorderString, "DamagedBridge"))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateHexData), ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates a border string format.
        /// </summary>
        /// <param name="borderString">The border string to validate</param>
        /// <param name="borderType">The border type name for error messages</param>
        /// <returns>True if valid, false otherwise</returns>
        private bool ValidateBorderString(string borderString, string borderType)
        {
            try
            {
                if (string.IsNullOrEmpty(borderString))
                {
                    // Empty is acceptable, will default to "000000"
                    return true;
                }

                if (borderString.Length != 6)
                {
                    AppService.CaptureUiMessage($"Hex at ({X}, {Y}): {borderType} border string must be 6 characters, got {borderString.Length}");
                    return false;
                }

                foreach (char c in borderString)
                {
                    if (c != '0' && c != '1')
                    {
                        AppService.CaptureUiMessage($"Hex at ({X}, {Y}): {borderType} border string contains invalid character '{c}'");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateBorderString), ex);
                return false;
            }
        }

        #endregion
    }
}