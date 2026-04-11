using HammerAndSickle.Controllers;
using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Renderers;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Core.Map
{
    /// <summary>
    /// Manages the rendering of non-map game icons including combat units, bases, and utility elements.
    /// Handles sprite selection based on weapon system profiles, nationality, facing, and deployment state.
    /// </summary>
    public class GameIconRenderer : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(GameIconRenderer);
        private const float STACKING_REDUCED_OPACITY = 0.6f;
        private const float STACKING_FULL_OPACITY = 1.0f;

        #region Singleton

        /// <summary>
        /// Singleton instance of the renderer.
        /// </summary>
        public static GameIconRenderer Instance { get; private set; }

        #endregion // Singleton

        #region Fields

        /// <summary>
        /// Dictionary to store and track unit icon prefab instances by their unit ID.
        /// </summary>
        private readonly Dictionary<string, Prefab_CombatUnitIcon> unitIconPrefabs = new();

        /// <summary>
        /// Dictionary to track hexes where air and ground units are stacked.
        /// Key is the hex position, value contains stacking state information.
        /// </summary>
        private readonly Dictionary<Position2D, HexStackingInfo> stackedHexes = new();

        /// <summary>
        /// Enables detailed debug logging throughout the renderer.
        /// </summary>
        [SerializeField] private bool _debug = false;

        #endregion // Fields

        #region Nested Types

        /// <summary>
        /// Tracks stacking state for a hex where air and ground units coexist.
        /// </summary>
        private class HexStackingInfo
        {
            public string AirUnitId { get; set; }
            public string GroundUnitId { get; set; }
            public bool IsAirDominant { get; set; } = true; // Default: air dominant
        }

        #endregion // Nested Types

        #region Inspector Fields

        [Header("Prefabs")]
        [SerializeField] private GameObject combatUnitIconPrefab;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Indicates if the renderer is properly initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

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
        /// </summary>
        private void Start()
        {
            if (!IsInitialized)
            {
                Debug.LogError($"{CLASS_NAME}.Start: Service failed to initialize properly.");
                return;
            }
        }

        /// <summary>
        /// Unity's OnDestroy method. Handles cleanup.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (Instance == this)
            {
                Instance = null;
            }
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
                SubscribeToEvents();
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
        /// Subscribes to relevant game events for unit stacking management.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnRedrawMapIcons += OnRedrawMapIcons;
                EventManager.Instance.OnStackingToggleRequested += OnStackingToggleRequested;
                EventManager.Instance.OnUnitSpottedLevelChanged += HandleUnitSpottedLevelChanged;
                if (_debug) Debug.Log($"[{CLASS_NAME}.SubscribeToEvents] Subscribed to EventManager events.");
            }
            else
            {
                Debug.LogWarning($"{CLASS_NAME}.SubscribeToEvents: EventManager instance not available.");
            }
        }

        /// <summary>
        /// Unsubscribes from game events to prevent memory leaks.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnRedrawMapIcons -= OnRedrawMapIcons;
                EventManager.Instance.OnStackingToggleRequested -= OnStackingToggleRequested;
                EventManager.Instance.OnUnitSpottedLevelChanged -= HandleUnitSpottedLevelChanged;
                if (_debug) Debug.Log($"[{CLASS_NAME}.UnsubscribeFromEvents] Unsubscribed from EventManager events.");
            }
        }

        /// <summary>
        /// Validates that all required components are properly referenced.
        /// Throws exceptions if any required components are missing.
        /// </summary>
        private void ValidateComponents()
        {
            if (_debug) Debug.Log($"[{CLASS_NAME}.ValidateComponents] Validating required components...");

            if (combatUnitIconPrefab == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(combatUnitIconPrefab)} is missing.");

            if (HexGridRenderer.Instance == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: HexGridRenderer singleton not found.");

            if (_debug) Debug.Log($"[{CLASS_NAME}.ValidateComponents] All required components validated successfully.");
        }

        #endregion // Initialization

        #region Public Methods

        /// <summary>
        /// Clears all unit icons from the rendering layers.
        /// </summary>
        public void ClearAllUnitIcons()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.ClearAllUnitIcons] Clearing all unit icons...");

                unitIconPrefabs.Clear();
                ClearStackingState();
                ClearContainer(HexGridRenderer.Instance.GroundUnitLayerTransform);
                ClearContainer(HexGridRenderer.Instance.AirUnitLayerTransform);

                if (_debug) Debug.Log($"[{CLASS_NAME}.ClearAllUnitIcons] All unit icons cleared.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearAllUnitIcons", e);
            }
        }

        /// <summary>
        /// Draws all combat units on the map.
        /// </summary>
        public void DrawAllUnits()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.DrawAllUnits] Starting unit rendering...");

                var units = GameDataManager.Instance.GetAllCombatUnits();

                int count = 0;
                foreach (var unit in units)
                {
                    // Fog of war: skip non-player units at SpottedLevel.Level0
                    if (unit.Side != Side.Player && unit.SpottedLevel == SpottedLevel.Level0)
                        continue;

                    CreateUnitIcon(unit);
                    count++;
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.DrawAllUnits] Rendered {count} unit icons.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DrawAllUnits", e);
            }
        }

        /// <summary>
        /// Creates a unit icon prefab at the unit's map position.
        /// Handles all unit types: ground units, air units, airbases, and facilities.
        /// Sets sprite, nationality flag, hit points display, and applies facing-based flipping.
        /// Automatically checks for air/ground unit stacking at the position.
        /// </summary>
        public void CreateUnitIcon(CombatUnit unit)
        {
            try
            {
                if (unit == null)
                {
                    if (_debug) Debug.LogWarning($"[{CLASS_NAME}.CreateUnitIcon] Unit is null, skipping.");
                    return;
                }

                // Don't render air units that are attached to a friendly airbase
                if (IsFixedWingAircraft(unit.Classification) && IsAtFriendlyAirbase(unit))
                {
                    if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Skipping '{unit.UnitName}' - attached to friendly airbase.");
                    return;
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Creating icon for unit '{unit.UnitName}' at ({unit.MapPos.IntX}, {unit.MapPos.IntY})");

                // Determine appropriate layer based on unit type
                Transform targetLayer = GetTargetLayerForUnit(unit);

                // Create prefab instance
                GameObject unitIconObject = Instantiate(combatUnitIconPrefab, targetLayer);
                unitIconObject.name = $"Unit_{unit.UnitID}_{unit.UnitName}";

                // Get prefab component
                if (!unitIconObject.TryGetComponent<Prefab_CombatUnitIcon>(out var unitIcon))
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
                
                // Set nationality symbol
                string symbolSprite = GetNationalSymbol(unit.Nationality);
                unitIcon.SetNationIcon(symbolSprite);

                // Set box icon base (color based on faction)
                string boxSprite = GetBoxIconBase(unit.Nationality);
                unitIcon.SetBoxIcon(boxSprite);

                // Calculate HP percentage (1-100)
                int hpPercent = Mathf.Clamp(Mathf.RoundToInt(unit.HitPoints.GetPercentage() * 100f), 1, 100);

                // Initialize the prefab with unit ID, current state, and deploy sprite resolver
                unitIcon.Initialize(unit.UnitID, hpPercent, unit.DeploymentPosition, unit.CurrentEmbarkmentState, GetDeploySpriteName);

                // Position the prefab
                Vector3 position = GetRenderPosition(new Vector2Int(unit.MapPos.IntX, unit.MapPos.IntY));
                unitIconObject.transform.position = position;

                // Store reference
                unitIconPrefabs[unit.UnitID] = unitIcon;

                // Ensure stacking icon is hidden by default
                unitIcon.ShowStackingIcon(false);

                // Check for stacking at this position
                CheckForStacking(unit.MapPos);

                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Successfully created icon for unit '{unit.UnitName}'");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CreateUnitIcon", e);
            }
        }
        
        /// <summary>
        /// Removes a unit icon from the renderer.
        /// Also rechecks stacking at the unit's position.
        /// </summary>
        public void RemoveUnitIcon(string unitId)
        {
            try
            {
                if (string.IsNullOrEmpty(unitId))
                {
                    if (_debug) Debug.LogWarning($"[{CLASS_NAME}.RemoveUnitIcon] Unit ID is null or empty.");
                    return;
                }

                // Get unit position before removal for stacking check
                CombatUnit unit = GameDataManager.Instance.GetCombatUnit(unitId);
                Position2D unitPosition = unit?.MapPos ?? Position2D.Zero;
                bool hasValidPosition = unit != null;

                if (unitIconPrefabs.TryGetValue(unitId, out Prefab_CombatUnitIcon unitIcon))
                {
                    if (unitIcon != null)
                    {
                        Destroy(unitIcon.gameObject);
                    }
                    unitIconPrefabs.Remove(unitId);

                    if (_debug) Debug.Log($"[{CLASS_NAME}.RemoveUnitIcon] Removed icon for unit '{unitId}'");

                    // Recheck stacking at the removed unit's position
                    if (hasValidPosition)
                    {
                        CheckForStacking(unitPosition);
                    }
                }
                else
                {
                    if (_debug) Debug.LogWarning($"[{CLASS_NAME}.RemoveUnitIcon] No icon found for unit '{unitId}'");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveUnitIcon", e);
            }
        }

        /// <summary>
        /// Updates the position of an existing unit icon.
        /// </summary>
        public void UpdateUnitIconPosition(string unitId, Position2D newPosition)
        {
            try
            {
                if (unitIconPrefabs.TryGetValue(unitId, out Prefab_CombatUnitIcon unitIcon))
                {
                    Vector3 position = GetRenderPosition(new Vector2Int(newPosition.IntX, newPosition.IntY));
                    unitIcon.transform.position = position;

                    if (_debug) Debug.Log($"[{CLASS_NAME}.UpdateUnitIconPosition] Updated position for unit '{unitId}' to ({newPosition.IntX}, {newPosition.IntY})");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateUnitIconPosition", e);
            }
        }

        /// <summary>
        /// Gets the appropriate sprite name for a combat unit based on its properties.
        /// Returns the sprite name and sets the out parameter for whether to flip horizontally.
        /// Resolves sprites through the WeaponProfile IconProfile system.
        /// Airbases are handled separately with stacking icons.
        /// All icons can be flipped horizontally for easterly directions (E, NE, SE).
        /// </summary>
        public string GetSpriteNameForUnit(CombatUnit unit, out bool shouldFlip)
        {
            shouldFlip = false;

            try
            {
                // Airbases use stacking icons based on attached air unit count
                if (unit.Classification == UnitClassification.AIRB)
                {
                    int airUnitCount = unit.GetAttachedAirUnitCount();
                    string airbaseSprite = GetAirbaseStackSprite(airUnitCount);

                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Airbase '{unit.UnitName}': {airUnitCount} air units attached, sprite={airbaseSprite}");
                    return airbaseSprite;
                }

                // All icons flip for easterly directions
                shouldFlip = ShouldFlipSprite(unit.Facing);

                // Normalize easterly directions to their western equivalents for sprite lookup
                HexDirection normalizedDirection = NormalizeDirection(unit.Facing);

                // Resolve sprite through the RegimentProfile icon system
                string spriteName = unit.RegimentProfile.GetIcon(unit.DeploymentPosition, normalizedDirection);

                if (string.IsNullOrEmpty(spriteName))
                {
                    if (_debug) Debug.LogWarning($"[{CLASS_NAME}.GetSpriteNameForUnit] No sprite found for unit '{unit.UnitName}'");
                    return SpriteManager.Utility_MismatchIcon;
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': sprite={spriteName}, Flip={shouldFlip}");
                return spriteName;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSpriteNameForUnit", e);
                return SpriteManager.Utility_MismatchIcon;
            }
        }

        /// <summary>
        /// Overload for backwards compatibility - ignores flip information.
        /// </summary>
        public string GetSpriteNameForUnit(CombatUnit unit)
        {
            return GetSpriteNameForUnit(unit, out _);
        }

        #endregion // Public Methods

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

        /// <summary>
        /// Gets the world position for a hex tile based on its grid coordinates.
        /// Uses HexGridSystem for coordinate conversion.
        /// </summary>
        private Vector3 GetRenderPosition(Vector2Int gridPos)
        {
            if (HexGridSystem.Instance != null && HexGridSystem.Instance.IsInitialized)
            {
                return HexGridSystem.Instance.HexToWorld(new Position2D(gridPos.x, gridPos.y));
            }

            Debug.LogWarning($"{CLASS_NAME}.GetRenderPosition: HexGridSystem not available.");
            return Vector3.zero;
        }

        /// <summary>
        /// Determines the appropriate rendering layer for a unit.
        /// Ground units, bases, and helicopters go to GroundUnitLayer.
        /// Fixed-wing aircraft go to AirUnitLayer.
        /// </summary>
        private Transform GetTargetLayerForUnit(CombatUnit unit)
        {
            if (IsFixedWingAircraft(unit.Classification))
            {
                return HexGridRenderer.Instance.AirUnitLayerTransform;
            }

            return HexGridRenderer.Instance.GroundUnitLayerTransform;
        }

        /// <summary>
        /// Checks if an air unit is currently attached to a friendly airbase.
        /// </summary>
        private bool IsAtFriendlyAirbase(CombatUnit unit)
        {
            var airbases = GameDataManager.Instance.GetUnits(u =>
                u.Classification == UnitClassification.AIRB && u.Side == unit.Side);

            foreach (var airbase in airbases)
            {
                var attachedIds = airbase.AttachedUnitIDs;
                for (int i = 0; i < attachedIds.Count; i++)
                {
                    if (attachedIds[i] == unit.UnitID)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a unit classification is a fixed-wing aircraft (not helicopter).
        /// </summary>
        private bool IsFixedWingAircraft(UnitClassification classification)
        {
            return classification == UnitClassification.FGT ||
                   classification == UnitClassification.ATT ||
                   classification == UnitClassification.BMB ||
                   classification == UnitClassification.RECONA ||
                   classification == UnitClassification.AWACS;
        }

        #endregion // Private Methods - Utilities

        #region Icon Category Methods

        /// <summary>
        /// Gets the appropriate airbase stack sprite based on attached air unit count.
        /// Returns AirbaseStack0 through AirbaseStack4 depending on how many
        /// air units are currently attached to the airbase.
        /// </summary>
        /// <param name="airUnitCount">Number of air units attached to the airbase</param>
        /// <returns>Sprite name for the airbase stack icon</returns>
        private string GetAirbaseStackSprite(int airUnitCount)
        {
            return airUnitCount switch
            {
                0 => SpriteManager.Utility_AirbaseStack0,
                1 => SpriteManager.Utility_AirbaseStack1,
                2 => SpriteManager.Utility_AirbaseStack2,
                3 => SpriteManager.Utility_AirbaseStack3,
                _ => SpriteManager.Utility_AirbaseStack4  // 4 or more (capped at MAX_AIR_UNITS)
            };
        }

        /// <summary>
        /// Gets the deployment state sprite name for a given deployment position.
        /// Used as a callback by Prefab_CombatUnitIcon to resolve deploy icon sprites.
        /// </summary>
        /// <param name="position">The deployment position to get the sprite for</param>
        /// <returns>Sprite name for the deployment state icon, or null if no icon should be shown</returns>
        private string GetDeploySpriteName(DeploymentPosition position, EmbarkmentState embarkmentState)
        {
            if (position == DeploymentPosition.Embarked)
            {
                return embarkmentState switch
                {
                    EmbarkmentState.EmbarkedNaval => SpriteManager.EmbarkedNavalIcon,
                    _ => SpriteManager.EmbarkedAirIcon // Fixed-wing and helo both use air icon
                };
            }

            return position switch
            {
                DeploymentPosition.Fortified => SpriteManager.FortifiedIcon,
                DeploymentPosition.Entrenched => SpriteManager.EntrenchedIcon,
                DeploymentPosition.HastyDefense => SpriteManager.DefensiveIcon,
                DeploymentPosition.Deployed => SpriteManager.DeployedIcon,
                DeploymentPosition.Mobile => SpriteManager.MountedIcon,
                _ => null
            };
        }

        #endregion // Icon Category Methods

        #region Direction and Flip Methods

        /// <summary>
        /// Normalizes easterly directions to their western equivalents for sprite lookup.
        /// E maps to W, NE maps to NW, SE maps to SW. Western directions pass through unchanged.
        /// </summary>
        private HexDirection NormalizeDirection(HexDirection direction)
        {
            return direction switch
            {
                HexDirection.E => HexDirection.W,
                HexDirection.NE => HexDirection.NW,
                HexDirection.SE => HexDirection.SW,
                _ => direction
            };
        }

        /// <summary>
        /// Determines if sprite should be flipped horizontally based on facing.
        /// All icon types (vehicles, infantry, aircraft, etc.) flip for easterly directions.
        /// </summary>
        private bool ShouldFlipSprite(HexDirection facing)
        {
            return facing == HexDirection.E ||
                   facing == HexDirection.NE ||
                   facing == HexDirection.SE;
        }

        #endregion // Direction and Flip Methods

        #region Nationality Symbol Methods

        /// <summary>
        /// Gets the national symbol sprite name for a given nationality.
        /// Symbols are small icons designed for unit icon display.
        /// </summary>
        /// <param name="nationality">The nationality to get the symbol for</param>
        /// <returns>Sprite name for the nationality symbol</returns>
        private string GetNationalSymbol(Nationality nationality)
        {
            return nationality switch
            {
                Nationality.USSR => SpriteManager.Symbol_SV,
                Nationality.USA => SpriteManager.Symbol_US,
                Nationality.UK => SpriteManager.Symbol_UK,
                Nationality.FRG => SpriteManager.Symbol_GE,
                Nationality.FRA => SpriteManager.Symbol_FR,
                Nationality.BE => SpriteManager.Symbol_BE,
                Nationality.DE => SpriteManager.Symbol_DE,
                Nationality.NE => SpriteManager.Symbol_NE,
                Nationality.MJ => SpriteManager.Symbol_MJ,
                Nationality.IR => SpriteManager.Symbol_Iran,
                Nationality.IQ => SpriteManager.Symbol_Iraq,
                Nationality.SAUD => SpriteManager.Symbol_Saudi,
                Nationality.KW => SpriteManager.Symbol_Kuwait,
                Nationality.China => SpriteManager.Symbol_China,
                _ => SpriteManager.Symbol_Default
            };
        }

        /// <summary>
        /// Gets the box icon base sprite name for a given nationality.
        /// Soviet = Red, NATO = Blue, Mujahideen/Arab = Green, Chinese = Grey.
        /// </summary>
        /// <param name="nationality">The nationality to get the box icon for</param>
        /// <returns>Sprite name for the icon base</returns>
        private string GetBoxIconBase(Nationality nationality)
        {
            return nationality switch
            {
                Nationality.USSR => SpriteManager.RedIconBase,
                Nationality.MJ => SpriteManager.GreenIconBase,
                Nationality.IR => SpriteManager.GreenIconBase,
                Nationality.IQ => SpriteManager.GreenIconBase,
                Nationality.SAUD => SpriteManager.GreenIconBase,
                Nationality.KW => SpriteManager.GreenIconBase,
                Nationality.China => SpriteManager.GreyIconBase,
                _ => SpriteManager.BlueIconBase // NATO nations (USA, UK, FRG, FRA, BE, DE, NE)
            };
        }

        #endregion // Nationality Symbol Methods

        #region Unit Stacking Methods

        /// <summary>
        /// Determines if a unit is an air unit based on its classification.
        /// All fixed-wing aircraft are considered air units for stacking purposes.
        /// Helicopters are ground units.
        /// </summary>
        /// <param name="unit">The unit to check</param>
        /// <returns>True if the unit is an air unit</returns>
        private bool IsAirUnit(CombatUnit unit)
        {
            return IsFixedWingAircraft(unit.Classification);
        }

        /// <summary>
        /// Checks for stacking at a hex position and updates stacking state.
        /// Called when a unit is created or moves to a new position.
        /// </summary>
        /// <param name="position">The hex position to check</param>
        public void CheckForStacking(Position2D position)
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.CheckForStacking] Checking position ({position.IntX}, {position.IntY})");

                string airUnitId = null;
                string groundUnitId = null;

                // Find all units at this position
                foreach (var kvp in unitIconPrefabs)
                {
                    string unitId = kvp.Key;
                    CombatUnit unit = GameDataManager.Instance.GetCombatUnit(unitId);

                    if (unit == null || !unit.MapPos.Equals(position))
                        continue;

                    if (IsAirUnit(unit))
                    {
                        airUnitId = unitId;
                    }
                    else
                    {
                        groundUnitId = unitId;
                    }
                }

                // Check if we have both air and ground units at this position
                if (!string.IsNullOrEmpty(airUnitId) && !string.IsNullOrEmpty(groundUnitId))
                {
                    // Create or update stacking info
                    if (!stackedHexes.TryGetValue(position, out HexStackingInfo stackInfo))
                    {
                        stackInfo = new HexStackingInfo();
                        stackedHexes[position] = stackInfo;
                    }

                    stackInfo.AirUnitId = airUnitId;
                    stackInfo.GroundUnitId = groundUnitId;

                    if (_debug) Debug.Log($"[{CLASS_NAME}.CheckForStacking] Stacking detected: Air={airUnitId}, Ground={groundUnitId}, AirDominant={stackInfo.IsAirDominant}");

                    UpdateStackingVisuals(position);
                }
                else
                {
                    // No stacking at this position - remove if it existed
                    if (stackedHexes.Remove(position))
                    {
                        if (_debug) Debug.Log($"[{CLASS_NAME}.CheckForStacking] Stacking cleared at ({position.IntX}, {position.IntY})");

                        // Reset opacity for any unit still at this position
                        if (!string.IsNullOrEmpty(airUnitId) && unitIconPrefabs.TryGetValue(airUnitId, out var airIcon))
                        {
                            airIcon.SetOpacity(STACKING_FULL_OPACITY);
                            airIcon.ShowStackingIcon(false);
                        }
                        if (!string.IsNullOrEmpty(groundUnitId) && unitIconPrefabs.TryGetValue(groundUnitId, out var groundIcon))
                        {
                            groundIcon.SetOpacity(STACKING_FULL_OPACITY);
                            groundIcon.ShowStackingIcon(false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CheckForStacking", e);
            }
        }

        /// <summary>
        /// Updates the visual representation of stacked units at a hex position.
        /// Sets opacity and stacking icon based on current dominance state.
        /// </summary>
        /// <param name="position">The hex position with stacked units</param>
        private void UpdateStackingVisuals(Position2D position)
        {
            try
            {
                if (!stackedHexes.TryGetValue(position, out HexStackingInfo stackInfo))
                    return;

                bool hasAirIcon = unitIconPrefabs.TryGetValue(stackInfo.AirUnitId, out var airIcon);
                bool hasGroundIcon = unitIconPrefabs.TryGetValue(stackInfo.GroundUnitId, out var groundIcon);

                if (!hasAirIcon || !hasGroundIcon)
                    return;

                if (stackInfo.IsAirDominant)
                {
                    // Air is dominant: full opacity, ground is faded
                    airIcon.SetOpacity(STACKING_FULL_OPACITY);
                    groundIcon.SetOpacity(STACKING_REDUCED_OPACITY);

                    // Show stacking icon on dominant unit indicating current mode
                    airIcon.SetStackingIcon(SpriteManager.Utility_StackingIconAir);
                    airIcon.ShowStackingIcon(true);
                    groundIcon.ShowStackingIcon(false);
                }
                else
                {
                    // Ground is dominant: full opacity, air is faded
                    groundIcon.SetOpacity(STACKING_FULL_OPACITY);
                    airIcon.SetOpacity(STACKING_REDUCED_OPACITY);

                    // Show stacking icon on dominant unit indicating current mode
                    groundIcon.SetStackingIcon(SpriteManager.Utility_StackingIconLand);
                    groundIcon.ShowStackingIcon(true);
                    airIcon.ShowStackingIcon(false);
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.UpdateStackingVisuals] Updated visuals at ({position.IntX}, {position.IntY}): AirDominant={stackInfo.IsAirDominant}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateStackingVisuals", e);
            }
        }

        /// <summary>
        /// Toggles the dominance state between air and ground units at a hex position.
        /// Called when the player clicks the stacking toggle button.
        /// </summary>
        /// <param name="position">The hex position to toggle</param>
        public void ToggleStackDominance(Position2D position)
        {
            try
            {
                if (!stackedHexes.TryGetValue(position, out HexStackingInfo stackInfo))
                {
                    if (_debug) Debug.LogWarning($"[{CLASS_NAME}.ToggleStackDominance] No stacking at ({position.IntX}, {position.IntY})");
                    return;
                }

                stackInfo.IsAirDominant = !stackInfo.IsAirDominant;

                if (_debug) Debug.Log($"[{CLASS_NAME}.ToggleStackDominance] Toggled at ({position.IntX}, {position.IntY}): AirDominant={stackInfo.IsAirDominant}");

                UpdateStackingVisuals(position);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ToggleStackDominance", e);
            }
        }

        /// <summary>
        /// Refreshes all unit icons to full opacity and clears stacking state.
        /// Call this after any unit movement to reset visual state before rechecking.
        /// </summary>
        public void RefreshAllUnitOpacity()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshAllUnitOpacity] Resetting all unit opacity...");

                foreach (var kvp in unitIconPrefabs)
                {
                    kvp.Value.SetOpacity(STACKING_FULL_OPACITY);
                    kvp.Value.ShowStackingIcon(false);
                }

                stackedHexes.Clear();

                if (_debug) Debug.Log($"[{CLASS_NAME}.RefreshAllUnitOpacity] All units reset to full opacity.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RefreshAllUnitOpacity", e);
            }
        }

        /// <summary>
        /// Rechecks stacking for all unit positions.
        /// Call this after RefreshAllUnitOpacity to re-establish stacking visuals.
        /// </summary>
        public void RecheckAllStacking()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.RecheckAllStacking] Rechecking all unit positions for stacking...");

                // Collect unique positions from all units
                HashSet<Position2D> positionsToCheck = new();

                foreach (var kvp in unitIconPrefabs)
                {
                    CombatUnit unit = GameDataManager.Instance.GetCombatUnit(kvp.Key);
                    if (unit != null)
                    {
                        positionsToCheck.Add(unit.MapPos);
                    }
                }

                // Check each position for stacking
                foreach (var position in positionsToCheck)
                {
                    CheckForStacking(position);
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.RecheckAllStacking] Checked {positionsToCheck.Count} positions.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RecheckAllStacking", e);
            }
        }

        /// <summary>
        /// Clears all stacking state data.
        /// </summary>
        public void ClearStackingState()
        {
            stackedHexes.Clear();
            if (_debug) Debug.Log($"[{CLASS_NAME}.ClearStackingState] Stacking state cleared.");
        }

        /// <summary>
        /// Handles the stacking toggle request from EventManager.
        /// Delegates to ToggleStackDominance to flip the air/ground dominance at the position.
        /// </summary>
        private void OnStackingToggleRequested(Position2D position)
        {
            ToggleStackDominance(position);
        }

        /// <summary>
        /// Handles the redraw map icons event from EventManager.
        /// Clears all existing icons and redraws them.
        /// </summary>
        private void OnRedrawMapIcons()
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.OnRedrawMapIcons] Redrawing all map icons...");

                ClearAllUnitIcons();
                DrawAllUnits();

                if (_debug) Debug.Log($"[{CLASS_NAME}.OnRedrawMapIcons] Redraw complete.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnRedrawMapIcons", e);
            }
        }

        #endregion // Unit Stacking Methods

        #region Fog of War

        /// <summary>
        /// Handles spotting level changes: show/hide units transitioning to/from Level0.
        /// </summary>
        private void HandleUnitSpottedLevelChanged(CombatUnit unit, SpottedLevel oldLevel, SpottedLevel newLevel)
        {
            try
            {
                if (unit.Side == Side.Player) return;

                if (oldLevel == SpottedLevel.Level0 && newLevel > SpottedLevel.Level0)
                {
                    // Unit just became visible — create its icon
                    CreateUnitIcon(unit);
                }
                else if (newLevel == SpottedLevel.Level0 && oldLevel > SpottedLevel.Level0)
                {
                    // Unit disappeared — remove its icon
                    RemoveUnitIcon(unit.UnitID);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleUnitSpottedLevelChanged), e);
            }
        }

        #endregion // Fog of War
    }
}

