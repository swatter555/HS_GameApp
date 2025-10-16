using System;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.Tilemaps;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Legacy.Map;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using HammerAndSickle.Models;

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

        #endregion

        #region Fields
        /// <summary>
        /// Dictionary to store and track city prefab instances by their hex coordinates.
        /// </summary>
        private readonly Dictionary<Vector2Int, Prefab_CityIcon> cityPrefabs = new();

        /// <summary>
        /// Dictionary to store and track bridge prefab instances by their border position key.
        /// Key format: "minX_minY_maxX_maxY_direction"
        /// </summary>
        private readonly Dictionary<string, Prefab_BridgeIcon> bridgePrefabs = new();

        /// <summary>
        /// Dictionary to store and track map icon prefab instances by their hex coordinates.
        /// </summary>
        private readonly Dictionary<Vector2Int, Prefab_MapIcon> mapIconPrefabs = new();

        #endregion

        #region Inspector Fields

        [Header("Rendering Tilemaps")]
        [SerializeField]
        private Tilemap hexOutlineTilemap;
        [SerializeField]
        private Tilemap hexSelectionTilemap;

        [Header("Rendering Layers")]
        [SerializeField]
        private GameObject mapIconLayer;
        [SerializeField]
        private GameObject bridgeIconLayer;
        [SerializeField]
        private GameObject cityIconLayer;
        [SerializeField]
        private GameObject textLabelLayer;

        #endregion

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

        #endregion

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
                Debug.LogError($"{GetType().Name}.Start: Service failed to initialize properly.");
                return;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the renderer service. Validates required components and
        /// sets up initial state. Called during Awake for the singleton instance.
        /// </summary>
        private void InitializeService()
        {
            try
            {
                ValidateComponents();
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(GetType().Name, "InitializeService", ex);
                IsInitialized = false;
            }
        }

        /// <summary>
        /// Validates that all required components are properly referenced.
        /// Throws exceptions if any required components are missing.
        /// </summary>
        private void ValidateComponents()
        {
            if (hexOutlineTilemap == null)
                throw new NullReferenceException($"{GetType().Name}.ValidateComponents: {nameof(hexOutlineTilemap)} TileRenderer is missing.");

            if (hexSelectionTilemap == null)
                throw new NullReferenceException($"{GetType().Name}.ValidateComponents: {nameof(hexSelectionTilemap)} TileRenderer is missing.");

            if (mapIconLayer == null)
                throw new NullReferenceException($"{GetType().Name}.ValidateComponents: {nameof(mapIconLayer)} TileRenderer is missing.");

            if (cityIconLayer == null)
                throw new NullReferenceException($"{GetType().Name}.ValidateComponents: {nameof(cityIconLayer)} GameObject is missing.");

            if (bridgeIconLayer == null)
                throw new NullReferenceException($"{GetType().Name}.ValidateComponents: {nameof(bridgeIconLayer)} GameObject is missing.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the world position of a hex tile based on its grid coordinates.
        /// </summary>
        public Vector3 GetRenderPosition(Vector2Int gridPos)
        {
            // Check if the service is initialized.
            if (IsInitialized)
            {
                // Get the world position of the cell center.
                return hexOutlineTilemap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
            }
            else throw new InvalidOperationException($"{GetType().Name}.GetTilemapRenderPosition: Service not initialized.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the map layers based on current map data.
        /// </summary>
        private void UpdateMapLayers()
        {
            // Clear existing map icons.
            ClearContainer(mapIconLayer.transform);
            ClearContainer(bridgeIconLayer.transform);
            ClearContainer(cityIconLayer.transform);

            // Draw the map and bridge icons.
            DrawMapIcons();

            // Draw the city icons.
            DrawCityIcons();
        }

        /// <summary>
        /// Draws hex outlines based on current map configuration and outline color settings.
        /// </summary>
        private void DrawHexOutlines()
        {
            try
            {
                // Clear existing hex outlines.
                hexOutlineTilemap.ClearAllTiles();

                // Get the appropriate sprite based on outline color
                string spriteName = GetOutlineSpriteName();
                Sprite sprite = SpriteManager.Instance.GetSprite(AtlasTypes.HexOutlineIcons, spriteName);

                // Create and configure the tile
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;

                // Draw tiles for each valid position
                for (int x = 0; x < GameDataManager.CurrentMapSize.Width; x++)
                {
                    for (int y = 0; y < GameDataManager.CurrentMapSize.Height; y++)
                    {
                        if (IsValidHexPosition(x, y))
                        {
                            hexOutlineTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawHexOutlines", e);
            }
        }

        /// <summary>
        /// Draws the hex selector outline on the map.
        /// </summary>
        private void DrawHexSelector()
        {
            try
            {
                // Clear existing hex selector.
                hexSelectionTilemap.ClearAllTiles();

                // Check if a hex is selected.
                Position2D selectedHex = GameDataManager.SelectedHex;
                if (selectedHex != GameDataManager.NoHexSelected)
                {
                    // Create and configure the tile
                    Tile tile = ScriptableObject.CreateInstance<Tile>();

                    // Draw the selection outline.
                    tile.sprite = SpriteManager.Instance.GetSprite(AtlasTypes.HexOutlineIcons, SpriteManager.HexSelectOutline);

                    // Set the tile at the position.
                    hexSelectionTilemap.SetTile(new Vector3Int(selectedHex.IntX, selectedHex.IntY, 0), tile);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawHexSelector", e);
            }
        }

        /// <summary>
        /// Draws hex icons based on current hex map data. Places icons for airbases and forts.
        /// </summary>
        private void DrawMapIcons()
        {
            try
            {
                // Iterate through the hex map
                for (int x = 0; x < GameDataManager.CurrentMapSize.Width; x++)
                {
                    for (int y = 0; y < GameDataManager.CurrentMapSize.Height; y++)
                    {
                        HexTile hex = GameDataManager.CurrentHexMap.GetHexAt(new Position2D(x, y));

                        // Check if this hex should have an icon.
                        if (hex.IsAirbase)
                        {
                            // Create airbase prefab instance,
                            GameObject mapIconObject = Instantiate(SpriteManager.Instance.MapIconPrefab, mapIconLayer.transform);

                            // Set the name of the object.
                            mapIconObject.name = $"Airbase_{x}_{y}";
                            Prefab_MapIcon mapIconPrefab = mapIconObject.GetComponent<Prefab_MapIcon>();

                            // Set the icon type and position.
                            mapIconPrefab.SetIconType(MapIconType.Airbase);
                            mapIconPrefab.SetPosition(new Vector2Int(x, y));

                            // Get the correct sprite from the atlas.
                            mapIconPrefab.GetSpriteRenderer().sprite = SpriteManager.Instance.GetThemedSprite(GameDataManager.CurrentMapTheme, ThemedSpriteTypes.Airbase);

                            // Position the prefab (adjust these values based on your grid spacing)
                            mapIconObject.transform.position = GetRenderPosition(new Vector2Int(x, y));
                        }
                        else if (hex.IsFort)
                        {
                            // Create airbase prefab instance,
                            GameObject mapIconObject = Instantiate(SpriteManager.Instance.MapIconPrefab, mapIconLayer.transform);

                            // Set the name of the object.
                            mapIconObject.name = $"Fort_{x}_{y}";
                            Prefab_MapIcon mapIconPrefab = mapIconObject.GetComponent<Prefab_MapIcon>();

                            // Set the icon type and position.
                            mapIconPrefab.SetIconType(MapIconType.Fort);
                            mapIconPrefab.SetPosition(new Vector2Int(x, y));

                            // Get the correct sprite from the atlas.
                            mapIconPrefab.GetSpriteRenderer().sprite = SpriteManager.Instance.GetThemedSprite(GameDataManager.CurrentMapTheme, ThemedSpriteTypes.Fort);

                            // Position the prefab (adjust these values based on your grid spacing)
                            mapIconObject.transform.position = GetRenderPosition(new Vector2Int(x, y));

                            // Store the prefab reference.
                            mapIconPrefabs[new Vector2Int(x, y)] = mapIconPrefab;
                        }

                        // Draw bridges.
                        DrawBridges(hex);
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(GetType().Name, "DrawHexSelector", e);
            }
        }

        /// <summary>
        /// Draws map icons based on current hex map data. Places icons for cities and airbases.
        /// </summary>
        private void DrawCityIcons()
        {
            try
            {
                // Iterate through the hex map
                for (int x = 0; x < GameDataManager.CurrentHexMap.MapSize.x; x++)
                {
                    for (int y = 0; y < GameDataManager.CurrentHexMap.MapSize.y; y++)
                    {
                        HexTile hexTile = GameDataManager.CurrentHexMap.GetHexAt(new Position2D(x, y));
                        TerrainType terrain = hexTile.Terrain;

                        // Check if this hex should have a city icon
                        if (terrain == TerrainType.MajorCity || terrain == TerrainType.MinorCity)
                        {
                            // Create city prefab instance
                            GameObject cityObj = Instantiate(SpriteManager.Instance.CityPrefab, cityIconLayer.transform);
                            cityObj.name = $"City_{x}_{y}";
                            Prefab_CityIcon cityPrefab = cityObj.GetComponent<Prefab_CityIcon>();

                            // Position the prefab (adjust these values based on your grid spacing)
                            Vector3 position = hexOutlineTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
                            cityObj.transform.position = position;

                            // Update the prefab's visual elements
                            cityPrefab.UpdateCityIcon(terrain, GameDataManager.CurrentMapTheme);
                            cityPrefab.UpdateNameplate(GameDataManager.CurrentMapTheme);
                            cityPrefab.UpdateControlFlag(hexTile.TileControl, hexTile.DefaultTileControl);
                            cityPrefab.UpdateCityName(hexTile.TileLabel);
                            cityPrefab.UpdateObjectiveStatus(hexTile.IsObjective);

                            // Store the prefab reference
                            cityPrefabs[new Vector2Int(x, y)] = cityPrefab;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(GetType().Name, "DrawCityIcons", e);
            }
        }

        /// <summary>
        /// Draws the bridge icons on the map.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="hex"></param>
        private void DrawBridges(HexTile hex)
        {
            try
            {
                // Draw the bridge icons.
                if (hex.BridgeBorders.Northwest)
                {
                    CreateBridgeObject(hex, BridgeType.Regular, HexDirection.NW);
                }
                if (hex.BridgeBorders.Northeast)
                {
                    CreateBridgeObject(hex, BridgeType.Regular, HexDirection.NE);
                }
                if (hex.BridgeBorders.East)
                {
                    CreateBridgeObject(hex, BridgeType.Regular, HexDirection.E);
                }
                if (hex.BridgeBorders.Southeast)
                {
                    CreateBridgeObject(hex, BridgeType.Regular, HexDirection.SE);
                }
                if (hex.BridgeBorders.Southwest)
                {
                    CreateBridgeObject(hex, BridgeType.Regular, HexDirection.SW);
                }
                if (hex.BridgeBorders.West)
                {
                    CreateBridgeObject(hex, BridgeType.Regular, HexDirection.W);
                }

                // Draw damaged bridges icons.
                if (hex.DamagedBridgeBorders.Northwest)
                {
                    CreateBridgeObject(hex, BridgeType.DamagedRegular, HexDirection.NW);
                }
                if (hex.DamagedBridgeBorders.Northeast)
                {
                    CreateBridgeObject(hex, BridgeType.DamagedRegular, HexDirection.NE);
                }
                if (hex.DamagedBridgeBorders.East)
                {
                    CreateBridgeObject(hex, BridgeType.DamagedRegular, HexDirection.E);
                }
                if (hex.DamagedBridgeBorders.Southeast)
                {
                    CreateBridgeObject(hex, BridgeType.DamagedRegular, HexDirection.SE);
                }
                if (hex.DamagedBridgeBorders.Southwest)
                {
                    CreateBridgeObject(hex, BridgeType.DamagedRegular, HexDirection.SW);
                }
                if (hex.DamagedBridgeBorders.West)
                {
                    CreateBridgeObject(hex, BridgeType.DamagedRegular, HexDirection.W);
                }

                // Draw pontoon bridges icons.
                if (hex.PontoonBridgeBorders.Northwest)
                {
                    CreateBridgeObject(hex, BridgeType.Pontoon, HexDirection.NW);
                }
                if (hex.PontoonBridgeBorders.Northeast)
                {
                    CreateBridgeObject(hex, BridgeType.Pontoon, HexDirection.NE);
                }
                if (hex.PontoonBridgeBorders.East)
                {
                    CreateBridgeObject(hex, BridgeType.Pontoon, HexDirection.E);
                }
                if (hex.PontoonBridgeBorders.Southeast)
                {
                    CreateBridgeObject(hex, BridgeType.Pontoon, HexDirection.SE);
                }
                if (hex.PontoonBridgeBorders.Southwest)
                {
                    CreateBridgeObject(hex, BridgeType.Pontoon, HexDirection.SW);
                }
                if (hex.PontoonBridgeBorders.West)
                {
                    CreateBridgeObject(hex, BridgeType.Pontoon, HexDirection.W);
                }
            }
            catch (System.Exception e)
            {
                AppService.HandleException("HexMapRenderer", "DrawBridges", e);
            }
        }

        /// <summary>
        /// Creates a bridge icon object on the map.
        /// </summary>
        private void CreateBridgeObject(HexTile hex, BridgeType bridgeType, HexDirection direction)
        {
            // Generate the key
            string borderKey = GenerateBridgeBorderKey(hex, direction, bridgeType);
            if (string.IsNullOrEmpty(borderKey)) return;

            // Check if the swapped version exists
            string swappedKey = GetSwappedBridgeKey(borderKey);
            if (bridgePrefabs.ContainsKey(swappedKey))
            {
                return; // Bridge already exists from neighbor's perspective
            }

            // Create bridge prefab instance
            GameObject bridgeIconObject = Instantiate(SpriteManager.Instance.BridgeIconPrefab, bridgeIconLayer.transform);

            // Set the name of the object.
            bridgeIconObject.name = $"{direction} {bridgeType} {hex.Position.X}_{hex.Position.Y}";
            Prefab_BridgeIcon bridgeIconPrefab = bridgeIconObject.GetComponent<Prefab_BridgeIcon>();

            // Set the icon type and position.
            bridgeIconPrefab.Type = bridgeType;
            bridgeIconPrefab.Dir = direction;
            bridgeIconPrefab.Pos = hex.Position.ToVector2Int();

            // Set the appropriate sprite based on bridge type and direction
            bridgeIconPrefab.Renderer.sprite = GetBridgeSprite(bridgeType, direction);

            // Position the prefab
            Vector3 position = hexOutlineTilemap.GetCellCenterWorld(new Vector3Int(hex.Position.IntX, hex.Position.IntY, 0));
            bridgeIconObject.transform.position = position;

            // Store the prefab reference with the border key
            bridgePrefabs[borderKey] = bridgeIconPrefab;
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

            return SpriteManager.Instance.GetSprite(AtlasTypes.BridgeIcons, spriteName);
        }

        /// <summary>
        /// Generates a unique border key for bridge placement.
        /// </summary>
        private string GenerateBridgeBorderKey(HexTile currentHex, HexDirection direction, BridgeType bridgeType)
        {
            // Get neighbor hex
            HexTile neighborHex = currentHex.GetNeighbor(direction);

            // Check if the neighbor hex is valid
            if (neighborHex == null) return string.Empty;

            // Get the opposite direction for the neighbor's perspective
            HexDirection oppositeDirection = HexMapUtil.GetOppositeDirection(direction);

            // Generate first half (current hex)
            string currentHexKey = string.Format("{0:D3}{1:D3}{2}{3}",
                currentHex.Position.X,
                currentHex.Position.Y,
                (int)direction,
                (int)bridgeType);

            // Generate second half (neighbor hex)
            string neighborHexKey = string.Format("{0:D3}{1:D3}{2}{3}",
                neighborHex.Position.X,
                neighborHex.Position.Y,
                (int)oppositeDirection,
                (int)bridgeType);

            // Return the combined key
            return currentHexKey + neighborHexKey;
        }

        /// <summary>
        /// Gets the swapped version of a bridge key (swaps the two 8-character halves).
        /// </summary>
        private string GetSwappedBridgeKey(string originalKey)
        {
            if (string.IsNullOrEmpty(originalKey) || originalKey.Length != 16)
                return string.Empty;

            return originalKey.Substring(8, 8) + originalKey.Substring(0, 8);
        }

        /// <summary>
        /// Gets the appropriate sprite name based on current outline color setting.
        /// </summary>
        /// <returns>The sprite name to use for hex outlines.</returns>
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
        private bool IsValidHexPosition(int x, int y)
        {
            // Don't render the last column if row is odd (prevents rendering out of bounds)
            if (y % 2 != 0 && x >= GameDataManager.CurrentMapSize.X - 1)
                return false;

            return true;
        }

        /// <summary>
        /// Clears all child objects from the specified container.
        /// </summary>
        /// <param name="container"></param>
        private void ClearContainer(Transform container)
        {
            try
            {
                // Loop backwards because destroying objects will shift the indices
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    GameObject.Destroy(container.GetChild(i).gameObject);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException("ClassName", "ClearContainer", e);
            }
        }

        #endregion
    }
}