using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using static UnityEngine.UI.CanvasScaler;

namespace HammerAndSickle.Core.Map
{
    /// <summary>
    /// Manages the rendering and visual representation of the hex-based map system.
    /// Handles tile placement, outline colors, and map updates while maintaining
    /// a structured organization through region-based separation.
    /// </summary>
    public class HexMapRenderer : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(HexMapRenderer);

        #region Singleton

        /// <summary>
        /// Singleton instance of the renderer. This property ensures only one instance
        /// exists in the scene. Access this instance through HexMapRenderer.Instance.
        /// </summary>
        public static HexMapRenderer Instance { get; private set; }

        #endregion // Singleton

        #region Fields

        /// <summary>
        /// Dictionary to store and track city prefab instances by their hex coordinates.
        /// </summary>
        private readonly Dictionary<Vector2Int, Prefab_CityIcon> cityPrefabs = new();

        /// <summary>
        /// Dictionary to store and track bridge prefab instances by their unique keys.
        /// </summary>
        private readonly Dictionary<string, Prefab_BridgeIcon> bridgePrefabs = new();

        /// <summary>
        /// Dictionary to store and track map icon prefab instances by their hex coordinates.
        /// </summary>
        private readonly Dictionary<Vector2Int, Prefab_MapIcon> mapIconPrefabs = new();

        /// <summary>
        /// Dictionary to store and track text label prefab instances by their hex coordinates.
        /// </summary>
        private readonly Dictionary<Vector2Int, Prefab_MapText> textLabelPrefabs = new();

        /// <summary>
        /// Dictionary to store and track unit icon prefab instances by their unit ID.
        /// </summary>
        private readonly Dictionary<string, Prefab_CombatUnitIcon> unitIconPrefabs = new();

        /// <summary>
        /// Controls whether map labels are rendered on the map.
        /// </summary>
        private bool isRenderMapLabels = true;

        /// <summary>
        /// Enables detailed debug logging throughout the renderer.
        /// </summary>
        [SerializeField] private bool _debug = false;

        #endregion // Fields

        #region Inspector Fields

        [Header("Rendering Tilemaps")]
        [SerializeField] private Tilemap hexOutlineTilemap;
        [SerializeField] private Tilemap hexSelectionTilemap;

        [Header("Rendering Layers")]
        [SerializeField] private GameObject mapIconLayer;
        [SerializeField] private GameObject bridgeIconLayer;
        [SerializeField] private GameObject cityIconLayer;
        [SerializeField] private GameObject textLabelLayer;
        [SerializeField] private GameObject mainUnitLayer;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Indicates if the renderer is properly initialized. This flag is set after
        /// successful validation of required components.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the GameObject reference for the text label layer.
        /// </summary>
        public GameObject TextLabelLayer => textLabelLayer;

        /// <summary>
        /// Gets or sets whether map labels are rendered. Setting this value refreshes the map.
        /// </summary>
        public bool IsRenderMapLabels
        {
            get => isRenderMapLabels;
            set
            {
                if (isRenderMapLabels != value)
                {
                    isRenderMapLabels = value;
                    RefreshMap();
                }
            }
        }

        #endregion // Properties

        #region Unity Lifecycle

        /// <summary>
        /// Unity's Awake method. Handles singleton instance management and validates
        /// required components. Any duplicate instances will be destroyed.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeService();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity's Start method. Verifies initialization and starts service operations.
        /// If initialization failed, logs error and prevents service startup.
        /// </summary>
        private void Start()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"{CLASS_NAME}.Start: Service failed to initialize properly.");
                return;
            }

            SubscribeToEvents();
        }

        /// <summary>
        /// Unity's OnDestroy method. Handles cleanup and unsubscription.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion // Unity Lifecycle

        #region Initialization

        /// <summary>
        /// Initializes the renderer service. Validates required components and
        /// sets up initial state. Called during Awake for the singleton instance.
        /// </summary>
        private void InitializeService()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.InitializeService] Starting initialization...");

                ValidateComponents();
                IsInitialized = true;

                if (_debug) Debug.Log($"[{CLASS_NAME}.InitializeService] Successfully initialized.");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, "InitializeService", ex);
                IsInitialized = false;

                if (_debug) Debug.Log($"[{CLASS_NAME}.InitializeService] Initialization failed.");
            }
        }

        /// <summary>
        /// Validates that all required components are properly referenced.
        /// Throws exceptions if any required components are missing.
        /// </summary>
        private void ValidateComponents()
        {
            if (_debug) Debug.Log($"[{CLASS_NAME}.ValidateComponents] Validating required components...");

            if (hexOutlineTilemap == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(hexOutlineTilemap)} is missing.");

            if (hexSelectionTilemap == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(hexSelectionTilemap)} is missing.");

            if (mapIconLayer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(mapIconLayer)} is missing.");

            if (cityIconLayer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(cityIconLayer)} is missing.");

            if (bridgeIconLayer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(bridgeIconLayer)} is missing.");

            if (_debug) Debug.Log($"[{CLASS_NAME}.ValidateComponents] All required components validated successfully.");
        }

        #endregion // Initialization

        #region Event Management

        /// <summary>
        /// Subscribes to hex selection events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (HexDetectionService.Instance != null)
            {
                HexDetectionService.Instance.OnHexSelected += HandleHexSelected;

                if (_debug) Debug.Log($"[{CLASS_NAME}.SubscribeToEvents] Successfully subscribed to HexDetectionService events.");
            }
            else
            {
                Debug.LogWarning($"{CLASS_NAME}: HexDetectionService instance not found. Hex selection events will not be handled.");
            }
        }

        /// <summary>
        /// Unsubscribes from hex selection events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (HexDetectionService.Instance != null)
            {
                HexDetectionService.Instance.OnHexSelected -= HandleHexSelected;

                if (_debug) Debug.Log($"[{CLASS_NAME}.UnsubscribeFromEvents] Unsubscribed from HexDetectionService events.");
            }
        }

        /// <summary>
        /// Handles hex selection change events.
        /// </summary>
        private void HandleHexSelected(Position2D hexPosition)
        {
            try
            {
                // Log the selected hex position
                if (_debug) Debug.Log($"[{CLASS_NAME}.HandleHexSelected] Hex selected at position: ({hexPosition.IntX}, {hexPosition.IntY})");

                // Validate map data exists
                if (GameDataManager.CurrentHexMap != null)
                {
                    // Validate hex is selected
                    if (hexPosition != GameDataManager.NoHexSelected)
                    {
                        // Get the selected hex tile
                        HexTile selectedHex = GameDataManager.CurrentHexMap.GetHexAt(hexPosition);
                        if (selectedHex != null)
                        {
                            if (_debug) Debug.Log($"Hex data selected at position: ({hexPosition.IntX}, {hexPosition.IntY})");
                            if (_debug) Debug.Log($"Terrain: {selectedHex.Terrain}, IsCity: {(selectedHex.Terrain == TerrainType.MajorCity || selectedHex.Terrain == TerrainType.MinorCity)}, IsAirbase: {selectedHex.IsAirbase}, IsFort: {selectedHex.IsFort}");

                            // Assign the selected hex in game data manager
                            GameDataManager.SelectedHexData = selectedHex;
                        }
                        else
                        {
                            if (_debug) Debug.LogWarning($"[{CLASS_NAME}.HandleHexSelected] No valid hex found at selected position.");
                        }
                    }
                    else
                    {
                        if (_debug) Debug.Log($"[{CLASS_NAME}.HandleHexSelected] No hex is currently selected.");
                    }
                }

                // Draw the map indicator of the selected hex
                DrawHexSelector();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleHexSelected), e);
            }
        }

        #endregion // Event Management

        #region Public Methods

        /// <summary>
        /// Gets the world position of a hex tile based on its grid coordinates.
        /// </summary>
        public UnityEngine.Vector3 GetRenderPosition(Vector2Int gridPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"{CLASS_NAME}.GetRenderPosition: Service not initialized.");

            return hexOutlineTilemap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
        }

        /// <summary>
        /// Refreshes the entire map display, redrawing all elements.
        /// </summary>
        public void RefreshMap()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Starting map refresh...");

                // PrepareBattle that we have a map to render
                if (GameDataManager.CurrentHexMap == null)
                {
                    AppService.CaptureUiMessage("Cannot refresh map: No hex map loaded.");
                    if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Aborted: No hex map loaded.");
                    return;
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Clearing existing prefab dictionaries and visual elements...");

                // Clear tracking dictionaries
                cityPrefabs.Clear();
                bridgePrefabs.Clear();
                mapIconPrefabs.Clear();
                textLabelPrefabs.Clear();
                unitIconPrefabs.Clear();

                // Clear existing visual elements
                ClearContainer(mapIconLayer.transform);
                ClearContainer(bridgeIconLayer.transform);
                ClearContainer(cityIconLayer.transform);
                ClearContainer(textLabelLayer.transform);
                ClearContainer(mainUnitLayer.transform);

                // Draw the hex outlines
                DrawHexOutlines();

                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Iterating through hexes to render features...");

                // Counters for summary
                int hexCount = 0;
                int cityCount = 0;
                int mapIconCount = 0;
                int bridgeCount = 0;
                int labelCount = 0;
                int unitCount = 0;

                // Iterate through all hexes and render their features
                foreach (var hex in GameDataManager.CurrentHexMap)
                {
                    if (hex == null) continue;

                    hexCount++;

                    // Draw map icons (airbases, forts)
                    int prevMapIconCount = mapIconPrefabs.Count;
                    DrawMapIconsForHex(hex);
                    if (mapIconPrefabs.Count > prevMapIconCount) mapIconCount++;

                    // Draw city icons
                    int prevCityCount = cityPrefabs.Count;
                    DrawCityIconForHex(hex);
                    if (cityPrefabs.Count > prevCityCount) cityCount++;

                    // Draw bridges (only for E, SE, SW to avoid duplicates)
                    int prevBridgeCount = bridgePrefabs.Count;
                    DrawBridgesForHex(hex);
                    bridgeCount += (bridgePrefabs.Count - prevBridgeCount);

                    // Draw text labels if enabled
                    if (isRenderMapLabels)
                    {
                        int prevLabelCount = textLabelPrefabs.Count;
                        DrawTextLabelsForHex(hex);
                        if (textLabelPrefabs.Count > prevLabelCount) labelCount++;
                    }
                }

                // Draw all combat units on the map
                // TODO: Get units from GameDataManager - need to verify how units are stored/accessed
                DrawAllUnits(ref unitCount);

                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Map refresh complete. Processed {hexCount} hexes. Created {cityCount} cities, {mapIconCount} map icons, {bridgeCount} bridges, {labelCount} labels, {unitCount} units.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RefreshMap", e);
            }
        }

        /// <summary>
        /// Draws the hex selector outline on the map.
        /// </summary>
        public void DrawHexSelector()
        {
            try
            {
                // Clear existing hex selector
                hexSelectionTilemap.ClearAllTiles();

                // Check if a hex is selected
                Position2D selectedHex = GameDataManager.SelectedHex;
                if (selectedHex != GameDataManager.NoHexSelected)
                {
                    if (_debug) Debug.Log($"[{CLASS_NAME}.DrawHexSelector] Drawing selector at ({selectedHex.IntX}, {selectedHex.IntY})");

                    // Create and configure the tile
                    Tile tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = SpriteManager.GetSprite(SpriteManager.HexSelectOutline);

                    // Set the tile at the position
                    hexSelectionTilemap.SetTile(new Vector3Int(selectedHex.IntX, selectedHex.IntY, 0), tile);
                }
                else
                {
                    if (_debug) Debug.Log($"[{CLASS_NAME}.DrawHexSelector] Cleared selector - no hex selected.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawHexSelector", e);
            }
        }

        /// <summary>
        /// Changes the control flag for a city at the specified position based on current tile control.
        /// </summary>
        public void ChangeControlFlag(Position2D position)
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.ChangeControlFlag] Attempting to change control flag at position ({position.IntX}, {position.IntY})");

                // PrepareBattle map exists
                if (GameDataManager.CurrentHexMap == null)
                {
                    AppService.CaptureUiMessage("Cannot change control flag: No hex map loaded.");
                    if (_debug) Debug.Log($"[{CLASS_NAME}.ChangeControlFlag] Failed: No hex map loaded.");
                    return;
                }

                // Get the hex at the position
                HexTile hex = GameDataManager.CurrentHexMap.GetHexAt(position);
                if (hex == null)
                {
                    AppService.CaptureUiMessage($"Cannot change control flag: No hex found at position {position}.");
                    if (_debug) Debug.Log($"[{CLASS_NAME}.ChangeControlFlag] Failed: No hex found at position.");
                    return;
                }

                // Check if there's a city prefab at this position
                Vector2Int pos = new(position.IntX, position.IntY);
                if (!cityPrefabs.TryGetValue(pos, out Prefab_CityIcon cityPrefab))
                {
                    AppService.CaptureUiMessage($"Cannot change control flag: No city found at position {position}.");
                    if (_debug) Debug.Log($"[{CLASS_NAME}.ChangeControlFlag] Failed: No city prefab found at position.");
                    return;
                }

                // Update the control flag based on current tile control
                cityPrefab.UpdateControlFlag(hex.TileControl, hex.DefaultTileControl);

                if (_debug) Debug.Log($"[{CLASS_NAME}.ChangeControlFlag] Successfully updated control flag. TileControl: {hex.TileControl}, DefaultTileControl: {hex.DefaultTileControl}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ChangeControlFlag", e);
            }
        }

        #endregion // Public Methods

        #region Private Methods - Hex Outlines

        /// <summary>
        /// Draws hex outlines based on current map configuration and outline color settings.
        /// </summary>
        private void DrawHexOutlines()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.DrawHexOutlines] Drawing hex outlines...");

                // Clear existing hex outlines
                hexOutlineTilemap.ClearAllTiles();

                // Get the appropriate sprite based on outline color
                string spriteName = GetOutlineSpriteName();
                Sprite sprite = SpriteManager.GetSprite(spriteName);

                if (_debug) Debug.Log($"[{CLASS_NAME}.DrawHexOutlines] Using outline color: {GameDataManager.CurrentHexOutlineColor}, sprite: {spriteName}");

                // Create and configure the tile
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;

                // Draw tiles for each valid position
                Vector2Int mapSize = GameDataManager.CurrentMapSize.ToVector2Int();
                int tileCount = 0;

                for (int x = 0; x < mapSize.x; x++)
                {
                    for (int y = 0; y < mapSize.y; y++)
                    {
                        if (IsValidHexPosition(x, y, mapSize))
                        {
                            hexOutlineTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                            tileCount++;
                        }
                    }
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.DrawHexOutlines] Drew {tileCount} hex outline tiles for map size {mapSize.x}x{mapSize.y}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawHexOutlines", e);
            }
        }

        /// <summary>
        /// Gets the appropriate sprite name based on current outline color setting.
        /// </summary>
        private string GetOutlineSpriteName()
        {
            return GameDataManager.CurrentHexOutlineColor switch
            {
                HexOutlineColor.Black => SpriteManager.BlackHexOutline,
                HexOutlineColor.White => SpriteManager.WhiteHexOutline,
                _ => SpriteManager.GreyHexOutline
            };
        }

        /// <summary>
        /// Checks if the given coordinates represent a valid hex position.
        /// </summary>
        private bool IsValidHexPosition(int x, int y, Vector2Int mapSize)
        {
            // Don't render the last column if row is odd (prevents rendering out of bounds)
            if (y % 2 != 0 && x >= mapSize.x - 1)
                return false;

            return true;
        }

        #endregion // Private Methods - Hex Outlines

        #region Private Methods - Map Icons

        /// <summary>
        /// Draws map icons for a specific hex (airbases and forts).
        /// </summary>
        private void DrawMapIconsForHex(HexTile hex)
        {
            try
            {
                if (hex.IsAirbase)
                {
                    CreateMapIcon(hex, MapIconType.Airbase);
                }
                else if (hex.IsFort)
                {
                    CreateMapIcon(hex, MapIconType.Fort);
                }
                // Note that UrbanDamage is a proxy for urban sprawl
                else if (hex.UrbanDamage > 0)
                {
                    CreateMapIcon(hex, MapIconType.UrbanSprawl);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawMapIconsForHex", e);
            }
        }

        /// <summary>
        /// Creates a map icon prefab at the specified hex position.
        /// </summary>
        private void CreateMapIcon(HexTile hex, MapIconType iconType)
        {
            if (_debug) Debug.Log($"[{CLASS_NAME}.CreateMapIcon] Creating {iconType} icon at ({hex.Position.IntX}, {hex.Position.IntY})");

            // Create prefab instance
            GameObject mapIconObject = Instantiate(SpriteManager.Instance.MapIconPrefab, mapIconLayer.transform);
            mapIconObject.name = $"{iconType}_{hex.Position.IntX}_{hex.Position.IntY}";

            // Get prefab component
            Prefab_MapIcon mapIconPrefab = mapIconObject.GetComponent<Prefab_MapIcon>();

            // Configure prefab
            mapIconPrefab.SetIconType(iconType);
            mapIconPrefab.SetPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));

            // Get the appropriate sprite name
            string spriteName;
            if (iconType == MapIconType.Airbase)
            {
                spriteName = SpriteManager.GEN_Airbase;
            }
            else if (iconType == MapIconType.Fort)
            {
                spriteName = SpriteManager.GEN_Fort;
            }
            else
            {
                spriteName = SpriteManager.ME_Sprawl;
            }

            // Set the sprite
            mapIconPrefab.GetSpriteRenderer().sprite = SpriteManager.GetSprite(spriteName);

            // Position the prefab
            mapIconObject.transform.position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));

            // Store reference
            mapIconPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = mapIconPrefab;
        }

        #endregion // Private Methods - Map Icons

        #region Private Methods - City Icons

        /// <summary>
        /// Draws city icon for a specific hex if it's a city.
        /// </summary>
        private void DrawCityIconForHex(HexTile hex)
        {
            try
            {
                TerrainType terrain = hex.Terrain;

                // Check if this hex should have a city icon
                if (terrain == TerrainType.MajorCity || terrain == TerrainType.MinorCity)
                {
                    CreateCityIcon(hex, terrain);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawCityIconForHex", e);
            }
        }

        /// <summary>
        /// Creates a city icon prefab at the specified hex position.
        /// </summary>
        private void CreateCityIcon(HexTile hex, TerrainType terrain)
        {
            if (_debug) Debug.Log($"[{CLASS_NAME}.CreateCityIcon] Creating {terrain} at ({hex.Position.IntX}, {hex.Position.IntY}), Label: '{hex.TileLabel}', IsObjective: {hex.IsObjective}");

            // Create city prefab instance
            GameObject cityObj = Instantiate(SpriteManager.Instance.CityPrefab, cityIconLayer.transform);
            cityObj.name = $"City_{hex.Position.IntX}_{hex.Position.IntY}";

            // Get prefab component
            Prefab_CityIcon cityPrefab = cityObj.GetComponent<Prefab_CityIcon>();

            // Position the prefab
            UnityEngine.Vector3 position = hexOutlineTilemap.GetCellCenterWorld(new Vector3Int(hex.Position.IntX, hex.Position.IntY, 0));
            cityObj.transform.position = position;

            // Update the prefab's visual elements
            cityPrefab.UpdateCityIcon(terrain, GameDataManager.CurrentMapTheme);
            cityPrefab.UpdateNameplate(GameDataManager.CurrentMapTheme);
            cityPrefab.UpdateControlFlag(hex.TileControl, hex.DefaultTileControl);
            cityPrefab.UpdateCityName(hex.TileLabel);
            cityPrefab.UpdateObjectiveStatus(hex.IsObjective);

            // Store the prefab reference
            cityPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = cityPrefab;
        }

        #endregion // Private Methods - City Icons

        #region Private Methods - Bridges

        /// <summary>
        /// Draws bridge icons for a specific hex.
        /// Only renders bridges in E, SE, and SW directions to avoid duplicates.
        /// </summary>
        private void DrawBridgesForHex(HexTile hex)
        {
            try
            {
                // Only render bridges in half the directions to avoid duplicates
                // Each bridge will be rendered once from one of the two hexes it connects
                DrawBridgeInDirection(hex, HexDirection.E, hex.BridgeBorders.East, hex.DamagedBridgeBorders.East, hex.PontoonBridgeBorders.East);
                DrawBridgeInDirection(hex, HexDirection.SE, hex.BridgeBorders.Southeast, hex.DamagedBridgeBorders.Southeast, hex.PontoonBridgeBorders.Southeast);
                DrawBridgeInDirection(hex, HexDirection.SW, hex.BridgeBorders.Southwest, hex.DamagedBridgeBorders.Southwest, hex.PontoonBridgeBorders.Southwest);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawBridgesForHex", e);
            }
        }

        /// <summary>
        /// Draws a bridge in the specified direction if one exists.
        /// </summary>
        private void DrawBridgeInDirection(HexTile hex, HexDirection direction, bool hasRegular, bool hasDamaged, bool hasPontoon)
        {
            // Check each bridge type and create if needed
            if (hasRegular)
            {
                CreateBridgeObject(hex, BridgeType.Regular, direction);
            }
            if (hasDamaged)
            {
                CreateBridgeObject(hex, BridgeType.DamagedRegular, direction);
            }
            if (hasPontoon)
            {
                CreateBridgeObject(hex, BridgeType.Pontoon, direction);
            }
        }

        /// <summary>
        /// Creates a bridge icon object on the map.
        /// </summary>
        private void CreateBridgeObject(HexTile hex, BridgeType bridgeType, HexDirection direction)
        {
            // Verify neighbor exists (bridges span between two hexes)
            HexTile neighborHex = hex.GetNeighbor(direction);
            if (neighborHex == null)
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateBridgeObject] Skipping {bridgeType} bridge at ({hex.Position.IntX}, {hex.Position.IntY}) direction {direction}: No neighbor found.");
                return;
            }

            // Generate unique key for this bridge
            string bridgeKey = GenerateBridgeKey(hex.Position, direction, bridgeType);

            // Skip if already rendered
            if (bridgePrefabs.ContainsKey(bridgeKey))
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateBridgeObject] Skipping duplicate bridge: {bridgeKey}");
                return;
            }

            if (_debug) Debug.Log($"[{CLASS_NAME}.CreateBridgeObject] Creating {bridgeType} bridge at ({hex.Position.IntX}, {hex.Position.IntY}) direction {direction}");

            // Create bridge prefab instance
            GameObject bridgeIconObject = Instantiate(SpriteManager.Instance.BridgeIconPrefab, bridgeIconLayer.transform);
            bridgeIconObject.name = $"{bridgeType}_{direction}_{hex.Position.IntX}_{hex.Position.IntY}";

            // Get prefab component
            Prefab_BridgeIcon bridgeIconPrefab = bridgeIconObject.GetComponent<Prefab_BridgeIcon>();

            // Configure prefab
            bridgeIconPrefab.Type = bridgeType;
            bridgeIconPrefab.Dir = direction;
            bridgeIconPrefab.Pos = hex.Position.ToVector2Int();
            bridgeIconPrefab.Renderer.sprite = GetBridgeSprite(bridgeType, direction);

            // Position the prefab
            UnityEngine.Vector3 position = hexOutlineTilemap.GetCellCenterWorld(new Vector3Int(hex.Position.IntX, hex.Position.IntY, 0));
            bridgeIconObject.transform.position = position;

            // Store the prefab reference
            bridgePrefabs[bridgeKey] = bridgeIconPrefab;
        }

        /// <summary>
        /// Generates a unique key for a bridge.
        /// </summary>
        private string GenerateBridgeKey(Position2D pos, HexDirection dir, BridgeType type)
        {
            return $"{pos.IntX}_{pos.IntY}_{(int)dir}_{(int)type}";
        }

        /// <summary>
        /// Gets the appropriate bridge sprite based on type and direction.
        /// </summary>
        private Sprite GetBridgeSprite(BridgeType bridgeType, HexDirection direction)
        {
            string spriteName = direction switch
            {
                HexDirection.NE => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeNE,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeNE,
                    BridgeType.Pontoon => SpriteManager.PontBridgeNE,
                    _ => throw new ArgumentOutOfRangeException(nameof(bridgeType))
                },
                HexDirection.E => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeE,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeE,
                    BridgeType.Pontoon => SpriteManager.PontBridgeE,
                    _ => throw new ArgumentOutOfRangeException(nameof(bridgeType))
                },
                HexDirection.SE => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeSE,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeSE,
                    BridgeType.Pontoon => SpriteManager.PontBridgeSE,
                    _ => throw new ArgumentOutOfRangeException(nameof(bridgeType))
                },
                HexDirection.SW => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeSW,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeSW,
                    BridgeType.Pontoon => SpriteManager.PontBridgeSW,
                    _ => throw new ArgumentOutOfRangeException(nameof(bridgeType))
                },
                HexDirection.W => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeW,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeW,
                    BridgeType.Pontoon => SpriteManager.PontBridgeW,
                    _ => throw new ArgumentOutOfRangeException(nameof(bridgeType))
                },
                HexDirection.NW => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeNW,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeNW,
                    BridgeType.Pontoon => SpriteManager.PontBridgeNW,
                    _ => throw new ArgumentOutOfRangeException(nameof(bridgeType))
                },
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };

            return SpriteManager.GetSprite(spriteName);
        }

        #endregion // Private Methods - Bridges

        #region Private Methods - Text Labels

        /// <summary>
        /// Draws text labels for a specific hex if it has large label text.
        /// Note: Small labels (TileLabel) are handled by CityIconPrefab.
        /// </summary>
        private void DrawTextLabelsForHex(HexTile hex)
        {
            try
            {
                // Only render large tile labels (small labels are for cities)
                if (string.IsNullOrEmpty(hex.LargeTileLabel))
                {
                    return;
                }

                // Create the text label prefab
                CreateTextLabel(hex, hex.LargeTileLabel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawTextLabelsForHex", e);
            }
        }

        /// <summary>
        /// Creates a text label prefab at the specified hex position.
        /// </summary>
        private void CreateTextLabel(HexTile hex, string labelText)
        {
            if (_debug) Debug.Log($"[{CLASS_NAME}.CreateTextLabel] Creating text label at ({hex.Position.IntX}, {hex.Position.IntY}): '{labelText}', Size: {hex.LabelSize}, Weight: {hex.LabelWeight}");

            // Create text prefab instance
            GameObject textObject = Instantiate(SpriteManager.Instance.MapTextPrefab, textLabelLayer.transform);
            textObject.name = $"TextLabel_{hex.Position.IntX}_{hex.Position.IntY}";

            // Get prefab component
            Prefab_MapText textPrefab = textObject.GetComponent<Prefab_MapText>();

            // Convert enum types from GameData namespace to Core namespace
            TextSize coreTextSize = hex.LabelSize switch
            {
                GameData.TextSize.Small => TextSize.Small,
                GameData.TextSize.Medium => TextSize.Medium,
                GameData.TextSize.Large => TextSize.Large,
                _ => TextSize.Medium
            };

            FontWeight coreFontWeight = hex.LabelWeight switch
            {
                GameData.FontWeight.Light => FontWeight.Light,
                GameData.FontWeight.Medium => FontWeight.Medium,
                GameData.FontWeight.Bold => FontWeight.Bold,
                _ => FontWeight.Medium
            };

            // Configure the text label
            textPrefab.SetText(labelText);
            textPrefab.SetSize(coreTextSize);
            textPrefab.SetFont(coreFontWeight);
            textPrefab.SetColor(hex.LabelColor);
            textPrefab.SetOutlineThickness(hex.LabelOutlineThickness);

            // Position the prefab at the hex center
            UnityEngine.Vector3 position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));
            textObject.transform.position = position;

            // Store the prefab reference
            textLabelPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = textPrefab;
        }

        #endregion // Private Methods - Text Labels

        #region Private Methods - Utilities

        /// <summary>
        /// Clears all child objects from the specified container.
        /// </summary>
        private void ClearContainer(Transform container)
        {
            try
            {
                int childCount = container.childCount;
                if (_debug && childCount > 0) Debug.Log($"[{CLASS_NAME}.ClearContainer] Clearing {childCount} child objects from {container.name}");

                // Loop backwards because destroying objects will shift the indices
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    Destroy(container.GetChild(i).gameObject);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearContainer", e);
            }
        }

        #endregion // Private Methods - Utilities

        #region Private Methods - Unit Icons

        // Sprite Selection System
        // =======================
        // Sprites are selected based on: WeaponSystem profile, Nationality, Facing, and DeploymentPosition
        //
        // Direction Rules:
        //   - Sprites exist in W, NW, SW variants (vehicles) or W only (infantry, aircraft)
        //   - Unit facing E/NE/SE uses W/NW/SW sprite flipped horizontally
        //
        // Fired Suffix (_F):
        //   - Artillery/rocket systems have _F variants for firing position
        //   - Use _F when DeploymentPosition is HastyDefense, Entrenched, or Fortified
        //
        // Special Cases:
        //   - Infantry: Uses nationality-specific sprites (SV_Regulars, US_Regulars, etc.)
        //   - MJ units: Have unique sprites for mortars, AAA, manpads, RPG, light artillery
        //   - Helicopters: Animated sprites (Frame0-5)
        //   - Embarked: Air transport (AN8) or naval transport
        //   - _IPO weapons: Intel Profile Only, no sprite needed
        //
        // Fallback Mappings:
        //   - IFV_BMD1 → BMD2, IFV_M3 → M2, SAM_RAPIER → US_Hawk
        //   - UK IFVs/APCs → Warrior, French APCs/IFVs → FR_M113, APC_VAB → FR_M113
        //
        // TODO: Add M60 tank sprite (TANK_M60A3)
        // TODO: Add China weapon systems and sprite mappings

        /// <summary>
        /// Draws all combat units on the map.
        /// </summary>
        private void DrawAllUnits(ref int unitCount)
        {
            try
            {
                // TODO: Determine how units are stored in GameDataManager
                // Options: GameDataManager.AllUnits? GameDataManager.CurrentUnits?
                // For now, using placeholder logic

                if (_debug) Debug.Log($"[{CLASS_NAME}.DrawAllUnits] Starting unit rendering...");

                // TODO: Replace with actual unit collection access
                // Example: foreach (var unit in GameDataManager.Instance.GetAllUnits())

                // Placeholder - remove when actual implementation is done
                if (_debug) Debug.LogWarning($"[{CLASS_NAME}.DrawAllUnits] Unit rendering not yet implemented - need GameDataManager unit access");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawAllUnits", e);
            }
        }

        /// <summary>
        /// Creates a unit icon prefab at the specified position.
        /// </summary>
        private void CreateUnitIcon(CombatUnit unit)
        {
            try
            {
                if (unit == null)
                {
                    if (_debug) Debug.LogWarning($"[{CLASS_NAME}.CreateUnitIcon] Unit is null, skipping.");
                    return;
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Creating icon for unit '{unit.UnitName}' at ({unit.MapPos.IntX}, {unit.MapPos.IntY})");

                // Create prefab instance
                GameObject unitIconObject = Instantiate(SpriteManager.Instance.UnitIconPrefab, mainUnitLayer.transform);
                unitIconObject.name = $"Unit_{unit.UnitID}_{unit.UnitName}";

                // Get prefab component
                Prefab_CombatUnitIcon unitIcon = unitIconObject.GetComponent<Prefab_CombatUnitIcon>();
                if (unitIcon == null)
                {
                    Debug.LogError($"{CLASS_NAME}.CreateUnitIcon: Prefab_CombatUnitIcon component not found on prefab!");
                    Destroy(unitIconObject);
                    return;
                }

                // Get sprite name and flip info for this unit
                string spriteName = GetSpriteNameForUnit(unit, out bool shouldFlip);

                // Check if this is an animated sprite
                if (spriteName.Contains("Frame", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Implement animated sprite rendering (frame cycling)
                    // For now, using Frame0 as static sprite
                    if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Unit uses animated sprite: {spriteName}");
                }

                // Set the unit icon sprite
                unitIcon.SetUnitIcon(spriteName);

                // Apply horizontal flip if unit is facing east (E, NE, SE)
                if (shouldFlip)
                {
                    unitIcon.UnitIconRenderer.flipX = true;
                    if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Flipping sprite for unit '{unit.UnitName}' facing {unit.Facing}");
                }

                // TODO: Implement NATO icon rendering
                // Requires: SetNatoIcon() method in Prefab_CombatUnitIcon
                // unitIcon.SetNatoIcon(GetNatoIcon(unit.Classification));

                // TODO: Implement hit points ratio text display
                // Requires: HitPointsRatio property in Prefab_CombatUnitIcon
                // unitIcon.HitPointsRatio = $"{unit.HitPoints.Current:F0}/{unit.HitPoints.Max:F0}";

                // Position the prefab
                UnityEngine.Vector3 position = GetRenderPosition(new Vector2Int(unit.MapPos.IntX, unit.MapPos.IntY));
                unitIconObject.transform.position = position;

                // Store reference
                unitIconPrefabs[unit.UnitID] = unitIcon;

                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Successfully created icon for unit '{unit.UnitName}'");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateUnitIcon", e);
            }
        }

        /// <summary>
        /// Gets the appropriate sprite name for a combat unit based on its properties.
        /// Returns the sprite name and sets the out parameter for whether to flip horizontally.
        /// </summary>
        private string GetSpriteNameForUnit(CombatUnit unit, out bool shouldFlip)
        {
            shouldFlip = false;

            try
            {
                // Handle embarked units separately
                if (unit.DeploymentPosition == DeploymentPosition.Embarked)
                {
                    return GetEmbarkedSpriteName(unit);
                }

                // Get active weapon system based on deployment position
                WeaponSystems weaponSystem = GetActiveWeaponSystem(unit);

                // Get base sprite name (includes nationality prefix)
                string baseSpriteName = GetBaseSpriteFromProfile(weaponSystem, unit.Nationality);

                // If no sprite for this weapon system, return fallback
                if (string.IsNullOrEmpty(baseSpriteName))
                {
                    return GetFallbackSprite(unit.Nationality);
                }

                // Check if this is an animated sprite (helicopters) - no direction suffix needed
                if (IsAnimatedSprite(weaponSystem))
                {
                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': Animated sprite={baseSpriteName}");
                    return baseSpriteName;
                }

                // Check if this sprite has no direction variants (infantry, bases)
                if (!HasDirectionSuffix(weaponSystem))
                {
                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': No direction sprite={baseSpriteName}");
                    return baseSpriteName;
                }

                // Determine direction suffix and flip
                string directionSuffix = GetDirectionSuffix(unit.Facing, HasMultipleDirections(weaponSystem));
                shouldFlip = ShouldFlipSprite(unit.Facing);

                // Determine if we need _F (fired) suffix for artillery
                string firedSuffix = NeedsFiredSuffix(weaponSystem, unit.DeploymentPosition) ? "_F" : "";

                // Combine: base + direction + fired
                string finalSpriteName = baseSpriteName + directionSuffix + firedSuffix;

                if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': Weapon={weaponSystem}, Sprite={finalSpriteName}, Flip={shouldFlip}");

                return finalSpriteName;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSpriteNameForUnit", e);
                return GetFallbackSprite(unit.Nationality);
            }
        }

        /// <summary>
        /// Overload for backwards compatibility - ignores flip information.
        /// </summary>
        private string GetSpriteNameForUnit(CombatUnit unit)
        {
            return GetSpriteNameForUnit(unit, out _);
        }

        /// <summary>
        /// Gets the embarked sprite name based on unit classification.
        /// </summary>
        private string GetEmbarkedSpriteName(CombatUnit unit)
        {
            // Airborne, Mechanized Airborne, and Special Forces use air transport
            if (unit.Classification == UnitClassification.AB ||
                unit.Classification == UnitClassification.MAB ||
                unit.Classification == UnitClassification.SPECF)
            {
                return SpriteManager.SV_AN8_W;
            }
            else
            {
                return SpriteManager.GEN_NavalTransport;
            }
        }

        /// <summary>
        /// Gets the direction suffix based on unit facing.
        /// </summary>
        private string GetDirectionSuffix(HexDirection facing, bool hasMultipleDirections)
        {
            if (!hasMultipleDirections)
            {
                return "_W";
            }

            return facing switch
            {
                HexDirection.W => "_W",
                HexDirection.NW => "_NW",
                HexDirection.SW => "_SW",
                HexDirection.E => "_W",   // Use W sprite, flip horizontally
                HexDirection.NE => "_NW", // Use NW sprite, flip horizontally
                HexDirection.SE => "_SW", // Use SW sprite, flip horizontally
                _ => "_W"
            };
        }

        /// <summary>
        /// Determines if sprite should be flipped horizontally based on facing.
        /// </summary>
        private bool ShouldFlipSprite(HexDirection facing)
        {
            return facing == HexDirection.E ||
                   facing == HexDirection.NE ||
                   facing == HexDirection.SE;
        }

        /// <summary>
        /// Determines if the _F (fired) suffix should be used for artillery sprites.
        /// </summary>
        private bool NeedsFiredSuffix(WeaponSystems weaponSystem, DeploymentPosition position)
        {
            // Only artillery/rocket systems have _F variants
            if (!HasFiredVariant(weaponSystem))
            {
                return false;
            }

            // Use _F when in HastyDefense or more entrenched positions
            return position == DeploymentPosition.HastyDefense ||
                   position == DeploymentPosition.Entrenched ||
                   position == DeploymentPosition.Fortified;
        }

        /// <summary>
        /// Checks if weapon system has a _F (fired) sprite variant.
        /// </summary>
        private bool HasFiredVariant(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Soviet SP Artillery
                WeaponSystems.SPA_2S1 => true,
                WeaponSystems.SPA_2S3 => true,
                WeaponSystems.SPA_2S5 => true,
                WeaponSystems.SPA_2S19 => true,
                // Soviet Rocket Artillery
                WeaponSystems.ROC_BM21 => true,
                WeaponSystems.ROC_BM27 => true,
                WeaponSystems.ROC_BM30 => true,
                // Soviet SSM
                WeaponSystems.SSM_SCUD => true,
                // US Artillery
                WeaponSystems.SPA_M109 => true,
                WeaponSystems.ROC_MLRS => true,
                // UK Artillery (uses US M109)
                // German Artillery (uses GE M109)
                // French Artillery (uses FR M109)
                _ => false
            };
        }

        /// <summary>
        /// Checks if weapon system has multiple direction variants (W, NW, SW).
        /// </summary>
        private bool HasMultipleDirections(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Tanks - all have 3 directions
                WeaponSystems.TANK_T55A => true,
                WeaponSystems.TANK_T64A => true,
                WeaponSystems.TANK_T64B => true,
                WeaponSystems.TANK_T72A => true,
                WeaponSystems.TANK_T72B => true,
                WeaponSystems.TANK_T80B => true,
                WeaponSystems.TANK_T80U => true,
                WeaponSystems.TANK_T80BV => true,
                WeaponSystems.TANK_M1 => true,
                WeaponSystems.TANK_LEOPARD1 => true,
                WeaponSystems.TANK_LEOPARD2 => true,
                WeaponSystems.TANK_CHALLENGER1 => true,
                WeaponSystems.TANK_AMX30 => true,

                // APCs - all have 3 directions
                WeaponSystems.APC_MTLB => true,
                WeaponSystems.APC_BTR70 => true,
                WeaponSystems.APC_BTR80 => true,
                WeaponSystems.APC_M113 => true,
                WeaponSystems.APC_LVTP7 => true,

                // IFVs - all have 3 directions
                WeaponSystems.IFV_BMP1 => true,
                WeaponSystems.IFV_BMP2 => true,
                WeaponSystems.IFV_BMP3 => true,
                WeaponSystems.IFV_BMD1 => true,
                WeaponSystems.IFV_BMD2 => true,
                WeaponSystems.IFV_BMD3 => true,
                WeaponSystems.IFV_M2 => true,
                WeaponSystems.IFV_M3 => true,
                WeaponSystems.IFV_MARDER => true,
                WeaponSystems.IFV_WARRIOR => true,

                // Recon - have 3 directions
                WeaponSystems.RCN_BRDM2 => true,
                WeaponSystems.RCN_BRDM2AT => true,

                // SPAAA - have 3 directions
                WeaponSystems.SPAAA_ZSU57 => true,
                WeaponSystems.SPAAA_ZSU23 => true,
                WeaponSystems.SPAAA_2K22 => true,
                WeaponSystems.SPAAA_M163 => true,
                WeaponSystems.SPAAA_GEPARD => true,

                // SPSAM - have 3 directions
                WeaponSystems.SPSAM_9K31 => true,
                WeaponSystems.SPSAM_CHAP => true,
                WeaponSystems.SPSAM_ROLAND => true,

                // Trucks - have 3 directions
                WeaponSystems.TRUCK_GENERIC => true,

                // Everything else has W only or no direction
                _ => false
            };
        }

        /// <summary>
        /// Checks if weapon system sprite has any direction suffix.
        /// </summary>
        private bool HasDirectionSuffix(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Infantry - no direction suffix
                WeaponSystems.INF_REG => false,
                WeaponSystems.INF_AB => false,
                WeaponSystems.INF_AM => false,
                WeaponSystems.INF_MAR => false,
                WeaponSystems.INF_SPEC => false,
                WeaponSystems.INF_ENG => false,

                // Bases - no direction suffix
                WeaponSystems.LANDBASE_GENERIC => false,
                WeaponSystems.AIRBASE_GENERIC => false,
                WeaponSystems.SUPPLYDEPOT_GENERIC => false,

                // MJ special units - no direction suffix
                WeaponSystems.CAVALRY_GENERIC => false,

                // System values - no sprite
                WeaponSystems.COMBAT => false,
                WeaponSystems.DEFAULT => false,

                // Everything else has direction suffix
                _ => true
            };
        }

        /// <summary>
        /// Checks if weapon system uses animated sprites.
        /// </summary>
        private bool IsAnimatedSprite(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Soviet Helicopters
                WeaponSystems.HEL_MI8T => true,
                WeaponSystems.HEL_MI8AT => true,
                WeaponSystems.HEL_MI24D => true,
                WeaponSystems.HEL_MI24V => true,
                WeaponSystems.HEL_MI28 => true,
                // US Helicopters
                WeaponSystems.HEL_AH64 => true,
                WeaponSystems.HEL_UH60 => true,
                // German Helicopters
                WeaponSystems.HEL_BO105 => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets fallback sprite based on nationality.
        /// </summary>
        private string GetFallbackSprite(Nationality nationality)
        {
            return nationality switch
            {
                Nationality.USSR => SpriteManager.SV_Regulars,
                Nationality.USA => SpriteManager.US_Regulars,
                Nationality.UK => SpriteManager.UK_Regulars,
                Nationality.FRG => SpriteManager.GE_Regulars,
                Nationality.FRA => SpriteManager.FR_Regulars,
                Nationality.MJ => SpriteManager.MJ_Regulars,
                _ => SpriteManager.SV_Regulars
            };
        }

        /// <summary>
        /// Gets the active weapon system based on unit deployment position.
        /// </summary>
        private WeaponSystems GetActiveWeaponSystem(CombatUnit unit)
        {
            return unit.DeploymentPosition switch
            {
                // Embarked uses embarked profile if available
                DeploymentPosition.Embarked => unit.EmbarkedProfileID != WeaponSystems.DEFAULT
                    ? unit.EmbarkedProfileID
                    : unit.DeployedProfileID,

                // Mobile uses mobile profile if available
                DeploymentPosition.Mobile => unit.MobileProfileID != WeaponSystems.DEFAULT
                    ? unit.MobileProfileID
                    : unit.DeployedProfileID,

                // All other states use deployed profile
                _ => unit.DeployedProfileID
            };
        }

        /// <summary>
        /// Maps WeaponSystems enum to base sprite name (without directional/fired suffixes).
        /// Requires nationality for infantry and MJ special handling.
        /// </summary>
        private string GetBaseSpriteFromProfile(WeaponSystems profile, Nationality nationality)
        {
            // Handle infantry profiles - requires nationality
            if (IsInfantryProfile(profile))
            {
                return GetInfantrySprite(profile, nationality);
            }

            // Handle MJ special cases
            if (nationality == Nationality.MJ)
            {
                string mjSprite = GetMujahideenSprite(profile);
                if (!string.IsNullOrEmpty(mjSprite))
                {
                    return mjSprite;
                }
            }

            return profile switch
            {
                // ═══════════════════════════════════════════════════════════════
                // SOVIET WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // Soviet Tanks
                WeaponSystems.TANK_T55A => "SV_T55A",
                WeaponSystems.TANK_T64A => "SV_T64A",
                WeaponSystems.TANK_T64B => "SV_T64B",
                WeaponSystems.TANK_T72A => "SV_T72A",
                WeaponSystems.TANK_T72B => "SV_T72B",
                WeaponSystems.TANK_T80B => "SV_T80B",
                WeaponSystems.TANK_T80U => "SV_T80U",
                WeaponSystems.TANK_T80BV => "SV_T80BVM",

                // Soviet APCs
                WeaponSystems.APC_MTLB => "SV_MTLB",
                WeaponSystems.APC_BTR70 => "SV_BTR70",
                WeaponSystems.APC_BTR80 => "SV_BTR80",

                // Soviet IFVs
                WeaponSystems.IFV_BMP1 => "SV_BMP1",
                WeaponSystems.IFV_BMP2 => "SV_BMP2",
                WeaponSystems.IFV_BMP3 => "SV_BMP3",
                WeaponSystems.IFV_BMD1 => "SV_BMD2", // Fallback: no BMD1 sprite
                WeaponSystems.IFV_BMD2 => "SV_BMD2",
                WeaponSystems.IFV_BMD3 => "SV_BMD3",

                // Soviet Recon
                WeaponSystems.RCN_BRDM2 => "SV_BRDM2",
                WeaponSystems.RCN_BRDM2AT => "SV_BRDM2AT",

                // Soviet Self-Propelled Artillery
                WeaponSystems.SPA_2S1 => "SV_2S1",
                WeaponSystems.SPA_2S3 => "SV_2S3",
                WeaponSystems.SPA_2S5 => "SV_2S5",
                WeaponSystems.SPA_2S19 => "SV_2S19",

                // Soviet Rocket Artillery
                WeaponSystems.ROC_BM21 => "SV_BM21",
                WeaponSystems.ROC_BM27 => "SV_BM27",
                WeaponSystems.ROC_BM30 => "SV_BM30",

                // Soviet SSM
                WeaponSystems.SSM_SCUD => "SV_ScudB",

                // Soviet SPAAA
                WeaponSystems.SPAAA_ZSU57 => "SV_ZSU57",
                WeaponSystems.SPAAA_ZSU23 => "SV_ZSU23",
                WeaponSystems.SPAAA_2K22 => "SV_2K22",

                // Soviet SPSAM
                WeaponSystems.SPSAM_9K31 => "SV_9K31",

                // Soviet SAM
                WeaponSystems.SAM_S75 => "SV_S75",
                WeaponSystems.SAM_S125 => "SV_S125",
                WeaponSystems.SAM_S300 => "SV_S300",

                // Soviet Helicopters (animated)
                WeaponSystems.HEL_MI8T => SpriteManager.SV_MI8_Frame0,  // Transport helo uses MI8
                WeaponSystems.HEL_MI8AT => SpriteManager.SV_MI8AT_Frame0,
                WeaponSystems.HEL_MI24D => SpriteManager.SV_MI24D_Frame0,
                WeaponSystems.HEL_MI24V => SpriteManager.SV_MI24V_Frame0,
                WeaponSystems.HEL_MI28 => SpriteManager.SV_MI28_Frame0,

                // Soviet AWACS
                WeaponSystems.AWACS_A50 => "SV_A50",

                // Soviet Fighters (note: sprite uses "Mig" not "MIG")
                WeaponSystems.FGT_MIG21 => "SV_Mig21",
                WeaponSystems.FGT_MIG23 => "SV_Mig23",
                WeaponSystems.FGT_MIG25 => "SV_Mig25",
                WeaponSystems.FGT_MIG29 => "SV_Mig29",
                WeaponSystems.FGT_MIG31 => "SV_Mig31",
                WeaponSystems.FGT_SU27 => "SV_SU27",
                WeaponSystems.FGT_SU47 => "SV_SU47",
                WeaponSystems.FGT_MIG27 => "SV_Mig27",

                // Soviet Attack Aircraft
                WeaponSystems.ATT_SU25 => "SV_SU25",
                WeaponSystems.ATT_SU25B => "SV_SU25B",

                // Soviet Bombers
                WeaponSystems.BMB_SU24 => "SV_SU24",
                WeaponSystems.BMB_TU16 => "SV_TU16",
                WeaponSystems.BMB_TU22 => "SV_TU22",
                WeaponSystems.BMB_TU22M3 => "SV_TU22M3",

                // Soviet Recon Aircraft
                WeaponSystems.RCNA_MIG25R => "SV_Mig25R",

                // Soviet Transport
                WeaponSystems.Transport_AIR => "SV_AN8",
                WeaponSystems.Transport_NAVAL => SpriteManager.GEN_NavalTransport,

                // ═══════════════════════════════════════════════════════════════
                // USA WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // US Tanks
                WeaponSystems.TANK_M1 => "US_M1",
                WeaponSystems.TANK_M60A3 => "US_M1", // TODO: Add M60 sprite, using M1 fallback

                // US IFVs
                WeaponSystems.IFV_M2 => "US_M2",
                WeaponSystems.IFV_M3 => "US_M2", // Fallback: use M2 sprite

                // US APCs
                WeaponSystems.APC_M113 => "US_M113",
                WeaponSystems.APC_LVTP7 => "US_LVTP",

                // US Artillery
                WeaponSystems.SPA_M109 => "US_M109",
                WeaponSystems.ROC_MLRS => "US_MLRS",

                // US SPAAA
                WeaponSystems.SPAAA_M163 => "US_M163",

                // US SPSAM
                WeaponSystems.SPSAM_CHAP => "US_Chaparral",

                // US SAM
                WeaponSystems.SAM_HAWK => "US_Hawk",

                // US Helicopters (animated)
                WeaponSystems.HEL_AH64 => SpriteManager.US_AH64_Frame0,
                WeaponSystems.HEL_UH60 => SpriteManager.US_UH60_Frame0,

                // US AWACS
                WeaponSystems.AWACS_E3 => "US_E3",

                // US Fighters
                WeaponSystems.FGT_F15 => "US_F15",
                WeaponSystems.FGT_F4 => "US_F4",
                WeaponSystems.FGT_F16 => "US_F16",

                // US Attack Aircraft
                WeaponSystems.ATT_A10 => "US_A10",

                // US Bombers
                WeaponSystems.BMB_F111 => "US_F111",
                WeaponSystems.BMB_F117 => "US_F117",

                // US Recon Aircraft
                WeaponSystems.RCNA_SR71 => "US_SR71",

                // ═══════════════════════════════════════════════════════════════
                // WEST GERMANY (FRG) WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // German Tanks
                WeaponSystems.TANK_LEOPARD1 => "GE_Leopard1",
                WeaponSystems.TANK_LEOPARD2 => "GE_Leopard2",

                // German IFVs
                WeaponSystems.IFV_MARDER => "GE_Marder",

                // German SPAAA
                WeaponSystems.SPAAA_GEPARD => "GE_Gepard",

                // German Helicopters (animated)
                WeaponSystems.HEL_BO105 => SpriteManager.GE_BO105_Frame0,

                // German Aircraft
                WeaponSystems.FGT_TORNADO_IDS => "GE_Tornado",

                // ═══════════════════════════════════════════════════════════════
                // UK WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // UK Tanks
                WeaponSystems.TANK_CHALLENGER1 => "UK_Challenger1",

                // UK IFVs/APCs - all use Warrior sprite
                WeaponSystems.IFV_WARRIOR => "UK_Warrior",

                // UK SAM - uses US Hawk sprite
                WeaponSystems.SAM_RAPIER => "US_Hawk",

                // UK Aircraft
                WeaponSystems.FGT_TORNADO_GR1 => "UK_TornadoGR1",

                // ═══════════════════════════════════════════════════════════════
                // FRANCE WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // French Tanks
                WeaponSystems.TANK_AMX30 => "FR_AMX30",

                // French APCs - uses FR_M113 sprite
                WeaponSystems.APC_VAB => "FR_M113",

                // French SPSAM
                WeaponSystems.SPSAM_ROLAND => "FR_Roland",

                // French Aircraft
                WeaponSystems.FGT_MIRAGE2000 => "FR_Mirage2000",
                WeaponSystems.ATT_JAGUAR => "FR_Jaguar",

                // ═══════════════════════════════════════════════════════════════
                // GENERIC WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                WeaponSystems.AAA_GENERIC => "GEN_AA",
                WeaponSystems.ART_LIGHT_GENERIC => "GEN_LightArt",
                WeaponSystems.ART_HEAVY_GENERIC => "GEN_HeavyArt",
                WeaponSystems.TRUCK_GENERIC => "GEN_Truck",
                WeaponSystems.CAVALRY_GENERIC => SpriteManager.MJ_Mounted,

                // Bases
                WeaponSystems.LANDBASE_GENERIC => SpriteManager.GEN_Base,
                WeaponSystems.AIRBASE_GENERIC => SpriteManager.GEN_Base, // TODO: Needs airbase sprite
                WeaponSystems.SUPPLYDEPOT_GENERIC => SpriteManager.GEN_Depot,

                // ═══════════════════════════════════════════════════════════════
                // IPO (Intel Profile Only) - No sprites needed
                // ═══════════════════════════════════════════════════════════════
                WeaponSystems.TANK_M551_IPO => null,
                WeaponSystems.HEL_OH58_IPO => null,
                WeaponSystems.RCN_LUCHS_IPO => null,
                WeaponSystems.APC_FV432_IPO => null,
                WeaponSystems.RCN_SCIMITAR_IPO => null,
                WeaponSystems.HEL_LYNX_IPO => null,
                WeaponSystems.IFV_AMX10P_IPO => null,
                WeaponSystems.RCN_ERC90_IPO => null,
                WeaponSystems.SPA_AUF1_IPO => null,
                WeaponSystems.MANPAD_GENERIC_IPO => null,
                WeaponSystems.ATGM_GENERIC_IPO => null,
                WeaponSystems.AT_RPG7_IPO => null,
                WeaponSystems.MORTAR_81MM_IPO => null,
                WeaponSystems.MORTAR_120MM_IPO => null,
                WeaponSystems.RR_RECOILLESS_RIFLE_IPO => null,

                // System values
                WeaponSystems.COMBAT => null,
                WeaponSystems.DEFAULT => null,

                // Fallback
                _ => null
            };
        }

        /// <summary>
        /// Checks if weapon system is an infantry profile.
        /// </summary>
        private bool IsInfantryProfile(WeaponSystems profile)
        {
            return profile == WeaponSystems.INF_REG ||
                   profile == WeaponSystems.INF_AB ||
                   profile == WeaponSystems.INF_AM ||
                   profile == WeaponSystems.INF_MAR ||
                   profile == WeaponSystems.INF_SPEC ||
                   profile == WeaponSystems.INF_ENG;
        }

        /// <summary>
        /// Gets infantry sprite based on nationality and infantry type.
        /// </summary>
        private string GetInfantrySprite(WeaponSystems profile, Nationality nationality)
        {
            return (profile, nationality) switch
            {
                // Soviet Infantry
                (WeaponSystems.INF_REG, Nationality.USSR) => SpriteManager.SV_Regulars,
                (WeaponSystems.INF_AB, Nationality.USSR) => SpriteManager.SV_Airborne,
                (WeaponSystems.INF_AM, Nationality.USSR) => SpriteManager.SV_AirMobile,
                (WeaponSystems.INF_SPEC, Nationality.USSR) => SpriteManager.SV_Spetsnaz,
                (WeaponSystems.INF_ENG, Nationality.USSR) => SpriteManager.SV_Engineers,
                (WeaponSystems.INF_MAR, Nationality.USSR) => SpriteManager.SV_Regulars, // Marines use Regulars

                // US Infantry
                (WeaponSystems.INF_REG, Nationality.USA) => SpriteManager.US_Regulars,
                (WeaponSystems.INF_AB, Nationality.USA) => SpriteManager.US_Airborne,
                (WeaponSystems.INF_MAR, Nationality.USA) => SpriteManager.US_Marines,
                (_, Nationality.USA) => SpriteManager.US_Regulars,

                // UK Infantry
                (_, Nationality.UK) => SpriteManager.UK_Regulars,

                // German Infantry
                (_, Nationality.FRG) => SpriteManager.GE_Regulars,

                // French Infantry
                (_, Nationality.FRA) => SpriteManager.FR_Regulars,

                // Mujahideen Infantry
                (WeaponSystems.INF_SPEC, Nationality.MJ) => SpriteManager.MJ_Elite,
                (_, Nationality.MJ) => SpriteManager.MJ_Regulars,

                // Default fallback
                _ => SpriteManager.SV_Regulars
            };
        }

        /// <summary>
        /// Gets Mujahideen-specific sprites for special weapon systems.
        /// MJ units use some IPO weapons as deployed profile but have unique sprites.
        /// </summary>
        private string GetMujahideenSprite(WeaponSystems profile)
        {
            return profile switch
            {
                // MJ has sprites for these IPO profiles
                WeaponSystems.MORTAR_81MM_IPO => SpriteManager.MJ_Mortar,
                WeaponSystems.MORTAR_120MM_IPO => SpriteManager.MJ_Mortar,
                WeaponSystems.MANPAD_GENERIC_IPO => SpriteManager.MJ_Stinger,
                WeaponSystems.AT_RPG7_IPO => SpriteManager.MJ_RPG,

                // MJ generic types
                WeaponSystems.AAA_GENERIC => SpriteManager.MJ_AA,
                WeaponSystems.ART_LIGHT_GENERIC => SpriteManager.MJ_Artillery,
                WeaponSystems.CAVALRY_GENERIC => SpriteManager.MJ_Mounted,

                // Not a MJ special case
                _ => null
            };
        }

        #endregion // Private Methods - Unit Icons
    }
}
