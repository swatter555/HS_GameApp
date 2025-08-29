using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics; // for Stopwatch
using UnityEngine;
using UnityEditor;

namespace HammerAndSickle.Utils
{
    /// <summary>
    /// Simple tool for converting .hsm files to JSON format.
    /// </summary>
    public class BinaryToJsonMapConverter : MonoBehaviour
    {
        #region Constants

        private const string CLASS_NAME = nameof(BinaryToJsonMapConverter);
        private const string OUTPUT_DIRECTORY = "Assets/Converted Files";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        #endregion

        #region Serialized Fields

        [Header("File Conversion")]
        [SerializeField] private UnityEngine.Object binaryFileToConvert;

        #endregion

        #region Unity Methods

        private void Start()
        {
            EnsureOutputDirectoryExists();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts the assigned .hsm file to JSON format.
        /// </summary>
        public async void ConvertFile()
        {
            
            UnityEngine.Debug.Log($"[{CLASS_NAME}] ConvertFile() invoked.");
            if (binaryFileToConvert == null)
            {
                UnityEngine.Debug.Log("No file assigned. Please drag a .hsm file to the Binary File To Convert field.");
                return;
            }

            #if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(binaryFileToConvert);
            UnityEngine.Debug.Log($"[{CLASS_NAME}] Resolved Asset path: '{assetPath}'");
            if (string.IsNullOrEmpty(assetPath))
            {
                UnityEngine.Debug.LogError("Could not get path for assigned file.");
                return;
            }

            if (!assetPath.EndsWith(".hsm", StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Debug.LogError($"Selected file is not a .hsm file: {assetPath}");
                return;
            }

            string fileName = Path.GetFileName(assetPath);
            UnityEngine.Debug.Log($"[{CLASS_NAME}] Preparing to convert '{fileName}' to JSON...");

            string fullPath = Path.GetFullPath(assetPath);
            UnityEngine.Debug.Log($"[{CLASS_NAME}] Full OS path: '{fullPath}'");

            var totalSw = Stopwatch.StartNew();
            string jsonPath = await ConvertBinaryMapToJsonAsync(fullPath);
            totalSw.Stop();
            UnityEngine.Debug.Log($"[{CLASS_NAME}] Total conversion time: {totalSw.ElapsedMilliseconds} ms");

            if (!string.IsNullOrEmpty(jsonPath))
            {
                UnityEngine.Debug.Log($"✓ Conversion successful: {Path.GetFileName(jsonPath)}");
                AssetDatabase.Refresh();

                // Ping the created file
                string relativePath = "Assets" + jsonPath.Substring(Application.dataPath.Length);
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Attempting to ping asset at: '{relativePath}'");
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    UnityEngine.Debug.Log($"[{CLASS_NAME}] Pinged created asset.");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[{CLASS_NAME}] Could not load asset for ping: '{relativePath}'");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("✗ Conversion failed");
            }
            #else
            Debug.LogError("File conversion only available in Unity Editor");
            #endif
        }

        #endregion

        #region Private Methods

        private async Task<string> ConvertBinaryMapToJsonAsync(string binaryFilePath)
        {
            UnityEngine.Debug.Log($"[{CLASS_NAME}] ConvertBinaryMapToJsonAsync() start. Source: '{binaryFilePath}'");
            try
            {
                // Load binary map data
                var loadSw = Stopwatch.StartNew();
                var binaryMapData = LoadBinaryMapFile(binaryFilePath);
                loadSw.Stop();
                UnityEngine.Debug.Log($"[{CLASS_NAME}] LoadBinaryMapFile() completed in {loadSw.ElapsedMilliseconds} ms");

                if (binaryMapData == null)
                {
                    UnityEngine.Debug.LogError($"[{CLASS_NAME}] LoadBinaryMapFile() returned null.");
                    return null;
                }

                // Validate binary data
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Validating binary data...");
                var validateSw = Stopwatch.StartNew();
                bool valid = binaryMapData.ValidateMapData();
                validateSw.Stop();
                UnityEngine.Debug.Log($"[{CLASS_NAME}] ValidateMapData() => {valid} in {validateSw.ElapsedMilliseconds} ms");
                if (!valid)
                {
                    UnityEngine.Debug.LogError($"Binary data validation failed: {Path.GetFileName(binaryFilePath)}");
                    return null;
                }

                // Convert to JSON format
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Converting to JSON structure...");
                var convertSw = Stopwatch.StartNew();
                var jsonMapData = ConvertToJsonMapData(binaryMapData);
                convertSw.Stop();
                UnityEngine.Debug.Log($"[{CLASS_NAME}] ConvertToJsonMapData() completed in {convertSw.ElapsedMilliseconds} ms");
                if (jsonMapData == null)
                {
                    UnityEngine.Debug.LogError($"[{CLASS_NAME}] ConvertToJsonMapData() returned null.");
                    return null;
                }

                // Get file name without extension
                string fileName = Path.GetFileNameWithoutExtension(binaryFilePath);
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Derived output base name: '{fileName}'");

                // Save pretty-printed JSON using .map extension
                string jsonFilePath = Path.Combine(OUTPUT_DIRECTORY, $"{fileName}.map");
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Target JSON path: '{jsonFilePath}'");

                // Save JSON file
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Writing JSON to disk...");
                var saveSw = Stopwatch.StartNew();
                bool success = await SaveJsonMapFileAsync(jsonMapData, jsonFilePath);
                saveSw.Stop();
                UnityEngine.Debug.Log($"[{CLASS_NAME}] SaveJsonMapFileAsync() => {success} in {saveSw.ElapsedMilliseconds} ms");

                return success ? jsonFilePath : null;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertBinaryMapToJsonAsync), ex);
                return null;
            }
        }

        private SerializableMapData LoadBinaryMapFile(string filePath)
        {
            UnityEngine.Debug.Log($"[{CLASS_NAME}] LoadBinaryMapFile() reading: '{filePath}'");
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                #pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
                var formatter = new BinaryFormatter();
                var mapData = (SerializableMapData)formatter.Deserialize(fileStream);
                #pragma warning restore SYSLIB0011

                int hexCount = mapData?.Hexes?.Length ?? 0;
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Loaded binary data. Hex count: {hexCount}");
                if (mapData?.Header != null)
                {
                    UnityEngine.Debug.Log($"[{CLASS_NAME}] Header: Name='{mapData.Header.MapName}', Size={mapData.Header.Width}x{mapData.Header.Height}, Version={mapData.Header.Version}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[{CLASS_NAME}] Header is null in loaded binary data.");
                }

                return mapData;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(LoadBinaryMapFile), ex);
                return null;
            }
        }

        private JsonMapData ConvertToJsonMapData(SerializableMapData binaryMapData)
        {
            UnityEngine.Debug.Log($"[{CLASS_NAME}] ConvertToJsonMapData() start.");
            try
            {
                if (binaryMapData?.Header == null || binaryMapData.Hexes == null)
                {
                    UnityEngine.Debug.LogError($"[{CLASS_NAME}] Input data invalid. Header null: {binaryMapData?.Header == null}, Hexes null: {binaryMapData?.Hexes == null}");
                    return null;
                }

                // Convert header - map SerializableMapHeader properties to JsonMapHeader
                var h = binaryMapData.Header;
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Mapping header fields...");
                var jsonHeader = new JsonMapHeader
                {
                    MapName = h.MapName,
                    MapConfiguration = h.MapConfiguration,
                    MapWidth = h.Width,            // Width -> MapWidth
                    MapHeight = h.Height,          // Height -> MapHeight
                    Version = h.Version,
                    CreatedDate = h.CreationDate,  // CreationDate -> CreatedDate
                    ConvertedDate = DateTime.UtcNow,
                    OriginalFormat = "Binary",
                    Description = h.Description
                };
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Header mapped. Name='{jsonHeader.MapName}', Size={jsonHeader.MapWidth}x{jsonHeader.MapHeight}, Version={jsonHeader.Version}");

                // Convert hex data
                UnityEngine.Debug.Log($"[{CLASS_NAME}] Converting hex array to JSON hex list...");
                var hexSw = Stopwatch.StartNew();
                var jsonHexes = binaryMapData.ConvertToJsonHexes();
                hexSw.Stop();
                int count = jsonHexes?.Count ?? 0;
                UnityEngine.Debug.Log($"[{CLASS_NAME}] ConvertToJsonHexes() produced {count} hexes in {hexSw.ElapsedMilliseconds} ms");
                if (jsonHexes == null || jsonHexes.Count == 0)
                {
                    UnityEngine.Debug.LogError($"[{CLASS_NAME}] No hexes produced during conversion.");
                    return null;
                }

                var jsonMapData = new JsonMapData
                {
                    Header = jsonHeader,
                    Hexes = jsonHexes
                };

                UnityEngine.Debug.Log($"[{CLASS_NAME}] Converted to JSON format: {jsonHexes.Count} hexes");
                return jsonMapData;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ConvertToJsonMapData), ex);
                return null;
            }
        }

        private async Task<bool> SaveJsonMapFileAsync(JsonMapData jsonMapData, string filePath)
        {
            UnityEngine.Debug.Log($"[{CLASS_NAME}] SaveJsonMapFileAsync() path: '{filePath}'");
            try
            {
                EnsureOutputDirectoryExists();

                var jsonString = JsonSerializer.Serialize(jsonMapData, JsonOptions);
                UnityEngine.Debug.Log($"[{CLASS_NAME}] JSON serialized. Length: {jsonString?.Length ?? 0} chars");

                await File.WriteAllTextAsync(filePath, jsonString);

                var fileInfo = new FileInfo(filePath);
                UnityEngine.Debug.Log($"Saved JSON file: {fileInfo.Length:N0} bytes at '{fileInfo.FullName}'");
                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SaveJsonMapFileAsync), ex);
                return false;
            }
        }

        private void EnsureOutputDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(OUTPUT_DIRECTORY))
                {
                    Directory.CreateDirectory(OUTPUT_DIRECTORY);
                    #if UNITY_EDITOR
                    AssetDatabase.Refresh();
                    #endif
                    UnityEngine.Debug.Log($"Created output directory: {OUTPUT_DIRECTORY}");
                }
                else
                {
                    UnityEngine.Debug.Log($"[{CLASS_NAME}] Output directory exists: {OUTPUT_DIRECTORY}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(EnsureOutputDirectoryExists), ex);
            }
        }

        #endregion
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Custom inspector for BinaryToJsonMapConverter to show the Convert File button.
    /// </summary>
    [CustomEditor(typeof(BinaryToJsonMapConverter))]
    public class BinaryToJsonMapConverterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            BinaryToJsonMapConverter converter = (BinaryToJsonMapConverter)target;

            if (GUILayout.Button("Convert File", GUILayout.Height(30)))
            {
                converter.ConvertFile();
            }
        }
    }
    #endif

    #region Data Classes
    /// <summary>
    /// JSON-compatible map data structure.
    /// </summary>
    [Serializable]
    public class JsonMapData
    {
        public JsonMapHeader Header { get; set; }
        public List<Hex> Hexes { get; set; }

        public JsonMapData()
        {
            Header = new JsonMapHeader();
            Hexes = new List<Hex>();
        }
    }

    /// <summary>
    /// JSON-compatible map header.
    /// </summary>
    [Serializable]
    public class JsonMapHeader
    {
        public string MapName { get; set; }
        public MapConfig MapConfiguration { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public int Version { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ConvertedDate { get; set; }
        public string OriginalFormat { get; set; }
        public string Description { get; set; }

        public JsonMapHeader()
        {
            MapName = string.Empty;
            MapConfiguration = MapConfig.None;
            MapWidth = 0;
            MapHeight = 0;
            Version = 1;
            CreatedDate = DateTime.MinValue;
            ConvertedDate = DateTime.UtcNow;
            OriginalFormat = "JSON";
            Description = string.Empty;
        }
    }
    #endregion
}
