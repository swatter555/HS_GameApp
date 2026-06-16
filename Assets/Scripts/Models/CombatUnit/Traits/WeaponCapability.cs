namespace HammerAndSickle.Models
{
    /// <summary>
    /// Behavioural toggles a Capability-kind trait can enable (Appendix W §1, third trait home).
    /// Phase 1 only collects these into a set; consumers wire in during later phases.
    /// </summary>
    public enum WeaponCapability
    {
        None = 0,

        // Mobility / crossing
        Amphibious,        // T30 — swim open water, cross rivers
        RiverCrossing,     // generic river-edge crossing
        DeepWading,        // T29 — snorkel: cross one unbridged river edge
        AirDroppable,      // T31 — airborne deploy slot
        ExtendedRange,     // T32 — supply-economy bonus
        MountainMovement,  // T46 — reduced move cost in non-clear terrain

        // Air engagement
        EngageAir,         // T42 family — MANPADS grants ground-to-air engage
        FireAndForget,     // T49 / T72 — lock-after-launch, no warning

        // Engineering / utility
        Bridgelayer,       // T77
        MineClearing,      // T78
        FieldFortification,// T79
        RiverAssault,      // T80
        ObstacleBreach,    // T81
        FieldRepair,       // T82 — ARV recovery

        // Air defense behaviour
        WildWeasel,        // T76 — SEAD counter-fire
        ShootScoot,        // T75 — relocate after firing (+1 OpportunityAction)
        MultiEngage,       // T24 — extra target per turn

        // Misc behaviour
        MountedFire,       // T37 — firing ports
        NightObscurant,    // T08/T26/T54 — smoke / IR / NoE concealment
        Stealth,           // T61 — RAM, intercept evasion
        NonCombatant,      // T83 — cannot initiate attack
        RocketArtillery    // T84 — +1 CombatAction (MRL salvo)
    }
}
