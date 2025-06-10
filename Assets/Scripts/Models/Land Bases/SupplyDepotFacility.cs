using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents a supply depot that provides supplies to combat units.
    /// Inherits base facility functionality from LandBaseFacility including damage tracking and operational status.
    /// Handles stockpile management, supply projection, generation, special abilities, and upgrade systems.
    /// Supports both main and secondary depot categories with different capability restrictions.
    /// </summary>
    [Serializable]
    public class SupplyDepotFacility : LandBaseFacility, ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(SupplyDepotFacility);

        #endregion // Constants


        #region Fields

        private bool hasAirSupply = false;          // Whether the depot can provide air supply
        private bool hasNavalSupply = false;        // Whether the depot can provide naval supply

        #endregion // Fields


        #region Properties

        public DepotSize DepotSize { get; private set; }                 // Size of the depot
        public float StockpileInDays { get; private set; }               // Current stockpile in days of supply
        public SupplyGenerationRate GenerationRate { get; private set; } // Generation rate of the depot
        public SupplyProjection SupplyProjection { get; private set; }   // Supply projection level
        public bool SupplyPenetration { get; private set; }              // Whether the depot has supply penetration capability
        public DepotCategory DepotCategory { get; private set; }         // Category of the depot (Main or Secondary)
        public bool IsMainDepot => DepotCategory == DepotCategory.Main;

        // Properties with rules.
        public bool HasAirSupply
        {
            get => hasAirSupply && IsMainDepot;
            set
            {
                if (IsMainDepot)
                {
                    hasAirSupply = value;
                }
                else if (value)
                {
                    throw new InvalidOperationException("Only main depots can have air supply capability");
                }
            }
        }

        public bool HasNavalSupply
        {
            get => hasNavalSupply && IsMainDepot;
            set
            {
                if (IsMainDepot)
                {
                    hasNavalSupply = value;
                }
                else if (value)
                {
                    throw new InvalidOperationException("Only main depots can have naval supply capability");
                }
            }
        }

        public int ProjectionRadius => CUConstants.ProjectionRangeValues[SupplyProjection];

        #endregion // Properties


        #region Constructors

        public SupplyDepotFacility() : base()
        {
            try
            {
                // Use base class default, then set depot-specific name and initialize depot properties
                SetBaseName("Supply Depot");
                SetDepotSize(DepotSize.Small); // Default to small depot
                SupplyPenetration = false;
                DepotCategory = DepotCategory.Secondary;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DefaultConstructor", e);
                throw;
            }
        }

        public SupplyDepotFacility(string name, Side side, DepotSize depotSize, bool isMainDepot = false, int initialDamage = 0)
            : base(name ?? "Supply Depot", side, initialDamage)
        {
            try
            {
                // Set depot category based on whether it's a main depot
                DepotCategory = isMainDepot ? DepotCategory.Main : DepotCategory.Secondary;
                SetDepotSize(depotSize);
                SupplyPenetration = false;

                // Main depots have special characteristics.
                if (isMainDepot)
                {
                    EnableAirSupply(); // Main depots can have air supply by default
                    EnableNavalSupply(); // Main depots can have naval supply by default
                }
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
        protected SupplyDepotFacility(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            try
            {
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
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Supply Management Methods

        /// <summary>
        /// Get the max amount of supplies the depot can hold based on its size.
        /// </summary>
        /// <returns>Days of supply</returns>
        private float GetMaxStockpile()
        {
            return CUConstants.MaxStockpileBySize[DepotSize];
        }

        /// <summary>
        /// The inherent generation rate that the depot can produce based on its generation rate setting.
        /// </summary>
        /// <returns></returns>
        private float GetCurrentGenerationRate()
        {
            float baseRate = CUConstants.GenerationRateValues[GenerationRate];
            float efficiencyMultiplier = GetEfficiencyMultiplier();

            return baseRate * efficiencyMultiplier;
        }

        /// <summary>
        /// Add supplies directly to depot.
        /// </summary>
        /// <param name="amount"></param>
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
                AppService.Instance.HandleException(CLASS_NAME, "AddSupplies", e);
                throw;
            }
        }

        /// <summary>
        /// Remove supplies from the depot.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
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
                AppService.Instance.HandleException(CLASS_NAME, "RemoveSupplies", e);
                return 0f;
            }
        }

        /// <summary>
        /// Add supplies to the depot based on its generation rate.
        /// </summary>
        /// <returns></returns>
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
                AppService.Instance.HandleException(CLASS_NAME, "GenerateSupplies", e);
                return 0f;
            }
        }

        /// <summary>
        /// Determines whether a supply depot can provide supply to a unit at a specified distance and after crossing a
        /// given number of enemy zones of control (ZOCs).
        /// </summary>
        /// <remarks>The method checks whether the depot is operational, whether the unit is within the
        /// depot's projection radius,  and whether the number of enemy ZOCs crossed is within the depot's supply
        /// penetration capability.</remarks>
        /// <param name="distanceInHexes">The distance to the unit in hexes. Must be less than or equal to the depot's projection radius.</param>
        /// <param name="enemyZOCsCrossed">The number of enemy zones of control (ZOCs) crossed to reach the unit. Must not exceed the depot's supply
        /// penetration capability.</param>
        /// <returns><see langword="true"/> if the depot can supply the unit at the specified distance and ZOC conditions;
        /// otherwise, <see langword="false"/>.</returns>
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

                // Check if enemy ZOCs crossed
                if (enemyZOCsCrossed > 0)
                {
                    // If supply penetration is not enabled, we cannot cross enemy ZOCs
                    if (!SupplyPenetration) return false;

                    // If supply penetration is enabled, we can cross enemy ZOCs but must check the limit
                    if (enemyZOCsCrossed > CUConstants.ZOC_RANGE) return false;
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
        /// Supplies a unit with resources based on the distance, enemy zones of control (ZOCs) crossed,  and
        /// operational efficiency.
        /// </summary>
        /// <remarks>The amount of supplies delivered is calculated based on several factors: <list
        /// type="bullet"> <item> <description>Distance to the unit, which reduces efficiency
        /// proportionally.</description> </item> <item> <description>Enemy ZOCs crossed, which further reduces
        /// efficiency.</description> </item> <item> <description>Operational efficiency, determined by internal
        /// multipliers.</description> </item> </list> The method ensures that the efficiency does not drop below a
        /// minimum threshold. Supplies are deducted  from the stockpile after delivery. If the stockpile is
        /// insufficient or the unit cannot be supplied,  no supplies are delivered.</remarks>
        /// <param name="distanceInHexes">The distance to the unit in hexes. A greater distance reduces the efficiency of the supply delivery.</param>
        /// <param name="enemyZOCsCrossed">The number of enemy zones of control (ZOCs) crossed to reach the unit. Each ZOC crossed reduces  the
        /// efficiency of the supply delivery.</param>
        /// <returns>The amount of supplies delivered to the unit, expressed in days of supply. Returns <see langword="0"/>  if
        /// the unit cannot be supplied due to insufficient stockpile or operational constraints.</returns>
        public float SupplyUnit(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                // Check if we can supply the unit
                if (!CanSupplyUnitAt(distanceInHexes, enemyZOCsCrossed))
                {
                    return 0f;
                }

                // Check if we have supplies to give
                if (StockpileInDays <= CUConstants.MaxDaysSupplyUnit)
                {
                    return 0f;
                }

                // Calculate efficiency based on distance, enemy ZOCs, and operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)ProjectionRadius * CUConstants.DISTANCE_EFF_MULT);
                float zocEfficiency = 1f - (enemyZOCsCrossed * CUConstants.ZOC_EFF_MULT);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * zocEfficiency * operationalEfficiency;

                // Ensure efficiency doesn't go below a minimum threshold
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                // Calculate amount to deliver
                float amountToDeliver = CUConstants.MaxDaysSupplyUnit * totalEfficiency;

                // Remove supplies from stockpile
                StockpileInDays -= CUConstants.MaxDaysSupplyUnit;

                // Return the actual amount delivered (proportional to what we could afford)
                return amountToDeliver;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SupplyUnit", e);
                return 0f;
            }
        }

        /// <summary>
        /// Calculates the percentage of the stockpile used relative to its maximum capacity.
        /// </summary>
        /// <remarks>If the maximum stockpile capacity is zero or an exception occurs, the method returns
        /// 0.</remarks>
        /// <returns>A <see cref="float"/> representing the stockpile usage as a percentage of the maximum capacity. Returns 0 if
        /// the maximum capacity is zero or an error occurs.</returns>
        public float GetStockpilePercentage()
        {
            try
            {
                float maxCapacity = GetMaxStockpile();
                return maxCapacity > 0 ? StockpileInDays / maxCapacity : 0f;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetStockpilePercentage", e);
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

        /// <summary>
        /// Upgrades the depot size to the next available tier.
        /// </summary>
        /// <remarks>The depot size progresses through the following tiers: Small, Medium, Large, and
        /// Huge.  If the depot is already at the maximum size (Huge), the method returns <see
        /// langword="false"/>.</remarks>
        /// <returns><see langword="true"/> if the depot size was successfully upgraded to the next tier;  otherwise, <see
        /// langword="false"/> if the depot is already at the maximum size or an error occurs.</returns>
        public bool UpgradeDepotSize()
        {
            try
            {
                // Change parameters based on size.
                switch (DepotSize)
                {
                    case DepotSize.Small:
                        SetDepotSize(DepotSize.Medium);
                        return true;
                    case DepotSize.Medium:
                        SetDepotSize(DepotSize.Large);
                        return true;
                    case DepotSize.Large:
                        SetDepotSize(DepotSize.Huge);
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
        /// The leader determines if supply penetration is enabled or not.
        /// </summary>
        /// <param name="enabled"></param>
        public void SetLeaderSupplyPenetration(bool enabled)
        {
            if (enabled) SupplyPenetration = true;
            else SupplyPenetration = false;
        }

        /// <summary>
        /// Set the size of the depot and sets to max stockpile.
        /// </summary>
        /// <param name="depotSize"></param>
        private void SetDepotSize (DepotSize depotSize)
        {
            try
            {
                switch (depotSize)
                {
                    case DepotSize.Small:
                        DepotSize = DepotSize.Small;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Minimal;
                        SupplyProjection = SupplyProjection.Local;
                        break;
                    case DepotSize.Medium:
                        DepotSize = DepotSize.Medium;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Basic;
                        SupplyProjection = SupplyProjection.Extended;
                        break;
                    case DepotSize.Large:
                        DepotSize = DepotSize.Large;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Standard;
                        SupplyProjection = SupplyProjection.Regional;
                        break;
                    case DepotSize.Huge:
                        DepotSize = DepotSize.Huge;
                        StockpileInDays = GetMaxStockpile();
                        GenerationRate = SupplyGenerationRate.Enhanced;
                        SupplyProjection = SupplyProjection.Strategic;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(depotSize), "Invalid depot size specified");
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetDepotSize", e);
            }
        }

        #endregion // Upgrade Methods


        #region Special Ability Methods

        /// <summary>
        /// Enables the air supply for the current depot.
        /// </summary>
        /// <remarks>This method sets the <see cref="HasAirSupply"/> property to <see langword="true"/> if
        /// the depot is the main depot. If the depot is not the main depot, the method returns <see langword="false"/>
        /// without enabling the air supply.</remarks>
        /// <returns><see langword="true"/> if the air supply was successfully enabled; otherwise, <see langword="false"/>.</returns>
        public bool EnableAirSupply()
        {
            try
            {
                if (!IsMainDepot) return false;

                HasAirSupply = true;

                return true;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "EnableAirSupply", e);
                return false;
            }
        }

        /// <summary>
        /// Enables the naval supply for the current depot.
        /// </summary>
        /// <returns></returns>
        public bool EnableNavalSupply()
        {
            try
            {
                if (!IsMainDepot) return false;

                HasNavalSupply = true;

                return true;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "EnableNavalSupply", e);
                return false;
            }
        }

        /// <summary>
        /// Performs air supply to a unit at a specified distance in hexes.
        /// </summary>
        /// <param name="distanceInHexes"></param>
        /// <returns></returns>
        public float PerformAirSupply(int distanceInHexes)
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

                if (StockpileInDays <= CUConstants.MaxDaysSupplyUnit)
                {
                    return 0f;
                }

                // Air supply efficiency decreases with distance and is affected by operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)CUConstants.AirSupplyMaxRange * CUConstants.DISTANCE_EFF_MULT);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * operationalEfficiency;

                // Ensure minimum efficiency
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                // Remove the supplies from stockpile
                StockpileInDays -= CUConstants.MaxDaysSupplyUnit;

                return CUConstants.MaxDaysSupplyUnit * totalEfficiency;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "PerformAirSupply", e);
                return 0f;
            }
        }

        /// <summary>
        /// Performs a naval supply operation, delivering supplies to a unit based on the distance and operational
        /// efficiency.
        /// </summary>
        /// <remarks>Naval supply operations are influenced by the distance to the target and the
        /// operational efficiency of the depot.  Supplies are deducted from the stockpile upon successful delivery. The
        /// method ensures a minimum efficiency threshold  for supply delivery.</remarks>
        /// <param name="distanceInHexes">The distance to the target unit in hexes. Must be within the maximum naval supply range.</param>
        /// <returns>The effective amount of supplies delivered, adjusted for distance and operational efficiency.  Returns 0 if
        /// the depot is not operational, lacks naval supply capability, the distance exceeds the maximum range,  or the
        /// stockpile is insufficient.</returns>
        public float PerformNavalSupply(int distanceInHexes)
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

                if (StockpileInDays <= CUConstants.MaxDaysSupplyUnit)
                {
                    return 0f;
                }

                // Naval supply is more efficient than air but still affected by distance and operational capacity
                float distanceEfficiency = 1f - (distanceInHexes / (float)CUConstants.NavalSupplyMaxRange * CUConstants.DISTANCE_EFF_MULT);
                float operationalEfficiency = GetEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * operationalEfficiency;

                // Ensure minimum efficiency
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                // Remove the supplies from stockpile
                StockpileInDays -= CUConstants.MaxDaysSupplyUnit;

                return CUConstants.MaxDaysSupplyUnit * totalEfficiency;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "PerformNavalSupply", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "OnNewTurn", e);
            }
        }

        #endregion // Game Cycle Methods


        #region Cloning

        public override LandBaseFacility Clone()
        {
            try
            {
                var clone = new SupplyDepotFacility();

                // Copy base class properties (BaseID will be new)
                clone.SetBaseName(this.BaseName);
                clone.SetSide(this.Side);
                clone.SetDamage(this.Damage);

                // Copy depot-specific attributes
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
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        public SupplyDepotFacility CloneTyped()
        {
            return (SupplyDepotFacility)Clone();
        }

        #endregion // Cloning


        #region ISerializable Implementation

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with the data needed to serialize the current object.
        /// </summary>
        /// <remarks>This method serializes the current object by adding relevant depot-specific data to the <paramref
        /// name="info"/> parameter. Base class properties are handled by the base implementation.</remarks>
        /// <param name="info">The <see cref="SerializationInfo"/> object to populate with serialization data. Cannot be <see langword="null"/>.</param>
        /// <param name="context">The <see cref="StreamingContext"/> structure that contains the source and destination of the serialized stream.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Call base class serialization first
                base.GetObjectData(info, context);

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
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation
    }
}