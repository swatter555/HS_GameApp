using System.Collections.Generic;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models
{
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
}
