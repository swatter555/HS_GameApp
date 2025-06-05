using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Base class for all land-based facilities including airbases and supply depots.
    /// Provides common functionality for damage tracking, repairs, operational status, and facility meta-data.
    /// Each facility has a unique identifier, name, side affiliation, and comprehensive damage management system.
    /// </summary>
    [Serializable]
    public class LandBaseFacility : ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(LandBaseFacility);
        public const int MAX_DAMAGE = 100;
        public const int MIN_DAMAGE = 0;

        #endregion // Constants


        #region Properties

        public string BaseID { get; private set; }                        // Unique identifier for the facility
        public string BaseName { get; private set; }                      // UnitProfileID of the facility
        public Side Side { get; private set; }                            // Side the facility belongs to
        public int Damage { get; private set; }                          // Damage level (0-100)
        public OperationalCapacity OperationalCapacity { get; private set; } // Operational capacity level

        #endregion // Properties


        #region Constructors

        public LandBaseFacility()
        {
            try
            {
                // Default constructor initializes with no damage and full operational capacity
                BaseID = Guid.NewGuid().ToString();
                BaseName = "Land Base";
                Side = Side.Player;
                Damage = MIN_DAMAGE;
                OperationalCapacity = OperationalCapacity.Full;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DefaultConstructor", e);
                throw;
            }
        }

        public LandBaseFacility(int initialDamage)
        {
            try
            {
                // Initialize with specified damage
                BaseID = Guid.NewGuid().ToString();
                BaseName = "Land Base";
                Side = Side.Player;
                AddDamage(initialDamage);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        public LandBaseFacility(string name, Side side, int initialDamage = 0)
        {
            try
            {
                // Initialize with name, side, and optional damage
                BaseID = Guid.NewGuid().ToString();
                BaseName = string.IsNullOrEmpty(name) ? "Land Base" : name;
                Side = side;
                Damage = MIN_DAMAGE;
                OperationalCapacity = OperationalCapacity.Full;

                if (initialDamage > 0)
                {
                    AddDamage(initialDamage);
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "FullConstructor", e);
                throw;
            }
        }

        // Deserialization constructor.
        protected LandBaseFacility(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Retrieve meta information
                BaseID = info.GetString(nameof(BaseID));
                BaseName = info.GetString(nameof(BaseName));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));

                // Retrieve base fields
                Damage = info.GetInt32(nameof(Damage));
                OperationalCapacity = (OperationalCapacity)info.GetValue(nameof(OperationalCapacity), typeof(OperationalCapacity));
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Public Methods

        public virtual void AddDamage(int incomingDamage)
        {
            try
            {
                // Validate incoming damage
                if (incomingDamage < 0)
                {
                    throw new ArgumentException("Incoming damage cannot be negative", nameof(incomingDamage));
                }

                // Add the incoming damage to the current damage, then clamp the result
                int newDamage = Damage + incomingDamage;
                Damage = Math.Max(MIN_DAMAGE, Math.Min(MAX_DAMAGE, newDamage));

                // Update operational capacity based on the new damage level
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AddDamage", e);
                throw;
            }
        }

        public virtual void RepairDamage(int repairAmount)
        {
            try
            {
                // Validate repair amount
                if (repairAmount < 0)
                {
                    throw new ArgumentException("Repair amount cannot be negative", nameof(repairAmount));
                }

                // Clamp the repair amount to be between 0 and 100
                repairAmount = Math.Max(0, Math.Min(MAX_DAMAGE, repairAmount));

                // Remove the repair amount from the current damage
                Damage -= repairAmount;

                // Clamp the total damage to be between 0 and 100
                Damage = Math.Max(MIN_DAMAGE, Math.Min(MAX_DAMAGE, Damage));

                // Update operational capacity based on the new damage level
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RepairDamage", e);
                throw;
            }
        }

        public virtual void SetDamage(int newDamageLevel)
        {
            try
            {
                // Validate damage level
                if (newDamageLevel < MIN_DAMAGE || newDamageLevel > MAX_DAMAGE)
                {
                    throw new ArgumentOutOfRangeException(nameof(newDamageLevel),
                        $"Damage level must be between {MIN_DAMAGE} and {MAX_DAMAGE}");
                }

                Damage = newDamageLevel;
                UpdateOperationalCapacity();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetDamage", e);
                throw;
            }
        }

        public virtual float GetEfficiencyMultiplier()
        {
            return OperationalCapacity switch
            {
                OperationalCapacity.Full => 1.0f,
                OperationalCapacity.SlightlyDegraded => 0.75f,
                OperationalCapacity.ModeratelyDegraded => 0.5f,
                OperationalCapacity.HeavilyDegraded => 0.25f,
                OperationalCapacity.OutOfOperation => 0.0f,
                _ => 0.0f,
            };
        }

        public bool IsOperational()
        {
            return OperationalCapacity != OperationalCapacity.OutOfOperation;
        }

        public bool IsFullyOperational()
        {
            return OperationalCapacity == OperationalCapacity.Full;
        }

        public void SetBaseName(string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(newName))
                {
                    throw new ArgumentException("Base name cannot be null or empty", nameof(newName));
                }

                BaseName = newName;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetBaseName", e);
                throw;
            }
        }

        public void SetSide(Side newSide)
        {
            try
            {
                Side = newSide;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetSide", e);
                throw;
            }
        }

        public virtual LandBaseFacility Clone()
        {
            try
            {
                var clone = new LandBaseFacility();

                // Copy meta information (except BaseID which is generated new)
                clone.BaseName = this.BaseName;
                clone.Side = this.Side;

                // Copy damage state
                clone.Damage = this.Damage;
                clone.OperationalCapacity = this.OperationalCapacity;

                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion // Public Methods


        #region Protected Methods

        protected virtual void UpdateOperationalCapacity()
        {
            if (Damage >= 81 && Damage <= 100)
            {
                OperationalCapacity = OperationalCapacity.OutOfOperation;
            }
            else if (Damage >= 61 && Damage <= 80)
            {
                OperationalCapacity = OperationalCapacity.HeavilyDegraded;
            }
            else if (Damage >= 41 && Damage <= 60)
            {
                OperationalCapacity = OperationalCapacity.ModeratelyDegraded;
            }
            else if (Damage >= 21 && Damage <= 40)
            {
                OperationalCapacity = OperationalCapacity.SlightlyDegraded;
            }
            else
            {
                OperationalCapacity = OperationalCapacity.Full;
            }
        }

        #endregion // Protected Methods


        #region ISerializable Implementation

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with the data needed to serialize the current object.
        /// </summary>
        /// <remarks>This method serializes the current object by adding relevant meta-data, damage state, and 
        /// operational capacity information to the <paramref name="info"/> parameter.</remarks>
        /// <param name="info">The <see cref="SerializationInfo"/> object to populate with serialization data. Cannot be <see langword="null"/>.</param>
        /// <param name="context">The <see cref="StreamingContext"/> structure that contains the source and destination of the serialized stream.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Store meta information
                info.AddValue(nameof(BaseID), BaseID);
                info.AddValue(nameof(BaseName), BaseName);
                info.AddValue(nameof(Side), Side);

                // Store base fields
                info.AddValue(nameof(Damage), Damage);
                info.AddValue(nameof(OperationalCapacity), OperationalCapacity);
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