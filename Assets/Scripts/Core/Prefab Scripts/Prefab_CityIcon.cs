using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;
using HammerAndSickle.Controllers;
using System;
using TMPro;
using UnityEngine;

namespace HammerAndSickle.Core
{
    /// <summary>
    /// Manages a city prefab instance including its icon, nameplate, control flag and text.
    /// </summary>
    public class Prefab_CityIcon : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(Prefab_CityIcon);
        
        #region Inspector Fields

        [Header("Component References")]
        [SerializeField] private SpriteRenderer cityIconRenderer;
        [SerializeField] private SpriteRenderer nameplateRenderer;
        [SerializeField] private SpriteRenderer controlFlagRenderer;
        [SerializeField] private SpriteRenderer objectiveFlagRenderer;
        [SerializeField] private TextMeshPro cityNameText;

        [Header("Text Configuration")]
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Color textColor = Color.black;

        #endregion

        #region Properties

        public string CityName
        {
            get => cityNameText.text;
            set => cityNameText.text = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            ConfigureText();
            objectiveFlagRenderer.gameObject.SetActive(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the city icon based on the terrain type and map theme.
        /// </summary>
        public void UpdateCityIcon(TerrainType terrain, MapTheme theme)
        {
            try
            {
                if (terrain == TerrainType.MajorCity)
                {
                    cityIconRenderer.sprite = theme switch
                    {
                        MapTheme.MiddleEast => SpriteManager.Instance.GetThemedSprite(MapTheme.MiddleEast, ThemedSpriteTypes.MajorCity),
                        MapTheme.Europe => SpriteManager.Instance.GetThemedSprite(MapTheme.Europe, ThemedSpriteTypes.MajorCity),
                        MapTheme.China => SpriteManager.Instance.GetThemedSprite(MapTheme.China, ThemedSpriteTypes.MajorCity),
                        _ => throw new ArgumentException($"{CLASS_NAME}.UpdateCityIcon: Invalid map theme '{theme}'.")
                    };
                }
                else if (terrain == TerrainType.MinorCity)
                {
                    cityIconRenderer.sprite = theme switch
                    {
                        MapTheme.MiddleEast => SpriteManager.Instance.GetThemedSprite(MapTheme.MiddleEast, ThemedSpriteTypes.MinorCity),
                        MapTheme.Europe => SpriteManager.Instance.GetThemedSprite(MapTheme.Europe, ThemedSpriteTypes.MinorCity),
                        MapTheme.China => SpriteManager.Instance.GetThemedSprite(MapTheme.China, ThemedSpriteTypes.MinorCity),
                        _ => throw new ArgumentException($"{CLASS_NAME}.UpdateCityIcon: Invalid map theme '{theme}'.")
                    };
                }
                else
                {
                    throw new ArgumentException($"{CLASS_NAME}.UpdateCityIcon: Invalid terrain type '{terrain}'.");
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateCityIcon", e);
            }
        }

        /// <summary>
        /// Updates the nameplate based on the map theme.
        /// </summary>
        public void UpdateNameplate(MapTheme theme)
        {
            try
            {
                nameplateRenderer.sprite = theme switch
                {
                    MapTheme.MiddleEast => SpriteManager.Instance.GetThemedSprite(MapTheme.MiddleEast, ThemedSpriteTypes.Nameplate),
                    MapTheme.Europe => SpriteManager.Instance.GetThemedSprite(MapTheme.Europe, ThemedSpriteTypes.Nameplate),
                    MapTheme.China => SpriteManager.Instance.GetThemedSprite(MapTheme.China, ThemedSpriteTypes.Nameplate),
                    _ => throw new ArgumentException($"{CLASS_NAME}.UpdateNameplate: Invalid map theme '{theme}'.")
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateNameplate", e);
            }
        }

        /// <summary>
        /// Updates the control flag based on the controlling side.
        /// </summary>
        public void UpdateControlFlag(TileControl tileControl, DefaultTileControl defaultControl)
        {
            try
            {
                if (tileControl != TileControl.Red)
                {
                    controlFlagRenderer.sprite = defaultControl switch
                    {
                        DefaultTileControl.BE => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_BE),
                        DefaultTileControl.DE => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_DE),
                        DefaultTileControl.FR => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_FR),
                        DefaultTileControl.MJ => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_MJ),
                        DefaultTileControl.NE => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_NE),
                        DefaultTileControl.UK => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_UK),
                        DefaultTileControl.US => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_US),
                        DefaultTileControl.GE => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_GE),
                        DefaultTileControl.CH => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_China),
                        DefaultTileControl.IR => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_Iran),
                        DefaultTileControl.IQ => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_Iraq),
                        DefaultTileControl.SA => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_Saudi),
                        DefaultTileControl.KW => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_Kuwait),
                        DefaultTileControl.None => SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_None),
                        _ => throw new ArgumentException($"{CLASS_NAME}.UpdateControlFlag: Invalid default control '{tileControl}.{defaultControl}'.")
                    };
                }
                else
                {
                    // All red controlled tiles are always SV
                    controlFlagRenderer.sprite = SpriteManager.Instance.GetSprite(AtlasTypes.ControlIcons, SpriteManager.Control_SV);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateControlFlag", e);
            }
        }

        /// <summary>
        /// Updates the city name.
        /// </summary>
        public void UpdateCityName(string name)
        {
            try
            {
                CityName = name;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateCityName", e);
            }
        }

        /// <summary>
        /// Updates the objective flag status.
        /// </summary>
        /// <param name="isObjective"></param>
        public void UpdateObjectiveStatus(bool isObjective)
        {
            objectiveFlagRenderer.gameObject.SetActive(isObjective);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates that all required references are set.
        /// </summary>
        private void ValidateReferences()
        {
            if (cityIconRenderer == null)
                throw new System.NullReferenceException($"{CLASS_NAME}.ValidateReferences: cityIconRenderer is null");
            if (nameplateRenderer == null)
                throw new System.NullReferenceException($"{CLASS_NAME}.ValidateReferences: nameplateRenderer is null");
            if (controlFlagRenderer == null)
                throw new System.NullReferenceException($"{CLASS_NAME}.ValidateReferences: controlFlagRenderer is null");
            if (objectiveFlagRenderer == null)
                throw new System.NullReferenceException($"{CLASS_NAME}.ValidateReferences: objectiveFlagRenderer is null");
            if (cityNameText == null)
                throw new System.NullReferenceException($"{CLASS_NAME}.ValidateReferences: cityNameText is null");
            if (fontAsset == null)
                throw new System.NullReferenceException($"{CLASS_NAME}.ValidateReferences: fontAsset is null");
        }

        /// <summary>
        /// Configures the TextMeshPro component with the specified settings.
        /// </summary>
        private void ConfigureText()
        {
            cityNameText.font = fontAsset;
            cityNameText.color = textColor;
        }

        #endregion
    }
}