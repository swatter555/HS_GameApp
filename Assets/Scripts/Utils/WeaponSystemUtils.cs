using HammerAndSickle.Models;
using System;
using System.Collections.Generic;

namespace HammerAndSickle.Utils
{
    /// <summary>
    /// Utility class for converting WeaponSystems enum values to descriptive display names.
    /// Provides proper military designations and common names for all weapon systems in the game.
    /// Soviet systems include flavor names and NATO codenames where applicable.
    /// </summary>
    public static class WeaponSystemUtils
    {
        #region Constants

        private static readonly Dictionary<WeaponSystems, string> WeaponSystemDisplayNames = new()
        {
            // Soviet weapon systems
            { WeaponSystems.TANK_T55A, "T-55A" },
            { WeaponSystems.TANK_T64A, "T-64A" },
            { WeaponSystems.TANK_T64B, "T-64B" },
            { WeaponSystems.TANK_T72A, "T-72A" },
            { WeaponSystems.TANK_T72B, "T-72B" },
            { WeaponSystems.TANK_T80B, "T-80B" },
            { WeaponSystems.TANK_T80U, "T-80U" },
            { WeaponSystems.TANK_T80BV, "T-80BV" },

            { WeaponSystems.APC_MTLB, "MT-LB" },
            { WeaponSystems.APC_BTR70, "BTR-70" },
            { WeaponSystems.APC_BTR80, "BTR-80" },
            { WeaponSystems.IFV_BMP1, "BMP-1" },
            { WeaponSystems.IFV_BMP2, "BMP-2" },
            { WeaponSystems.IFV_BMP3, "BMP-3" },
            { WeaponSystems.IFV_BMD2, "BMD-2" },
            { WeaponSystems.IFV_BMD3, "BMD-3" },

            { WeaponSystems.RCN_BRDM2, "BRDM-2" },
            { WeaponSystems.RCN_BRDM2AT, "BRDM-2AT" },

            { WeaponSystems.SPA_2S1, "2S1 Gvozdika" },
            { WeaponSystems.SPA_2S3, "2S3 Akatsiya" },
            { WeaponSystems.SPA_2S5, "2S5 Giatsint-S" },
            { WeaponSystems.SPA_2S19, "2S19 Msta-S" },
            { WeaponSystems.ROC_BM21, "BM-21 Grad" },
            { WeaponSystems.ROC_BM27, "BM-27 Uragan" },
            { WeaponSystems.ROC_BM30, "BM-30 Smerch" },
            { WeaponSystems.SSM_SCUD, "9K72 Scud" },

            { WeaponSystems.SPAAA_ZSU57, "ZSU-57-2" },
            { WeaponSystems.SPAAA_ZSU23, "ZSU-23-4 Shilka" },
            { WeaponSystems.SPAAA_2K22, "2K22 Tunguska" },
            { WeaponSystems.SPSAM_9K31, "9K31 Strela-1" },
            { WeaponSystems.SAM_S75, "S-75 Dvina" },
            { WeaponSystems.SAM_S125, "S-125 Neva" },
            { WeaponSystems.SAM_S300, "S-300" },

            { WeaponSystems.HEL_MI8T, "Mi-8T \"Hip\"" },
            { WeaponSystems.HEL_MI8AT, "Mi-8AT \"Hip\"" },
            { WeaponSystems.HEL_MI24D, "Mi-24D \"Hind\"" },
            { WeaponSystems.HEL_MI24V, "Mi-24V \"Hind\"" },
            { WeaponSystems.HEL_MI28, "Mi-28 \"Havoc\"" },
            { WeaponSystems.HELTRAN_MI8, "Mi-8 \"Hip\"" },
            { WeaponSystems.AWACS_A50, "A-50 \"Mainstay\"" },
            { WeaponSystems.TRAN_AN8, "An-8 \"Camp\"" },
            { WeaponSystems.ASF_MIG21, "MiG-21 \"Fishbed\"" },
            { WeaponSystems.ASF_MIG23, "MiG-23 \"Flogger\"" },
            { WeaponSystems.ASF_MIG25, "MiG-25 \"Foxbat\"" },
            { WeaponSystems.ASF_MIG29, "MiG-29 \"Fulcrum\"" },
            { WeaponSystems.ASF_MIG31, "MiG-31 \"Foxhound\"" },
            { WeaponSystems.ASF_SU27, "Su-27 \"Flanker\"" },
            { WeaponSystems.ASF_SU47, "Su-47 Berkut" },
            { WeaponSystems.MRF_MIG27, "MiG-27 \"Flogger\"" },
            { WeaponSystems.ATT_SU25, "Su-25 \"Frogfoot\"" },
            { WeaponSystems.ATT_SU25B, "Su-25B \"Frogfoot\"" },
            { WeaponSystems.BMB_SU24, "Su-24 \"Fencer\"" },
            { WeaponSystems.BMB_TU16, "Tu-16 \"Badger\"" },
            { WeaponSystems.BMB_TU22, "Tu-22 \"Blinder\"" },
            { WeaponSystems.BMB_TU22M3, "Tu-22M3 \"Backfire\"" },
            { WeaponSystems.RCNA_MIG25R, "MiG-25R \"Foxbat\"" },

            { WeaponSystems.REG_INF_SV, "Regular Infantry" },
            { WeaponSystems.AB_INF_SV, "Airborne Infantry" },
            { WeaponSystems.AM_INF_SV, "Air Mobile Infantry" },
            { WeaponSystems.MAR_INF_SV, "Marine Infantry" },
            { WeaponSystems.SPEC_INF_SV, "Special Forces Infantry" },
            { WeaponSystems.ENG_INF_SV, "Engineer Infantry" },
            
            // USA weapon systems
            { WeaponSystems.TANK_M1, "M1 Abrams" },
            { WeaponSystems.IFV_M2, "M2 Bradley" },
            { WeaponSystems.IFV_M3, "M3 Bradley" },
            { WeaponSystems.APC_M113, "M113" },
            { WeaponSystems.APC_LVTP7, "LVTP-7" },
            { WeaponSystems.SPA_M109, "M109 Paladin" },
            { WeaponSystems.ROC_MLRS, "M270 MLRS" },
            { WeaponSystems.SPAAA_M163, "M-163 VADS" },
            { WeaponSystems.SPSAM_CHAP, "M-48 Chaparral" },
            { WeaponSystems.SAM_HAWK, "MIM-23 Hawk" },
            { WeaponSystems.HEL_AH64, "AH-64 Apache" },
            { WeaponSystems.HELTRAN_UH1, "UH-1 Huey" },
            { WeaponSystems.ASF_F15, "F-15 Eagle" },
            { WeaponSystems.ASF_F4, "F-4 Phantom II" },
            { WeaponSystems.MRF_F16, "F-16 Fighting Falcon" },
            { WeaponSystems.ATT_A10, "A-10 Thunderbolt II" },
            { WeaponSystems.BMB_F111, "F-111 Aardvark" },
            { WeaponSystems.BMB_F117, "F-117 Nighthawk" },
            { WeaponSystems.RCNA_SR71, "SR-71 Blackbird" },

            { WeaponSystems.REG_INF_US, "Regular Infantry" },
            { WeaponSystems.AB_INF_US, "Airborne Infantry" },
            { WeaponSystems.AM_INF_US, "Air Mobile Infantry" },
            { WeaponSystems.MAR_INF_US, "Marine Infantry" },
            { WeaponSystems.SPEC_INF_US, "Special Forces Infantry" },
            { WeaponSystems.ENG_INF_US, "Engineer Infantry" },
            
            // West Germany (FRG) weapon systems
            { WeaponSystems.TANK_LEOPARD1, "Leopard 1" },
            { WeaponSystems.TANK_LEOPARD2, "Leopard 2" },
            { WeaponSystems.IFV_MARDER, "Marder" },
            { WeaponSystems.SPAAA_GEPARD, "Gepard" },
            { WeaponSystems.HEL_BO105, "BO 105" },
            { WeaponSystems.MRF_TornadoIDS, "Tornado IDS" },

            { WeaponSystems.REG_INF_FRG, "Regular Infantry" },
            { WeaponSystems.AB_INF_FRG, "Airborne Infantry" },
            { WeaponSystems.AM_INF_FRG, "Air Mobile Infantry" },
            { WeaponSystems.MAR_INF_FRG, "Marine Infantry" },
            { WeaponSystems.SPEC_INF_FRG, "Special Forces Infantry" },
            { WeaponSystems.ENG_INF_FRG, "Engineer Infantry" },
            
            // United Kingdom weapon systems
            { WeaponSystems.TANK_CHALLENGER1, "Challenger 1" },
            { WeaponSystems.IFV_WARRIOR, "Warrior" },

            { WeaponSystems.REG_INF_UK, "Regular Infantry" },
            { WeaponSystems.AB_INF_UK, "Airborne Infantry" },
            { WeaponSystems.AM_INF_UK, "Air Mobile Infantry" },
            { WeaponSystems.MAR_INF_UK, "Marine Infantry" },
            { WeaponSystems.SPEC_INF_UK, "Special Forces Infantry" },
            { WeaponSystems.ENG_INF_UK, "Engineer Infantry" },
            
            // France weapon systems
            { WeaponSystems.TANK_AMX30, "AMX-30" },
            { WeaponSystems.SPAAA_ROLAND, "Roland" },
            { WeaponSystems.ASF_MIRAGE2000, "Mirage 2000" },
            { WeaponSystems.ATT_JAGUAR, "Jaguar" },

            { WeaponSystems.REG_INF_FRA, "Regular Infantry" },
            { WeaponSystems.AB_INF_FRA, "Airborne Infantry" },
            { WeaponSystems.AM_INF_FRA, "Air Mobile Infantry" },
            { WeaponSystems.MAR_INF_FRA, "Marine Infantry" },
            { WeaponSystems.SPEC_INF_FRA, "Special Forces Infantry" },
            { WeaponSystems.ENG_INF_FRA, "Engineer Infantry" },
            
            // Arab armies weapon systems
            { WeaponSystems.REG_INF_ARAB, "Regular Infantry" },
            { WeaponSystems.AB_INF_ARAB, "Airborne Infantry" },
            { WeaponSystems.AM_INF_ARAB, "Air Mobile Infantry" },
            { WeaponSystems.MAR_INF_ARAB, "Marine Infantry" },
            { WeaponSystems.SPEC_INF_ARAB, "Special Forces Infantry" },
            { WeaponSystems.ENG_INF_ARAB, "Engineer Infantry" },
            
            // Generic weapon systems
            { WeaponSystems.AAA_GENERIC, "Light Anti-Aircraft Gun" },
            { WeaponSystems.ART_LIGHT_GENERIC, "Light Artillery" },
            { WeaponSystems.ART_HEAVY_GENERIC, "Heavy Artillery" },
            { WeaponSystems.MANPAD_GENERIC, "MANPAD" },
            { WeaponSystems.ATGM_GENERIC, "ATGM" },
            { WeaponSystems.LANDBASE_GENERIC, "Land Base" },
            { WeaponSystems.AIRBASE_GENERIC, "Airbase" },
            { WeaponSystems.SUPPLYDEPOT_GENERIC, "Supply Depot" }
        };

        #endregion // Constants

        #region Public Methods

        /// <summary>
        /// Gets the descriptive display name for a weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system enum value</param>
        /// <returns>The descriptive string representation</returns>
        public static string GetDisplayName(WeaponSystems weaponSystem)
        {
            return WeaponSystemDisplayNames.TryGetValue(weaponSystem, out string displayName)
                ? displayName
                : weaponSystem.ToString();
        }

        /// <summary>
        /// Checks if a weapon system has a defined display name.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to check</param>
        /// <returns>True if a display name is defined</returns>
        public static bool HasDisplayName(WeaponSystems weaponSystem)
        {
            return WeaponSystemDisplayNames.ContainsKey(weaponSystem);
        }

        /// <summary>
        /// Gets all supported weapon systems with their display names.
        /// </summary>
        /// <returns>Dictionary mapping weapon system enums to display names</returns>
        public static IReadOnlyDictionary<WeaponSystems, string> GetAllDisplayNames()
        {
            return WeaponSystemDisplayNames;
        }

        /// <summary>
        /// Gets the display name for a weapon system, with fallback formatting.
        /// If no display name is found, formats the enum name by replacing underscores with spaces.
        /// </summary>
        /// <param name="weaponSystem">The weapon system enum value</param>
        /// <returns>The formatted display name</returns>
        public static string GetFormattedDisplayName(WeaponSystems weaponSystem)
        {
            if (WeaponSystemDisplayNames.TryGetValue(weaponSystem, out string displayName))
            {
                return displayName;
            }

            // Fallback: format enum name by replacing underscores with spaces
            return weaponSystem.ToString().Replace('_', ' ');
        }

        #endregion // Public Methods
    }
}