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
        [SerializeField] private HexLayer riverBankLayer;
        [SerializeField] private HexLayer riverWaterLayer;
        [SerializeField] private HexLayer roadLayer;
        [SerializeField] private HexLayer bridgeIconLayer;
        [SerializeField] private HexLayer cityIconLayer;
        [SerializeField] private HexLayer impassableLayer;
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
                if (riverBankLayer == null) throw new NullReferenceException($"{nameof(riverBankLayer)} not assigned.");
                if (riverWaterLayer == null) throw new NullReferenceException($"{nameof(riverWaterLayer)} not assigned.");
                if (roadLayer == null) throw new NullReferenceException($"{nameof(roadLayer)} not assigned.");
                if (bridgeIconLayer == null) throw new NullReferenceException($"{nameof(bridgeIconLayer)} not assigned.");
                if (cityIconLayer == null) throw new NullReferenceException($"{nameof(cityIconLayer)} not assigned.");
                if (impassableLayer == null) throw new NullReferenceException($"{nameof(impassableLayer)} not assigned.");
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
                riverBankLayer.Clear();
                riverWaterLayer.Clear();
                roadLayer.Clear();
                impassableLayer.Clear();
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

                    DrawImpassableForHex(hex);

                    DrawRiversForHex(hex);

                    DrawRoadForHex(hex);

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

            var theme = GameDataManager.CurrentMapTheme;
            string spriteName = iconType switch
            {
                MapIconType.Airbase => theme switch
                {
                    MapTheme.MiddleEast => SpriteManager.ME_Airbase,
                    _ => throw new ArgumentException($"{CLASS_NAME}.CreateMapIcon: Airbase icon not defined for map theme '{theme}'.")
                },
                MapIconType.Fort => theme switch
                {
                    MapTheme.MiddleEast => SpriteManager.ME_Fort,
                    _ => throw new ArgumentException($"{CLASS_NAME}.CreateMapIcon: Fort icon not defined for map theme '{theme}'.")
                },
                _ => SpriteManager.ME_Sprawl
            };
            prefab.GetSpriteRenderer().sprite = SpriteManager.GetSprite(spriteName);

            obj.transform.position = GetRenderPosition(new Vector2Int(hex.Position.IntX, hex.Position.IntY));
            mapIconPrefabs[new Vector2Int(hex.Position.IntX, hex.Position.IntY)] = prefab;
        }

        #endregion // Private Methods — Map Icons

        #region Private Methods — Impassable Overlay

        private void DrawImpassableForHex(HexTile hex)
        {
            try
            {
                if (hex.Terrain != TerrainType.Impassable) return;

                var sprite = SpriteManager.GetSprite(SpriteManager.Impassable);
                if (sprite == null) return;

                var pos = new Vector2Int(hex.Position.IntX, hex.Position.IntY);
                var worldPos = GetRenderPosition(pos);
                impassableLayer.SetSprite($"impassable_{pos.x}_{pos.y}", sprite, worldPos, Color.white);
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawImpassableForHex), e); }
        }

        #endregion // Private Methods — Impassable Overlay

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

        #region Private Methods — Rivers

        // Per-hex full iteration over all 6 edges + all 6 corners. No ownership rules, no flipping.
        // Every shared edge gets stamped twice (once per neighbor); every interior corner up to three
        // times (once per meeting hex). Identical art at identical world position composes into one
        // visual via the bank-below + water-above layer pair, so overlap is invisible.
        //
        // Bank/water layers eliminate junction seam-jank: a generic bank texture (slightly larger,
        // green) sits under the pure-water sprites, so overlapping water polygons union cleanly into
        // bends and confluences without the renderer needing to compose junctions geometrically.
        //
        // Corner river count semantics:
        //   0 → nothing.
        //   1 → terminus sprite if the lone river is on a focal-side inside edge (the focal that
        //       owns that edge stamps; if the lone river is on the corner's outer edge, this hex
        //       skips and the appropriate neighbor stamps it from its own iteration). Debug warning
        //       in either case (lone edges should not exist in valid map data).
        //   2 with both edges focal-side → nothing; the two edge sprites meet cleanly at the corner.
        //   2 with one focal + outer → Single_X_L or Single_X_R, picked by which inside edge has
        //       the river (L = inside edge that's left of outward from the vertex perspective).
        //   3 → Double junction sprite.
        //
        // Bank stamping convention: every water sprite has a "{name}_Bank" companion. If the bank
        // sprite isn't authored yet, GetSprite returns null and the bank stamp is silently skipped.
        private void DrawRiversForHex(HexTile hex)
        {
            try
            {
                if (!hex.IsRiver) return;

                Vector3 worldPos = HexGridSystem.Instance.HexToWorld(hex.Position);
                var rb = hex.RiverBorders;

                // Precompute (insideL, insideR, outer, count) for all 6 corners. The count drives:
                //   0 → nothing, 1 → terminus, 2 → single junction or clean meet, 3 → double junction.
                // count==1 also PRE-EMPTS edge stamps: the terminus sprite at a count==1 corner
                // depicts the entire edge body plus the cap, replacing the would-be edge sprite
                // (which is wider than the cap and would otherwise overdraw it from neighbor hexes).
                var cN  = new CornerInfo(rb.Northwest, rb.Northeast, ReadOuterEdge(hex, HexDirection.NW, HexDirection.E,  HexDirection.NE, HexDirection.W));
                var cNE = new CornerInfo(rb.Northeast, rb.East,      ReadOuterEdge(hex, HexDirection.NE, HexDirection.SE, HexDirection.E,  HexDirection.NW));
                var cSE = new CornerInfo(rb.East,      rb.Southeast, ReadOuterEdge(hex, HexDirection.E,  HexDirection.SW, HexDirection.SE, HexDirection.NE));
                var cS  = new CornerInfo(rb.Southeast, rb.Southwest, ReadOuterEdge(hex, HexDirection.SE, HexDirection.W,  HexDirection.SW, HexDirection.E));
                var cSW = new CornerInfo(rb.Southwest, rb.West,      ReadOuterEdge(hex, HexDirection.SW, HexDirection.NW, HexDirection.W,  HexDirection.SE));
                var cNW = new CornerInfo(rb.West,      rb.Northwest, ReadOuterEdge(hex, HexDirection.W,  HexDirection.NE, HexDirection.NW, HexDirection.SW));

                // Edges — pre-empted if either corner of the edge has count==1 (terminus replaces).
                if (rb.Northeast && cN.count  != 1 && cNE.count != 1) StampRiverFeature(hex, "eNE", SpriteManager.RiverEdge_NE_0, SpriteManager.RiverEdge_NE_0_Bank, worldPos);
                if (rb.East      && cNE.count != 1 && cSE.count != 1) StampRiverFeature(hex, "eE",  SpriteManager.RiverEdge_E_0,  SpriteManager.RiverEdge_E_0_Bank,  worldPos);
                if (rb.Southeast && cSE.count != 1 && cS.count  != 1) StampRiverFeature(hex, "eSE", SpriteManager.RiverEdge_SE_0, SpriteManager.RiverEdge_SE_0_Bank, worldPos);
                if (rb.Southwest && cS.count  != 1 && cSW.count != 1) StampRiverFeature(hex, "eSW", SpriteManager.RiverEdge_SW_0, SpriteManager.RiverEdge_SW_0_Bank, worldPos);
                if (rb.West      && cSW.count != 1 && cNW.count != 1) StampRiverFeature(hex, "eW",  SpriteManager.RiverEdge_W_0,  SpriteManager.RiverEdge_W_0_Bank,  worldPos);
                if (rb.Northwest && cNW.count != 1 && cN.count  != 1) StampRiverFeature(hex, "eNW", SpriteManager.RiverEdge_NW_0, SpriteManager.RiverEdge_NW_0_Bank, worldPos);

                // Corners — all 6. Termini (count==1) AND junctions (count==2/3) handled here.
                ProcessCorner(hex, worldPos, "cN", cN,
                    termL: (SpriteManager.RiverTerm_NW_N, SpriteManager.RiverTerm_NW_N_Bank),
                    termR: (SpriteManager.RiverTerm_NE_N, SpriteManager.RiverTerm_NE_N_Bank),
                    singleL: (SpriteManager.RiverSingle_N_L, SpriteManager.RiverSingle_N_L_Bank),
                    singleR: (SpriteManager.RiverSingle_N_R, SpriteManager.RiverSingle_N_R_Bank),
                    doubleSprite: (SpriteManager.RiverDouble_N, SpriteManager.RiverDouble_N_Bank));

                ProcessCorner(hex, worldPos, "cNE", cNE,
                    termL: (SpriteManager.RiverTerm_NE_NE, SpriteManager.RiverTerm_NE_NE_Bank),
                    termR: (SpriteManager.RiverTerm_E_NE, SpriteManager.RiverTerm_E_NE_Bank),
                    singleL: (SpriteManager.RiverSingle_NE_L, SpriteManager.RiverSingle_NE_L_Bank),
                    singleR: (SpriteManager.RiverSingle_NE_R, SpriteManager.RiverSingle_NE_R_Bank),
                    doubleSprite: (SpriteManager.RiverDouble_NE, SpriteManager.RiverDouble_NE_Bank));

                ProcessCorner(hex, worldPos, "cSE", cSE,
                    termL: (SpriteManager.RiverTerm_E_SE, SpriteManager.RiverTerm_E_SE_Bank),
                    termR: (SpriteManager.RiverTerm_SE_SE, SpriteManager.RiverTerm_SE_SE_Bank),
                    singleL: (SpriteManager.RiverSingle_SE_L, SpriteManager.RiverSingle_SE_L_Bank),
                    singleR: (SpriteManager.RiverSingle_SE_R, SpriteManager.RiverSingle_SE_R_Bank),
                    doubleSprite: (SpriteManager.RiverDouble_SE, SpriteManager.RiverDouble_SE_Bank));

                ProcessCorner(hex, worldPos, "cS", cS,
                    termL: (SpriteManager.RiverTerm_SE_S, SpriteManager.RiverTerm_SE_S_Bank),
                    termR: (SpriteManager.RiverTerm_SW_S, SpriteManager.RiverTerm_SW_S_Bank),
                    singleL: (SpriteManager.RiverSingle_S_L, SpriteManager.RiverSingle_S_L_Bank),
                    singleR: (SpriteManager.RiverSingle_S_R, SpriteManager.RiverSingle_S_R_Bank),
                    doubleSprite: (SpriteManager.RiverDouble_S, SpriteManager.RiverDouble_S_Bank));

                ProcessCorner(hex, worldPos, "cSW", cSW,
                    termL: (SpriteManager.RiverTerm_SW_SW, SpriteManager.RiverTerm_SW_SW_Bank),
                    termR: (SpriteManager.RiverTerm_W_SW, SpriteManager.RiverTerm_W_SW_Bank),
                    singleL: (SpriteManager.RiverSingle_SW_L, SpriteManager.RiverSingle_SW_L_Bank),
                    singleR: (SpriteManager.RiverSingle_SW_R, SpriteManager.RiverSingle_SW_R_Bank),
                    doubleSprite: (SpriteManager.RiverDouble_SW, SpriteManager.RiverDouble_SW_Bank));

                ProcessCorner(hex, worldPos, "cNW", cNW,
                    termL: (SpriteManager.RiverTerm_W_NW, SpriteManager.RiverTerm_W_NW_Bank),
                    termR: (SpriteManager.RiverTerm_NW_NW, SpriteManager.RiverTerm_NW_NW_Bank),
                    singleL: (SpriteManager.RiverSingle_NW_L, SpriteManager.RiverSingle_NW_L_Bank),
                    singleR: (SpriteManager.RiverSingle_NW_R, SpriteManager.RiverSingle_NW_R_Bank),
                    doubleSprite: (SpriteManager.RiverDouble_NW, SpriteManager.RiverDouble_NW_Bank));
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawRiversForHex), e); }
        }

        // Lightweight per-corner data. Built once at the top of DrawRiversForHex and reused by
        // both edge pre-emption (count==1 ⇒ skip the edge stamp; the terminus replaces it) and
        // ProcessCorner (which stamps the actual terminus / single / double).
        private readonly struct CornerInfo
        {
            public readonly bool insideL;
            public readonly bool insideR;
            public readonly bool outer;
            public readonly int count;

            public CornerInfo(bool insideL, bool insideR, bool outer)
            {
                this.insideL = insideL;
                this.insideR = insideR;
                this.outer = outer;
                count = (insideL ? 1 : 0) + (insideR ? 1 : 0) + (outer ? 1 : 0);
            }
        }

        private void ProcessCorner(
            HexTile hex, Vector3 worldPos, string keySuffix,
            CornerInfo info,
            (string water, string bank) termL, (string water, string bank) termR,
            (string water, string bank) singleL, (string water, string bank) singleR,
            (string water, string bank) doubleSprite)
        {
            if (info.count == 0) return;

            if (info.count == 1)
            {
                // count==1 at this corner means exactly one river edge meets here. The edge stamp
                // for that edge has been suppressed by the pre-emption check in DrawRiversForHex,
                // so this terminus sprite is the entire visual for that edge — it must depict the
                // full edge body plus the cap. If the lone river is on the corner's outer edge,
                // the owning neighbor stamps it from its own iteration (and its own edge stamp is
                // likewise suppressed); stamping here would land the visual at the wrong place.
                if (info.insideL) StampRiverFeature(hex, keySuffix, termL.water, termL.bank, worldPos);
                else if (info.insideR) StampRiverFeature(hex, keySuffix, termR.water, termR.bank, worldPos);
                return;
            }

            if (info.count == 3)
            {
                StampRiverFeature(hex, keySuffix, doubleSprite.water, doubleSprite.bank, worldPos);
                return;
            }

            // count == 2
            if (info.insideL && info.insideR) return; // both focal-side; edges meet cleanly, no junction.

            // Single — pick L or R variant by which focal-side edge has the river paired with outer.
            // Note: L/R is named from the vertex perspective looking outward (toward the outer edge),
            // not from focal's perspective. So when focal's "left-side inside edge" (insideL by my
            // hex-frame labeling) has the river, the sprite needed is the R variant from the vertex
            // POV, and vice versa.
            var sprite = info.insideL ? singleR : singleL;
            StampRiverFeature(hex, keySuffix, sprite.water, sprite.bank, worldPos);
        }

        // Stamps a river feature on both layers: the water sprite on riverWaterLayer, and the
        // matching bank companion on riverBankLayer. Missing sprites are silently skipped.
        private void StampRiverFeature(HexTile hex, string keySuffix, string waterSpriteName, string bankSpriteName, Vector3 worldPos)
        {
            if (waterSpriteName == null && bankSpriteName == null) return;
            string key = $"river_{hex.Position.IntX}_{hex.Position.IntY}_{keySuffix}";

            if (bankSpriteName != null)
            {
                var bankSprite = SpriteManager.GetSprite(bankSpriteName);
                if (bankSprite != null)
                    riverBankLayer.SetSprite(key, bankSprite, worldPos, Color.white);
            }

            if (waterSpriteName != null)
            {
                var waterSprite = SpriteManager.GetSprite(waterSpriteName);
                if (waterSprite != null)
                    riverWaterLayer.SetSprite(key, waterSprite, worldPos, Color.white);
            }
        }

        // Reads the outer (third) edge at a corner. The edge is shared between two neighbors;
        // we try the first, fall back to the second if its data is missing (map-edge corner).
        private static bool ReadOuterEdge(HexTile focal, HexDirection neighborADir, HexDirection edgeFromA, HexDirection neighborBDir, HexDirection edgeFromB)
        {
            HexTile a = focal.GetNeighbor(neighborADir);
            if (a?.RiverBorders != null) return a.RiverBorders.GetBorder(edgeFromA);
            HexTile b = focal.GetNeighbor(neighborBDir);
            if (b?.RiverBorders != null) return b.RiverBorders.GetBorder(edgeFromB);
            return false;
        }

        #endregion // Private Methods — Rivers

        #region Private Methods — Roads

        // One whole-hex sprite per road configuration. The 6-bit mask is derived at render time
        // from HexTile.IsRoad on the focal hex and its 6 neighbors — two adjacent IsRoad hexes
        // imply a road edge between them. Bit order: NE=32, E=16, SE=8, SW=4, W=2, NW=1. Sprite
        // is full-canvas and depicts all edges + intersections; stamped at the hex center.
        private void DrawRoadForHex(HexTile hex)
        {
            try
            {
                if (!hex.IsRoad) return;

                int mask =
                    (NeighborIsRoad(hex, HexDirection.NE) ? 32 : 0) |
                    (NeighborIsRoad(hex, HexDirection.E)  ? 16 : 0) |
                    (NeighborIsRoad(hex, HexDirection.SE) ?  8 : 0) |
                    (NeighborIsRoad(hex, HexDirection.SW) ?  4 : 0) |
                    (NeighborIsRoad(hex, HexDirection.W)  ?  2 : 0) |
                    (NeighborIsRoad(hex, HexDirection.NW) ?  1 : 0);

                if (mask == 0) return; // isolated roaded hex with no roaded neighbors; nothing to draw.

                string spriteName = GetRoadSpriteName(mask);
                if (spriteName == null) return;

                var sprite = SpriteManager.GetSprite(spriteName);
                if (sprite == null) return;

                Vector3 worldPos = HexGridSystem.Instance.HexToWorld(hex.Position);
                string key = $"road_{hex.Position.IntX}_{hex.Position.IntY}";
                roadLayer.SetSprite(key, sprite, worldPos, Color.white);
            }
            catch (Exception e) { AppService.HandleException(CLASS_NAME, nameof(DrawRoadForHex), e); }
        }

        private static bool NeighborIsRoad(HexTile hex, HexDirection dir)
        {
            var n = hex.GetNeighbor(dir);
            return n != null && n.IsRoad;
        }

        private static string GetRoadSpriteName(int mask) => mask switch
        {
            // 1 edge
            0b100000 => SpriteManager.Road_100000,
            0b010000 => SpriteManager.Road_010000,
            0b001000 => SpriteManager.Road_001000,
            0b000100 => SpriteManager.Road_000100,
            0b000010 => SpriteManager.Road_000010,
            0b000001 => SpriteManager.Road_000001,
            // 2 edges
            0b110000 => SpriteManager.Road_110000,
            0b101000 => SpriteManager.Road_101000,
            0b100100 => SpriteManager.Road_100100,
            0b100010 => SpriteManager.Road_100010,
            0b100001 => SpriteManager.Road_100001,
            0b011000 => SpriteManager.Road_011000,
            0b010100 => SpriteManager.Road_010100,
            0b010010 => SpriteManager.Road_010010,
            0b010001 => SpriteManager.Road_010001,
            0b001100 => SpriteManager.Road_001100,
            0b001010 => SpriteManager.Road_001010,
            0b001001 => SpriteManager.Road_001001,
            0b000110 => SpriteManager.Road_000110,
            0b000101 => SpriteManager.Road_000101,
            0b000011 => SpriteManager.Road_000011,
            // 3 edges
            0b111000 => SpriteManager.Road_111000,
            0b110100 => SpriteManager.Road_110100,
            0b110010 => SpriteManager.Road_110010,
            0b110001 => SpriteManager.Road_110001,
            0b101100 => SpriteManager.Road_101100,
            0b101010 => SpriteManager.Road_101010,
            0b101001 => SpriteManager.Road_101001,
            0b100110 => SpriteManager.Road_100110,
            0b100101 => SpriteManager.Road_100101,
            0b100011 => SpriteManager.Road_100011,
            0b011100 => SpriteManager.Road_011100,
            0b011010 => SpriteManager.Road_011010,
            0b011001 => SpriteManager.Road_011001,
            0b010110 => SpriteManager.Road_010110,
            0b010101 => SpriteManager.Road_010101,
            0b010011 => SpriteManager.Road_010011,
            0b001110 => SpriteManager.Road_001110,
            0b001101 => SpriteManager.Road_001101,
            0b001011 => SpriteManager.Road_001011,
            0b000111 => SpriteManager.Road_000111,
            // 4 edges
            0b111100 => SpriteManager.Road_111100,
            0b111010 => SpriteManager.Road_111010,
            0b111001 => SpriteManager.Road_111001,
            0b110110 => SpriteManager.Road_110110,
            0b110101 => SpriteManager.Road_110101,
            0b110011 => SpriteManager.Road_110011,
            0b101110 => SpriteManager.Road_101110,
            0b101101 => SpriteManager.Road_101101,
            0b101011 => SpriteManager.Road_101011,
            0b100111 => SpriteManager.Road_100111,
            0b011110 => SpriteManager.Road_011110,
            0b011101 => SpriteManager.Road_011101,
            0b011011 => SpriteManager.Road_011011,
            0b010111 => SpriteManager.Road_010111,
            0b001111 => SpriteManager.Road_001111,
            // 5 edges
            0b111110 => SpriteManager.Road_111110,
            0b111101 => SpriteManager.Road_111101,
            0b111011 => SpriteManager.Road_111011,
            0b110111 => SpriteManager.Road_110111,
            0b101111 => SpriteManager.Road_101111,
            0b011111 => SpriteManager.Road_011111,
            // 6 edges
            0b111111 => SpriteManager.Road_111111,
            _ => null
        };

        #endregion // Private Methods — Roads

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
