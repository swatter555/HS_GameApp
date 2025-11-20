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

  //✅ What's Done:
  //- Unit icon prefab dictionary and tracking
  //- Outline color and thickness inspector fields
  //- Unit rendering integrated into RefreshMap()
  //- Sprite name resolution based on:
  //- DeployedProfileID → base sprite name
  //- Facing → directional suffix(_E/_W)
  //- DeploymentPosition → deployment suffix(_P or none)
  //- Embarked → special handling(AB/MAB/SPECF → AN8, others → naval)
  //- Animated sprite detection(Frame checking)
  //- Soviet unit mappings(tanks, IFVs, artillery, aircraft, helicopters)

  //📋 TODOs Marked in Code:
  //1. How to access units from GameDataManager
  //2. Complete USA/German/UK/French unit mappings
  //3. Infantry sprite selection based on Nationality
  //4. Generic unit handling
  //5. Naval transport sprite(doesn't exist yet in SpriteManager)
  //6. Flag sprite mapping
  //7. NATO icon mapping
  //8. Hit points ratio text
  //9. Outline rendering implementation (dual-render approach)
  //10. Animated sprite rendering method

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
                GameObject unitIconObject = null;
                if (unit.Side == Side.Player)
                {
                    unitIconObject = Instantiate(SpriteManager.Instance.RedUnitIconPrefab, mainUnitLayer.transform);
                }
                else
                {
                    unitIconObject = Instantiate(SpriteManager.Instance.BlueUnitIconPrefab, mainUnitLayer.transform);
                }
                unitIconObject.name = $"Unit_{unit.UnitID}_{unit.UnitName}";

                // Get prefab component
                Prefab_CombatUnitIcon unitIcon = unitIconObject.GetComponent<Prefab_CombatUnitIcon>();
                if (unitIcon == null)
                {
                    Debug.LogError($"{CLASS_NAME}.CreateUnitIcon: Prefab_CombatUnitIcon component not found on prefab!");
                    Destroy(unitIconObject);
                    return;
                }

                // Get sprite name for this unit
                string spriteName = GetSpriteNameForUnit(unit);

                // Check if this is an animated sprite
                if (spriteName.Contains("Frame", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Route to animated rendering method
                    // For now, just use the static sprite (Frame0 is already in spriteName)
                    if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Unit uses animated sprite: {spriteName}");
                }

                // Set the unit icon sprite
                unitIcon.SetUnitIcon(spriteName);

                // TODO: Set NATO icon based on Classification/Role
                // unitIcon.SetNatoIcon(GetNatoIcon(unit.Classification));

                // TODO: Set hit points ratio text
                // unitIcon.HitPointsRatio = $"{unit.HitPoints.Current:F0}/{unit.HitPoints.Max:F0}";

                // Position the prefab
                UnityEngine.Vector3 position = GetRenderPosition(new Vector2Int(unit.MapPos.IntX, unit.MapPos.IntY));
                unitIconObject.transform.position = position;

                // TODO: Apply outline rendering (dual-render approach)
                // This will be handled by a separate outline component/method
                // ApplyOutlineToUnit(unitIcon, outlineColor, outlineThickness);

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
        /// </summary>
        private string GetSpriteNameForUnit(CombatUnit unit)
        {
            try
            {
                // Handle embarked units separately
                if (unit.DeploymentPosition == DeploymentPosition.Embarked)
                {
                    return GetEmbarkedSpriteName(unit);
                }

                // Get base sprite name from DeployedProfileID
                string baseSpriteName = GetBaseSpriteFromProfile(unit.DeployedProfileID);

                // Add directional suffix based on unit facing
                string directionalSuffix = unit.Facing == HexDirection.E ? "_E" : "_W";

                // Determine if we need _P (packed) variant based on deployment position
                string deploymentSuffix = GetDeploymentSuffix(unit.DeploymentPosition);

                // Combine: base + directional + deployment
                string finalSpriteName = baseSpriteName + directionalSuffix + deploymentSuffix;

                if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': Profile={unit.DeployedProfileID}, Sprite={finalSpriteName}");

                return finalSpriteName;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSpriteNameForUnit", e);
                // Return fallback sprite
                return "SV_Regulars"; // TODO: Define a better fallback
            }
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
                return "SV_AN8_Frame0"; // Air transport (animated helicopter, use Frame0 for static)
            }
            else
            {
                // TODO: Create naval transport sprite in SpriteManager
                // For now, return placeholder
                return "GEN_M35_E"; // Temporary - should be naval transport sprite
            }
        }

        /// <summary>
        /// Gets deployment suffix (_P for deployed positions, empty for mobile).
        /// </summary>
        private string GetDeploymentSuffix(DeploymentPosition position)
        {
            return position switch
            {
                DeploymentPosition.Mobile => "",      // Mobile = no _P suffix
                DeploymentPosition.Embarked => "",    // Handled separately
                _ => "_P"                              // Deployed/HastyDefense/Entrenched/Fortified = _P suffix
            };
        }

        /// <summary>
        /// Maps WeaponSystems enum to base sprite name (without directional/deployment suffixes).
        /// </summary>
        private string GetBaseSpriteFromProfile(WeaponSystems profile)
        {
            // TODO: Complete mapping for all WeaponSystems values
            // TODO: Handle missing sprites (many UK, French units don't have sprites)
            // TODO: Handle infantry profiles - need to use Nationality to pick correct sprite
            // TODO: Handle generic profiles properly

            return profile switch
            {
                // Soviet Tanks
                WeaponSystems.TANK_T55A => "SV_T55A",
                WeaponSystems.TANK_T64A => "SV_T64A",
                WeaponSystems.TANK_T64B => "SV_T64B",
                WeaponSystems.TANK_T72A => "SV_T72A",
                WeaponSystems.TANK_T72B => "SV_T72B",
                WeaponSystems.TANK_T80B => "SV_T80B",
                WeaponSystems.TANK_T80U => "SV_T80U",
                WeaponSystems.TANK_T80BV => "SV_T80BV",

                // Soviet APCs
                WeaponSystems.APC_MTLB => "SV_MTLB",
                WeaponSystems.APC_BTR70 => "SV_BTR70",
                WeaponSystems.APC_BTR80 => "SV_BTR80",

                // Soviet IFVs
                WeaponSystems.IFV_BMP1 => "SV_BMP1",
                WeaponSystems.IFV_BMP2 => "SV_BMP2",
                WeaponSystems.IFV_BMP3 => "SV_BMP3",
                // WeaponSystems.IFV_BMD1 => "SV_BMD2", // TODO: No BMD1 sprite, using BMD2 as fallback
                WeaponSystems.IFV_BMD2 => "SV_BMD2",
                WeaponSystems.IFV_BMD3 => "SV_BMD3",

                // Soviet Recon
                WeaponSystems.RCN_BRDM2 => "SV_BRDM2",
                WeaponSystems.RCN_BRDM2AT => "SV_BRDM2AT",

                // Soviet Self-Propelled Artillery (these have _P variants)
                WeaponSystems.SPA_2S1 => "SV_2S1",
                WeaponSystems.SPA_2S3 => "SV_2S3",
                WeaponSystems.SPA_2S5 => "SV_2S5",
                WeaponSystems.SPA_2S19 => "SV_2S19",

                // Soviet Rocket Artillery (these have _P variants)
                WeaponSystems.ROC_BM21 => "SV_BM21",
                WeaponSystems.ROC_BM27 => "SV_BM27",
                WeaponSystems.ROC_BM30 => "SV_BM30",

                // Soviet SSM (has _P variant)
                WeaponSystems.SSM_SCUD => "SV_ScudB",

                // Soviet SPAAA (some have _P variants)
                WeaponSystems.SPAAA_ZSU57 => "SV_ZSU57",
                WeaponSystems.SPAAA_ZSU23 => "SV_ZSU23",
                WeaponSystems.SPAAA_2K22 => "SV_2K22",

                // Soviet SPSAM (has _P variant)
                WeaponSystems.SPSAM_9K31 => "SV_9K31",

                // Soviet SAM (some have _P variants)
                WeaponSystems.SAM_S75 => "SV_S75",
                WeaponSystems.SAM_S125 => "SV_S125",
                WeaponSystems.SAM_S300 => "SV_S300",

                // Soviet Helicopters (animated - use Frame0)
                WeaponSystems.TRANHEL_MI8T => "SV_MI8T_Frame0",
                WeaponSystems.HEL_MI8AT => "SV_MI8_Frame0",
                WeaponSystems.HEL_MI24D => "SV_MI24D_Frame0",
                WeaponSystems.HEL_MI24V => "SV_MI24V_Frame0",
                WeaponSystems.HEL_MI28 => "SV_MI28_Frame0",

                // Soviet Transport Aircraft (animated)
                // WeaponSystems.TRANAIR_AN12 => "SV_AN8_Frame0", // TODO: No AN12 sprite, using AN8

                // Soviet AWACS
                // WeaponSystems.AWACS_A50 => "SV_AN50", // TODO: Verify if AN50 is same as A50

                // Soviet Fighters
                WeaponSystems.FGT_MIG21 => "SV_MIG21",
                WeaponSystems.FGT_MIG23 => "SV_MIG23",
                WeaponSystems.FGT_MIG25 => "SV_MIG25",
                WeaponSystems.FGT_MIG29 => "SV_MIG29",
                WeaponSystems.FGT_MIG31 => "SV_MIG31",
                WeaponSystems.FGT_SU27 => "SV_SU27",
                WeaponSystems.FGT_SU47 => "SV_SU47",
                WeaponSystems.FGT_MIG27 => "SV_MIG27",

                // Soviet Attack Aircraft
                WeaponSystems.ATT_SU25 => "SV_SU25",
                WeaponSystems.ATT_SU25B => "SV_SU25B",

                // Soviet Bombers
                WeaponSystems.BMB_SU24 => "SV_SU24",
                WeaponSystems.BMB_TU16 => "SV_TU16",
                WeaponSystems.BMB_TU22 => "SV_TU22",
                WeaponSystems.BMB_TU22M3 => "SV_TU22M3",

                // TODO: Add USA units
                // TODO: Add German units
                // TODO: Add UK units
                // TODO: Add French units
                // TODO: Add Generic units
                // TODO: Add Infantry profiles (need to map based on Nationality)

                // Fallback
                _ => "SV_Regulars" // Default fallback sprite
            };
        }

        #endregion // Private Methods - Unit Icons
    }
}
