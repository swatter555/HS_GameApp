using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core
{
    public class Prefab_TerrainPanel : MonoBehaviour
    {
        #region Singleton

        private static Prefab_TerrainPanel _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static Prefab_TerrainPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance in scene
                    _instance = FindAnyObjectByType<Prefab_TerrainPanel>();

                    // Create new instance if none exists
                    if (_instance == null)
                    {
                        GameObject go = new("Prefab_TerrainPanel");
                        _instance = go.AddComponent<Prefab_TerrainPanel>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Inspector Fields

        [Header("Terrain Panel Fields")]
        [SerializeField] private Image _terrainImage;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _locationText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _riverBordersText;
        [SerializeField] private TextMeshProUGUI _bridgeBordersText;
        [SerializeField] private TextMeshProUGUI _moveCostText;
        [SerializeField] private TextMeshProUGUI _defenseBonusText;
        [SerializeField] private TextMeshProUGUI _summaryText;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the terrain panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";
            if (_terrainImage == null)
            {
                errorString += "Terrain Image is not assigned. ";
            }
            if (_typeText == null)
            {
                errorString += "Type Text is not assigned. ";
            }
            if (_locationText == null)
            {
                errorString += "Location Text is not assigned. ";
            }
            if (_titleText == null)
            {
                errorString += "Title Text is not assigned. ";
            }
            if (_riverBordersText == null)
            {
                errorString += "River Borders Text is not assigned. ";
            }
            if (_bridgeBordersText == null)
            {
                errorString += "Bridge Borders Text is not assigned. ";
            }
            if (_moveCostText == null)
            {
                errorString += "Move Cost Text is not assigned. ";
            }
            if (_defenseBonusText == null)
            {
                errorString += "Defense Bonus Text is not assigned. ";
            }
            if (_summaryText == null)
            {
                errorString += "Summary Text is not assigned. ";
            }

            if (!string.IsNullOrEmpty(errorString))
            {
                Debug.LogError($"[Prefab_TerrainPanel] Initialization errors found: {errorString}");
                return false;
            }

            return true;
        }

        #endregion // Initialization

        #region Terrain Panel Update

        /// <summary>
        /// Updates the terrain panel with data from the currently selected hex.
        /// </summary>
        public void UpdateTerrainPanel()
        {
            if (GameDataManager.SelectedHex == GameDataManager.NoHexSelected)
                return;

            var hexData = GameDataManager.SelectedHexData;
            if (hexData == null)
                return;

            UpdateTerrainPortrait(hexData.Terrain, GameDataManager.CurrentMapTheme);
            SetTypeText(FormatTerrainType(hexData.Terrain));
            SetLocationText(FormatPosition(GameDataManager.SelectedHex));
            SetTitleText(hexData.TileLabel);
            SetRiverBordersText(FormatBorders(hexData.RiverBorders, "Rivers: "));
            SetBridgeBordersText(FormatBridgeBorders(hexData.BridgeBorders, hexData.DamagedBridgeBorders, hexData.PontoonBridgeBorders));
            SetMoveCostText($"{GetMoveCost(hexData.Terrain)}");
            SetDefenseBonusText($"{GetDefenseBonus(hexData.Terrain)}");
            SetSummaryText(GenerateSummary(hexData));
        }

        /// <summary>
        /// Updates the terrain portrait image based on terrain type and map theme.
        /// </summary>
        private void UpdateTerrainPortrait(TerrainType terrain, MapTheme theme)
        {
            var spriteName = GetTerrainPortraitSpriteName(terrain, theme);
            if (string.IsNullOrEmpty(spriteName))
                return;

            var sprite = SpriteManager.GetSprite(spriteName);
            if (sprite != null)
                SetTerrainImage(sprite);
        }

        #endregion

        #region Terrain Panel Helper Methods

        /// <summary>
        /// Sets the terrain image sprite.
        /// </summary>
        public void SetTerrainImage(Sprite sprite)
        {
            if (_terrainImage != null)
                _terrainImage.sprite = sprite;
        }

        /// <summary>
        /// Sets the terrain type text.
        /// </summary>
        public void SetTypeText(string text)
        {
            if (_typeText != null)
                _typeText.text = text;
        }

        /// <summary>
        /// Sets the location text.
        /// </summary>
        public void SetLocationText(string text)
        {
            if (_locationText != null)
                _locationText.text = text;
        }

        /// <summary>
        /// Sets the title text.
        /// </summary>
        public void SetTitleText(string text)
        {
            if (_titleText != null)
                _titleText.text = text;
        }

        /// <summary>
        /// Sets the river borders text.
        /// </summary>
        public void SetRiverBordersText(string text)
        {
            if (_riverBordersText != null)
                _riverBordersText.text = text;
        }

        /// <summary>
        /// Sets the bridge borders text.
        /// </summary>
        public void SetBridgeBordersText(string text)
        {
            if (_bridgeBordersText != null)
                _bridgeBordersText.text = text;
        }

        /// <summary>
        /// Sets the movement cost text.
        /// </summary>
        public void SetMoveCostText(string text)
        {
            if (_moveCostText != null)
                _moveCostText.text = text;
        }

        /// <summary>
        /// Sets the defense bonus text.
        /// </summary>
        public void SetDefenseBonusText(string text)
        {
            if (_defenseBonusText != null)
                _defenseBonusText.text = text;
        }

        /// <summary>
        /// Sets the summary text.
        /// </summary>
        public void SetSummaryText(string text)
        {
            if (_summaryText != null)
                _summaryText.text = text;
        }

        #endregion // Terrain Panel Helper Methods

        #region Formatting Helpers

        /// <summary>
        /// Converts TerrainType enum to a display string.
        /// </summary>
        private string FormatTerrainType(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Water => "Water",
                TerrainType.Clear => "Clear",
                TerrainType.Forest => "Forest",
                TerrainType.Rough => "Rough",
                TerrainType.Marsh => "Marsh",
                TerrainType.Mountains => "Mountains",
                TerrainType.MinorCity => "Minor City",
                TerrainType.MajorCity => "Major City",
                TerrainType.Impassable => "Impassable",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Formats Position2D as "x, y".
        /// </summary>
        private string FormatPosition(Position2D position)
        {
            return $"{position.X}, {position.Y}";
        }

        /// <summary>
        /// Formats JSONFeatureBorders as direction list with T/F indicators.
        /// Format: "prefix NW(T),NE(F),E(T),SE(F),SW(F),W(T)"
        /// </summary>
        private string FormatBorders(JSONFeatureBorders borders, string prefix)
        {
            if (borders == null)
                return $"{prefix}NW(F),NE(F),E(F),SE(F),SW(F),W(F)";

            return $"{prefix}NW({(borders.Northwest ? "T" : "F")}),NE({(borders.Northeast ? "T" : "F")}),E({(borders.East ? "T" : "F")}),SE({(borders.Southeast ? "T" : "F")}),SW({(borders.Southwest ? "T" : "F")}),W({(borders.West ? "T" : "F")})";
        }

        /// <summary>
        /// Formats bridge borders considering all three bridge types (regular, damaged, pontoon).
        /// Shows T for functioning bridges (regular or pontoon), F for damaged or no bridges.
        /// </summary>
        private string FormatBridgeBorders(JSONFeatureBorders regularBridges, JSONFeatureBorders damagedBridges, JSONFeatureBorders pontoonBridges)
        {
            string GetBridgeStatus(bool regular, bool damaged, bool pontoon)
            {
                if (pontoon) return "T";  // Pontoon bridge is functioning
                if (regular) return "T";  // Regular bridge is functioning
                if (damaged) return "F";  // Bridge exists but is damaged/not functioning
                return "F";               // No bridge
            }

            bool regularNW = regularBridges?.Northwest ?? false;
            bool damagedNW = damagedBridges?.Northwest ?? false;
            bool pontoonNW = pontoonBridges?.Northwest ?? false;

            bool regularNE = regularBridges?.Northeast ?? false;
            bool damagedNE = damagedBridges?.Northeast ?? false;
            bool pontoonNE = pontoonBridges?.Northeast ?? false;

            bool regularE = regularBridges?.East ?? false;
            bool damagedE = damagedBridges?.East ?? false;
            bool pontoonE = pontoonBridges?.East ?? false;

            bool regularSE = regularBridges?.Southeast ?? false;
            bool damagedSE = damagedBridges?.Southeast ?? false;
            bool pontoonSE = pontoonBridges?.Southeast ?? false;

            bool regularSW = regularBridges?.Southwest ?? false;
            bool damagedSW = damagedBridges?.Southwest ?? false;
            bool pontoonSW = pontoonBridges?.Southwest ?? false;

            bool regularW = regularBridges?.West ?? false;
            bool damagedW = damagedBridges?.West ?? false;
            bool pontoonW = pontoonBridges?.West ?? false;

            return $" Bridges: NW({GetBridgeStatus(regularNW, damagedNW, pontoonNW)}),NE({GetBridgeStatus(regularNE, damagedNE, pontoonNE)}),E({GetBridgeStatus(regularE, damagedE, pontoonE)}),SE({GetBridgeStatus(regularSE, damagedSE, pontoonSE)}),SW({GetBridgeStatus(regularSW, damagedSW, pontoonSW)}),W({GetBridgeStatus(regularW, damagedW, pontoonW)})";
        }

        /// <summary>
        /// Gets the defense bonus value for a terrain type.
        /// </summary>
        private int GetDefenseBonus(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Water => 0,
                TerrainType.Clear => 0,
                TerrainType.Forest => 1,
                TerrainType.Rough => 2,
                TerrainType.Marsh => 3,
                TerrainType.Mountains => 4,
                TerrainType.MinorCity => 1,
                TerrainType.MajorCity => 3,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the movement cost for a terrain type.
        /// </summary>
        private int GetMoveCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Water => (int)HexMovementCost.Water,
                TerrainType.Clear => (int)HexMovementCost.Plains,
                TerrainType.Forest => (int)HexMovementCost.Forest,
                TerrainType.Rough => (int)HexMovementCost.Rough,
                TerrainType.Marsh => (int)HexMovementCost.Marsh,
                TerrainType.Mountains => (int)HexMovementCost.Mountains,
                TerrainType.MinorCity => (int)HexMovementCost.MinorCity,
                TerrainType.MajorCity => (int)HexMovementCost.MajorCity,
                _ => 0
            };
        }

        /// <summary>
        /// Generates a summary text for the hex.
        /// </summary>
        private string GenerateSummary(HexTile hexData)
        {
            var features = new System.Collections.Generic.List<string>();

            if (hexData.UrbanDamage > 0) features.Add($"Urban Sprawl");
            if (hexData.IsRoad) features.Add("Road");
            if (hexData.IsRail) features.Add("Rail");
            if (hexData.IsFort) features.Add("Fort");
            if (hexData.IsAirbase) features.Add("Airbase");
            if (hexData.IsObjective) features.Add("Objective");

            return features.Count > 0 ? string.Join(", ", features) : "No special features";
        }

        /// <summary>
        /// Gets the terrain portrait sprite name based on terrain type and map theme.
        /// </summary>
        private string GetTerrainPortraitSpriteName(TerrainType terrain, MapTheme theme)
        {
            // Water is theme-independent
            if (terrain == TerrainType.Water)
                return SpriteManager.TP_Water;

            // Map terrain types to themed sprite names
            return theme switch
            {
                MapTheme.MiddleEast => terrain switch
                {
                    TerrainType.Clear => SpriteManager.ME_TP_Clear,
                    TerrainType.Forest => SpriteManager.ME_TP_Forest,
                    TerrainType.Marsh => SpriteManager.ME_TP_Marsh,
                    TerrainType.Mountains => SpriteManager.ME_TP_Mountains,
                    TerrainType.Rough => SpriteManager.ME_TP_Rough,
                    TerrainType.MinorCity => SpriteManager.ME_TP_Town,
                    TerrainType.MajorCity => SpriteManager.ME_TP_City,
                    _ => null
                },
                MapTheme.Europe => terrain switch
                {
                    TerrainType.Clear => SpriteManager.EU_TP_Clear,
                    TerrainType.Forest => SpriteManager.EU_TP_Forest,
                    TerrainType.Marsh => SpriteManager.EU_TP_Marsh,
                    TerrainType.Mountains => SpriteManager.EU_TP_Mountains,
                    TerrainType.Rough => SpriteManager.EU_TP_Rough,
                    TerrainType.MinorCity => SpriteManager.EU_TP_Town,
                    TerrainType.MajorCity => SpriteManager.EU_TP_City,
                    _ => null
                },
                MapTheme.China => terrain switch
                {
                    TerrainType.Clear => SpriteManager.CH_TP_Clear,
                    TerrainType.Forest => SpriteManager.CH_TP_Forest,
                    TerrainType.Marsh => SpriteManager.CH_TP_Marsh,
                    TerrainType.Mountains => SpriteManager.CH_TP_Mountains,
                    TerrainType.Rough => SpriteManager.CH_TP_Rough,
                    TerrainType.MinorCity => SpriteManager.CH_TP_Town,
                    TerrainType.MajorCity => SpriteManager.CH_TP_City,
                    _ => null
                },
                _ => null
            };
        }

        #endregion
    }
}
