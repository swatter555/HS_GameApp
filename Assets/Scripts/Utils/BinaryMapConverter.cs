using HammerAndSickle.Models.Map;
using HammerAndSickle.Models.Map.Legacy;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace HammerAndSickle.Utils
{
    /// <summary>
    /// Utility for converting legacy binary map files to JSON format.
    /// This is a one-time conversion tool for migrating existing maps.
    /// </summary>
    public static class BinaryMapConverter
    {
        #region Constants
        private const string CLASS_NAME = nameof(BinaryMapConverter);
        private const string BINARY_MAP_EXTENSION = ".map";
        private const string JSON_MAP_EXTENSION = ".json";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        #endregion

        #region Public Methods

        /// <summary>
        /// Converts a single binary map file to JSON format.
        /// </summary>
        /// <param name="binaryFilePath">Path to the binary map file</param>
        /// <param name="outputDirectory">Directory to save the JSON file (optional)</param>
        /// <returns>Path to the created JSON file, or null if conversion failed</returns>
        public static async Task<string> ConvertBinaryMapToJsonAsync(string binaryFilePath, string outputDirectory = null)
        {
            try
            {
                if (string.IsNullOrEmpty(binaryFilePath) || !File.Exists(binaryFilePath))
                {
                    AppService.CaptureUiMessage($"Binary map file not found: {binaryFilePath}");
                    return null;
                }

                AppService.CaptureUiMessage($"Starting conversion of {Path.GetFileName(binaryFilePath)}");

                // Load binary map data
                var binaryMapData = LoadBinaryMapFile(binaryFilePath);
                if (binaryMapData == null)
                {
                    AppService.CaptureUiMessage($"Failed to load binary map data from {binaryFilePath}");
                    return null;
                }

                // Validate binary data
                if (!binaryMapData.ValidateMapData())
                {
                    AppService.CaptureUiMessage($"Binary map data validation failed for {binaryFilePath}");
                    return null;
                }

                // Convert to JSON format
                var jsonMapData = ConvertToJsonMapData(binaryMapData);
                if (jsonMapData == null)
                {
                    AppService.CaptureUiMessage($"Failed to convert binary data to JSON format");
                    return null;
                }

                // Determine output path
                outputDirectory ??= Path.GetDirectoryName(binaryFilePath);
                string fileName = Path.GetFileNameWithoutExtension(binaryFilePath);
                string jsonFilePath = Path.Combine(outputDirectory, $"{fileName}{JSON_MAP_EXTENSION}");

                // Save JSON file
                bool success = await SaveJsonMapFileAsync(jsonMapData, jsonFilePath);
                if (!success)
                {
                    AppService.CaptureUiMessage($"Failed to save JSON map file to {jsonFilePath}");
                    return null;
                }

                AppService.CaptureUiMessage($"Successfully converted {Path.GetFileName(binaryFilePath)} to {Path.GetFileName(jsonFilePath)}");
                return jsonFilePath;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertBinaryMapToJsonAsync), ex);
                return null;
            }
        }

        /// <summary>
        /// Converts all binary map files in a directory to JSON format.
        /// </summary>
        /// <param name="inputDirectory">Directory containing binary map files</param>
        /// <param name="outputDirectory">Directory to save JSON files (optional, defaults to input directory)</param>
        /// <returns>List of successfully converted JSON file paths</returns>
        public static async Task<List<string>> ConvertAllBinaryMapsAsync(string inputDirectory, string outputDirectory = null)
        {
            var convertedFiles = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(inputDirectory) || !Directory.Exists(inputDirectory))
                {
                    AppService.CaptureUiMessage($"Input directory not found: {inputDirectory}");
                    return convertedFiles;
                }

                outputDirectory ??= inputDirectory;

                // Find all binary map files
                var binaryFiles = Directory.GetFiles(inputDirectory, $"*{BINARY_MAP_EXTENSION}", SearchOption.TopDirectoryOnly);

                if (binaryFiles.Length == 0)
                {
                    AppService.CaptureUiMessage($"No binary map files found in {inputDirectory}");
                    return convertedFiles;
                }

                AppService.CaptureUiMessage($"Found {binaryFiles.Length} binary map files to convert");

                // Convert each file
                foreach (var binaryFile in binaryFiles)
                {
                    var jsonFile = await ConvertBinaryMapToJsonAsync(binaryFile, outputDirectory);
                    if (!string.IsNullOrEmpty(jsonFile))
                    {
                        convertedFiles.Add(jsonFile);
                    }
                }

                AppService.CaptureUiMessage($"Conversion complete: {convertedFiles.Count}/{binaryFiles.Length} files converted successfully");
                return convertedFiles;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertAllBinaryMapsAsync), ex);
                return convertedFiles;
            }
        }

        /// <summary>
        /// Validates that a converted JSON map can be loaded correctly.
        /// </summary>
        /// <param name="jsonFilePath">Path to the JSON map file</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public static async Task<bool> ValidateConvertedMapAsync(string jsonFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath))
                {
                    AppService.CaptureUiMessage($"JSON map file not found: {jsonFilePath}");
                    return false;
                }

                var jsonMapData = await LoadJsonMapFileAsync(jsonFilePath);
                if (jsonMapData == null)
                {
                    AppService.CaptureUiMessage($"Failed to load JSON map data from {jsonFilePath}");
                    return false;
                }

                // Validate hex data
                bool allValid = true;
                foreach (var hex in jsonMapData.Hexes)
                {
                    if (!hex.ValidateHex())
                    {
                        allValid = false;
                    }
                }

                if (allValid)
                {
                    AppService.CaptureUiMessage($"JSON map validation successful: {Path.GetFileName(jsonFilePath)}");
                }
                else
                {
                    AppService.CaptureUiMessage($"JSON map validation failed: {Path.GetFileName(jsonFilePath)}");
                }

                return allValid;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateConvertedMapAsync), ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads a binary map file using BinaryFormatter.
        /// </summary>
        /// <param name="filePath">Path to the binary map file</param>
        /// <returns>Deserialized map data, or null if failed</returns>
        private static SerializableMapData LoadBinaryMapFile(string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                #pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete - we need it for legacy conversion
                var formatter = new BinaryFormatter();
                var mapData = (SerializableMapData)formatter.Deserialize(fileStream);
                #pragma warning restore SYSLIB0011

                AppService.CaptureUiMessage($"Successfully loaded binary map data: {mapData?.Hexes?.Count ?? 0} hexes");
                return mapData;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadBinaryMapFile), ex);
                return null;
            }
        }

        /// <summary>
        /// Converts legacy binary map data to JSON-compatible format.
        /// </summary>
        /// <param name="binaryMapData">Legacy map data</param>
        /// <returns>JSON-compatible map data</returns>
        private static JsonMapData ConvertToJsonMapData(SerializableMapData binaryMapData)
        {
            try
            {
                if (binaryMapData?.Header == null || binaryMapData.Hexes == null)
                {
                    AppService.CaptureUiMessage("Invalid binary map data structure");
                    return null;
                }

                // Convert header information
                var jsonHeader = new JsonMapHeader
                {
                    MapName = binaryMapData.Header.MapName,
                    MapConfiguration = binaryMapData.Header.MapConfiguration,
                    Theme = binaryMapData.Header.Theme,
                    MapWidth = binaryMapData.Header.MapWidth,
                    MapHeight = binaryMapData.Header.MapHeight,
                    Version = binaryMapData.Header.Version,
                    CreatedDate = binaryMapData.Header.CreatedDate,
                    ConvertedDate = DateTime.UtcNow,
                    OriginalFormat = "Binary"
                };

                // Convert hex data
                var jsonHexes = binaryMapData.ConvertToJsonHexes();
                if (jsonHexes == null || jsonHexes.Count == 0)
                {
                    AppService.CaptureUiMessage("Failed to convert hex data to JSON format");
                    return null;
                }

                var jsonMapData = new JsonMapData
                {
                    Header = jsonHeader,
                    Hexes = jsonHexes
                };

                AppService.CaptureUiMessage($"Successfully converted map data: {jsonHexes.Count} hexes");
                return jsonMapData;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertToJsonMapData), ex);
                return null;
            }
        }

        /// <summary>
        /// Saves JSON map data to file asynchronously.
        /// </summary>
        /// <param name="jsonMapData">Map data to save</param>
        /// <param name="filePath">Output file path</param>
        /// <returns>True if successful, false otherwise</returns>
        private static async Task<bool> SaveJsonMapFileAsync(JsonMapData jsonMapData, string filePath)
        {
            try
            {
                // Ensure output directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize to JSON
                var jsonString = JsonSerializer.Serialize(jsonMapData, JsonOptions);

                // Write to file
                await File.WriteAllTextAsync(filePath, jsonString);

                var fileInfo = new FileInfo(filePath);
                AppService.CaptureUiMessage($"JSON map saved: {Path.GetFileName(filePath)} ({fileInfo.Length:N0} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveJsonMapFileAsync), ex);
                return false;
            }
        }

        /// <summary>
        /// Loads a JSON map file for validation purposes.
        /// </summary>
        /// <param name="filePath">Path to the JSON map file</param>
        /// <returns>Loaded map data, or null if failed</returns>
        private static async Task<JsonMapData> LoadJsonMapFileAsync(string filePath)
        {
            try
            {
                var jsonString = await File.ReadAllTextAsync(filePath);
                var mapData = JsonSerializer.Deserialize<JsonMapData>(jsonString, JsonOptions);

                AppService.CaptureUiMessage($"Successfully loaded JSON map data: {mapData?.Hexes?.Count ?? 0} hexes");
                return mapData;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadJsonMapFileAsync), ex);
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// JSON-compatible map data structure for the new format.
    /// </summary>
    [Serializable]
    public class JsonMapData
    {
        /// <summary>
        /// Map header with metadata.
        /// </summary>
        public JsonMapHeader Header { get; set; }

        /// <summary>
        /// Collection of hex tiles.
        /// </summary>
        public List<Hex> Hexes { get; set; }

        public JsonMapData()
        {
            Header = new JsonMapHeader();
            Hexes = new List<Hex>();
        }
    }

    /// <summary>
    /// JSON-compatible map header with additional conversion metadata.
    /// </summary>
    [Serializable]
    public class JsonMapHeader
    {
        public string MapName { get; set; }
        public MapConfig MapConfiguration { get; set; }
        public MapTheme Theme { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ConvertedDate { get; set; }
        public string OriginalFormat { get; set; }

        public JsonMapHeader()
        {
            MapName = string.Empty;
            MapConfiguration = MapConfig.None;
            Theme = MapTheme.None;
            MapWidth = 0;
            MapHeight = 0;
            Version = 1;
            CreatedDate = DateTime.MinValue;
            ConvertedDate = DateTime.UtcNow;
            OriginalFormat = "JSON";
        }
    }

    /// <summary>
    /// Simple utility for batch conversion operations.
    /// </summary>
    public static class MapConversionHelper
    {
        #region Constants
        private const string CLASS_NAME = nameof(MapConversionHelper);
        #endregion

        /// <summary>
        /// Creates a conversion report showing before/after statistics.
        /// </summary>
        /// <param name="inputDirectory">Directory that was processed</param>
        /// <param name="convertedFiles">List of successfully converted files</param>
        /// <returns>Formatted conversion report</returns>
        public static string CreateConversionReport(string inputDirectory, List<string> convertedFiles)
        {
            try
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== Binary-to-JSON Map Conversion Report ===");
                report.AppendLine($"Input Directory: {inputDirectory}");
                report.AppendLine($"Conversion Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine();

                if (convertedFiles?.Count > 0)
                {
                    report.AppendLine($"Successfully Converted Files ({convertedFiles.Count}):");
                    foreach (var file in convertedFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        report.AppendLine($"  - {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
                    }
                }
                else
                {
                    report.AppendLine("No files were successfully converted.");
                }

                report.AppendLine();
                report.AppendLine("=== End Report ===");

                return report.ToString();
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(CreateConversionReport), ex);
                return "Error generating conversion report.";
            }
        }

        /// <summary>
        /// Saves the conversion report to a text file.
        /// </summary>
        /// <param name="report">Report content</param>
        /// <param name="outputDirectory">Directory to save the report</param>
        /// <returns>Path to the saved report file</returns>
        public static string SaveConversionReport(string report, string outputDirectory)
        {
            try
            {
                var reportFileName = $"conversion_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var reportFilePath = Path.Combine(outputDirectory, reportFileName);

                File.WriteAllText(reportFilePath, report);
                AppService.CaptureUiMessage($"Conversion report saved: {reportFileName}");

                return reportFilePath;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveConversionReport), ex);
                return null;
            }
        }
    }
}