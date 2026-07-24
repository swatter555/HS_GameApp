// =============================================================================
// ParkedCode.cs
//
// All-purpose graveyard for code bits pulled out of active files but kept for
// later reinstatement. NOTHING HERE COMPILES — every block is commented out.
// Head each block with its origin file + date parked, so a future reinstate is
// a copy-paste back to source.
// =============================================================================


// -----------------------------------------------------------------------------
// Prefab_UnitPanel — detailed ground/air combat-stat readout + ranges
// Parked 2026-07-23 (Bob: unit panel slimmed to NATO symbol + one text field).
// Reinstate when detailed stats return (likely a modal / expanded panel).
// Was: the range fields + ground/air stat blocks + the ground/air toggle in
// UpdateUnitPanel + FormatStat + IsFixedWingAircraft. NATO symbol code stayed
// live in Prefab_UnitPanel.
// -----------------------------------------------------------------------------
/*
        // --- serialized fields ---
        [SerializeField] private TextMeshProUGUI _directRangeText;
        [SerializeField] private TextMeshProUGUI _indirectRangeText;
        [SerializeField] private TextMeshProUGUI _spottingRangeText;

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

        // --- UpdateUnitPanel body (ranges + ground/air toggle + rating block) ---
        bool isAir = IsFixedWingAircraft(unit.Classification);

        if (_groundPanelImage != null)
            _groundPanelImage.gameObject.SetActive(!isAir);
        if (_airPanelImage != null)
            _airPanelImage.gameObject.SetActive(isAir);

        SetText(_directRangeText, FormatStat(unit.ActivePrimaryRange));
        SetText(_indirectRangeText, FormatStat(unit.ActiveIndirectRange));
        SetText(_spottingRangeText, FormatStat(unit.ActiveSpottingRange));

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

        // --- helpers ---
        private bool IsFixedWingAircraft(UnitClassification classification)
        {
            return classification == UnitClassification.FGT ||
                   classification == UnitClassification.ATT ||
                   classification == UnitClassification.BMB ||
                   classification == UnitClassification.RECONA ||
                   classification == UnitClassification.AWACS;
        }

        // Rounded-up whole number, capped at 99.
        private string FormatStat(float value)
        {
            int rounded = Mathf.CeilToInt(value);
            return Mathf.Min(rounded, 99).ToString();
        }
*/


// -----------------------------------------------------------------------------
// Prefab_LeaderPanel — the entire reactive leader panel.
// Parked 2026-07-23 (Bob: leader info becomes a modal dialog; the reactive
// stacking panel is deleted). Display logic here is the reference for the modal:
// portrait, rank-board-by-grade, grade string, command ability, reputation, and
// per-tier unlocked-skill counts. Fed from GameDataManager.SelectedLeader.
// -----------------------------------------------------------------------------
/*
        [Header("Leader Fields")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Image _rankImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _gradeText;
        [SerializeField] private TextMeshProUGUI _commandText;
        [SerializeField] private TextMeshProUGUI _reputationText;
        [SerializeField] private TextMeshProUGUI _tier1Text;
        [SerializeField] private TextMeshProUGUI _tier2Text;
        [SerializeField] private TextMeshProUGUI _tier3Text;
        [SerializeField] private TextMeshProUGUI _tier4Text;
        [SerializeField] private TextMeshProUGUI _tier5Text;

        public void UpdateLeaderPanel()
        {
            var leader = GameDataManager.SelectedLeader;
            if (leader == null)
                return;

            SetImage(_portraitImage, leader.PortraitId);
            SetImage(_rankImage, GetRankBoardSpriteName(leader.CommandGrade));
            SetText(_nameText, $"{leader.FormattedRank} {leader.Name}");

            SetText(_gradeText, leader.CommandGrade switch
            {
                CommandGrade.JuniorGrade => "Junior",
                CommandGrade.SeniorGrade => "Senior",
                CommandGrade.TopGrade => "Top",
                _ => leader.CommandGrade.ToString()
            });

            SetText(_commandText, leader.CombatCommand.ToString());
            SetText(_reputationText, leader.ReputationPoints.ToString());

            SetText(_tier1Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier1).ToString());
            SetText(_tier2Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier2).ToString());
            SetText(_tier3Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier3).ToString());
            SetText(_tier4Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier4).ToString());
            SetText(_tier5Text, leader.GetUnlockedSkillCountByTier(SkillTier.Tier5).ToString());
        }

        private string GetRankBoardSpriteName(CommandGrade grade)
        {
            return grade switch
            {
                CommandGrade.JuniorGrade => SpriteManager.Colonel,
                CommandGrade.SeniorGrade => SpriteManager.MajorGeneral,
                CommandGrade.TopGrade => SpriteManager.LieutenantGeneral,
                _ => SpriteManager.Colonel
            };
        }

        private void SetImage(Image imageField, string spriteName)
        {
            if (imageField == null || string.IsNullOrEmpty(spriteName))
                return;

            var sprite = SpriteManager.GetSprite(spriteName);
            if (sprite != null)
                imageField.sprite = sprite;
        }
*/


// -----------------------------------------------------------------------------
// Prefab_UnitPanel — NATO symbol image (classification -> sprite, nationality -> tint).
// Parked 2026-07-24 (Bob: NATO symbol removed from the unit panel; will likely reappear
// on the future "graphical" panel). Was: the _natoSymbol field + UpdateNatoSymbol +
// GetNatoSpriteName + GetNationalityColor.
// -----------------------------------------------------------------------------
/*
        [SerializeField] private Image _natoSymbol;

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
*/
