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
        MMP   // Max Movement Points
    }
}
