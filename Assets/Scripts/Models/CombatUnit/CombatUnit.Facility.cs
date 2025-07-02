using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Models
{
 /*───────────────────────────────────────────────────────────────────────────────
  CombatUnit.Facility ─ base-facility subsystem (HQ, Airbase, Supply Depot)
────────────────────────────────────────────────────────────────────────────────
 Summary
 ═══════
 • Adds facility-specific state & behaviour to **CombatUnit** when *IsBase* is
   true: damage/repair, operational capacity, attached air-unit logistics, and
   supply-depot stockpile & projection math.  Handles three facility types
   (HQ, Airbase, SupplyDepot) selected from the unit’s *Classification*. :contentReference[oaicite:0]{index=0}

 Public properties
 ═════════════════
   int                      BaseDamage              { get; private set; }
   OperationalCapacity      OperationalCapacity     { get; private set; }
   FacilityType             FacilityType            { get; private set; }

   // Supply-depot only
   DepotSize                DepotSize               { get; private set; }
   float                    StockpileInDays         { get; private set; }
   SupplyGenerationRate     GenerationRate          { get; private set; }
   SupplyProjection         SupplyProjection        { get; private set; }
   bool                     SupplyPenetration       { get; private set; }
   DepotCategory            DepotCategory           { get; private set; }
   int                      ProjectionRadius        => IsBase ? … : 0;
   bool                     IsMainDepot             => IsBase && DepotCategory == Main;

   // Airbase only
   IReadOnlyList<CombatUnit> AirUnitsAttached       { get; private set; }

 Constructors
 ═════════════
   ⟡ (none in this partial; constructed via main *CombatUnit* ctor)

 Public API (method signatures ⇢ brief purpose)
 ═════════════════════════════════════════════
 ― Facility damage / capacity ―
   public void   AddFacilityDamage(int dmg)               // apply bomb/strike damage
   public void   RepairFacilityDamage(int amt)            // restore damage
   public void   SetFacilityDamage(int lvl)               // direct set (load/debug)
   public float  GetFacilityEfficiencyMultiplier()        // 0-1 scalar vs. capacity
   public bool   IsFacilityOperational()                  // capacity ≠ OutOfOperation

 ― Airbase management ―
   public bool          AddAirUnit(CombatUnit unit)       // attach aircraft
   public bool          RemoveAirUnit(CombatUnit unit)    // detach by ref
   public bool          RemoveAirUnitByID(string id)      // detach by ID
   public CombatUnit    GetAirUnitByID(string id)
   public int           GetAttachedAirUnitCount()
   public int           GetAirUnitCapacity()
   public bool          HasAirUnit(CombatUnit unit)
   public bool          HasAirUnitByID(string id)
   public void          ClearAllAirUnits()
   public bool          CanLaunchAirOperations()
   public bool          CanRepairAircraft()
   public bool          CanAcceptNewAircraft()
   public List<CombatUnit> GetOperationalAirUnits()
   public int           GetOperationalAirUnitCount()

 ― Supply-depot management ―
   public bool   AddSupplies(float days)                  // manual stock add
   public void   RemoveSupplies(float days)               // manual stock remove
   public bool   GenerateSupplies()                       // auto gen (per turn)
   public bool   CanSupplyUnitAt(int dist, int zoc)       // projection gate check
   public float  SupplyUnit(int dist, int zoc)            // over-land supply push
   public float  PerformAirSupply(int dist)               // main-depot airlift
   public float  PerformNavalSupply(int dist)             // main-depot sealift
   public float  GetStockpilePercentage()                 // fill ratio 0-1
   public bool   IsStockpileEmpty()
   public float  GetRemainingSupplyCapacity()
   public bool   UpgradeDepotSize()                       // bump S→M→L→H
   public void   SetSupplyPenetration(bool enabled)       // toggle ZOC bypass

 Private helpers
 ═══════════════
   // construction
   void   InitializeFacility(DepotCategory cat = Secondary, DepotSize size = Small)
   void   SetupHQ()
   void   SetupAirbase()
   void   SetupSupplyDepot(DepotCategory cat, DepotSize size)

   // damage & capacity
   void   UpdateOperationalCapacity()

   // depot maths
   float  GetMaxStockpile()
   float  GetCurrentGenerationRate()
   void   SetDepotSize(DepotSize size)

 Developer notes
 ═══════════════
 • **OperationalCapacity drive-wheel** – All efficiency checks funnel through
   *GetFacilityEfficiencyMultiplier()*; any new capacity tier must update both
   that switch and the constants table.
 • **AirUnit attachment persistence** – Air units are stored by hard reference
   at runtime and by *UnitID* during serialization (*_attachedUnitIDs* list).
   Ensure *ResolveReferences()* in the root class re-hydrates this list after
   load.
 • **Supply projection model** – Distance & ZOC efficiency multipliers are
   tuned in *CUConstants.DISTANCE_EFF_MULT* and *.ZOC_EFF_MULT*.  Balance
   designers can tweak without touching code.
───────────────────────────────────────────────────────────────────────────────*/
    public partial class CombatUnit
    {
        #region Facility Fields

        // Units attached to an airbase
        private readonly List<CombatUnit> _airUnitsAttached = new List<CombatUnit>();
        private readonly List<string> _attachedUnitIDs = new List<string>(); // For deserialization

        #endregion // Facility Fields


        #region Facility Properties

        // Common facility properties
        public int BaseDamage { get; private set; }
        public OperationalCapacity OperationalCapacity { get; private set; }
        public FacilityType FacilityType { get; private set; }

        // Supply depot specific properties
        public DepotSize DepotSize { get; private set; }
        public float StockpileInDays { get; private set; }
        public SupplyGenerationRate GenerationRate { get; private set; }
        public SupplyProjection SupplyProjection { get; private set; }
        public bool SupplyPenetration { get; private set; }
        public DepotCategory DepotCategory { get; private set; }
        public int ProjectionRadius => IsBase ? CUConstants.ProjectionRangeValues[SupplyProjection] : 0;
        public bool IsMainDepot => IsBase && DepotCategory == DepotCategory.Main;

        // Airbase specific properties
        public IReadOnlyList<CombatUnit> AirUnitsAttached { get; private set; }

        #endregion // Facility Properties


        #region Facility Initialization

        /// <summary>
        /// Initializes facility properties for base units during construction.
        /// Called from main CombatUnit constructor when IsBase is true.
        /// </summary>
        /// <param name="category">Depot category for supply depots</param>
        /// <param name="size">Depot size for supply depots</param>
        private void InitializeFacility(DepotCategory category = DepotCategory.Secondary, DepotSize size = DepotSize.Small)
        {
            try
            {
                if (!IsBase) return;

                // Initialize common facility properties
                BaseDamage = 0;
                OperationalCapacity = OperationalCapacity.Full;
                SupplyPenetration = false;

                // Initialize readonly collection
                AirUnitsAttached = _airUnitsAttached.AsReadOnly();

                // Set facility type and specific properties based on classification
                switch (Classification)
                {
                    case UnitClassification.HQ:
                        SetupHQ();
                        break;
                    case UnitClassification.DEPOT:
                        SetupSupplyDepot(category, size);
                        break;
                    case UnitClassification.AIRB:
                        SetupAirbase();
                        break;
                    default:
                        throw new ArgumentException($"Unit classification {Classification} is not a valid base type");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "InitializeFacility", e);
                throw;
            }
        }

        /// <summary>
        /// Configures the unit as a headquarters facility.
        /// </summary>
        private void SetupHQ()
        {
            FacilityType = FacilityType.HQ;
            // HQ has no additional specific properties beyond common facility properties
        }

        /// <summary>
        /// Configures the unit as an airbase facility.
        /// </summary>
        private void SetupAirbase()
        {
            FacilityType = FacilityType.Airbase;
            // Airbase uses the air units collection initialized above
        }

        /// <summary>
        /// Configures the unit as a supply depot facility.
        /// </summary>
        /// <param name="category">Depot category (Main or Secondary)</param>
        /// <param name="size">Depot size</param>
        private void SetupSupplyDepot(DepotCategory category, DepotSize size)
        {
            FacilityType = FacilityType.SupplyDepot;
            DepotCategory = category;
            SetDepotSize(size);
        }

        #endregion // Facility Initialization


        #region Base Damage and Operational Capacity Management

        /// <summary>
        /// Applies damage to the facility, reducing operational capacity.
        /// </summary>
        /// <param name="incomingDamage">Amount of damage to apply</param>
        public void AddFacilityDamage(int incomingDamage)
        {
            try
            {
                if (!IsBase)
                {
                    throw new InvalidOperationException("Cannot add facility damage to non-base units");
                }

                if (incomingDamage < 0)
                {
                    throw new ArgumentException("Incoming damage cannot be negative", nameof(incomingDamage));
                }

                int newDamage = BaseDamage + incomingDamage;
                BaseDamage = Math.Max(CUConstants.MIN_DAMAGE, Math.Min(CUConstants.MAX_DAMAGE, newDamage));

                UpdateOperationalCapacity();

                AppService.CaptureUiMessage($"{UnitName} has suffered {incomingDamage} facility damage. Current damage level: {BaseDamage}.");
                AppService.CaptureUiMessage($"{UnitName} current operational capacity is: {OperationalCapacity}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddFacilityDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Repairs facility damage, restoring operational capacity.
        /// </summary>
        /// <param name="repairAmount">Amount of damage to repair</param>
        public void RepairFacilityDamage(int repairAmount)
        {
            try
            {
                if (!IsBase)
                {
                    throw new InvalidOperationException("Cannot repair facility damage on non-base units");
                }

                if (repairAmount < 0)
                {
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));
                }

                repairAmount = Math.Max(0, Math.Min(CUConstants.MAX_DAMAGE, repairAmount));
                BaseDamage -= repairAmount;
                BaseDamage = Math.Max(CUConstants.MIN_DAMAGE, Math.Min(CUConstants.MAX_DAMAGE, BaseDamage));

                UpdateOperationalCapacity();

                AppService.CaptureUiMessage($"{UnitName} has been repaired by {repairAmount}. Current damage level: {BaseDamage}.");
                AppService.CaptureUiMessage($"{UnitName} current operational capacity is: {OperationalCapacity}");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RepairFacilityDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Sets facility damage to a specific level (0-100).
        /// </summary>
        /// <param name="newDamageLevel">New damage level</param>
        public void SetFacilityDamage(int newDamageLevel)
        {
            try
            {
                if (!IsBase)
                {
                    throw new InvalidOperationException("Cannot set facility damage on non-base units");
                }

                if (newDamageLevel < CUConstants.MIN_DAMAGE || newDamageLevel > CUConstants.MAX_DAMAGE)
                {
                    throw new ArgumentOutOfRangeException(nameof(newDamageLevel),
                        $"Damage level must be between {CUConstants.MIN_DAMAGE} and {CUConstants.MAX_DAMAGE}");
                }

                BaseDamage = newDamageLevel;
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetFacilityDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the facility efficiency multiplier based on operational capacity.
        /// </summary>
        /// <returns>Efficiency multiplier (0.0 to 1.0)</returns>
        public float GetFacilityEfficiencyMultiplier()
        {
            if (!IsBase) return 0.0f;

            return OperationalCapacity switch
            {
                OperationalCapacity.Full => CUConstants.BASE_CAPACITY_LVL5,
                OperationalCapacity.SlightlyDegraded => CUConstants.BASE_CAPACITY_LVL4,
                OperationalCapacity.ModeratelyDegraded => CUConstants.BASE_CAPACITY_LVL3,
                OperationalCapacity.HeavilyDegraded => CUConstants.BASE_CAPACITY_LVL2,
                OperationalCapacity.OutOfOperation => CUConstants.BASE_CAPACITY_LVL1,
                _ => 0.0f,
            };
        }

        /// <summary>
        /// Checks if the facility is operational.
        /// </summary>
        /// <returns>True if facility can operate</returns>
        public bool IsFacilityOperational()
        {
            return IsBase && OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Updates operational capacity based on current damage level.
        /// </summary>
        private void UpdateOperationalCapacity()
        {
            if (BaseDamage >= 81 && BaseDamage <= 100)
            {
                OperationalCapacity = OperationalCapacity.OutOfOperation;
            }
            else if (BaseDamage >= 61 && BaseDamage <= 80)
            {
                OperationalCapacity = OperationalCapacity.HeavilyDegraded;
            }
            else if (BaseDamage >= 41 && BaseDamage <= 60)
            {
                OperationalCapacity = OperationalCapacity.ModeratelyDegraded;
            }
            else if (BaseDamage >= 21 && BaseDamage <= 40)
            {
                OperationalCapacity = OperationalCapacity.SlightlyDegraded;
            }
            else
            {
                OperationalCapacity = OperationalCapacity.Full;
            }
        }

        #endregion // Base Damage and Operational Capacity Management


        #region Airbase Management

        /// <summary>
        /// Attaches an air unit to this airbase.
        /// </summary>
        /// <param name="unit">Air unit to attach</param>
        /// <returns>True if attachment was successful</returns>
        public bool AddAirUnit(CombatUnit unit)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot add air units to non-airbase facilities");
                }

                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                if (_airUnitsAttached.Count >= CUConstants.MAX_AIR_UNITS)
                {
                    AppService.CaptureUiMessage($"{UnitName} is already at maximum air unit capacity.");
                    return false;
                }

                if (unit.UnitType != UnitType.AirUnit)
                {
                    throw new InvalidOperationException($"Only air units can be attached to an airbase. Unit type: {unit.UnitType}");
                }

                if (_airUnitsAttached.Contains(unit))
                {
                    AppService.CaptureUiMessage($"{unit.UnitName} is already attached to this airbase");
                    return false;
                }

                _airUnitsAttached.Add(unit);
                AppService.CaptureUiMessage($"{unit.UnitName} has been attached to {UnitName}.");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddAirUnit", e);
                return false;
            }
        }

        /// <summary>
        /// Removes an air unit from this airbase.
        /// </summary>
        /// <param name="unit">Air unit to remove</param>
        /// <returns>True if removal was successful</returns>
        public bool RemoveAirUnit(CombatUnit unit)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot remove air units from non-airbase facilities");
                }

                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                if (_airUnitsAttached.Remove(unit))
                {
                    AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {UnitName}.");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveAirUnit", e);
                return false;
            }
        }

        /// <summary>
        /// Removes an air unit by ID from this airbase.
        /// </summary>
        /// <param name="unitID">ID of air unit to remove</param>
        /// <returns>True if removal was successful</returns>
        public bool RemoveAirUnitByID(string unitID)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot remove air units from non-airbase facilities");
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    throw new ArgumentException("Unit ID cannot be null or empty", nameof(unitID));
                }

                var unit = _airUnitsAttached.FirstOrDefault(u => u.UnitID == unitID);
                if (unit != null)
                {
                    if (_airUnitsAttached.Remove(unit))
                    {
                        AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {UnitName}.");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveAirUnitByID", e);
                return false;
            }
        }

        /// <summary>
        /// Gets an air unit by ID from this airbase.
        /// </summary>
        /// <param name="unitID">ID of air unit to retrieve</param>
        /// <returns>Air unit if found, null otherwise</returns>
        public CombatUnit GetAirUnitByID(string unitID)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(unitID))
                {
                    return null;
                }

                return _airUnitsAttached.FirstOrDefault(u => u.UnitID == unitID);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetAirUnitByID", e);
                return null;
            }
        }

        /// <summary>
        /// Gets the number of air units currently attached.
        /// </summary>
        /// <returns>Count of attached air units</returns>
        public int GetAttachedAirUnitCount()
        {
            return IsBase && FacilityType == FacilityType.Airbase ? _airUnitsAttached.Count : 0;
        }

        /// <summary>
        /// Gets the remaining air unit capacity.
        /// </summary>
        /// <returns>Number of additional air units that can be attached</returns>
        public int GetAirUnitCapacity()
        {
            return IsBase && FacilityType == FacilityType.Airbase
                ? CUConstants.MAX_AIR_UNITS - _airUnitsAttached.Count
                : 0;
        }

        /// <summary>
        /// Checks if a specific air unit is attached.
        /// </summary>
        /// <param name="unit">Unit to check</param>
        /// <returns>True if unit is attached</returns>
        public bool HasAirUnit(CombatUnit unit)
        {
            return IsBase && FacilityType == FacilityType.Airbase && unit != null && _airUnitsAttached.Contains(unit);
        }

        /// <summary>
        /// Checks if an air unit with the specified ID is attached.
        /// </summary>
        /// <param name="unitID">Unit ID to check</param>
        /// <returns>True if unit is attached</returns>
        public bool HasAirUnitByID(string unitID)
        {
            return IsBase && FacilityType == FacilityType.Airbase &&
                   !string.IsNullOrEmpty(unitID) &&
                   _airUnitsAttached.Any(u => u.UnitID == unitID);
        }

        /// <summary>
        /// Removes all air units from this airbase.
        /// </summary>
        public void ClearAllAirUnits()
        {
            try
            {
                if (IsBase && FacilityType == FacilityType.Airbase)
                {
                    _airUnitsAttached.Clear();
                    AppService.CaptureUiMessage($"All air units have been removed from {UnitName}.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearAllAirUnits", e);
            }
        }

        /// <summary>
        /// Checks if this airbase can launch air operations.
        /// </summary>
        /// <returns>True if launch operations are possible</returns>
        public bool CanLaunchAirOperations()
        {
            return IsBase && FacilityType == FacilityType.Airbase &&
                   OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Checks if this airbase can repair aircraft.
        /// </summary>
        /// <returns>True if repair operations are possible</returns>
        public bool CanRepairAircraft()
        {
            return IsBase && FacilityType == FacilityType.Airbase &&
                   OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Checks if this airbase can accept new aircraft.
        /// </summary>
        /// <returns>True if airbase has capacity</returns>
        public bool CanAcceptNewAircraft()
        {
            return IsBase && FacilityType == FacilityType.Airbase && GetAirUnitCapacity() > 0;
        }

        /// <summary>
        /// Gets all operational air units attached to this airbase.
        /// </summary>
        /// <returns>List of operational air units</returns>
        public List<CombatUnit> GetOperationalAirUnits()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.Airbase)
                {
                    return new List<CombatUnit>();
                }

                return _airUnitsAttached.Where(unit => unit != null &&
                                                      unit.EfficiencyLevel != EfficiencyLevel.StaticOperations)
                                      .ToList();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetOperationalAirUnits", e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Gets the count of operational air units.
        /// </summary>
        /// <returns>Number of operational air units</returns>
        public int GetOperationalAirUnitCount()
        {
            return GetOperationalAirUnits().Count;
        }

        #endregion // Airbase Management


        #region Supply Depot Management

        /// <summary>
        /// Gets the maximum stockpile capacity based on depot size.
        /// </summary>
        /// <returns>Maximum stockpile in days</returns>
        private float GetMaxStockpile()
        {
            return IsBase && FacilityType == FacilityType.SupplyDepot
                ? CUConstants.MaxStockpileBySize[DepotSize]
                : 0f;
        }

        /// <summary>
        /// Gets the current generation rate considering efficiency.
        /// </summary>
        /// <returns>Current generation rate</returns>
        private float GetCurrentGenerationRate()
        {
            if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;

            float baseRate = CUConstants.GenerationRateValues[GenerationRate];
            float efficiencyMultiplier = GetFacilityEfficiencyMultiplier();
            return baseRate * efficiencyMultiplier;
        }

        /// <summary>
        /// Adds supplies directly to the depot stockpile.
        /// </summary>
        /// <param name="amount">Amount of supplies to add</param>
        /// <returns>True if supplies were added successfully</returns>
        public bool AddSupplies(float amount)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;

                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                if (StockpileInDays >= GetMaxStockpile())
                {
                    AppService.CaptureUiMessage($"{UnitName} stockpile is already full. Cannot add more supplies.");
                    return false;
                }

                float maxCapacity = GetMaxStockpile();
                float newAmount = StockpileInDays + amount;
                StockpileInDays = Math.Min(newAmount, maxCapacity);

                AppService.CaptureUiMessage($"{UnitName} has added {amount} days of supply. Current stockpile: {StockpileInDays} days.");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddSupplies", e);
                return false;
            }
        }

        /// <summary>
        /// Removes supplies from the depot stockpile.
        /// </summary>
        /// <param name="amount">Amount of supplies to remove</param>
        public void RemoveSupplies(float amount)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return;

                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                float actualAmount = Math.Min(amount, StockpileInDays);
                StockpileInDays -= actualAmount;

                AppService.CaptureUiMessage($"{UnitName} has removed {actualAmount} days of supply. Current stockpile: {StockpileInDays} days.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveSupplies", e);
            }
        }

        /// <summary>
        /// Generates supplies based on the depot's generation rate (called once per turn).
        /// </summary>
        /// <returns>True if supplies were generated successfully</returns>
        public bool GenerateSupplies()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;

                if (!IsFacilityOperational())
                {
                    AppService.CaptureUiMessage($"{UnitName} is not operational and cannot generate supplies.");
                    return false;
                }

                float generatedAmount = GetCurrentGenerationRate();
                float maxCapacity = GetMaxStockpile();
                float amountToAdd = Math.Min(generatedAmount, maxCapacity - StockpileInDays);
                StockpileInDays += amountToAdd;

                AppService.CaptureUiMessage($"{UnitName} has generated {amountToAdd} days of supply. Current stockpile: {StockpileInDays} days.");
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateSupplies", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if this depot can supply a unit at the specified distance and ZOC conditions.
        /// </summary>
        /// <param name="distanceInHexes">Distance to target unit</param>
        /// <param name="enemyZOCsCrossed">Number of enemy ZOCs crossed</param>
        /// <returns>True if supply is possible</returns>
        public bool CanSupplyUnitAt(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;

                if (!IsFacilityOperational()) return false;

                if (distanceInHexes > ProjectionRadius) return false;

                if (enemyZOCsCrossed > 0)
                {
                    if (!SupplyPenetration) return false;
                    if (enemyZOCsCrossed > CUConstants.ZOC_RANGE) return false;
                }

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CanSupplyUnitAt", e);
                return false;
            }
        }

        /// <summary>
        /// Supplies a unit with calculated efficiency based on distance and ZOCs.
        /// </summary>
        /// <param name="distanceInHexes">Distance to target unit</param>
        /// <param name="enemyZOCsCrossed">Number of enemy ZOCs crossed</param>
        /// <returns>Amount of supplies delivered</returns>
        public float SupplyUnit(int distanceInHexes, int enemyZOCsCrossed)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;

                if (!CanSupplyUnitAt(distanceInHexes, enemyZOCsCrossed)) return 0f;

                if (StockpileInDays <= CUConstants.MaxDaysSupplyUnit) return 0f;

                // Calculate efficiency
                float distanceEfficiency = 1f - (distanceInHexes / (float)ProjectionRadius * CUConstants.DISTANCE_EFF_MULT);
                float zocEfficiency = 1f - (enemyZOCsCrossed * CUConstants.ZOC_EFF_MULT);
                float operationalEfficiency = GetFacilityEfficiencyMultiplier();
                float totalEfficiency = distanceEfficiency * zocEfficiency * operationalEfficiency;
                totalEfficiency = Math.Max(totalEfficiency, 0.1f);

                float amountToDeliver = CUConstants.MaxDaysSupplyUnit * totalEfficiency;
                StockpileInDays -= CUConstants.MaxDaysSupplyUnit;

                return amountToDeliver;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SupplyUnit", e);
                return 0f;
            }
        }

        /// <summary>
        /// Performs air supply operation (main depot only).
        /// </summary>
        /// <param name="distanceInHexes">Distance to target</param>
        /// <returns>Amount of supplies delivered</returns>
        public float PerformAirSupply(int distanceInHexes)
        {
            try
            {
                if (!IsFacilityOperational() || !IsMainDepot || FacilityType != FacilityType.SupplyDepot)
                    return 0f;

                if (distanceInHexes > CUConstants.AirSupplyMaxRange) return 0f;
                if (StockpileInDays <= CUConstants.MaxDaysSupplyUnit) return 0f;

                float distanceEfficiency = 1f - (distanceInHexes / (float)CUConstants.AirSupplyMaxRange * CUConstants.DISTANCE_EFF_MULT);
                float operationalEfficiency = GetFacilityEfficiencyMultiplier();
                float totalEfficiency = Math.Max(distanceEfficiency * operationalEfficiency, 0.1f);

                StockpileInDays -= CUConstants.MaxDaysSupplyUnit;
                return CUConstants.MaxDaysSupplyUnit * totalEfficiency;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformAirSupply", e);
                return 0f;
            }
        }

        /// <summary>
        /// Performs naval supply operation (main depot only).
        /// </summary>
        /// <param name="distanceInHexes">Distance to target</param>
        /// <returns>Amount of supplies delivered</returns>
        public float PerformNavalSupply(int distanceInHexes)
        {
            try
            {
                if (!IsFacilityOperational() || !IsMainDepot || FacilityType != FacilityType.SupplyDepot)
                    return 0f;

                if (distanceInHexes > CUConstants.NavalSupplyMaxRange) return 0f;
                if (StockpileInDays <= CUConstants.MaxDaysSupplyUnit) return 0f;

                float distanceEfficiency = 1f - (distanceInHexes / (float)CUConstants.NavalSupplyMaxRange * CUConstants.DISTANCE_EFF_MULT);
                float operationalEfficiency = GetFacilityEfficiencyMultiplier();
                float totalEfficiency = Math.Max(distanceEfficiency * operationalEfficiency, 0.1f);

                StockpileInDays -= CUConstants.MaxDaysSupplyUnit;
                return CUConstants.MaxDaysSupplyUnit * totalEfficiency;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "PerformNavalSupply", e);
                return 0f;
            }
        }

        /// <summary>
        /// Gets the stockpile percentage (0.0 to 1.0).
        /// </summary>
        /// <returns>Stockpile fill ratio</returns>
        public float GetStockpilePercentage()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return 0f;

                float maxCapacity = GetMaxStockpile();
                return maxCapacity > 0 ? StockpileInDays / maxCapacity : 0f;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetStockpilePercentage", e);
                return 0f;
            }
        }

        /// <summary>
        /// Checks if the stockpile is empty.
        /// </summary>
        /// <returns>True if stockpile is empty</returns>
        public bool IsStockpileEmpty()
        {
            return !IsBase || FacilityType != FacilityType.SupplyDepot || StockpileInDays <= 0f;
        }

        /// <summary>
        /// Gets the remaining supply capacity.
        /// </summary>
        /// <returns>Remaining capacity in days</returns>
        public float GetRemainingSupplyCapacity()
        {
            return IsBase && FacilityType == FacilityType.SupplyDepot
                ? GetMaxStockpile() - StockpileInDays
                : 0f;
        }

        /// <summary>
        /// Upgrades the depot to the next size tier.
        /// </summary>
        /// <returns>True if upgrade was successful</returns>
        public bool UpgradeDepotSize()
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return false;

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
                AppService.HandleException(CLASS_NAME, "UpgradeDepotSize", e);
                return false;
            }
        }

        /// <summary>
        /// Sets supply penetration capability (typically controlled by leader skills).
        /// </summary>
        /// <param name="enabled">True to enable supply penetration</param>
        public void SetSupplyPenetration(bool enabled)
        {
            if (IsBase && FacilityType == FacilityType.SupplyDepot)
            {
                SupplyPenetration = enabled;
            }
        }

        /// <summary>
        /// Sets the depot size and initializes related properties.
        /// </summary>
        /// <param name="depotSize">New depot size</param>
        private void SetDepotSize(DepotSize depotSize)
        {
            try
            {
                if (!IsBase || FacilityType != FacilityType.SupplyDepot) return;

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

                if (IsBase)
                {
                    AppService.CaptureUiMessage($"{UnitName} depot has been upgraded to {DepotSize} size. Stockpile: {StockpileInDays} days.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetDepotSize", e);
            }
        }

        #endregion // Supply Depot Management
    }
}