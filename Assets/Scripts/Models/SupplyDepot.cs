using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Defines the size of a supply depot and its maximum storage capacity.
    /// </summary>
    public enum DepotSize
    {
        Small,   // 30 days of supply
        Medium,  // 50 days of supply
        Large,   // 80 days of supply
        Huge     // 110 days of supply
    }

    /// <summary>
    /// Defines the supply generation rate of a depot.
    /// </summary>
    public enum SupplyGenerationRate
    {
        Minimal,        // 0.5 days of supply per turn
        Basic,          // 1.0 days of supply per turn
        Standard,       // 1.5 days of supply per turn
        Enhanced,       // 2.5 days of supply per turn
        Industrial      // 4.0 days of supply per turn
    }

    /// <summary>
    /// Defines how far a depot can project supplies effectively.
    /// </summary>
    public enum SupplyProjection
    {
        Local,          // 2 hex radius
        Extended,       // 4 hex radius
        Regional,       // 6 hex radius
        Strategic,      // 9 hex radius
        Theater         // 12 hex radius
    }

    /// <summary>
    /// Defines the category of a depot.
    /// </summary>
    public enum DepotCategory
    {
        Main,       // Primary depot with special abilities
        Secondary   // Standard field depot
    }

    /// <summary>
    /// This class represents a supply depot that provides supplies to combat units.
    /// It handles stockpile management, supply projection, generation, and special abilities.
    /// </summary>
    [Serializable]
    public class SupplyDepot : LandBase, ISerializable
    {
        #region Constants
        private const string CLASS_NAME = nameof(SupplyDepot);

        // Maximum stockpile capacities by depot size
        private static readonly Dictionary<DepotSize, float> MaxStockpileBySize = new Dictionary<DepotSize, float>
        {
            { DepotSize.Small, 30f },
            { DepotSize.Medium, 50f },
            { DepotSize.Large, 80f },
            { DepotSize.Huge, 110f }
        };

        // Supply generation rates by level
        private static readonly Dictionary<SupplyGenerationRate, float> GenerationRateValues = new Dictionary<SupplyGenerationRate, float>
        {
            { SupplyGenerationRate.Minimal, 1.0f },
            { SupplyGenerationRate.Basic, 2.0f },
            { SupplyGenerationRate.Standard, 3.0f },
            { SupplyGenerationRate.Enhanced, 4.0f },
            { SupplyGenerationRate.Industrial, 5.0f }
        };

        // Supply projection ranges in hexes
        private static readonly Dictionary<SupplyProjection, int> ProjectionRangeValues = new Dictionary<SupplyProjection, int>
        {
            { SupplyProjection.Local, 2 },
            { SupplyProjection.Extended, 4 },
            { SupplyProjection.Regional, 6 },
            { SupplyProjection.Strategic, 9 },
            { SupplyProjection.Theater, 12 }
        };

        // Amount any unit can stockpile
        private const int MaxUnitStockpileAmount = 7;

        // Constants for special abilities
        private const int AirSupplyMaxRange = 16;
        private const int NavalSupplyMaxRange = 12;
        private const int IntelligenceNetworkRadius = 5;
        #endregion

        #region Fields
        // Basic depot information
        private string depotID;
        private string depotName;
        private Side side;

        // Depot attributes
        private DepotSize depotSize = DepotSize.Small;
        private float stockpileInDays = 20f;
        private SupplyGenerationRate generationRate = SupplyGenerationRate.Minimal;
        private SupplyProjection supplyProjection = SupplyProjection.Local;
        private bool supplyPenetration = false;
        private DepotCategory depotCategory = DepotCategory.Secondary;

        // Special abilities
        private bool hasAirSupply = false;
        private bool hasNavalSupply = false;
        private bool hasIntelligenceCapability = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the unique identifier for this depot.
        /// </summary>
        public string DepotID => depotID;

        /// <summary>
        /// Gets or sets the name of this depot.
        /// </summary>
        public string DepotName
        {
            get => depotName;
            set => depotName = value;
        }

        /// <summary>
        /// Gets or sets which side this depot belongs to.
        /// </summary>
        public Side Side
        {
            get => side;
            set => side = value;
        }

        /// <summary>
        /// Gets or sets the depot size, which determines maximum stockpile capacity.
        /// </summary>
        public DepotSize DepotSize
        {
            get => depotSize;
            set
            {
                depotSize = value;

                // Ensure current stockpile doesn't exceed the new maximum
                stockpileInDays = Math.Min(stockpileInDays, GetMaxStockpile());
            }
        }

        /// <summary>
        /// Gets or sets the current stockpile in days. 
        /// Value is clamped to maximum capacity based on depot size.
        /// </summary>
        public float StockpileInDays
        {
            get => stockpileInDays;
            set => stockpileInDays = Math.Clamp(value, 0, GetMaxStockpile());
        }

        /// <summary>
        /// Gets the maximum possible stockpile based on current depot size.
        /// </summary>
        public float MaxStockpile => GetMaxStockpile();

        /// <summary>
        /// Gets or sets the rate at which this depot generates supplies each turn.
        /// </summary>
        public SupplyGenerationRate GenerationRate
        {
            get => generationRate;
            set => generationRate = value;
        }

        /// <summary>
        /// Gets or sets how far this depot can project supplies effectively.
        /// </summary>
        public SupplyProjection SupplyProjection
        {
            get => supplyProjection;
            set => supplyProjection = value;
        }

        /// <summary>
        /// Gets the actual projection radius in hexes based on the current level.
        /// </summary>
        public int ProjectionRadius => ProjectionRangeValues[supplyProjection];

        /// <summary>
        /// Gets or sets how effectively supplies from this depot can penetrate enemy territory.
        /// </summary>
        public bool SupplyPenetration
        {
            get => supplyPenetration;
            set => supplyPenetration = value;
        }

        /// <summary>
        /// Gets or sets the category of this depot.
        /// </summary>
        public DepotCategory DepotCategory
        {
            get => depotCategory;
            set
            {
                // If downgrading from Main to Secondary, clear special abilities
                if (depotCategory == DepotCategory.Main && value == DepotCategory.Secondary)
                {
                    hasAirSupply = false;
                    hasNavalSupply = false;
                    hasIntelligenceCapability = false;
                }
                depotCategory = value;
            }
        }

        /// <summary>
        /// Gets whether this depot is a Main Depot.
        /// </summary>
        public bool IsMainDepot => depotCategory == DepotCategory.Main;

        /// <summary>
        /// Gets or sets whether this depot has air supply capability.
        /// </summary>
        public bool HasAirSupply
        {
            get => hasAirSupply && IsMainDepot;
            set => hasAirSupply = IsMainDepot && value;
        }

        /// <summary>
        /// Gets or sets whether this depot has naval supply capability.
        /// </summary>
        public bool HasNavalSupply
        {
            get => hasNavalSupply && IsMainDepot;
            set => hasNavalSupply = IsMainDepot && value;
        }

        /// <summary>
        /// Gets or sets whether this depot has intelligence capability.
        /// </summary>
        public bool HasIntelligenceCapability
        {
            get => hasIntelligenceCapability && IsMainDepot;
            set => hasIntelligenceCapability = IsMainDepot && value;
        }

        /// <summary>
        /// Gets the intelligence detection radius in hexes.
        /// </summary>
        public int IntelligenceRadius => HasIntelligenceCapability ? IntelligenceNetworkRadius : 0;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new supply depot with default values.
        /// </summary>
        public SupplyDepot() : base()
        {
            depotID = Guid.NewGuid().ToString();
            depotName = "Supply Depot";
            side = Side.Player;
            depotSize = DepotSize.Small;
            stockpileInDays = GetMaxStockpile() / 2; // Start half full
            generationRate = SupplyGenerationRate.Minimal;
            supplyProjection = SupplyProjection.Local;
            supplyPenetration = SupplyPenetration = false;
            depotCategory = DepotCategory.Secondary;
        }

        /// <summary>
        /// Creates a new supply depot with the specified parameters.
        /// </summary>
        /// <param name="name">The name of the depot</param>
        /// <param name="side">Which side the depot belongs to</param>
        /// <param name="depotSize">Size of the depot</param>
        /// <param name="isMainDepot">Whether this is a main depot</param>
        /// <param name="initialDamage">Initial damage level (0-100)</param>
        public SupplyDepot(string name, Side side, DepotSize depotSize, bool isMainDepot = false, int initialDamage = 0)
            : base(initialDamage)
        {
            try
            {
                this.depotID = Guid.NewGuid().ToString();
                this.depotName = string.IsNullOrEmpty(name) ? "Supply Depot" : name;
                this.side = side;
                this.depotSize = depotSize;
                this.depotCategory = isMainDepot ? DepotCategory.Main : DepotCategory.Secondary;

                // Initialize with default values
                this.stockpileInDays = GetMaxStockpile() / 2; // Start half full
                this.generationRate = SupplyGenerationRate.Standard;
                this.supplyProjection = SupplyProjection.Extended;
                this.supplyPenetration = SupplyPenetration;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected SupplyDepot(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            try
            {
                // Basic depot information
                depotID = info.GetString(nameof(depotID));
                depotName = info.GetString(nameof(depotName));
                side = (Side)info.GetValue(nameof(side), typeof(Side));

                // Depot attributes
                depotSize = (DepotSize)info.GetValue(nameof(depotSize), typeof(DepotSize));
                stockpileInDays = info.GetSingle(nameof(stockpileInDays));
                generationRate = (SupplyGenerationRate)info.GetValue(nameof(generationRate), typeof(SupplyGenerationRate));
                supplyProjection = (SupplyProjection)info.GetValue(nameof(supplyProjection), typeof(SupplyProjection));
                supplyPenetration = (bool)info.GetValue(nameof(supplyPenetration), typeof(bool));
                depotCategory = (DepotCategory)info.GetValue(nameof(depotCategory), typeof(DepotCategory));

                // Special abilities
                hasAirSupply = info.GetBoolean(nameof(hasAirSupply));
                hasNavalSupply = info.GetBoolean(nameof(hasNavalSupply));
                hasIntelligenceCapability = info.GetBoolean(nameof(hasIntelligenceCapability));
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }
        #endregion

        #region Supply Management Methods
        /// <summary>
        /// Gets the maximum stockpile capacity based on current depot size.
        /// </summary>
        /// <returns>Maximum stockpile in days</returns>
        private float GetMaxStockpile()
        {
            return MaxStockpileBySize[depotSize];
        }

        /// <summary>
        /// Gets the current supply generation rate in days of supply per turn.
        /// </summary>
        /// <returns></returns>
        private float GetCurrentGenerationRate()
        {
            float baseRate = GenerationRateValues[generationRate];
            float efficiencyMultiplier = GetEfficiencyMultiplier();

            return baseRate * efficiencyMultiplier;
        }

        /// <summary>
        /// Adds supplies to the depot.
        /// </summary>
        /// <param name="amount">Amount of days of supply to add</param>
        public void AddSupplies(float amount)
        {
            try
            {
                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                StockpileInDays += amount;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AddSupplies", e);
                throw;
            }
        }

        /// <summary>
        /// Removes supplies from the depot.
        /// </summary>
        /// <param name="amount">Amount of days of supply to remove</param>
        /// <returns>Actual amount removed (may be less if stockpile is insufficient)</returns>
        public float RemoveSupplies(float amount)
        {
            try
            {
                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                float actualAmount = Math.Min(amount, stockpileInDays);
                StockpileInDays -= actualAmount;

                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RemoveSupplies", e);
                throw;
            }
        }

        /// <summary>
        /// Processes supply generation for a turn.
        /// </summary>
        /// <returns>Amount of supplies generated</returns>
        public float GenerateSupplies()
        {
            try
            {
                // If depot is completely out of operation, no supplies are generated
                if (!IsOperational())
                {
                    return 0f;
                }

                float generatedAmount = GetCurrentGenerationRate();

                // Don't exceed maximum capacity
                float amountToAdd = Math.Min(generatedAmount, MaxStockpile - StockpileInDays);
                StockpileInDays += amountToAdd;

                return amountToAdd;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GenerateSupplies", e);
                throw;
            }
        }

        /// <summary>
        /// Determines if a unit at the specified distance can be supplied by this depot.
        /// </summary>
        /// <param name="distanceInHexes">Distance to unit in hexes</param>
        /// <param name="enemyZOCsCrossed">Number of enemy ZOCs that must be crossed</param>
        /// <returns>True if the unit can be supplied, false otherwise</returns>
        public bool CanSupplyUnitAt(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                // Check if the depot has SupplyPentration.
                int zocPenetration = 0;
                if (SupplyPenetration) zocPenetration = 1;

                // Check if unit is within projection radius
                if (distanceInHexes > ProjectionRadius)
                {
                    return false;
                }

                // Check if supply penetration is sufficient
                if (enemyZOCsCrossed > zocPenetration)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CanSupplyUnitAt", e);
                return false;
            }
        }

        /// <summary>
        /// Supplies a unit with the requested amount of supplies.
        /// </summary>
        /// <param name="requestedAmount">Amount of supplies requested</param>
        /// <param name="distanceInHexes">Distance to unit in hexes</param>
        /// <param name="enemyZOCsCrossed">Number of enemy ZOCs that must be crossed</param>
        /// <returns>Actual amount supplied</returns>
        public float SupplyUnit(float requestedAmount, int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                if (requestedAmount <= 0)
                {
                    return 0f;
                }

                // Check if we can supply the unit
                if (!CanSupplyUnitAt(distanceInHexes, enemyZOCsCrossed))
                {
                    return 0f;
                }

                // Check if the depot is operational at all
                if (!IsOperational())
                {
                    return 0f;
                }

                // Calculate efficiency based on distance, enemy ZOCs, and operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)ProjectionRadius * 0.5f);
                float zocEfficiency = 1 - (enemyZOCsCrossed * 0.65f);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float efficiency = distanceEfficiency * zocEfficiency * operationalEfficiency;

                // Calculate amount to deliver
                float maxDeliverable = Math.Min(requestedAmount, MaxUnitStockpileAmount);
                float amountToDeliver = maxDeliverable * efficiency;

                // Remove supplies from stockpile (accounting for losses due to efficiency)
                RemoveSupplies(amountToDeliver / efficiency);

                return amountToDeliver;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SupplyUnit", e);
                return 0f;
            }
        }
        #endregion

        #region Upgrade Methods
        /// <summary>
        /// Upgrades the depot size to the next level.
        /// </summary>
        /// <returns>True if upgrade was successful, false if already at maximum</returns>
        public bool UpgradeDepotSize()
        {
            try
            {
                switch (depotSize)
                {
                    case DepotSize.Small:
                        DepotSize = DepotSize.Medium;
                        return true;
                    case DepotSize.Medium:
                        DepotSize = DepotSize.Large;
                        return true;
                    case DepotSize.Large:
                        DepotSize = DepotSize.Huge;
                        return true;
                    default:
                        return false; // Already at maximum
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpgradeDepotSize", e);
                return false;
            }
        }

        /// <summary>
        /// Upgrades the depot's generation rate to the next level.
        /// </summary>
        /// <returns>True if upgrade was successful, false if already at maximum</returns>
        public bool UpgradeGenerationRate()
        {
            try
            {
                switch (generationRate)
                {
                    case SupplyGenerationRate.Minimal:
                        GenerationRate = SupplyGenerationRate.Basic;
                        return true;
                    case SupplyGenerationRate.Basic:
                        GenerationRate = SupplyGenerationRate.Standard;
                        return true;
                    case SupplyGenerationRate.Standard:
                        GenerationRate = SupplyGenerationRate.Enhanced;
                        return true;
                    case SupplyGenerationRate.Enhanced:
                        GenerationRate = SupplyGenerationRate.Industrial;
                        return true;
                    default:
                        return false; // Already at maximum
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpgradeGenerationRate", e);
                return false;
            }
        }

        /// <summary>
        /// Upgrades the depot's supply projection to the next level.
        /// </summary>
        /// <returns>True if upgrade was successful, false if already at maximum</returns>
        public bool UpgradeSupplyProjection()
        {
            try
            {
                switch (supplyProjection)
                {
                    case SupplyProjection.Local:
                        SupplyProjection = SupplyProjection.Extended;
                        return true;
                    case SupplyProjection.Extended:
                        SupplyProjection = SupplyProjection.Regional;
                        return true;
                    case SupplyProjection.Regional:
                        SupplyProjection = SupplyProjection.Strategic;
                        return true;
                    case SupplyProjection.Strategic:
                        SupplyProjection = SupplyProjection.Theater;
                        return true;
                    default:
                        return false; // Already at maximum
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpgradeSupplyProjection", e);
                return false;
            }
        }

        /// <summary>
        /// Upgrades the depot's supply penetration.
        /// </summary>
        /// <returns>True if upgrade was successful, false if already at maximum</returns>
        public bool UpgradeSupplyPenetration()
        {
            try
            {
                if (!SupplyPenetration)
                {
                    SupplyPenetration = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "UpgradeSupplyPenetration", e);
                return false;
            }
        }
        #endregion

        #region Special Ability Methods
        /// <summary>
        /// Enables the Air Supply special ability if this is a Main Depot.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool EnableAirSupply()
        {
            if (!IsMainDepot)
                return false;

            hasAirSupply = true;
            return true;
        }

        /// <summary>
        /// Enables the Naval Supply special ability if this is a Main Depot.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool EnableNavalSupply()
        {
            if (!IsMainDepot)
                return false;

            hasNavalSupply = true;
            return true;
        }

        /// <summary>
        /// Enables the Intelligence Capability special ability if this is a Main Depot.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool EnableIntelligenceCapability()
        {
            if (!IsMainDepot)
                return false;

            hasIntelligenceCapability = true;
            return true;
        }

        /// <summary>
        /// Performs an air supply operation to deliver supplies to an isolated unit.
        /// </summary>
        /// <param name="distanceInHexes">Distance to the unit</param>
        /// <param name="amountRequested">Amount of supplies requested</param>
        /// <returns>Amount of supplies actually delivered</returns>
        public float PerformAirSupply(int distanceInHexes, float amountRequested)
        {
            try
            {
                // First check if the depot is operational and has air supply capability
                if (!IsOperational() || !HasAirSupply)
                {
                    return 0f;
                }

                if (distanceInHexes > AirSupplyMaxRange)
                {
                    return 0f;
                }

                // Air supply efficiency decreases with distance and is affected by operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)AirSupplyMaxRange * 0.7f);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float efficiencyFactor = distanceEfficiency * operationalEfficiency;

                float maxDeliverable = Math.Min(amountRequested, MaxUnitStockpileAmount);
                float actualAmount = maxDeliverable * efficiencyFactor;

                // Remove the supplies from stockpile
                RemoveSupplies(actualAmount / efficiencyFactor); // Account for supplies lost in transit

                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "PerformAirSupply", e);
                return 0f;
            }
        }

        /// <summary>
        /// Performs a naval supply operation to deliver supplies to a coastal unit.
        /// </summary>
        /// <param name="distance/// <summary>
        /// Performs a naval supply operation to deliver supplies to a coastal unit.
        /// </summary>
        /// <param name="distanceInHexes">Distance along coast to the unit</param>
        /// <param name="amountRequested">Amount of supplies requested</param>
        /// <returns>Amount of supplies actually delivered</returns>
        public float PerformNavalSupply(int distanceInHexes, float amountRequested)
        {
            try
            {
                // First check if the depot is operational and has naval supply capability
                if (!IsOperational() || !HasNavalSupply)
                {
                    return 0f;
                }

                if (distanceInHexes > NavalSupplyMaxRange)
                {
                    return 0f;
                }

                // Naval supply is more efficient than air but still affected by distance and operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)NavalSupplyMaxRange * 0.4f);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float efficiencyFactor = distanceEfficiency * operationalEfficiency;

                float maxDeliverable = Math.Min(amountRequested, stockpileInDays * 0.5f); // Can deliver 50% of on-hand supplies
                float actualAmount = maxDeliverable * efficiencyFactor;

                // Remove the supplies from stockpile
                RemoveSupplies(actualAmount / efficiencyFactor); // Account for supplies lost in transit

                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "PerformNavalSupply", e);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the intelligence detection radius in hexes.
        /// </summary>
        /// <returns>Current detection radius based on intelligence capability</returns>
        public int GetIntelligenceRadius()
        {
            if (!HasIntelligenceCapability)
            {
                return 0;
            }

            return IntelligenceNetworkRadius;
        }
        #endregion

        #region Game Cycle Methods
        /// <summary>
        /// Updates the depot state for a new turn.
        /// </summary>
        public void OnNewTurn()
        {
            try
            {
                // Generate new supplies
                GenerateSupplies();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "OnNewTurn", e);
            }
        }

        /// <summary>
        /// Creates a clone of this depot.
        /// </summary>
        /// <returns>A new SupplyDepot with the same properties</returns>
        public SupplyDepot Clone()
        {
            try
            {
                var clone = new SupplyDepot();

                // Copy basic information (except ID which is generated new)
                clone.depotName = this.depotName;
                clone.side = this.side;

                // Copy depot attributes
                clone.depotSize = this.depotSize;
                clone.stockpileInDays = this.stockpileInDays;
                clone.generationRate = this.generationRate;
                clone.supplyProjection = this.supplyProjection;
                clone.supplyPenetration = this.supplyPenetration;
                clone.depotCategory = this.depotCategory;

                // Copy special abilities
                clone.hasAirSupply = this.hasAirSupply;
                clone.hasNavalSupply = this.hasNavalSupply;
                clone.hasIntelligenceCapability = this.hasIntelligenceCapability;

                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }
        #endregion

        #region ISerializable Implementation
        /// <summary>
        /// Serializes this SupplyDepot instance.
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Call base class serialization first
                base.GetObjectData(info, context);

                // Basic depot information
                info.AddValue(nameof(depotID), depotID);
                info.AddValue(nameof(depotName), depotName);
                info.AddValue(nameof(side), side);

                // Depot attributes
                info.AddValue(nameof(depotSize), depotSize);
                info.AddValue(nameof(stockpileInDays), stockpileInDays);
                info.AddValue(nameof(generationRate), generationRate);
                info.AddValue(nameof(supplyProjection), supplyProjection);
                info.AddValue(nameof(supplyPenetration), supplyPenetration);
                info.AddValue(nameof(depotCategory), depotCategory);

                // Special abilities
                info.AddValue(nameof(hasAirSupply), hasAirSupply);
                info.AddValue(nameof(hasNavalSupply), hasNavalSupply);
                info.AddValue(nameof(hasIntelligenceCapability), hasIntelligenceCapability);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }
        #endregion
    }
}