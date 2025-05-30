using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents an airbase facility that can host and support air units.
    /// Inherits base damage and operational capacity functionality from LandBaseProfile.
    /// </summary>
    [Serializable]
    public class AirbaseSubProfile : LandBaseProfile, ISerializable
    {
        #region Constants
        private const string CLASS_NAME = nameof(AirbaseSubProfile);

        public const int MaxBaseAirUnits = 4;

        #endregion // Constants


        #region Fields

        private List<CombatUnit> airUnitsAttached = new List<CombatUnit>();
        private List<string> attachedUnitIDs = new List<string>();           // Store IDs during deserialization

        #endregion // Fields


        #region Properties

        public IReadOnlyList<CombatUnit> AirUnitsAttached => airUnitsAttached;

        #endregion // Properties


        #region Constructors

        public AirbaseSubProfile() : base()
        {
            // Default constructor uses base class initialization
        }


        public AirbaseSubProfile(int initialDamage) : base(initialDamage)
        {
            // Uses base class initialization with initial damage
        }

        /// <summary>
        /// Deserialization constructor - SAFER VERSION
        /// </summary>
        protected AirbaseSubProfile(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            try
            {
                // Deserialize airbase-specific fields
                int airUnitCount = info.GetInt32("AirUnitCount");

                // Clear the current list
                airUnitsAttached.Clear();

                // Instead of deserializing full CombatUnit objects (circular reference risk),
                // store only the Unit IDs and reconstruct references later
                for (int i = 0; i < airUnitCount; i++)
                {
                    // OPTION 1: Store only Unit IDs (recommended)
                    string unitID = info.GetString($"AirUnitID_{i}");
                    // Store IDs for later resolution by game state manager
                    // airUnitsAttached will be populated by external system

                    // OPTION 2: Full object serialization (risky)
                    // CombatUnit unit = (CombatUnit)info.GetValue($"AirUnit_{i}", typeof(CombatUnit));
                    // if (unit != null)
                    // {
                    //     airUnitsAttached.Add(unit);
                    // }
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

                // Add the unit to the airbase
                airUnitsAttached.Add(unit);
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
                throw;
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

        #endregion // Air Unit Management


        #region Operational Methods

        public override float GetEfficiencyMultiplier()
        {
            // Air operations can be more sensitive to damage than other facilities
            switch (OperationalCapacity)
            {
                case OperationalCapacity.Full:
                    return 1.0f;
                case OperationalCapacity.SlightlyDegraded:
                    return 0.7f;  // Slightly worse than base class
                case OperationalCapacity.ModeratelyDegraded:
                    return 0.4f;  // Slightly worse than base class
                case OperationalCapacity.HeavilyDegraded:
                    return 0.2f;  // Slightly worse than base class
                case OperationalCapacity.OutOfOperation:
                    return 0.0f;
                default:
                    return 0.0f;
            }
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

        #endregion // Operational Methods


        #region ISerializable Implementation

        /// <summary>
        /// Gets the list of attached unit IDs for external reference resolution.
        /// Used by game state manager to reconnect units after deserialization.
        /// </summary>
        public IReadOnlyList<string> GetAttachedUnitIDs()
        {
            return airUnitsAttached.Select(u => u.UnitID).ToList();
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

                // OPTION 1: Store only Unit IDs (recommended to avoid circular references)
                for (int i = 0; i < airUnitsAttached.Count; i++)
                {
                    info.AddValue($"AirUnitID_{i}", airUnitsAttached[i].UnitID);
                }

                // OPTION 2: Store full objects (may cause circular reference issues)
                // for (int i = 0; i < airUnitsAttached.Count; i++)
                // {
                //     info.AddValue($"AirUnit_{i}", airUnitsAttached[i]);
                // }
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