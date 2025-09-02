using System;
using System.Collections.Generic;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Legacy.Map
{
    /// <summary>
    /// Represents a single hex tile in the game map, containing terrain, features, borders,
    /// labels, and game state information with neighbor relationships.
    /// </summary>
    public class Hex : IDisposable
    {
        #region Constants
        private const string CLASS_NAME = nameof(Hex);
        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the hex is properly initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Indicates if the hex has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        // Game State Properties
        public TerrainType Terrain { get; private set; }
        public Vector2Int Position { get; private set; }
        public bool IsRail { get; private set; }
        public bool IsRoad { get; private set; }
        public bool IsFort { get; private set; }
        public bool IsAirbase { get; private set; }
        public bool IsObjective { get; private set; }
        public bool IsVisible { get; private set; }
        public string TileLabel { get; set; }
        public string LargeTileLabel { get; set; }
        public TextSize LabelSize { get; set; }
        public FontWeight LabelWeight { get; set; }
        public TextColor LabelColor { get; set; }
        public float LabelOutlineThickness { get; set; }
        public float VictoryValue { get; set; }
        public float AirbaseDamage { get; set; }
        public int UrbanDamage { get; set; }
        public TileControl TileControl { get; set; }
        public DefaultTileControl DefaultTileControl { get; set; }
        public FeatureBorders RiverBorders { get; set; }
        public FeatureBorders BridgeBorders { get; set; }
        public FeatureBorders PontoonBridgeBorders { get; set; }
        public FeatureBorders DamagedBridgeBorders { get; set; }
        public int MovementCost { get; private set; }

        #endregion

        #region Private Fields

        private readonly bool enableDebugLogging;
        private Dictionary<HexDirection, Hex> Neighbors;

        #endregion

        #region Constructor

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

        private void Initialize()
        {
            try
            {
                // Initialize all properties with default values
                Terrain = TerrainType.Clear;
                IsRail = false;
                IsRoad = false;
                IsFort = false;
                IsAirbase = false;
                IsObjective = false;
                IsVisible = false;
                TileLabel = string.Empty;
                LargeTileLabel = string.Empty;
                LabelSize = TextSize.Small;
                LabelWeight = FontWeight.Medium;
                LabelColor = TextColor.Blue;
                LabelOutlineThickness = 0.1f;
                VictoryValue = 0f;
                AirbaseDamage = 0f;
                UrbanDamage = 0;
                TileControl = TileControl.None;
                DefaultTileControl = DefaultTileControl.None;

                // Initialize border features
                RiverBorders = new FeatureBorders(BorderType.River);
                BridgeBorders = new FeatureBorders(BorderType.Bridge);
                PontoonBridgeBorders = new FeatureBorders(BorderType.PontoonBridge);
                DamagedBridgeBorders = new FeatureBorders(BorderType.DestroyedBridge);

                MovementCost = 0;
                Neighbors = new Dictionary<HexDirection, Hex>();

                IsInitialized = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Hex at position {Position} initialized successfully.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Initialize", e);
                IsInitialized = false;
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the position of the hex.
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(Vector2Int position)
        {
            ValidateState();

            try
            {
                Position = position;

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Position set at {position}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetPosition", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the fort status of the hex. Mutually exclusive with airbase.
        /// </summary>
        /// <param name="value">True to enable fort, false to disable</param>
        /// <exception cref="InvalidOperationException">Thrown when hex is not initialized or is disposed</exception>
        public void SetIsFort(bool value)
        {
            ValidateState();

            try
            {
                IsFort = value;
                if (IsFort)
                {
                    IsAirbase = false;
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Fort status at {Position} set to {value}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIsFort", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the airbase status of the hex. Mutually exclusive with fort.
        /// </summary>
        /// <param name="value">True to enable airbase, false to disable</param>
        /// <exception cref="InvalidOperationException">Thrown when hex is not initialized or is disposed</exception>
        public void SetIsAirbase(bool value)
        {
            ValidateState();

            try
            {
                IsAirbase = value;
                if (IsAirbase)
                {
                    IsFort = false;
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Airbase status at {Position} set to {value}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIsAirbase", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the rail status of the hex.
        /// </summary>
        /// <param name="value">True to enable rail, false to disable</param>
        /// <exception cref="InvalidOperationException">Thrown when hex is not initialized or is disposed</exception>
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
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIsRail", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the road status of the hex.
        /// </summary>
        /// <param name="value">True to enable road, false to disable</param>
        /// <exception cref="InvalidOperationException">Thrown when hex is not initialized or is disposed</exception>
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
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIsRoad", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the objective status of the hex.
        /// </summary>
        /// <param name="value"></param>
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
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIsObjective", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the visibility status of the hex to the red side.
        /// </summary>
        /// <param name="value"></param>
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
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetIsVisible", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the terrain type and updates the movement cost.
        /// </summary>
        /// <param name="type">The terrain type to set</param>
        /// <exception cref="InvalidOperationException">Thrown when hex is not initialized or is disposed</exception>
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
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetTerrain", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the label text for the hex.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="neighbor"></param>
        public void SetNeighbor(HexDirection direction, Hex neighbor)
        {
            ValidateState();

            try
            {
                if (neighbor == null)
                {
                    Neighbors.Remove(direction);
                }
                else
                {
                    Neighbors[direction] = neighbor;
                }

                if (enableDebugLogging)
                {
                    Debug.Log($"{CLASS_NAME}: Set neighbor at direction {direction} for hex at {Position}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetNeighbor", e);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a neighboring hex in a specified direction.
        /// </summary>
        /// <param name="direction">The direction of the neighboring hex to retrieve.</param>
        /// <returns>The neighboring hex in the specified direction, or null if no neighbor is present.</returns>
        public Hex GetNeighbor(HexDirection direction)
        {
            // Check if the Neighbors dictionary contains the key for the specified direction.
            if (Neighbors.ContainsKey(direction))
            {
                return Neighbors[direction];
            }
            else
            {
                // Return null if the neighbor does not exist in the specified direction.
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the movement cost based on current terrain type.
        /// </summary>
        private void UpdateMovementCost()
        {
            MovementCost = Terrain switch
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
                _ => throw new InvalidOperationException($"Invalid terrain type {Terrain}")
            };
        }

        /// <summary>
        /// Validates the current state of the hex.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when hex is not initialized or is disposed</exception>
        private void ValidateState()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{CLASS_NAME} is not initialized.");
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
                        Neighbors?.Clear();

                        // Clear border features
                        RiverBorders = null;
                        BridgeBorders = null;
                        PontoonBridgeBorders = null;
                        DamagedBridgeBorders = null;

                        if (enableDebugLogging)
                        {
                            Debug.Log($"{CLASS_NAME}: Hex at position {Position} disposed.");
                        }
                    }
                    catch (Exception e)
                    {
                        AppService.HandleException(CLASS_NAME, "Dispose", e);
                    }
                }

                IsDisposed = true;
            }
        }

        #endregion
    }
}