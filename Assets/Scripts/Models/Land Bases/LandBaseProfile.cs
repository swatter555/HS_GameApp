
using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Base class for all land-based facilities including airbases and supply depots.
    /// Provides common functionality for damage tracking, repairs, and operational status.
    /// </summary>
    [Serializable]
    public class LandBaseProfile : ISerializable
    {
        #region Constants

        private const string CLASS_NAME = nameof(LandBaseProfile);
        public const int MAX_DAMAGE = 100;
        public const int MIN_DAMAGE = 0;

        #endregion // Constants

        
        #region Properties

        public int Damage { get; private set; }                               // Damage level (0-100)
        public OperationalCapacity OperationalCapacity { get; private set; }  // Operational capacity level

        #endregion // Properties


        #region Constructors

        public LandBaseProfile()
        {
            // Default constructor initializes with no damage and full operational capacity
            Damage = MIN_DAMAGE;
            OperationalCapacity = OperationalCapacity.Full;
        }

        public LandBaseProfile(int initialDamage)
        {
            try
            {
                // Initialize with specified damage
                AddDamage(initialDamage);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        // Deserialization constructor.
        protected LandBaseProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
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
                // Clamp the incoming damage to be between 1 and 100
                incomingDamage = Math.Max(1, Math.Min(MAX_DAMAGE, incomingDamage));

                // Add the incoming damage to the current damage
                Damage += incomingDamage;

                // Clamp the total damage to be between 0 and 100
                Damage = Math.Max(MIN_DAMAGE, Math.Min(MAX_DAMAGE, Damage));

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
                // Clamp the repair amount to be between 1 and 100
                repairAmount = Math.Max(1, Math.Min(MAX_DAMAGE, repairAmount));

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

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
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