namespace HammerAndSickle.Models
{
    /// <summary>
    /// The combat stat axes a trait/delta can address. Mirrors the WeaponProfile rating
    /// vocabulary (Appendix W §1). Resolver output is keyed by this enum.
    /// </summary>
    public enum ProfileStat
    {
        HA,   // Hard Attack
        HD,   // Hard Defense
        SA,   // Soft Attack
        SD,   // Soft Defense
        GAT,  // Ground-Air Attack (interdiction)
        GAD,  // Ground-Air Defense (local)
        DF,   // Dogfighting
        MAN,  // Maneuverability
        TS,   // Top Speed
        SUR,  // Survivability
        GA,   // Ground Attack (aircraft)
        OL,   // Ordnance Load
        STL,  // Stealth
        PR,   // Primary Range
        IR,   // Indirect Range
        SR,   // Spotting Range
        MMP,  // Max Movement Points

        // Air-to-ground STRIKE RIDERS (Rule B / Appendix W §3 R10). NOT band stats and NOT part of the
        // 17-stat line — they are attacker-side, target-conditional bonuses that the resolver accumulates
        // and FromProfileDef stores on the WeaponProfile rider fields. effGA = GA + (Hard?GaVsHard:GaVsSoft)
        // + (base?GaVsBase:0); GAD never splits. The base sub-system riders feed OC suppression / parked
        // hits. STORED but unconsumed until the air-to-ground combat path reads them (see Claude_TODO.md).
        GaVsHard,      // additive onto effGA when the target is Hard
        GaVsSoft,      // additive onto effGA when the target is Soft
        GaVsBase,      // additive onto effGA when the target is a base
        OcSuppression, // base operational-capacity suppression (runway cratering)
        ParkedHit      // band bonus vs parked aircraft on an airbase
    }
}
