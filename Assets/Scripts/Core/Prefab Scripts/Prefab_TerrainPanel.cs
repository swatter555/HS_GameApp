using System.Globalization;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Displays the selected hex through separate Inspector-owned text fields and a terrain portrait.
    /// </summary>
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
        [FormerlySerializedAs("_infoText")]
        [SerializeField, InspectorName("Terrain Type")] private TextMeshProUGUI _terrainTypeText;
        [SerializeField, InspectorName("City Name")] private TextMeshProUGUI _cityNameText;
        [SerializeField, InspectorName("Hex Location")] private TextMeshProUGUI _hexLocationText;
        [SerializeField, InspectorName("Movement Cost")] private TextMeshProUGUI _movementCostText;
        [SerializeField, InspectorName("Defense Modifier")] private TextMeshProUGUI _defenseModifierText;
        [SerializeField, InspectorName("Terrain Portrait")] private UnityEngine.UI.Image _terrainImage;
        private string _displayedPortraitName;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the terrain panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";
            if (_terrainTypeText == null) errorString += "Terrain Type is not assigned. ";
            if (_cityNameText == null) errorString += "City Name is not assigned. ";
            if (_hexLocationText == null) errorString += "Hex Location is not assigned. ";
            if (_movementCostText == null) errorString += "Movement Cost is not assigned. ";
            if (_defenseModifierText == null) errorString += "Defense Modifier is not assigned. ";
            if (_terrainImage == null) errorString += "Terrain Portrait is not assigned. ";

            if (!string.IsNullOrEmpty(errorString))
            {
                Debug.LogError($"[Prefab_TerrainPanel] Initialization errors found: {errorString}");
                return false;
            }

            return true;
        }

        #endregion // Initialization

        #region Terrain Panel Update

        /// <summary>Updates each field from the selected hex; authored captions supply the labels.</summary>
        public void UpdateTerrainPanel()
        {
            var hexData = GameDataManager.SelectedHexData;
            if (GameDataManager.SelectedHex == GameDataManager.NoHexSelected || hexData == null)
            {
                Clear();
                return;
            }

            UpdateTerrainPortrait(hexData.Terrain, GameDataManager.CurrentMapTheme);
            if (_terrainTypeText != null) _terrainTypeText.text = FormatTerrainType(hexData.Terrain);
            if (_cityNameText != null) _cityNameText.text = string.IsNullOrWhiteSpace(hexData.TileLabel) ? string.Empty : hexData.TileLabel;
            if (_hexLocationText != null) _hexLocationText.text = $"{GameDataManager.SelectedHex.IntX},{GameDataManager.SelectedHex.IntY}";
            if (_movementCostText != null) _movementCostText.text = hexData.MovementCost.ToString(CultureInfo.InvariantCulture);
            if (_defenseModifierText != null) _defenseModifierText.text = GetDefenseBonus(hexData.Terrain).ToString(CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Clears all fields; the Battle HUD hides this root until a valid hex is selected.
        ///
        /// The portrait Image is DISABLED rather than just having its sprite nulled: an Image with a null
        /// sprite still draws as a solid white rectangle.
        /// </summary>
        public void Clear()
        {
            if (_terrainTypeText != null) _terrainTypeText.text = string.Empty;
            if (_cityNameText != null) _cityNameText.text = string.Empty;
            if (_hexLocationText != null) _hexLocationText.text = string.Empty;
            if (_movementCostText != null) _movementCostText.text = string.Empty;
            if (_defenseModifierText != null) _defenseModifierText.text = string.Empty;

            if (_terrainImage != null)
            {
                _terrainImage.sprite = null;
                _terrainImage.enabled = false;
            }

            _displayedPortraitName = null;
        }

        /// <summary>
        /// Updates the terrain portrait image based on terrain type and map theme.
        /// </summary>
        private void UpdateTerrainPortrait(TerrainType terrain, MapTheme theme)
        {
            if (_terrainImage == null) return;
            var spriteName = GetTerrainPortraitSpriteName(terrain, theme);
            if (_displayedPortraitName == spriteName) return;

            _displayedPortraitName = spriteName;
            var sprite = string.IsNullOrEmpty(spriteName) ? null : SpriteManager.GetSprite(spriteName);
            _terrainImage.sprite = sprite;
            _terrainImage.enabled = sprite != null;
        }

        #endregion

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
        /// Gets the defense bonus value for a terrain type.
        /// </summary>
        private int GetDefenseBonus(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Water => (int)HexDefenseBonus.Water,
                TerrainType.Clear => (int)HexDefenseBonus.Plains,
                TerrainType.Forest => (int)HexDefenseBonus.Forest,
                TerrainType.Rough => (int)HexDefenseBonus.Rough,
                TerrainType.Marsh => (int)HexDefenseBonus.Marsh,
                TerrainType.Mountains => (int)HexDefenseBonus.Mountains,
                TerrainType.MinorCity => (int)HexDefenseBonus.MinorCity,
                TerrainType.MajorCity => (int)HexDefenseBonus.MajorCity,
                _ => 0
            };
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
