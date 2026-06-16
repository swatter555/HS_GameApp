namespace HammerAndSickle.Models
{
    /// <summary>
    /// Which of the three trait homes (Appendix W §1) an effect resolves into, plus the
    /// floor variant used by capability-stat traits (e.g. MANPADS "enables GAT 6").
    /// </summary>
    public enum EffectKind
    {
        StatDelta,    // signed int added to one stat
        StatFloor,    // raise one stat to at least N (does not lower)
        IcmMultiply,  // multiplier folded into storedICM
        Capability    // toggles a WeaponCapability
    }

    /// <summary>
    /// Whether an effect fires now or is catalogued-but-inert awaiting a future system
    /// (~26 traits are dormant per Appendix W §14). The resolver skips Dormant effects.
    /// </summary>
    public enum EffectStatus
    {
        Live,
        Dormant
    }

    /// <summary>
    /// Catalog section a trait belongs to (organisational only; mirrors WeaponTrait_Catalog_01 §2–§12).
    /// </summary>
    public enum TraitCategory
    {
        TankProtection,
        TankFirepower,
        ArtilleryMunition,
        FireControlOptics,
        GroundMobility,
        IfvApc,
        Infantry,
        Helicopter,
        FixedWing,
        AirDefense,
        Engineering,
        Economy
    }
}
