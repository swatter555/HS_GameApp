using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;

namespace HammerAndSickle.Audio
{
    /// <summary>
    /// The fourteen weapon-fire sound families (HS_DesignDoc §27.7.5). There are 177 weapon profiles;
    /// per-profile fire audio is unshippable, so profiles collapse to families and a family owns one
    /// sound (with variants).
    ///
    /// ⚠ NOT PERSISTED — a transient audio classification, never written to a save or to shipped content,
    /// so it is exempt from the never-rename rule (CLAUDE.md item 11). Members may be renamed freely.
    /// It is also NOT serialized into any scene or prefab, so unlike <c>GameAudioManager.SoundEffect</c>
    /// it is not append-only either — reorder at will.
    /// </summary>
    public enum WeaponSoundFamily
    {
        None,
        SmallArms,
        HeavyMachineGun,
        Autocannon,
        TankGun,
        AntiTankMissile,
        ArtilleryGun,
        RocketArtillery,
        SurfaceToAirMissile,
        AntiAircraftGun,
        HelicopterAttack,
        AircraftCannon,
        AircraftGroundAttack,
        AircraftBombs
    }

    /// <summary>
    /// Resolves a weapon to the sound family that should be heard when it fires.
    /// </summary>
    /// <remarks>
    /// ⚠ THIS DELIBERATELY DOES NOT CLASSIFY ANYTHING ITSELF. It maps
    /// <see cref="EquipmentBays.ClassifyWeaponType"/>'s output — the SINGLE prefix classifier that
    /// already backs both the intel report and the §24.8.7 loss report — onto sound families. A second
    /// prefix list would be a third opinion about what counts as a tank, and the loss-report pass (P6)
    /// specifically rejected that in favour of sharing this classifier. Do not "optimise" this into its
    /// own name-matching.
    ///
    /// ⚠ Consequence, and it is the correct one: a new <c>WeaponType</c> whose prefix the classifier does
    /// not recognise falls to <see cref="EquipmentBucket.None"/> and therefore to
    /// <see cref="WeaponSoundFamily.None"/> — SILENT, not mis-sounded. Same failure direction the two
    /// reports already have.
    /// </remarks>
    public static class WeaponSoundClassifier
    {
        #region Public API

        /// <summary>Sound family for a weapon type.</summary>
        public static WeaponSoundFamily FamilyFor(WeaponType type) =>
            FamilyFor(EquipmentBays.ClassifyWeaponType(type));

        /// <summary>
        /// Sound family for a unit, resolved through its CURRENTLY ACTIVE weapon profile.
        /// </summary>
        /// <remarks>
        /// ⚠ Active, not deployed — the same regiment fires a different weapon depending on posture
        /// (§9.10.4: dismounted infantry in Deployed, its BMP in Mobile), so a unit's fire sound is a
        /// function of deployment state and must not be cached per unit.
        /// </remarks>
        public static WeaponSoundFamily FamilyFor(CombatUnit unit)
        {
            var profile = unit?.GetActiveWeaponProfile();
            return profile == null ? WeaponSoundFamily.None : FamilyFor(profile.WeaponType);
        }

        /// <summary>Sound family for an already-classified equipment bucket.</summary>
        public static WeaponSoundFamily FamilyFor(EquipmentBucket bucket) => bucket switch
        {
            EquipmentBucket.Personnel => WeaponSoundFamily.SmallArms,
            EquipmentBucket.TANK      => WeaponSoundFamily.TankGun,
            EquipmentBucket.IFV       => WeaponSoundFamily.Autocannon,

            // APCs and wheeled recon both fight from a heavy MG (KPVT and friends) rather than a cannon.
            EquipmentBucket.APC       => WeaponSoundFamily.HeavyMachineGun,
            EquipmentBucket.RCN       => WeaponSoundFamily.HeavyMachineGun,

            // AT covers both towed guns and missile launchers; the missile is the dominant late-Cold-War
            // presentation and the one the player associates with the class.
            EquipmentBucket.AT        => WeaponSoundFamily.AntiTankMissile,

            EquipmentBucket.ART       => WeaponSoundFamily.ArtilleryGun,
            EquipmentBucket.ROC       => WeaponSoundFamily.RocketArtillery,
            EquipmentBucket.SAM       => WeaponSoundFamily.SurfaceToAirMissile,
            EquipmentBucket.AAA       => WeaponSoundFamily.AntiAircraftGun,
            EquipmentBucket.HEL       => WeaponSoundFamily.HelicopterAttack,
            EquipmentBucket.FGT       => WeaponSoundFamily.AircraftCannon,
            EquipmentBucket.ATT       => WeaponSoundFamily.AircraftGroundAttack,
            EquipmentBucket.BMB       => WeaponSoundFamily.AircraftBombs,

            // Unarmed by design: AWACS, TRN (transports), RCNA (photo recon), and the None fallback.
            // These classes never initiate fire, so silence is the correct answer, not a missing case.
            _ => WeaponSoundFamily.None
        };

        #endregion // Public API
    }
}
