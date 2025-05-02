using System.Collections;
using System.Collections.Generic;

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
        REG_INF,
        ENG_INF,
        SF_INF
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
    ///  The WeaponSystemProfile is used to give a CombatUnit it's combat values. CombatUnits will
    ///  have a DeployedProfile and a MountedProfile.Typically, an infanty or artillery based unit
    ///  will have a mounted and a deployed profile. The deployed profile will be used for as base 
    ///  unit that cannot change. However, the mounted profile can be upgraded from null to a 
    ///  to something on it's upgrade list.When a unit is mounted, combat values are derived entirely
    ///  from the mounted profile.
    /// </summary>
    public class WeaponSystemProfile
    {
        public string Name { get; private set; }
        public Nationality Nationality { get; private set; }
        public WeaponSystems WeaponSystem { get; private set; }
        public List<UpgradeType> UpgradeTypes { get; private set; }

        // Values for land combat.
        public int LandHardAttack { get; set; }
        public int LandSoftAttack { get; set; }
        public int LandAirAttack { get; set; }
        public int LandHardDefense { get; set; }
        public int LandSoftDefense { get; set; }
        public int LandAirDefense { get; set; }

        // Values for air combat.
        public int AirAttack { get; set; }
        public int AirDefense { get; set; }
        public int AirAvionics { get; set; }
        public int AirGroundAttack { get; set; }
        public int AirGroundDefense { get; set; }
        public int AirStrategicAttack { get; set; }

        // Values for range.
        public float PrimaryRange { get; set; }
        public float IndirectRange { get; set; }
        public float DetectionRange { get; set; }
        public float SupportRange { get; set; }

        // Movement points
        public float MovementModifier { get; set; }

        // ZOC in hexes
        public int ZOCModifier { get; set; }

        /// <summary>
        /// Creates a new instance of the WeaponSystemProfile class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="nationality"></param>
        /// <param name="weaponSystem"></param>
        /// <param name="upgradeType"></param>
        public WeaponSystemProfile(string name, Nationality nationality, WeaponSystems weaponSystem)
        {
            Name = name;
            Nationality = nationality;
            WeaponSystem = weaponSystem;
        }

        // TODO: Method to add upgrade types.
    }
}