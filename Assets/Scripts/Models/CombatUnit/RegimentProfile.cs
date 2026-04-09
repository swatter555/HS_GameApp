using HammerAndSickle.Services;
using System;
using System.Text.Json.Serialization;
using HammerAndSickle.Core.GameData;
using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Contains the weapon system buckets for profile stats.
    /// </summary>
    public class IntelReport
    {
        #region Properties

        // Bucketted numbers for each unit type.
        public int Personnel { get; set; } = 0;
        public int TANK { get; set; } = 0;
        public int IFV { get; set; } = 0;
        public int APC { get; set; } = 0;
        public int RCN { get; set; } = 0;
        public int ART { get; set; } = 0;
        public int ROC { get; set; } = 0;
        public int SAM { get; set; } = 0;
        public int AAA { get; set; } = 0;
        public int AT { get; set; } = 0;
        public int HEL { get; set; } = 0;
        public int AWACS { get; set; } = 0;
        public int TRN { get; set; } = 0;
        public int FGT { get; set; } = 0;
        public int ATT { get; set; } = 0;
        public int BMB { get; set; } = 0;
        public int RCNA { get; set; } = 0;

        // More intel about parent unit.
        public Nationality UnitNationality = Nationality.USSR;
        public string UnitName { get; set; } = "Default";
        public DeploymentPosition DeploymentPosition { get; set; } = DeploymentPosition.Deployed;
        public ExperienceLevel UnitExperienceLevel = ExperienceLevel.Raw;
        public EfficiencyLevel UnitEfficiencyLevel = EfficiencyLevel.StaticOperations;

        #endregion // Properties
    }

    /// <summary>
    /// Maps a unit icon to its required sprite resource strings.
    /// </summary>
    public class RegimentIconProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(RegimentIconProfile);

        #endregion // Constants

        #region Properties

        public RegimentIconType IconType { get; set; }
        public string W { get; set; }
        public string NW { get; set; }
        public string SW { get; set; }
        public string W_F { get; set; }
        public string NW_F { get; set; }
        public string SW_F { get; set; }

        #endregion // Properties

        #region Constructor

        public RegimentIconProfile()
        {
            IconType = RegimentIconType.Single;
            W = string.Empty;
            NW = string.Empty;
            SW = string.Empty;
            W_F = string.Empty;
            NW_F = string.Empty;
            SW_F = string.Empty;
        }

        public RegimentIconProfile(RegimentIconType _iconType)
        {
            IconType = _iconType;
            W = string.Empty;
            NW = string.Empty;
            SW = string.Empty;
            W_F = string.Empty;
            NW_F = string.Empty;
            SW_F = string.Empty;
        }

        #endregion // Constructor

        #region Access Methods

        /// <summary>
        /// Returns the single icon sprite. Valid for all icon types.
        /// </summary>
        public string GetIcon()
        {
            return W;
        }

        /// <summary>
        /// Returns the sprite for a given facing direction (W, NW, SW).
        /// Valid for Directional and Directional_Fire icon types.
        /// </summary>
        public string GetDirectionalIcon(HexDirection direction)
        {
            try
            {
                if (IconType != RegimentIconType.Directional && IconType != RegimentIconType.Directional_Fire)
                    throw new InvalidOperationException(
                        $"Directional icons not available for icon type {IconType}");

                return direction switch
                {
                    HexDirection.W => W,
                    HexDirection.NW => NW,
                    HexDirection.SW => SW,
                    _ => throw new ArgumentException(
                        $"Invalid direction {direction}. Only W, NW, SW are valid.", nameof(direction))
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetDirectionalIcon", e);
                throw;
            }
        }

        /// <summary>
        /// Returns the firing sprite for a given facing direction (W, NW, SW).
        /// Valid only for Directional_Fire icon type.
        /// </summary>
        public string GetFiringIcon(HexDirection direction)
        {
            try
            {
                if (IconType != RegimentIconType.Directional_Fire)
                    throw new InvalidOperationException(
                        $"Firing icons not available for icon type {IconType}");

                return direction switch
                {
                    HexDirection.W => W_F,
                    HexDirection.NW => NW_F,
                    HexDirection.SW => SW_F,
                    _ => throw new ArgumentException(
                        $"Invalid direction {direction}. Only W, NW, SW are valid.", nameof(direction))
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetFiringIcon", e);
                throw;
            }
        }

        /// <summary>
        /// Returns the sprite for a given animation frame (0-5).
        /// Valid only for Helo_Animation icon type.
        /// </summary>
        public string GetAnimationFrame(int frame)
        {
            try
            {
                if (IconType != RegimentIconType.Helo_Animation)
                    throw new InvalidOperationException(
                        $"Animation frames not available for icon type {IconType}");

                return frame switch
                {
                    0 => W,
                    1 => NW,
                    2 => SW,
                    3 => W_F,
                    4 => NW_F,
                    5 => SW_F,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(frame), $"Frame must be 0-5, got {frame}")
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetAnimationFrame", e);
                throw;
            }
        }

        #endregion // Access Methods

        #region Validation

        /// <summary>
        /// Validates that required sprites are set based on the IconType.
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(W))
            {
                error = "Primary icon (W) is required for all icon types.";
                return false;
            }

            if (IconType == RegimentIconType.Directional || IconType == RegimentIconType.Directional_Fire)
            {
                if (string.IsNullOrEmpty(NW) || string.IsNullOrEmpty(SW))
                {
                    error = $"Directional icons (NW, SW) are required for {IconType}.";
                    return false;
                }
            }

            if (IconType == RegimentIconType.Directional_Fire)
            {
                if (string.IsNullOrEmpty(W_F) || string.IsNullOrEmpty(NW_F) || string.IsNullOrEmpty(SW_F))
                {
                    error = "Firing icons (W_F, NW_F, SW_F) are required for Directional_Fire.";
                    return false;
                }
            }

            if (IconType == RegimentIconType.Helo_Animation)
            {
                if (string.IsNullOrEmpty(NW) || string.IsNullOrEmpty(SW) ||
                    string.IsNullOrEmpty(W_F) || string.IsNullOrEmpty(NW_F) || string.IsNullOrEmpty(SW_F))
                {
                    error = "All six animation frames are required for Helo_Animation.";
                    return false;
                }
            }

            return true;
        }

        #endregion // Validation
    }

    /// <summary>
    /// Provides stat profiles for individual weapons.
    /// </summary>
    public class WeaponProfile
    {
        /*
         * Important notes:
         * WeaponProfile acts as both a repository of ratings for the RegimentProfile, but
         * also as repository of data for intel reports.
         *
         * =====================================================================
         * RATING DEFINITIONS (authoritative, per design 2026-04)
         * =====================================================================
         * HardAttack (HA)
         *     The ability and effectiveness to deliver high-caliber ballistic
         *     or ATGM anti-tank rounds.
         *
         * HardDefense (HD)
         *     The ability to survive incoming HardAttack fire.
         *
         * SoftAttack (SA)
         *     The ability to kill soft targets across environments.
         *
         * SoftDefense (SD)
         *     The ability to withstand weapons typically associated with
         *     infantry, including their organic anti-tank capability.
         *
         * GroundAirAttack (GAT)
         *     The ability to deliver anti-air capability at long range.
         *     Think dedicated SAM batteries meant to control large amounts of
         *     airspace and interdict air operations in an area. This is the
         *     INTERDICTION stat — it reaches beyond the owning hex.
         *
         * GroundAirDefense (GAD)
         *     The inherent capability of a ground unit to defend the hex it is
         *     in from air attack and inflict losses. Strictly local: GAD never
         *     counts outside the hex the ground unit occupies. Defensive only,
         *     non-interdiction.
         *
         * Dogfighting (DF)
         *     The ability to shoot down enemy aircraft effectively. Includes
         *     radar targeting effectiveness and the lethality of onboard guns
         *     and missiles.
         *
         * Maneuverability (MAN)
         *     Raw agility of the aircraft — used to avoid damage and to get
         *     into a position to win a dogfight.
         *
         * TopSpeed (TS)
         *     The ability to intercept and to escape enemy aircraft.
         *
         * Survivability (SUR)
         *     The aircraft's ability to take physical damage, and also its
         *     electronic defense capabilities. An AWACS has relatively high
         *     Survivability because of electronic defenses; an A-10 has high
         *     Survivability strictly because of armor. Both are valid.
         *
         * GroundAttack (GA)
         *     The ability of an aircraft to damage ground units.
         *
         * OrdnanceLoad (OL)
         *     How many bombs and missiles the aircraft carries. Works as a
         *     MULTIPLIER on Ground Attack effect (more payload = more damage
         *     per sortie).
         *
         * Stealth (STL)
         *     The ability to avoid interception entirely. Stealth bypasses
         *     (or reduces) both GroundAirAttack AND GroundAirDefense.
         *
         * =====================================================================
         * NOTES ON SPECIAL CASES
         * =====================================================================
         * - Rocket artillery (ROC_*) carry the DoubleFire capability. They are
         *   the ultimate form of artillery — two indirect volleys per action.
         * - Helicopters are statted on the ground axis (HA/HD/SA/SD). They do
         *   not use DF/MAN/TS/SUR/GA/OL.
         * - Pure SAM systems (HA 1 / SA 1) are air-only and cannot meaningfully
         *   engage ground targets.
         * =====================================================================
         */

        #region Properties

        public string LongName { get; private set; } = "Default Weapon";
        public string ShortName { get; private set; } = "Default";

        // The type of weapon, for sprite picking and upgrade logic.
        public WeaponType WeaponType { get; private set; } = WeaponType.NONE;

        // Bucketted stats for intel report purposes.
        public Dictionary<WeaponType, int> IntelReportStats { get; private set; } = null;

        // Properties for ground units
        public float HardAttack { get; private set; } = 0;
        public int HardDefense { get; private set; } = 0;
        public int SoftAttack { get; private set; } = 0;
        public int SoftDefense { get; private set; } = 0;
        public int GroundAirAttack { get; private set; } = 0;
        public int GroundAirDefense { get; private set; } = 0;

        // Properties for air units
        public int Dogfighting { get; private set; } = 0;
        public int Maneuverability { get; private set; } = 0;
        public int TopSpeed { get; private set; } = 0;
        public int Survivability { get; private set; } = 0;
        public int GroundAttack { get; private set; } = 0;
        public int OrdinanceLoad { get; private set; } = 0;
        public int Stealth { get; private set; } = 0;

        // Range and movement properties
        public float PrimaryRange { get; private set; } = 0;
        public float IndirectRange { get; private set; } = 0;
        public float SpottingRange { get; private set; } = 0;

        // Max movement points
        public int MaxMovementPoints { get; private set; } = 0;

        // Special capability flags
        public bool IsAmphibious { get; private set; } = false;   // Whether this unit can cross rivers easily
        public bool IsDoubleFire { get; private set; } = false;   // MLRS units fire twice per attack
        public bool IsAttackCapable { get; private set; } = false; // Whether this unit can perform attacks

        // Capability enums
        public AllWeatherRating AllWeatherCapability { get; private set; } = AllWeatherRating.AllWeather;
        public SIGINT_Rating SIGINT_Rating { get; private set; }           = SIGINT_Rating.UnitLevel;
        public NBC_Rating NBC_Rating { get; private set; }                 = NBC_Rating.None;
        public NVG_Rating NVGCapability { get; private set; }              = NVG_Rating.None;
        public UnitSilhouette Silhouette { get; private set; }             = UnitSilhouette.Medium;

        // Unit icon sprites associated with this stat profile
        public RegimentIconProfile IconProfile { get; set; } = null;

        // Upgrade and availability properties
        public UpgradePath UpgradePath { get; private set; } = UpgradePath.None;
        public int PrestigeCost { get; private set; } = 0;
        public int TurnAvailable { get; private set; } = 0;

        #endregion // Properties

        #region Constructors

        public WeaponProfile(string _longName, string _shortName, WeaponType _type, int _hardAtt, int _hardDef,
                             int _softAtt, int _softDef, int _gat, int _gad,int _df, int _man,int _topSpd, int _surv, int _ga, int _ol,
                             int _stealth, int _pr, int _ir, int _sr, int _mmp, bool _isAmph, bool _isDF, bool _isAtt,
                             AllWeatherRating _awr, SIGINT_Rating _sir, NBC_Rating _nbc, NVG_Rating _nvg, UnitSilhouette _sil,
                             UpgradePath _upgradePath = UpgradePath.None, int _turnAvailable = 0)
        {
            LongName = _longName;
            ShortName = _shortName;
            WeaponType = _type;
            HardAttack = _hardAtt;
            HardDefense = _hardDef;
            SoftAttack = _softAtt;
            SoftDefense = _softDef;
            GroundAirAttack = _gat;
            GroundAirDefense = _gad;
            Dogfighting = _df;
            Maneuverability = _man;
            TopSpeed = _topSpd;
            Survivability = _surv;
            GroundAttack = _ga;
            OrdinanceLoad = _ol;
            Stealth = _stealth;
            PrimaryRange = _pr;
            IndirectRange = _ir;
            SpottingRange = _sr;
            MaxMovementPoints = _mmp;
            IsAmphibious = _isAmph;
            IsDoubleFire = _isDF;
            IsAttackCapable = _isAtt;
            AllWeatherCapability = _awr;
            SIGINT_Rating = _sir;
            NBC_Rating = _nbc;
            NVGCapability = _nvg;
            Silhouette = _sil;
            UpgradePath = _upgradePath;
            TurnAvailable = _turnAvailable;
            IconProfile = new RegimentIconProfile();
            IntelReportStats = new Dictionary<WeaponType, int>();
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Adds a stat value to the IntelReportStats dictionary for this weapon profile.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void AddIntelReportStat(WeaponType type, int value)
        {
            IntelReportStats ??= new Dictionary<WeaponType, int>();
            IntelReportStats[type] = value;
        }

        /// <summary>
        /// Sets the prestige cost of this weapon profile based on the given tier and type costs.
        /// </summary>
        public void SetPrestigeCost(PrestigeTierCost _tier, PrestigeTypeCost _type)
        {
            PrestigeCost = (int)_tier + (int)_type;
        }

        #endregion // Public Methods
    }

    /// <summary>
    /// A RegimentProfile provides stat profiles for different deployment states in CombatUnits.
    /// </summary>
    public class RegimentProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(Models.RegimentProfile);

        #endregion // Constants

        #region Properties

        // General properties
        [JsonInclude]
        [JsonPropertyName("name")]
        public string Name { get; private set; } = "Default";

        [JsonInclude]
        [JsonPropertyName("profileType")]
        public RegimentProfileType ProfileType { get; private set; } = RegimentProfileType.Default;

        // The stat profile associated with the embarked deployment state.
        [JsonInclude]
        [JsonPropertyName("embarked")]
        public WeaponType Embarked { get; private set; } = WeaponType.NONE;

        // The stat profile associated with the mobile deployment state.
        [JsonInclude]
        [JsonPropertyName("mobile")]
        public WeaponType Mobile { get; private set; } = WeaponType.NONE;

        // The stat profile associated with the deployed deployment state.
        [JsonInclude]
        [JsonPropertyName("deployed")]
        public WeaponType Deployed { get; private set; } = WeaponType.NONE;

        // Contains all bucketted stats totaled from the stat profile. Rebuilt at runtime.
        [JsonIgnore]
        public Dictionary<WeaponType, int> TotalIntelStats = null;

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Parameterless constructor for JSON deserialization.
        /// The deserializer will populate [JsonInclude] properties after construction.
        /// </summary>
        [JsonConstructor]
        public RegimentProfile()
        {
            TotalIntelStats = new Dictionary<WeaponType, int>();
        }

        #endregion // Constructors

        #region Initialization

        /// <summary>
        /// Initializes RegimentProfile with all required data.
        /// </summary>
        public void InitializeRegimentProfile(
            string name,
            RegimentProfileType profileType,
            WeaponType mobile,
            WeaponType deployed,
            WeaponType embarked)
        {
            try
            {
                Name = name;
                ProfileType = profileType;
                Mobile = mobile;
                Deployed = deployed;
                Embarked = embarked;
                TotalIntelStats = new Dictionary<WeaponType, int>();
                BuildIntelStats();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(InitializeRegimentProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Accumulates intel report stats from all assigned weapon profiles into TotalIntelStats.
        /// Merges entries from deployed, mobile, and embarked profiles, summing duplicate keys.
        /// </summary>
        public void BuildIntelStats()
        {
            try
            {
                TotalIntelStats = new Dictionary<WeaponType, int>();

                AccumulateIntelStats(GetDeployedProfile());
                AccumulateIntelStats(GetMobileProfile());
                AccumulateIntelStats(GetEmbarkedProfile());
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(BuildIntelStats), e);
                throw;
            }
        }

        private void AccumulateIntelStats(WeaponProfile profile)
        {
            if (profile?.IntelReportStats == null)
                return;

            foreach (var kvp in profile.IntelReportStats)
            {
                if (TotalIntelStats.ContainsKey(kvp.Key))
                    TotalIntelStats[kvp.Key] += kvp.Value;
                else
                    TotalIntelStats[kvp.Key] = kvp.Value;
            }
        }

        #endregion // Initialization

        #region Accessors

        /// <summary>
        /// Creates an IntelReport by sorting TotalIntelStats into equipment buckets
        /// based on WeaponType name prefixes.
        /// </summary>
        public IntelReport GetIntelReport()
        {
            try
            {
                var report = new IntelReport();

                if (TotalIntelStats == null)
                    return report;

                foreach (var kvp in TotalIntelStats)
                {
                    string name = kvp.Key.ToString();
                    int value = kvp.Value;

                    if (name == nameof(WeaponType.Personnel) || name.StartsWith("INF_"))
                        report.Personnel += value;
                    else if (name.StartsWith("TANK_"))
                        report.TANK += value;
                    else if (name.StartsWith("IFV_"))
                        report.IFV += value;
                    else if (name.StartsWith("APC_"))
                        report.APC += value;
                    else if (name.StartsWith("RCN_"))
                        report.RCN += value;
                    else if (name.StartsWith("ART_") || name.StartsWith("SPA_"))
                        report.ART += value;
                    else if (name.StartsWith("ROC_"))
                        report.ROC += value;
                    else if (name.StartsWith("SAM_") || name.StartsWith("SPSAM_") || name.StartsWith("MANPAD_"))
                        report.SAM += value;
                    else if (name.StartsWith("AAA_") || name.StartsWith("SPAAA_"))
                        report.AAA += value;
                    else if (name.StartsWith("AT_"))
                        report.AT += value;
                    else if (name.StartsWith("HEL_"))
                        report.HEL += value;
                    else if (name.StartsWith("AWACS_"))
                        report.AWACS += value;
                    else if (name.StartsWith("TRN_") || name.StartsWith("TRK_"))
                        report.TRN += value;
                    else if (name.StartsWith("FGT_"))
                        report.FGT += value;
                    else if (name.StartsWith("ATT_"))
                        report.ATT += value;
                    else if (name.StartsWith("BMB_"))
                        report.BMB += value;
                    else if (name.StartsWith("RCNA_"))
                        report.RCNA += value;
                }

                return report;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetIntelReport), e);
                throw;
            }
        }

        /// <summary>
        /// Returns the WeaponProfile for the deployed state from the WeaponProfileDB.
        /// Returns null if no deployed profile is assigned.
        /// </summary>
        public WeaponProfile GetDeployedProfile()
        {
            try
            {
                return Deployed == WeaponType.NONE ? null : WeaponProfileDB.GetWeaponProfile(Deployed);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetDeployedProfile), e);
                return null;
            }
        }

        /// <summary>
        /// Returns the WeaponProfile for the mobile state from the WeaponProfileDB.
        /// Returns null if no mobile profile is assigned.
        /// </summary>
        public WeaponProfile GetMobileProfile()
        {
            try
            {
                return Mobile == WeaponType.NONE ? null : WeaponProfileDB.GetWeaponProfile(Mobile);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetMobileProfile), e);
                return null;
            }
        }

        /// <summary>
        /// Returns the WeaponProfile for the embarked state from the WeaponProfileDB.
        /// Returns null if no embarked profile is assigned.
        /// </summary>
        public WeaponProfile GetEmbarkedProfile()
        {
            try
            {
                return Embarked == WeaponType.NONE ? null : WeaponProfileDB.GetWeaponProfile(Embarked);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetEmbarkedProfile), e);
                return null;
            }
        }

        #endregion // Accessors

        #region Icon Helpers

        /// <summary>
        /// Returns the unit icon string based on deployment position and facing direction.
        /// Units with firing icons use them when in HastyDefense, Entrenched, or Fortified positions.
        /// </summary>
        public string GetIcon(DeploymentPosition position, HexDirection direction)
        {
            try
            {
                var profile = position switch
                {
                    DeploymentPosition.Embarked => GetEmbarkedProfile(),
                    DeploymentPosition.Mobile => GetMobileProfile(),
                    _ => GetDeployedProfile()
                };

                if (profile?.IconProfile == null)
                    throw new InvalidOperationException($"No icon profile available for position {position}");

                var iconProfile = profile.IconProfile;
                bool useFiringIcon = iconProfile.IconType == RegimentIconType.Directional_Fire &&
                    (position == DeploymentPosition.HastyDefense ||
                     position == DeploymentPosition.Entrenched ||
                     position == DeploymentPosition.Fortified);

                return iconProfile.IconType switch
                {
                    RegimentIconType.Single => iconProfile.GetIcon(),
                    RegimentIconType.Directional => iconProfile.GetDirectionalIcon(direction),
                    RegimentIconType.Directional_Fire => useFiringIcon
                        ? iconProfile.GetFiringIcon(direction)
                        : iconProfile.GetDirectionalIcon(direction),
                    RegimentIconType.Helo_Animation => iconProfile.GetIcon(),
                    _ => throw new ArgumentException($"Unknown icon type: {iconProfile.IconType}")
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetIcon), e);
                throw;
            }
        }

        #endregion // Icon Helpers
    }
}
