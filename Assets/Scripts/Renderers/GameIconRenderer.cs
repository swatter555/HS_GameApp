using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
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

        [Header("Rendering Layers")]
        [SerializeField] private GameObject groundUnitLayer;
        [SerializeField] private GameObject airUnitLayer;
        [SerializeField] private GameObject utilityLayer1;
        [SerializeField] private GameObject utilityLayer2;

        #endregion // Inspector Fields

        #region Properties

        /// <summary>
        /// Indicates if the renderer is properly initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the GameObject reference for the ground unit layer.
        /// </summary>
        public GameObject GroundUnitLayer => groundUnitLayer;

        /// <summary>
        /// Gets the GameObject reference for the air unit layer.
        /// </summary>
        public GameObject AirUnitLayer => airUnitLayer;

        /// <summary>
        /// Gets the GameObject reference for utility layer 1.
        /// </summary>
        public GameObject UtilityLayer1 => utilityLayer1;

        /// <summary>
        /// Gets the GameObject reference for utility layer 2.
        /// </summary>
        public GameObject UtilityLayer2 => utilityLayer2;

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
        /// Unity's Update method. Reserved for future animation and update logic.
        /// </summary>
        private void Update()
        {
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
                EventManager.Instance.OnUnitPositionChanged += OnUnitPositionChanged;
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
                EventManager.Instance.OnUnitPositionChanged -= OnUnitPositionChanged;
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

            if (groundUnitLayer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(groundUnitLayer)} is missing.");

            if (airUnitLayer == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(airUnitLayer)} is missing.");

            if (utilityLayer1 == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(utilityLayer1)} is missing.");

            if (utilityLayer2 == null)
                throw new NullReferenceException($"{CLASS_NAME}.ValidateComponents: {nameof(utilityLayer2)} is missing.");

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
                ClearContainer(groundUnitLayer.transform);
                ClearContainer(airUnitLayer.transform);

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
        public void DrawAllUnits(ref int unitCount)
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

                if (_debug) Debug.Log($"[{CLASS_NAME}.CreateUnitIcon] Creating icon for unit '{unit.UnitName}' at ({unit.MapPos.IntX}, {unit.MapPos.IntY})");

                // Determine appropriate layer based on unit type
                GameObject targetLayer = GetTargetLayerForUnit(unit);

                // Create prefab instance
                GameObject unitIconObject = UnityEngine.Object.Instantiate(SpriteManager.Instance.UnitIconPrefab, targetLayer.transform);
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

                // Set nationality symbol
                string symbolSprite = GetNationalSymbol(unit.Nationality);
                unitIcon.SetFlag(symbolSprite);

                // Set hit points display
                unitIcon.InitializeHitPointsText();
                unitIcon.HitPointsRatio = $"{unit.HitPoints.Current}/{unit.HitPoints.Max}";

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
                Position2D unitPosition = Position2D.Zero;
                CombatUnit unit = GameDataManager.Instance.GetCombatUnit(unitId);
                if (unit != null)
                {
                    unitPosition = unit.MapPos;
                }

                if (unitIconPrefabs.TryGetValue(unitId, out Prefab_CombatUnitIcon unitIcon))
                {
                    if (unitIcon != null)
                    {
                        Destroy(unitIcon.gameObject);
                    }
                    unitIconPrefabs.Remove(unitId);

                    if (_debug) Debug.Log($"[{CLASS_NAME}.RemoveUnitIcon] Removed icon for unit '{unitId}'");

                    // Recheck stacking at the removed unit's position
                    if (unitPosition != null)
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
        ///
        /// Icon Categories:
        /// 0. Airbases: AirbaseStack0-4 based on attached air unit count
        /// 1. Animated: Base + _Frame0 suffix (helicopters)
        /// 2. 3-Direction + Fired: Base + _W/_NW/_SW + optional _F (artillery, rockets, SPAAA, SPSAM)
        /// 3. 3-Direction Vehicles: Base + _W/_NW/_SW suffix (tanks, IFVs, APCs, recon, trucks)
        /// 4. No Suffix: Base name only (infantry, aircraft, static SAMs, bases)
        ///
        /// All icons can be flipped horizontally for easterly directions (E, NE, SE).
        /// </summary>
        public string GetSpriteNameForUnit(CombatUnit unit, out bool shouldFlip)
        {
            shouldFlip = false;

            try
            {
                // Category 0: Airbases use stacking icons based on attached air unit count
                if (unit.Classification == UnitClassification.AIRB)
                {
                    int airUnitCount = unit.GetAttachedAirUnitCount();
                    string airbaseSprite = GetAirbaseStackSprite(airUnitCount);

                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Airbase '{unit.UnitName}': {airUnitCount} air units attached, sprite={airbaseSprite}");
                    return airbaseSprite;
                }

                // Get active weapon system based on deployment position (handles Embarked, Mobile, Deployed)
                WeaponSystems weaponSystem = GetActiveWeaponSystem(unit);
                Nationality nationality = unit.Nationality;

                // All icons flip for easterly directions
                shouldFlip = ShouldFlipSprite(unit.Facing);

                // Category 1: Animated sprites (helicopters)
                if (IsAnimatedSprite(weaponSystem))
                {
                    string animatedSprite = GetAnimatedSpriteName(weaponSystem);
                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': Animated sprite={animatedSprite}, Flip={shouldFlip}");
                    return animatedSprite ?? SpriteManager.Utility_MismatchIcon;
                }

                // Category 2: 3-Direction + Fired variant (artillery, rockets, SSM, S300, SPAAA, SPSAM)
                if (Has3DirectionsAndFiredVariant(weaponSystem))
                {
                    string baseSprite = GetBaseSpriteFromProfile(weaponSystem, nationality);
                    if (string.IsNullOrEmpty(baseSprite))
                    {
                        return SpriteManager.Utility_MismatchIcon;
                    }

                    string directionSuffix = GetDirectionSuffixFor3Dir(unit.Facing);
                    string firedSuffix = IsFiredPosition(unit.DeploymentPosition) ? "_F" : "";
                    string finalSprite = baseSprite + directionSuffix + firedSuffix;

                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': 3-Dir+Fired={finalSprite}, Flip={shouldFlip}");
                    return finalSprite;
                }

                // Category 3: 3-Direction vehicles without fired variant (tanks, IFVs, APCs, recon, trucks)
                if (Has3Directions(weaponSystem))
                {
                    string baseSprite = GetBaseSpriteFromProfile(weaponSystem, nationality);
                    if (string.IsNullOrEmpty(baseSprite))
                    {
                        return SpriteManager.Utility_MismatchIcon;
                    }

                    string directionSuffix = GetDirectionSuffixFor3Dir(unit.Facing);
                    string finalSprite = baseSprite + directionSuffix;

                    if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': 3-Dir vehicle={finalSprite}, Flip={shouldFlip}");
                    return finalSprite;
                }

                // Category 4: No suffix (infantry, aircraft, SAMs, bases, transports, generic)
                string noSuffixSprite = GetBaseSpriteFromProfile(weaponSystem, nationality);
                if (string.IsNullOrEmpty(noSuffixSprite))
                {
                    return SpriteManager.Utility_MismatchIcon;
                }

                if (_debug) Debug.Log($"[{CLASS_NAME}.GetSpriteNameForUnit] Unit '{unit.UnitName}': No suffix={noSuffixSprite}, Flip={shouldFlip}");
                return noSuffixSprite;
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
        /// Uses HexMapRenderer for coordinate conversion.
        /// </summary>
        private Vector3 GetRenderPosition(Vector2Int gridPos)
        {
            if (HexMapRenderer.Instance != null && HexMapRenderer.Instance.IsInitialized)
            {
                return HexMapRenderer.Instance.GetRenderPosition(gridPos);
            }

            Debug.LogWarning($"{CLASS_NAME}.GetRenderPosition: HexMapRenderer not available.");
            return Vector3.zero;
        }

        /// <summary>
        /// Determines the appropriate rendering layer for a unit.
        /// Ground units, bases, and helicopters go to GroundUnitLayer.
        /// Fixed-wing aircraft go to AirUnitLayer.
        /// </summary>
        private GameObject GetTargetLayerForUnit(CombatUnit unit)
        {
            WeaponSystems weaponSystem = GetActiveWeaponSystem(unit);

            // Fixed-wing aircraft go to air layer
            if (IsFixedWingAircraft(weaponSystem))
            {
                return airUnitLayer;
            }

            // Everything else (ground units, bases, helicopters) goes to ground layer
            return groundUnitLayer;
        }

        /// <summary>
        /// Checks if weapon system is a fixed-wing aircraft (not helicopter).
        /// </summary>
        private bool IsFixedWingAircraft(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Fighters
                WeaponSystems.FGT_MIG21 => true,
                WeaponSystems.FGT_MIG23 => true,
                WeaponSystems.FGT_MIG25 => true,
                WeaponSystems.FGT_MIG29 => true,
                WeaponSystems.FGT_MIG31 => true,
                WeaponSystems.FGT_MIG27 => true,
                WeaponSystems.FGT_SU27 => true,
                WeaponSystems.FGT_SU47 => true,
                WeaponSystems.FGT_F15 => true,
                WeaponSystems.FGT_F4 => true,
                WeaponSystems.FGT_F16 => true,
                WeaponSystems.FGT_TORNADO_IDS => true,
                WeaponSystems.FGT_TORNADO_GR1 => true,
                WeaponSystems.FGT_MIRAGE2000 => true,

                // Attack Aircraft
                WeaponSystems.ATT_SU25 => true,
                WeaponSystems.ATT_SU25B => true,
                WeaponSystems.ATT_A10 => true,
                WeaponSystems.ATT_JAGUAR => true,

                // Bombers
                WeaponSystems.BMB_SU24 => true,
                WeaponSystems.BMB_TU16 => true,
                WeaponSystems.BMB_TU22 => true,
                WeaponSystems.BMB_TU22M3 => true,
                WeaponSystems.BMB_F111 => true,
                WeaponSystems.BMB_F117 => true,

                // Recon Aircraft
                WeaponSystems.RCNA_MIG25R => true,
                WeaponSystems.RCNA_SR71 => true,

                // AWACS
                WeaponSystems.AWACS_A50 => true,
                WeaponSystems.AWACS_E3 => true,

                _ => false
            };
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
        /// Checks if weapon system uses animated sprites (helicopters).
        /// Animated sprites have Frame0-5 variants for rotor animation.
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
        /// Gets the Frame0 sprite name for animated weapon systems.
        /// TODO: Implement animation cycling when unit is selected.
        /// </summary>
        private string GetAnimatedSpriteName(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Soviet Helicopters
                WeaponSystems.HEL_MI8T => SpriteManager.SV_MI8_Frame0,
                WeaponSystems.HEL_MI8AT => SpriteManager.SV_MI8AT_Frame0,
                WeaponSystems.HEL_MI24D => SpriteManager.SV_MI24D_Frame0,
                WeaponSystems.HEL_MI24V => SpriteManager.SV_MI24V_Frame0,
                WeaponSystems.HEL_MI28 => SpriteManager.SV_MI28_Frame0,
                // US Helicopters
                WeaponSystems.HEL_AH64 => SpriteManager.US_AH64_Frame0,
                WeaponSystems.HEL_UH60 => SpriteManager.US_UH60_Frame0,
                // German Helicopters
                WeaponSystems.HEL_BO105 => SpriteManager.GE_BO105_Frame0,
                _ => null
            };
        }

        /// <summary>
        /// Checks if weapon system has BOTH 3 direction variants (W, NW, SW) AND a fired (_F) variant.
        /// These are artillery, rocket systems, SSMs, S300, and mobile AA/SAM systems that show
        /// both directional facing and deployed/firing position.
        /// </summary>
        private bool Has3DirectionsAndFiredVariant(WeaponSystems weaponSystem)
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

                // Soviet S300
                WeaponSystems.SAM_S300 => true,

                // SPAAA (mobile anti-aircraft)
                WeaponSystems.SPAAA_ZSU57 => true,
                WeaponSystems.SPAAA_ZSU23 => true,
                WeaponSystems.SPAAA_2K22 => true,
                WeaponSystems.SPAAA_M163 => true,
                WeaponSystems.SPSAM_GEPARD => true,

                // SPSAM (mobile SAM)
                WeaponSystems.SPSAM_9K31 => true,
                WeaponSystems.SPSAM_CHAP => true,
                WeaponSystems.SPAAA_ROLAND_FR => true,

                // US/NATO Artillery (shared M109 has nation-specific sprites)
                WeaponSystems.SPA_M109 => true,
                WeaponSystems.ROC_MLRS => true,

                _ => false
            };
        }

        /// <summary>
        /// Checks if weapon system has 3 direction variants (W, NW, SW) WITHOUT a fired variant.
        /// These are mobile ground vehicles that show directional facing only.
        /// </summary>
        private bool Has3Directions(WeaponSystems weaponSystem)
        {
            return weaponSystem switch
            {
                // Tanks
                WeaponSystems.TANK_T55A => true,
                WeaponSystems.TANK_T64A => true,
                WeaponSystems.TANK_T64B => true,
                WeaponSystems.TANK_T72A => true,
                WeaponSystems.TANK_T72B => true,
                WeaponSystems.TANK_T80B => true,
                WeaponSystems.TANK_T80U => true,
                WeaponSystems.TANK_T80BV => true,
                WeaponSystems.TANK_M1 => true,
                WeaponSystems.TANK_M60A3 => true,
                WeaponSystems.TANK_LEOPARD1 => true,
                WeaponSystems.TANK_LEOPARD2 => true,
                WeaponSystems.TANK_CHALLENGER1 => true,
                WeaponSystems.TANK_AMX30 => true,

                // APCs
                WeaponSystems.APC_MTLB => true,
                WeaponSystems.APC_BTR70 => true,
                WeaponSystems.APC_BTR80 => true,
                WeaponSystems.APC_M113 => true,
                WeaponSystems.APC_LVTP7 => true,
                WeaponSystems.APC_VAB => true,

                // IFVs
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

                // Recon
                WeaponSystems.RCN_BRDM2 => true,
                WeaponSystems.RCN_BRDM2AT => true,

                // Trucks
                WeaponSystems.TRUCK_GEN => true,

                _ => false
            };
        }

        /// <summary>
        /// Determines if unit is in a "fired" deployment position.
        /// Units in defensive positions show their deployed/firing sprite.
        /// </summary>
        private bool IsFiredPosition(DeploymentPosition position)
        {
            return position == DeploymentPosition.HastyDefense ||
                   position == DeploymentPosition.Entrenched ||
                   position == DeploymentPosition.Fortified;
        }

        #endregion // Icon Category Methods

        #region Direction and Flip Methods

        /// <summary>
        /// Gets direction suffix for 3-direction vehicle sprites.
        /// Returns _W, _NW, or _SW based on facing. Easterly directions use
        /// their western counterpart (flipped horizontally by the renderer).
        /// </summary>
        private string GetDirectionSuffixFor3Dir(HexDirection facing)
        {
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
        /// All icon types (vehicles, infantry, aircraft, etc.) flip for easterly directions.
        /// </summary>
        private bool ShouldFlipSprite(HexDirection facing)
        {
            return facing == HexDirection.E ||
                   facing == HexDirection.NE ||
                   facing == HexDirection.SE;
        }

        #endregion // Direction and Flip Methods

        #region Profile Resolution Methods

        /// <summary>
        /// Gets the active weapon system based on unit deployment position.
        /// - Embarked: Uses EmbarkedProfileID (air or naval transport)
        /// - Mobile: Uses MobileProfileID if available
        /// - Deployed/Defensive: Uses DeployedProfileID
        /// </summary>
        private WeaponSystems GetActiveWeaponSystem(CombatUnit unit)
        {
            return unit.DeploymentPosition switch
            {
                DeploymentPosition.Embarked => unit.EmbarkedProfileID != WeaponSystems.NONE
                    ? unit.EmbarkedProfileID
                    : unit.DeployedProfileID,

                DeploymentPosition.Mobile => unit.MobileProfileID != WeaponSystems.NONE
                    ? unit.MobileProfileID
                    : unit.DeployedProfileID,

                _ => unit.DeployedProfileID
            };
        }

        /// <summary>
        /// Maps WeaponSystems enum to base sprite name.
        /// Handles nationality-specific sprites for shared equipment (M109, M113, Gepard, etc.).
        ///
        /// NOTE: Arab and Chinese nations have dedicated sprite constants (AR_, CH_ prefixes)
        /// but require WeaponSystems enum additions before full integration.
        /// </summary>
        private string GetBaseSpriteFromProfile(WeaponSystems profile, Nationality nationality)
        {
            // Handle infantry profiles - requires nationality-specific sprites
            if (IsInfantryProfile(profile))
            {
                return GetInfantrySprite(profile, nationality);
            }

            // Handle Mujahideen special equipment sprites
            if (nationality == Nationality.MJ)
            {
                string mjSprite = GetMujahideenSprite(profile);
                if (!string.IsNullOrEmpty(mjSprite))
                {
                    return mjSprite;
                }
            }

            // Handle nationality-specific vehicle sprites (M109, M113, Gepard shared across nations)
            string nationalitySprite = GetNationalitySpecificSprite(profile, nationality);
            if (!string.IsNullOrEmpty(nationalitySprite))
            {
                return nationalitySprite;
            }

            // Default weapon system mappings
            return profile switch
            {
                // ═══════════════════════════════════════════════════════════════
                // SOVIET WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // Tanks
                WeaponSystems.TANK_T55A => "SV_T55A",
                WeaponSystems.TANK_T64A => "SV_T64A",
                WeaponSystems.TANK_T64B => "SV_T64B",
                WeaponSystems.TANK_T72A => "SV_T72A",
                WeaponSystems.TANK_T72B => "SV_T72B",
                WeaponSystems.TANK_T80B => "SV_T80B",
                WeaponSystems.TANK_T80U => "SV_T80U",
                WeaponSystems.TANK_T80BV => "SV_T80BVM",

                // APCs
                WeaponSystems.APC_MTLB => "SV_MTLB",
                WeaponSystems.APC_BTR70 => "SV_BTR70",
                WeaponSystems.APC_BTR80 => "SV_BTR80",

                // IFVs
                WeaponSystems.IFV_BMP1 => "SV_BMP1",
                WeaponSystems.IFV_BMP2 => "SV_BMP2",
                WeaponSystems.IFV_BMP3 => "SV_BMP3",
                WeaponSystems.IFV_BMD1 => "SV_BMD2", // Fallback: no BMD1 sprite
                WeaponSystems.IFV_BMD2 => "SV_BMD2",
                WeaponSystems.IFV_BMD3 => "SV_BMD3",

                // Recon
                WeaponSystems.RCN_BRDM2 => "SV_BRDM2",
                WeaponSystems.RCN_BRDM2AT => "SV_BRDM2AT",

                // Self-Propelled Artillery
                WeaponSystems.SPA_2S1 => "SV_2S1",
                WeaponSystems.SPA_2S3 => "SV_2S3",
                WeaponSystems.SPA_2S5 => "SV_2S5",
                WeaponSystems.SPA_2S19 => "SV_2S19",

                // Rocket Artillery
                WeaponSystems.ROC_BM21 => "SV_BM21",
                WeaponSystems.ROC_BM27 => "SV_BM27",
                WeaponSystems.ROC_BM30 => "SV_BM30",

                // SSM
                WeaponSystems.SSM_SCUD => "SV_ScudB",

                // SPAAA
                WeaponSystems.SPAAA_ZSU57 => "SV_ZSU57",
                WeaponSystems.SPAAA_ZSU23 => "SV_ZSU23",
                WeaponSystems.SPAAA_2K22 => "SV_2K22",

                // SPSAM
                WeaponSystems.SPSAM_9K31 => "SV_9K31",

                // Static SAM
                WeaponSystems.SAM_S75 => "SV_S75",
                WeaponSystems.SAM_S125 => "SV_S125",
                WeaponSystems.SAM_S300 => "SV_S300",

                // AWACS
                WeaponSystems.AWACS_A50 => "SV_A50",

                // Fighters
                WeaponSystems.FGT_MIG21 => "SV_Mig21",
                WeaponSystems.FGT_MIG23 => "SV_Mig23",
                WeaponSystems.FGT_MIG25 => "SV_Mig25",
                WeaponSystems.FGT_MIG29 => "SV_Mig29",
                WeaponSystems.FGT_MIG31 => "SV_Mig31",
                WeaponSystems.FGT_SU27 => "SV_SU27",
                WeaponSystems.FGT_SU47 => "SV_SU47",
                WeaponSystems.FGT_MIG27 => "SV_Mig27",

                // Attack Aircraft
                WeaponSystems.ATT_SU25 => "SV_SU25",
                WeaponSystems.ATT_SU25B => "SV_SU25B",

                // Bombers
                WeaponSystems.BMB_SU24 => "SV_SU24",
                WeaponSystems.BMB_TU16 => "SV_TU16",
                WeaponSystems.BMB_TU22 => "SV_TU22",
                WeaponSystems.BMB_TU22M3 => "SV_TU22M3",

                // Recon Aircraft
                WeaponSystems.RCNA_MIG25R => "SV_Mig25R",

                // Transport
                WeaponSystems.TRN_AN8 => SpriteManager.SV_AN8,
                WeaponSystems.TRN_NAVAL => SpriteManager.GEN_NavalTransport,

                // ═══════════════════════════════════════════════════════════════
                // USA WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // Tanks
                WeaponSystems.TANK_M1 => "US_M1",
                WeaponSystems.TANK_M60A3 => "US_M60",

                // IFVs
                WeaponSystems.IFV_M2 => "US_M2",
                WeaponSystems.IFV_M3 => "US_M2", // Uses M2 sprite

                // APCs (default - nationality override handled above)
                WeaponSystems.APC_M113 => "US_M113",
                WeaponSystems.APC_LVTP7 => "US_LVTP",

                // Artillery (default - nationality override handled above)
                WeaponSystems.SPA_M109 => "US_M109",
                WeaponSystems.ROC_MLRS => "US_MLRS",

                // SPAAA
                WeaponSystems.SPAAA_M163 => "US_M163",

                // SPSAM
                WeaponSystems.SPSAM_CHAP => "US_Chaparral",

                // Static SAM
                WeaponSystems.SAM_HAWK => "US_Hawk",
                WeaponSystems.SAM_RAPIER => "US_Hawk", // UK Rapier uses Hawk sprite

                // AWACS
                WeaponSystems.AWACS_E3 => "US_E3",

                // Fighters
                WeaponSystems.FGT_F15 => "US_F15",
                WeaponSystems.FGT_F4 => "US_F4",
                WeaponSystems.FGT_F16 => "US_F16",

                // Attack Aircraft
                WeaponSystems.ATT_A10 => "US_A10",

                // Bombers
                WeaponSystems.BMB_F111 => "US_F111",
                WeaponSystems.BMB_F117 => "US_F117",

                // Recon Aircraft
                WeaponSystems.RCNA_SR71 => "US_SR71",

                // ═══════════════════════════════════════════════════════════════
                // WEST GERMANY (FRG) WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // Tanks
                WeaponSystems.TANK_LEOPARD1 => "GE_Leopard1",
                WeaponSystems.TANK_LEOPARD2 => "GE_Leopard2",

                // IFVs
                WeaponSystems.IFV_MARDER => "GE_Marder",

                // SPAAA (default - nationality override handled above)
                WeaponSystems.SPSAM_GEPARD => "GE_Gepard",

                // Aircraft
                WeaponSystems.FGT_TORNADO_IDS => "GE_Tornado",

                // ═══════════════════════════════════════════════════════════════
                // UK WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // Tanks
                WeaponSystems.TANK_CHALLENGER1 => "UK_Challenger1",

                // IFVs
                WeaponSystems.IFV_WARRIOR => "UK_Warrior",

                // Aircraft
                WeaponSystems.FGT_TORNADO_GR1 => "UK_TornadoGR1",

                // ═══════════════════════════════════════════════════════════════
                // FRANCE WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                // Tanks
                WeaponSystems.TANK_AMX30 => "FR_AMX30",

                // APCs
                WeaponSystems.APC_VAB => "FR_M113", // VAB uses M113 sprite

                // SPSAM
                WeaponSystems.SPAAA_ROLAND_FR => "FR_Roland",

                // Aircraft
                WeaponSystems.FGT_MIRAGE2000 => "FR_Mirage2000",
                WeaponSystems.ATT_JAGUAR => "FR_Jaguar",

                // ═══════════════════════════════════════════════════════════════
                // GENERIC WEAPON SYSTEMS
                // ═══════════════════════════════════════════════════════════════

                WeaponSystems.AAA_GEN => SpriteManager.GEN_AA,
                WeaponSystems.ART_LIGHT_GEN => SpriteManager.GEN_LightArt,
                WeaponSystems.ART_HEAVY_GEN => SpriteManager.GEN_HeavyArt,
                WeaponSystems.ART_LIGHT_MORTAR_GEN => SpriteManager.GEN_LightArt,
                WeaponSystems.ART_HEAVY_MORTAR_GEN => SpriteManager.GEN_HeavyArt,
                WeaponSystems.TRUCK_GEN => "GEN_Truck",
                WeaponSystems.INF_CAV_GEN => SpriteManager.MJ_Mounted,
                WeaponSystems.MANPAD_GEN => SpriteManager.GEN_AA,
                WeaponSystems.AT_LIGHT_GEN => SpriteManager.GEN_LightArt,

                // Bases
                WeaponSystems.LANDBASE_GENERIC => SpriteManager.GEN_Base,
                WeaponSystems.AIRBASE_GENERIC => SpriteManager.GEN_Base, // TODO: Needs airbase sprite
                WeaponSystems.SUPPLYDEPOT_GENERIC => SpriteManager.GEN_Depot,

                // System values
                WeaponSystems.UTILITY_ID => null,
                WeaponSystems.NONE => null,

                _ => null
            };
        }

        /// <summary>
        /// Returns nationality-specific sprite for shared equipment.
        /// Equipment like M109, M113, and Gepard have different sprites per nation.
        /// Returns null if no nationality-specific sprite exists.
        /// </summary>
        private string GetNationalitySpecificSprite(WeaponSystems profile, Nationality nationality)
        {
            // M109 Self-Propelled Artillery - US, UK, GE variants
            if (profile == WeaponSystems.SPA_M109)
            {
                return nationality switch
                {
                    Nationality.USA => "US_M109",
                    Nationality.UK => "UK_M109",
                    Nationality.FRG => "GE_M109",
                    Nationality.IR or Nationality.IQ or Nationality.SAUD or Nationality.KW => "US_M109",
                    _ => null // Fall through to default
                };
            }

            // M113 APC - US, FR variants
            if (profile == WeaponSystems.APC_M113)
            {
                return nationality switch
                {
                    Nationality.USA => "US_M113",
                    Nationality.FRA => "FR_M113",
                    Nationality.FRG => "US_M113", // Germany uses US sprite
                    Nationality.UK => "US_M113",  // UK uses US sprite
                    // Arab nations
                    Nationality.IR or Nationality.IQ or Nationality.SAUD or Nationality.KW => "US_M113",
                    _ => null
                };
            }

            // Gepard SPAAA - GE, FR variants
            if (profile == WeaponSystems.SPSAM_GEPARD)
            {
                return nationality switch
                {
                    Nationality.FRG => "GE_Gepard",
                    Nationality.FRA => "FR_Gepard",
                    _ => null
                };
            }

            // MLRS - currently only US has sprite
            if (profile == WeaponSystems.ROC_MLRS)
            {
                return nationality switch
                {
                    Nationality.USA => "US_MLRS",
                    Nationality.UK => "US_MLRS",   // UK uses US sprite
                    Nationality.FRG => "US_MLRS", // Germany uses US sprite
                    _ => null
                };
            }

            return null;
        }

        #endregion // Profile Resolution Methods

        #region Infantry and Special Unit Sprites

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
        ///
        /// NOTE: Saudi and Kuwait currently share IR_Regulars sprite. China uses CH_Infantry.
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
                (WeaponSystems.INF_MAR, Nationality.USSR) => SpriteManager.SV_Regulars,

                // US Infantry
                (WeaponSystems.INF_REG, Nationality.USA) => SpriteManager.US_Regulars,
                (WeaponSystems.INF_AB, Nationality.USA) => SpriteManager.US_Airborne,
                (WeaponSystems.INF_MAR, Nationality.USA) => SpriteManager.US_Marines,
                (_, Nationality.USA) => SpriteManager.US_Regulars,

                // UK Infantry
                (_, Nationality.UK) => SpriteManager.UK_Regulars,

                // German Infantry
                (_, Nationality.FRG) => SpriteManager.GER_Regulars,

                // French Infantry
                (_, Nationality.FRA) => SpriteManager.FR_Regulars,

                // Mujahideen Infantry
                (WeaponSystems.INF_SPEC, Nationality.MJ) => SpriteManager.MJ_Elite,
                (_, Nationality.MJ) => SpriteManager.MJ_Regulars,

                // Arab Nations
                (_, Nationality.IR) => SpriteManager.IR_Regulars,
                (_, Nationality.IQ) => SpriteManager.IQ_Regulars,
                (_, Nationality.SAUD) => SpriteManager.IR_Regulars,  // Uses IR sprite
                (_, Nationality.KW) => SpriteManager.IR_Regulars,    // Uses IR sprite

                // China
                (_, Nationality.China) => SpriteManager.CH_Infantry,

                // Default fallback
                _ => SpriteManager.SV_Regulars
            };
        }

        /// <summary>
        /// Gets Mujahideen-specific sprites for special weapon systems.
        /// MJ units use unique sprites for mortars, manpads, RPGs, etc.
        /// </summary>
        private string GetMujahideenSprite(WeaponSystems profile)
        {
            return profile switch
            {
                WeaponSystems.ART_LIGHT_MORTAR_GEN => SpriteManager.MJ_Mortar,
                WeaponSystems.ART_HEAVY_MORTAR_GEN => SpriteManager.MJ_Mortar,
                WeaponSystems.MANPAD_GEN => SpriteManager.MJ_Stinger,
                WeaponSystems.AT_LIGHT_GEN => SpriteManager.MJ_RPG,
                WeaponSystems.AAA_GEN => SpriteManager.MJ_AA,
                WeaponSystems.ART_LIGHT_GEN => SpriteManager.MJ_Artillery,
                WeaponSystems.INF_CAV_GEN => SpriteManager.MJ_Mounted,
                _ => null
            };
        }

        #endregion // Infantry and Special Unit Sprites

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

        #endregion // Nationality Symbol Methods

        #region Unit Stacking Methods

        /// <summary>
        /// Determines if a unit is an air unit based on its classification.
        /// Air units include fighters, attackers, bombers, and reconnaissance aircraft.
        /// </summary>
        /// <param name="unit">The unit to check</param>
        /// <returns>True if the unit is an air unit</returns>
        private bool IsAirUnit(CombatUnit unit)
        {
            return unit.Classification == UnitClassification.FGT ||
                   unit.Classification == UnitClassification.ATT ||
                   unit.Classification == UnitClassification.BMB ||
                   unit.Classification == UnitClassification.RECONA;
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
        /// Handles unit position changed events from EventManager.
        /// Checks both old and new positions for stacking changes.
        /// </summary>
        private void OnUnitPositionChanged(UnitPositionChangedEventArgs args)
        {
            try
            {
                if (_debug) Debug.Log($"[{CLASS_NAME}.OnUnitPositionChanged] Unit {args.UnitID} moved from ({args.OldPosition.IntX}, {args.OldPosition.IntY}) to ({args.NewPosition.IntX}, {args.NewPosition.IntY})");

                // Check old position - stacking may have been resolved
                CheckForStacking(args.OldPosition);

                // Check new position - new stacking may have occurred
                CheckForStacking(args.NewPosition);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "OnUnitPositionChanged", e);
            }
        }

        #endregion // Unit Stacking Methods
    }
}
