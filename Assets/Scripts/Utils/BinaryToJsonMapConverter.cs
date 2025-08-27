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

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HammerAndSickle.Editor.Tools
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
            if (binaryFileToConvert == null)
            {
                Debug.Log("No file assigned. Please drag a .hsm file to the Binary File To Convert field.");
                return;
            }

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(binaryFileToConvert);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Could not get path for assigned file.");
                return;
            }

            if (!assetPath.EndsWith(".hsm", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"Selected file is not a .hsm file: {assetPath}");
                return;
            }

            Debug.Log($"Converting {Path.GetFileName(assetPath)} to JSON...");

            string fullPath = Path.GetFullPath(assetPath);
            string jsonPath = await ConvertBinaryMapToJsonAsync(fullPath);

            if (!string.IsNullOrEmpty(jsonPath))
            {
                Debug.Log($"✓ Conversion successful: {Path.GetFileName(jsonPath)}");
                AssetDatabase.Refresh();

                // Ping the created file
                string relativePath = "Assets" + jsonPath.Substring(Application.dataPath.Length);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                }
            }
            else
            {
                Debug.LogError("✗ Conversion failed");
            }
#else
            Debug.LogError("File conversion only available in Unity Editor");
#endif
        }
        #endregion

        #region Private Methods
        private async Task<string> ConvertBinaryMapToJsonAsync(string binaryFilePath)
        {
            try
            {
                // Load binary map data
                var binaryMapData = LoadBinaryMapFile(binaryFilePath);
                if (binaryMapData == null)
                {
                    return null;
                }

                // Validate binary data
                if (!binaryMapData.ValidateMapData())
                {
                    Debug.LogError($"Binary data validation failed: {Path.GetFileName(binaryFilePath)}");
                    return null;
                }

                // Convert to JSON format
                var jsonMapData = ConvertToJsonMapData(binaryMapData);
                if (jsonMapData == null)
                {
                    return null;
                }

                // Create output file path
                string fileName = Path.GetFileNameWithoutExtension(binaryFilePath);
                string jsonFilePath = Path.Combine(OUTPUT_DIRECTORY, $"{fileName}.json");

                // Save JSON file
                bool success = await SaveJsonMapFileAsync(jsonMapData, jsonFilePath);
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
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
                var formatter = new BinaryFormatter();
                var mapData = (SerializableMapData)formatter.Deserialize(fileStream);
#pragma warning restore SYSLIB0011

                Debug.Log($"Loaded binary data: {mapData?.Hexes?.Count ?? 0} hexes");
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
            try
            {
                if (binaryMapData?.Header == null || binaryMapData.Hexes == null)
                {
                    return null;
                }

                // Convert header
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
                    return null;
                }

                var jsonMapData = new JsonMapData
                {
                    Header = jsonHeader,
                    Hexes = jsonHexes
                };

                Debug.Log($"Converted to JSON format: {jsonHexes.Count} hexes");
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
            try
            {
                EnsureOutputDirectoryExists();

                var jsonString = JsonSerializer.Serialize(jsonMapData, JsonOptions);
                await File.WriteAllTextAsync(filePath, jsonString);

                var fileInfo = new FileInfo(filePath);
                Debug.Log($"Saved JSON file: {fileInfo.Length:N0} bytes");
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
                    Debug.Log($"Created output directory: {OUTPUT_DIRECTORY}");
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
    #endregion
}