using System.Collections.Generic;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Reactive terrain panel: a terrain portrait image + a single formatted text block
    /// (slimmed from per-field labels 2026-07-23).
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
        [SerializeField] private Image _terrainImage;
        [SerializeField] private TextMeshProUGUI _infoText;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the terrain panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";
            if (_terrainImage == null) errorString += "Terrain Image is not assigned. ";
            if (_infoText == null) errorString += "Info Text is not assigned. ";

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
        /// Line 1: "x, y  &lt;terrain type&gt;".
        /// Line 2: location name (omitted when the hex has no label).
        /// Line 3: "Move Cost N, Defense Bonus N".
        /// Line 4: feature summary.
        /// </summary>
        public void UpdateTerrainPanel()
        {
            if (GameDataManager.SelectedHex == GameDataManager.NoHexSelected)
                return;

            var hexData = GameDataManager.SelectedHexData;
            if (hexData == null)
                return;

            UpdateTerrainPortrait(hexData.Terrain, GameDataManager.CurrentMapTheme);

            var pos = GameDataManager.SelectedHex;
            var lines = new List<string>();

            // This line only when the hex is named.
            if (!string.IsNullOrWhiteSpace(hexData.TileLabel))
                lines.Add(hexData.TileLabel);

            // Add hex type.
            lines.Add(FormatTerrainType(hexData.Terrain));

            // Add move cost and defense bonus.
            lines.Add($"MOV:{GetMoveCost(hexData.Terrain)}");
            lines.Add($"DEF:{GetDefenseBonus(hexData.Terrain)}");

            // Add the location text.
            lines.Add($"{pos.IntX},{pos.IntY}");

            // Add summary text.
            lines.Add(GenerateSummary(hexData));

            if (_infoText != null)
                _infoText.text = string.Join("\n", lines);
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
            if (sprite != null && _terrainImage != null)
                _terrainImage.sprite = sprite;
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
                TerrainType.Clear => (int)HexMovementCost.Clear,
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
            var features = new List<string>();

            if (hexData.UrbanDamage > 0) features.Add("Urban Sprawl");
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
