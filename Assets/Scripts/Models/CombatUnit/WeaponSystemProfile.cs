using HammerAndSickle.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Weapon systems in the game.
    /// </summary>
    public enum WeaponSystems
    {
        // Soviet weapon systems
        TANK_T55A,
        TANK_T64A,
        TANK_T64B,
        TANK_T72A,
        TANK_T72B,
        TANK_T80B,
        TANK_T80U,
        TANK_T80BV,
        APC_MTLB,
        APC_BTR70,
        APC_BTR80,
        IFV_BMP1,
        IFV_BMP2,
        IFV_BMP3,
        IFV_BMD2,
        IFV_BMD3,
        RCN_BRDM2,
        RCN_BRDM2AT,
        TRUCK_RED,
        SPA_2S1,
        SPA_2S3,
        SPA_2S5,
        SPA_2S19,
        ART_D20,
        ART_D30,
        ROC_BM21,
        ROC_BM27,
        ROC_BM30,
        SSM_SCUD,
        SPAAA_ZSU57,
        SPAAA_ZSU23,
        SPAAA_2K22,
        SPSAM_9K31,
        SAM_S75,
        SAM_S125,
        SAM_S300,
        HEL_MI8T,
        HEL_MI8AT,
        HEL_MI24D,
        HEL_MI24V,
        HEL_MI28,
        AWACS_A50,
        TRAN_AN8,
        ASF_MIG21,
        ASF_MIG23,
        ASF_MIG25,
        ASF_MIG29,
        ASF_MIG31,
        ASF_SU27,
        ASF_SU47,
        MRF_MIG27,
        ATT_SU25,
        ATT_SU25B,
        BMB_SU24,
        BMB_TU16,
        BMB_TU22,
        BMB_TU22M3,
        RCN_MIG25R,

        // US
        TANK_M1,
        IFV_M2,
        IFV_M3,
        APC_M113,
        APC_LVTP7,
        SPA_M109,
        ROC_MLRS,
        SPAAA_M163,
        SPSAM_CHAP,
        SAM_HAWK,
        HEL_AH64,
        ASF_F15,
        ASF_F4,
        MRF_F16,
        ATT_A10,
        BMB_F111,
        BMB_F117,
        RCN_SR71,

        // West Germany
        TANK_LEOPARD1,
        TANK_LEOPARD2,
        IFV_MARDER,
        SPAAA_GEPARD,
        HEL_BO105,
        MRF_TornadoIDS,

        // UK
        TANK_CHALLENGER1,
        IFV_WARRIOR,

        // France
        TANK_AMX30,
        SPAAA_ROLAND,
        ASF_MIRAGE2000,
        ATT_JAGUAR,

        // Types of infantry
        SV_REG_INF,
        SV_AB_INF,
        SV_AM_INF,
        SV_MAR_INF,
        SV_SPEC_INF,
        SV_ENG_INF,

        US_REG_INF,
        US_AB_INF,
        US_AM_INF,
        US_MAR_INF,
        US_SPEC_INF,
        US_ENG_INF,

        FRG_REG_INF,
        FRG_AB_INF,
        FRG_AM_INF,
        FRG_MAR_INF,
        FRG_SPEC_INF,
        FRG_ENG_INF,

        UK_REG_INF,
        UK_AB_INF,
        UK_AM_INF,
        UK_MAR_INF,
        UK_SPEC_INF,
        UK_ENG_INF,

        FRA_REG_INF,
        FRA_AB_INF,
        FRA_AM_INF,
        FRA_MAR_INF,
        FRA_SPEC_INF,
        FRA_ENG_INF,

        ARAB_REG_INF,
        ARAB_AB_INF,
        ARAB_AM_INF,
        ARAB_MAR_INF,
        ARAB_SPEC_INF,
        ARAB_ENG_INF,

        AIRBASE,
        SUPPLY_DEPOT,

        EAST_SIGINT,
        WEST_SIGINT
    }

    /// <summary>
    /// The type of upgrade a unit can receive.
    /// </summary>
    public enum UpgradeType
    {
        None,
        AFV,
        IFV,
        APC,
        RECON,
        SPART,
        ART,
        ROC,
        SSM,
        SAM,
        SPSAM,
        AAA,
        SPAAA,
        Fighter,
        Attack,
        Bomber
    }

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

        /// <summary>
        /// The maximum allowed value for any combat statistic
        /// </summary>
        public const int MAX_COMBAT_VALUE = 10;

        /// <summary>
        /// The minimum allowed value for any combat statistic
        /// </summary>
        public const int MIN_COMBAT_VALUE = 0;

        /// <summary>
        /// The maximum allowed range for any weapon system in hexes
        /// </summary>
        public const float MAX_RANGE = 25.0f;

        /// <summary>
        /// The minimum allowed range for any weapon system in hexes
        /// </summary>
        public const float MIN_RANGE = 0.0f;

        #endregion

        #region Properties

        /// <summary>
        /// The display name of this weapon system profile
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The nationality of this weapon system
        /// </summary>
        public Nationality Nationality { get; private set; }

        /// <summary>
        /// The specific weapon system this profile represents
        /// </summary>
        public WeaponSystems WeaponSystem { get; private set; }

        /// <summary>
        /// List of possible upgrade types compatible with this weapon system
        /// </summary>
        public List<UpgradeType> UpgradeTypes { get; private set; }

        /// <summary>
        /// Attack value against armored/hard targets like tanks or fortifications
        /// </summary>
        private int landHardAttack;
        public int LandHardAttack
        {
            get => landHardAttack;
            set => landHardAttack = ValidateCombatValue(value);
        }

        /// <summary>
        /// Attack value against unarmored/soft targets like infantry
        /// </summary>
        private int landSoftAttack;
        public int LandSoftAttack
        {
            get => landSoftAttack;
            set => landSoftAttack = ValidateCombatValue(value);
        }

        /// <summary>
        /// Attack value against air units from ground units
        /// </summary>
        private int landAirAttack;
        public int LandAirAttack
        {
            get => landAirAttack;
            set => landAirAttack = ValidateCombatValue(value);
        }

        /// <summary>
        /// Defense value against attacks targeting armored/hard units
        /// </summary>
        private int landHardDefense;
        public int LandHardDefense
        {
            get => landHardDefense;
            set => landHardDefense = ValidateCombatValue(value);
        }

        /// <summary>
        /// Defense value against attacks targeting unarmored/soft units
        /// </summary>
        private int landSoftDefense;
        public int LandSoftDefense
        {
            get => landSoftDefense;
            set => landSoftDefense = ValidateCombatValue(value);
        }

        /// <summary>
        /// Defense value against attacks from air units
        /// </summary>
        private int landAirDefense;
        public int LandAirDefense
        {
            get => landAirDefense;
            set => landAirDefense = ValidateCombatValue(value);
        }

        /// <summary>
        /// Air unit's attack value against other air units
        /// </summary>
        private int airAttack;
        public int AirAttack
        {
            get => airAttack;
            set => airAttack = ValidateCombatValue(value);
        }

        /// <summary>
        /// Air unit's defense value against attacks from other air units
        /// </summary>
        private int airDefense;
        public int AirDefense
        {
            get => airDefense;
            set => airDefense = ValidateCombatValue(value);
        }

        /// <summary>
        /// Represents the sophistication of an air unit's sensors and electronics
        /// </summary>
        private int airAvionics;
        public int AirAvionics
        {
            get => airAvionics;
            set => airAvionics = ValidateCombatValue(value);
        }

        /// <summary>
        /// Air unit's attack value against ground targets
        /// </summary>
        private int airGroundAttack;
        public int AirGroundAttack
        {
            get => airGroundAttack;
            set => airGroundAttack = ValidateCombatValue(value);
        }

        /// <summary>
        /// Air unit's defense value against ground-based anti-air systems
        /// </summary>
        private int airGroundDefense;
        public int AirGroundDefense
        {
            get => airGroundDefense;
            set => airGroundDefense = ValidateCombatValue(value);
        }

        /// <summary>
        /// Air unit's capability for strategic bombing missions
        /// </summary>
        private int airStrategicAttack;
        public int AirStrategicAttack
        {
            get => airStrategicAttack;
            set => airStrategicAttack = ValidateCombatValue(value);
        }

        /// <summary>
        /// The standard attack range of the weapon system in hexes
        /// </summary>
        private float primaryRange;
        public float PrimaryRange
        {
            get => primaryRange;
            set => primaryRange = ValidateRange(value);
        }

        /// <summary>
        /// The indirect fire range (for artillery, rockets, etc.) in hexes
        /// </summary>
        private float indirectRange;
        public float IndirectRange
        {
            get => indirectRange;
            set => indirectRange = ValidateRange(value);
        }

        /// <summary>
        /// How far the unit can detect enemies in hexes
        /// </summary>
        private float detectionRange;
        public float DetectionRange
        {
            get => detectionRange;
            set => detectionRange = ValidateRange(value);
        }

        /// <summary>
        /// How far the unit can provide support to friendly units in hexes
        /// </summary>
        private float supportRange;
        public float SupportRange
        {
            get => supportRange;
            set => supportRange = ValidateRange(value);
        }

        /// <summary>
        /// Modifier affecting the unit's movement points
        /// </summary>
        public float MovementModifier { get; set; }

        /// <summary>
        /// Modifier affecting the unit's zone of control in hexes
        /// </summary>
        public int ZOCModifier { get; set; }

        // Capability ratings
        public SIGINT_Rating SIGINT_Rating { get; private set; }
        public NBC_Rating NBC_Rating { get; private set; }
        public StrategicMobility StrategicMobility { get; private set; }
        public NVG_Rating NVGCapability { get; private set; }
        public AllWeatherRating AllWeatherCapability { get; private set; }
        public UnitSilhouette Silhouette { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the WeaponSystemProfile class with default values.
        /// </summary>
        /// <param name="name">The display name of this weapon system profile</param>
        /// <param name="nationality">The nationality this weapon system belongs to</param>
        /// <param name="weaponSystem">The specific weapon system this profile represents</param>
        public WeaponSystemProfile(string name, Nationality nationality, WeaponSystems weaponSystem)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            }

            Name = name;
            Nationality = nationality;
            WeaponSystem = weaponSystem;
            UpgradeTypes = new List<UpgradeType>();

            // Initialize with default values
            LandHardAttack = 0;
            LandSoftAttack = 0;
            LandAirAttack = 0;
            LandHardDefense = 0;
            LandSoftDefense = 0;
            LandAirDefense = 0;
            AirAttack = 0;
            AirDefense = 0;
            AirAvionics = 0;
            AirGroundAttack = 0;
            AirGroundDefense = 0;
            AirStrategicAttack = 0;
            PrimaryRange = 1.0f;
            IndirectRange = 0.0f;
            DetectionRange = 1.0f;
            SupportRange = 0.0f;
            MovementModifier = 1.0f;
            ZOCModifier = 0;
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected WeaponSystemProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                Name = info.GetString(nameof(Name));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                WeaponSystem = (WeaponSystems)info.GetValue(nameof(WeaponSystem), typeof(WeaponSystems));

                // Combat values
                landHardAttack = info.GetInt32(nameof(landHardAttack));
                landSoftAttack = info.GetInt32(nameof(landSoftAttack));
                landAirAttack = info.GetInt32(nameof(landAirAttack));
                landHardDefense = info.GetInt32(nameof(landHardDefense));
                landSoftDefense = info.GetInt32(nameof(landSoftDefense));
                landAirDefense = info.GetInt32(nameof(landAirDefense));
                airAttack = info.GetInt32(nameof(airAttack));
                airDefense = info.GetInt32(nameof(airDefense));
                airAvionics = info.GetInt32(nameof(airAvionics));
                airGroundAttack = info.GetInt32(nameof(airGroundAttack));
                airGroundDefense = info.GetInt32(nameof(airGroundDefense));
                airStrategicAttack = info.GetInt32(nameof(airStrategicAttack));

                // Range values
                primaryRange = info.GetSingle(nameof(primaryRange));
                indirectRange = info.GetSingle(nameof(indirectRange));
                detectionRange = info.GetSingle(nameof(detectionRange));
                supportRange = info.GetSingle(nameof(supportRange));

                // Modifiers
                MovementModifier = info.GetSingle(nameof(MovementModifier));
                ZOCModifier = info.GetInt32(nameof(ZOCModifier));

                // Deserialize UpgradeTypes
                int upgradeTypesCount = info.GetInt32("UpgradeTypesCount");
                UpgradeTypes = new List<UpgradeType>(upgradeTypesCount);

                for (int i = 0; i < upgradeTypesCount; i++)
                {
                    UpgradeType upgradeType = (UpgradeType)info.GetValue($"UpgradeType_{i}", typeof(UpgradeType));
                    UpgradeTypes.Add(upgradeType);
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an upgrade type to this weapon system profile if it doesn't already exist.
        /// </summary>
        /// <param name="upgradeType">The upgrade type to add</param>
        /// <returns>True if the upgrade type was added, false if it already existed</returns>
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

        /// <summary>
        /// Removes an upgrade type from this weapon system profile if it exists.
        /// </summary>
        /// <param name="upgradeType">The upgrade type to remove</param>
        /// <returns>True if the upgrade type was removed, false if it didn't exist</returns>
        public bool RemoveUpgradeType(UpgradeType upgradeType)
        {
            return UpgradeTypes.Remove(upgradeType);
        }

        /// <summary>
        /// Checks if this weapon system profile supports a specific upgrade type.
        /// </summary>
        /// <param name="upgradeType">The upgrade type to check</param>
        /// <returns>True if the upgrade type is supported, false otherwise</returns>
        public bool HasUpgradeType(UpgradeType upgradeType)
        {
            return UpgradeTypes.Contains(upgradeType);
        }

        /// <summary>
        /// Creates a deep copy of this weapon system profile.
        /// </summary>
        /// <returns>A new WeaponSystemProfile instance with the same values</returns>
        public WeaponSystemProfile Clone()
        {
            var clone = new WeaponSystemProfile(Name, Nationality, WeaponSystem)
            {
                // Copy all combat values
                LandHardAttack = LandHardAttack,
                LandSoftAttack = LandSoftAttack,
                LandAirAttack = LandAirAttack,
                LandHardDefense = LandHardDefense,
                LandSoftDefense = LandSoftDefense,
                LandAirDefense = LandAirDefense,
                AirAttack = AirAttack,
                AirDefense = AirDefense,
                AirAvionics = AirAvionics,
                AirGroundAttack = AirGroundAttack,
                AirGroundDefense = AirGroundDefense,
                AirStrategicAttack = AirStrategicAttack,

                // Copy all range values
                PrimaryRange = PrimaryRange,
                IndirectRange = IndirectRange,
                DetectionRange = DetectionRange,
                SupportRange = SupportRange,

                // Copy modifiers
                MovementModifier = MovementModifier,
                ZOCModifier = ZOCModifier
            };

            // Copy upgrade types
            foreach (var upgradeType in UpgradeTypes)
            {
                clone.AddUpgradeType(upgradeType);
            }

            return clone;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates a combat value to ensure it remains within the allowed range.
        /// </summary>
        /// <param name="value">The combat value to validate</param>
        /// <returns>The validated combat value, clamped if necessary</returns>
        private int ValidateCombatValue(int value)
        {
            return Mathf.Clamp(value, MIN_COMBAT_VALUE, MAX_COMBAT_VALUE);
        }

        /// <summary>
        /// Validates a range value to ensure it remains within the allowed range.
        /// </summary>
        /// <param name="value">The range value to validate</param>
        /// <returns>The validated range value, clamped if necessary</returns>
        private float ValidateRange(float value)
        {
            return Mathf.Clamp(value, MIN_RANGE, MAX_RANGE);
        }

        #endregion

        #region ISerializable Implementation

        /// <summary>
        /// Serializes this WeaponSystemProfile instance.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(WeaponSystem), WeaponSystem);

                // Combat values
                info.AddValue(nameof(landHardAttack), landHardAttack);
                info.AddValue(nameof(landSoftAttack), landSoftAttack);
                info.AddValue(nameof(landAirAttack), landAirAttack);
                info.AddValue(nameof(landHardDefense), landHardDefense);
                info.AddValue(nameof(landSoftDefense), landSoftDefense);
                info.AddValue(nameof(landAirDefense), landAirDefense);
                info.AddValue(nameof(airAttack), airAttack);
                info.AddValue(nameof(airDefense), airDefense);
                info.AddValue(nameof(airAvionics), airAvionics);
                info.AddValue(nameof(airGroundAttack), airGroundAttack);
                info.AddValue(nameof(airGroundDefense), airGroundDefense);
                info.AddValue(nameof(airStrategicAttack), airStrategicAttack);

                // Range values
                info.AddValue(nameof(primaryRange), primaryRange);
                info.AddValue(nameof(indirectRange), indirectRange);
                info.AddValue(nameof(detectionRange), detectionRange);
                info.AddValue(nameof(supportRange), supportRange);

                // Modifiers
                info.AddValue(nameof(MovementModifier), MovementModifier);
                info.AddValue(nameof(ZOCModifier), ZOCModifier);

                // Serialize UpgradeTypes
                info.AddValue("UpgradeTypesCount", UpgradeTypes.Count);

                for (int i = 0; i < UpgradeTypes.Count; i++)
                {
                    info.AddValue($"UpgradeType_{i}", UpgradeTypes[i]);
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion

        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this weapon system profile.
        /// </summary>
        /// <returns>A new WeaponSystemProfile instance with the same values</returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }
}