using HammerAndSickle.Services;
using System;
using System.Resources;
using UnityEngine;
using UnityEngine.U2D;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// This enum contains the names of all sprite atlases used in the game.
    /// </summary>
    public enum AtlasTypes
    {
        HexOutlineIcons,
        MapIcons,
        ControlIcons,
        BridgeIcons
    }

    /// <summary>
    /// This enum contains the types of sprites used in the game.
    /// </summary>
    public enum ThemedSpriteTypes
    {
        Nameplate,
        Fort,
        MajorCity,
        MinorCity,
        Airbase
    }

    /// <summary>
    /// This enum contains the map themes used in the game.
    /// </summary>
    public enum MapTheme
    {
        MiddleEast,
        Europe,
        China
    }

    public class SpriteManager : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(SpriteManager);

        #region Constants

        // Hex outline colors.
        public const string BlackHexOutline  = "BlackHexOutline";
        public const string WhiteHexOutline  = "WhiteHexOutline";
        public const string GreyHexOutline   = "GreyHexOutline";
        public const string HexSelectOutline = "HexSelectOutline";

        // Middle East themed map icons.
        public const string ME_Nameplate = "ME_Nameplate";
        public const string ME_Fort      = "ME_Fort";
        public const string ME_MajorCity = "ME_MajorCity";
        public const string ME_MinorCity = "ME_MinorCity";
        public const string ME_Airbase   = "ME_Airbase";

        // Europe themed map icons.
        public const string EU_Nameplate = "EU_Nameplate";
        public const string EU_Fort      = "EU_Fort";
        public const string EU_MajorCity = "EU_MajorCity";
        public const string EU_MinorCity = "EU_MinorCity";
        public const string EU_Airbase   = "EU_Airbase";

        // China themed map icons.
        public const string CH_Nameplate = "CH_Nameplate";
        public const string CH_Fort      = "CH_Fort";
        public const string CH_MajorCity = "CH_MajorCity";
        public const string CH_MinorCity = "CH_MinorCity";
        public const string CH_Airbase   = "CH_Airbase";

        // Control icons.
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

        // Bridge icons.
        public const string BridgeW  = "Bridge_W";
        public const string BridgeE  = "Bridge_E";
        public const string BridgeNW = "Bridge_NW";
        public const string BridgeNE = "Bridge_NE";
        public const string BridgeSW = "Bridge_SW";
        public const string BridgeSE = "Bridge_SE";

        // Damaged bridge icons.
        public const string DamagedBridgeW  = "DamagedBridge_W";
        public const string DamagedBridgeE  = "DamagedBridge_E";
        public const string DamagedBridgeNW = "DamagedBridge_NW";
        public const string DamagedBridgeNE = "DamagedBridge_NE";
        public const string DamagedBridgeSW = "DamagedBridge_SW";
        public const string DamagedBridgeSE = "DamagedBridge_SE";

        // Pontoon bridge icons.
        public const string PontBridgeW  = "Pont_W";
        public const string PontBridgeE  = "Pont_E";
        public const string PontBridgeNW = "Pont_NW";
        public const string PontBridgeNE = "Pont_NE";
        public const string PontBridgeSW = "Pont_SW";
        public const string PontBridgeSE = "Pont_SE";

        // Parameter used with themed sprites.
        public const string VoidSpriteName = "None";

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

        [Header("Prefabs")]
        [SerializeField] private GameObject _cityPrefab;
        [SerializeField] private GameObject _mapIconPrefab;
        [SerializeField] private GameObject _bridgeIconPrefab;
        [SerializeField] private GameObject _mapTextPrefab;

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
        /// Retrieves a sprite from the specified atlas type and sprite name. Validates the inputs.
        /// </summary>
        public Sprite GetSprite(AtlasTypes atlasType, string spriteName)
        {
            try
            {
                if (atlasType == AtlasTypes.HexOutlineIcons)
                {
                    return spriteName switch
                    {
                        BlackHexOutline => _hexIconAtlas.GetSprite(BlackHexOutline),
                        WhiteHexOutline => _hexIconAtlas.GetSprite(WhiteHexOutline),
                        GreyHexOutline => _hexIconAtlas.GetSprite(GreyHexOutline),
                        HexSelectOutline => _hexIconAtlas.GetSprite(HexSelectOutline),
                        _ => throw new ArgumentException($"{CLASS_NAME}.GetSprite: Invalid sprite name '{spriteName}' for atlas type '{atlasType}'.")
                    };
                }
                else if (atlasType == AtlasTypes.ControlIcons)
                {
                    return spriteName switch
                    {
                        Control_SV => _controlIconAtlas.GetSprite(Control_SV),
                        Control_BE => _controlIconAtlas.GetSprite(Control_BE),
                        Control_DE => _controlIconAtlas.GetSprite(Control_DE),
                        Control_FR => _controlIconAtlas.GetSprite(Control_FR),
                        Control_GE => _controlIconAtlas.GetSprite(Control_GE),
                        Control_MJ => _controlIconAtlas.GetSprite(Control_MJ),
                        Control_US => _controlIconAtlas.GetSprite(Control_US),
                        Control_NE => _controlIconAtlas.GetSprite(Control_NE),
                        Control_UK => _controlIconAtlas.GetSprite(Control_UK),
                        Control_China => _controlIconAtlas.GetSprite(Control_China),
                        Control_Iran => _controlIconAtlas.GetSprite(Control_Iran),
                        Control_Iraq => _controlIconAtlas.GetSprite(Control_Iraq),
                        Control_Kuwait => _controlIconAtlas.GetSprite(Control_Kuwait),
                        Control_Saudi => _controlIconAtlas.GetSprite(Control_Saudi),
                        Control_None => _controlIconAtlas.GetSprite(Control_None),
                        _ => throw new ArgumentException($"{CLASS_NAME}.GetSprite: Invalid sprite name '{spriteName}' for atlas type '{atlasType}'.")
                    };
                }
                else if (atlasType == AtlasTypes.BridgeIcons)
                {
                    return spriteName switch
                    {
                        BridgeW => _bridgeIconAtlas.GetSprite(BridgeW),
                        BridgeE => _bridgeIconAtlas.GetSprite(BridgeE),
                        BridgeNW => _bridgeIconAtlas.GetSprite(BridgeNW),
                        BridgeNE => _bridgeIconAtlas.GetSprite(BridgeNE),
                        BridgeSW => _bridgeIconAtlas.GetSprite(BridgeSW),
                        BridgeSE => _bridgeIconAtlas.GetSprite(BridgeSE),
                        DamagedBridgeW => _bridgeIconAtlas.GetSprite(DamagedBridgeW),
                        DamagedBridgeE => _bridgeIconAtlas.GetSprite(DamagedBridgeE),
                        DamagedBridgeNW => _bridgeIconAtlas.GetSprite(DamagedBridgeNW),
                        DamagedBridgeNE => _bridgeIconAtlas.GetSprite(DamagedBridgeNE),
                        DamagedBridgeSW => _bridgeIconAtlas.GetSprite(DamagedBridgeSW),
                        DamagedBridgeSE => _bridgeIconAtlas.GetSprite(DamagedBridgeSE),
                        PontBridgeW => _bridgeIconAtlas.GetSprite(PontBridgeW),
                        PontBridgeE => _bridgeIconAtlas.GetSprite(PontBridgeE),
                        PontBridgeNW => _bridgeIconAtlas.GetSprite(PontBridgeNW),
                        PontBridgeNE => _bridgeIconAtlas.GetSprite(PontBridgeNE),
                        PontBridgeSW => _bridgeIconAtlas.GetSprite(PontBridgeSW),
                        PontBridgeSE => _bridgeIconAtlas.GetSprite(PontBridgeSE),
                        _ => throw new ArgumentException($"{CLASS_NAME}.GetSprite: Invalid sprite name '{spriteName}' for atlas type '{atlasType}'.")
                    };
                }
                else throw new ArgumentException($"{CLASS_NAME}.GetSprite: Invalid atlas type '{atlasType}'.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetSprite", e);
                return null;  // Return null when an error occurs.
            }
        }

        /// <summary>
        /// Retrieves a themed sprite based on the map theme and sprite type. Validates the inputs.
        /// </summary>
        public Sprite GetThemedSprite(MapTheme theme, ThemedSpriteTypes spriteType)
        {
            try
            {
                // Choice themed sprite based on the map theme and sprite type.
                if (theme == MapTheme.MiddleEast)
                {
                    return spriteType switch
                    {
                        ThemedSpriteTypes.Nameplate => _bridgeIconAtlas.GetSprite(ME_Nameplate),
                        ThemedSpriteTypes.Fort => _bridgeIconAtlas.GetSprite(ME_Fort),
                        ThemedSpriteTypes.MajorCity => _bridgeIconAtlas.GetSprite(ME_MajorCity),
                        ThemedSpriteTypes.MinorCity => _bridgeIconAtlas.GetSprite(ME_MinorCity),
                        ThemedSpriteTypes.Airbase => _bridgeIconAtlas.GetSprite(ME_Airbase),
                        _ => throw new ArgumentException($"{CLASS_NAME}.GetThemedSprite: Invalid sprite type '{spriteType}' for theme '{theme}'.")
                    };
                }
                else if (theme == MapTheme.Europe)
                {
                    return spriteType switch
                    {
                        ThemedSpriteTypes.Nameplate => _bridgeIconAtlas.GetSprite(EU_Nameplate),
                        ThemedSpriteTypes.Fort => _bridgeIconAtlas.GetSprite(EU_Fort),
                        ThemedSpriteTypes.MajorCity => _bridgeIconAtlas.GetSprite(EU_MajorCity),
                        ThemedSpriteTypes.MinorCity => _bridgeIconAtlas.GetSprite(EU_MinorCity),
                        ThemedSpriteTypes.Airbase => _bridgeIconAtlas.GetSprite(EU_Airbase),
                        _ => throw new ArgumentException($"{CLASS_NAME}.GetThemedSprite: Invalid sprite type '{spriteType}' for theme '{theme}'.")
                    };
                }
                else if (theme == MapTheme.China)
                {
                    return spriteType switch
                    {
                        ThemedSpriteTypes.Nameplate => _bridgeIconAtlas.GetSprite(CH_Nameplate),
                        ThemedSpriteTypes.Fort => _bridgeIconAtlas.GetSprite(CH_Fort),
                        ThemedSpriteTypes.MajorCity => _bridgeIconAtlas.GetSprite(CH_MajorCity),
                        ThemedSpriteTypes.MinorCity => _bridgeIconAtlas.GetSprite(CH_MinorCity),
                        ThemedSpriteTypes.Airbase => _bridgeIconAtlas.GetSprite(CH_Airbase),
                        _ => throw new ArgumentException($"{CLASS_NAME}.GetThemedSprite: Invalid sprite type '{spriteType}' for theme '{theme}'.")
                    };
                }
                else throw new ArgumentException($"{CLASS_NAME}.GetThemedSprite: Invalid map theme '{theme}'.");
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetThemedSprite", e);
                return null;  // Return null when an error occurs.
            }
        }

        #endregion // Static Methods
    }
}