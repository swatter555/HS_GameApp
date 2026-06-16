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

        // Unit icon sprites associated with this stat profile
        public RegimentIconProfile IconProfile { get; set; } = null;

        // Upgrade and availability properties
        public UpgradePath UpgradePath { get; private set; } = UpgradePath.None;
        public int PrestigeCost { get; private set; } = 0;
        public int TurnAvailable { get; private set; } = 0;

        // Individual Combat Modifier — the accumulator of fire-control/quality traits (Appendix W §1).
        // Default 1.0; set by TraitResolver during the Phase 3 per-profile rebuild. Replaces the retired
        // unit-level CombatUnit.IndividualCombatModifier (a unit's stances are different profiles).
        public float ICM { get; private set; } = GameData.ICM_DEFAULT;

        // Hard/Soft target class (Appendix W §7.4.1). The TARGET's class drives the ground combat axis
        // (Hard → HA vs HD, Soft → SA vs SD). Auto-defaulted from WeaponType prefix in the constructor;
        // override per profile via SetTargetClass (e.g. armored-car recon). Inert for aircraft (§7.7.11).
        public TargetClass TargetClass { get; private set; } = TargetClass.Soft;

        // Transport role (§10.3.13) for embarked-slot validation. None unless this profile is used as an
        // organic/inorganic transport. Set via SetTransportCategory on the transport profiles.
        public TransportCategory TransportCategory { get; private set; } = TransportCategory.None;

        // Capabilities resolved from this profile's traits (Appendix W §1). Populated by FromProfileDef.
        // The legacy IsAmphibious / IsDoubleFire / IsAttackCapable bools are DERIVED from this set at build
        // so existing gameplay readers keep working; Phase 4 migrates readers to HasCapability and removes
        // the bools.
        private readonly HashSet<WeaponCapability> _capabilities = new HashSet<WeaponCapability>();

        #endregion // Properties

        #region Constructors

        public WeaponProfile(string _longName, string _shortName, WeaponType _type, int _hardAtt, int _hardDef,
                             int _softAtt, int _softDef, int _gat, int _gad,int _df, int _man,int _topSpd, int _surv, int _ga, int _ol,
                             int _stealth, int _pr, int _ir, int _sr, int _mmp, bool _isAmph, bool _isDF, bool _isAtt,
                             UpgradePath _upgradePath = UpgradePath.None, int _turnAvailable = 0)
        {
            LongName = _longName;
            ShortName = _shortName;
            WeaponType = _type;
            TargetClass = DefaultTargetClass(_type);
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

        /// <summary>
        /// Sets the stored ICM (product of this profile's fire-control/quality traits), clamped to
        /// the GameData guardrails. Called by the trait resolver during profile construction.
        /// </summary>
        public void SetICM(float icm)
        {
            ICM = System.Math.Clamp(icm, GameData.ICM_MIN, GameData.ICM_MAX);
        }

        /// <summary>
        /// Overrides the hard/soft target class set by the constructor's prefix default (§7.4.1.2 —
        /// e.g. armored-car / cavalry-scout recon profiles that fight as Hard targets).
        /// </summary>
        public void SetTargetClass(TargetClass targetClass)
        {
            TargetClass = targetClass;
        }

        /// <summary>
        /// Marks this profile's transport role (§10.3.13). Set on helo/fixed-wing transport profiles
        /// used as embarked slots; leave None for everything else.
        /// </summary>
        public void SetTransportCategory(TransportCategory category)
        {
            TransportCategory = category;
        }

        /// <summary>True if this profile carries the given capability (resolved from its traits).</summary>
        public bool HasCapability(WeaponCapability capability) => _capabilities.Contains(capability);

        /// <summary>Replaces the resolved capability set (called by FromProfileDef after resolution).</summary>
        public void SetCapabilities(IEnumerable<WeaponCapability> capabilities)
        {
            _capabilities.Clear();
            if (capabilities != null)
                foreach (WeaponCapability c in capabilities) _capabilities.Add(c);
        }

        #endregion // Public Methods

        #region Factory

        /// <summary>
        /// Builds a WeaponProfile from the Archetype + Delta + Trait model (Appendix W §1). The ProfileDef
        /// is resolved by <see cref="TraitResolver"/> into the FULL statline — all 17 ProfileStats
        /// (HA/HD/SA/SD/GAT/GAD, the air block DF/MAN/TS/SUR, plus GA/OL/STL/PR/IR/SR and MMP) — the stored
        /// ICM, and a capability set. This is the Phase 3 entry point that replaces the additive per-profile
        /// constructor calls in WeaponProfileDB.
        ///
        /// Design §2 = Option A (RATIFIED 2026-06-15): payload/range/spotting stats flow through the ProfileDef
        /// like every other stat, so trait deltas on those axes are LIVE — e.g. GUN_LAUNCHED_ATGM's PR +1
        /// (standoff direct fire) and OPTICS/THERMAL's SR +1. The archetype carries the family base for these
        /// (e.g. SR 2 / PR 1 for tanks); deltas + traits adjust off it.
        ///
        /// The legacy capability bools are DERIVED from the resolved capabilities (the Phase-4 bridge):
        ///   IsAmphibious = Amphibious · IsDoubleFire = RocketArtillery · IsAttackCapable = !NonCombatant.
        /// TargetClass is auto-set from the WeaponType prefix by the constructor; override afterward if needed.
        /// </summary>
        public static WeaponProfile FromProfileDef(
            string longName, string shortName, WeaponType type, ProfileDef def,
            UpgradePath upgradePath = UpgradePath.None, int turnAvailable = 0)
        {
            TraitResolver.Result r = TraitResolver.Resolve(def);
            var caps = new HashSet<WeaponCapability>(r.Capabilities);

            var profile = new WeaponProfile(
                longName, shortName, type,
                r.Stat(ProfileStat.HA), r.Stat(ProfileStat.HD), r.Stat(ProfileStat.SA), r.Stat(ProfileStat.SD),
                r.Stat(ProfileStat.GAT), r.Stat(ProfileStat.GAD),
                r.Stat(ProfileStat.DF), r.Stat(ProfileStat.MAN), r.Stat(ProfileStat.TS), r.Stat(ProfileStat.SUR),
                r.Stat(ProfileStat.GA), r.Stat(ProfileStat.OL), r.Stat(ProfileStat.STL),
                r.Stat(ProfileStat.PR), r.Stat(ProfileStat.IR), r.Stat(ProfileStat.SR), r.Stat(ProfileStat.MMP),
                caps.Contains(WeaponCapability.Amphibious),        // _isAmph
                caps.Contains(WeaponCapability.RocketArtillery),   // _isDF
                !caps.Contains(WeaponCapability.NonCombatant),     // _isAtt
                upgradePath, turnAvailable);

            profile.SetICM(r.ICM);
            profile.SetCapabilities(caps);
            return profile;
        }

        #endregion // Factory

        #region Private Methods

        /// <summary>
        /// Default hard/soft class by WeaponType prefix (§7.4.1.2): HARD for TANK/IFV/APC/HEL/SPAAA/SPSAM,
        /// SOFT for everything else (SPA stays Soft — distinct first token from SPAAA/SPSAM). Overridable
        /// per profile via <see cref="SetTargetClass"/>.
        /// </summary>
        private static TargetClass DefaultTargetClass(WeaponType type)
        {
            string prefix = type.ToString().Split('_')[0];
            return prefix switch
            {
                "TANK" => TargetClass.Hard,
                "IFV" => TargetClass.Hard,
                "APC" => TargetClass.Hard,
                "HEL" => TargetClass.Hard,
                "SPAAA" => TargetClass.Hard,
                "SPSAM" => TargetClass.Hard,
                _ => TargetClass.Soft,
            };
        }

        #endregion // Private Methods
    }
}
