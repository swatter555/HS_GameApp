using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Serialization;

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
        public const string ME_Sprawl    = "ME_Sprawl";
        public const string ME_Fort      = "ME_Fort";
        public const string ME_Airbase   = "ME_Airbase";

        // Europe
        public const string EU_Nameplate = "EU_Nameplate";
        public const string EU_MajorCity = "EU_MajorCity";
        public const string EU_MinorCity = "EU_MinorCity";
        public const string EU_Sprawl    = "EU_Sprawl";
        public const string EU_Fort      = "EU_Fort";
        public const string EU_Airbase   = "EU_Airbase";

        // China
        public const string CH_Nameplate = "CH_Nameplate";
        public const string CH_MajorCity = "CH_MajorCity";
        public const string CH_MinorCity = "CH_MinorCity";
        public const string CH_Sprawl    = "CH_Sprawl";
        public const string CH_Fort      = "CH_Fort";
        public const string CH_Airbase   = "CH_Airbase";

        // Theme-agnostic
        public const string Impassable = "Impassable";

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

        #region River Sprites

        // Each river water sprite has a paired "_Bank" companion drawn on a layer beneath. Banks
        // form the green/earth halo around water; identical art at identical position composes
        // across overlapping stamps without geometric junction logic.

        // Edge sprites (6) — one per HexDirection. Each hex draws all of its own river edges.
        // Shared edges get drawn twice (once per neighbor); identical art at identical position.
        public const string RiverEdge_NE_0      = "RiverEdge_NE_0";
        public const string RiverEdge_NE_0_Bank = "RiverEdge_NE_0_Bank";
        public const string RiverEdge_E_0       = "RiverEdge_E_0";
        public const string RiverEdge_E_0_Bank  = "RiverEdge_E_0_Bank";
        public const string RiverEdge_SE_0      = "RiverEdge_SE_0";
        public const string RiverEdge_SE_0_Bank = "RiverEdge_SE_0_Bank";
        public const string RiverEdge_SW_0      = "RiverEdge_SW_0";
        public const string RiverEdge_SW_0_Bank = "RiverEdge_SW_0_Bank";
        public const string RiverEdge_W_0       = "RiverEdge_W_0";
        public const string RiverEdge_W_0_Bank  = "RiverEdge_W_0_Bank";
        public const string RiverEdge_NW_0      = "RiverEdge_NW_0";
        public const string RiverEdge_NW_0_Bank = "RiverEdge_NW_0_Bank";

        // Terminus sprites (12) — 2 per edge direction, named by edge + endpoint corner compass.
        // count==1 at a corner: stamp terminus for the lone river edge.
        public const string RiverTerm_NE_N       = "RiverTerm_NE_N";
        public const string RiverTerm_NE_N_Bank  = "RiverTerm_NE_N_Bank";
        public const string RiverTerm_NE_NE      = "RiverTerm_NE_NE";
        public const string RiverTerm_NE_NE_Bank = "RiverTerm_NE_NE_Bank";
        public const string RiverTerm_E_NE       = "RiverTerm_E_NE";
        public const string RiverTerm_E_NE_Bank  = "RiverTerm_E_NE_Bank";
        public const string RiverTerm_E_SE       = "RiverTerm_E_SE";
        public const string RiverTerm_E_SE_Bank  = "RiverTerm_E_SE_Bank";
        public const string RiverTerm_SE_SE      = "RiverTerm_SE_SE";
        public const string RiverTerm_SE_SE_Bank = "RiverTerm_SE_SE_Bank";
        public const string RiverTerm_SE_S       = "RiverTerm_SE_S";
        public const string RiverTerm_SE_S_Bank  = "RiverTerm_SE_S_Bank";
        public const string RiverTerm_SW_S       = "RiverTerm_SW_S";
        public const string RiverTerm_SW_S_Bank  = "RiverTerm_SW_S_Bank";
        public const string RiverTerm_SW_SW      = "RiverTerm_SW_SW";
        public const string RiverTerm_SW_SW_Bank = "RiverTerm_SW_SW_Bank";
        public const string RiverTerm_W_SW       = "RiverTerm_W_SW";
        public const string RiverTerm_W_SW_Bank  = "RiverTerm_W_SW_Bank";
        public const string RiverTerm_W_NW       = "RiverTerm_W_NW";
        public const string RiverTerm_W_NW_Bank  = "RiverTerm_W_NW_Bank";
        public const string RiverTerm_NW_NW      = "RiverTerm_NW_NW";
        public const string RiverTerm_NW_NW_Bank = "RiverTerm_NW_NW_Bank";
        public const string RiverTerm_NW_N       = "RiverTerm_NW_N";
        public const string RiverTerm_NW_N_Bank  = "RiverTerm_NW_N_Bank";

        // Double junctions (6) — one per corner orientation. count==3 at a corner.
        public const string RiverDouble_N       = "RiverDouble_N";
        public const string RiverDouble_N_Bank  = "RiverDouble_N_Bank";
        public const string RiverDouble_NE      = "RiverDouble_NE";
        public const string RiverDouble_NE_Bank = "RiverDouble_NE_Bank";
        public const string RiverDouble_SE      = "RiverDouble_SE";
        public const string RiverDouble_SE_Bank = "RiverDouble_SE_Bank";
        public const string RiverDouble_S       = "RiverDouble_S";
        public const string RiverDouble_S_Bank  = "RiverDouble_S_Bank";
        public const string RiverDouble_SW      = "RiverDouble_SW";
        public const string RiverDouble_SW_Bank = "RiverDouble_SW_Bank";
        public const string RiverDouble_NW      = "RiverDouble_NW";
        public const string RiverDouble_NW_Bank = "RiverDouble_NW_Bank";

        // Single junctions (12) — 2 per corner orientation, with L/R disambiguating which inside
        // edge pairs with the outer. L = the focal-side edge that's left from the vertex looking
        // outward. count==2 with one focal + outer at a corner.
        public const string RiverSingle_N_L       = "RiverSingle_N_L";
        public const string RiverSingle_N_L_Bank  = "RiverSingle_N_L_Bank";
        public const string RiverSingle_N_R       = "RiverSingle_N_R";
        public const string RiverSingle_N_R_Bank  = "RiverSingle_N_R_Bank";
        public const string RiverSingle_NE_L      = "RiverSingle_NE_L";
        public const string RiverSingle_NE_L_Bank = "RiverSingle_NE_L_Bank";
        public const string RiverSingle_NE_R      = "RiverSingle_NE_R";
        public const string RiverSingle_NE_R_Bank = "RiverSingle_NE_R_Bank";
        public const string RiverSingle_SE_L      = "RiverSingle_SE_L";
        public const string RiverSingle_SE_L_Bank = "RiverSingle_SE_L_Bank";
        public const string RiverSingle_SE_R      = "RiverSingle_SE_R";
        public const string RiverSingle_SE_R_Bank = "RiverSingle_SE_R_Bank";
        public const string RiverSingle_S_L       = "RiverSingle_S_L";
        public const string RiverSingle_S_L_Bank  = "RiverSingle_S_L_Bank";
        public const string RiverSingle_S_R       = "RiverSingle_S_R";
        public const string RiverSingle_S_R_Bank  = "RiverSingle_S_R_Bank";
        public const string RiverSingle_SW_L      = "RiverSingle_SW_L";
        public const string RiverSingle_SW_L_Bank = "RiverSingle_SW_L_Bank";
        public const string RiverSingle_SW_R      = "RiverSingle_SW_R";
        public const string RiverSingle_SW_R_Bank = "RiverSingle_SW_R_Bank";
        public const string RiverSingle_NW_L      = "RiverSingle_NW_L";
        public const string RiverSingle_NW_L_Bank = "RiverSingle_NW_L_Bank";
        public const string RiverSingle_NW_R      = "RiverSingle_NW_R";
        public const string RiverSingle_NW_R_Bank = "RiverSingle_NW_R_Bank";

        #endregion // River Sprites

        #region Road Sprites

        // One whole-hex sprite per edge configuration. Six-bit mask Road_<NE><E><SE><SW><W><NW>.
        // 63 sprites total — Road_000000 (no road) is omitted; hexes whose IsRoad is false (or
        // whose neighbors have no IsRoad neighbors) skip stamping. The renderer derives the mask
        // at draw time from HexTile.IsRoad on the focal + 6 neighbors. Each sprite is full-canvas
        // authored to depict all that hex's road edges plus intersections; renderer just looks
        // up by mask, no rotation/flip/composition.

        // 1 edge (6)
        public const string Road_100000 = "Road_100000";
        public const string Road_010000 = "Road_010000";
        public const string Road_001000 = "Road_001000";
        public const string Road_000100 = "Road_000100";
        public const string Road_000010 = "Road_000010";
        public const string Road_000001 = "Road_000001";

        // 2 edges (15)
        public const string Road_110000 = "Road_110000";
        public const string Road_101000 = "Road_101000";
        public const string Road_100100 = "Road_100100";
        public const string Road_100010 = "Road_100010";
        public const string Road_100001 = "Road_100001";
        public const string Road_011000 = "Road_011000";
        public const string Road_010100 = "Road_010100";
        public const string Road_010010 = "Road_010010";
        public const string Road_010001 = "Road_010001";
        public const string Road_001100 = "Road_001100";
        public const string Road_001010 = "Road_001010";
        public const string Road_001001 = "Road_001001";
        public const string Road_000110 = "Road_000110";
        public const string Road_000101 = "Road_000101";
        public const string Road_000011 = "Road_000011";

        // 3 edges (20)
        public const string Road_111000 = "Road_111000";
        public const string Road_110100 = "Road_110100";
        public const string Road_110010 = "Road_110010";
        public const string Road_110001 = "Road_110001";
        public const string Road_101100 = "Road_101100";
        public const string Road_101010 = "Road_101010";
        public const string Road_101001 = "Road_101001";
        public const string Road_100110 = "Road_100110";
        public const string Road_100101 = "Road_100101";
        public const string Road_100011 = "Road_100011";
        public const string Road_011100 = "Road_011100";
        public const string Road_011010 = "Road_011010";
        public const string Road_011001 = "Road_011001";
        public const string Road_010110 = "Road_010110";
        public const string Road_010101 = "Road_010101";
        public const string Road_010011 = "Road_010011";
        public const string Road_001110 = "Road_001110";
        public const string Road_001101 = "Road_001101";
        public const string Road_001011 = "Road_001011";
        public const string Road_000111 = "Road_000111";

        // 4 edges (15)
        public const string Road_111100 = "Road_111100";
        public const string Road_111010 = "Road_111010";
        public const string Road_111001 = "Road_111001";
        public const string Road_110110 = "Road_110110";
        public const string Road_110101 = "Road_110101";
        public const string Road_110011 = "Road_110011";
        public const string Road_101110 = "Road_101110";
        public const string Road_101101 = "Road_101101";
        public const string Road_101011 = "Road_101011";
        public const string Road_100111 = "Road_100111";
        public const string Road_011110 = "Road_011110";
        public const string Road_011101 = "Road_011101";
        public const string Road_011011 = "Road_011011";
        public const string Road_010111 = "Road_010111";
        public const string Road_001111 = "Road_001111";

        // 5 edges (6)
        public const string Road_111110 = "Road_111110";
        public const string Road_111101 = "Road_111101";
        public const string Road_111011 = "Road_111011";
        public const string Road_110111 = "Road_110111";
        public const string Road_101111 = "Road_101111";
        public const string Road_011111 = "Road_011111";

        // 6 edges (1)
        public const string Road_111111 = "Road_111111";

        #endregion // Road Sprites

        #region Rank Icons

        public const string Colonel             = "Col";
        public const string ColonelGeneral      = "ColGeneral";
        public const string GeneralOfArmy       = "GenArmy";
        public const string LieutenantGeneral   = "LtGeneral";
        public const string MajorGeneral        = "MjGeneral";

        #endregion // Rank Icons

        #region Terrain Portraits

        // Water
        public const string TP_Water = "Ocean";

        // Middle East
        public const string ME_TP_Clear     = "ME_Clear";
        public const string ME_TP_Forest    = "ME_Forest";
        public const string ME_TP_Marsh     = "ME_Marsh";
        public const string ME_TP_Mountains = "ME_Mountain";
        public const string ME_TP_Rough     = "ME_Rough";
        public const string ME_TP_City      = "ME_City";
        public const string ME_TP_Town      = "ME_Town";

        // Europe
        public const string EU_TP_Clear     = "EU_Clear";
        public const string EU_TP_Forest    = "EU_Forest";
        public const string EU_TP_Marsh     = "EU_Marsh";
        public const string EU_TP_Mountains = "EU_Mountain";
        public const string EU_TP_Rough     = "EU_Rough";
        public const string EU_TP_City      = "EU_City";
        public const string EU_TP_Town      = "EU_Town";

        // China
        public const string CH_TP_Clear     = "CH_Clear";
        public const string CH_TP_Forest    = "CH_Forest";
        public const string CH_TP_Marsh     = "CH_Marsh";
        public const string CH_TP_Mountains = "CH_Mountain";
        public const string CH_TP_Rough     = "CH_Rough";
        public const string CH_TP_City      = "CH_City";
        public const string CH_TP_Town      = "CH_Town";

        #endregion // Terrain Portraits

        #region Soviet Unit Icons

        // Single top-down sprites rotate at runtime; helicopter frames keep their numbered names.
        public const string SV_2K12 = "SV_2K12";
        public const string SV_2K22 = "SV_2K22";
        public const string SV_2S1 = "SV_2S1";
        public const string SV_2S19 = "SV_2S19";
        public const string SV_2S3 = "SV_2S3";
        public const string SV_2S5 = "SV_2S5";
        public const string SV_9K31 = "SV_9K31";
        public const string SV_A50 = "SV_A50";
        public const string SV_AAA = "SV_AAA";
        public const string SV_AN12 = "SV_AN12";
        public const string SV_AirMobile = "SV_AirMobile";
        public const string SV_Airborne = "SV_Airborne";
        public const string SV_BM21 = "SV_BM21";
        public const string SV_BM27 = "SV_BM27";
        public const string SV_BM30 = "SV_BM30";
        public const string SV_BMD2 = "SV_BMD2";
        public const string SV_BMD3 = "SV_BMD3";
        public const string SV_BMP1 = "SV_BMP1";
        public const string SV_BMP2 = "SV_BMP2";
        public const string SV_BMP3 = "SV_BMP3";
        public const string SV_BRDM2 = "SV_BRDM2";
        public const string SV_BRDM2AT = "SV_BRDM2AT";
        public const string SV_BTR70 = "SV_BTR70";
        public const string SV_BTR80 = "SV_BTR80";
        public const string SV_Engineers = "SV_Engineers";
        public const string SV_HeavyArt = "SV_HeavyArt";
        public const string SV_LightArt = "SV_LightArt";
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
        public const string SV_MI8AT_Frame0 = "SV_MI8AT_Frame0";
        public const string SV_MI8AT_Frame1 = "SV_MI8AT_Frame1";
        public const string SV_MI8AT_Frame2 = "SV_MI8AT_Frame2";
        public const string SV_MI8AT_Frame3 = "SV_MI8AT_Frame3";
        public const string SV_MI8AT_Frame4 = "SV_MI8AT_Frame4";
        public const string SV_MI8AT_Frame5 = "SV_MI8AT_Frame5";
        public const string SV_MI8_Frame0 = "SV_MI8_Frame0";
        public const string SV_MI8_Frame1 = "SV_MI8_Frame1";
        public const string SV_MI8_Frame2 = "SV_MI8_Frame2";
        public const string SV_MI8_Frame3 = "SV_MI8_Frame3";
        public const string SV_MI8_Frame4 = "SV_MI8_Frame4";
        public const string SV_MI8_Frame5 = "SV_MI8_Frame5";
        public const string SV_MTLB = "SV_MTLB";
        public const string SV_Marines = "SV_Marines";
        public const string SV_Mig21 = "SV_Mig21";
        public const string SV_Mig23 = "SV_Mig23";
        public const string SV_Mig25 = "SV_Mig25";
        public const string SV_Mig25R = "SV_Mig25R";
        public const string SV_Mig27 = "SV_Mig27";
        public const string SV_Mig29 = "SV_Mig29";
        public const string SV_Mig31 = "SV_Mig31";
        public const string SV_Regulars = "SV_Regulars";
        public const string SV_S125 = "SV_S125";
        public const string SV_S300 = "SV_S300";
        public const string SV_S75 = "SV_S75";
        public const string SV_SU17 = "SV_SU17";
        public const string SV_SU24 = "SV_SU24";
        public const string SV_SU25 = "SV_SU25";
        public const string SV_SU25B = "SV_SU25B";
        public const string SV_SU27 = "SV_SU27";
        public const string SV_SU47 = "SV_SU47";
        public const string SV_ScudB = "SV_ScudB";
        public const string SV_Spetsnaz = "SV_Spetsnaz";
        public const string SV_T55A = "SV_T55A";
        public const string SV_T55MV = "SV_T55MV";
        public const string SV_T62 = "SV_T62";
        public const string SV_T62MV = "SV_T62MV";
        public const string SV_T64A = "SV_T64A";
        public const string SV_T64B = "SV_T64B";
        public const string SV_T72A = "SV_T72A";
        public const string SV_T72B = "SV_T72B";
        public const string SV_T80B = "SV_T80B";
        public const string SV_T80BVM = "SV_T80BVM";
        public const string SV_T80U = "SV_T80U";
        public const string SV_TU16 = "SV_TU16";
        public const string SV_TU22 = "SV_TU22";
        public const string SV_TU22M3 = "SV_TU22M3";
        public const string SV_Truck = "SV_Truck";
        public const string SV_ZSU23 = "SV_ZSU23";
        public const string SV_ZSU57 = "SV_ZSU57";

        #endregion

        #region NATO Unit Icons

        // Single top-down sprites rotate at runtime; helicopter frames keep their numbered names.
        public const string FR_AAA = "FR_AAA";
        public const string FR_AMX10P = "FR_AMX10P";
        public const string FR_AMX30 = "FR_AMX30";
        public const string FR_AMX30DCA = "FR_AMX30DCA";
        public const string FR_AUF1 = "FR_AUF1";
        public const string FR_AirMobile = "FR_AirMobile";
        public const string FR_Airborne = "FR_Airborne";
        public const string FR_Crotale = "FR_Crotale";
        public const string FR_ERC90 = "FR_ERC90";
        public const string FR_Gazelle_Frame0 = "FR_Gazelle_Frame0";
        public const string FR_Gazelle_Frame1 = "FR_Gazelle_Frame1";
        public const string FR_Gazelle_Frame2 = "FR_Gazelle_Frame2";
        public const string FR_Gazelle_Frame3 = "FR_Gazelle_Frame3";
        public const string FR_Gazelle_Frame4 = "FR_Gazelle_Frame4";
        public const string FR_Gazelle_Frame5 = "FR_Gazelle_Frame5";
        public const string FR_HeavyArt = "FR_HeavyArt";
        public const string FR_Jaguar = "FR_Jaguar";
        public const string FR_LightArt = "FR_LightArt";
        public const string FR_Mirage2000 = "FR_Mirage2000";
        public const string FR_MirageF1 = "FR_MirageF1";
        public const string FR_Puma_Frame0 = "FR_Puma_Frame0";
        public const string FR_Puma_Frame1 = "FR_Puma_Frame1";
        public const string FR_Puma_Frame2 = "FR_Puma_Frame2";
        public const string FR_Puma_Frame3 = "FR_Puma_Frame3";
        public const string FR_Puma_Frame4 = "FR_Puma_Frame4";
        public const string FR_Puma_Frame5 = "FR_Puma_Frame5";
        public const string FR_Regulars = "FR_Regulars";
        public const string FR_VAB = "FR_VAB";
        public const string GE_AAA = "GE_AAA";
        public const string GE_AirMobile = "GE_AirMobile";
        public const string GE_Airborne = "GE_Airborne";
        public const string GE_AlphaJet = "GE_AlphaJet";
        public const string GE_BO105_Frame0 = "GE_BO105_Frame0";
        public const string GE_BO105_Frame1 = "GE_BO105_Frame1";
        public const string GE_BO105_Frame2 = "GE_BO105_Frame2";
        public const string GE_BO105_Frame3 = "GE_BO105_Frame3";
        public const string GE_BO105_Frame4 = "GE_BO105_Frame4";
        public const string GE_BO105_Frame5 = "GE_BO105_Frame5";
        public const string GE_F4 = "GE_F4";
        public const string GE_Gepard = "GE_Gepard";
        public const string GE_HeavyArt = "GE_HeavyArt";
        public const string GE_Leopard1 = "GE_Leopard1";
        public const string GE_Leopard2 = "GE_Leopard2";
        public const string GE_LightArt = "GE_LightArt";
        public const string GE_Luchs = "GE_Luchs";
        public const string GE_M109 = "GE_M109";
        public const string GE_M113 = "GE_M113";
        public const string GE_MLRS = "GE_MLRS";
        public const string GE_Marder = "GE_Marder";
        public const string GE_Regulars = "GE_Regulars";
        public const string GE_Roland = "GE_Roland";
        public const string GE_Tornado = "GE_Tornado";
        public const string GE_UH1D_Frame0 = "GE_UH1D_Frame0";
        public const string GE_UH1D_Frame1 = "GE_UH1D_Frame1";
        public const string GE_UH1D_Frame2 = "GE_UH1D_Frame2";
        public const string GE_UH1D_Frame3 = "GE_UH1D_Frame3";
        public const string GE_UH1D_Frame4 = "GE_UH1D_Frame4";
        public const string GE_UH1D_Frame5 = "GE_UH1D_Frame5";
        public const string NATO_AAA = "NATO_AAA";
        public const string NATO_Centurion = "NATO_Centurion";
        public const string NATO_F16 = "NATO_F16";
        public const string NATO_HeavyArt = "NATO_HeavyArt";
        public const string NATO_Leopard1 = "NATO_Leopard1";
        public const string NATO_LightArt = "NATO_LightArt";
        public const string NATO_M109 = "NATO_M109";
        public const string NATO_M113 = "NATO_M113";
        public const string NATO_M113CV = "NATO_M113CV";
        public const string NATO_PRTL = "NATO_PRTL";
        public const string NATO_Regulars = "NATO_Regulars";
        public const string NATO_Truck = "NATO_Truck";
        public const string NATO_YPR765 = "NATO_YPR765";
        public const string UK_AAA = "UK_AAA";
        public const string UK_AirMobile = "UK_AirMobile";
        public const string UK_Airborne = "UK_Airborne";
        public const string UK_Challenger1 = "UK_Challenger1";
        public const string UK_Chieftain = "UK_Chieftain";
        public const string UK_F4 = "UK_F4";
        public const string UK_FV105 = "UK_FV105";
        public const string UK_FV432 = "UK_FV432";
        public const string UK_HeavyArt = "UK_HeavyArt";
        public const string UK_Jaguar = "UK_Jaguar";
        public const string UK_LightArt = "UK_LightArt";
        public const string UK_Lynx_Frame0 = "UK_Lynx_Frame0";
        public const string UK_Lynx_Frame1 = "UK_Lynx_Frame1";
        public const string UK_Lynx_Frame2 = "UK_Lynx_Frame2";
        public const string UK_Lynx_Frame3 = "UK_Lynx_Frame3";
        public const string UK_Lynx_Frame4 = "UK_Lynx_Frame4";
        public const string UK_Lynx_Frame5 = "UK_Lynx_Frame5";
        public const string UK_M109 = "UK_M109";
        public const string UK_Puma_Frame0 = "UK_Puma_Frame0";
        public const string UK_Puma_Frame1 = "UK_Puma_Frame1";
        public const string UK_Puma_Frame2 = "UK_Puma_Frame2";
        public const string UK_Puma_Frame3 = "UK_Puma_Frame3";
        public const string UK_Puma_Frame4 = "UK_Puma_Frame4";
        public const string UK_Puma_Frame5 = "UK_Puma_Frame5";
        public const string UK_Rapier = "UK_Rapier";
        public const string UK_Regulars = "UK_Regulars";
        public const string UK_Tornado = "UK_Tornado";
        public const string UK_Warrior = "UK_Warrior";
        public const string US_A10 = "US_A10";
        public const string US_AAA = "US_AAA";
        public const string US_AH1_Frame0 = "US_AH1_Frame0";
        public const string US_AH1_Frame1 = "US_AH1_Frame1";
        public const string US_AH1_Frame2 = "US_AH1_Frame2";
        public const string US_AH1_Frame3 = "US_AH1_Frame3";
        public const string US_AH1_Frame4 = "US_AH1_Frame4";
        public const string US_AH1_Frame5 = "US_AH1_Frame5";
        public const string US_AH64_Frame0 = "US_AH64_Frame0";
        public const string US_AH64_Frame1 = "US_AH64_Frame1";
        public const string US_AH64_Frame2 = "US_AH64_Frame2";
        public const string US_AH64_Frame3 = "US_AH64_Frame3";
        public const string US_AH64_Frame4 = "US_AH64_Frame4";
        public const string US_AH64_Frame5 = "US_AH64_Frame5";
        public const string US_AirMobile = "US_AirMobile";
        public const string US_Airborne = "US_Airborne";
        public const string US_Chaparral = "US_Chaparral";
        public const string US_E3 = "US_E3";
        public const string US_F111 = "US_F111";
        public const string US_F117 = "US_F117";
        public const string US_F14 = "US_F14";
        public const string US_F15 = "US_F15";
        public const string US_F16 = "US_F16";
        public const string US_F4 = "US_F4";
        public const string US_Hawk = "US_Hawk";
        public const string US_HeavyArt = "US_HeavyArt";
        public const string US_Humvee = "US_Humvee";
        public const string US_LVTP = "US_LVTP";
        public const string US_LightArt = "US_LightArt";
        public const string US_M1 = "US_M1";
        public const string US_M109 = "US_M109";
        public const string US_M113 = "US_M113";
        public const string US_M163 = "US_M163";
        public const string US_M2 = "US_M2";
        public const string US_M60 = "US_M60";
        public const string US_MLRS = "US_MLRS";
        public const string US_Marines = "US_Marines";
        public const string US_Regulars = "US_Regulars";
        public const string US_SR71 = "US_SR71";
        public const string US_Truck = "US_Truck";
        public const string US_UH1C_Frame0 = "US_UH1C_Frame0";
        public const string US_UH1C_Frame1 = "US_UH1C_Frame1";
        public const string US_UH1C_Frame2 = "US_UH1C_Frame2";
        public const string US_UH1C_Frame3 = "US_UH1C_Frame3";
        public const string US_UH1C_Frame4 = "US_UH1C_Frame4";
        public const string US_UH1C_Frame5 = "US_UH1C_Frame5";
        public const string US_UH1_Frame0 = "US_UH1_Frame0";
        public const string US_UH1_Frame1 = "US_UH1_Frame1";
        public const string US_UH1_Frame2 = "US_UH1_Frame2";
        public const string US_UH1_Frame3 = "US_UH1_Frame3";
        public const string US_UH1_Frame4 = "US_UH1_Frame4";
        public const string US_UH1_Frame5 = "US_UH1_Frame5";
        public const string US_UH60_Frame0 = "US_UH60_Frame0";
        public const string US_UH60_Frame1 = "US_UH60_Frame1";
        public const string US_UH60_Frame2 = "US_UH60_Frame2";
        public const string US_UH60_Frame3 = "US_UH60_Frame3";
        public const string US_UH60_Frame4 = "US_UH60_Frame4";
        public const string US_UH60_Frame5 = "US_UH60_Frame5";

        #endregion

        #region Regional Unit Icons

        // Single top-down sprites rotate at runtime; helicopter frames keep their numbered names.
        public const string IQ_2K12 = "IQ_2K12";
        public const string IQ_2S1 = "IQ_2S1";
        public const string IQ_AAA = "IQ_AAA";
        public const string IQ_BMP1 = "IQ_BMP1";
        public const string IQ_BRDM2 = "IQ_BRDM2";
        public const string IQ_HeavyArt = "IQ_HeavyArt";
        public const string IQ_LightArt = "IQ_LightArt";
        public const string IQ_MI8AT_Frame0 = "IQ_MI8AT_Frame0";
        public const string IQ_MI8AT_Frame1 = "IQ_MI8AT_Frame1";
        public const string IQ_MI8AT_Frame2 = "IQ_MI8AT_Frame2";
        public const string IQ_MI8AT_Frame3 = "IQ_MI8AT_Frame3";
        public const string IQ_MI8AT_Frame4 = "IQ_MI8AT_Frame4";
        public const string IQ_MI8AT_Frame5 = "IQ_MI8AT_Frame5";
        public const string IQ_MTLB = "IQ_MTLB";
        public const string IQ_Mig21 = "IQ_Mig21";
        public const string IQ_Mig23 = "IQ_Mig23";
        public const string IQ_MirageF1 = "IQ_MirageF1";
        public const string IQ_Regulars = "IQ_Regulars";
        public const string IQ_S75 = "IQ_S75";
        public const string IQ_SU17 = "IQ_SU17";
        public const string IQ_T55 = "IQ_T55";
        public const string IQ_T62 = "IQ_T62";
        public const string IQ_T72 = "IQ_T72";
        public const string IQ_Truck = "IQ_Truck";
        public const string IQ_ZSU57 = "IQ_ZSU57";
        public const string IR_AAA = "IR_AAA";
        public const string IR_AH1_Frame0 = "IR_AH1_Frame0";
        public const string IR_AH1_Frame1 = "IR_AH1_Frame1";
        public const string IR_AH1_Frame2 = "IR_AH1_Frame2";
        public const string IR_AH1_Frame3 = "IR_AH1_Frame3";
        public const string IR_AH1_Frame4 = "IR_AH1_Frame4";
        public const string IR_AH1_Frame5 = "IR_AH1_Frame5";
        public const string IR_Chieftain = "IR_Chieftain";
        public const string IR_F14 = "IR_F14";
        public const string IR_F4 = "IR_F4";
        public const string IR_F5 = "IR_F5";
        public const string IR_HeavyArt = "IR_HeavyArt";
        public const string IR_LightArt = "IR_LightArt";
        public const string IR_M109 = "IR_M109";
        public const string IR_M113 = "IR_M113";
        public const string IR_M113Recon = "IR_M113Recon";
        public const string IR_M60 = "IR_M60";
        public const string IR_Regulars = "IR_Regulars";
        public const string IR_Truck = "IR_Truck";
        public const string MJ_AAA = "MJ_AAA";
        public const string MJ_Artillery = "MJ_Artillery";
        public const string MJ_Elite = "MJ_Elite";
        public const string MJ_Mortar = "MJ_Mortar";
        public const string MJ_Mounted = "MJ_Mounted";
        public const string MJ_RPG = "MJ_RPG";
        public const string MJ_Regulars = "MJ_Regulars";
        public const string MJ_Stinger = "MJ_Stinger";
        public const string SA_AAA = "SA_AAA";
        public const string SA_AMX10P = "SA_AMX10P";
        public const string SA_AMX30 = "SA_AMX30";
        public const string SA_AUF1 = "SA_AUF1";
        public const string SA_F15 = "SA_F15";
        public const string SA_F5 = "SA_F5";
        public const string SA_Guard = "SA_Guard";
        public const string SA_HeavyArt = "SA_HeavyArt";
        public const string SA_LightArt = "SA_LightArt";
        public const string SA_M113 = "SA_M113";
        public const string SA_Regulars = "SA_Regulars";
        public const string SA_Shahine = "SA_Shahine";
        public const string SA_Truck = "SA_Truck";

        #endregion

        #region Chinese Unit Icons

        // Single top-down sprites rotate at runtime; helicopter frames keep their numbered names.
        public const string CH_AAA = "CH_AAA";
        public const string CH_Airborne = "CH_Airborne";
        public const string CH_H6 = "CH_H6";
        public const string CH_HQ2 = "CH_HQ2";
        public const string CH_HQ7 = "CH_HQ7";
        public const string CH_HeavyArt = "CH_HeavyArt";
        public const string CH_Infantry = "CH_Infantry";
        public const string CH_J6 = "CH_J6";
        public const string CH_J7 = "CH_J7";
        public const string CH_J8 = "CH_J8";
        public const string CH_LightArt = "CH_LightArt";
        public const string CH_PHZ89 = "CH_PHZ89";
        public const string CH_Q5 = "CH_Q5";
        public const string CH_Truck = "CH_Truck";
        public const string CH_Type53 = "CH_Type53";
        public const string CH_Type59 = "CH_Type59";
        public const string CH_Type62 = "CH_Type62";
        public const string CH_Type63 = "CH_Type63";
        public const string CH_Type80 = "CH_Type80";
        public const string CH_Type83 = "CH_Type83";
        public const string CH_Type86 = "CH_Type86";
        public const string CH_Z9_Frame0 = "CH_Z9_Frame0";
        public const string CH_Z9_Frame1 = "CH_Z9_Frame1";
        public const string CH_Z9_Frame2 = "CH_Z9_Frame2";
        public const string CH_Z9_Frame3 = "CH_Z9_Frame3";
        public const string CH_Z9_Frame4 = "CH_Z9_Frame4";
        public const string CH_Z9_Frame5 = "CH_Z9_Frame5";

        #endregion

        #region Generic Unit Icons

        // Single top-down sprites rotate at runtime; helicopter frames keep their numbered names.
        public const string GEN_Base = "GEN_Base";
        public const string GEN_Depot = "GEN_Depot";
        public const string GEN_NavalTransport = "GEN_NavalTransport";

        #endregion

        // Pending national support split: persisted shared profiles still use these legacy names.
        public const string GEN_LightArt = "GEN_LightArt";
        public const string GEN_HeavyArt = "GEN_HeavyArt";
        public const string AR_Truck_W = "AR_Truck_W";

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

        #region Prefab_CombatUnitIcon

        // Nationality Symbols
        public const string Symbol_BE      = "BE_Symbol";
        public const string Symbol_China   = "CH_Symbol";
        public const string Symbol_DE      = "DE_Symbol";
        public const string Symbol_FR      = "FR_Symbol";
        public const string Symbol_GE      = "GE_Symbol";
        public const string Symbol_Iran    = "IR_Symbol";
        public const string Symbol_Iraq    = "IQ_Symbol";
        public const string Symbol_Kuwait  = "KW_Symbol";
        public const string Symbol_MJ      = "MJ_Symbol";
        public const string Symbol_NE      = "NE_Symbol";
        public const string Symbol_Saudi   = "SA_Symbol";
        public const string Symbol_SV      = "SV_Symbol";
        public const string Symbol_UK      = "UK_Symbol";
        public const string Symbol_US      = "US_Symbol";
        public const string Symbol_Default = "DF_Symbol";
        // Nationality flags sized for the unit information overlay.
        public const string FlagIcon_BE = "BE_Flag_Icon";
        public const string FlagIcon_CH = "CH_Flag_Icon";
        public const string FlagIcon_DE = "DE_Flag_Icon";
        public const string FlagIcon_FR = "FR_Flag_Icon";
        public const string FlagIcon_GE = "GE_Flag_Icon";
        public const string FlagIcon_IR = "IR_Flag_Icon";
        public const string FlagIcon_IQ = "IQ_Flag_Icon";
        public const string FlagIcon_KW = "KW_Flag_Icon";
        public const string FlagIcon_MJ = "MJ_Flag_Icon";
        public const string FlagIcon_NE = "NE_Flag_Icon";
        public const string FlagIcon_SA = "SA_Flag_Icon";
        public const string FlagIcon_SV = "SV_Flag_Icon";
        public const string FlagIcon_UK = "UK_Flag_Icon";
        public const string FlagIcon_US = "US_Flag_Icon";

        // Information overlays (NATO, Soviet, Regional including China)
        public const string UnitIcon_Blue  = "UnitIcon_Blue";
        public const string UnitIcon_Red   = "UnitIcon_Red";
        public const string UnitIcon_Green = "UnitIcon_Green";

        // Deployment Status
        public const string DeployedIcon      = "Deployed_Icon";
        public const string DefensiveIcon     = "Defensive_Icon";
        public const string EntrenchedIcon    = "Entrenched_Icon";
        public const string FortifiedIcon     = "Fortified_Icon";
        public const string MountedIcon       = "Mounted_Icon";
        public const string EmbarkedAirIcon   = "Embarked_Air_Icon";
        public const string EmbarkedNavalIcon = "Embarked_Naval_Icon";

        // Deployment posture UNKNOWN (§12.2.2 / §24.3.2.2): shown on an enemy icon below SpottedLevel3,
        // where the ladder has not yet revealed the real posture. Same slot and size as the icons above.
        public const string UnknownDeploymentIcon = "Unknown_Icon";

        #endregion

        #region Utility Icons

        public const string Utility_AirbaseStack0 = "AirbaseStack_0";
        public const string Utility_AirbaseStack1 = "AirbaseStack_1";
        public const string Utility_AirbaseStack2 = "AirbaseStack_2";
        public const string Utility_AirbaseStack3 = "AirbaseStack_3";
        public const string Utility_AirbaseStack4 = "AirbaseStack_4";
        public const string Utility_AirMissionMarker = "AirMissionMarker";
        public const string Utility_StackingIconAir = "StackingIcon_AirSelect";
        public const string Utility_StackingIconLand = "StackingIcon_LandSelect";
        public const string Utility_MismatchIcon = "MismatchIcon";

        #endregion // Utility Icons

        #region Movement & Turn Overlays

        // ----------------------------------------------------------------------------
        // Movement overlays — world-space hex sprites stamped on the HexGridRenderer
        // Overlay layers (movementRange / movementPath / threat rings). Resolve via
        // _movementOverlayAtlas. Values match the packed sprite names in the "Movement
        // Overlays" atlas (= the PNG filenames in Assets/Art/Sprites/Movement Overlays/).
        // Overlay art ships as a SOLID-WHITE texture (Bob 2026-07-21, supersedes the same-day
        // as-authored/no-tint pass). HexGridRenderer applies its serialized RGB tint × per-overlay
        // opacity slider to atlas art and the procedural fallback identically.
        // HEX-SHAPED sprites (MoveRangeFill, MoveRangeZocStop, TargetPickOutline, ThreatFill_*)
        // are stamped through HexGridRenderer.FitToCellScale — the cell is a REGULAR pointy-top
        // hex, 2.56 wide × 2.956 tall, so square-canvas hex art renders ~13.5% short without it.
        // Point markers (MovePathStep/End, FacingChevrons) render authored-size. Ask Bob which
        // kind any NEW overlay sprite is at planning time (ratified 2026-07-21).
        // ----------------------------------------------------------------------------

        // Reachable-hex highlight drawn over every in-range hex (§5.10.1).
        public const string MoveRangeFill = "MoveRangeFill";

        // Reachable hex that is a ZoC-to-ZoC terminal — movement ends here (§5.6).
        public const string MoveRangeZocStop = "MoveRangeZocStop";

        // Path-preview waypoint marker for intermediate hexes on the previewed path (§5.10.3).
        public const string MovePathStep = "MovePathStep";

        // Path-preview destination marker stamped on the endpoint hex (§5.10.3).
        public const string MovePathEnd = "MovePathEnd";

        // Facing chevron for manual (Shift+click) facing rotation (§5.8) — one pre-rotated sprite per
        // HexDirection so no runtime rotation is needed. Values match HexDirection.ToString(); resolve
        // via GetFacingChevron(HexDirection).
        public const string FacingChevron_NE = "FacingChevron_NE";
        public const string FacingChevron_E  = "FacingChevron_E";
        public const string FacingChevron_SE = "FacingChevron_SE";
        public const string FacingChevron_SW = "FacingChevron_SW";
        public const string FacingChevron_W  = "FacingChevron_W";
        public const string FacingChevron_NW = "FacingChevron_NW";

        // Valid-target highlight in pick modes — leader unit-pick (§24.5.5) + AOB placement (§24.7a.1).
        public const string TargetPickOutline = "TargetPickOutline";

        // AD threat rings by GAT band (§24.7a.8): 9–11 amber / 12–14 orange-red / 15+ deep red.
        public const string ThreatFill_Amber  = "ThreatFill_Amber";
        public const string ThreatFill_Red    = "ThreatFill_Red";
        public const string ThreatFill_DeepRed = "ThreatFill_DeepRed";

        // ----------------------------------------------------------------------------
        // Weather HUD icons (optional) — screen-space status indicator. Single-state in
        // v1 (§4.5); per-turn variance is a future pass. Resolve via the EXISTING
        // _utilityIconAtlas (drop the 3 sprites into that atlas — no new atlas needed).
        // ----------------------------------------------------------------------------
        public const string Weather_Clear    = "Weather_Clear";
        public const string Weather_Overcast = "Weather_Overcast";
        public const string Weather_Storm    = "Weather_Storm";

        #endregion // Movement & Turn Overlays

        #region Officer Portraits

        // Head layers (zero-based names match the imported Sprites).
        public const string OfficerPortraitHead0 = "ui-element-head-0";
        public const string OfficerPortraitHead1 = "ui-element-head-1";
        public const string OfficerPortraitHead2 = "ui-element-head-2";
        public const string OfficerPortraitHead3 = "ui-element-head-3";
        public const string OfficerPortraitHead4 = "ui-element-head-4";
        public const string OfficerPortraitHead5 = "ui-element-head-5";
        public const string OfficerPortraitHead6 = "ui-element-head-6";
        public const string OfficerPortraitHead7 = "ui-element-head-7";
        public const string OfficerPortraitHead8 = "ui-element-head-8";
        public const string OfficerPortraitHead9 = "ui-element-head-9";
        public const string OfficerPortraitHead10 = "ui-element-head-10";
        public const string OfficerPortraitHead11 = "ui-element-head-11";
        public const string OfficerPortraitHead12 = "ui-element-head-12";
        public const string OfficerPortraitHead13 = "ui-element-head-13";
        public const string OfficerPortraitHead14 = "ui-element-head-14";
        public const string OfficerPortraitHead15 = "ui-element-head-15";
        public const string OfficerPortraitHead16 = "ui-element-head-16";
        public const string OfficerPortraitHead17 = "ui-element-head-17";
        public const string OfficerPortraitHead18 = "ui-element-head-18";
        public const string OfficerPortraitHead19 = "ui-element-head-19";

        // Uniform layers for composing officer portraits.
        public const string OfficerUniformColonel = "ui-element-col-uni";
        public const string OfficerUniformMajorGeneral = "ui-element-mj-gen-uni";
        public const string OfficerUniformLieutenantGeneral = "ui-element-lt-gen-uni";
        public const string OfficerUniformColonelGeneral = "ui-element-col-gen-uni";
        public const string OfficerUniformArmyGeneral = "ui-element-army-gen-uni";
        public const string OfficerUniformMarshal = "ui-element-marshall-uni";

        // Retained portrait identifiers used by existing leader content and persistence.
        public const string RussianPortrait01 = "Russian01";
        public const string RussianPortrait02 = "Russian02";
        public const string RussianPortrait03 = "Russian03";
        public const string RussianPortrait04 = "Russian04";
        public const string RussianPortrait05 = "Russian05";
        public const string RussianPortrait06 = "Russian06";
        public const string RussianPortrait07 = "Russian07";
        public const string RussianPortrait08 = "Russian08";
        public const string RussianPortrait09 = "Russian09";
        public const string RussianPortrait10 = "Russian10";
        public const string RussianPortrait11 = "Russian11";
        public const string RussianPortrait12 = "Russian12";
        public const string RussianPortrait13 = "Russian13";
        public const string RussianPortrait14 = "Russian14";
        public const string RussianPortrait15 = "Russian15";
        public const string RussianPortrait16 = "Russian16";
        public const string RussianPortrait17 = "Russian17";
        public const string RussianPortrait18 = "Russian18";
        public const string RussianPortrait19 = "Russian19";
        public const string RussianPortrait20 = "Russian20";
        public const string RussianPortrait21 = "Russian21";
        public const string RussianPortrait22 = "Russian22";
        public const string RussianPortrait23 = "Russian23";
        public const string RussianPortrait24 = "Russian24";
        public const string RussianPortrait25 = "Russian25";
        public const string RussianPortrait26 = "Russian26";
        public const string RussianPortrait27 = "Russian27";
        public const string RussianPortrait28 = "Russian28";
        public const string RussianPortrait29 = "Russian29";
        public const string RussianPortrait30 = "Russian30";
        public const string RussianPortrait31 = "Russian31";
        public const string RussianPortrait32 = "Russian32";
        public const string RussianPortrait33 = "Russian33";
        public const string RussianPortrait34 = "Russian34";
        public const string RussianPortrait35 = "Russian35";

        #endregion // Officer Portraits

        #endregion // Sprite Name Constants

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
        [SerializeField] private SpriteAtlas _natoSymbolIconAtlas;
        [SerializeField] private SpriteAtlas _nationalFlagAtlas;
        [SerializeField] private SpriteAtlas _nationalSymbolAtlas;
        [SerializeField] private SpriteAtlas _utilityIconAtlas;
        [SerializeField] private SpriteAtlas _unitPrefabIconAtlas;
        [SerializeField] private SpriteAtlas _sovietIconAtlas;
        [SerializeField] private SpriteAtlas _natoIconAtlas;
        [SerializeField] private SpriteAtlas _genericIconAtlas;
        [FormerlySerializedAs("_arabIconAtlas")]
        [SerializeField] private SpriteAtlas _regionalIconAtlas;
        [SerializeField] private SpriteAtlas _chineseIconAtlas;
        [SerializeField] private SpriteAtlas _officerPortraitAtlas;
        [SerializeField] private SpriteAtlas _riverIconAtlas;
        [SerializeField] private SpriteAtlas _roadIconAtlas;

        // Movement/turn overlays (range, path, ZoC, facing). Null until Bob creates the
        // atlas asset and wires it; GetSprite null-guards it like every other atlas.
        [SerializeField] private SpriteAtlas _movementOverlayAtlas;

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
        /// Resolves the pre-rotated facing chevron sprite for a HexDirection (§5.8). One sprite per
        /// direction — no runtime rotation. Returns null (with the standard GetSprite warning) if the
        /// sprite is missing from the movement-overlay atlas.
        /// </summary>
        public static Sprite GetFacingChevron(HexDirection direction) => direction switch
        {
            HexDirection.NE => GetSprite(FacingChevron_NE),
            HexDirection.E  => GetSprite(FacingChevron_E),
            HexDirection.SE => GetSprite(FacingChevron_SE),
            HexDirection.SW => GetSprite(FacingChevron_SW),
            HexDirection.W  => GetSprite(FacingChevron_W),
            HexDirection.NW => GetSprite(FacingChevron_NW),
            _               => null,
        };

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

                // Try river icon atlas
                if (Instance._riverIconAtlas != null)
                {
                    sprite = Instance._riverIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try road icon atlas
                if (Instance._roadIconAtlas != null)
                {
                    sprite = Instance._roadIconAtlas.GetSprite(spriteName);
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

                // Try Regional unit icon atlas
                if (Instance._regionalIconAtlas != null)
                {
                    sprite = Instance._regionalIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try Chinese unit icon atlas
                if (Instance._chineseIconAtlas != null)
                {
                    sprite = Instance._chineseIconAtlas.GetSprite(spriteName);
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

                // Try unit prefab icon atlas
                if (Instance._unitPrefabIconAtlas != null)
                {
                    sprite = Instance._unitPrefabIconAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try officer portrait atlas
                if (Instance._officerPortraitAtlas != null)
                {
                    sprite = Instance._officerPortraitAtlas.GetSprite(spriteName);
                    if (sprite != null) return sprite;
                }

                // Try movement/turn overlay atlas (range, path, ZoC, facing)
                if (Instance._movementOverlayAtlas != null)
                {
                    sprite = Instance._movementOverlayAtlas.GetSprite(spriteName);
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
