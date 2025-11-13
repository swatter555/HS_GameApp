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
        public const string GEN_Depot = "GEN_Depot";
        public const string GEN_Base = "GEN_Base";

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
        public const string SV_2K22_E = "SV_2K22_E";
        public const string SV_2K22_E_P = "SV_2K22_E_P";
        public const string SV_2K22_W = "SV_2K22_W";
        public const string SV_2K22_W_P = "SV_2K22_W_P";
        public const string SV_9K31_E = "SV_9K31_E";
        public const string SV_9K31_E_P = "SV_9K31_E_P";
        public const string SV_9K31_W = "SV_9K31_W";
        public const string SV_9K31_W_P = "SV_9K31_W_P";
        public const string SV_ZSU23_E = "SV_ZSU23_E";
        public const string SV_ZSU23_W = "SV_ZSU23_W";
        public const string SV_ZSU57_E = "SV_ZSU57_E";
        public const string SV_ZSU57_W = "SV_ZSU57_W";

        // Artillery
        public const string SV_2S1_E = "SV_2S1_E";
        public const string SV_2S1_E_P = "SV_2S1_E_P";
        public const string SV_2S1_W = "SV_2S1_W";
        public const string SV_2S1_W_P = "SV_2S1_W_P";
        public const string SV_2S3_E = "SV_2S3_E";
        public const string SV_2S3_E_P = "SV_2S3_E_P";
        public const string SV_2S3_W = "SV_2S3_W";
        public const string SV_2S3_W_P = "SV_2S3_W_P";
        public const string SV_2S5_E = "SV_2S5_E";
        public const string SV_2S5_E_P = "SV_2S5_E_P";
        public const string SV_2S5_W = "SV_2S5_W";
        public const string SV_2S5_W_P = "SV_2S5_W_P";
        public const string SV_2S19_E = "SV_2S19_E";
        public const string SV_2S19_E_P = "SV_2S19_E_P";
        public const string SV_2S19_W = "SV_2S19_W";
        public const string SV_2S19_W_P = "SV_2S19_W_P";

        // Rocket Artillery
        public const string SV_BM21_E = "SV_BM21_E";
        public const string SV_BM21_E_P = "SV_BM21_E_P";
        public const string SV_BM21_W = "SV_BM21_W";
        public const string SV_BM21_W_P = "SV_BM21_W_P";
        public const string SV_BM27_E = "SV_BM27_E";
        public const string SV_BM27_E_P = "SV_BM27_E_P";
        public const string SV_BM27_W = "SV_BM27_W";
        public const string SV_BM27_W_P = "SV_BM27_W_P";
        public const string SV_BM30_E = "SV_BM30_E";
        public const string SV_BM30_E_P = "SV_BM30_E_P";
        public const string SV_BM30_W = "SV_BM30_W";
        public const string SV_BM30_W_P = "SV_BM30_W_P";

        // Infantry Fighting Vehicles
        public const string SV_BMD2_E = "SV_BMD2_E";
        public const string SV_BMD2_W = "SV_BMD2_W";
        public const string SV_BMD3_E = "SV_BMD3_E";
        public const string SV_BMD3_W = "SV_BMD3_W";
        public const string SV_BMP1_E = "SV_BMP1_E";
        public const string SV_BMP1_W = "SV_BMP1_W";
        public const string SV_BMP2_E = "SV_BMP2_E";
        public const string SV_BMP2_W = "SV_BMP2_W";
        public const string SV_BMP3_E = "SV_BMP3_E";
        public const string SV_BMP3_W = "SV_BMP3_W";

        // Reconnaissance & APCs
        public const string SV_BRDM2_E = "SV_BRDM2_E";
        public const string SV_BRDM2_W = "SV_BRDM2_W";
        public const string SV_BRDM2AT_E = "SV_BRDM2AT_E";
        public const string SV_BRDM2AT_W = "SV_BRDM2AT_W";
        public const string SV_BTR70_E = "SV_BTR70_E";
        public const string SV_BTR70_W = "SV_BTR70_W";
        public const string SV_BTR80_E = "SV_BTR80_E";
        public const string SV_BTR80_W = "SV_BTR80_W";
        public const string SV_MTLB_E = "SV_MTLB_E";
        public const string SV_MTLB_W = "SV_MTLB_W";

        // Helicopters - Animated (6 frames each)
        public const string SV_MI8_Frame0 = "SV_MI8_Frame0";
        public const string SV_MI8_Frame1 = "SV_MI8_Frame1";
        public const string SV_MI8_Frame2 = "SV_MI8_Frame2";
        public const string SV_MI8_Frame3 = "SV_MI8_Frame3";
        public const string SV_MI8_Frame4 = "SV_MI8_Frame4";
        public const string SV_MI8_Frame5 = "SV_MI8_Frame5";
        public const string SV_MI8T_Frame0 = "SV_MI8T_Frame0";
        public const string SV_MI8T_Frame1 = "SV_MI8T_Frame1";
        public const string SV_MI8T_Frame2 = "SV_MI8T_Frame2";
        public const string SV_MI8T_Frame3 = "SV_MI8T_Frame3";
        public const string SV_MI8T_Frame4 = "SV_MI8T_Frame4";
        public const string SV_MI8T_Frame5 = "SV_MI8T_Frame5";
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

        // Transport Aircraft - Animated
        public const string SV_AN8_Frame0 = "SV_AN8_Frame0";
        public const string SV_AN8_Frame1 = "SV_AN8_Frame1";
        public const string SV_AN8_Frame2 = "SV_AN8_Frame2";
        public const string SV_AN8_Frame3 = "SV_AN8_Frame3";
        public const string SV_AN8_Frame4 = "SV_AN8_Frame4";
        public const string SV_AN8_Frame5 = "SV_AN8_Frame5";

        // Fixed-Wing Aircraft
        public const string SV_AN50_E = "SV_AN50_E";
        public const string SV_AN50_W = "SV_AN50_W";
        public const string SV_MIG21_E = "SV_MIG21_E";
        public const string SV_MIG21_W = "SV_MIG21_W";
        public const string SV_MIG23_E = "SV_MIG23_E";
        public const string SV_MIG23_W = "SV_MIG23_W";
        public const string SV_MIG25_E = "SV_MIG25_E";
        public const string SV_MIG25_W = "SV_MIG25_W";
        public const string SV_MIG27_E = "SV_MIG27_E";
        public const string SV_MIG27_W = "SV_MIG27_W";
        public const string SV_MIG29_E = "SV_MIG29_E";
        public const string SV_MIG29_W = "SV_MIG29_W";
        public const string SV_MIG31_E = "SV_MIG31_E";
        public const string SV_MIG31_W = "SV_MIG31_W";
        public const string SV_SU24_E = "SV_SU24_E";
        public const string SV_SU24_W = "SV_SU24_W";
        public const string SV_SU25_E = "SV_SU25_E";
        public const string SV_SU25_W = "SV_SU25_W";
        public const string SV_SU25B_E = "SV_SU25B_E";
        public const string SV_SU25B_W = "SV_SU25B_W";
        public const string SV_SU27_E = "SV_SU27_E";
        public const string SV_SU27_W = "SV_SU27_W";
        public const string SV_SU47_E = "SV_SU47_E";
        public const string SV_SU47_W = "SV_SU47_W";
        public const string SV_TU16_E = "SV_TU16_E";
        public const string SV_TU16_W = "SV_TU16_W";
        public const string SV_TU22_E = "SV_TU22_E";
        public const string SV_TU22_W = "SV_TU22_W";
        public const string SV_TU22M3_E = "SV_TU22M3_E";
        public const string SV_TU22M3_W = "SV_TU22M3_W";

        // Tanks
        public const string SV_T55A_E = "SV_T55A_E";
        public const string SV_T55A_W = "SV_T55A_W";
        public const string SV_T64A_E = "SV_T64A_E";
        public const string SV_T64A_W = "SV_T64A_W";
        public const string SV_T64B_E = "SV_T64B_E";
        public const string SV_T64B_W = "SV_T64B_W";
        public const string SV_T72A_E = "SV_T72A_E";
        public const string SV_T72A_W = "SV_T72A_W";
        public const string SV_T72B_E = "SV_T72B_E";
        public const string SV_T72B_W = "SV_T72B_W";
        public const string SV_T80B_E = "SV_T80B_E";
        public const string SV_T80B_W = "SV_T80B_W";
        public const string SV_T80BV_E = "SV_T80BV_E";
        public const string SV_T80BV_W = "SV_T80BV_W";
        public const string SV_T80U_E = "SV_T80U_E";
        public const string SV_T80U_W = "SV_T80U_W";

        // SAM Systems
        public const string SV_S75_E = "SV_S75_E";
        public const string SV_S75_W = "SV_S75_W";
        public const string SV_S125_E = "SV_S125_E";
        public const string SV_S125_W = "SV_S125_W";
        public const string SV_S300_E = "SV_S300_E";
        public const string SV_S300_E_P = "SV_S300_E_P";
        public const string SV_S300_W = "SV_S300_W";
        public const string SV_S300_W_P = "SV_S300_W_P";

        // Missiles
        public const string SV_ScudB_E = "SV_ScudB_E";
        public const string SV_ScudB_E_P = "SV_ScudB_E_P";
        public const string SV_ScudB_W = "SV_ScudB_W";
        public const string SV_ScudB_W_P = "SV_ScudB_W_P";

        // Infantry & Support
        public const string SV_Airborne = "SV_Airborne";
        public const string SV_AirMobile = "SV_AirMobile";
        public const string SV_Engineers = "SV_Engineers";
        public const string SV_Regulars = "SV_Regulars";
        public const string SV_Spetsnaz = "SV_Spetsnaz";
        public const string SV_Truck_E = "SV_Truck_E";
        public const string SV_Truck_W = "SV_Truck_W";

        #endregion // Soviet Unit Icons

        #region NATO Unit Icons

        // US Infantry & Support
        public const string US_Airborne = "US_Airborne";
        public const string US_Marines = "US_Marines";
        public const string US_Regulars = "US_Regulars";

        // US Vehicles
        public const string US_Humvee_E = "US_Humvee_E";
        public const string US_Humvee_W = "US_Humvee_W";
        public const string US_LVTP_E = "US_LVTP_E";
        public const string US_LVTP_W = "US_LVTP_W";
        public const string US_M113_E = "US_M113_E";
        public const string US_M113_W = "US_M113_W";
        public const string US_M2_E = "US_M2_E";
        public const string US_M2_W = "US_M2_W";

        // US Tanks
        public const string US_M1_E = "US_M1_E";
        public const string US_M1_W = "US_M1_W";

        // US Artillery
        public const string US_M109_E = "US_M109_E";
        public const string US_M109_E_P = "US_M109_E_P";
        public const string US_M109_W = "US_M109_W";
        public const string US_M109_W_P = "US_M109_W_P";
        public const string US_MLRS_E = "US_MLRS_E";
        public const string US_MLRS_E_P = "US_MLRS_E_P";
        public const string US_MLRS_W = "US_MLRS_W";
        public const string US_MLRS_W_P = "US_MLRS_W_P";

        // US Anti-Aircraft
        public const string US_Chap_E = "US_Chap_E";
        public const string US_Chap_E_P = "US_Chap_E_P";
        public const string US_Chap_W = "US_Chap_W";
        public const string US_Chap_W_P = "US_Chap_W_P";
        public const string US_M163_E = "US_M163_E";
        public const string US_M163_E_P = "US_M163_E_P";
        public const string US_M163_W = "US_M163_W";
        public const string US_M163_W_P = "US_M163_W_P";

        // US SAM Systems
        public const string US_Hawk_E = "US_Hawk_E";
        public const string US_Hawk_W = "US_Hawk_W";

        // US Helicopters - Animated
        public const string US_AH64_Frame0 = "US_AH64_Frame0";
        public const string US_AH64_Frame1 = "US_AH64_Frame1";
        public const string US_AH64_Frame2 = "US_AH64_Frame2";
        public const string US_AH64_Frame3 = "US_AH64_Frame3";
        public const string US_AH64_Frame4 = "US_AH64_Frame4";
        public const string US_AH64_Frame5 = "US_AH64_Frame5";

        // US Aircraft
        public const string US_A10_E = "US_A10_E";
        public const string US_A10_W = "US_A10_W";
        public const string US_F111_E = "US_F111_E";
        public const string US_F111_W = "US_F111_W";
        public const string US_F117_E = "US_F117_E";
        public const string US_F117_W = "US_F117_W";
        public const string US_F15_E = "US_F15_E";
        public const string US_F15_W = "US_F15_W";
        public const string US_F16_E = "US_F16_E";
        public const string US_F16_W = "US_F16_W";
        public const string US_F4_E = "US_F4_E";
        public const string US_F4_W = "US_F4_W";
        public const string US_SR71_E = "US_SR71_E";
        public const string US_SR71_W = "US_SR71_W";

        // UK Infantry & Support
        public const string UK_Regulars = "UK_Regulars";

        // UK Vehicles
        public const string UK_Warrior_E = "UK_Warrior_E";
        public const string UK_Warrior_W = "UK_Warrior_W";

        // UK Tanks
        public const string UK_Challenger_E = "UK_Challenger_E";
        public const string UK_Challenger_W = "UK_Challenger_W";

        // UK Artillery
        public const string UK_M109_E = "UK_M109_E";
        public const string UK_M109_E_P = "UK_M109_E_P";
        public const string UK_M109_W = "UK_M109_W";
        public const string UK_M109_W_P = "UK_M109_W_P";
        public const string UK_MLRS_E = "UK_MLRS_E";
        public const string UK_MLRS_E_P = "UK_MLRS_E_P";
        public const string UK_MLRS_W = "UK_MLRS_W";
        public const string UK_MLRS_W_P = "UK_MLRS_W_P";

        // UK Anti-Aircraft
        public const string UK_Chap_E = "UK_Chap_E";
        public const string UK_Chap_E_P = "UK_Chap_E_P";
        public const string UK_Chap_W = "UK_Chap_W";
        public const string UK_Chap_W_P = "UK_Chap_W_P";
        public const string UK_M163_E = "UK_M163_E";
        public const string UK_M163_E_P = "UK_M163_E_P";
        public const string UK_M163_W = "UK_M163_W";
        public const string UK_M163_W_P = "UK_M163_W_P";

        // German Infantry & Support
        public const string GER_Regulars = "GER_Regulars";

        // German Vehicles
        public const string GER_Marder_E = "GER_Marder_E";
        public const string GER_Marder_W = "GER_Marder_W";

        // German Tanks
        public const string GER_Leopard1_E = "GER_Leopard1_E";
        public const string GER_Leopard1_W = "GER_Leopard1_W";
        public const string GER_Leopard2_E = "GER_Leopard2_E";
        public const string GER_Leopard2_W = "GER_Leopard2_W";

        // German Artillery
        public const string GER_M109_E = "GER_M109_E";
        public const string GER_M109_E_P = "GER_M109_E_P";
        public const string GER_M109_W = "GER_M109_W";
        public const string GER_M109_W_P = "GER_M109_W_P";
        public const string GER_MLRS_E = "GER_MLRS_E";
        public const string GER_MLRS_E_P = "GER_MLRS_E_P";
        public const string GER_MLRS_W = "GER_MLRS_W";
        public const string GER_MLRS_W_P = "GER_MLRS_W_P";

        // German Anti-Aircraft
        public const string GER_Chap_E = "GER_Chap_E";
        public const string GER_Chap_E_P = "GER_Chap_E_P";
        public const string GER_Chap_W = "GER_Chap_W";
        public const string GER_Chap_W_P = "GER_Chap_W_P";
        public const string GER_Gepard_E = "GER_Gepard_E";
        public const string GER_Gepard_E_P = "GER_Gepard_E_P";
        public const string GER_Gepard_W = "GER_Gepard_W";
        public const string GER_Gepard_W_P = "GER_Gepard_W_P";

        // German Helicopters - Animated
        public const string GER_BO105_Frame0 = "GER_BO105_Frame0";
        public const string GER_BO105_Frame1 = "GER_BO105_Frame1";
        public const string GER_BO105_Frame2 = "GER_BO105_Frame2";
        public const string GER_BO105_Frame3 = "GER_BO105_Frame3";
        public const string GER_BO105_Frame4 = "GER_BO105_Frame4";
        public const string GER_BO105_Frame5 = "GER_BO105_Frame5";

        // German Aircraft
        public const string GER_Tornado_E = "GER_Tornado_E";
        public const string GER_Tornado_W = "GER_Tornado_W";

        // French Infantry & Support
        public const string FR_Regulars = "FR_Regulars";

        // French Vehicles
        public const string FRA_M113_E = "FRA_M113_E";
        public const string FRA_M113_W = "FRA_M113_W";

        // French Artillery
        public const string FRA_M109_E = "FRA_M109_E";
        public const string FRA_M109_E_P = "FRA_M109_E_P";
        public const string FRA_M109_W = "FRA_M109_W";
        public const string FRA_M109_W_P = "FRA_M109_W_P";
        public const string FRA_MLRS_E = "FRA_MLRS_E";
        public const string FRA_MLRS_E_P = "FRA_MLRS_E_P";
        public const string FRA_MLRS_W = "FRA_MLRS_W";
        public const string FRA_MLRS_W_P = "FRA_MLRS_W_P";

        // French Anti-Aircraft
        public const string FRA_Gepard_E = "FRA_Gepard_E";
        public const string FRA_Gepard_E_P = "FRA_Gepard_E_P";
        public const string FRA_Gepard_W = "FRA_Gepard_W";
        public const string FRA_Gepard_W_P = "FRA_Gepard_W_P";

        // French SAM Systems
        public const string FRA_Roland_E = "FRA_Roland_E";
        public const string FRA_Roland_E_P = "FRA_Roland_E_P";
        public const string FRA_Roland_W = "FRA_Roland_W";
        public const string FRA_Roland_W_P = "FRA_Roland_W_P";
        public const string FRA_FRAMX30_E = "FRA_FRAMX30_E";
        public const string FRA_FRAMX30_W = "FRA_FRAMX30_W";

        // French Aircraft
        public const string FRA_Jaguar_E = "FRA_Jaguar_E";
        public const string FRA_Jaguar_W = "FRA_Jaguar_W";
        public const string FRA_Mirage2000_E = "FRA_Mirage2000_E";
        public const string FRA_Mirage2000_W = "FRA_Mirage2000_W";

        // Mujahideen
        public const string MJ_Mounted = "MJ_Mounted";
        public const string MJ_Regulars = "MJ_Regulars";
        public const string MJ_Elite = "MJ_Elite";

        // Generic NATO Units
        public const string GEN_HeavyArtillery_E = "GEN_HvyArt_E";
        public const string GEN_HeavyArtillery_W = "GEN_HvyArt_W";
        public const string GEN_LightArtillery_E = "GEN_LtArt_E";
        public const string GEN_LightArtillery_W = "GEN_LtArt_W";
        public const string GEN_M30_E = "GEN_AA_E";
        public const string GEN_M30_W = "GEN_AA_W";
        public const string GEN_M31_E = "GEN_Mortar_E";
        public const string GEN_M31_W = "GEN_Mortar_W";
        public const string GEN_M35_E = "GEN_M35_E";
        public const string GEN_M35_W = "GEN_M35_W";

        #endregion // NATO Unit Icons

        #region Nationality Icons

        public const string BEIcon = "BEIcon";
        public const string CHIcon = "CHIcon";
        public const string DEIcon = "DEIcon";
        public const string FRAIcon = "FRAIcon";
        public const string GERIcon = "GERIcon";
        public const string IRIcon = "IRIcon";
        public const string IRQIcon = "IRQIcon";
        public const string KWIcon = "KWIcon";
        public const string MJIcon = "MJIcon";
        public const string NEIcon = "NEIcon";
        public const string SAIcon = "SAIcon";
        public const string SovietIcon = "SovietIcon";
        public const string UKIcon = "UKIcon";
        public const string USIcon = "USIcon";

        #endregion // Nationality Icons

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

        [Header("Prefabs")]
        [SerializeField] private GameObject _cityPrefab;
        [SerializeField] private GameObject _mapIconPrefab;
        [SerializeField] private GameObject _bridgeIconPrefab;
        [SerializeField] private GameObject _mapTextPrefab;

        [Header("Unit Icons")]
        [SerializeField] private SpriteAtlas _sovietIconAtlas;
        [SerializeField] private SpriteAtlas _natoIconAtlas;
        [SerializeField] private SpriteAtlas _genericIconAtlas;
        [SerializeField] private SpriteAtlas _nationalIconAtlas;

        #endregion

        #region Properties

        public GameObject CityPrefab => _cityPrefab;
        public GameObject MapIconPrefab => _mapIconPrefab;
        public GameObject BridgeIconPrefab => _bridgeIconPrefab;
        public GameObject MapTextPrefab => _mapTextPrefab;

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

                // Try national icon atlas
                if (Instance._nationalIconAtlas != null)
                {
                    sprite = Instance._nationalIconAtlas.GetSprite(spriteName);
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
