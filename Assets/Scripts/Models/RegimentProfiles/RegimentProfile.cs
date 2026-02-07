using HammerAndSickle.Services;
using System;
using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models
{
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
        #region Properties

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

        #endregion // Properties

        #region Constructors

        public WeaponProfile(int _hardAtt, int _hardDef, int _softAtt, int _softDef, int _gat, int _gad, int _df, int _man,
                                    int _topSpd, int _surv, int _ga, int _ol, int _stealth, int _pr, int _ir, int _sr, int _mmp,
                                    bool _isAmph, bool _isDF, bool _isAtt, AllWeatherRating _awr, SIGINT_Rating _sir, NBC_Rating _nbc,
                                    NVG_Rating _nvg, UnitSilhouette _sil)
        {
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
            IconProfile = new RegimentIconProfile();
        }

        #endregion // Constructors
    }
    
    /// <summary>
    /// A RegimentProfile provides stat profiles for different deployment states in CombatUnits.
    /// </summary>
    public class RegimentProfile
    {
        #region Constants

        private const string CLASS_NAME = nameof(RegimentProfile);

        #endregion // Constants

        #region Properties

        // General properties
        public string Name { get; private set; } = "Default";
        public RegimentProfileType ProfileType { get; private set; } = RegimentProfileType.Default;
        public int TurnAvailable { get; private set; } = 0;      // The campaign turn this is available.
        public int PrestigeCost { get; private set; } = 0;       // Prestige cost for purchasing this unit type
        public UpgradeType UpgradePath { get; private set; } = UpgradeType.None;  // The upgrade path for this profile

        // The stat profile associated with the mobile deployment state.
        public WeaponProfile Mobile { get; private set; } = null;

        // The stat profile associated with the deployed deployment state.
        public WeaponProfile Deployed { get; private set; } = null;

        // The stat profile associated with the embarked deployment state.
        public WeaponProfile Embarked { get; private set; } = null;

        #endregion // Properties

        #region Initialization

        /// <summary>
        /// Initializes the RegimentProfile with all required data.
        /// </summary>
        public void Initialize(
            string name,
            RegimentProfileType profileType,
            int turnAvailable,
            int prestigeCost,
            UpgradeType upgradePath,
            WeaponProfile mobile,
            WeaponProfile deployed,
            WeaponProfile embarked)
        {
            try
            {
                Name = name;
                ProfileType = profileType;
                TurnAvailable = turnAvailable;
                PrestigeCost = prestigeCost;
                UpgradePath = upgradePath;
                Mobile = mobile;
                Deployed = deployed;
                Embarked = embarked;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Initialize), e);
                throw;
            }
        }

        #endregion // Initialization

        #region Derived Properties

        /// <summary>
        /// Returns true if this profile has a mobile stats profile.
        /// </summary>
        public bool HasMobileProfile => Mobile != null;

        /// <summary>
        /// Returns true if this profile has a deployed stats profile.
        /// </summary>
        public bool HasDeployedProfile => Deployed != null;

        /// <summary>
        /// Returns true if this profile has an embarked stats profile.
        /// </summary>
        public bool HasEmbarkedProfile => Embarked != null;

        /// <summary>
        /// Returns true if any profile has air combat stats (Dogfighting, Maneuverability, TopSpeed).
        /// </summary>
        public bool IsAirUnit =>
            (Mobile != null && (Mobile.Dogfighting > 0 || Mobile.Maneuverability > 0 || Mobile.TopSpeed > 0)) ||
            (Deployed != null && (Deployed.Dogfighting > 0 || Deployed.Maneuverability > 0 || Deployed.TopSpeed > 0)) ||
            (Embarked != null && (Embarked.Dogfighting > 0 || Embarked.Maneuverability > 0 || Embarked.TopSpeed > 0));

        /// <summary>
        /// Returns true if any profile has ground combat stats (HardAttack, SoftAttack, HardDefense, SoftDefense).
        /// </summary>
        public bool IsGroundUnit =>
            (Mobile != null && (Mobile.HardAttack > 0 || Mobile.SoftAttack > 0 || Mobile.HardDefense > 0 || Mobile.SoftDefense > 0)) ||
            (Deployed != null && (Deployed.HardAttack > 0 || Deployed.SoftAttack > 0 || Deployed.HardDefense > 0 || Deployed.SoftDefense > 0)) ||
            (Embarked != null && (Embarked.HardAttack > 0 || Embarked.SoftAttack > 0 || Embarked.HardDefense > 0 || Embarked.SoftDefense > 0));

        #endregion // Derived Properties

        #region Setters

        public void SetEmbarkedProfile(WeaponProfile profile)
        {
            Embarked = profile;
        }

        #endregion // Setters

        #region Accessors

        /// <summary>
        /// Returns the appropriate WeaponProfile based on deployment position.
        /// Deployed, HastyDefense, Entrenched, and Fortified use the Deployed profile.
        /// Mobile falls back to Deployed if no Mobile profile exists (e.g., static units).
        /// Embarked returns null if no Embarked profile exists (unit cannot be transported).
        /// </summary>
        public WeaponProfile GetStatsProfile(DeploymentPosition position)
        {
            try
            {
                return position switch
                {
                    DeploymentPosition.Embarked => Embarked,
                    DeploymentPosition.Mobile => Mobile ?? Deployed,
                    DeploymentPosition.Deployed => Deployed,
                    DeploymentPosition.HastyDefense => Deployed,
                    DeploymentPosition.Entrenched => Deployed,
                    DeploymentPosition.Fortified => Deployed,
                    _ => throw new ArgumentException($"Invalid deployment position: {position}", nameof(position))
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetStatsProfile), e);
                throw;
            }
        }

        /// <summary>
        /// Returns the maximum movement points for the given deployment position.
        /// </summary>
        public int GetMaxMovementPoints(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.MaxMovementPoints ?? 0;
        }

        /// <summary>
        /// Returns the primary range for the given deployment position.
        /// </summary>
        public float GetPrimaryRange(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.PrimaryRange ?? 0f;
        }

        /// <summary>
        /// Returns the indirect fire range for the given deployment position.
        /// </summary>
        public float GetIndirectRange(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.IndirectRange ?? 0f;
        }

        /// <summary>
        /// Returns the spotting range for the given deployment position.
        /// </summary>
        public float GetSpottingRange(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.SpottingRange ?? 0f;
        }

        #endregion // Accessors

        #region Capability Checks

        /// <summary>
        /// Returns true if the unit is amphibious at the given deployment position.
        /// </summary>
        public bool IsAmphibious(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.IsAmphibious ?? false;
        }

        /// <summary>
        /// Returns true if the unit has double fire capability at the given deployment position.
        /// </summary>
        public bool IsDoubleFire(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.IsDoubleFire ?? false;
        }

        /// <summary>
        /// Returns true if the unit can perform attacks at the given deployment position.
        /// </summary>
        public bool IsAttackCapable(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile?.IsAttackCapable ?? false;
        }

        /// <summary>
        /// Returns true if the unit has indirect fire capability at the given deployment position.
        /// </summary>
        public bool HasIndirectFire(DeploymentPosition position)
        {
            var profile = GetStatsProfile(position);
            return profile != null && profile.IndirectRange > 0;
        }

        /// <summary>
        /// Returns true if this profile has a valid stats profile for the given deployment position.
        /// </summary>
        public bool HasStatsProfile(DeploymentPosition position)
        {
            return position switch
            {
                DeploymentPosition.Embarked => Embarked != null,
                DeploymentPosition.Mobile => Mobile != null,
                _ => Deployed != null
            };
        }

        #endregion // Capability Checks

        #region Icon Helpers

        /// <summary>
        /// Returns the unit icon string based on deployment position and facing direction.
        /// Units with firing icons use them when in HastyDefense, Entrenched, or Fortified positions.
        /// </summary>
        public string GetIcon(DeploymentPosition position, HexDirection direction)
        {
            try
            {
                var profile = GetStatsProfile(position);
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
