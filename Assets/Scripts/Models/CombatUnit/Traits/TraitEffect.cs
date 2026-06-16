namespace HammerAndSickle.Models
{
    /// <summary>
    /// One atomic effect of a trait. A trait is a bundle of these. Constructed via the static
    /// factory helpers so call sites read declaratively (Appendix W §1 magnitude language).
    /// </summary>
    public readonly struct TraitEffect
    {
        #region Properties

        public EffectKind Kind { get; }
        public EffectStatus Status { get; }

        // StatDelta / StatFloor payload
        public ProfileStat Stat { get; }
        public int Amount { get; }

        // IcmMultiply payload. WeatherMultiplier carries the poor-weather value for the
        // weather-conditional optics (T23); 0 means "no weather ramp". Phase 1 applies Multiplier
        // (the Clear value) only — the weather pass reads WeatherMultiplier later.
        public float Multiplier { get; }
        public float WeatherMultiplier { get; }

        // Capability payload
        public WeaponCapability Capability { get; }

        #endregion // Properties

        #region Construction

        private TraitEffect(EffectKind kind, EffectStatus status, ProfileStat stat, int amount,
                            float multiplier, float weatherMultiplier, WeaponCapability capability)
        {
            Kind = kind;
            Status = status;
            Stat = stat;
            Amount = amount;
            Multiplier = multiplier;
            WeatherMultiplier = weatherMultiplier;
            Capability = capability;
        }

        /// <summary>Signed additive change to one stat (NUDGE +1 · STEP +2 · BAND +3 · BIG +5).</summary>
        public static TraitEffect Delta(ProfileStat stat, int amount, EffectStatus status = EffectStatus.Live)
            => new TraitEffect(EffectKind.StatDelta, status, stat, amount, 1f, 0f, WeaponCapability.None);

        /// <summary>Raise a stat to at least <paramref name="amount"/> (e.g. MANPADS "enables GAT 6/8").</summary>
        public static TraitEffect Floor(ProfileStat stat, int amount, EffectStatus status = EffectStatus.Live)
            => new TraitEffect(EffectKind.StatFloor, status, stat, amount, 1f, 0f, WeaponCapability.None);

        /// <summary>Quality multiplier folded into storedICM (QUALITY_S ×1.05 · M ×1.10 · L ×1.20).</summary>
        public static TraitEffect Icm(float multiplier, EffectStatus status = EffectStatus.Live)
            => new TraitEffect(EffectKind.IcmMultiply, status, ProfileStat.HA, 0, multiplier, 0f, WeaponCapability.None);

        /// <summary>Weather-conditional ICM (T23): clear value live now, poor value parked for the weather pass.</summary>
        public static TraitEffect IcmWeather(float clear, float poor, EffectStatus status = EffectStatus.Live)
            => new TraitEffect(EffectKind.IcmMultiply, status, ProfileStat.HA, 0, clear, poor, WeaponCapability.None);

        /// <summary>Enables a behaviour/flag the engine reads.</summary>
        public static TraitEffect Cap(WeaponCapability capability, EffectStatus status = EffectStatus.Live)
            => new TraitEffect(EffectKind.Capability, status, ProfileStat.HA, 0, 1f, 0f, capability);

        #endregion // Construction
    }
}
