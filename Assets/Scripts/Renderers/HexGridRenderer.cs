using System;
using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Patterns;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using UnityEngine;
using CoreTextSize = HammerAndSickle.Core.TextSize;
using CoreFontWeight = HammerAndSickle.Core.FontWeight;

namespace HammerAndSickle.Renderers
{
    /// <summary>
    /// Layer-based hex map renderer. Replaces HexMapRenderer entirely.
    /// Owns 12 HexLayer instances, manages prefab drawing for cities/bridges/icons/text,
    /// handles event subscriptions, and delegates all position math to HexGridSystem.
    /// </summary>
    public class HexGridRenderer : Singleton<HexGridRenderer>
    {
        private const string CLASS_NAME = nameof(HexGridRenderer);

        #region Serialized Fields

        [Header("Map Layers (Sorting Layer: Map)")]
        [SerializeField] private HexLayer hexOutlineLayer;
        [SerializeField] private HexLayer hexSelectLayer;
        [SerializeField] private HexLayer mapIconLayer;
        [SerializeField] private HexLayer bridgeIconLayer;
        [SerializeField] private HexLayer cityIconLayer;
        [SerializeField] private HexLayer mapTextLayer;

        [Header("Unit Layers (Sorting Layer: Units)")]
        [SerializeField] private HexLayer groundUnitLayer;
        [SerializeField] private HexLayer airUnitLayer;

        [Header("Overlay Layers (Sorting Layer: Overlay)")]
        [SerializeField] private HexLayer utility1Layer;
        [SerializeField] private HexLayer utility2Layer;
        [SerializeField] private HexLayer movementRangeLayer;
        [SerializeField] private HexLayer movementPathLayer;

        [Header("Movement Overlay Colors")]
        [SerializeField] private Color movementRangeColor = new(0.2f, 0.8f, 0.2f, 0.25f);
        [SerializeField] private Color zocTerminalColor = new(1.0f, 0.5f, 0.0f, 0.35f);
        [SerializeField] private Color pathPreviewColor = new(0.2f, 0.4f, 1.0f, 0.3f);

        [Header("Debug")]
        [SerializeField] private bool _debug;

        #endregion // Serialized Fields

        #region Fields

        private Sprite _hexFillSprite;
        private bool isRenderMapLabels = true;

        // Prefab tracking dictionaries (migrated from HexMapRenderer)
        private readonly Dictionary<Vector2Int, Prefab_CityIcon> cityPrefabs = new();
        private readonly Dictionary<string, Prefab_BridgeIcon> bridgePrefabs = new();
        private readonly Dictionary<Vector2Int, Prefab_MapIcon> mapIconPrefabs = new();
        private readonly Dictionary<Vector2Int, Prefab_MapText> textLabelPrefabs = new();

        #endregion // Fields

        #region Properties

        /// <summary>True when all required layers are assigned.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Transform for parenting ground unit prefabs.</summary>
        public Transform GroundUnitLayerTransform => groundUnitLayer != null ? groundUnitLayer.transform : null;

        /// <summary>Transform for parenting air unit prefabs.</summary>
        public Transform AirUnitLayerTransform => airUnitLayer != null ? airUnitLayer.transform : null;

        /// <summary>Transform for parenting utility prefabs (slot 1).</summary>
        public Transform Utility1LayerTransform => utility1Layer != null ? utility1Layer.transform : null;

        /// <summary>Transform for parenting utility prefabs (slot 2).</summary>
        public Transform Utility2LayerTransform => utility2Layer != null ? utility2Layer.transform : null;

        /// <summary>Gets or sets whether map labels are rendered. Setting refreshes the map.</summary>
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

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this) ValidateLayers();
        }

        private void Start()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"{CLASS_NAME}.Start: Failed to initialize properly.");
                return;
            }
            SubscribeToEvents();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
            base.OnDestroy();
        }

        #endregion // Unity Lifecycle

        #region Initialization

        private void ValidateLayers()
        {
            try
            {
                if (hexOutlineLayer == null) throw new NullReferenceException($"{nameof(hexOutlineLayer)} not assigned.");
                if (hexSelectLayer == null) throw new NullReferenceException($"{nameof(hexSelectLayer)} not assigned.");
                if (mapIconLayer == null) throw new NullReferenceException($"{nameof(mapIconLayer)} not assigned.");
                if (bridgeIconLayer == null) throw new NullReferenceException($"{nameof(bridgeIconLayer)} not assigned.");
                if (cityIconLayer == null) throw new NullReferenceException($"{nameof(cityIconLayer)} not assigned.");
                if (mapTextLayer == null) throw new NullReferenceException($"{nameof(mapTextLayer)} not assigned.");
                if (groundUnitLayer == null) throw new NullReferenceException($"{nameof(groundUnitLayer)} not assigned.");
                if (airUnitLayer == null) throw new NullReferenceException($"{nameof(airUnitLayer)} not assigned.");
                if (movementRangeLayer == null) throw new NullReferenceException($"{nameof(movementRangeLayer)} not assigned.");
                if (movementPathLayer == null) throw new NullReferenceException($"{nameof(movementPathLayer)} not assigned.");

                IsInitialized = true;
                if (_debug) Debug.Log($"[{CLASS_NAME}] All layers validated.");
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateLayers), ex);
                IsInitialized = false;
            }
        }

        #endregion // Initialization

        #region Event Management

        private void SubscribeToEvents()
        {
            if (HexDetectionService.Instance != null)
            {
                HexDetectionService.Instance.OnHexSelected += HandleHexSelected;
                if (_debug) Debug.Log($"[{CLASS_NAME}.SubscribeToEvents] Subscribed to HexDetectionService.");
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnMovementRangeComputed += HandleMovementRangeComputed;
                EventManager.Instance.OnMovementRangeCleared += ClearMovementRange;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (HexDetectionService.Instance != null)
                HexDetectionService.Instance.OnHexSelected -= HandleHexSelected;

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnMovementRangeComputed -= HandleMovementRangeComputed;
                EventManager.Instance.OnMovementRangeCleared -= ClearMovementRange;
            }
        }

        private void HandleHexSelected(Position2D hexPosition)
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.HandleHexSelected] Hex selected at ({hexPosition.IntX}, {hexPosition.IntY})");

                if (GameDataManager.CurrentHexMap != null && hexPosition != GameDataManager.NoHexSelected)
                {
                    HexTile selectedHex = GameDataManager.CurrentHexMap.GetHexAt(hexPosition);
                    if (selectedHex != null)
                    {
                        GameDataManager.SelectedHexData = selectedHex;
                    }
                }

                DrawHexSelector();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleHexSelected), e);
            }
        }

        private void HandleMovementRangeComputed(CombatUnit unit, Dictionary<Position2D, int> reachable)
        {
            try
            {
                HashSet<Position2D> zocTerminals = null;
                if (MovementController.Instance != null)
                    zocTerminals = new HashSet<Position2D>();

                ShowMovementRange(reachable, zocTerminals);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleMovementRangeComputed), e);
            }
        }

        #endregion // Event Management

        #region Public API — Position

        /// <summary>
        /// Gets the world position for a hex grid coordinate.
        /// </summary>
        public Vector3 GetRenderPosition(Vector2Int gridPos)
        {
            return HexGridSystem.Instance.HexToWorld(new Position2D(gridPos.x, gridPos.y));
        }

        #endregion // Public API — Position

        #region Public API — Full Map Refresh

        /// <summary>
        /// Clears and redraws all map layers (outlines, cities, bridges, icons, text).
        /// </summary>
        public void RefreshMap()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Starting map refresh...");

                if (GameDataManager.CurrentHexMap == null)
                {
                    AppService.CaptureUiMessage("Cannot refresh map: No hex map loaded.");
                    return;
                }

                // Clear tracking dictionaries
                cityPrefabs.Clear();
                bridgePrefabs.Clear();
                mapIconPrefabs.Clear();
                textLabelPrefabs.Clear();

                // Clear visual layers
                hexOutlineLayer.Clear();
                ClearContainer(mapIconLayer.transform);
                ClearContainer(bridgeIconLayer.transform);
                ClearContainer(cityIconLayer.transform);
                ClearContainer(mapTextLayer.transform);

                DrawHexOutlines();

                int hexCount = 0, cityCount = 0, iconCount = 0, bridgeCount = 0, labelCount = 0;

                foreach (var hex in GameDataManager.CurrentHexMap)
                {
                    if (hex == null) continue;
                    hexCount++;

                    int prev = mapIconPrefabs.Count;
                    DrawMapIconsForHex(hex);
                    if (mapIconPrefabs.Count > prev) iconCount++;

                    prev = cityPrefabs.Count;
                    DrawCityIconForHex(hex);
                    if (cityPrefabs.Count > prev) cityCount++;

                    prev = bridgePrefabs.Count;
                    DrawBridgesForHex(hex);
                    bridgeCount += bridgePrefabs.Count - prev;

                    if (isRenderMapLabels)
                    {
                        prev = textLabelPrefabs.Count;
                        DrawTextLabelsForHex(hex);
                        if (textLabelPrefabs.Count > prev) labelCount++;
                    }
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshMap] Complete. {hexCount} hexes, {cityCount} cities, {iconCount} icons, {bridgeCount} bridges, {labelCount} labels.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RefreshMap), e);
            }
        }

        #endregion // Public API — Full Map Refresh

        #region Public API — Hex Selection

        /// <summary>
        /// Draws the selection highlight on the currently selected hex. Clears previous selection.
        /// </summary>
        public void DrawHexSelector()
        {
            try
            {
                hexSelectLayer.Clear();

                Position2D selectedHex = GameDataManager.SelectedHex;
                if (selectedHex != GameDataManager.NoHexSelected)
                {
                    var worldPos = HexGridSystem.Instance.HexToWorld(selectedHex);
                    var sprite = SpriteManager.GetSprite(SpriteManager.HexSelectOutline);
                    hexSelectLayer.SetSprite($"sel_{selectedHex.IntX}_{selectedHex.IntY}", sprite, worldPos, Color.white);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(DrawHexSelector), e);
            }
        }

        #endregion // Public API — Hex Selection

        #region Public API — Control Flag

        /// <summary>
        /// Updates the control flag for a city at the specified position.
        /// </summary>
        public void ChangeControlFlag(Position2D position)
        {
            try
            {
                if (GameDataManager.CurrentHexMap == null) return;

                HexTile hex = GameDataManager.CurrentHexMap.GetHexAt(position);
                if (hex == null) return;

                Vector2Int pos = new(position.IntX, position.IntY);
                if (cityPrefabs.TryGetValue(pos, out Prefab_CityIcon cityPrefab))
                {
                    cityPrefab.UpdateControlFlag(hex.TileControl, hex.DefaultTileControl);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ChangeControlFlag), e);
            }
        }

        #endregion // Public API — Control Flag

        #region Public API — Movement Overlays

        /// <summary>
        /// Populates the movement range overlay layer.
        /// </summary>
        public void ShowMovementRange(Dictionary<Position2D, int> reachable, HashSet<Position2D> zocTerminals)
        {
            try
            {
                movementRangeLayer.Clear();
                var sprite = GetOrCreateHexFillSprite();

                foreach (var kvp in reachable)
                {
                    var color = (zocTerminals != null && zocTerminals.Contains(kvp.Key))
                        ? zocTerminalColor
                        : movementRangeColor;

                    var worldPos = HexGridSystem.Instance.HexToWorld(kvp.Key);
                    movementRangeLayer.SetSprite($"mr_{kvp.Key.IntX}_{kvp.Key.IntY}", sprite, worldPos, color);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ShowMovementRange), e);
            }
        }

        /// <summary>Clears the movement range overlay.</summary>
        public void ClearMovementRange()
        {
            try { movementRangeLayer.Clear(); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(ClearMovementRange), e); }
        }

        /// <summary>Populates the path preview overlay layer.</summary>
        public void ShowPathPreview(List<Position2D> path)
        {
            try
            {
                movementPathLayer.Clear();
                var sprite = GetOrCreateHexFillSprite();

                foreach (var pos in path)
                {
                    var worldPos = HexGridSystem.Instance.HexToWorld(pos);
                    movementPathLayer.SetSprite($"pp_{pos.IntX}_{pos.IntY}", sprite, worldPos, pathPreviewColor);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ShowPathPreview), e);
            }
        }

        /// <summary>Clears the path preview overlay.</summary>
        public void ClearPathPreview()
        {
            try { movementPathLayer.Clear(); }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(ClearPathPreview), e); }
        }

        #endregion // Public API — Movement Overlays

        #region Private Methods — Hex Outlines

        private void DrawHexOutlines()
        {
            var grid = HexGridSystem.Instance;
            if (!grid.IsInitialized) return;

            string spriteName = GetOutlineSpriteName();
            var sprite = SpriteManager.GetSprite(spriteName);
            int count = 0;

            for (int y = 0; y < grid.MapHeight; y++)
            {
                for (int x = 0; x < grid.MapWidth; x++)
                {
                    var pos = new Position2D(x, y);
                    if (!grid.IsInBounds(pos)) continue;

                    var worldPos = grid.HexToWorld(pos);
                    hexOutlineLayer.SetSprite($"outline_{x}_{y}", sprite, worldPos, Color.white);
                    count++;
                }
            }

            if (_debug) Debug.Log($"[{CLASS_NAME}.DrawHexOutlines] Drew {count} outlines.");
        }

        private static string GetOutlineSpriteName()
        {
            return GameDataManager.CurrentHexOutlineColor switch
            {
                HexOutlineColor.Black => SpriteManager.BlackHexOutline,
                HexOutlineColor.White => SpriteManager.WhiteHexOutline,
                _ => SpriteManager.GreyHexOutline
            };
        }

        #endregion // Private Methods — Hex Outlines

        #region Private Methods — Map Icons

        private void DrawMapIconsForHex(HexTile hex)
        {
            try
            {
                if (hex.IsAirbase) CreateMapIcon(hex, MapIconType.Airbase);
                else if (hex.IsFort) CreateMapIcon(hex, MapIconType.Fort);
                else if (hex.UrbanDamage > 0) CreateMapIcon(hex, MapIconType.UrbanSprawl);
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawMapIconsForHex), e); }
        }

        private void CreateMapIcon(HexTile hex, MapIconType iconType)
        {
            GameObject obj = UnityEngine.Object.Instantiate(SpriteManager.Instance.MapIconPrefab, mapIconLayer.transform);
            obj.name = $"{iconType}_{hex.Position.IntX}_{hex.Position.IntY}";

            var prefab = obj.GetComponent<Prefab_MapIcon>();
            prefab.SetIconType(iconType);
            prefab.SetPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));

            string spriteName = iconType switch
            {
                MapIconType.Airbase => SpriteManager.GEN_Airbase,
                MapIconType.Fort => SpriteManager.GEN_Fort,
                _ => SpriteManager.ME_Sprawl
            };
            prefab.GetSpriteRenderer().sprite = SpriteManager.GetSprite(spriteName);

            obj.transform.position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));
            mapIconPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = prefab;
        }

        #endregion // Private Methods — Map Icons

        #region Private Methods — City Icons

        private void DrawCityIconForHex(HexTile hex)
        {
            try
            {
                if (hex.Terrain == TerrainType.MajorCity || hex.Terrain == TerrainType.MinorCity)
                    CreateCityIcon(hex, hex.Terrain);
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawCityIconForHex), e); }
        }

        private void CreateCityIcon(HexTile hex, TerrainType terrain)
        {
            GameObject obj = UnityEngine.Object.Instantiate(SpriteManager.Instance.CityPrefab, cityIconLayer.transform);
            obj.name = $"City_{hex.Position.IntX}_{hex.Position.IntY}";

            var prefab = obj.GetComponent<Prefab_CityIcon>();

            obj.transform.position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));

            prefab.UpdateCityIcon(terrain, GameDataManager.CurrentMapTheme);
            prefab.UpdateNameplate(GameDataManager.CurrentMapTheme);
            prefab.UpdateControlFlag(hex.TileControl, hex.DefaultTileControl);
            prefab.UpdateCityName(hex.TileLabel);
            prefab.UpdateObjectiveStatus(hex.IsObjective);

            cityPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = prefab;
        }

        #endregion // Private Methods — City Icons

        #region Private Methods — Bridges

        private void DrawBridgesForHex(HexTile hex)
        {
            try
            {
                DrawBridgeInDirection(hex, HexDirection.E, hex.BridgeBorders.East, hex.DamagedBridgeBorders.East, hex.PontoonBridgeBorders.East);
                DrawBridgeInDirection(hex, HexDirection.SE, hex.BridgeBorders.Southeast, hex.DamagedBridgeBorders.Southeast, hex.PontoonBridgeBorders.Southeast);
                DrawBridgeInDirection(hex, HexDirection.SW, hex.BridgeBorders.Southwest, hex.DamagedBridgeBorders.Southwest, hex.PontoonBridgeBorders.Southwest);
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawBridgesForHex), e); }
        }

        private void DrawBridgeInDirection(HexTile hex, HexDirection direction, bool hasRegular, bool hasDamaged, bool hasPontoon)
        {
            if (hasRegular) CreateBridgeObject(hex, BridgeType.Regular, direction);
            if (hasDamaged) CreateBridgeObject(hex, BridgeType.DamagedRegular, direction);
            if (hasPontoon) CreateBridgeObject(hex, BridgeType.Pontoon, direction);
        }

        private void CreateBridgeObject(HexTile hex, BridgeType bridgeType, HexDirection direction)
        {
            HexTile neighbor = hex.GetNeighbor(direction);
            if (neighbor == null) return;

            string bridgeKey = $"{hex.Position.IntX}_{hex.Position.IntY}_{(int)direction}_{(int)bridgeType}";
            if (bridgePrefabs.ContainsKey(bridgeKey)) return;

            GameObject obj = UnityEngine.Object.Instantiate(SpriteManager.Instance.BridgeIconPrefab, bridgeIconLayer.transform);
            obj.name = $"{bridgeType}_{direction}_{hex.Position.IntX}_{hex.Position.IntY}";

            var prefab = obj.GetComponent<Prefab_BridgeIcon>();
            prefab.Type = bridgeType;
            prefab.Dir = direction;
            prefab.Pos = hex.Position.ToVector2Int();
            prefab.Renderer.sprite = GetBridgeSprite(bridgeType, direction);

            obj.transform.position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));
            bridgePrefabs[bridgeKey] = prefab;
        }

        private static Sprite GetBridgeSprite(BridgeType bridgeType, HexDirection direction)
        {
            string spriteName = direction switch
            {
                HexDirection.NE => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeNE,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeNE,
                    _ => SpriteManager.PontBridgeNE
                },
                HexDirection.E => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeE,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeE,
                    _ => SpriteManager.PontBridgeE
                },
                HexDirection.SE => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeSE,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeSE,
                    _ => SpriteManager.PontBridgeSE
                },
                HexDirection.SW => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeSW,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeSW,
                    _ => SpriteManager.PontBridgeSW
                },
                HexDirection.W => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeW,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeW,
                    _ => SpriteManager.PontBridgeW
                },
                _ => bridgeType switch
                {
                    BridgeType.Regular => SpriteManager.BridgeNW,
                    BridgeType.DamagedRegular => SpriteManager.DamagedBridgeNW,
                    _ => SpriteManager.PontBridgeNW
                }
            };
            return SpriteManager.GetSprite(spriteName);
        }

        #endregion // Private Methods — Bridges

        #region Private Methods — Text Labels

        private void DrawTextLabelsForHex(HexTile hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex.LargeTileLabel)) return;
                CreateTextLabel(hex, hex.LargeTileLabel);
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawTextLabelsForHex), e); }
        }

        private void CreateTextLabel(HexTile hex, string labelText)
        {
            GameObject obj = UnityEngine.Object.Instantiate(SpriteManager.Instance.MapTextPrefab, mapTextLayer.transform);
            obj.name = $"TextLabel_{hex.Position.IntX}_{hex.Position.IntY}";

            var prefab = obj.GetComponent<Prefab_MapText>();

            CoreTextSize coreTextSize = hex.LabelSize switch
            {
                HammerAndSickle.Core.GameData.TextSize.Small => CoreTextSize.Small,
                HammerAndSickle.Core.GameData.TextSize.Medium => CoreTextSize.Medium,
                HammerAndSickle.Core.GameData.TextSize.Large => CoreTextSize.Large,
                _ => CoreTextSize.Medium
            };

            CoreFontWeight coreFontWeight = hex.LabelWeight switch
            {
                HammerAndSickle.Core.GameData.FontWeight.Light => CoreFontWeight.Light,
                HammerAndSickle.Core.GameData.FontWeight.Medium => CoreFontWeight.Medium,
                HammerAndSickle.Core.GameData.FontWeight.Bold => CoreFontWeight.Bold,
                _ => CoreFontWeight.Medium
            };

            prefab.SetText(labelText);
            prefab.SetSize(coreTextSize);
            prefab.SetFont(coreFontWeight);
            prefab.SetColor(hex.LabelColor);
            prefab.SetOutlineThickness(hex.LabelOutlineThickness);

            obj.transform.position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));
            textLabelPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = prefab;
        }

        #endregion // Private Methods — Text Labels

        #region Private Methods — Utilities

        private void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        #endregion // Private Methods — Utilities

        #region Private Methods — Runtime Sprite Generation

        /// <summary>
        /// Generates a hex-shaped filled sprite at runtime for overlay tinting.
        /// </summary>
        private Sprite GetOrCreateHexFillSprite()
        {
            if (_hexFillSprite != null) return _hexFillSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float cx = size / 2f;
            float cy = size / 2f;
            float radius = size / 2f - 2f;

            var verts = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i - 30f);
                verts[i] = new Vector2(cx + radius * Mathf.Cos(angle), cy + radius * Mathf.Sin(angle));
            }

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    var p = new Vector2(px, py);
                    bool inside = false;
                    for (int i = 0; i < 6; i++)
                    {
                        if (PointInTriangle(p, new Vector2(cx, cy), verts[i], verts[(i + 1) % 6]))
                        {
                            inside = true;
                            break;
                        }
                    }
                    tex.SetPixel(px, py, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            _hexFillSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _hexFillSprite;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        #endregion // Private Methods — Runtime Sprite Generation
    }
}
