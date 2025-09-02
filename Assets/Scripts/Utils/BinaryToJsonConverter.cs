using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using HammerAndSickle.Services;
using HammerAndSickle.Legacy.Map;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HammerAndSickle.Tools
{
    /// <summary>
    /// Unity tool for converting binary .hsm map files to JSON format.
    /// First iteration: loads binary data into temporary storage for validation.
    /// </summary>
    public class BinaryToJsonConverter : MonoBehaviour
    {
        #region Constants
        private const string CLASS_NAME = nameof(BinaryToJsonConverter);
        private const string HSM_EXTENSION = ".hsm";
        #endregion

        #region Inspector Fields
        [Header("File Selection")]
        [SerializeField]
        [Tooltip("Drag and drop the .hsm binary file here")]
        private UnityEngine.Object binaryMapFile = null;

        [Header("Loaded Data Status")]
        [SerializeField] private string loadedMapName = "";
        [SerializeField] private string mapDimensions = "";
        [SerializeField] private int hexCount = 0;
        [SerializeField] private string creationDate = "";
        [SerializeField] private string fileStatus = "No file selected";
        #endregion

        #region Private Fields
        private TemporaryMapData loadedMapData = null;
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts the selected binary file by loading it into temporary storage.
        /// </summary>
        public void ConvertBinaryFile()
        {
            Debug.Log($"{CLASS_NAME}: Starting conversion process");

            try
            {
                if (!ValidateSelectedFileForConversion())
                {
                    return;
                }

                string filePath = GetFilePath();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                Debug.Log($"{CLASS_NAME}: Attempting to load binary map data...");

                // Load binary data using original map editor logic with custom binder
                loadedMapData = LoadBinaryMapData(filePath);

                if (loadedMapData != null)
                {
                    Debug.Log($"{CLASS_NAME}: Binary data loaded successfully");
                    UpdateDisplayFields();
                    Debug.Log($"{CLASS_NAME}: Display fields updated");
                    AppService.CaptureUiMessage($"Successfully loaded map: {loadedMapData.Header.MapName}");
                    AppService.CaptureUiMessage($"Loaded {loadedMapData.Hexes?.Length ?? 0} hexes");
                }
                else
                {
                    Debug.LogError($"{CLASS_NAME}: Failed to load map data - LoadBinaryMapData returned null");
                    AppService.CaptureUiMessage("Failed to load map data");
                    fileStatus = "Failed to load data";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception during conversion: {ex.Message}");
                Debug.LogError($"{CLASS_NAME}: Stack trace: {ex.StackTrace}");
                AppService.HandleException(CLASS_NAME, nameof(ConvertBinaryFile), ex);
                AppService.CaptureUiMessage("Conversion failed: " + ex.Message);
                fileStatus = $"Error: {ex.Message}";
            }

            Debug.Log($"{CLASS_NAME}: Conversion process completed");
        }

        /// <summary>
        /// Validates if a file is selected (basic check for button state).
        /// </summary>
        public bool IsValidFileSelected()
        {
            return binaryMapFile != null;
        }
        #endregion

        #region Private Methods
        private bool ValidateSelectedFileForConversion()
        {
            if (binaryMapFile == null)
            {
                Debug.LogError($"{CLASS_NAME}: No binary file assigned to binaryMapFile field");
                AppService.CaptureUiMessage("No binary file selected");
                fileStatus = "No file selected";
                return false;
            }

            string assetPath = null;
#if UNITY_EDITOR
            assetPath = AssetDatabase.GetAssetPath(binaryMapFile);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"{CLASS_NAME}: Could not get asset path for file");
                AppService.CaptureUiMessage("Could not locate file in project");
                fileStatus = "Invalid file path";
                return false;
            }

            if (!assetPath.EndsWith(HSM_EXTENSION, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"{CLASS_NAME}: File must have {HSM_EXTENSION} extension. Current: {Path.GetExtension(assetPath)}");
                AppService.CaptureUiMessage($"File must have {HSM_EXTENSION} extension");
                fileStatus = $"Wrong extension: {Path.GetExtension(assetPath)}";
                return false;
            }

            string fileName = Path.GetFileName(assetPath);
            long fileSize = 0;
            string fullPath = Path.GetFullPath(assetPath);

            if (File.Exists(fullPath))
            {
                fileSize = new FileInfo(fullPath).Length;
                fileStatus = $"Converting: {fileName} ({fileSize:N0} bytes)";
            }
            else
            {
                fileStatus = $"File not found: {fileName}";
                return false;
            }
#else
            Debug.LogError($"{CLASS_NAME}: File conversion only available in editor");
            AppService.CaptureUiMessage("Conversion only available in editor");
            fileStatus = "Editor only feature";
            return false;
#endif

            return true;
        }

        private string GetFilePath()
        {
#if UNITY_EDITOR
            if (binaryMapFile == null) return null;

            string assetPath = AssetDatabase.GetAssetPath(binaryMapFile);
            if (string.IsNullOrEmpty(assetPath)) return null;

            string fullPath = Path.GetFullPath(assetPath);
            Debug.Log($"{CLASS_NAME}: Full file path: {fullPath}");

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"{CLASS_NAME}: File does not exist at path: {fullPath}");
                AppService.CaptureUiMessage("File not found");
                fileStatus = "File not found";
                return null;
            }

            return fullPath;
#else
            return null;
#endif
        }

        private TemporaryMapData LoadBinaryMapData(string filePath)
        {
            Debug.Log($"{CLASS_NAME}: LoadBinaryMapData called with path: {filePath}");

            try
            {
                Debug.Log($"{CLASS_NAME}: Opening file stream for reading");

                TemporaryMapData mapData;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Debug.Log($"{CLASS_NAME}: File stream opened successfully, stream length: {stream.Length} bytes");

                    Debug.Log($"{CLASS_NAME}: Creating BinaryFormatter with custom SerializationBinder");
                    BinaryFormatter formatter = new BinaryFormatter();

                    // Apply custom binder to handle cross-assembly type resolution
                    formatter.Binder = new MapDataSerializationBinder();

                    Debug.Log($"{CLASS_NAME}: Attempting deserialization with binder");
                    mapData = (TemporaryMapData)formatter.Deserialize(stream);

                    Debug.Log($"{CLASS_NAME}: Deserialization completed successfully");
                }

                Debug.Log($"{CLASS_NAME}: Validating loaded data");

                if (mapData?.Header == null)
                {
                    Debug.LogError($"{CLASS_NAME}: mapData or Header is null");
                    AppService.CaptureUiMessage("Map data or header is null");
                    return null;
                }

                if (mapData.Hexes == null)
                {
                    Debug.LogError($"{CLASS_NAME}: mapData.Hexes is null");
                    AppService.CaptureUiMessage("Map hexes array is null");
                    return null;
                }

                Debug.Log($"{CLASS_NAME}: Data validation passed");
                Debug.Log($"{CLASS_NAME}: Map name: {mapData.Header.MapName}");
                Debug.Log($"{CLASS_NAME}: Map dimensions: {mapData.Header.Width}x{mapData.Header.Height}");
                Debug.Log($"{CLASS_NAME}: Hex count: {mapData.Hexes.Length}");
                Debug.Log($"{CLASS_NAME}: Map configuration: {mapData.Header.MapConfiguration}");
                Debug.Log($"{CLASS_NAME}: Creation date: {mapData.Header.CreationDate}");

                return mapData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception in LoadBinaryMapData: {ex.Message}");
                Debug.LogError($"{CLASS_NAME}: Exception type: {ex.GetType().Name}");
                Debug.LogError($"{CLASS_NAME}: Stack trace: {ex.StackTrace}");
                AppService.HandleException(CLASS_NAME, nameof(LoadBinaryMapData), ex);
                throw;
            }
        }

        private void UpdateDisplayFields()
        {
            Debug.Log($"{CLASS_NAME}: Updating inspector display fields");

            try
            {
                if (loadedMapData?.Header != null)
                {
                    loadedMapName = loadedMapData.Header.MapName;
                    mapDimensions = $"{loadedMapData.Header.MapConfiguration} ({loadedMapData.Header.Width} x {loadedMapData.Header.Height})";
                    hexCount = loadedMapData.Hexes?.Length ?? 0;
                    creationDate = loadedMapData.Header.CreationDate.ToString("yyyy-MM-dd HH:mm");
                    fileStatus = "Data loaded successfully";

                    Debug.Log($"{CLASS_NAME}: Display fields updated successfully");
                    Debug.Log($"{CLASS_NAME}: - Map Name: {loadedMapName}");
                    Debug.Log($"{CLASS_NAME}: - Dimensions: {mapDimensions}");
                    Debug.Log($"{CLASS_NAME}: - Hex Count: {hexCount}");
                    Debug.Log($"{CLASS_NAME}: - Creation Date: {creationDate}");
                }
                else
                {
                    Debug.LogWarning($"{CLASS_NAME}: Cannot update display fields - loadedMapData or Header is null");
                    ClearDisplayFields();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception updating display fields: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(UpdateDisplayFields), ex);
            }
        }

        private void ClearDisplayFields()
        {
            Debug.Log($"{CLASS_NAME}: Clearing display fields");
            loadedMapName = "";
            mapDimensions = "";
            hexCount = 0;
            creationDate = "";
            fileStatus = "No file selected";
        }

        private void OnValidate()
        {
            // Update file status when file selection changes in inspector
            if (binaryMapFile == null)
            {
                ClearDisplayFields();
            }
            else
            {
#if UNITY_EDITOR
                string assetPath = AssetDatabase.GetAssetPath(binaryMapFile);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string fileName = Path.GetFileName(assetPath);
                    if (File.Exists(Path.GetFullPath(assetPath)))
                    {
                        long fileSize = new FileInfo(Path.GetFullPath(assetPath)).Length;
                        fileStatus = $"Selected: {fileName} ({fileSize:N0} bytes)";
                    }
                    else
                    {
                        fileStatus = $"File not found: {fileName}";
                    }
                }
                else
                {
                    fileStatus = "Invalid file selection";
                }
#endif
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Debug.Log($"{CLASS_NAME}: BinaryToJsonConverter initialized");

            try
            {
                AppService.CaptureUiMessage("Binary to JSON Converter ready");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception in Start: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(Start), ex);
            }
        }
        #endregion
    }

    /// <summary>
    /// Custom SerializationBinder to handle cross-assembly type resolution for map data deserialization.
    /// Maps original map editor types to temporary structures in current assembly.
    /// </summary>
    public class MapDataSerializationBinder : SerializationBinder
    {
        private const string CLASS_NAME = nameof(MapDataSerializationBinder);

        public override Type BindToType(string assemblyName, string typeName)
        {
            try
            {
                Debug.Log($"{CLASS_NAME}: Binding type: {typeName} from assembly: {assemblyName}");

                // Map original types to our temporary structures
                Type targetType = typeName switch
                {
                    "HammerAndSickle.Core.Services.SerializableMapData" => typeof(TemporaryMapData),
                    "HammerAndSickle.Core.Map.MapHeader" => typeof(TemporaryMapHeader),
                    "HammerAndSickle.Core.Services.SerializableHex" => typeof(TemporaryHex),

                    // Enum types should resolve to our Legacy.Map namespace
                    "HammerAndSickle.Core.Map.MapConfig" => typeof(MapConfig),
                    "HammerAndSickle.Core.Map.TerrainType" => typeof(TerrainType),
                    "HammerAndSickle.Core.Map.TextSize" => typeof(TextSize),
                    "HammerAndSickle.Core.Map.FontWeight" => typeof(FontWeight),
                    "HammerAndSickle.Core.Map.TextColor" => typeof(TextColor),
                    "HammerAndSickle.Core.Map.TileControl" => typeof(TileControl),
                    "HammerAndSickle.Core.Map.DefaultTileControl" => typeof(DefaultTileControl),

                    // Let other types resolve normally
                    _ => Type.GetType($"{typeName}, {assemblyName}")
                };

                if (targetType != null)
                {
                    Debug.Log($"{CLASS_NAME}: Successfully mapped {typeName} to {targetType.FullName}");
                }
                else
                {
                    Debug.LogWarning($"{CLASS_NAME}: Could not resolve type: {typeName}");
                }

                return targetType;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception in BindToType for {typeName}: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(BindToType), ex);

                // Fall back to default resolution
                return Type.GetType($"{typeName}, {assemblyName}");
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for BinaryToJsonConverter with Convert button.
    /// </summary>
    [CustomEditor(typeof(BinaryToJsonConverter))]
    public class BinaryToJsonConverterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            GUILayout.Space(10);

            BinaryToJsonConverter converter = (BinaryToJsonConverter)target;

            // Convert button - enabled if any file is selected (validation happens on click)
            GUI.enabled = converter.IsValidFileSelected();

            if (GUILayout.Button("Convert", GUILayout.Height(30)))
            {
                converter.ConvertBinaryFile();
            }

            GUI.enabled = true;

            // Status display
            if (!converter.IsValidFileSelected())
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Select any file to enable conversion (HSM validation happens on click)", MessageType.Info);
            }
        }
    }
#endif

    #region Temporary Data Structures
    /// <summary>
    /// Temporary map data structure for binary deserialization.
    /// Exact copy of the map editor's SerializableMapData structure.
    /// </summary>
    [Serializable]
    public class TemporaryMapData
    {
        public TemporaryMapHeader Header;
        public TemporaryHex[] Hexes;
    }

    /// <summary>
    /// Temporary map header for binary deserialization.
    /// Exact copy of the map editor's MapHeader structure.
    /// </summary>
    [Serializable]
    public class TemporaryMapHeader
    {
        public string MapName;
        public int Width;
        public int Height;
        public MapConfig MapConfiguration;
        public DateTime CreationDate;
        public DateTime LastModifiedDate;
        public string Description;
        public int Version = 1;
    }

    /// <summary>
    /// Temporary hex data structure for binary deserialization.
    /// Exact copy of the map editor's SerializableHex structure.
    /// </summary>
    [Serializable]
    public class TemporaryHex
    {
        // Position
        public int X;
        public int Y;

        // Basic properties
        public TerrainType Terrain;
        public bool IsRail;
        public bool IsRoad;
        public bool IsFort;
        public bool IsAirbase;
        public bool IsObjective;
        public bool IsVisible;
        public string TileLabel;
        public string LargeTileLabel;
        public TextSize LabelSize;
        public FontWeight LabelWeight;
        public TextColor LabelColor;
        public float LabelOutlineThickness;
        public float VictoryValue;
        public float AirbaseDamage;
        public int UrbanDamage;
        public TileControl TileControl;
        public DefaultTileControl DefaultTileControl;

        // Border data - 6-character binary strings
        public string RiverBorders;
        public string BridgeBorders;
        public string PontoonBridgeBorders;
        public string DamagedBridgeBorders;
    }
    #endregion
}