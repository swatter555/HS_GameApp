using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HammerAndSickle.Core
{
    public class Prefab_UnitPanel : MonoBehaviour
    {
        #region Singleton

        private static Prefab_UnitPanel _instance;

        /// <summary>
        /// Singleton instance with Unity-compliant lazy initialization.
        /// </summary>
        public static Prefab_UnitPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<Prefab_UnitPanel>();

                    if (_instance == null)
                    {
                        GameObject go = new("Prefab_UnitPanel");
                        _instance = go.AddComponent<Prefab_UnitPanel>();
                    }
                }
                return _instance;
            }
        }

        #endregion // Singleton

        #region Inspector Fields

        [Header("Shared Fields")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _moveActionText;
        [SerializeField] private TextMeshProUGUI _combatActionText;
        [SerializeField] private TextMeshProUGUI _deployActionText;
        [SerializeField] private TextMeshProUGUI _intelActionText;
        [SerializeField] private TextMeshProUGUI _directRangeText;
        [SerializeField] private TextMeshProUGUI _indirectRangeText;
        [SerializeField] private TextMeshProUGUI _spottingRangeText;
        [SerializeField] private TextMeshProUGUI _currentMovementText;
        [SerializeField] private TextMeshProUGUI _maxMovementText;
        [SerializeField] private Image _natoSymbol;

        [Header("Ground Unit Panel Fields")]
        [SerializeField] private Image _groundPanelImage;
        [SerializeField] private TextMeshProUGUI _hardAttackText;
        [SerializeField] private TextMeshProUGUI _hardDefenseText;
        [SerializeField] private TextMeshProUGUI _softAttackText;
        [SerializeField] private TextMeshProUGUI _softDefenseText;
        [SerializeField] private TextMeshProUGUI _airDefenseText;
        [SerializeField] private TextMeshProUGUI _airAttackText;

        [Header("Air Unit Panel Fields")]
        [SerializeField] private Image _airPanelImage;
        [SerializeField] private TextMeshProUGUI _dogfightingText;
        [SerializeField] private TextMeshProUGUI _maneuverText;
        [SerializeField] private TextMeshProUGUI _speedText;
        [SerializeField] private TextMeshProUGUI _groundAttackText;
        [SerializeField] private TextMeshProUGUI _surviveText;
        [SerializeField] private TextMeshProUGUI _ordinanceText;
        [SerializeField] private TextMeshProUGUI _stealthText;

        #endregion // Inspector Fields

        #region Initialization

        /// <summary>
        /// Initializes the unit panel by verifying that all required UI components are assigned.
        /// </summary>
        public bool Initialize()
        {
            string errorString = "";

            // Shared fields
            if (_nameText == null) errorString += "Name Text is not assigned. ";
            if (_moveActionText == null) errorString += "Move Action Text is not assigned. ";
            if (_combatActionText == null) errorString += "Combat Action Text is not assigned. ";
            if (_deployActionText == null) errorString += "Deploy Action Text is not assigned. ";
            if (_intelActionText == null) errorString += "Intel Action Text is not assigned. ";
            if (_directRangeText == null) errorString += "Direct Range Text is not assigned. ";
            if (_indirectRangeText == null) errorString += "Indirect Range Text is not assigned. ";
            if (_spottingRangeText == null) errorString += "Spotting Range Text is not assigned. ";
            if (_currentMovementText == null) errorString += "Current Movement Text is not assigned. ";
            if (_maxMovementText == null) errorString += "Max Movement Text is not assigned. ";

            // Ground panel
            if (_groundPanelImage == null) errorString += "Ground Panel Image is not assigned. ";
            if (_hardAttackText == null) errorString += "Hard Attack Text is not assigned. ";
            if (_hardDefenseText == null) errorString += "Hard Defense Text is not assigned. ";
            if (_softAttackText == null) errorString += "Soft Attack Text is not assigned. ";
            if (_softDefenseText == null) errorString += "Soft Defense Text is not assigned. ";
            if (_airDefenseText == null) errorString += "Air Defense Text is not assigned. ";
            if (_airAttackText == null) errorString += "Air Attack Text is not assigned. ";

            // Air panel
            if (_airPanelImage == null) errorString += "Air Panel Image is not assigned. ";
            if (_dogfightingText == null) errorString += "Dogfighting Text is not assigned. ";
            if (_maneuverText == null) errorString += "Maneuver Text is not assigned. ";
            if (_speedText == null) errorString += "Speed Text is not assigned. ";
            if (_groundAttackText == null) errorString += "Ground Attack Text is not assigned. ";
            if (_surviveText == null) errorString += "Survive Text is not assigned. ";
            if (_ordinanceText == null) errorString += "Ordinance Text is not assigned. ";
            if (_stealthText == null) errorString += "Stealth Text is not assigned. ";

            if (!string.IsNullOrEmpty(errorString))
            {
                Debug.LogError($"[Prefab_UnitPanel] Initialization errors found: {errorString}");
                return false;
            }

            return true;
        }

        #endregion // Initialization

        #region Unit Panel Update

        /// <summary>
        /// Updates the unit panel with data from the currently selected unit.
        /// </summary>
        public void UpdateUnitPanel()
        {
            var unit = GameDataManager.SelectedUnit;
            if (unit == null)
                return;

            bool isAir = IsFixedWingAircraft(unit.Classification);

            // Toggle ground/air panel visibility
            if (_groundPanelImage != null)
                _groundPanelImage.gameObject.SetActive(!isAir);

            if (_airPanelImage != null)
                _airPanelImage.gameObject.SetActive(isAir);

            // Shared fields
            SetText(_nameText, unit.UnitName);
            SetText(_moveActionText, FormatStat(unit.MoveActions.Current));
            SetText(_combatActionText, FormatStat(unit.CombatActions.Current));
            SetText(_deployActionText, FormatStat(unit.DeploymentActions.Current));
            SetText(_intelActionText, FormatStat(unit.IntelActions.Current));
            SetText(_currentMovementText, FormatStat(unit.MovementPoints.Current));
            SetText(_maxMovementText, FormatStat(unit.MovementPoints.Max));
            SetText(_directRangeText, FormatStat(unit.ActivePrimaryRange));
            SetText(_indirectRangeText, FormatStat(unit.ActiveIndirectRange));
            SetText(_spottingRangeText, FormatStat(unit.ActiveSpottingRange));

            // NATO symbol
            UpdateNatoSymbol(unit);

            // Combat rating data
            var rating = unit.GetCombatRatingTotal();

            if (isAir)
            {
                SetText(_dogfightingText, FormatStat(rating.Dogfighting));
                SetText(_maneuverText, FormatStat(rating.Maneuverability));
                SetText(_speedText, FormatStat(rating.TopSpeed));
                SetText(_groundAttackText, FormatStat(rating.GroundAttack));
                SetText(_surviveText, FormatStat(rating.Survivability));
                SetText(_ordinanceText, FormatStat(rating.OrdinanceLoad));
                SetText(_stealthText, FormatStat(rating.Stealth));
            }
            else
            {
                SetText(_hardAttackText, FormatStat(rating.HardAttack));
                SetText(_hardDefenseText, FormatStat(rating.HardDefense));
                SetText(_softAttackText, FormatStat(rating.SoftAttack));
                SetText(_softDefenseText, FormatStat(rating.SoftDefense));
                SetText(_airAttackText, FormatStat(rating.GroundAirAttack));
                SetText(_airDefenseText, FormatStat(rating.GroundAirDefense));
            }
        }

        #endregion // Unit Panel Update

        #region NATO Symbol

        private void UpdateNatoSymbol(CombatUnit unit)
        {
            if (_natoSymbol == null)
                return;

            string spriteName = GetNatoSpriteName(unit.Classification);
            if (spriteName != null)
            {
                var sprite = SpriteManager.GetSprite(spriteName);
                if (sprite != null)
                    _natoSymbol.sprite = sprite;
            }

            _natoSymbol.color = GetNationalityColor(unit.Nationality);
        }

        private static string GetNatoSpriteName(UnitClassification classification) => classification switch
        {
            UnitClassification.TANK   => SpriteManager.Icon_Tank,
            UnitClassification.MECH   => SpriteManager.Icon_Mech,
            UnitClassification.MOT    => SpriteManager.Icon_Mot,
            UnitClassification.CAV    => SpriteManager.Icon_ArmoredCav,
            UnitClassification.INF    => SpriteManager.Icon_Infantry,
            UnitClassification.ENG    => SpriteManager.Icon_Engineer,
            UnitClassification.MAR    => SpriteManager.Icon_Marine,
            UnitClassification.MMAR   => SpriteManager.Icon_ArmoredMarine,
            UnitClassification.AT     => SpriteManager.Icon_Antitank,
            UnitClassification.RECON  => SpriteManager.Icon_Recon,
            UnitClassification.AB     => SpriteManager.Icon_Airborne,
            UnitClassification.MAB    => SpriteManager.Icon_MechAB,
            UnitClassification.AM     => SpriteManager.Icon_AirMobile,
            UnitClassification.MAM    => SpriteManager.Icon_MechanizedAM,
            UnitClassification.SPECF  => SpriteManager.Icon_SpecialForces,
            UnitClassification.ART    => SpriteManager.Icon_Artillery,
            UnitClassification.SPA    => SpriteManager.Icon_SPA,
            UnitClassification.ROC    => SpriteManager.Icon_RocketArtillery,
            UnitClassification.BM     => SpriteManager.Icon_BallisticMissile,
            UnitClassification.AAA    => SpriteManager.Icon_AAA,
            UnitClassification.SPAAA  => SpriteManager.Icon_SPAAA,
            UnitClassification.SAM    => SpriteManager.Icon_SAM,
            UnitClassification.SPSAM  => SpriteManager.Icon_SPSAM,
            UnitClassification.HELO   => SpriteManager.Icon_HELO,
            UnitClassification.FGT    => SpriteManager.Icon_FGT,
            UnitClassification.ATT    => SpriteManager.Icon_ATT,
            UnitClassification.BMB    => SpriteManager.Icon_BMB,
            UnitClassification.RECONA => SpriteManager.Icon_RCA,
            UnitClassification.AWACS  => SpriteManager.Icon_LargeFW,
            UnitClassification.HQ     => SpriteManager.Icon_HQ,
            UnitClassification.DEPOT  => SpriteManager.Icon_Depot,
            UnitClassification.AIRB   => SpriteManager.Icon_Airbase,
            _                         => null
        };

        private static Color GetNationalityColor(Nationality nationality) => nationality switch
        {
            Nationality.USSR                                        => Color.red,
            Nationality.MJ or Nationality.IR or Nationality.IQ or
            Nationality.SAUD or Nationality.KW                     => Color.green,
            Nationality.China                                       => Color.yellow,
            _                                                       => Color.blue
        };

        #endregion // NATO Symbol

        #region Helper Methods

        /// <summary>
        /// Determines if a unit classification represents a fixed-wing aircraft.
        /// </summary>
        private bool IsFixedWingAircraft(UnitClassification classification)
        {
            return classification == UnitClassification.FGT ||
                   classification == UnitClassification.ATT ||
                   classification == UnitClassification.BMB ||
                   classification == UnitClassification.RECONA ||
                   classification == UnitClassification.AWACS;
        }

        /// <summary>
        /// Formats a float value as a rounded-up whole number string, capped at 99.
        /// </summary>
        private string FormatStat(float value)
        {
            int rounded = Mathf.CeilToInt(value);
            return Mathf.Min(rounded, 99).ToString();
        }

        /// <summary>
        /// Sets a TextMeshProUGUI text value with null safety.
        /// </summary>
        private void SetText(TextMeshProUGUI textField, string value)
        {
            if (textField != null)
                textField.text = value;
        }

        #endregion // Helper Methods
    }
}
