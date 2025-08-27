using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models.Map
{
    /// <summary>
    /// Represents a single hex tile in the game map with JSON serialization support.
    /// Contains terrain, features, borders, labels, and game state information.
    /// </summary>
    [Serializable]
    public class Hex : IDisposable
    {
        #region Constants
        private const string CLASS_NAME = nameof(Hex);
        #endregion

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
        [JsonInclude]
        public Vector2Int Position { get; set; }

        // Terrain Properties
        [JsonInclude]
        public TerrainType Terrain { get; set; }

        [JsonInclude]
        public int MovementCost { get; private set; }

        // Infrastructure Features
        [JsonInclude]
        public bool IsRail { get; set; }

        [JsonInclude]
        public bool IsRoad { get; set; }

        [JsonInclude]
        public bool IsFort { get; set; }

        [JsonInclude]
        public bool IsAirbase { get; set; }

        // Game State
        [JsonInclude]
        public bool IsObjective { get; set; }

        [JsonInclude]
        public bool IsVisible { get; set; }

        [JsonInclude]
        public TileControl TileControl { get; set; }

        [JsonInclude]
        public DefaultTileControl DefaultTileControl { get; set; }

        // Labels and Display
        [JsonInclude]
        public string TileLabel { get; set; }

        [JsonInclude]
        public string LargeTileLabel { get; set; }

        [JsonInclude]
        public TextSize LabelSize { get; set; }

        [JsonInclude]
        public FontWeight LabelWeight { get; set; }

        [JsonInclude]
        public TextColor LabelColor { get; set; }

        [JsonInclude]
        public float LabelOutlineThickness { get; set; }

        // Victory and Damage
        [JsonInclude]
        public float VictoryValue { get; set; }

        [JsonInclude]
        public float AirbaseDamage { get; set; }

        [JsonInclude]
        public int UrbanDamage { get; set; }

        // Border Features
        [JsonInclude]
        public FeatureBorders RiverBorders { get; set; }

        [JsonInclude]
        public FeatureBorders BridgeBorders { get; set; }

        [JsonInclude]
        public FeatureBorders PontoonBridgeBorders { get; set; }

        [JsonInclude]
        public FeatureBorders DamagedBridgeBorders { get; set; }

        #endregion

        #region Private Fields

        [JsonIgnore]
        private readonly bool enableDebugLogging;

        [JsonIgnore]
        private Dictionary<HexDirection, Hex> neighbors;

        #endregion

        #region Constructors

        /// <summary>
        /// Parameterless constructor for JSON serialization.
        /// </summary>
        [JsonConstructor]
        public Hex()
        {
            enableDebugLogging = false;
            Initialize();
        }

        /// <summary>
        /// Initializes a new hex instance at the specified position.
        /// </summary>
        /// <param name="position">Grid position of the hex</param>
        /// <param name="enableLogging">Enable debug logging for this hex</param>
        public Hex(Vector2Int position, bool enableLogging = false)
        {
            Position = position;
            enableDebugLogging = enableLogging;
            Initialize();
        }

        /// <summary>
        /// Initializes a new hex instance at the specified coordinates.
        /// </summary>
        /// <param name="posX">X coordinate</param>
        /// <param name="posY">Y coordinate</param>
        /// <param name="enableLogging">Enable debug logging for this hex</param>
        public Hex(int posX, int posY, bool enableLogging = false)
            : this(new Vector2Int(posX, posY), enableLogging)
        {
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the hex with default values.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Initialize terrain and movement
                if (Terrain == default)
                {
                    Terrain = TerrainType.Clear;
                }
                UpdateMovementCost();

                // Initialize infrastructure features
                // Keep existing values if they were set during deserialization

                // Initialize labels and display if not already set
                TileLabel ??= string.Empty;
                LargeTileLabel ??= string.Empty;
                if (LabelSize == default) LabelSize = TextSize.Small;
                if (LabelWeight == default) LabelWeight = FontWeight.Medium;
                if (LabelColor == default) LabelColor = TextColor.Blue;
                if (LabelOutlineThickness == default) LabelOutlineThickness = 0.1f;

                // Initialize border features if not already set
                RiverBorders ??= new FeatureBorders(BorderType.River);
                BridgeBorders ??= new FeatureBorders(BorderType.Bridge);
                PontoonBridgeBorders ??= new FeatureBorders(BorderType.PontoonBridge);
                DamagedBridgeBorders ??= new FeatureBorders(BorderType.DestroyedBridge);

                // Initialize neighbors dictionary
                neighbors = new Dictionary<HexDirection, Hex>();

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

        #endregion

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
        /// Sets the fort status of the hex. Mutually exclusive with airbase.
        /// </summary>
        /// <param name="value">True to enable fort, false to disable</param>
        public void SetIsFort(bool value)
        {
            ValidateState();

            try
            {
                IsFort = value;
                if (IsFort && IsAirbase)
                {
                    IsAirbase = false;
                    AppService.CaptureUiMessage("Fort and Airbase are mutually exclusive. Airbase disabled.");
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
        /// Sets the airbase status of the hex. Mutually exclusive with fort.
        /// </summary>
        /// <param name="value">True to enable airbase, false to disable</param>
        public void SetIsAirbase(bool value)
        {
            ValidateState();

            try
            {
                IsAirbase = value;
                if (IsAirbase && IsFort)
                {
                    IsFort = false;
                    AppService.CaptureUiMessage("Fort and Airbase are mutually exclusive. Fort disabled.");
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
        public void SetNeighbor(HexDirection direction, Hex neighbor)
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
        public Hex GetNeighbor(HexDirection direction)
        {
            try
            {
                return neighbors.TryGetValue(direction, out Hex neighbor) ? neighbor : null;
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
        public IReadOnlyDictionary<HexDirection, Hex> GetAllNeighbors()
        {
            try
            {
                return neighbors;
            }
            catch (Exception ex)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetAllNeighbors), ex);
                return new Dictionary<HexDirection, Hex>();
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
                // Check mutually exclusive features
                if (IsFort && IsAirbase)
                {
                    AppService.CaptureUiMessage($"Hex at {Position}: Fort and Airbase cannot both be true");
                    return false;
                }

                // Check terrain consistency
                if (!Enum.IsDefined(typeof(TerrainType), Terrain))
                {
                    AppService.CaptureUiMessage($"Hex at {Position}: Invalid terrain type {Terrain}");
                    return false;
                }

                // Validate movement cost matches terrain
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

        #endregion

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
                MovementCost = (int)HexMovementCost.Plains; // Safe default
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
                TerrainType.Clear => (int)HexMovementCost.Plains,
                TerrainType.Forest => (int)HexMovementCost.Forest,
                TerrainType.Rough => (int)HexMovementCost.Rough,
                TerrainType.Marsh => (int)HexMovementCost.Marsh,
                TerrainType.Mountains => (int)HexMovementCost.Mountains,
                TerrainType.MinorCity => (int)HexMovementCost.MinorCity,
                TerrainType.MajorCity => (int)HexMovementCost.MajorCity,
                TerrainType.Impassable => (int)HexMovementCost.Impassable,
                _ => (int)HexMovementCost.Plains
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

        #endregion

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

        #endregion
    }
}