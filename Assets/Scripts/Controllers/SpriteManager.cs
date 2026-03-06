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

        public const string Colonel           = "Col";
        public const string ColonelGeneral    = "ColGeneral";
        public const string GeneralOfArmy     = "GenArmy";
        public const string LieutenantGeneral = "LtGeneral";
        public const string MajorGeneral      = "MjGeneral";

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
        public const string SV_2K22_W    = "SV_2K22_W";
        public const string SV_2K22_NW   = "SV_2K22_NW";
        public const string SV_2K22_SW   = "SV_2K22_SW";
        public const string SV_2K22_W_F  = "SV_2K22_W_F";
        public const string SV_2K22_NW_F = "SV_2K22_NW_F";
        public const string SV_2K22_SW_F = "SV_2K22_SW_F";
        public const string SV_9K31_W    = "SV_9K31_W";
        public const string SV_9K31_NW   = "SV_9K31_NW";
        public const string SV_9K31_SW   = "SV_9K31_SW";
        public const string SV_9K31_W_F  = "SV_9K31_W_F";
        public const string SV_9K31_NW_F = "SV_9K31_NW_F";
        public const string SV_9K31_SW_F = "SV_9K31_SW_F";
        public const string SV_ZSU23_W    = "SV_ZSU23_W";
        public const string SV_ZSU23_NW   = "SV_ZSU23_NW";
        public const string SV_ZSU23_SW   = "SV_ZSU23_SW";
        public const string SV_ZSU23_W_F  = "SV_ZSU23_W_F";
        public const string SV_ZSU23_NW_F = "SV_ZSU23_NW_F";
        public const string SV_ZSU23_SW_F = "SV_ZSU23_SW_F";
        public const string SV_ZSU57_W    = "SV_ZSU57_W";
        public const string SV_ZSU57_NW   = "SV_ZSU57_NW";
        public const string SV_ZSU57_SW   = "SV_ZSU57_SW";
        public const string SV_ZSU57_W_F  = "SV_ZSU57_W_F";
        public const string SV_ZSU57_NW_F = "SV_ZSU57_NW_F";
        public const string SV_ZSU57_SW_F = "SV_ZSU57_SW_F";

        // 2K12 SPSAM
        public const string SV_2K12_W    = "SV_2K12_W";
        public const string SV_2K12_NW   = "SV_2K12_NW";
        public const string SV_2K12_SW   = "SV_2K12_SW";
        public const string SV_2K12_W_F  = "SV_2K12_W_F";
        public const string SV_2K12_NW_F = "SV_2K12_NW_F";
        public const string SV_2K12_SW_F = "SV_2K12_SW_F";

        // Artillery
        public const string SV_2S1_W    = "SV_2S1_W";
        public const string SV_2S1_NW   = "SV_2S1_NW";
        public const string SV_2S1_SW   = "SV_2S1_SW";
        public const string SV_2S1_W_F  = "SV_2S1_W_F";
        public const string SV_2S1_NW_F = "SV_2S1_NW_F";
        public const string SV_2S1_SW_F = "SV_2S1_SW_F";
        public const string SV_2S3_W    = "SV_2S3_W";
        public const string SV_2S3_NW   = "SV_2S3_NW";
        public const string SV_2S3_SW   = "SV_2S3_SW";
        public const string SV_2S3_W_F  = "SV_2S3_W_F";
        public const string SV_2S3_NW_F = "SV_2S3_NW_F";
        public const string SV_2S3_SW_F = "SV_2S3_SW_F";
        public const string SV_2S5_W    = "SV_2S5_W";
        public const string SV_2S5_NW   = "SV_2S5_NW";
        public const string SV_2S5_SW   = "SV_2S5_SW";
        public const string SV_2S5_W_F  = "SV_2S5_W_F";
        public const string SV_2S5_NW_F = "SV_2S5_NW_F";
        public const string SV_2S5_SW_F = "SV_2S5_SW_F";
        public const string SV_2S19_W    = "SV_2S19_W";
        public const string SV_2S19_NW   = "SV_2S19_NW";
        public const string SV_2S19_SW   = "SV_2S19_SW";
        public const string SV_2S19_W_F  = "SV_2S19_W_F";
        public const string SV_2S19_NW_F = "SV_2S19_NW_F";
        public const string SV_2S19_SW_F = "SV_2S19_SW_F";
        public const string SV_AA       = "SV_AA";
        public const string SV_HeavyArt = "SV_HeavyArt";
        public const string SV_LightArt = "SV_LightArt";

        // Rocket Artillery
        public const string SV_BM21_W    = "SV_BM21_W";
        public const string SV_BM21_NW   = "SV_BM21_NW";
        public const string SV_BM21_SW   = "SV_BM21_SW";
        public const string SV_BM21_W_F  = "SV_BM21_W_F";
        public const string SV_BM21_NW_F = "SV_BM21_NW_F";
        public const string SV_BM21_SW_F = "SV_BM21_SW_F";
        public const string SV_BM27_W    = "SV_BM27_W";
        public const string SV_BM27_NW   = "SV_BM27_NW";
        public const string SV_BM27_SW   = "SV_BM27_SW";
        public const string SV_BM27_W_F  = "SV_BM27_W_F";
        public const string SV_BM27_NW_F = "SV_BM27_NW_F";
        public const string SV_BM27_SW_F = "SV_BM27_SW_F";
        public const string SV_BM30_W    = "SV_BM30_W";
        public const string SV_BM30_NW   = "SV_BM30_NW";
        public const string SV_BM30_SW   = "SV_BM30_SW";
        public const string SV_BM30_W_F  = "SV_BM30_W_F";
        public const string SV_BM30_NW_F = "SV_BM30_NW_F";
        public const string SV_BM30_SW_F = "SV_BM30_SW_F";

        // Missiles
        public const string SV_ScudB_W    = "SV_ScudB_W";
        public const string SV_ScudB_NW   = "SV_ScudB_NW";
        public const string SV_ScudB_SW   = "SV_ScudB_SW";
        public const string SV_ScudB_W_F  = "SV_ScudB_W_F";
        public const string SV_ScudB_NW_F = "SV_ScudB_NW_F";
        public const string SV_ScudB_SW_F = "SV_ScudB_SW_F";

        // Personnel Fighting Vehicles
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

        // Reconnaissance & APC
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
        public const string SV_AN8 = "SV_AN8";

        // Fixed-Wing Aircraft
        public const string SV_A50    = "SV_A50";
        public const string SV_Mig21  = "SV_Mig21";
        public const string SV_Mig23  = "SV_Mig23";
        public const string SV_Mig25  = "SV_Mig25";
        public const string SV_Mig25R = "SV_Mig25R";
        public const string SV_Mig27  = "SV_Mig27";
        public const string SV_Mig29  = "SV_Mig29";
        public const string SV_Mig31  = "SV_Mig31";
        public const string SV_SU17   = "SV_SU17";
        public const string SV_SU24   = "SV_SU24";
        public const string SV_SU25   = "SV_SU25";
        public const string SV_SU25B  = "SV_SU25B";
        public const string SV_SU27   = "SV_SU27";
        public const string SV_SU47   = "SV_SU47";
        public const string SV_TU16   = "SV_TU16";
        public const string SV_TU22   = "SV_TU22";
        public const string SV_TU22M3 = "SV_TU22M3";

        // MBT
        public const string SV_T55A_W = "SV_T55A_W";
        public const string SV_T55A_NW = "SV_T55A_NW";
        public const string SV_T55A_SW = "SV_T55A_SW";
        public const string SV_T62_W  = "SV_T62_W";
        public const string SV_T62_NW = "SV_T62_NW";
        public const string SV_T62_SW = "SV_T62_SW";
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
        public const string SV_S75     = "SV_S75";
        public const string SV_S125    = "SV_S125";
        public const string SV_S300_W    = "SV_S300_W";
        public const string SV_S300_NW   = "SV_S300_NW";
        public const string SV_S300_SW   = "SV_S300_SW";
        public const string SV_S300_W_F  = "SV_S300_W_F";
        public const string SV_S300_NW_F = "SV_S300_NW_F";
        public const string SV_S300_SW_F = "SV_S300_SW_F";

        // Personnel & Support
        public const string SV_Airborne = "SV_Airborne";
        public const string SV_AirMobile = "SV_AirMobile";
        public const string SV_Engineers = "SV_Engineers";
        public const string SV_Marines = "SV_Marines";
        public const string SV_Regulars = "SV_Regulars";
        public const string SV_Spetsnaz = "SV_Spetsnaz";
        public const string SV_Truck_W = "SV_Truck_W";
        public const string SV_Truck_NW = "SV_Truck_NW";
        public const string SV_Truck_SW = "SV_Truck_SW";

        #endregion // Soviet Unit Icons

        #region NATO Unit Icons

        // US Personnel & Support
        public const string US_Airborne = "US_Airborne";
        public const string US_AirMobile = "US_AirMobile";
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

        // US MBT
        public const string US_M1_W  = "US_M1_W";
        public const string US_M1_NW = "US_M1_NW";
        public const string US_M1_SW = "US_M1_SW";
        public const string US_M60_W  = "US_M60_W";
        public const string US_M60_NW = "US_M60_NW";
        public const string US_M60_SW = "US_M60_SW";

        // US Artillery
        public const string US_M109_W    = "US_M109_W";
        public const string US_M109_NW   = "US_M109_NW";
        public const string US_M109_SW   = "US_M109_SW";
        public const string US_M109_W_F  = "US_M109_W_F";
        public const string US_M109_NW_F = "US_M109_NW_F";
        public const string US_M109_SW_F = "US_M109_SW_F";
        public const string US_MLRS_W    = "US_MLRS_W";
        public const string US_MLRS_NW   = "US_MLRS_NW";
        public const string US_MLRS_SW   = "US_MLRS_SW";
        public const string US_MLRS_W_F  = "US_MLRS_W_F";
        public const string US_MLRS_NW_F = "US_MLRS_NW_F";
        public const string US_MLRS_SW_F = "US_MLRS_SW_F";

        // US Anti-Aircraft
        public const string US_Chaparral_W    = "US_Chaparral_W";
        public const string US_Chaparral_NW   = "US_Chaparral_NW";
        public const string US_Chaparral_SW   = "US_Chaparral_SW";
        public const string US_Chaparral_W_F  = "US_Chaparral_W_F";
        public const string US_Chaparral_NW_F = "US_Chaparral_NW_F";
        public const string US_Chaparral_SW_F = "US_Chaparral_SW_F";
        public const string US_M163_W    = "US_M163_W";
        public const string US_M163_NW   = "US_M163_NW";
        public const string US_M163_SW   = "US_M163_SW";
        public const string US_M163_W_F  = "US_M163_W_F";
        public const string US_M163_NW_F = "US_M163_NW_F";
        public const string US_M163_SW_F = "US_M163_SW_F";

        // US SAM Systems
        public const string US_Hawk = "US_Hawk";

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
        public const string US_E3   = "US_E3";
        public const string US_A10  = "US_A10";
        public const string US_F111 = "US_F111";
        public const string US_F117 = "US_F117";
        public const string US_F14  = "US_F14";
        public const string US_F15  = "US_F15";
        public const string US_F16  = "US_F16";
        public const string US_F4   = "US_F4";
        public const string US_SR71 = "US_SR71";

        // UK Personnel & Support
        public const string UK_Airborne = "UK_Airborne";
        public const string UK_Regulars = "UK_Regulars";

        // UK Vehicles
        public const string UK_FV105_W = "UK_FV105_W";
        public const string UK_FV105_NW = "UK_FV105_NW";
        public const string UK_FV105_SW = "UK_FV105_SW";
        public const string UK_Warrior_W = "UK_Warrior_W";
        public const string UK_Warrior_NW = "UK_Warrior_NW";
        public const string UK_Warrior_SW = "UK_Warrior_SW";

        // UK MBT
        public const string UK_Challenger1_W = "UK_Challenger1_W";
        public const string UK_Challenger1_NW = "UK_Challenger1_NW";
        public const string UK_Challenger1_SW = "UK_Challenger1_SW";

        // UK Artillery
        public const string UK_M109_W    = "UK_M109_W";
        public const string UK_M109_NW   = "UK_M109_NW";
        public const string UK_M109_SW   = "UK_M109_SW";
        public const string UK_M109_W_F  = "UK_M109_W_F";
        public const string UK_M109_NW_F = "UK_M109_NW_F";
        public const string UK_M109_SW_F = "UK_M109_SW_F";

        // UK Aircraft
        public const string UK_TornadoGR1 = "UK_TornadoGR1";

        // German Personnel & Support
        public const string GER_Airborne = "GER_Airborne";
        public const string GER_Regulars = "GER_Regulars";

        // German Vehicles
        public const string GE_Luchs_W = "GE_Luchs_W";
        public const string GE_Luchs_NW = "GE_Luchs_NW";
        public const string GE_Luchs_SW = "GE_Luchs_SW";
        public const string GE_Marder_W = "GE_Marder_W";
        public const string GE_Marder_NW = "GE_Marder_NW";
        public const string GE_Marder_SW = "GE_Marder_SW";

        // German MBT
        public const string GE_Leopard1_W = "GE_Leopard1_W";
        public const string GE_Leopard1_NW = "GE_Leopard1_NW";
        public const string GE_Leopard1_SW = "GE_Leopard1_SW";
        public const string GE_Leopard2_W = "GE_Leopard2_W";
        public const string GE_Leopard2_NW = "GE_Leopard2_NW";
        public const string GE_Leopard2_SW = "GE_Leopard2_SW";

        // German Artillery
        public const string GE_M109_W    = "GE_M109_W";
        public const string GE_M109_NW   = "GE_M109_NW";
        public const string GE_M109_SW   = "GE_M109_SW";
        public const string GE_M109_W_F  = "GE_M109_W_F";
        public const string GE_M109_NW_F = "GE_M109_NW_F";
        public const string GE_M109_SW_F = "GE_M109_SW_F";

        // German Anti-Aircraft
        public const string GE_Gepard_W    = "GE_Gepard_W";
        public const string GE_Gepard_NW   = "GE_Gepard_NW";
        public const string GE_Gepard_SW   = "GE_Gepard_SW";
        public const string GE_Gepard_W_F  = "GE_Gepard_W_F";
        public const string GE_Gepard_NW_F = "GE_Gepard_NW_F";
        public const string GE_Gepard_SW_F = "GE_Gepard_SW_F";

        // German Helicopters - Animated
        public const string GE_BO105_Frame0 = "GE_BO105_Frame0";
        public const string GE_BO105_Frame1 = "GE_BO105_Frame1";
        public const string GE_BO105_Frame2 = "GE_BO105_Frame2";
        public const string GE_BO105_Frame3 = "GE_BO105_Frame3";
        public const string GE_BO105_Frame4 = "GE_BO105_Frame4";
        public const string GE_BO105_Frame5 = "GE_BO105_Frame5";

        // German Aircraft
        public const string GE_Tornado = "GE_Tornado";
        public const string GE_F4      = "GE_F4";

        // French Personnel & Support
        public const string FR_Airborne = "FR_Airborne";
        public const string FR_Regulars = "FR_Regulars";

        // French Vehicles
        public const string FR_AMX30_W = "FR_AMX30_W";
        public const string FR_AMX30_NW = "FR_AMX30_NW";
        public const string FR_AMX30_SW = "FR_AMX30_SW";
        public const string FR_ERC90_W = "FR_ERC90_W";
        public const string FR_ERC90_NW = "FR_ERC90_NW";
        public const string FR_ERC90_SW = "FR_ERC90_SW";
        public const string FR_M113_W = "FR_M113_W";
        public const string FR_M113_NW = "FR_M113_NW";
        public const string FR_M113_SW = "FR_M113_SW";

        // French Anti-Aircraft
        public const string FR_Gepard_W    = "FR_Gepard_W";
        public const string FR_Gepard_NW   = "FR_Gepard_NW";
        public const string FR_Gepard_SW   = "FR_Gepard_SW";
        public const string FR_Gepard_W_F  = "FR_Gepard_W_F";
        public const string FR_Gepard_NW_F = "FR_Gepard_NW_F";
        public const string FR_Gepard_SW_F = "FR_Gepard_SW_F";

        // French SAM Systems
        public const string FR_Roland_W    = "FR_Roland_W";
        public const string FR_Roland_NW   = "FR_Roland_NW";
        public const string FR_Roland_SW   = "FR_Roland_SW";
        public const string FR_Roland_W_F  = "FR_Roland_W_F";
        public const string FR_Roland_NW_F = "FR_Roland_NW_F";
        public const string FR_Roland_SW_F = "FR_Roland_SW_F";

        // French Aircraft
        public const string FR_Jaguar     = "FR_Jaguar";
        public const string FR_Mirage2000 = "FR_Mirage2000";
        public const string FR_MirageF1   = "FR_MirageF1";

        // Mujahideen
        public const string MJ_AA = "MJ_AA";
        public const string MJ_Artillery = "MJ_Artillery";
        public const string MJ_Elite = "MJ_Elite";
        public const string MJ_Mortar = "MJ_Mortar";
        public const string MJ_Mounted = "MJ_Mounted";
        public const string MJ_Regulars = "MJ_Regulars";
        public const string MJ_RPG = "MJ_RPG";
        public const string MJ_Stinger = "MJ_Stinger";

        // NATO Generic
        public const string NATO_Regulars = "NATO_Regulars";

        // Iraq
        public const string IQ_Regulars = "IQ_Regulars";
        public const string IQ_MirageF1 = "IQ_MirageF1";

        // Iran
        public const string IR_Regulars = "IR_Regulars";

        #endregion // NATO Unit Icons

        #region Arab Unit Icons

        // Arab MBT
        public const string AR_M60_W  = "AR_M60_W";
        public const string AR_M60_NW = "AR_M60_NW";
        public const string AR_M60_SW = "AR_M60_SW";
        public const string AR_T55_W  = "AR_T55_W";
        public const string AR_T55_NW = "AR_T55_NW";
        public const string AR_T55_SW = "AR_T55_SW";

        // Arab Vehicles
        public const string AR_BMP1_W  = "AR_BMP1_W";
        public const string AR_BMP1_NW = "AR_BMP1_NW";
        public const string AR_BMP1_SW = "AR_BMP1_SW";
        public const string AR_M113_W  = "AR_M113_W";
        public const string AR_M113_NW = "AR_M113_NW";
        public const string AR_M113_SW = "AR_M113_SW";
        public const string AR_MTLB_W  = "AR_MTLB_W";
        public const string AR_MTLB_NW = "AR_MTLB_NW";
        public const string AR_MTLB_SW = "AR_MTLB_SW";
        public const string AR_Truck_W  = "AR_Truck_W";
        public const string AR_Truck_NW = "AR_Truck_NW";
        public const string AR_Truck_SW = "AR_Truck_SW";

        // Arab Artillery
        public const string AR_HeavyArt = "AR_HeavyArt";
        public const string AR_LightArt = "AR_LightArt";
        public const string AR_2S1_W    = "AR_2S1_W";
        public const string AR_2S1_NW   = "AR_2S1_NW";
        public const string AR_2S1_SW   = "AR_2S1_SW";
        public const string AR_2S1_W_F  = "AR_2S1_W_F";
        public const string AR_2S1_NW_F = "AR_2S1_NW_F";
        public const string AR_2S1_SW_F = "AR_2S1_SW_F";
        public const string AR_2K12_W    = "AR_2K12_W";
        public const string AR_2K12_NW   = "AR_2K12_NW";
        public const string AR_2K12_SW   = "AR_2K12_SW";
        public const string AR_2K12_W_F  = "AR_2K12_W_F";
        public const string AR_2K12_NW_F = "AR_2K12_NW_F";
        public const string AR_2K12_SW_F = "AR_2K12_SW_F";

        // Arab Anti-Aircraft
        public const string AR_ZSU57_W    = "AR_ZSU57_W";
        public const string AR_ZSU57_NW   = "AR_ZSU57_NW";
        public const string AR_ZSU57_SW   = "AR_ZSU57_SW";
        public const string AR_ZSU57_W_F  = "AR_ZSU57_W_F";
        public const string AR_ZSU57_NW_F = "AR_ZSU57_NW_F";
        public const string AR_ZSU57_SW_F = "AR_ZSU57_SW_F";

        // Arab Aircraft
        public const string AR_F4    = "AR_F4";
        public const string AR_F14   = "AR_F14";
        public const string AR_Mig21 = "AR_Mig21";
        public const string AR_Mig23 = "AR_Mig23";
        public const string AR_SU17  = "AR_SU17";

        #endregion // Arab Unit Icons

        #region Chinese Unit Icons

        // Chinese Personnel
        public const string CH_Infantry = "CH_Infantry";
        public const string CH_Airborne = "CH_Airborne";

        // Chinese MBT
        public const string CH_Type59_W  = "CH_Type59_W"; // T-54 equivalent
        public const string CH_Type59_NW = "CH_Type59_NW";
        public const string CH_Type59_SW = "CH_Type59_SW";
        public const string CH_Type80_W  = "CH_Type80_W"; // T-62 equivalent
        public const string CH_Type80_NW = "CH_Type80_NW";
        public const string CH_Type80_SW = "CH_Type80_SW";
        public const string CH_Type95_W  = "CH_Type95_W"; // T-80 equivalent
        public const string CH_Type95_NW = "CH_Type95_NW";
        public const string CH_Type95_SW = "CH_Type95_SW";

        // Chinese Vehicles
        public const string CH_Type63_W  = "CH_Type63_W"; // MTLB equivalent
        public const string CH_Type63_NW = "CH_Type63_NW";
        public const string CH_Type63_SW = "CH_Type63_SW";
        public const string CH_Type86_W  = "CH_Type86_W"; // BMP-1 equivalent
        public const string CH_Type86_NW = "CH_Type86_NW";
        public const string CH_Type86_SW = "CH_Type86_SW";

        // Chinese Artillery
        public const string CH_HeavyArt = "CH_HeavyArt";
        public const string CH_LightArt = "CH_LightArt";
        public const string CH_Type82_W    = "CH_Type82_W";   // 2S1 equivalent
        public const string CH_Type82_NW   = "CH_Type82_NW";
        public const string CH_Type82_SW   = "CH_Type82_SW";
        public const string CH_Type82_W_F  = "CH_Type82_W_F";
        public const string CH_Type82_NW_F = "CH_Type82_NW_F";
        public const string CH_Type82_SW_F = "CH_Type82_SW_F";
        public const string CH_PHZ89_W    = "CH_PHZ89_W";      // BM-21 equivalent
        public const string CH_PHZ89_NW   = "CH_PHZ89_NW";
        public const string CH_PHZ89_SW   = "CH_PHZ89_SW";
        public const string CH_PHZ89_W_F  = "CH_PHZ89_W_F";
        public const string CH_PHZ89_NW_F = "CH_PHZ89_NW_F";
        public const string CH_PHZ89_SW_F = "CH_PHZ89_SW_F";

        // Chinese AAA
        public const string CH_Type53_W = "CH_Type53_W";  // ZSU-57-2 equivalent
        public const string CH_Type53_NW = "CH_Type53_NW";
        public const string CH_Type53_SW = "CH_Type53_SW";
        public const string CH_Type53_W_F = "CH_Type53_W_F";
        public const string CH_Type53_NW_F = "CH_Type53_NW_F";
        public const string CH_Type53_SW_F = "CH_Type53_SW_F";

        // Chinese Anti-Aircraft 9K31 equivalent
        public const string CH_HQ7_W    = "CH_HQ7_W";
        public const string CH_HQ7_NW   = "CH_HQ7_NW";
        public const string CH_HQ7_SW   = "CH_HQ7_SW";
        public const string CH_HQ7_W_F  = "CH_HQ7_W_F";
        public const string CH_HQ7_NW_F = "CH_HQ7_NW_F";
        public const string CH_HQ7_SW_F = "CH_HQ7_SW_F";

        // Chinese Helicopters - Animated (6 frames)
        public const string CH_H9_Frame0 = "CH_H9_Frame0"; // MI-24 equivalent
        public const string CH_H9_Frame1 = "CH_H9_Frame1";
        public const string CH_H9_Frame2 = "CH_H9_Frame2";
        public const string CH_H9_Frame3 = "CH_H9_Frame3";
        public const string CH_H9_Frame4 = "CH_H9_Frame4";
        public const string CH_H9_Frame5 = "CH_H9_Frame5";

        // Chinese Aircraft
        public const string CH_H6 = "CH_H6"; // Bomber
        public const string CH_J7 = "CH_J7"; // Mig21
        public const string CH_J8 = "CH_J8"; // Mig23
        public const string CH_Q5 = "CH_Q5"; // Attack Mig19

        #endregion // Chinese Unit Icons

        #region Generic Unit Icons

        // Generic Units
        public const string GEN_AA = "GEN_AA";
        public const string GEN_Base = "GEN_Base";
        public const string GEN_Depot = "GEN_Depot";
        public const string GEN_HeavyArt = "GEN_HeavyArt";
        public const string GEN_LightArt = "GEN_LightArt";
        public const string GEN_NavalTransport = "GEN_NavalTransport";
        public const string GEN_Truck_W = "GEN_Truck_W";
        public const string GEN_Truck_NW = "GEN_Truck_NW";
        public const string GEN_Truck_SW = "GEN_Truck_SW";

        #endregion // Generic Unit Icons

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

        // Mechanized
        public const string Icon_Tank          = "Icon_TANK";
        public const string Icon_Mech          = "Icon_MECH";
        public const string Icon_Mot           = "Icon_MOT";
        public const string Icon_ArmoredCav    = "Icon_ARMCAV";

        // Infantry
        public const string Icon_Infantry      = "Icon_Infantry";
        public const string Icon_Engineer      = "Icon_ENG";
        public const string Icon_Marine        = "Icon_MAR";
        public const string Icon_ArmoredMarine = "Icon_ARMMAR";
        public const string Icon_Antitank      = "Icon_AT";
        public const string Icon_Recon         = "Icon_RECON";
        public const string Icon_Airborne      = "Icon_AB";
        public const string Icon_MechAB        = "Icon_MECHAB";
        public const string Icon_AirMobile     = "Icon_AM";
        public const string Icon_MechanizedAM  = "Icon_MECHAM";
        public const string Icon_SpecialForces = "Icon_SOF";

        // Artillery
        public const string Icon_Artillery        = "Icon_ART";
        public const string Icon_SPA              = "Icon_SPA";
        public const string Icon_RocketArtillery  = "Icon_ROC";
        public const string Icon_BallisticMissile = "Icon_BM";

        // Air defense
        public const string Icon_AAA            = "Icon_AAA";
        public const string Icon_SPAAA          = "Icon_SPAAA";
        public const string Icon_SAM            = "Icon_SAM";
        public const string Icon_SPSAM          = "Icon_SPSAM";

        // Aircraft and Helos
        public const string Icon_HELO           = "Icon_Helo";
        public const string Icon_FGT            = "Icon_FGT";
        public const string Icon_ATT            = "Icon_ATT";
        public const string Icon_BMB            = "Icon_BMB";
        public const string Icon_LargeFW        = "Icon_LARGEFW";
        public const string Icon_RCA            = "Icon_RCA";

        // Bases
        public const string Icon_Depot          = "Icon_DEPOT";
        public const string Icon_Airbase        = "Icon_AIRBASE";
        public const string Icon_HQ             = "Icon_HQ";

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

        public const string Utility_AirbaseStack0 = "AirbaseStack0";
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
