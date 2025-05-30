using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// This class represents a supply depot that provides supplies to combat units.
    /// It handles stockpile management, supply projection, generation, and special abilities.
    /// </summary>
    [Serializable]
    public class SupplyDepotSubProfile : LandBaseProfile, ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(SupplyDepotSubProfile);

        #endregion // Constants


        #region Fields

        private bool hasAirSupply = false;          // Whether the depot can provide air supply
        private bool hasNavalSupply = false;        // Whether the depot can provide naval supply

        #endregion // Fields


        #region Properties

        public string DepotID { get; private set; }                      // Unique identifier for the depot
        public string DepotName { get; private set; }                    // Name of the depot
        public Side Side { get; private set; }                           // Side the depot belongs to
        public DepotSize DepotSize { get; private set; }                 // Size of the depot
        public float StockpileInDays { get; private set; }               // Current stockpile in days of supply
        public SupplyGenerationRate GenerationRate { get; private set; } // Generation rate of the depot
        public SupplyProjection SupplyProjection { get; private set; }   // Supply projection level
        public bool SupplyPenetration { get; private set; }              // Whether the depot has supply penetration capability
        public DepotCategory DepotCategory { get; private set; }         // Category of the depot (Main or Secondary)
        public bool IsMainDepot => DepotCategory == DepotCategory.Main;

        // Properties with rules.
        public bool HasAirSupply { get => hasAirSupply && IsMainDepot; set => hasAirSupply = IsMainDepot && value; }
        public bool HasNavalSupply { get => hasNavalSupply && IsMainDepot; set => hasNavalSupply = IsMainDepot && value; }
        public int ProjectionRadius => CUConstants.ProjectionRangeValues[SupplyProjection];

        #endregion // Properties


        #region Constructors

        public SupplyDepotSubProfile() : base()
        {
            try
            {
                DepotID = Guid.NewGuid().ToString();
                DepotName = "Supply Depot";
                Side = Side.Player;
                DepotSize = DepotSize.Small;
                StockpileInDays = GetMaxStockpile() / 2; // Start half full
                GenerationRate = SupplyGenerationRate.Minimal;
                SupplyProjection = SupplyProjection.Local;
                SupplyPenetration = false;
                DepotCategory = DepotCategory.Secondary;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "DefaultConstructor", e);
                throw;
            }
        }

        public SupplyDepotSubProfile(string name, Side side, DepotSize depotSize, bool isMainDepot = false, int initialDamage = 0)
            : base(initialDamage)
        {
            try
            {
                DepotID = Guid.NewGuid().ToString();
                DepotName = string.IsNullOrEmpty(name) ? "Supply Depot" : name;
                Side = side;
                DepotSize = depotSize;
                DepotCategory = isMainDepot ? DepotCategory.Main : DepotCategory.Secondary;

                // Initialize with default values
                StockpileInDays = GetMaxStockpile() / 2; // Start half full
                GenerationRate = SupplyGenerationRate.Standard;
                SupplyProjection = SupplyProjection.Extended;
                SupplyPenetration = false; // FIXED: Don't assign to itself
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected SupplyDepotSubProfile(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            try
            {
                // Basic depot information
                DepotID = info.GetString(nameof(DepotID));
                DepotName = info.GetString(nameof(DepotName));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));

                // Depot attributes
                DepotSize = (DepotSize)info.GetValue(nameof(DepotSize), typeof(DepotSize));
                StockpileInDays = info.GetSingle(nameof(StockpileInDays));
                GenerationRate = (SupplyGenerationRate)info.GetValue(nameof(GenerationRate), typeof(SupplyGenerationRate));
                SupplyProjection = (SupplyProjection)info.GetValue(nameof(SupplyProjection), typeof(SupplyProjection));
                SupplyPenetration = info.GetBoolean(nameof(SupplyPenetration));
                DepotCategory = (DepotCategory)info.GetValue(nameof(DepotCategory), typeof(DepotCategory));

                // Special abilities - Use backing fields
                hasAirSupply = info.GetBoolean("HasAirSupply");
                hasNavalSupply = info.GetBoolean("HasNavalSupply");
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors

        #region Supply Management Methods

        private float GetMaxStockpile()
        {
            return CUConstants.MaxStockpileBySize[DepotSize];
        }

        private float GetCurrentGenerationRate()
        {
            float baseRate = CUConstants.GenerationRateValues[GenerationRate];
            float efficiencyMultiplier = GetEfficiencyMultiplier();

            return baseRate * efficiencyMultiplier;
        }

        public void AddSupplies(float amount)
        {
            try
            {
                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                float maxCapacity = GetMaxStockpile();
                float newAmount = StockpileInDays + amount;

                // Clamp to maximum capacity
                StockpileInDays = Math.Min(newAmount, maxCapacity);
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "AddSupplies", e);
                throw;
            }
        }

        public float RemoveSupplies(float amount)
        {
            try
            {
                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                float actualAmount = Math.Min(amount, StockpileInDays);
                StockpileInDays -= actualAmount;

                return actualAmount;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "RemoveSupplies", e);
                return 0f;
            }
        }

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
                float maxCapacity = GetMaxStockpile();

                // Don't exceed maximum capacity
                float amountToAdd = Math.Min(generatedAmount, maxCapacity - StockpileInDays);
                StockpileInDays += amountToAdd;

                return amountToAdd;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "GenerateSupplies", e);
                return 0f;
            }
        }

        public bool CanSupplyUnitAt(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                // Check if the depot is operational
                if (!IsOperational())
                {
                    return false;
                }

                // Check if unit is within projection radius
                if (distanceInHexes > ProjectionRadius)
                {
                    return false;
                }

                // Check if supply penetration is sufficient
                int zocPenetration = SupplyPenetration ? 1 : 0;
                if (enemyZOCsCrossed > zocPenetration)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "CanSupplyUnitAt", e);
                return false;
            }
        }

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

                // Check if we have supplies to give
                if (StockpileInDays <= 0)
                {
                    return 0f;
                }

                // Calculate efficiency based on distance, enemy ZOCs, and operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)ProjectionRadius * 0.5f);
                float zocEfficiency = 1f - (enemyZOCsCrossed * 0.65f);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * zocEfficiency * operationalEfficiency;

                // Ensure efficiency doesn't go below a minimum threshold
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                // Calculate amount to deliver
                float maxDeliverable = Math.Min(requestedAmount, CUConstants.MaxDaysSupplyUnit);
                float amountToDeliver = maxDeliverable * totalEfficiency;

                // Calculate actual supply cost (accounting for losses due to efficiency)
                float supplyCost = amountToDeliver / totalEfficiency;
                float actualSupplyCost = Math.Min(supplyCost, StockpileInDays);

                // Remove supplies from stockpile
                StockpileInDays -= actualSupplyCost;

                // Return the actual amount delivered (proportional to what we could afford)
                return amountToDeliver * (actualSupplyCost / supplyCost);
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "SupplyUnit", e);
                return 0f;
            }
        }

        public float GetStockpilePercentage()
        {
            try
            {
                float maxCapacity = GetMaxStockpile();
                return maxCapacity > 0 ? StockpileInDays / maxCapacity : 0f;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "GetStockpilePercentage", e);
                return 0f;
            }
        }

        public bool IsStockpileFull()
        {
            return Math.Abs(StockpileInDays - GetMaxStockpile()) < 0.001f;
        }

        public bool IsStockpileEmpty()
        {
            return StockpileInDays <= 0f;
        }

        public float GetRemainingCapacity()
        {
            return GetMaxStockpile() - StockpileInDays;
        }

        #endregion // Supply Management Methods


        #region Upgrade Methods

        public bool UpgradeDepotSize()
        {
            try
            {
                DepotSize originalSize = DepotSize;
                switch (DepotSize)
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
                AppService.Instance?.HandleException(CLASS_NAME, "UpgradeDepotSize", e);
                return false;
            }
        }

        public bool UpgradeGenerationRate()
        {
            try
            {
                switch (GenerationRate)
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
                AppService.Instance?.HandleException(CLASS_NAME, "UpgradeGenerationRate", e);
                return false;
            }
        }

        public bool UpgradeSupplyProjection()
        {
            try
            {
                switch (SupplyProjection)
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
                AppService.Instance?.HandleException(CLASS_NAME, "UpgradeSupplyProjection", e);
                return false;
            }
        }

        public bool UpgradeSupplyPenetration()
        {
            try
            {
                if (!SupplyPenetration)
                {
                    SupplyPenetration = true;
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "UpgradeSupplyPenetration", e);
                return false;
            }
        }

        public bool CanUpgradeDepotSize()
        {
            return DepotSize != DepotSize.Huge;
        }

        public bool CanUpgradeGenerationRate()
        {
            return GenerationRate != SupplyGenerationRate.Industrial;
        }

        public bool CanUpgradeSupplyProjection()
        {
            return SupplyProjection != SupplyProjection.Theater;
        }

        public bool CanUpgradeSupplyPenetration()
        {
            return !SupplyPenetration;
        }

        #endregion // Upgrade Methods


        #region Special Ability Methods

        public bool EnableAirSupply()
        {
            try
            {
                if (!IsMainDepot)
                    return false;

                HasAirSupply = true;
                return true;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "EnableAirSupply", e);
                return false;
            }
        }

        public bool EnableNavalSupply()
        {
            try
            {
                if (!IsMainDepot)
                    return false;

                HasNavalSupply = true;
                return true;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "EnableNavalSupply", e);
                return false;
            }
        }

        public float PerformAirSupply(int distanceInHexes, float amountRequested)
        {
            try
            {
                // Check if the depot is operational and has air supply capability
                if (!IsOperational() || !HasAirSupply)
                {
                    return 0f;
                }

                if (distanceInHexes > CUConstants.AirSupplyMaxRange)
                {
                    return 0f;
                }

                if (amountRequested <= 0 || StockpileInDays <= 0)
                {
                    return 0f;
                }

                // Air supply efficiency decreases with distance and is affected by operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)CUConstants.AirSupplyMaxRange * 0.7f);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * operationalEfficiency;

                // Ensure minimum efficiency
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                float maxDeliverable = Math.Min(amountRequested, CUConstants.MaxDaysSupplyUnit);
                float actualAmount = maxDeliverable * totalEfficiency;

                // Calculate supply cost (account for supplies lost in transit)
                float supplyCost = actualAmount / totalEfficiency;
                float actualSupplyCost = Math.Min(supplyCost, StockpileInDays);

                // Remove the supplies from stockpile
                StockpileInDays -= actualSupplyCost;

                return actualAmount * (actualSupplyCost / supplyCost);
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "PerformAirSupply", e);
                return 0f;
            }
        }

        public float PerformNavalSupply(int distanceInHexes, float amountRequested)
        {
            try
            {
                // Check if the depot is operational and has naval supply capability
                if (!IsOperational() || !HasNavalSupply)
                {
                    return 0f;
                }

                if (distanceInHexes > CUConstants.NavalSupplyMaxRange)
                {
                    return 0f;
                }

                if (amountRequested <= 0 || StockpileInDays <= 0)
                {
                    return 0f;
                }

                // Naval supply is more efficient than air but still affected by distance and operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)CUConstants.NavalSupplyMaxRange * 0.4f);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * operationalEfficiency;

                // Ensure minimum efficiency
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                // Can deliver up to 50% of on-hand supplies
                float maxDeliverable = Math.Min(amountRequested, StockpileInDays * 0.5f);
                float actualAmount = maxDeliverable * totalEfficiency;

                // Calculate supply cost
                float supplyCost = actualAmount / totalEfficiency;
                float actualSupplyCost = Math.Min(supplyCost, StockpileInDays);

                // Remove the supplies from stockpile
                StockpileInDays -= actualSupplyCost;

                return actualAmount * (actualSupplyCost / supplyCost);
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "PerformNavalSupply", e);
                return 0f;
            }
        }

        #endregion // Special Ability Methods


        #region Game Cycle Methods

        public void OnNewTurn()
        {
            try
            {
                // Generate new supplies
                GenerateSupplies();
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "OnNewTurn", e);
            }
        }

        public SupplyDepotSubProfile Clone()
        {
            try
            {
                var clone = new SupplyDepotSubProfile();

                // Copy basic information (except ID which is generated new)
                clone.DepotName = this.DepotName;
                clone.Side = this.Side;

                // Copy depot attributes
                clone.DepotSize = this.DepotSize;
                clone.StockpileInDays = this.StockpileInDays;
                clone.GenerationRate = this.GenerationRate;
                clone.SupplyProjection = this.SupplyProjection;
                clone.SupplyPenetration = this.SupplyPenetration;
                clone.DepotCategory = this.DepotCategory;

                // Copy special abilities
                clone.HasAirSupply = this.HasAirSupply;
                clone.HasNavalSupply = this.HasNavalSupply;

                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion // Game Cycle Methods


        #region ISerializable Implementation

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Call base class serialization first
                base.GetObjectData(info, context);

                // Basic depot information
                info.AddValue(nameof(DepotID), DepotID);
                info.AddValue(nameof(DepotName), DepotName);
                info.AddValue(nameof(Side), Side);

                // Depot attributes
                info.AddValue(nameof(DepotSize), DepotSize);
                info.AddValue(nameof(StockpileInDays), StockpileInDays);
                info.AddValue(nameof(GenerationRate), GenerationRate);
                info.AddValue(nameof(SupplyProjection), SupplyProjection);
                info.AddValue(nameof(SupplyPenetration), SupplyPenetration);
                info.AddValue(nameof(DepotCategory), DepotCategory);

                // Special abilities - Use backing fields
                info.AddValue("HasAirSupply", hasAirSupply);
                info.AddValue("HasNavalSupply", hasNavalSupply);
            }
            catch (Exception e)
            {
                AppService.Instance?.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation
    }
}