using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using HammerAndSickle.Services;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Utils;
using System.Text.Json;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HammerAndSickle.Tools
{
    /// <summary>
    /// Unity tool for converting binary .hsm map files to JSON format.
    /// Loads binary data and converts to JSON-compliant GameHex structures.
    /// </summary>
    public class BinaryToJsonConverter : MonoBehaviour
    {
        #region Constants

        private const string CLASS_NAME = nameof(BinaryToJsonConverter);
        private const string HSM_EXTENSION = ".hsm";
        private const string MAP_EXTENSION = ".map";
        private static readonly char[] INVALID_FILENAME_CHARS = Path.GetInvalidFileNameChars();
        private static readonly string CONVERTED_FILES_FOLDER = Path.Combine(Application.dataPath, "Converted Files");

        #endregion // Constants

        #region Inspector Fields

        [Header("File Selection")]
        [Tooltip("Drag and drop the .hsm binary file here")]
        public UnityEngine.Object binaryMapFile = null;

        [Header("Output Configuration")]
        [Tooltip("Output filename for the .map file (without extension)")]
        public string outputFileName = "";

        [Header("Loaded Data Status")]
        [SerializeField] private string loadedMapName = "";
        [SerializeField] private string mapDimensions = "";
        [SerializeField] private int hexCount = 0;
        [SerializeField] private string creationDate = "";
        [SerializeField] private string fileStatus = "No file selected";

        #endregion // Inspector Fields

        #region Private Fields

        private TemporaryMapData loadedMapData = null;

        #endregion // Private Fields

        #region Public Methods

        /// <summary>
        /// Converts the selected binary file to JSON format.
        /// Complete workflow: Load binary → Convert to JSON → Write file.
        /// </summary>
        public void ConvertBinaryFile()
        {
            Debug.Log($"{CLASS_NAME}: Starting complete conversion process");

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

                // Phase 1: Load binary data
                Debug.Log($"{CLASS_NAME}: Phase 1 - Loading binary map data...");
                loadedMapData = LoadBinaryMapData(filePath);

                if (loadedMapData == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Failed to load map data - LoadBinaryMapData returned null");
                    AppService.CaptureUiMessage("Failed to load map data");
                    fileStatus = "Failed to load data";
                    return;
                }

                Debug.Log($"{CLASS_NAME}: Phase 1 complete - Binary data loaded successfully");
                UpdateDisplayFields();
                AppService.CaptureUiMessage($"Loaded map: {loadedMapData.Header.MapName} ({loadedMapData.Hexes?.Length ?? 0} hexes)");

                // Phase 2: Convert to JSON format
                Debug.Log($"{CLASS_NAME}: Phase 2 - Converting to JSON format...");
                JsonMapData jsonMapData = ConvertToJsonMapData(loadedMapData);

                if (jsonMapData == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Failed to convert to JSON format");
                    AppService.CaptureUiMessage("Failed to convert map data to JSON format");
                    fileStatus = "JSON conversion failed";
                    return;
                }

                Debug.Log($"{CLASS_NAME}: Phase 2 complete - JSON conversion successful");
                AppService.CaptureUiMessage("Map data converted to JSON format");

                // Phase 3: Write JSON file
                Debug.Log($"{CLASS_NAME}: Phase 3 - Writing JSON file...");
                bool writeSuccess = WriteJsonMapFile(jsonMapData);

                if (writeSuccess)
                {
                    Debug.Log($"{CLASS_NAME}: Phase 3 complete - File written successfully");
                    AppService.CaptureUiMessage($"Conversion complete! File saved as: {outputFileName}{MAP_EXTENSION}");
                    fileStatus = $"✅ Converted: {outputFileName}{MAP_EXTENSION}";
                }
                else
                {
                    Debug.LogError($"{CLASS_NAME}: Failed to write JSON file");
                    AppService.CaptureUiMessage("Failed to write JSON file");
                    fileStatus = "File write failed";
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

            Debug.Log($"{CLASS_NAME}: Complete conversion process finished");
        }

        /// <summary>
        /// Validates if both file and filename are properly selected and valid.
        /// </summary>
        public bool IsValidFileSelected()
        {
            return binaryMapFile != null && IsValidOutputFileName(outputFileName);
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Validates the selected binary file and output file name for conversion.
        /// </summary>
        private bool ValidateSelectedFileForConversion()
        {
            if (binaryMapFile == null)
            {
                Debug.LogError($"{CLASS_NAME}: No binary file assigned to binaryMapFile field");
                AppService.CaptureUiMessage("No binary file selected");
                fileStatus = "No file selected";
                return false;
            }

            if (!IsValidOutputFileName(outputFileName))
            {
                Debug.LogError($"{CLASS_NAME}: Invalid output filename: '{outputFileName}'");
                AppService.CaptureUiMessage("Invalid or missing output filename");
                fileStatus = "Invalid output filename";
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
                fileStatus = $"Ready: {fileName} → {outputFileName}{MAP_EXTENSION}";
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

        /// <summary>
        /// Converts TemporaryMapData to JsonMapData format.
        /// </summary>
        private JsonMapData ConvertToJsonMapData(TemporaryMapData tempData)
        {
            try
            {
                Debug.Log($"{CLASS_NAME}: Converting temporary map data to JSON format");

                if (tempData?.Header == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Temporary data or header is null");
                    return null;
                }

                if (tempData.Hexes == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Temporary hex array is null");
                    return null;
                }

                Debug.Log($"{CLASS_NAME}: Converting {tempData.Hexes.Length} hexes");

                // Handle null or empty map name - use filename as fallback
                string mapName = string.IsNullOrWhiteSpace(tempData.Header.MapName)
                    ? GetFallbackMapName()
                    : tempData.Header.MapName;

                Debug.Log($"{CLASS_NAME}: Using map name: '{mapName}'");

                // Convert header
                JsonMapHeader jsonHeader = new(
                    mapName,
                    tempData.Header.MapConfiguration,
                    "placeholder" // Checksum will be calculated after hex conversion
                );

                // Convert hex array
                GameHex[] gameHexes = new GameHex[tempData.Hexes.Length];
                for (int i = 0; i < tempData.Hexes.Length; i++)
                {
                    gameHexes[i] = ConvertTemporaryHexToGameHex(tempData.Hexes[i]);
                    if (gameHexes[i] == null)
                    {
                        Debug.LogError($"{CLASS_NAME}: Failed to convert hex at index {i}");
                        AppService.CaptureUiMessage($"Failed to convert hex at index {i}");
                        return null;
                    }
                }

                Debug.Log($"{CLASS_NAME}: Successfully converted all hexes");

                // Create JSON map data
                JsonMapData jsonMapData = new(jsonHeader, gameHexes);

                // Calculate and update checksum
                Debug.Log($"{CLASS_NAME}: Calculating checksum for converted data");
                bool checksumSuccess = MapChecksumUtility.UpdateChecksum(jsonMapData);

                if (!checksumSuccess)
                {
                    Debug.LogError($"{CLASS_NAME}: Failed to calculate checksum");
                    AppService.CaptureUiMessage("Failed to calculate data checksum");
                    return null;
                }

                // Validate final result
                if (!jsonMapData.IsValid())
                {
                    Debug.LogError($"{CLASS_NAME}: Converted JSON map data failed validation");
                    AppService.CaptureUiMessage("Converted map data failed validation");
                    return null;
                }

                Debug.Log($"{CLASS_NAME}: JSON map data conversion completed successfully");
                return jsonMapData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception in ConvertToJsonMapData: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(ConvertToJsonMapData), ex);
                return null;
            }
        }

        /// <summary>
        /// Generates a fallback map name when the original is null or empty.
        /// Uses the input filename without extension as the map name.
        /// </summary>
        /// <returns>Fallback map name</returns>
        private string GetFallbackMapName()
        {
            try
            {
                if (binaryMapFile != null)
                {
                    #if UNITY_EDITOR
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(binaryMapFile);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(assetPath);
                        Debug.Log($"{CLASS_NAME}: Using filename as fallback map name: '{fileName}'");
                        return fileName;
                    }
                    #endif
                }

                // Final fallback
                string fallback = "Unnamed Map";
                Debug.Log($"{CLASS_NAME}: Using final fallback map name: '{fallback}'");
                return fallback;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetFallbackMapName), ex);
                return "Unnamed Map";
            }
        }

        /// <summary>
        /// Converts a TemporaryHex to a GameHex.
        /// </summary>
        private GameHex ConvertTemporaryHexToGameHex(TemporaryHex tempHex)
        {
            try
            {
                if (tempHex == null)
                {
                    Debug.LogError($"{CLASS_NAME}: Temporary hex is null");
                    return null;
                }

                // Create GameHex with position conversion
                Vector2Int position = new(tempHex.X, tempHex.Y);
                GameHex gameHex = new(position);

                // Copy basic properties
                gameHex.SetTerrain(tempHex.Terrain);
                gameHex.SetIsRail(tempHex.IsRail);
                gameHex.SetIsRoad(tempHex.IsRoad);
                gameHex.SetIsFort(tempHex.IsFort);
                gameHex.SetIsAirbase(tempHex.IsAirbase);
                gameHex.SetIsObjective(tempHex.IsObjective);
                gameHex.SetIsVisible(tempHex.IsVisible);

                // Copy label and display properties
                gameHex.TileLabel = tempHex.TileLabel ?? string.Empty;
                gameHex.LargeTileLabel = tempHex.LargeTileLabel ?? string.Empty;
                gameHex.LabelSize = tempHex.LabelSize;
                gameHex.LabelWeight = tempHex.LabelWeight;
                gameHex.LabelColor = tempHex.LabelColor;
                gameHex.LabelOutlineThickness = tempHex.LabelOutlineThickness;

                // Copy game state properties
                gameHex.VictoryValue = tempHex.VictoryValue;
                gameHex.AirbaseDamage = tempHex.AirbaseDamage;
                gameHex.UrbanDamage = tempHex.UrbanDamage;
                gameHex.TileControl = tempHex.TileControl;
                gameHex.DefaultTileControl = tempHex.DefaultTileControl;

                // Convert border strings to JSONFeatureBorders
                gameHex.RiverBorders = new JSONFeatureBorders(tempHex.RiverBorders, BorderType.River);
                gameHex.BridgeBorders = new JSONFeatureBorders(tempHex.BridgeBorders, BorderType.Bridge);
                gameHex.PontoonBridgeBorders = new JSONFeatureBorders(tempHex.PontoonBridgeBorders, BorderType.PontoonBridge);
                gameHex.DamagedBridgeBorders = new JSONFeatureBorders(tempHex.DamagedBridgeBorders, BorderType.DestroyedBridge);

                return gameHex;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception converting hex at ({tempHex?.X}, {tempHex?.Y}): {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(ConvertTemporaryHexToGameHex), ex);
                return null;
            }
        }

        /// <summary>
        /// Writes the provided JSON map data to a file in the specified output directory.
        /// </summary>
        private bool WriteJsonMapFile(JsonMapData jsonMapData)
        {
            try
            {
                Debug.Log($"{CLASS_NAME}: Writing JSON map file");

                if (jsonMapData == null)
                {
                    Debug.LogError($"{CLASS_NAME}: JsonMapData is null");
                    return false;
                }

                // Ensure output directory exists
                string outputDirectory = CONVERTED_FILES_FOLDER;
                if (!Directory.Exists(outputDirectory))
                {
                    Debug.Log($"{CLASS_NAME}: Creating output directory: {outputDirectory}");
                    Directory.CreateDirectory(outputDirectory);

                    #if UNITY_EDITOR
                    UnityEditor.AssetDatabase.Refresh();
                    #endif
                }

                // Create full output path
                string outputPath = Path.Combine(outputDirectory, $"{outputFileName}{MAP_EXTENSION}");
                Debug.Log($"{CLASS_NAME}: Writing to: {outputPath}");

                // Configure JSON serialization options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // Pretty-printed JSON for readability
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };

                // Serialize and write file
                string jsonString = JsonSerializer.Serialize(jsonMapData, options);
                File.WriteAllText(outputPath, jsonString);

                Debug.Log($"{CLASS_NAME}: JSON file written successfully");
                Debug.Log($"{CLASS_NAME}: File size: {new FileInfo(outputPath).Length:N0} bytes");

                #if UNITY_EDITOR
                // Refresh Unity's asset database to show the new file
                UnityEditor.AssetDatabase.Refresh();
                #endif

                // Validate written file by attempting to read it back
                Debug.Log($"{CLASS_NAME}: Validating written file");
                string readBack = File.ReadAllText(outputPath);
                JsonMapData validationData = JsonSerializer.Deserialize<JsonMapData>(readBack, options);

                if (validationData?.IsValid() == true)
                {
                    Debug.Log($"{CLASS_NAME}: File validation successful");

                    // Verify checksum integrity
                    bool checksumValid = MapChecksumUtility.ValidateChecksum(validationData);
                    if (checksumValid)
                    {
                        Debug.Log($"{CLASS_NAME}: Checksum validation passed");
                        AppService.CaptureUiMessage($"File written and validated: {outputFileName}{MAP_EXTENSION}");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"{CLASS_NAME}: Checksum validation failed but file is readable");
                        AppService.CaptureUiMessage($"File written but checksum validation failed: {outputFileName}{MAP_EXTENSION}");
                        return true; // Still consider success since file is readable
                    }
                }
                else
                {
                    Debug.LogError($"{CLASS_NAME}: File validation failed - written file is not readable");
                    AppService.CaptureUiMessage("File was written but failed validation");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{CLASS_NAME}: Exception writing JSON file: {ex.Message}");
                AppService.HandleException(CLASS_NAME, nameof(WriteJsonMapFile), ex);
                return false;
            }
        }

        /// <summary>
        /// Determines whether the specified filename is valid for use as an output file name.
        /// </summary>
        /// <remarks>This method checks for invalid characters based on a predefined set of restricted
        /// characters. It also ensures that the file name does not begin or end with problematic characters such as
        /// whitespace or a period, which may cause issues in certain file systems.</remarks>
        private bool IsValidOutputFileName(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return false;
            }

            // Check for invalid filename characters
            if (filename.Any(c => INVALID_FILENAME_CHARS.Contains(c)))
            {
                return false;
            }

            // Additional checks for problematic filenames
            if (filename.StartsWith(" ") || filename.EndsWith(" "))
            {
                return false;
            }

            if (filename.StartsWith(".") || filename.EndsWith("."))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the full file path of the binary map file in the Unity Editor.
        /// </summary>
        /// <remarks>This method is only functional in the Unity Editor. If the binary map file is not
        /// set,  or if the file does not exist at the resolved path, the method returns <see langword="null"/>.
        /// Additionally, if the file is not found, an error message is logged, and the file status is
        /// updated.</remarks>
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

        /// <summary>
        /// Loads and deserializes the binary map data from the specified file path.
        /// </summary>
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
                    BinaryFormatter formatter = new()
                    {
                        // Apply custom binder to handle cross-assembly type resolution
                        Binder = new MapDataSerializationBinder()
                    };

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

        /// <summary>
        /// Updates the display fields in the inspector to reflect the current state of the loaded map data.
        /// </summary>
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
                    fileStatus = $"Ready: {Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(binaryMapFile))}{HSM_EXTENSION} → {outputFileName}{MAP_EXTENSION}";

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

        /// <summary>
        /// Clears the display fields in the inspector.
        /// </summary>
        private void ClearDisplayFields()
        {
            Debug.Log($"{CLASS_NAME}: Clearing display fields");
            loadedMapName = "";
            mapDimensions = "";
            hexCount = 0;
            creationDate = "";
            fileStatus = "No file selected";
        }

        /// <summary>
        /// Validates the current state of the file selection and updates the file status accordingly.
        /// </summary>
        /// <remarks>This method is called automatically by Unity when changes are made to the serialized
        /// fields in the Inspector. It ensures that the file selection and output filename are valid, and updates the
        /// <c>fileStatus</c> field to reflect the current state. The validation includes checking if the selected file
        /// exists, verifying the output filename, and providing appropriate status messages.</remarks>
        private void OnValidate()
        {
            // Update file status when file selection or filename changes in inspector
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

                    if (!File.Exists(Path.GetFullPath(assetPath)))
                    {
                        fileStatus = $"File not found: {fileName}";
                    }
                    else if (string.IsNullOrWhiteSpace(outputFileName))
                    {
                        long fileSize = new FileInfo(Path.GetFullPath(assetPath)).Length;
                        fileStatus = $"Selected: {fileName} ({fileSize:N0} bytes) - Output filename required";
                    }
                    else if (!IsValidOutputFileName(outputFileName))
                    {
                        fileStatus = $"Selected: {fileName} - Invalid filename characters";
                    }
                    else
                    {
                        long fileSize = new FileInfo(Path.GetFullPath(assetPath)).Length;
                        fileStatus = $"Ready: {fileName} → {outputFileName}{MAP_EXTENSION}";
                    }
                }
                else
                {
                    fileStatus = "Invalid file selection";
                }
                #endif
            }
        }

        #endregion // Private Methods

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

        #endregion // Unity Lifecycle
    }

    /// <summary>
    /// Custom SerializationBinder to handle cross-assembly type resolution for map data deserialization.
    /// Maps original map editor types to temporary structures in current assembly.
    /// </summary>
    public class MapDataSerializationBinder : SerializationBinder
    {
        // Class name for logging
        private const string CLASS_NAME = nameof(MapDataSerializationBinder);

        /// <summary>
        /// Resolves a type based on its assembly and type name, with custom mappings for specific types.
        /// </summary>
        /// <remarks>This method provides custom mappings for specific types, redirecting them to
        /// alternative types  as defined in the method. If the type is not explicitly mapped, it attempts to resolve
        /// the type  using the default resolution mechanism. <para> If an exception occurs during resolution, the
        /// method logs the error and falls back to the default  resolution mechanism. </para></remarks>
        /// <param name="assemblyName">The name of the assembly containing the type to resolve.</param>
        /// <param name="typeName">The fully qualified name of the type to resolve.</param>
        /// <returns>The resolved <see cref="Type"/> corresponding to the specified <paramref name="typeName"/> and  <paramref
        /// name="assemblyName"/>. Returns <see langword="null"/> if the type cannot be resolved.</returns>
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
    /// Custom editor for BinaryToJsonConverter with Convert button and enhanced validation feedback.
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

            // Convert button - enabled only when both file and filename are valid
            GUI.enabled = converter.IsValidFileSelected();

            if (GUILayout.Button("Convert", GUILayout.Height(30)))
            {
                converter.ConvertBinaryFile();
            }

            GUI.enabled = true;

            // Status display with progressive help
            GUILayout.Space(5);

            if (converter.binaryMapFile == null)
            {
                EditorGUILayout.HelpBox("Select a .hsm file to begin conversion", MessageType.Info);
            }
            else if (string.IsNullOrWhiteSpace(converter.outputFileName))
            {
                EditorGUILayout.HelpBox("Enter an output filename to enable conversion", MessageType.Warning);
            }
            else if (!IsValidFilename(converter.outputFileName))
            {
                EditorGUILayout.HelpBox("Check filename for invalid characters", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Ready to convert! Click the Convert button.", MessageType.None);
            }
        }

        private bool IsValidFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (filename.Any(c => invalidChars.Contains(c))) return false;

            if (filename.StartsWith(" ") || filename.EndsWith(" ")) return false;
            if (filename.StartsWith(".") || filename.EndsWith(".")) return false;

            return true;
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

    #endregion // Temporary Data Structures
}