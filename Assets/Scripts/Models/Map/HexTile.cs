using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using UnityEngine;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Represents a single hex tile in the game map with JSON serialization support.
    /// Contains terrain, features, borders, labels, and game state information.
    /// </summary>
    [Serializable]
    public class HexTile : IDisposable, IJsonOnDeserialized
    {
        #region Constants

        private const string CLASS_NAME = nameof(HexTile);

        #endregion // Constants

        #region Properties

        /// <summary>
        /// Indicates if the hex is properly initialized.
        /// </summary>
        [JsonIgnore]
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates if the hex has been disposed.
        /// </summary>
        [JsonIgnore]
        public bool IsDisposed { get; private set; }

        // Core Position and Identity
        [JsonPropertyName("position")]
        public Position2D Position { get; set; }

        // Terrain Properties
        [JsonPropertyName("terrain")]
        public TerrainType Terrain { get; set; }

        // Movement Cost
        [JsonPropertyName("movementCost")]
        public int MovementCost { get; set; }

        // Infrastructure Features
        [JsonPropertyName("isRail")]
        public bool IsRail { get; set; }

        [JsonPropertyName("isRoad")]
        public bool IsRoad { get; set; }

        [JsonPropertyName("isFort")]
        public bool IsFort { get; set; }

        [JsonPropertyName("isAirbase")]
        public bool IsAirbase { get; set; }

        // Naval port hex. Mutually exclusive with Fort and Airbase. CombatUnits will check this
        // when up-deploying onto ships.
        [JsonPropertyName("isPort")]
        public bool IsPort { get; set; }

        // Game State
        [JsonPropertyName("isObjective")]
        public bool IsObjective { get; set; }

        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; }

        /// <summary>
        /// Scenario-authored flag marking hexes where the player may place ground/helo units
        /// during the Deployment phase.
        /// </summary>
        [JsonPropertyName("isDeploymentZone")]
        public bool IsDeploymentZone { get; set; }

        /// <summary>
        /// Map flag for Marine coastal landings (beachhead). Serialized data only for now — no
        /// consumer is wired to it yet.
        /// </summary>
        [JsonPropertyName("isBeachhead")]
        public bool IsBeachhead { get; set; }

        [JsonPropertyName("tileControl")]
        public TileControl TileControl { get; set; }

        [JsonPropertyName("defaultTileControl")]
        public DefaultTileControl DefaultTileControl { get; set; }

        /// <summary>
        /// Ownership-persistence scalar in the range (0, 1.0], default 1.0. This is the supply-decay
        /// hex-control value (±0.4 per Upkeep) that sits underneath the binary <see cref="TileControl"/>
        /// owner — it is NOT the owner itself. Serialized with map data.
        /// </summary>
        [JsonPropertyName("hexControlLevel")]
        public float HexControlLevel { get; set; } = 1.0f;

        // Labels and Display
        [JsonPropertyName("tileLabel")]
        public string TileLabel { get; set; }

        [JsonPropertyName("largeTileLabel")]
        public string LargeTileLabel { get; set; }

        [JsonPropertyName("labelSize")]
        public TextSize LabelSize { get; set; }

        [JsonPropertyName("labelWeight")]
        public FontWeight LabelWeight { get; set; }

        [JsonPropertyName("labelColor")]
        public TextColor LabelColor { get; set; }

        [JsonPropertyName("labelOutlineThickness")]
        public float LabelOutlineThickness { get; set; }

        // Victory and Damage
        [JsonPropertyName("victoryValue")]
        public float VictoryValue { get; set; }

        // Urban Damage is now a proxy value for urban sprawl tiles.
        [JsonPropertyName("urbanDamage")]
        public int UrbanDamage { get; set; }

        // Border Features
        [JsonPropertyName("riverBorders")]
        public JSONFeatureBorders RiverBorders { get; set; }

        [JsonPropertyName("bridgeBorders")]
        public JSONFeatureBorders BridgeBorders { get; set; }

        [JsonPropertyName("pontoonBridgeBorders")]
        public JSONFeatureBorders PontoonBridgeBorders { get; set; }

        [JsonPropertyName("damagedBridgeBorders")]
        public JSONFeatureBorders DamagedBridgeBorders { get; set; }

        /// <summary>
        /// True when any river edge is set on this hex. Computed from RiverBorders so the edges
        /// remain the single source of truth — no sync risk between a separate flag and the edge data.
        /// Renderer uses this for an early-exit optimization on non-river hexes.
        /// </summary>
        [JsonIgnore]
        public bool IsRiver => RiverBorders?.HasAnyBorders() ?? false;

        // Reserved for future use — serialized so the map/save schema stays forward-compatible.
        // These carry no behavior today; repurpose one of them later instead of breaking the schema.
        [JsonPropertyName("reservedInt1")]
        public int ReservedInt1 { get; set; }

        [JsonPropertyName("reservedInt2")]
        public int ReservedInt2 { get; set; }

        [JsonPropertyName("reservedFlag1")]
        public bool ReservedFlag1 { get; set; }

        [JsonPropertyName("reservedFlag2")]
        public bool ReservedFlag2 { get; set; }

        #endregion // Properties

        #region Private Fields

        [JsonIgnore]
        private readonly bool enableDebugLogging;

        [JsonIgnore]
        private Dictionary<HexDirection, HexTile> neighbors;

        #endregion // Private Fields

        #region Constructors

        /// <summary>
        /// Parameterless constructor used by System.Text.Json. Serializable properties are populated by
        /// the deserializer after construction; movement cost and transient state are finalized in
        /// <see cref="OnDeserialized"/>. Adding a new serialized field is now just a new property — no
        /// constructor surgery and no break to existing call sites.
        /// </summary>
        [JsonConstructor]
        public HexTile()
        {
            // Non-serialized fields only. All serializable properties are set by the deserializer
            // after this runs, then OnDeserialized() finalizes derived/transient state.
            enableDebugLogging = false;
        }

        /// <summary>
        /// Initializes a new hex instance at the specified position.
        /// Used for manual hex creation (not JSON deserialization).
        /// </summary>
        /// <param name="position">Grid position of the hex</param>
        /// <param name="enableLogging">Enable debug logging for this hex</param>
        public HexTile(Vector2Int position, bool enableLogging = false)
        {
            Position = position;
            Terrain = TerrainType.Clear;
            enableDebugLogging = enableLogging;

            // Apply default values for labels and display properties
            SetDefaults();

            // Calculate movement cost from terrain
            UpdateMovementCost();

            // PrepareBattle transient state
            Initialize();
        }

        #endregion // Constructors

        #region Initialization

        /// <summary>
        /// Initializes transient state only. All serializable properties must already be set.
        /// This method only handles [JsonIgnore] fields that cannot be serialized.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // PrepareBattle neighbors dictionary (not serialized due to circular references)
                neighbors ??= new Dictionary<HexDirection, HexTile>();

                IsInitialized = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Hex at position {Position} initialized successfully");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), ex);
                IsInitialized = false;
                throw;
            }
        }

        /// <summary>
        /// System.Text.Json post-deserialization hook. Runs after all serialized properties are set,
        /// taking over the work the old explicit JSON constructor performed: it recalculates movement
        /// cost from terrain (authoritative over any serialized value) and initializes transient state.
        /// </summary>
        public void OnDeserialized()
        {
            // Recalculate movement cost from terrain (intentionally overrides any serialized value)
            UpdateMovementCost();

            // Initialize transient (non-serialized) state
            Initialize();
        }

        /// <summary>
        /// Sets default values for labels and display properties.
        /// Used only during manual hex creation, not JSON deserialization.
        /// </summary>
        private void SetDefaults()
        {
            try
            {
                TileLabel = string.Empty;
                LargeTileLabel = string.Empty;
                LabelSize = TextSize.Small;
                LabelWeight = FontWeight.Medium;
                LabelColor = TextColor.Blue;
                LabelOutlineThickness = 0.1f;

                // Border sets — non-null so manually-created hexes can write edges (e.g. SetBorder).
                // JSON load populates these via the property setters instead; reads are null-safe (IsRiver).
                RiverBorders = new JSONFeatureBorders(BorderType.River);
                BridgeBorders = new JSONFeatureBorders(BorderType.Bridge);
                PontoonBridgeBorders = new JSONFeatureBorders(BorderType.PontoonBridge);
                DamagedBridgeBorders = new JSONFeatureBorders(BorderType.DestroyedBridge);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetDefaults), ex);
                throw;
            }
        }

        #endregion // Initialization

        #region Public Methods

        /// <summary>
        /// Sets the position of the hex.
        /// </summary>
        /// <param name="position">New position</param>
        public void SetPosition(Vector2Int position)
        {
            ValidateState();

            try
            {
                Position = position;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Position set to {position}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetPosition), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the position of the object.
        /// </summary>
        /// <param name="position">The new position to assign, represented as a <see cref="Position2D"/> object.</param>
        public void SetPosition(Position2D position)
        {
            ValidateState();

            try
            {
                Position = position;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Position set to {position}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetPosition), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the terrain type and updates the movement cost.
        /// </summary>
        /// <param name="type">The terrain type to set</param>
        public void SetTerrain(TerrainType type)
        {
            ValidateState();

            try
            {
                Terrain = type;
                UpdateMovementCost();

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Terrain at {Position} set to {type}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetTerrain), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the fort status of the hex. Mutually exclusive with airbase and port.
        /// </summary>
        /// <param name="value">True to enable fort, false to disable</param>
        public void SetIsFort(bool value)
        {
            ValidateState();

            try
            {
                IsFort = value;
                if (IsFort)
                {
                    if (IsAirbase)
                    {
                        IsAirbase = false;
                        AppService.CaptureUiMessage("Fort, Airbase, and Port are mutually exclusive. Airbase disabled.");
                    }
                    if (IsPort)
                    {
                        IsPort = false;
                        AppService.CaptureUiMessage("Fort, Airbase, and Port are mutually exclusive. Port disabled.");
                    }
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Fort status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsFort), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the airbase status of the hex. Mutually exclusive with fort and port.
        /// </summary>
        /// <param name="value">True to enable airbase, false to disable</param>
        public void SetIsAirbase(bool value)
        {
            ValidateState();

            try
            {
                IsAirbase = value;
                if (IsAirbase)
                {
                    if (IsFort)
                    {
                        IsFort = false;
                        AppService.CaptureUiMessage("Fort, Airbase, and Port are mutually exclusive. Fort disabled.");
                    }
                    if (IsPort)
                    {
                        IsPort = false;
                        AppService.CaptureUiMessage("Fort, Airbase, and Port are mutually exclusive. Port disabled.");
                    }
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Airbase status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsAirbase), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the port status of the hex. Mutually exclusive with fort and airbase.
        /// CombatUnits will check this when up-deploying onto ships.
        /// </summary>
        /// <param name="value">True to enable port, false to disable</param>
        public void SetIsPort(bool value)
        {
            ValidateState();

            try
            {
                IsPort = value;
                if (IsPort)
                {
                    if (IsFort)
                    {
                        IsFort = false;
                        AppService.CaptureUiMessage("Fort, Airbase, and Port are mutually exclusive. Fort disabled.");
                    }
                    if (IsAirbase)
                    {
                        IsAirbase = false;
                        AppService.CaptureUiMessage("Fort, Airbase, and Port are mutually exclusive. Airbase disabled.");
                    }
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Port status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsPort), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the rail status of the hex.
        /// </summary>
        /// <param name="value">True to enable rail, false to disable</param>
        public void SetIsRail(bool value)
        {
            ValidateState();

            try
            {
                IsRail = value;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Rail status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsRail), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the road status of the hex.
        /// </summary>
        /// <param name="value">True to enable road, false to disable</param>
        public void SetIsRoad(bool value)
        {
            ValidateState();

            try
            {
                IsRoad = value;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Road status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsRoad), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the objective status of the hex.
        /// </summary>
        /// <param name="value">True to make objective, false to remove</param>
        public void SetIsObjective(bool value)
        {
            ValidateState();

            try
            {
                IsObjective = value;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Objective status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsObjective), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the visibility status of the hex.
        /// </summary>
        /// <param name="value">True to make visible, false to hide</param>
        public void SetIsVisible(bool value)
        {
            ValidateState();

            try
            {
                IsVisible = value;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Visibility status at {Position} set to {value}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetIsVisible), ex);
                throw;
            }
        }

        /// <summary>
        /// Sets a neighboring hex in a specified direction.
        /// </summary>
        /// <param name="direction">Direction of the neighbor</param>
        /// <param name="neighbor">The neighboring hex (null to remove)</param>
        public void SetNeighbor(HexDirection direction, HexTile neighbor)
        {
            ValidateState();

            try
            {
                if (neighbor == null)
                {
                    neighbors.Remove(direction);
                }
                else
                {
                    neighbors[direction] = neighbor;
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Set neighbor at direction {direction} for hex at {Position}");
                }
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetNeighbor), ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a neighboring hex in a specified direction.
        /// </summary>
        /// <param name="direction">The direction of the neighboring hex to retrieve</param>
        /// <returns>The neighboring hex in the specified direction, or null if no neighbor exists</returns>
        public HexTile GetNeighbor(HexDirection direction)
        {
            try
            {
                return neighbors.TryGetValue(direction, out HexTile neighbor) ? neighbor : null;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetNeighbor), ex);
                return null;
            }
        }

        /// <summary>
        /// Gets all neighbors of this hex.
        /// </summary>
        /// <returns>Dictionary of all neighboring hexes by direction</returns>
        public IReadOnlyDictionary<HexDirection, HexTile> GetAllNeighbors()
        {
            try
            {
                return neighbors;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllNeighbors), ex);
                return new Dictionary<HexDirection, HexTile>();
            }
        }

        /// <summary>
        /// Validates the current state of this hex for consistency.
        /// </summary>
        /// <returns>True if hex is valid, false otherwise</returns>
        public bool ValidateHex()
        {
            try
            {
                // Check mutually exclusive features (Fort / Airbase / Port — at most one)
                int facilityCount = (IsFort ? 1 : 0) + (IsAirbase ? 1 : 0) + (IsPort ? 1 : 0);
                if (facilityCount > 1)
                {
                    AppService.CaptureUiMessage($"Hex at {Position}: Fort, Airbase, and Port are mutually exclusive");
                    return false;
                }

                // Check terrain consistency
                if (!Enum.IsDefined(typeof(TerrainType), Terrain))
                {
                    AppService.CaptureUiMessage($"Hex at {Position}: Invalid terrain type {Terrain}");
                    return false;
                }

                // PrepareBattle movement cost matches terrain
                int expectedCost = GetExpectedMovementCost(Terrain);
                if (MovementCost != expectedCost)
                {
                    AppService.CaptureUiMessage($"Hex at {Position}: Movement cost {MovementCost} doesn't match terrain {Terrain} (expected {expectedCost})");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(ValidateHex), ex);
                return false;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        /// <summary>
        /// Updates the movement cost based on current terrain type.
        /// </summary>
        private void UpdateMovementCost()
        {
            try
            {
                MovementCost = GetExpectedMovementCost(Terrain);
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateMovementCost), ex);
                MovementCost = (int)HexMovementCost.Clear; // Safe default
            }
        }

        /// <summary>
        /// Gets the expected movement cost for a terrain type.
        /// </summary>
        /// <param name="terrain">Terrain type</param>
        /// <returns>Movement cost for the terrain</returns>
        private static int GetExpectedMovementCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Water => (int)HexMovementCost.Water,
                TerrainType.Clear => (int)HexMovementCost.Clear,
                TerrainType.Forest => (int)HexMovementCost.Forest,
                TerrainType.Rough => (int)HexMovementCost.Rough,
                TerrainType.Marsh => (int)HexMovementCost.Marsh,
                TerrainType.Mountains => (int)HexMovementCost.Mountains,
                TerrainType.MinorCity => (int)HexMovementCost.MinorCity,
                TerrainType.MajorCity => (int)HexMovementCost.MajorCity,
                TerrainType.Impassable => (int)HexMovementCost.Impassable,
                _ => (int)HexMovementCost.Clear
            };
        }

        /// <summary>
        /// Validates the current state of the hex.
        /// </summary>
        private void ValidateState()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{CLASS_NAME} is not initialized");
            }

            if (IsDisposed)
            {
                throw new ObjectDisposedException(CLASS_NAME);
            }
        }

        #endregion // Private Methods

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the hex and its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the hex's resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources, false if finalizing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Clear neighbors dictionary
                        neighbors?.Clear();

                        // Clear border features
                        RiverBorders = null;
                        BridgeBorders = null;
                        PontoonBridgeBorders = null;
                        DamagedBridgeBorders = null;

                        if (enableDebugLogging)
                        {
                            Debug.Log($"{CLASS_NAME}: Hex at position {Position} disposed");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppService.HandleException(CLASS_NAME, nameof(Dispose), ex);
                    }
                }

                IsDisposed = true;
            }
        }

        #endregion // IDisposable Implementation
    }
}
