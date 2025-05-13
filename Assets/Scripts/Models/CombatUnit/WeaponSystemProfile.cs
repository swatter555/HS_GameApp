using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    

    /// <summary>
    /// The WeaponSystemProfile defines the combat capabilities of a unit.
    /// It provides values for offensive and defensive capabilities against different target types,
    /// as well as movement and range parameters. Units can have different profiles when deployed
    /// or mounted on transports.
    /// </summary>
    [Serializable]
    public class WeaponSystemProfile : ISerializable, ICloneable
    {
        #region Constants

        private const string CLASS_NAME = nameof(WeaponSystemProfile);

        #endregion // Constants


        #region Properties

        public string Name { get; private set; }
        public Nationality Nationality { get; private set; }
        public WeaponSystems WeaponSystem { get; private set; }
        public List<UpgradeType> UpgradeTypes { get; private set; }
       
        // Combat ratings for land units.
        public int LandHardAttack { get; private set; }
        public int LandSoftAttack { get; private set; } 
        public int LandHardDefense { get; private set; }
        public int LandSoftDefense { get; private set; }
        public int LandAirAttack { get; private set; }
        public int LandAirDefense { get; private set; }

        // Combat rating for air units.
        public int AirAttack { get; private set; }
        public int AirDefense { get; private set; }
        public int AirAvionics { get; private set; }
        public int AirGroundAttack { get; private set; }
        public int AirGroundDefense { get; private set; }
        public int AirStrategicAttack { get; private set; }
        public float PrimaryRange { get; private set; }
        public float IndirectRange { get; private set; }
        public float SpottingRange { get; private set; }
        public float MovementModifier { get; private set; }

        // Property enums
        public AllWeatherRating AllWeatherCapability { get; private set; }
        public SIGINT_Rating SIGINT_Rating { get; private set; }
        public NBC_Rating NBC_Rating { get; private set; }
        public StrategicMobility StrategicMobility { get; private set; }
        public NVG_Rating NVGCapability { get; private set; }
        public UnitSilhouette Silhouette { get; private set; }

        #endregion // Properties


        #region Constructor


        #endregion // Constructor


        #region Public Methods

        public bool AddUpgradeType(UpgradeType upgradeType)
        {
            if (upgradeType == UpgradeType.None)
            {
                return false;
            }

            if (!UpgradeTypes.Contains(upgradeType))
            {
                UpgradeTypes.Add(upgradeType);
                return true;
            }

            return false;
        }

        public bool RemoveUpgradeType(UpgradeType upgradeType)
        {
            return UpgradeTypes.Remove(upgradeType);
        }

        public bool HasUpgradeType(UpgradeType upgradeType)
        {
            return UpgradeTypes.Contains(upgradeType);
        }

        public WeaponSystemProfile Clone()
        {
            return null; // Implement deep copy logic here
        }

        #endregion // Public Methods


        #region Private Methods

        private int ValidateCombatValue(int value)
        {
            return Mathf.Clamp(value, CUConstants.MIN_COMBAT_VALUE, CUConstants.MAX_COMBAT_VALUE);
        }

        private float ValidateRange(float value)
        {
            return Mathf.Clamp(value, CUConstants.MIN_RANGE, CUConstants.MAX_RANGE);
        }

        #endregion // Private Methods


        #region ISerializable Implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
               // TODO: Implement
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region ICloneable Implementation

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion // ICloneable Implementation
    }
}