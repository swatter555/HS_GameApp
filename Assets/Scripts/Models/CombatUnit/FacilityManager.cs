using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HammerAndSickle.Models
{
 /*───────────────────────────────────────────────────────────────────────────────
 FacilityManager  —  comprehensive base facility operations and resource management
 ────────────────────────────────────────────────────────────────────────────────
 Overview
 ════════
 **FacilityManager** handles all aspects of base facility operations in Hammer & Sickle,
 managing three distinct facility types: Headquarters (HQ), Airbases, and Supply Depots.
 Each facility type provides specialized capabilities essential for military operations,
 from command coordination to air operations to logistics support.

 The manager integrates with the parent CombatUnit system while maintaining specialized
 functionality for base operations, damage assessment, and resource distribution.
 It supports complex supply chain mechanics with distance-based efficiency calculations
 and operational capacity modifiers.

 Major Responsibilities
 ══════════════════════
 • Multi-type facility management
     - HQ: Command and control operations
     - Airbase: Air unit attachment and operations support
     - Supply Depot: Resource generation, storage, and distribution
 • Damage and operational capacity assessment
     - 5-tier damage system (0-100% with operational thresholds)
     - Efficiency multipliers affecting all facility operations
     - Repair and maintenance tracking
 • Air unit attachment system
     - Up to 4 air units per airbase with type validation
     - Operational readiness assessment and capacity management
     - Launch capability and maintenance support
 • Advanced supply distribution
     - Multi-modal supply (ground, air, naval) with range limitations
     - Distance and ZOC-based efficiency calculations
     - Stockpile management with size-based capacity scaling
 • Reference resolution and persistence
     - Two-phase loading with air unit attachment restoration
     - Cloning support for facility templates
     - Parent relationship validation and consistency checking

 Design Highlights
 ═════════════════
 • **Unified Facility Interface**: Single class manages three distinct facility types
   with type-specific validation and specialized method routing.
 • **Dynamic Efficiency System**: Operational capacity affects all facility functions
   through configurable multipliers (100% → 75% → 50% → 25% → 0%).
 • **Advanced Supply Mechanics**: Multi-factor efficiency calculations including
   distance decay, enemy ZOC penalties, and operational status modifiers.
 • **Resource Scaling**: Depot size determines capacity, generation rates, and
   projection ranges with automatic progression (Small → Medium → Large → Huge).
 • **Parent Integration**: Bidirectional relationship with CombatUnit ensuring
   consistency and proper lifecycle management.

 Public-Method Reference
 ═══════════════════════
   ── Facility Setup & Configuration ─────────────────────────────────────────────
   SetupHQ(parent)                        Configures facility as headquarters.
   SetupAirbase(parent)                   Configures facility as airbase.
   SetupSupplyDepot(parent, category, size) Configures facility as supply depot.
   SetParent(parent)                      Sets parent CombatUnit relationship.

   ── Damage & Operational Status ────────────────────────────────────────────────
   AddDamage(amount)                      Applies incoming damage with UI feedback.
   RepairDamage(amount)                   Repairs damage and updates capacity.
   SetDamage(level)                       Directly sets damage level (0-100).
   GetEfficiencyMultiplier()              Returns operational efficiency (0.0-1.0).
   IsOperational()                        Checks if facility can function.

   ── Airbase Operations ─────────────────────────────────────────────────────────
   AddAirUnit(unit)                       Attaches air unit with validation.
   RemoveAirUnit(unit)                    Detaches air unit by reference.
   RemoveAirUnitByID(unitID)              Detaches air unit by ID string.
   GetAirUnitByID(unitID)                 Retrieves attached air unit.
   HasAirUnit(unit)                       Checks if unit is attached.
   HasAirUnitByID(unitID)                 Checks attachment by ID.
   GetAttachedUnitCount()                 Returns total attached units.
   GetAirUnitCapacity()                   Returns remaining attachment slots.
   ClearAllAirUnits()                     Removes all attached air units.
   CanLaunchAirOperations()               Validates launch capability.
   CanRepairAircraft()                    Validates maintenance capability.
   CanAcceptNewAircraft()                 Checks capacity availability.
   GetOperationalAirUnits()               Returns functional air units list.
   GetOperationalAirUnitCount()           Returns functional air units count.

   ── Supply Depot Operations ────────────────────────────────────────────────────
   AddSupplies(amount)                    Adds supplies to stockpile.
   RemoveSupplies(amount)                 Removes supplies from stockpile.
   GenerateSupplies()                     Produces supplies per turn.
   CanSupplyUnitAt(distance, zocs)        Validates supply delivery capability.
   SupplyUnit(distance, zocs)             Delivers supplies with efficiency.
   PerformAirSupply(distance)             Executes air supply operation.
   PerformNavalSupply(distance)           Executes naval supply operation.
   GetStockpilePercentage()               Returns stockpile fill ratio.
   IsStockpileEmpty()                     Checks if supplies depleted.
   GetRemainingSupplyCapacity()           Returns available storage space.
   UpgradeDepotSize()                     Advances to next size tier.
   SetLeaderSupplyPenetration(enabled)    Enables ZOC penetration capability.

   ── Persistence & Cloning ──────────────────────────────────────────────────────
   GetObjectData(info, context)          ISerializable save implementation.
   ResolveReferences(manager)             Restores air unit attachments.
   HasUnresolvedReferences()              Checks for pending reference resolution.
   Clone()                                Creates clean template copy.
   CloneAsAirbase(source, parent)         Static airbase clone factory.
   CloneAsSupplyDepot(source, parent, cat, size) Static depot clone factory.
   CloneAsHQ(source, parent)              Static HQ clone factory.

 Facility Type Specializations
 ═════════════════════════════
 **Headquarters (HQ)**
 • Command and control coordination center
 • Basic facility with damage tracking and operational status
 • Supports intelligence gathering and command bonuses
 • No specialized resource management or unit attachments

 **Airbase Facilities**
 • Air unit attachment and operations support (up to 4 units)
 • Launch capability dependent on operational status
 • Aircraft maintenance and repair services
 • Type validation ensuring only air units attach
 • Operational readiness assessment for attached squadrons

 **Supply Depot Facilities**
 • Multi-tier size system with scaling capabilities:
   - Small: 30 days capacity, local projection (4 hex)
   - Medium: 50 days capacity, extended projection (8 hex)
   - Large: 80 days capacity, regional projection (12 hex)
   - Huge: 110 days capacity, strategic projection (16 hex)
 • Generation rate scaling: Minimal → Basic → Standard → Enhanced
 • Main depot designation enables special supply modes:
   - Air supply: 16 hex range, no ZOC restrictions
   - Naval supply: 12 hex range, enhanced efficiency
 • ZOC penetration capability for contested supply lines

 Supply Distribution Mechanics
 ═════════════════════════════
 **Efficiency Calculation System**
 Supply delivery effectiveness uses multi-factor efficiency:

 ```
 Total Efficiency = Distance Efficiency × ZOC Efficiency × Operational Efficiency
 
 Distance Efficiency = 1.0 - (distance / max_range × 0.4)
 ZOC Efficiency = 1.0 - (zocs_crossed × 0.3)
 Operational Efficiency = Based on damage level (1.0 → 0.75 → 0.5 → 0.25 → 0.0)
 ```

 **Supply Mode Comparison**
 • **Ground Supply**: Full projection range, ZOC restrictions apply
 • **Air Supply**: 16 hex range, ignores ZOCs, main depot only
 • **Naval Supply**: 12 hex range, enhanced efficiency, main depot only

 **Distribution Rules**
 • Minimum efficiency threshold: 10% (0.1) regardless of penalties
 • Standard delivery amount: 7 days of unit supply
 • Stockpile reserved: 7 days minimum retained at depot
 • ZOC penetration: Requires leader skill or main depot capability

 Operational Capacity System
 ═══════════════════════════
 Damage-based operational effectiveness with clear thresholds:

 **Capacity Levels**
 • **Full** (0-20 damage): 100% efficiency, all operations available
 • **Slightly Degraded** (21-40 damage): 75% efficiency, reduced performance
 • **Moderately Degraded** (41-60 damage): 50% efficiency, limited operations
 • **Heavily Degraded** (61-80 damage): 25% efficiency, minimal capability
 • **Out of Operation** (81-100 damage): 0% efficiency, no operations possible

 **Impact on Operations**
 • Supply generation rates affected by efficiency multiplier
 • Air operations unavailable when out of operation
 • All supply distribution efficiency reduced proportionally
 • Repair and upgrade capabilities limited by operational status

 Reference Resolution Pattern
 ════════════════════════════
 FacilityManager implements sophisticated reference resolution for air unit attachments:

 • **Serialization**: Stores air unit IDs with "FM_" prefix to avoid conflicts
 • **Deserialization**: Loads IDs into temporary storage (_attachedUnitIDs)
 • **Resolution**: GameDataManager reconnects air unit object references
 • **Validation**: Type checking ensures only air units are attached
 • **Error Handling**: Missing units logged as warnings, not fatal errors
 • **Cleanup**: Temporary storage cleared after successful resolution

 Parent Relationship Management
 ══════════════════════════════
 Bidirectional consistency maintained between FacilityManager and CombatUnit:
 • Parent CombatUnit references this FacilityManager instance
 • FacilityManager maintains private reference to parent CombatUnit
 • Validation methods ensure relationship integrity
 • Setup methods automatically establish parent relationships
 • Cloning preserves relationships for new unit templates

 ───────────────────────────────────────────────────────────────────────────────
 KEEP THIS COMMENT BLOCK IN SYNC WITH FACILITY SYSTEM CHANGES!
 ───────────────────────────────────────────────────────────────────────────── */
    [Serializable]
    public class FacilityManager : ISerializable, IResolvableReferences
    {
        #region Constants

        private const string CLASS_NAME = nameof(FacilityManager);

        #endregion // Constants


        #region Fields

        // The parent CombatUnit
        private CombatUnit _parent;

        // Units attached to an airbase
        private readonly List<CombatUnit> _airUnitsAttached;     // List of air units attached to the base.
        private readonly List<string> _attachedUnitIDs;          // Store IDs during deserialization

        #endregion // Fields


        #region Properties

        // Common properties
        public int BaseDamage { get; private set; }                          // Damage to the base's capabilities.
        public OperationalCapacity OperationalCapacity { get; private set; } // The level of operational capacity of the base.
        public FacilityType FacilityType { get; private set; }               // The type of base.

        // Supply depot specific properties
        public DepotSize DepotSize { get; private set; }                 // Size of the depot
        public float StockpileInDays { get; private set; }               // Current stockpile in days of supply
        public SupplyGenerationRate GenerationRate { get; private set; } // Generation rate of the depot
        public SupplyProjection SupplyProjection { get; private set; }   // Supply projection level
        public bool SupplyPenetration { get; private set; }              // Whether the depot has supply penetration capability
        public DepotCategory DepotCategory { get; private set; }         // Category of the depot (Main or Secondary)
        public int ProjectionRadius => CUConstants.ProjectionRangeValues[SupplyProjection]; // Resupply radius in hexes.

        /// <summary>
        /// The "main" depot is the player's primary supply depot, there can only be one. The primary
        /// distinction is that the main depot can support air and naval resupply actions, no other depots can.
        /// </summary>
        public bool IsMainDepot => DepotCategory == DepotCategory.Main;

        // Airbase specific properties
        public IReadOnlyList<CombatUnit> AirUnitsAttached { get; private set; } // List of air units attached to the base

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Default construtor.
        /// </summary>
        public FacilityManager()
        {
            _parent = null;
            _airUnitsAttached = new List<CombatUnit>();    
            _attachedUnitIDs = new List<string>();

            BaseDamage = 0;
            OperationalCapacity = OperationalCapacity.Full;
            FacilityType = FacilityType.HQ;
            DepotSize = DepotSize.Small;
            StockpileInDays = 0f;
            GenerationRate = SupplyGenerationRate.Minimal;
            SupplyProjection = SupplyProjection.Local;
            SupplyPenetration = false;
            DepotCategory = DepotCategory.Secondary;
            AirUnitsAttached = _airUnitsAttached.AsReadOnly();
        }

        /// <summary>
        /// Configures the current facility as an airbase.
        /// </summary>
        /// <remarks>This method sets the <see cref="FacilityType"/> property to <see
        /// cref="FacilityType.Airbase"/>. Use this method to designate the facility as an airbase in the
        /// system.</remarks>
        public void SetupAirbase(CombatUnit parent)
        {
            // Sanity check
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "Parent combat unit cannot be null");
            }

            // Set the parent unit
            _parent = parent;

            // Set base type.
            FacilityType = FacilityType.Airbase;
        }

        /// <summary>
        /// Configures the supply depot with the specified category and size.
        /// </summary>
        /// <remarks>This method sets the facility type to <see cref="FacilityType.SupplyDepot"/> and
        /// adjusts the depot's properties based on the specified category and size. If the category is <see
        /// cref="DepotCategory.Main"/>, the depot is marked as the main depot; otherwise, it is classified as a
        /// secondary depot. The size parameter is used to configure the depot's capacity.</remarks>
        /// <param name="category">The category of the depot, indicating whether it is a main or secondary depot.</param>
        /// <param name="size">The size of the depot, which determines its capacity and maximum stockpile.</param>
        public void SetupSupplyDepot(CombatUnit parent, DepotCategory category, DepotSize size)
        {
            // Sanity check
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "Parent combat unit cannot be null");
            }

            // Set the parent unit
            _parent = parent;

            // Set base type
            FacilityType = FacilityType.SupplyDepot;

            // Classigy the depot category.
            DepotCategory = category;

            // Set the size of the depot and adjust max stockpile accordingly.
            SetDepotSize(size);
        }

        /// <summary>
        /// Configures the current facility as the headquarters (HQ) for the specified parent combat unit.
        /// </summary>
        /// <param name="parent">The parent <see cref="CombatUnit"/> to associate with this headquarters. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="parent"/> is <see langword="null"/>.</exception>
        public void SetupHQ(CombatUnit parent)
        {
            // Sanity check
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "Parent combat unit cannot be null");
            }

            // Set the parent unit
            _parent = parent;

            // Set base type.
            FacilityType = FacilityType.HQ;
        }

        #endregion // Constructors


        #region Base Damage and Operational Capacity Management

        /// <summary>
        /// Add damage to a base.
        /// </summary>
        /// <param name="incomingDamage"></param>
        public void AddDamage(int incomingDamage)
        {
            try
            {
                // Validate incoming damage
                if (incomingDamage < 0)
                {
                    throw new ArgumentException("Incoming damage cannot be negative", nameof(incomingDamage));
                }

                // Add the incoming damage to the current damage, then clamp the result
                int newDamage = BaseDamage + incomingDamage;
                BaseDamage = Math.Max(CUConstants.MIN_DAMAGE, Math.Min(CUConstants.MAX_DAMAGE, newDamage));

                // Update operational capacity based on the new damage level
                UpdateOperationalCapacity();

                // Notify the UI about the damage event
                if(_parent != null)
                {
                    AppService.CaptureUiMessage($"{_parent.UnitName} has suffered {incomingDamage} damage. Current damage level: {BaseDamage}.");
                    AppService.CaptureUiMessage($"{_parent.UnitName} current operational capacity is: {OperationalCapacity}");
                }
                else
                {
                    AppService.CaptureUiMessage($"A facility has suffered {incomingDamage} damage. Current damage level: {BaseDamage}.");
                    AppService.CaptureUiMessage($"Current operational capacity is: {OperationalCapacity}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Repairs the damage to the object by reducing the current damage level.
        /// </summary>
        /// <remarks>The method adjusts the current damage level by subtracting the specified repair
        /// amount, ensuring  that the resulting damage level remains within the valid range. After the damage is
        /// updated, the  operational capacity of the object is recalculated to reflect the new damage level.</remarks>
        /// <param name="repairAmount">The amount of damage to repair. Must be a non-negative value. The actual repair amount is clamped  to ensure
        /// it does not exceed the maximum allowable damage.</param>
        public void RepairDamage(int repairAmount)
        {
            try
            {
                // Validate repair amount
                if (repairAmount < 0)
                {
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));
                }

                // Clamp the repair amount to be between 0 and 100
                repairAmount = Math.Max(0, Math.Min(CUConstants.MAX_DAMAGE, repairAmount));

                // Remove the repair amount from the current damage
                BaseDamage -= repairAmount;

                // Clamp the total damage to be between 0 and 100
                BaseDamage = Math.Max(CUConstants.MIN_DAMAGE, Math.Min(CUConstants.MAX_DAMAGE, BaseDamage));

                // Update operational capacity based on the new damage level
                UpdateOperationalCapacity();

                // Notify the UI about the repair event
                if (_parent != null)
                {
                    AppService.CaptureUiMessage($"{_parent.UnitName} has been repaired by {repairAmount}. Current damage level: {BaseDamage}.");
                    AppService.CaptureUiMessage($"{_parent.UnitName} current operational capacity is: {OperationalCapacity}");
                }
                else
                {
                    AppService.CaptureUiMessage($"A facility has been repaired by {repairAmount}. Current damage level: {BaseDamage}.");
                    AppService.CaptureUiMessage($"Current operational capacity is: {OperationalCapacity}");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RepairDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Set damage to a given level (0-100).
        /// </summary>
        /// <param name="newDamageLevel"></param>
        public void SetDamage(int newDamageLevel)
        {
            try
            {
                // Validate damage level
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
                AppService.HandleException(CLASS_NAME, "SetDamage", e);
                throw;
            }
        }

        /// <summary>
        /// Calculates the efficiency multiplier based on the current operational capacity.
        /// </summary>
        /// <returns>A <see cref="float"/> value representing the efficiency multiplier: <list type="bullet">
        /// <item><description>1.0 if the operational capacity is <see
        /// cref="OperationalCapacity.Full"/>.</description></item> <item><description>0.75 if the operational capacity
        /// is <see cref="OperationalCapacity.SlightlyDegraded"/>.</description></item> <item><description>0.5 if the
        /// operational capacity is <see cref="OperationalCapacity.ModeratelyDegraded"/>.</description></item>
        /// <item><description>0.25 if the operational capacity is <see
        /// cref="OperationalCapacity.HeavilyDegraded"/>.</description></item> <item><description>0.0 if the operational
        /// capacity is <see cref="OperationalCapacity.OutOfOperation"/> or an unrecognized value.</description></item>
        /// </list></returns>
        public float GetEfficiencyMultiplier()
        {
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
        /// Returns whether the facility is operational based on its current operational capacity.
        /// </summary>
        /// <returns></returns>
        public bool IsOperational()
        {
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Updates the operational capacity of the object based on its current damage level.
        /// </summary>
        /// <remarks>This method evaluates the current damage level and assigns the appropriate 
        /// operational capacity state. The operational capacity is categorized into  five levels: Full,
        /// SlightlyDegraded, ModeratelyDegraded, HeavilyDegraded,  and OutOfOperation, depending on the damage
        /// percentage.</remarks>
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
        /// Add an air unit to the airbase.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool AddAirUnit(CombatUnit unit)
        {
            try
            {
                // Validate parent consistency first
                if (!ValidateParentConsistency())
                {
                    throw new InvalidOperationException("Invalid parent relationship detected");
                }

                // Make sure it's the proper base type.
                if (FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot add air units to a non-airbase facility");
                }

                // Check for null
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                // Make sure we don't add more than the maximum allowed
                if (_airUnitsAttached.Count >= CUConstants.MAX_AIR_UNITS)
                {
                    AppService.CaptureUiMessage($"{_parent.UnitName} is already full.");
                    return false; // Cannot add unit if capacity is full
                }

                // Make sure it's an aircraft unit
                if (unit.UnitType != UnitType.AirUnit)
                {
                    throw new InvalidOperationException($"Only air units can be attached to an airbase. Unit type: {unit.UnitType}");
                }

                // Check for duplicates
                if (_airUnitsAttached.Contains(unit))
                {
                    AppService.CaptureUiMessage($"{unit.UnitName} is already attached to this airbase");
                    return false; // Unit is already attached
                }

                // Add the unit to the airbase
                _airUnitsAttached.Add(unit);

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddAirUnit", e);
                throw;
            }
        }

        /// <summary>
        /// Remove an air unit from the airbase.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool RemoveAirUnit(CombatUnit unit)
        {
            try
            {
                // Make sure it's the proper base type.
                if (FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot remove air units from a non-airbase facility");
                }

                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                if (_airUnitsAttached.Remove(unit))
                {
                    AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {_parent.UnitName}.");
                    return true; // Successfully removed
                }
                else return false; // Unit was not found in the list
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveAirUnit", e);
                return false;
            }
        }

        /// <summary>
        /// Removes an air unit with the specified ID from the collection of attached air units.
        /// </summary>
        /// <remarks>If the specified air unit is not found in the collection, the method returns <see
        /// langword="false"/>. Any exceptions that occur during the operation are logged and handled internally, and
        /// the method will return <see langword="false"/> in such cases.</remarks>
        /// <param name="unitID">The unique identifier of the air unit to remove. Cannot be null or empty.</param>
        /// <returns><see langword="true"/> if the air unit was successfully removed; otherwise, <see langword="false"/>.</returns>
        public bool RemoveAirUnitByID(string unitID)
        {
            try
            {
                // Make sure it's the proper base type.
                if (FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot remove air units from a non-airbase facility");
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
                        AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {_parent.UnitName}.");
                        return true; // Successfully removed
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
        /// Retrieves an air unit with the specified identifier.
        /// </summary>
        /// <remarks>This method searches the collection of attached air units for a unit with a matching
        /// identifier. If an exception occurs during execution, it is handled internally, and the method returns <see
        /// langword="null"/>.</remarks>
        /// <param name="unitID">The unique identifier of the air unit to retrieve. Cannot be null or empty.</param>
        /// <returns>The <see cref="CombatUnit"/> that matches the specified identifier, or <see langword="null"/>  if no
        /// matching unit is found or if <paramref name="unitID"/> is null or empty.</returns>
        public CombatUnit GetAirUnitByID(string unitID)
        {
            try
            {
                // Make sure it's the proper base type.
                if (FacilityType != FacilityType.Airbase)
                {
                    throw new InvalidOperationException("Cannot get air units from a non-airbase facility");
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
        /// <returns>The total count of attached air units.</returns>
        public int GetAttachedUnitCount()
        {
            if (FacilityType == FacilityType.Airbase)
            {
                return _airUnitsAttached.Count;
            }
            else return 0;
        }

        /// <summary>
        /// Calculates the remaining capacity for attaching additional air units.
        /// </summary>
        /// <remarks>The returned value will be non-negative. If the number of currently attached air
        /// units equals  or exceeds the maximum allowed, the method will return 0.</remarks>
        /// <returns>The number of additional air units that can be attached. This value is the difference between  the maximum
        /// allowed air units and the number of currently attached air units.</returns>
        public int GetAirUnitCapacity()
        {
            if (FacilityType == FacilityType.Airbase)
            {
                return CUConstants.MAX_AIR_UNITS - _airUnitsAttached.Count;
            }
            else return 0;
        }

        /// <summary>
        /// Determines whether the specified combat unit is an air unit attached to this instance.
        /// </summary>
        /// <param name="unit">The combat unit to check. Must not be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the specified combat unit is an air unit attached to this instance; otherwise,
        /// <see langword="false"/>.</returns>
        public bool HasAirUnit(CombatUnit unit)
        {
            if (FacilityType == FacilityType.Airbase)
            {
                return unit != null && _airUnitsAttached.Contains(unit);
            }
            return false;
        }

        /// <summary>
        /// Check if the airbase has an air unit with the specified ID.
        /// </summary>
        /// <param name="unitID"></param>
        /// <returns></returns>
        public bool HasAirUnitByID(string unitID)
        {
            if (FacilityType == FacilityType.Airbase)
            {
                return !string.IsNullOrEmpty(unitID) && _airUnitsAttached.Any(u => u.UnitID == unitID);
            }
            return false;
        }

        /// <summary>
        /// Removes all air units from the collection.
        /// </summary>
        /// <remarks>This method clears the internal collection of air units.  If an exception occurs
        /// during the operation, it is handled and logged.</remarks>
        public void ClearAllAirUnits()
        {
            try
            {
                if (FacilityType == FacilityType.Airbase)
                {
                    _airUnitsAttached.Clear();
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearAllAirUnits", e);
            }
        }

        /// <summary>
        /// Check is airbase can launch air operations.
        /// </summary>
        /// <returns></returns>
        public bool CanLaunchAirOperations()
        {
            if (FacilityType == FacilityType.Airbase)
            {
                return OperationalCapacity != OperationalCapacity.OutOfOperation;
            }
            else return false;
        }

        /// <summary>
        /// Determines whether the aircraft can be repaired based on the current operational capacity.
        /// </summary>
        /// <returns><see langword="true"/> if the operational capacity is sufficient to allow aircraft repairs;  otherwise, <see
        /// langword="false"/>.</returns>
        public bool CanRepairAircraft()
        {
            if (FacilityType == FacilityType.Airbase)
            {
                return OperationalCapacity != OperationalCapacity.OutOfOperation;
            }
            else return false;
        }

        /// <summary>
        /// Determines whether the system can accept a new aircraft.
        /// </summary>
        /// <returns><see langword="true"/> if the system has capacity for additional aircraft; otherwise, <see
        /// langword="false"/>.</returns>
        public bool CanAcceptNewAircraft()
        {
            if (FacilityType == FacilityType.Airbase)
            {
                if (GetAirUnitCapacity() > 0) return true;
                else return false;
            }
            else return false;
        }

        /// <summary>
        /// Get the list of air units that are currently operational.
        /// </summary>
        /// <returns></returns>
        public List<CombatUnit> GetOperationalAirUnits()
        {
            try
            {
                if (FacilityType == FacilityType.Airbase)
                {
                    return _airUnitsAttached.Where(unit => unit != null && unit.EfficiencyLevel != EfficiencyLevel.StaticOperations).ToList();
                }
                else return new List<CombatUnit>();

            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetOperationalAirUnits", e);
                return new List<CombatUnit>();
            }
        }

        /// <summary>
        /// Gets the total number of operational air units.
        /// </summary>
        /// <returns>The number of air units that are currently operational.</returns>
        public int GetOperationalAirUnitCount()
        {
            if (FacilityType == FacilityType.Airbase) return GetOperationalAirUnits().Count;
            else return 0;
        }

        #endregion // Airbase Management


        #region Supply Depot Management

        /// <summary>
        /// Get the max amount of supplies the depot can hold based on its size.
        /// </summary>
        /// <returns>Days of supply</returns>
        private float GetMaxStockpile()
        {
            if (FacilityType == FacilityType.SupplyDepot) return CUConstants.MaxStockpileBySize[DepotSize];
            else return 0f;
        }

        /// <summary>
        /// The inherent generation rate that the depot can produce based on its generation rate setting.
        /// </summary>
        /// <returns></returns>
        private float GetCurrentGenerationRate()
        {
            if (FacilityType == FacilityType.SupplyDepot)
            {
                float baseRate = CUConstants.GenerationRateValues[GenerationRate];
                float efficiencyMultiplier = GetEfficiencyMultiplier();

                return baseRate * efficiencyMultiplier;
            }
            else return 0f;

        }

        /// <summary>
        /// Add supplies directly to depot.
        /// </summary>
        /// <param name="amount">Success</param>
        public bool AddSupplies(float amount)
        {
            try
            {
                if (FacilityType != FacilityType.SupplyDepot) return false;

                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                if (StockpileInDays >= GetMaxStockpile())
                {
                    // Notify the UI that the stockpile is full
                    AppService.CaptureUiMessage($"{_parent.UnitName} stockpile is already full. Cannot add more supplies.");
                    return false;
                }

                float maxCapacity = GetMaxStockpile();
                float newAmount = StockpileInDays + amount;

                // Clamp to maximum capacity
                StockpileInDays = Math.Min(newAmount, maxCapacity);

                // Notify the UI about the supply addition
                if (_parent != null)
                {
                    AppService.CaptureUiMessage($"{_parent.UnitName} has added {amount} days of supply. Current stockpile: {StockpileInDays} days.");
                }
                else
                {
                    AppService.CaptureUiMessage($"A supply depot has added {amount} days of supply. Current stockpile: {StockpileInDays} days.");
                }

                    return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "AddSupplies", e);
                return false;
            }
        }

        /// <summary>
        /// Remove supplies from the depot.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public void RemoveSupplies(float amount)
        {
            try
            {
                if (FacilityType != FacilityType.SupplyDepot) return;

                if (amount <= 0)
                {
                    throw new ArgumentException("Supply amount must be positive", nameof(amount));
                }

                float actualAmount = Math.Min(amount, StockpileInDays);
                StockpileInDays -= actualAmount;

                // Notify the UI about the supply removal
                if (_parent != null)
                {
                    AppService.CaptureUiMessage($"{_parent.UnitName} has removed {actualAmount} days of supply. Current stockpile: {StockpileInDays} days.");
                }
                else
                {
                    AppService.CaptureUiMessage($"A supply depot has removed {actualAmount} days of supply. Current stockpile: {StockpileInDays} days.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveSupplies", e);
            }
        }

        /// <summary>
        /// Generate supply is called once per turn to generate supplies based on the depot's generation rate.
        /// </summary>
        /// <returns>Success</returns>
        public bool GenerateSupplies()
        {
            try
            {
                if (FacilityType != FacilityType.SupplyDepot) return false;

                // If depot is completely out of operation, no supplies are generated
                if (!IsOperational())
                {
                    AppService.CaptureUiMessage($"{_parent.UnitName} is not operational and cannot generate supplies.");
                    return false;
                }

                float generatedAmount = GetCurrentGenerationRate();
                float maxCapacity = GetMaxStockpile();

                // Don't exceed maximum capacity
                float amountToAdd = Math.Min(generatedAmount, maxCapacity - StockpileInDays);
                StockpileInDays += amountToAdd;

                // Notify the UI about the supply generation
                AppService.CaptureUiMessage($"{_parent.UnitName} has generated {amountToAdd} days of supply. Current stockpile: {StockpileInDays} days.");

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GenerateSupplies", e);
                return false;
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
                if (FacilityType != FacilityType.SupplyDepot) return false;

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
                AppService.HandleException(CLASS_NAME, "CanSupplyUnitAt", e);
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
                if (FacilityType != FacilityType.SupplyDepot) return 0f;

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
                AppService.HandleException(CLASS_NAME, "SupplyUnit", e);
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
                if (FacilityType != FacilityType.SupplyDepot) return 0f;

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
        /// Check if the stockpile is empty.
        /// </summary>
        /// <returns></returns>
        public bool IsStockpileEmpty()
        {
            if (FacilityType == FacilityType.SupplyDepot)
            {
                return StockpileInDays <= 0f;
            }
            else return true;

        }

        /// <summary>
        /// Get the remaining supply capacity in days.
        /// </summary>
        /// <returns></returns>
        public float GetRemainingSupplyCapacity()
        {
            if (FacilityType == FacilityType.SupplyDepot)
            {
                return GetMaxStockpile() - StockpileInDays;
            }
            else return 0f;
        }

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
                if (FacilityType != FacilityType.SupplyDepot) return false;

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
                AppService.HandleException(CLASS_NAME, "UpgradeDepotSize", e);
                return false;
            }
        }

        /// <summary>
        /// The leader determines if supply penetration is enabled or not.
        /// </summary>
        /// <param name="enabled"></param>
        public void SetLeaderSupplyPenetration(bool enabled)
        {
            SupplyPenetration = enabled;
        }

        /// <summary>
        /// Set the size of the depot and sets to max stockpile.
        /// </summary>
        /// <param name="depotSize"></param>
        private void SetDepotSize(DepotSize depotSize)
        {
            try
            {
                if (FacilityType != FacilityType.SupplyDepot) return;

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
                AppService.HandleException(CLASS_NAME, "SetDepotSize", e);
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
                if (!IsOperational() || !IsMainDepot || FacilityType != FacilityType.SupplyDepot)
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
                AppService.HandleException(CLASS_NAME, "PerformAirSupply", e);
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
                if (!IsOperational() || !IsMainDepot || FacilityType != FacilityType.SupplyDepot)
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
                AppService.HandleException(CLASS_NAME, "PerformNavalSupply", e);
                return 0f;
            }
        }

        #endregion // Supply Management Methods


        #region Helpers

        /// <summary>
        /// Set the parent CombatUnit.
        /// </summary>
        /// <param name="parent"></param>
        internal void SetParent(CombatUnit parent)
        {
            try
            {
                if (parent == null)
                {
                    throw new ArgumentNullException(nameof(parent), "Parent combat unit cannot be null");
                }

                _parent = parent;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetParent", e);
                throw;
            }
        }

        /// <summary>
        /// Validates that the parent relationship is consistent.
        /// </summary>
        /// <returns>True if parent relationship is valid</returns>
        private bool ValidateParentConsistency()
        {
            if (_parent == null) return false;

            // Check if parent's FacilityManager points back to this instance
            if (_parent.FacilityManager != this)
            {
                AppService.HandleException(CLASS_NAME, "ValidateParentConsistency",
                    new InvalidOperationException($"Parent unit {_parent.UnitID} does not reference this FacilityManager"),
                    ExceptionSeverity.Minor);
                return false;
            }

            return true;
        }

        #endregion // Helpers


        #region Serialization Constructor

        /// <summary>
        /// Deserialization constructor for loading FacilityManager from saved data.
        /// </summary>
        /// <param name="info">Serialization info containing saved data</param>
        /// <param name="context">Streaming context for deserialization</param>
        internal FacilityManager(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Initialize collections first
                _airUnitsAttached = new List<CombatUnit>();
                _attachedUnitIDs = new List<string>();

                // Parent will be set by CombatUnit when ResolveReferences is called
                _parent = null;

                // Load common properties with FM_ prefix to match serialization
                BaseDamage = info.GetInt32("FM_" + nameof(BaseDamage));
                OperationalCapacity = (OperationalCapacity)info.GetValue("FM_" + nameof(OperationalCapacity), typeof(OperationalCapacity));
                FacilityType = (FacilityType)info.GetValue("FM_" + nameof(FacilityType), typeof(FacilityType));

                // Load supply depot properties with FM_ prefix
                DepotSize = (DepotSize)info.GetValue("FM_" + nameof(DepotSize), typeof(DepotSize));
                StockpileInDays = info.GetSingle("FM_" + nameof(StockpileInDays));
                GenerationRate = (SupplyGenerationRate)info.GetValue("FM_" + nameof(GenerationRate), typeof(SupplyGenerationRate));
                SupplyProjection = (SupplyProjection)info.GetValue("FM_" + nameof(SupplyProjection), typeof(SupplyProjection));
                SupplyPenetration = info.GetBoolean("FM_" + nameof(SupplyPenetration));
                DepotCategory = (DepotCategory)info.GetValue("FM_" + nameof(DepotCategory), typeof(DepotCategory));

                // Load air unit attachments for later resolution
                int airUnitCount = info.GetInt32("FM_AirUnitCount");
                for (int i = 0; i < airUnitCount; i++)
                {
                    string unitID = info.GetString($"FM_AirUnitID_{i}");
                    if (!string.IsNullOrEmpty(unitID))
                    {
                        _attachedUnitIDs.Add(unitID);
                    }
                }

                // Initialize readonly property
                AirUnitsAttached = _airUnitsAttached.AsReadOnly();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(FacilityManager), e);
                throw;
            }
        }

        #endregion // Serialization Constructor


        #region ISerializable Implementation

        /// <summary>
        /// Serializes FacilityManager data for saving to file.
        /// </summary>
        /// <param name="info">Serialization info to store data</param>
        /// <param name="context">Streaming context for serialization</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Serialize common properties with FM_ prefix to avoid naming conflicts
                info.AddValue("FM_" + nameof(BaseDamage), BaseDamage);
                info.AddValue("FM_" + nameof(OperationalCapacity), OperationalCapacity);
                info.AddValue("FM_" + nameof(FacilityType), FacilityType);

                // Serialize supply depot properties with FM_ prefix
                info.AddValue("FM_" + nameof(DepotSize), DepotSize);
                info.AddValue("FM_" + nameof(StockpileInDays), StockpileInDays);
                info.AddValue("FM_" + nameof(GenerationRate), GenerationRate);
                info.AddValue("FM_" + nameof(SupplyProjection), SupplyProjection);
                info.AddValue("FM_" + nameof(SupplyPenetration), SupplyPenetration);
                info.AddValue("FM_" + nameof(DepotCategory), DepotCategory);

                // Serialize air unit attachments as Unit IDs with FM_ prefix to avoid circular references
                info.AddValue("FM_AirUnitCount", _airUnitsAttached.Count);
                for (int i = 0; i < _airUnitsAttached.Count; i++)
                {
                    info.AddValue($"FM_AirUnitID_{i}", _airUnitsAttached[i].UnitID);
                }

                // Note: Parent CombatUnit is NOT serialized - it will be set by CombatUnit during ResolveReferences
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetObjectData), e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region IResolvableReferences Implementation

        /// <summary>
        /// Checks if there are unresolved unit references that need to be resolved.
        /// </summary>
        /// <returns>True if ResolveReferences() needs to be called</returns>
        public bool HasUnresolvedReferences()
        {
            return _attachedUnitIDs.Count > 0;
        }

        /// <summary>
        /// Gets the list of unresolved reference IDs that need to be resolved.
        /// Implements IResolvableReferences interface.
        /// </summary>
        /// <returns>Collection of object IDs that this object references</returns>
        public IReadOnlyList<string> GetUnresolvedReferenceIDs()
        {
            return _attachedUnitIDs.Select(unitID => $"FM_AirUnit:{unitID}").ToList().AsReadOnly();
        }

        /// <summary>
        /// Resolves object references using the provided data manager.
        /// Called after all objects have been deserialized.
        /// Implements IResolvableReferences interface.
        /// </summary>
        /// <param name="manager">Game data manager containing all loaded objects</param>
        public void ResolveReferences(GameDataManager manager)
        {
            try
            {
                _airUnitsAttached.Clear();

                foreach (string unitID in _attachedUnitIDs)
                {
                    var unit = manager.GetCombatUnit(unitID);
                    if (unit != null)
                    {
                        if (unit.UnitType == UnitType.AirUnit)
                        {
                            _airUnitsAttached.Add(unit);
                        }
                        else
                        {
                            // Log warning about incorrect unit type but don't throw
                            AppService.HandleException(CLASS_NAME, "ResolveReferences",
                                new InvalidOperationException($"Unit {unitID} is not an air unit (Type: {unit.UnitType})"),
                                ExceptionSeverity.Minor);
                        }
                    }
                    else
                    {
                        // Log warning about missing unit but don't throw
                        AppService.HandleException(CLASS_NAME, "ResolveReferences",
                            new KeyNotFoundException($"Unit {unitID} not found in game data manager"),
                            ExceptionSeverity.Minor);
                    }
                }

                _attachedUnitIDs.Clear(); // Clean up temporary storage
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResolveReferences", e);
            }
        }

        #endregion // IResolvableReferences Implementation


        #region Cloning Support

        /// <summary>
        /// Creates a clean template copy of this FacilityManager with no attachments.
        /// This method is specifically for cloning unit templates during scenario creation.
        /// </summary>
        /// <returns>A new FacilityManager with copied properties but no parent or attachments</returns>
        public FacilityManager Clone()
        {
            try
            {
                var clone = new FacilityManager();

                // Copy common properties
                clone.BaseDamage = this.BaseDamage;
                clone.OperationalCapacity = this.OperationalCapacity;
                clone.FacilityType = this.FacilityType;

                // Copy supply depot properties
                clone.DepotSize = this.DepotSize;
                clone.StockpileInDays = this.StockpileInDays;
                clone.GenerationRate = this.GenerationRate;
                clone.SupplyProjection = this.SupplyProjection;
                clone.SupplyPenetration = this.SupplyPenetration;
                clone.DepotCategory = this.DepotCategory;

                // NOTE: Parent and air unit attachments are NEVER cloned for templates
                // Templates should be clean and ready for fresh assignments
                // _parent will be set when facility is attached to a new CombatUnit
                // _airUnitsAttached remains empty - no air units should be attached to templates

                return clone;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CloneAsTemplate", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy configured as an airbase.
        /// Convenience method for cloning airbase facilities.
        /// </summary>
        /// <param name="parent">The parent CombatUnit for the cloned facility</param>
        /// <returns>A new FacilityManager configured as an airbase</returns>
        public static FacilityManager CloneAsAirbase(FacilityManager source, CombatUnit parent)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var clone = source.Clone();
            clone.SetupAirbase(parent);
            return clone;
        }

        /// <summary>
        /// Creates a deep copy configured as a supply depot.
        /// Convenience method for cloning supply depot facilities.
        /// </summary>
        /// <param name="parent">The parent CombatUnit for the cloned facility</param>
        /// <param name="category">Category of the depot (Main or Secondary)</param>
        /// <param name="size">Size of the depot</param>
        /// <returns>A new FacilityManager configured as a supply depot</returns>
        public static FacilityManager CloneAsSupplyDepot(FacilityManager source, CombatUnit parent, DepotCategory category, DepotSize size)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var clone = source.Clone();
            clone.SetupSupplyDepot(parent, category, size);
            return clone;
        }

        /// <summary>
        /// Creates a deep copy configured as headquarters.
        /// Convenience method for cloning HQ facilities.
        /// </summary>
        /// <param name="parent">The parent CombatUnit for the cloned facility</param>
        /// <returns>A new FacilityManager configured as headquarters</returns>
        public static FacilityManager CloneAsHQ(FacilityManager source, CombatUnit parent)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var clone = source.Clone();
            clone.SetupHQ(parent);
            return clone;
        }

        #endregion // Cloning Support
    }
}