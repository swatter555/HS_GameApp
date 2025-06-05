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
                AppService.Instance.HandleException(CLASS_NAME, "DefaultConstructor", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "DamageConstructor", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "FullConstructor", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Air Unit Management

        public void AddAirUnit(CombatUnit unit)
        {
            try
            {
                // Check for null
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                // Check if unit is already assigned to another base
                if (unit.LandBaseFacility != null && unit.LandBaseFacility != this)
                {
                    throw new InvalidOperationException($"Unit {unit.UnitName} is already assigned to base {unit.LandBaseFacility.BaseName}");
                }

                // Make sure we don't add more than the maximum allowed
                if (airUnitsAttached.Count >= MaxBaseAirUnits)
                {
                    throw new InvalidOperationException($"Airbase is at maximum capacity ({MaxBaseAirUnits} units)");
                }

                // Make sure it's an aircraft unit
                if (unit.UnitType != UnitType.AirUnit)
                {
                    throw new InvalidOperationException($"Only air units can be attached to an airbase. Unit type: {unit.UnitType}");
                }

                // Check for duplicates
                if (airUnitsAttached.Contains(unit))
                {
                    throw new InvalidOperationException($"Unit {unit.UnitName} is already attached to this airbase");
                }

                // Add the unit to the airbase
                airUnitsAttached.Add(unit);

                // Update the unit's facility reference (assuming this property exists or will be added)
                // This would require adding LandBaseFacility property to CombatUnit
                // unit.SetLandBaseFacility(this);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AddAirUnit", e);
                throw;
            }
        }

        public bool RemoveAirUnit(CombatUnit unit)
        {
            try
            {
                if (unit == null)
                {
                    throw new ArgumentNullException(nameof(unit), "Air unit cannot be null");
                }

                return airUnitsAttached.Remove(unit);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RemoveAirUnit", e);
                return false;
            }
        }

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
                    return airUnitsAttached.Remove(unit);
                }

                return false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RemoveAirUnitByID", e);
                return false;
            }
        }

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
                AppService.Instance.HandleException(CLASS_NAME, "GetAirUnitByID", e);
                return null;
            }
        }

        public int GetAttachedUnitCount()
        {
            return airUnitsAttached.Count;
        }

        public bool HasCapacityForMoreUnits()
        {
            return airUnitsAttached.Count < MaxBaseAirUnits;
        }

        public int GetRemainingCapacity()
        {
            return MaxBaseAirUnits - airUnitsAttached.Count;
        }

        public bool HasAirUnit(CombatUnit unit)
        {
            return unit != null && airUnitsAttached.Contains(unit);
        }

        public bool HasAirUnitByID(string unitID)
        {
            return !string.IsNullOrEmpty(unitID) && airUnitsAttached.Any(u => u.UnitID == unitID);
        }

        public void ClearAllAirUnits()
        {
            try
            {
                airUnitsAttached.Clear();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ClearAllAirUnits", e);
            }
        }

        #endregion // Air Unit Management


        #region Operational Methods

        public override float GetEfficiencyMultiplier()
        {
            // Air operations can be more sensitive to damage than other facilities
            return OperationalCapacity switch
            {
                OperationalCapacity.Full => CUConstants.AIRBASE_CAPACITY_LVL5,
                OperationalCapacity.SlightlyDegraded => CUConstants.AIRBASE_CAPACITY_LVL4,// Slightly worse than base class
                OperationalCapacity.ModeratelyDegraded => CUConstants.AIRBASE_CAPACITY_LVL3,// Slightly worse than base class
                OperationalCapacity.HeavilyDegraded => CUConstants.AIRBASE_CAPACITY_LVL2,// Slightly worse than base class
                OperationalCapacity.OutOfOperation => CUConstants.AIRBASE_CAPACITY_LVL1,
                _ => 0.0f,
            };
        }

        public bool CanLaunchAirOperations()
        {
            // Air operations require at least moderately degraded status
            return OperationalCapacity != OperationalCapacity.HeavilyDegraded &&
                   OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        public bool CanReceiveAircraft()
        {
            // Even heavily damaged airbases can receive aircraft in an emergency
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        public bool CanRefuelAndRearm()
        {
            // Refuel and rearm requires better operational status
            return OperationalCapacity == OperationalCapacity.Full ||
                   OperationalCapacity == OperationalCapacity.SlightlyDegraded;
        }

        public bool CanRepairAircraft()
        {
            // Aircraft repairs require full operational capacity
            return OperationalCapacity == OperationalCapacity.Full;
        }

        public bool CanAcceptNewAircraft()
        {
            return CanReceiveAircraft() && HasCapacityForMoreUnits();
        }

        public List<CombatUnit> GetOperationalAirUnits()
        {
            try
            {
                return airUnitsAttached.Where(unit => unit != null && unit.EfficiencyLevel != EfficiencyLevel.StaticOperations).ToList();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetOperationalAirUnits", e);
                return new List<CombatUnit>();
            }
        }

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
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
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
                            AppService.Instance.HandleException(CLASS_NAME, "ResolveUnitReferences",
                                new InvalidOperationException($"Unit {unitID} is not an air unit (Type: {unit.UnitType})"));
                        }
                    }
                    else
                    {
                        // Log warning about missing unit but don't throw
                        AppService.Instance.HandleException(CLASS_NAME, "ResolveUnitReferences",
                            new KeyNotFoundException($"Unit {unitID} not found in lookup dictionary"));
                    }
                }

                attachedUnitIDs.Clear(); // Clean up temporary storage
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "ResolveUnitReferences", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation
    }
}