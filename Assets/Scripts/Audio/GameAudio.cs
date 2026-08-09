using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;

namespace HammerAndSickle.Audio
{
    /// <summary>
    /// The one entry point for playing a sound effect.
    ///
    /// <code>
    /// GameAudio.Play(SoundEffect.ButtonClick);              // UI, turn, weather — no unit caused it
    /// GameAudio.PlayFrom(SoundEffect.TankGun, firer);       // a unit caused it — FOG GATED
    /// </code>
    /// </summary>
    /// <remarks>
    /// ⚠ THE TWO METHODS EXIST TO MAKE THE FOG GATE UNFORGETTABLE (§27.7.4), and that is the entire reason
    /// this is not a single Play() with an optional argument. A single method would default to ungated, so
    /// forgetting the source would leak an unspotted enemy's position through audio — silently, with no
    /// compile error and nothing visible in play except that the player mysteriously knows too much. With
    /// two methods the question "which unit is this sound attributed to?" has to be answered at every call
    /// site that has a unit, and answering it wrong is a visible mistake in the diff rather than an
    /// omission.
    ///
    /// ⚠ ATTRIBUTION RULE (§27.7.4.2): pass the unit the sound BELONGS to, not the selected unit. The
    /// FIRING sound belongs to the firer; the IMPACT belongs to the target. That is what lets an unseen
    /// battery shell the player audibly without identifying itself.
    ///
    /// ⚠ NEVER LAZY-CREATES. Every method no-ops when no GameAudioManager exists, so this is safe to call
    /// from anywhere including headless EditorTests — unlike `GameAudioManager.Instance`, whose getter
    /// BUILDS a GameObject and would spawn a manager inside any test that made a noise.
    /// </remarks>
    public static class GameAudio
    {
        #region Public API

        /// <summary>
        /// Plays a sound that no unit caused — UI, turn and phase changes, weather, objectives.
        /// Not fog-gated, because it reveals nothing about a unit.
        /// </summary>
        public static void Play(GameAudioManager.SoundEffect id, float volumeScale = 1f)
        {
            GameAudioManager.Existing?.PlaySfx(id, volumeScale);
        }

        /// <summary>
        /// Plays a sound caused by a unit, gated on whether the player may hear that unit
        /// (<see cref="AudioFogPolicy.CanHear"/>). Silent when the source is an unspotted enemy.
        /// </summary>
        /// <param name="id">The sound.</param>
        /// <param name="source">The unit the sound is ATTRIBUTED to — firer for a shot, target for an impact.</param>
        /// <param name="volumeScale">Per-play volume multiplier.</param>
        public static void PlayFrom(GameAudioManager.SoundEffect id, CombatUnit source, float volumeScale = 1f)
        {
            if (!AudioFogPolicy.CanHear(source)) return;
            GameAudioManager.Existing?.PlaySfx(id, volumeScale);
        }

        /// <summary>
        /// Plays the weapon-fire sound for a unit's ACTIVE weapon, resolved through
        /// <see cref="WeaponSoundClassifier"/>. Fog-gated like <see cref="PlayFrom"/>.
        /// </summary>
        /// <remarks>
        /// ⚠ ORDERING (§7.13.5.4 / §12.4.9): firing REVEALS the firer, so apply that spotting change
        /// BEFORE calling this. Called first, the gate sees the pre-reveal level and suppresses a shot the
        /// player is entitled to hear.
        /// ⚠ Returns silently for an unarmed class (transports, AWACS, photo recon) — they map to
        /// <see cref="WeaponSoundFamily.None"/> and have no fire sound by design.
        /// </remarks>
        public static void PlayWeaponFire(CombatUnit firer, float volumeScale = 1f)
        {
            if (!AudioFogPolicy.CanHear(firer)) return;

            WeaponSoundFamily family = WeaponSoundClassifier.FamilyFor(firer);
            if (family == WeaponSoundFamily.None) return;

            GameAudioManager.Existing?.PlaySfx(SoundEffectFor(family), volumeScale);
        }

        /// <summary>
        /// Plays a unit's movement sound: ONE fire-and-forget shot for the whole move (§27.7.7), with the
        /// clip chosen from how the unit is ACTUALLY travelling and how long the move will take.
        /// </summary>
        /// <param name="unit">The mover. Fog-gated like <see cref="PlayFrom"/>.</param>
        /// <param name="predictedSeconds">
        /// Duration of the committed path. Known before the first step, which is what makes the choice
        /// deterministic rather than a mid-move correction.
        /// </param>
        /// <remarks>
        /// ⚠ THE MEDIUM COMES FROM THE ACTIVE PROFILE, never the classification — that is the whole fix.
        /// An air-assault regiment is `MAM` walking, riding MT-LBs and flying; only the active profile
        /// distinguishes foot from tracked from helo.
        /// ⚠ THE LONG CUT IS DECIDED BY MEASURING THE ACTUAL CLIP, not by a constant. Bob's movement
        /// recordings run 1.5–2.5 s by design, well past the ~1 s the original plan assumed, so a
        /// hard-coded threshold was already wrong the day the first wav landed. Measuring means
        /// re-recording retunes it for free — and if a standard clip already covers a medium's longest
        /// possible move, its long row simply never gets picked and needs no clip at all.
        /// </remarks>
        public static void PlayMovement(CombatUnit unit, float predictedSeconds, float volumeScale = 1f)
        {
            if (!AudioFogPolicy.CanHear(unit)) return;

            MovementMedium medium = MovementModeService.CurrentMedium(unit);
            GameAudioManager.SoundEffect standard = GameAudioManager.GetMovementSFX(medium);
            if (standard == GameAudioManager.SoundEffect.None) return;

            GameAudioManager manager = GameAudioManager.Existing;
            if (manager == null) return;

            // Escalate only when the standard recording would leave a silent gap mid-move. A row with no
            // clip reports 0 length and never escalates — there is nothing to fall short of yet.
            float standardLength = manager.ClipSecondsFor(standard);
            bool longCut = standardLength > 0f && predictedSeconds > standardLength;

            manager.PlaySfx(GameAudioManager.GetMovementSFX(medium, longCut), volumeScale);
        }

        /// <summary>
        /// Plays the impact sound on the unit that was HIT.
        /// </summary>
        /// <remarks>
        /// ⚠ GATED ON THE TARGET, NOT THE FIRER (§27.7.4.2), and that asymmetry is the whole mechanism: an
        /// unseen battery shelling the player produces NO gun report and a FULL impact, so the player hears
        /// their regiment taking fire without learning what fired. It is also why no "generic substitute
        /// sound" concept is needed anywhere in the design.
        ///
        /// ⚠ Armour vs soft comes from <see cref="RegimentProfile.ClassifyWeaponType"/> — the same single
        /// classifier behind the intel and §24.8.7 loss reports — so audio can never call something a tank
        /// that the loss report calls an AFV. Do not add a second list here.
        /// </remarks>
        public static void PlayImpact(CombatUnit target, float volumeScale = 1f)
        {
            if (!AudioFogPolicy.CanHear(target)) return;

            GameAudioManager.Existing?.PlaySfx(ImpactSoundFor(target), volumeScale);
        }

        #endregion // Public API

        #region Sound selection

        /// <summary>
        /// Maps a weapon sound family to its catalog id. ⚠ The ONE place this mapping lives — a second
        /// copy is how the sound and the loss report start disagreeing about what a weapon is.
        /// </summary>
        private static GameAudioManager.SoundEffect SoundEffectFor(WeaponSoundFamily family) => family switch
        {
            WeaponSoundFamily.SmallArms            => GameAudioManager.SoundEffect.FireSmallArms,
            WeaponSoundFamily.HeavyMachineGun      => GameAudioManager.SoundEffect.FireHeavyMachineGun,
            WeaponSoundFamily.Autocannon           => GameAudioManager.SoundEffect.FireAutocannon,
            WeaponSoundFamily.TankGun              => GameAudioManager.SoundEffect.FireTankGun,
            WeaponSoundFamily.AntiTankMissile      => GameAudioManager.SoundEffect.FireAntiTankMissile,
            WeaponSoundFamily.ArtilleryGun         => GameAudioManager.SoundEffect.FireArtilleryGun,
            WeaponSoundFamily.RocketArtillery      => GameAudioManager.SoundEffect.FireRocketArtillery,
            WeaponSoundFamily.SurfaceToAirMissile  => GameAudioManager.SoundEffect.FireSurfaceToAirMissile,
            WeaponSoundFamily.AntiAircraftGun      => GameAudioManager.SoundEffect.FireAntiAircraftGun,
            WeaponSoundFamily.HelicopterAttack     => GameAudioManager.SoundEffect.FireHelicopterAttack,
            WeaponSoundFamily.AircraftCannon       => GameAudioManager.SoundEffect.FireAircraftCannon,
            WeaponSoundFamily.AircraftGroundAttack => GameAudioManager.SoundEffect.FireAircraftGroundAttack,
            WeaponSoundFamily.AircraftBombs        => GameAudioManager.SoundEffect.FireAircraftBombs,

            // ⚠ Includes WeaponSoundFamily.None — an unarmed class (transport, AWACS, photo recon) has no
            // fire sound BY DESIGN, and a new family with no decision lands here silently rather than
            // borrowing another arm's sound. Silent beats confidently wrong.
            _ => GameAudioManager.SoundEffect.None
        };

        /// <summary>
        /// Which impact a hit on this unit should sound like. Structures first (a facility is neither
        /// armour nor soft), then the target's ACTIVE profile through the shared classifier.
        /// </summary>
        private static GameAudioManager.SoundEffect ImpactSoundFor(CombatUnit target)
        {
            if (target == null) return GameAudioManager.SoundEffect.None;
            if (target.IsBase) return GameAudioManager.SoundEffect.ImpactStructure;

            WeaponProfile profile = target.GetActiveWeaponProfile();
            if (profile == null) return GameAudioManager.SoundEffect.ImpactSoft;

            return RegimentProfile.ClassifyWeaponType(profile.WeaponType) switch
            {
                EquipmentBucket.TANK or EquipmentBucket.IFV or EquipmentBucket.APC
                    => GameAudioManager.SoundEffect.ImpactArmour,
                _ => GameAudioManager.SoundEffect.ImpactSoft
            };
        }

        #endregion // Sound selection
    }
}
