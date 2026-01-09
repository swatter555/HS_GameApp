using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.U2D;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Controllers
{
    public class SpriteManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(SpriteManager);

        #region Sprite Name Constants

        #region Hex Outlines

        public const string BlackHexOutline  = "BlackHexOutline";
        public const string WhiteHexOutline  = "WhiteHexOutline";
        public const string GreyHexOutline   = "GreyHexOutline";
        public const string HexSelectOutline = "HexSelectOutline";

        #endregion // Hex Outlines

        #region Map Icons - Themed

        // Middle East
        public const string ME_Nameplate = "ME_Nameplate";
        public const string ME_MajorCity = "ME_MajorCity";
        public const string ME_MinorCity = "ME_MinorCity";
        public const string ME_Sprawl  = "ME_Sprawl";

        // Europe
        public const string EU_Nameplate = "EU_Nameplate";
        public const string EU_MajorCity = "EU_MajorCity";
        public const string EU_MinorCity = "EU_MinorCity";

        // China
        public const string CH_Nameplate = "CH_Nameplate";
        public const string CH_MajorCity = "CH_MajorCity";
        public const string CH_MinorCity = "CH_MinorCity";

        // Generic
        public const string GEN_Fort = "GEN_Fort";
        public const string GEN_Airbase = "GEN_Airbase";

        // Void sprite parameter
        public const string VoidSpriteName = "None";

        #endregion // Map Icons - Themed

        #region Control Icons

        public const string Control_SV     = "Control_SV";
        public const string Control_BE     = "Control_BE";
        public const string Control_DE     = "Control_DE";
        public const string Control_FR     = "Control_FR";
        public const string Control_GE     = "Control_GE";
        public const string Control_MJ     = "Control_MJ";
        public const string Control_US     = "Control_US";
        public const string Control_NE     = "Control_NE";
        public const string Control_UK     = "Control_UK";
        public const string Control_China  = "Control_China";
        public const string Control_Iran   = "Control_Iran";
        public const string Control_Iraq   = "Control_Iraq";
        public const string Control_Kuwait = "Control_Kuwait";
        public const string Control_Saudi  = "Control_Saudi";
        public const string Control_None   = "Control_None";
        public const string MainObjective  = "MainObjective";

        #endregion // Control Icons

        #region Bridge Icons

        // Normal Bridges
        public const string BridgeW  = "Bridge_W";
        public const string BridgeE  = "Bridge_E";
        public const string BridgeNW = "Bridge_NW";
        public const string BridgeNE = "Bridge_NE";
        public const string BridgeSW = "Bridge_SW";
        public const string BridgeSE = "Bridge_SE";

        // Damaged Bridges
        public const string DamagedBridgeW  = "DamagedBridge_W";
        public const string DamagedBridgeE  = "DamagedBridge_E";
        public const string DamagedBridgeNW = "DamagedBridge_NW";
        public const string DamagedBridgeNE = "DamagedBridge_NE";
        public const string DamagedBridgeSW = "DamagedBridge_SW";
        public const string DamagedBridgeSE = "DamagedBridge_SE";

        // Pontoon Bridges
        public const string PontBridgeW  = "Pont_W";
        public const string PontBridgeE  = "Pont_E";
        public const string PontBridgeNW = "Pont_NW";
        public const string PontBridgeNE = "Pont_NE";
        public const string PontBridgeSW = "Pont_SW";
        public const string PontBridgeSE = "Pont_SE";

        #endregion // Bridge Icons

        #region Rank Icons

        public const string Colonel = "Col";
        public const string ColonelGeneral = "ColGeneral";
        public const string GeneralOfArmy = "GenArmy";
        public const string LieutenantGeneral = "LtGeneral";
        public const string MajorGeneral = "MjGeneral";

        #endregion // Rank Icons

        #region Terrain Portraits

        // Water
        public const string TP_Water = "Ocean";

        // Middle East
        public const string ME_TP_Clear     = "ME_Clear";
        public const string ME_TP_Forest    = "ME_Forest";
        public const string ME_TP_Marsh     = "ME_Marsh";
        public const string ME_TP_Mountains = "ME_Mountains";
        public const string ME_TP_Rough     = "ME_Rough";
        public const string ME_TP_City      = "ME_City";
        public const string ME_TP_Town      = "ME_Town";

        // Europe
        public const string EU_TP_Clear     = "EU_Clear";
        public const string EU_TP_Forest    = "EU_Forest";
        public const string EU_TP_Marsh     = "EU_Marsh";
        public const string EU_TP_Mountains = "EU_Mountains";
        public const string EU_TP_Rough     = "EU_Rough";
        public const string EU_TP_City      = "EU_City";
        public const string EU_TP_Town      = "EU_Town";

        // China
        public const string CH_TP_Clear     = "CH_Clear";
        public const string CH_TP_Forest    = "CH_Forest";
        public const string CH_TP_Marsh     = "CH_Marsh";
        public const string CH_TP_Mountains = "CH_Mountains";
        public const string CH_TP_Rough     = "CH_Rough";
        public const string CH_TP_City      = "CH_City";
        public const string CH_TP_Town      = "CH_Town";

        #endregion // Terrain Portraits

        #endregion // Sprite Name Constants

        #region Soviet Unit Icons

        // Anti-Aircraft Systems
        public const string SV_2K22_W = "SV_2K22_W";
        public const string SV_2K22_NW = "SV_2K22_NW";
        public const string SV_2K22_SW = "SV_2K22_SW";
        public const string SV_9K31_W = "SV_9K31_W";
        public const string SV_9K31_NW = "SV_9K31_NW";
        public const string SV_9K31_SW = "SV_9K31_SW";
        public const string SV_ZSU23_W = "SV_ZSU23_W";
        public const string SV_ZSU23_NW = "SV_ZSU23_NW";
        public const string SV_ZSU23_SW = "SV_ZSU23_SW";
        public const string SV_ZSU57_W = "SV_ZSU57_W";
        public const string SV_ZSU57_NW = "SV_ZSU57_NW";
        public const string SV_ZSU57_SW = "SV_ZSU57_SW";

        // Artillery
        public const string SV_2S1_W = "SV_2S1_W";
        public const string SV_2S1_W_F = "SV_2S1_W_F";
        public const string SV_2S3_W = "SV_2S3_W";
        public const string SV_2S3_W_F = "SV_2S3_W_F";
        public const string SV_2S5_W = "SV_2S5_W";
        public const string SV_2S5_W_F = "SV_2S5_W_F";
        public const string SV_2S19_W = "SV_2S19_W";
        public const string SV_2S19_W_F = "SV_2S19_W_F";
        public const string SV_AA_W = "SV_AA_W";
        public const string SV_HeavyArt_W = "SV_HeavyArt_W";
        public const string SV_LightArt_W = "SV_LightArt_W";

        // Rocket Artillery
        public const string SV_BM21_W = "SV_BM21_W";
        public const string SV_BM21_W_F = "SV_BM21_W_F";
        public const string SV_BM27_W = "SV_BM27_W";
        public const string SV_BM27_W_F = "SV_BM27_W_F";
        public const string SV_BM30_W = "SV_BM30_W";
        public const string SV_BM30_W_F = "SV_BM30_W_F";

        // Missiles
        public const string SV_ScudB_W = "SV_ScubB_W";
        public const string SV_ScudB_W_F = "SV_ScubB_W_F";

        // Infantry Fighting Vehicles
        public const string SV_BMD2_W = "SV_BMD2_W";
        public const string SV_BMD2_NW = "SV_BMD2_NW";
        public const string SV_BMD2_SW = "SV_BMD2_SW";
        public const string SV_BMD3_W = "SV_BMD3_W";
        public const string SV_BMD3_NW = "SV_BMD3_NW";
        public const string SV_BMD3_SW = "SV_BMD3_SW";
        public const string SV_BMP1_W = "SV_BMP1_W";
        public const string SV_BMP1_NW = "SV_BMP1_NW";
        public const string SV_BMP1_SW = "SV_BMP1_SW";
        public const string SV_BMP2_W = "SV_BMP2_W";
        public const string SV_BMP2_NW = "SV_BMP2_NW";
        public const string SV_BMP2_SW = "SV_BMP2_SW";
        public const string SV_BMP3_W = "SV_BMP3_W";
        public const string SV_BMP3_NW = "SV_BMP3_NW";
        public const string SV_BMP3_SW = "SV_BMP3_SW";

        // Reconnaissance & APCs
        public const string SV_BRDM2_W = "SV_BRDM2_W";
        public const string SV_BRDM2_NW = "SV_BRDM2_NW";
        public const string SV_BRDM2_SW = "SV_BRDM2_SW";
        public const string SV_BRDM2AT_W = "SV_BRDM2AT_W";
        public const string SV_BRDM2AT_NW = "SV_BRDM2AT_NW";
        public const string SV_BRDM2AT_SW = "SV_BRDM2AT_SW";
        public const string SV_BTR70_W = "SV_BTR70_W";
        public const string SV_BTR70_NW = "SV_BTR70_NW";
        public const string SV_BTR70_SW = "SV_BTR70_SW";
        public const string SV_BTR80_W = "SV_BTR80_W";
        public const string SV_BTR80_NW = "SV_BTR80_NW";
        public const string SV_BTR80_SW = "SV_BTR80_SW";
        public const string SV_MTLB_W = "SV_MTLB_W";
        public const string SV_MTLB_NW = "SV_MTLB_NW";
        public const string SV_MTLB_SW = "SV_MTLB_SW";

        // Helicopters - Animated (6 frames each)
        public const string SV_MI8_Frame0 = "SV_MI8_Frame0";
        public const string SV_MI8_Frame1 = "SV_MI8_Frame1";
        public const string SV_MI8_Frame2 = "SV_MI8_Frame2";
        public const string SV_MI8_Frame3 = "SV_MI8_Frame3";
        public const string SV_MI8_Frame4 = "SV_MI8_Frame4";
        public const string SV_MI8_Frame5 = "SV_MI8_Frame5";
        public const string SV_MI8AT_Frame0 = "SV_MI8AT_Frame0";
        public const string SV_MI8AT_Frame1 = "SV_MI8AT_Frame1";
        public const string SV_MI8AT_Frame2 = "SV_MI8AT_Frame2";
        public const string SV_MI8AT_Frame3 = "SV_MI8AT_Frame3";
        public const string SV_MI8AT_Frame4 = "SV_MI8AT_Frame4";
        public const string SV_MI8AT_Frame5 = "SV_MI8AT_Frame5";
        public const string SV_MI24D_Frame0 = "SV_MI24D_Frame0";
        public const string SV_MI24D_Frame1 = "SV_MI24D_Frame1";
        public const string SV_MI24D_Frame2 = "SV_MI24D_Frame2";
        public const string SV_MI24D_Frame3 = "SV_MI24D_Frame3";
        public const string SV_MI24D_Frame4 = "SV_MI24D_Frame4";
        public const string SV_MI24D_Frame5 = "SV_MI24D_Frame5";
        public const string SV_MI24V_Frame0 = "SV_MI24V_Frame0";
        public const string SV_MI24V_Frame1 = "SV_MI24V_Frame1";
        public const string SV_MI24V_Frame2 = "SV_MI24V_Frame2";
        public const string SV_MI24V_Frame3 = "SV_MI24V_Frame3";
        public const string SV_MI24V_Frame4 = "SV_MI24V_Frame4";
        public const string SV_MI24V_Frame5 = "SV_MI24V_Frame5";
        public const string SV_MI28_Frame0 = "SV_MI28_Frame0";
        public const string SV_MI28_Frame1 = "SV_MI28_Frame1";
        public const string SV_MI28_Frame2 = "SV_MI28_Frame2";
        public const string SV_MI28_Frame3 = "SV_MI28_Frame3";
        public const string SV_MI28_Frame4 = "SV_MI28_Frame4";
        public const string SV_MI28_Frame5 = "SV_MI28_Frame5";

        // Transport Aircraft
        public const string SV_AN8_W = "SV_AN8_W";

        // Fixed-Wing Aircraft
        public const string SV_A50_W = "SV_A50_W";
        public const string SV_Mig21_W = "SV_Mig21_W";
        public const string SV_Mig23_W = "SV_Mig23_W";
        public const string SV_Mig25_W = "SV_Mig25_W";
        public const string SV_Mig25R_W = "SV_Mig25R_W";
        public const string SV_Mig27_W = "SV_Mig27_W";
        public const string SV_Mig29_W = "SV_Mig29_W";
        public const string SV_Mig31_W = "SV_Mig31_W";
        public const string SV_SU24_W = "SV_SU24_W";
        public const string SV_SU25_W = "SV_SU25_W";
        public const string SV_SU25B_W = "SV_SU25B_W";
        public const string SV_SU27_W = "SV_SU27_W";
        public const string SV_SU47_W = "SV_SU47_W";
        public const string SV_TU16_W = "SV_TU16_W";
        public const string SV_TU22_W = "SV_TU22_W";
        public const string SV_TU22M3_W = "SV_TU22M3_W";

        // Tanks
        public const string SV_T55A_W = "SV_T55A_W";
        public const string SV_T55A_NW = "SV_T55A_NW";
        public const string SV_T55A_SW = "SV_T55A_SW";
        public const string SV_T64A_W = "SV_T64A_W";
        public const string SV_T64A_NW = "SV_T64A_NW";
        public const string SV_T64A_SW = "SV_T64A_SW";
        public const string SV_T64B_W = "SV_T64B_W";
        public const string SV_T64B_NW = "SV_T64B_NW";
        public const string SV_T64B_SW = "SV_T64B_SW";
        public const string SV_T72A_W = "SV_T72A_W";
        public const string SV_T72A_NW = "SV_T72A_NW";
        public const string SV_T72A_SW = "SV_T72A_SW";
        public const string SV_T72B_W = "SV_T72B_W";
        public const string SV_T72B_NW = "SV_T72B_NW";
        public const string SV_T72B_SW = "SV_T72B_SW";
        public const string SV_T80B_W = "SV_T80B_W";
        public const string SV_T80B_NW = "SV_T80B_NW";
        public const string SV_T80B_SW = "SV_T80B_SW";
        public const string SV_T80BVM_W = "SV_T80BVM_W";
        public const string SV_T80BVM_NW = "SV_T80BVM_NW";
        public const string SV_T80BVM_SW = "SV_T80BVM_SW";
        public const string SV_T80U_W = "SV_T80U_W";
        public const string SV_T80U_NW = "SV_T80U_NW";
        public const string SV_T80U_SW = "SV_T80U_SW";

        // SAM Systems
        public const string SV_S75_W = "SV_S75_W";
        public const string SV_S125_W = "SV_S125_W";
        public const string SV_S300_W = "SV_S300_W";
        public const string SV_S300_W_F = "SV_S300_W_F";

        // Infantry & Support
        public const string SV_Airborne = "SV_Airborne";
        public const string SV_AirMobile = "SV_AirMobile";
        public const string SV_Engineers = "SV_Engineers";
        public const string SV_Regulars = "SV_Regulars";
        public const string SV_Spetsnaz = "SV_Spetsnaz";
        public const string SV_Truck_W = "SV_Truck_W";
        public const string SV_Truck_NW = "SV_Truck_NW";
        public const string SV_Truck_SW = "SV_Truck_SW";

        #endregion // Soviet Unit Icons

        #region NATO Unit Icons

        // US Infantry & Support
        public const string US_Airborne = "US_Airborne";
        public const string US_Marines = "US_Marines";
        public const string US_Regulars = "US_Regulars";

        // US Vehicles
        public const string US_Humvee_W = "US_Humvee_W";
        public const string US_Humvee_NW = "US_Humvee_NW";
        public const string US_Humvee_SW = "US_Humvee_SW";
        public const string US_LVTP_W = "US_LVTP_W";
        public const string US_LVTP_NW = "US_LVTP_NW";
        public const string US_LVTP_SW = "US_LVTP_SW";
        public const string US_M113_W = "US_M113_W";
        public const string US_M113_NW = "US_M113_NW";
        public const string US_M113_SW = "US_M113_SW";
        public const string US_M2_W = "US_M2_W";
        public const string US_M2_NW = "US_M2_NW";
        public const string US_M2_SW = "US_M2_SW";

        // US Tanks
        public const string US_M1_W = "US_M1_W";
        public const string US_M1_NW = "US_M1_NW";
        public const string US_M1_SW = "US_M1_SW";

        // US Artillery
        public const string US_M109_W = "US_M109_W";
        public const string US_M109_W_F = "US_M109_W_F";
        public const string US_MLRS_W = "US_MLRS_W";
        public const string US_MLRS_W_F = "US_MLRS_W_F";

        // US Anti-Aircraft
        public const string US_Chaparral_W = "US_Chaparral_W";
        public const string US_Chaparral_NW = "US_Chaparral_NW";
        public const string US_Chaparral_SW = "US_Chaparral_SW";
        public const string US_M163_W = "US_M163_W";
        public const string US_M163_NW = "US_M163_NW";
        public const string US_M163_SW = "US_M163_SW";

        // US SAM Systems
        public const string US_Hawk_W = "US_Hawk_W";

        // US Helicopters - Animated
        public const string US_AH64_Frame0 = "US_AH64_Frame0";
        public const string US_AH64_Frame1 = "US_AH64_Frame1";
        public const string US_AH64_Frame2 = "US_AH64_Frame2";
        public const string US_AH64_Frame3 = "US_AH64_Frame3";
        public const string US_AH64_Frame4 = "US_AH64_Frame4";
        public const string US_AH64_Frame5 = "US_AH64_Frame5";
        public const string US_UH60_Frame0 = "US_UH60_Frame0";
        public const string US_UH60_Frame1 = "US_UH60_Frame1";
        public const string US_UH60_Frame2 = "US_UH60_Frame2";
        public const string US_UH60_Frame3 = "US_UH60_Frame3";
        public const string US_UH60_Frame4 = "US_UH60_Frame4";
        public const string US_UH60_Frame5 = "US_UH60_Frame5";

        // US Aircraft
        public const string US_E3_W = "US_E3_W";
        public const string US_A10_W = "US_A10_W";
        public const string US_F111_W = "US_F111_W";
        public const string US_F117_W = "US_F117_W";
        public const string US_F15_W = "US_F15_W";
        public const string US_F16_W = "US_F16_W";
        public const string US_F4_W = "US_F4_W";
        public const string US_SR71_W = "US_SR71_W";

        // UK Infantry & Support
        public const string UK_Regulars = "UK_Regulars";

        // UK Vehicles
        public const string UK_Warrior_W = "UK_Warrior_W";
        public const string UK_Warrior_NW = "UK_Warrior_NW";
        public const string UK_Warrior_SW = "UK_Warrior_SW";

        // UK Tanks
        public const string UK_Challenger1_W = "UK_Challenger1_W";
        public const string UK_Challenger1_NW = "UK_Challenger1_NW";
        public const string UK_Challenger1_SW = "UK_Challenger1_SW";

        // UK Artillery
        public const string UK_M109_W = "UK_M109_W";
        public const string UK_M109_W_F = "UK_M109_W_F";

        // UK Aircraft
        public const string UK_TornadoGR1_W = "UK_TornadoGR1_W";

        // German Infantry & Support
        public const string GE_Regulars = "GE_Regulars";

        // German Vehicles
        public const string GE_Marder_W = "GE_Marder_W";
        public const string GE_Marder_NW = "GE_Marder_NW";
        public const string GE_Marder_SW = "GE_Marder_SW";

        // German Tanks
        public const string GE_Leopard1_W = "GE_Leopard1_W";
        public const string GE_Leopard1_NW = "GE_Leopard1_NW";
        public const string GE_Leopard1_SW = "GE_Leopard1_SW";
        public const string GE_Leopard2_W = "GE_Leopard2_W";
        public const string GE_Leopard2_NW = "GE_Leopard2_NW";
        public const string GE_Leopard2_SW = "GE_Leopard2_SW";

        // German Artillery
        public const string GE_M109_W = "GE_M109_W";
        public const string GE_M109_W_F = "GE_M109_W_F";

        // German Anti-Aircraft
        public const string GE_Gepard_W = "GE_Gepard_W";
        public const string GE_Gepard_NW = "GE_Gepard_NW";
        public const string GE_Gepard_SW = "GE_Gepard_SW";

        // German Helicopters - Animated
        public const string GE_BO105_Frame0 = "GE_BO105_Frame0";
        public const string GE_BO105_Frame1 = "GE_BO105_Frame1";
        public const string GE_BO105_Frame2 = "GE_BO105_Frame2";
        public const string GE_BO105_Frame3 = "GE_BO105_Frame3";
        public const string GE_BO105_Frame4 = "GE_BO105_Frame4";
        public const string GE_BO105_Frame5 = "GE_BO105_Frame5";

        // German Aircraft
        public const string GE_Tornado_W = "GE_Tornado_W";
        public const string GE_F4_W = "GE_F4_W";

        // French Infantry & Support
        public const string FR_Regulars = "FR_Regulars";

        // French Vehicles
        public const string FR_AMX30_W = "FR_AMX30_W";
        public const string FR_AMX30_NW = "FR_AMX30_NW";
        public const string FR_AMX30_SW = "FR_AMX30_SW";
        public const string FR_M113_W = "FR_M113_W";
        public const string FR_M113_NW = "FR_M113_NW";
        public const string FR_M113_SW = "FR_M113_SW";

        // French Artillery
        public const string FR_M109_W = "FR_M109_W";
        public const string FR_M109_W_F = "FR_M109_W_F";

        // French Anti-Aircraft
        public const string FR_Gepard_W = "FR_Gepard_W";
        public const string FR_Gepard_NW = "FR_Gepard_NW";
        public const string FR_Gepard_SW = "FR_Gepard_SW";

        // French SAM Systems
        public const string FR_Roland_W = "FR_Roland_W";
        public const string FR_Roland_NW = "FR_Roland_NW";
        public const string FR_Roland_SW = "FR_Roland_SW";

        // French Aircraft
        public const string FR_Jaguar_W = "FR_Jaguar_W";
        public const string FR_Mirage2000_W = "FR_Mirage2000_W";

        // Mujahideen
        public const string MJ_AA = "MJ_AA";
        public const string MJ_Artillery = "MJ_Artillery";
        public const string MJ_Elite = "MJ_Elite";
        public const string MJ_Mortar = "MJ_Mortar";
        public const string MJ_Mounted = "MJ_Mounted";
        public const string MJ_Regulars = "MJ_Regulars";
        public const string MJ_RPG = "MJ_RPG";
        public const string MJ_Stinger = "MJ_Stinger";

        // Generic Units
        public const string GEN_AA_W = "GEN_AA_W";
        public const string GEN_Base = "GEN_Base";
        public const string GEN_Depot = "GEN_Depot";
        public const string GEN_HeavyArt_W = "GEN_HeavyArt_W";
        public const string GEN_LightArt_W = "GEN_LightArt_W";
        public const string GEN_NavalTransport = "GEN_NavalTransport";
        public const string GEN_Truck_W = "GEN_Truck_W";
        public const string GEN_Truck_NW = "GEN_Truck_NW";
        public const string GEN_Truck_SW = "GEN_Truck_SW";

        #endregion // NATO Unit Icons

        #region Nationality Flags

        public const string Flag_BE     = "Flag_BE";
        public const string Flag_China  = "Flag_China";
        public const string Flag_DE     = "Flag_DE";
        public const string Flag_FR     = "Flag_FR";
        public const string Flag_GE     = "Flag_GE";
        public const string Flag_Iran   = "Flag_Iran";
        public const string Flag_Iraq   = "Flag_Iraq";
        public const string Flag_Kuwait = "Flag_Kuwait";
        public const string Flag_MJ     = "Flag_MJ";
        public const string Flag_NE     = "Flag_NE";
        public const string Flag_Saudi  = "Flag_Saudi";
        public const string Flag_SV     = "Flag_SV";
        public const string Flag_UK     = "Flag_UK";
        public const string Flag_US     = "Flag_US";

        #endregion // Nationality Flags

        #region NATO Symbol Icons

        public const string Icon_AAA            = "Icon_AAA";
        public const string Icon_AirAssault     = "Icon_AirAssault";
        public const string Icon_Airbase        = "Icon_Airbase";
        public const string Icon_Antitank       = "Icon_Antitank";
        public const string Icon_ArmoredCav     = "Icon_ArmoredCav";
        public const string Icon_ArmoredMarine  = "Icon_ArmoredMarine";
        public const string Icon_Artillery      = "Icon_Artillery";
        public const string Icon_Engineer       = "Icon_Engineer";
        public const string Icon_FixedWing      = "Icon_FixedWing";
        public const string Icon_FixedWingRecon = "Icon_FixedWingRecon";
        public const string Icon_Infantry       = "Icon_Infantry";
        public const string Icon_Marine         = "Icon_Marine";
        public const string Icon_Mech           = "Icon_Mech";
        public const string Icon_Mot            = "Icon_Mot";
        public const string Icon_Recon          = "Icon_Recon";
        public const string Icon_SAM            = "Icon_SAM";
        public const string Icon_Signal         = "Icon_Signal";
        public const string Icon_SPArtillery    = "Icon_SPArtillery";
        public const string Icon_Tank           = "Icon_Tank";

        #endregion // NATO Symbol Icons

        #region Unit Base Icons

        public const string BlueIconBase = "BlueIconBase";
        public const string GreenIconBase = "GreenIconBase";
        public const string RedIconBase = "RedIconBase";

        #endregion // Unit Base Icons

        #region National Symbols

        public const string Symbol_BE     = "Symbol_BE";
        public const string Symbol_China  = "Symbol_CH";
        public const string Symbol_DE     = "Symbol_DE";
        public const string Symbol_FR     = "Symbol_FR";
        public const string Symbol_GE     = "Symbol_GE";
        public const string Symbol_Iran   = "Symbol_IR";
        public const string Symbol_Iraq   = "Symbol_IQ";
        public const string Symbol_Kuwait = "Symbol_KQ";
        public const string Symbol_MJ     = "Symbol_MJ";
        public const string Symbol_NE     = "Symbol_NE";
        public const string Symbol_Saudi  = "Symbol_SA";
        public const string Symbol_SV     = "Symbol_SV";
        public const string Symbol_UK     = "Symbol_UK";
        public const string Symbol_US     = "Symbol_US";
        public const string Symbol_Default = "Symbol_DF";

        #endregion // National Symbols

        #region Utility Icons

        public const string Utility_AirbaseStack1 = "AirbaseStack1";
        public const string Utility_AirbaseStack2 = "AirbaseStack2";
        public const string Utility_AirbaseStack3 = "AirbaseStack3";
        public const string Utility_AirbaseStack4 = "AirbaseStack4";
        public const string Utility_AirMissionMarker = "AirMissionMarker";
        public const string Utility_StackingIconAir = "StackingIcon_AirSelect";
        public const string Utility_StackingIconLand = "StackingIcon_LandSelect";
        public const string Utility_MismatchIcon = "MismatchIcon";

        #endregion

        #region Singleton

        private static SpriteManager _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static SpriteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance in scene (using new Unity API)
                    _instance = FindAnyObjectByType<SpriteManager>();

                    // Create new instance if none exists
                    if (_instance == null)
                    {
                        GameObject go = new("SpriteManager");
                        _instance = go.AddComponent<SpriteManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Inspector Fields

        [Header("Sprite Atlases")]
        [SerializeField] private SpriteAtlas _hexIconAtlas;
        [SerializeField] private SpriteAtlas _controlIconAtlas;
        [SerializeField] private SpriteAtlas _mapIconAtlas;
        [SerializeField] private SpriteAtlas _bridgeIconAtlas;
        [SerializeField] private SpriteAtlas _rankAtlas;
        [SerializeField] private SpriteAtlas _terrainPortraitAtlas;
        [SerializeField] private SpriteAtlas _sovietIconAtlas;
        [SerializeField] private SpriteAtlas _natoIconAtlas;
        [SerializeField] private SpriteAtlas _genericIconAtlas;
        [SerializeField] private SpriteAtlas _natoSymbolIconAtlas;
        [SerializeField] private SpriteAtlas _nationalFlagAtlas;
        [SerializeField] private SpriteAtlas _nationalSymbolAtlas;
        [SerializeField] private SpriteAtlas _utilityIconAtlas;

        [Header("Prefabs")]
        [SerializeField] private GameObject _cityPrefab;
        [SerializeField] private GameObject _mapIconPrefab;
        [SerializeField] private GameObject _bridgeIconPrefab;
        [SerializeField] private GameObject _mapTextPrefab;
        [SerializeField] private GameObject _unitIconPrefab;

        #endregion

        #region Properties

        public GameObject CityPrefab => _cityPrefab;
        public GameObject MapIconPrefab => _mapIconPrefab;
        public GameObject BridgeIconPrefab => _bridgeIconPrefab;
        public GameObject MapTextPrefab => _mapTextPrefab;
        public GameObject UnitIconPrefab => _unitIconPrefab;

        #endregion // Properties

        #region Unity Lifecycle

        private void Awake()
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion // Unity Lifecycle

        #region Static Methods

        /// <summary>
        /// Retrieves a sprite by name, searching through all atlases.
        /// </summary>
        public static Sprite GetSprite(string spriteName)
        {
            if (Instance == null)
            {
                UnityEngine.Debug.LogError($"{CLASS_NAME}.GetSprite: Instance is null.");
                return null;
            }

            try
            {
                // Search through all atlases
                Sprite sprite = null;

                // Try hex icon atlas
                if (Instance._hexIconAtlas != null)
                {
                    sprite = Instance._hexIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try control icon atlas
                if (Instance._controlIconAtlas != null)
                {
                    sprite = Instance._controlIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try map icon atlas
                if (Instance._mapIconAtlas != null)
                {
                    sprite = Instance._mapIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try bridge icon atlas
                if (Instance._bridgeIconAtlas != null)
                {
                    sprite = Instance._bridgeIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try rank atlas
                if (Instance._rankAtlas != null)
                {
                    sprite = Instance._rankAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try terrain portrait atlas
                if (Instance._terrainPortraitAtlas != null)
                {
                    sprite = Instance._terrainPortraitAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try Soviet unit icon atlas
                if (Instance._sovietIconAtlas != null)
                {
                    sprite = Instance._sovietIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try NATO unit icon atlas
                if (Instance._natoIconAtlas != null)
                {
                    sprite = Instance._natoIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try generic icon atlas
                if (Instance._genericIconAtlas != null)
                {
                    sprite = Instance._genericIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try NATO symbol icon atlas
                if (Instance._natoSymbolIconAtlas != null)
                {
                    sprite = Instance._natoSymbolIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try national flag atlas
                if (Instance._nationalFlagAtlas != null)
                {
                    sprite = Instance._nationalFlagAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try national symbol atlas
                if (Instance._nationalSymbolAtlas != null)
                {
                    sprite = Instance._nationalSymbolAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try utility icon atlas
                if (Instance._utilityIconAtlas != null)
                {
                    sprite = Instance._utilityIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Sprite not found in any atlas - log warning and return null
                string warningMessage = $"{CLASS_NAME}.GetSprite: Sprite '{spriteName}' not found in any atlas.";
                UnityEngine.Debug.LogWarning(warningMessage);
                AppService.CaptureUiMessage(warningMessage);
                return null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSprite", e);
                return null;
            }
        }

        #endregion // Static Methods
    }
}
