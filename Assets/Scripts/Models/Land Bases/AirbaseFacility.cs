using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents an airbase facility that can host and support air units.
    /// Inherits base damage and operational capacity functionality from LandBaseFacility.
    /// Manages air unit attachment, capacity tracking, and operational capabilities based on damage levels.
    /// Supports serialization with reference resolution pattern for attached air units.
    /// </summary>
    [Serializable]
    public class AirbaseFacility : LandBaseFacility, ISerializable
    {
        #region Constants
        private const string CLASS_NAME = nameof(AirbaseFacility);

        public const int MaxBaseAirUnits = 4;

        #endregion // Constants


        #region Fields

        private List<CombatUnit> airUnitsAttached = new();
        private List<string> attachedUnitIDs = new();           // Store IDs during deserialization

        #endregion // Fields


        #region Properties

        public IReadOnlyList<CombatUnit> AirUnitsAttached => airUnitsAttached;

        #endregion // Properties


        #region Constructors

        public AirbaseFacility() : base()
        {
            try
            {
                // Use base class default constructor, then set airbase-specific name
                SetBaseName("Airbase");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DefaultConstructor", e);
                throw;
            }
        }

        public AirbaseFacility(int initialDamage) : base(initialDamage)
        {
            try
            {
                // Use base class constructor with damage, then set airbase-specific name
                SetBaseName("Airbase");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DamageConstructor", e);
                throw;
            }
        }

        public AirbaseFacility(string name, Side side, int initialDamage = 0) : base(name, side, initialDamage)
        {
            try
            {
                // Use full base constructor - name and side already set
                // Just ensure we have a reasonable default name if none provided
                if (string.IsNullOrEmpty(name))
                {
                    SetBaseName("Airbase");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "FullConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        protected AirbaseFacility(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            try
            {
                // Deserialize airbase-specific fields
                int airUnitCount = info.GetInt32("AirUnitCount");

                // Clear the current lists
                airUnitsAttached.Clear();
                attachedUnitIDs.Clear();

                // Store Unit IDs for later resolution by game state manager
                for (int i = 0; i < airUnitCount; i++)
                {
                    string unitID = info.GetString($"AirUnitID_{i}");
                    attachedUnitIDs.Add(unitID);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Air Unit Management

        /// <summary>
        /// Add an air unit to the airbase.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool AddAirUnit(CombatUnit unit)
        {
            try
            {
                // Check for null
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                // Make sure we don't add more than the maximum allowed
                if (airUnitsAttached.Count >= MaxBaseAirUnits)
                {
                    AppService.CaptureUiMessage($"{this.BaseName} is already full.");
                    return false; // Cannot add unit if capacity is full
                }

                // Make sure it's an aircraft unit
                if (unit.UnitType != UnitType.AirUnit)
                {
                    throw new InvalidOperationException($"Only air units can be attached to an airbase. Unit type: {unit.UnitType}");
                }

                // Check for duplicates
                if (airUnitsAttached.Contains(unit))
                {
                    AppService.CaptureUiMessage($"Unit {unit.UnitName} is already attached to this airbase");
                    return false; // Unit is already attached
                }

                // Add the unit to the airbase
                airUnitsAttached.Add(unit);

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
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                if (airUnitsAttached.Remove(unit))
                {
                    AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {BaseName}.");
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
                if (string.IsNullOrEmpty(unitID))
                {
                    throw new ArgumentException("Unit ID cannot be null or empty", nameof(unitID));
                }

                var unit = airUnitsAttached.FirstOrDefault(u => u.UnitID == unitID);
                if (unit != null)
                {
                   if (airUnitsAttached.Remove(unit))
                   {
                        AppService.CaptureUiMessage($"Unit {unit.UnitName} has been removed from {BaseName}.");
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
                if (string.IsNullOrEmpty(unitID))
                {
                    return null;
                }

                return airUnitsAttached.FirstOrDefault(u => u.UnitID == unitID);
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
            return airUnitsAttached.Count;
        }

        /// <summary>
        /// Determines whether the base has capacity to accommodate additional air units.
        /// </summary>
        /// <returns><see langword="true"/> if the number of attached air units is less than the maximum allowed;  otherwise,
        /// <see langword="false"/>.</returns>
        public bool HasCapacityForMoreUnits()
        {
            return airUnitsAttached.Count < MaxBaseAirUnits;
        }

        /// <summary>
        /// Calculates the remaining capacity for attaching additional air units.
        /// </summary>
        /// <remarks>The returned value will be non-negative. If the number of currently attached air
        /// units equals  or exceeds the maximum allowed, the method will return 0.</remarks>
        /// <returns>The number of additional air units that can be attached. This value is the difference between  the maximum
        /// allowed air units and the number of currently attached air units.</returns>
        public int GetRemainingCapacity()
        {
            return MaxBaseAirUnits - airUnitsAttached.Count;
        }

        /// <summary>
        /// Determines whether the specified combat unit is an air unit attached to this instance.
        /// </summary>
        /// <param name="unit">The combat unit to check. Must not be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the specified combat unit is an air unit attached to this instance; otherwise,
        /// <see langword="false"/>.</returns>
        public bool HasAirUnit(CombatUnit unit)
        {
            return unit != null && airUnitsAttached.Contains(unit);
        }

        /// <summary>
        /// Check if the airbase has an air unit with the specified ID.
        /// </summary>
        /// <param name="unitID"></param>
        /// <returns></returns>
        public bool HasAirUnitByID(string unitID)
        {
            return !string.IsNullOrEmpty(unitID) && airUnitsAttached.Any(u => u.UnitID == unitID);
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
                airUnitsAttached.Clear();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ClearAllAirUnits", e);
            }
        }

        #endregion // Air Unit Management


        #region Operational Methods

        /// <summary>
        /// Check is airbase can launch air operations.
        /// </summary>
        /// <returns></returns>
        public bool CanLaunchAirOperations()
        {
            // No air operations if the base is out of operation.
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Determines whether the aircraft can be repaired based on the current operational capacity.
        /// </summary>
        /// <returns><see langword="true"/> if the operational capacity is sufficient to allow aircraft repairs;  otherwise, <see
        /// langword="false"/>.</returns>
        public bool CanRepairAircraft()
        {
            // Aircraft repairs require full operational capacity
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        /// <summary>
        /// Determines whether the system can accept a new aircraft.
        /// </summary>
        /// <returns><see langword="true"/> if the system has capacity for additional aircraft; otherwise, <see
        /// langword="false"/>.</returns>
        public bool CanAcceptNewAircraft()
        {
            return HasCapacityForMoreUnits();
        }

        /// <summary>
        /// Get the list of air units that are currently operational.
        /// </summary>
        /// <returns></returns>
        public List<CombatUnit> GetOperationalAirUnits()
        {
            try
            {
                return airUnitsAttached.Where(unit => unit != null && unit.EfficiencyLevel != EfficiencyLevel.StaticOperations).ToList();
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
            return GetOperationalAirUnits().Count;
        }

        #endregion // Operational Methods


        #region Cloning

        public override LandBaseFacility Clone()
        {
            try
            {
                var clone = new AirbaseFacility();

                // Copy base class properties (BaseID will be new)
                clone.SetBaseName(this.BaseName);
                clone.SetSide(this.Side);
                clone.SetDamage(this.Damage);

                // Note: Air units are NOT cloned - the clone starts with no attached units
                // This is intentional as air units should be managed separately
                // If you need to clone with units, use a different method

                return clone;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        public AirbaseFacility CloneTyped()
        {
            return (AirbaseFacility)Clone();
        }

        #endregion // Cloning


        #region Serialization Support Methods

        /// <summary>
        /// Gets the list of attached unit IDs for external reference resolution.
        /// Used by game state manager to reconnect units after deserialization.
        /// </summary>
        public IReadOnlyList<string> GetAttachedUnitIDs()
        {
            return airUnitsAttached.Select(u => u.UnitID).ToList();
        }

        /// <summary>
        /// Gets the list of unresolved unit IDs from deserialization.
        /// Used to check if unit references need to be resolved.
        /// </summary>
        public IReadOnlyList<string> GetUnresolvedUnitIDs()
        {
            return attachedUnitIDs.AsReadOnly();
        }

        /// <summary>
        /// Resolves unit references after deserialization.
        /// Called by game state manager with reconstructed unit objects.
        /// </summary>
        /// <param name="unitLookup">Dictionary mapping unit IDs to CombatUnit instances</param>
        public void ResolveUnitReferences(Dictionary<string, CombatUnit> unitLookup)
        {
            try
            {
                airUnitsAttached.Clear();

                foreach (string unitID in attachedUnitIDs)
                {
                    if (unitLookup.TryGetValue(unitID, out CombatUnit unit))
                    {
                        if (unit.UnitType == UnitType.AirUnit)
                        {
                            airUnitsAttached.Add(unit);
                        }
                        else
                        {
                            // Log warning about incorrect unit type but don't throw
                            AppService.HandleException(CLASS_NAME, "ResolveUnitReferences",
                                new InvalidOperationException($"Unit {unitID} is not an air unit (Type: {unit.UnitType})"));
                        }
                    }
                    else
                    {
                        // Log warning about missing unit but don't throw
                        AppService.HandleException(CLASS_NAME, "ResolveUnitReferences",
                            new KeyNotFoundException($"Unit {unitID} not found in lookup dictionary"));
                    }
                }

                attachedUnitIDs.Clear(); // Clean up temporary storage
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "ResolveUnitReferences", e);
            }
        }

        /// <summary>
        /// Checks if there are unresolved unit references that need to be resolved.
        /// </summary>
        /// <returns>True if ResolveUnitReferences() needs to be called</returns>
        public bool HasUnresolvedReferences()
        {
            return attachedUnitIDs.Count > 0;
        }

        #endregion // Serialization Support Methods


        #region ISerializable Implementation

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with the data needed to serialize the current object.
        /// </summary>
        /// <remarks>This method serializes the current object by adding relevant data to the <paramref
        /// name="info"/> parameter. It includes the count of attached air units and their unique identifiers.  The
        /// serialization process avoids circular references by storing only the unit IDs.</remarks>
        /// <param name="info">The <see cref="SerializationInfo"/> object to populate with serialization data. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="context">The <see cref="StreamingContext"/> structure that contains the source and destination of the serialized
        /// stream.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Call base class serialization first
                base.GetObjectData(info, context);

                // Store the number of air units
                info.AddValue("AirUnitCount", airUnitsAttached.Count);

                // Store only Unit IDs to avoid circular references
                for (int i = 0; i < airUnitsAttached.Count; i++)
                {
                    info.AddValue($"AirUnitID_{i}", airUnitsAttached[i].UnitID);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation
    }
}