using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    public sealed class FacilityManager
    {
        #region Constants

        private const string CLASS_NAME = nameof(LandBaseFacility);

        #endregion // Constants


        #region Fields

        // The parent CombatUnit.
        private readonly CombatUnit _parent;

        #endregion // Fields


        #region Properties

        public int BaseDamage { get; private set; }                          // Damage to the base's capabilities.
        public OperationalCapacity OperationalCapacity { get; private set; } // The level of operational capacity of the base.
        public FacilityType FacilityType { get; private set; }               // The type of base.

        #endregion // Properties


        #region Constructors


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
                AppService.CaptureUiMessage($"{_parent.UnitName} has suffered {incomingDamage} damage. Current damage level: {BaseDamage}.");
                AppService.CaptureUiMessage($"{_parent.UnitName} current operational capacity is: {OperationalCapacity}");
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
                AppService.CaptureUiMessage($"{_parent.UnitName} has been repaired by {repairAmount}. Current damage level: {BaseDamage}.");
                AppService.CaptureUiMessage($"{_parent.UnitName} current operational capacity is: {OperationalCapacity}");
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

        #endregion // Airbase Management


        #region Supply Depot Management

        #endregion // Supply Depot Management

    }
}