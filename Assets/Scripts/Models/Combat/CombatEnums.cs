namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// The 11-band damage ladder (HS_DesignDoc §7.6). Bands map Δ (= attacker stat − defender stat)
    /// to a dice expression that rolls DIRECT hit points against MAX_HP. Declared in ASCENDING order
    /// of lethality so a +1 "band shift" (embarkment malus §7.10.1.1, WW survival §11.1.2.4) is a
    /// simple index step, clamped at the ends.
    /// </summary>
    public enum DamageBand
    {
        Hopeless,       // Δ ≤ −13 : 0 HP
        Forlorn,        // −12..−11 : 1d2−1
        Difficult,      // −10..−8  : 1d3−1
        Grim,           // −7..−5   : 1d3
        Disadvantaged,  // −4..−2   : 1d4
        Even,           // −1..+1   : 1d8−1   (the swingy pillar)
        Favorable,      // +2..+4   : 1d6+2
        Advantaged,     // +5..+7   : 1d8+3
        Strong,         // +8..+10  : 1d10+4
        Commanding,     // +11..+13 : 2d6+5
        Crushing        // ≥ +14    : 2d8+6
    }

    /// <summary>
    /// Which configuration of the damage engine is running (HS_DesignDoc §7.7.1, "one engine, three
    /// configurations"). Selects the deployment/OL/terrain rules that differ per attack type.
    /// </summary>
    public enum AttackType
    {
        Direct,     // ground direct fire (two-way; the return lane is a second Direct resolution)
        Indirect,   // artillery / counter-battery (one-way forward; no deployment mult on the firer)
        Airstrike   // air-to-ground GA vs GAD; applies the OL/9 multiplier; fixed-wing skip deployment
    }

    /// <summary>
    /// Terrain damage-mitigation tier (HS_DesignDoc §7.5.6.2). The defender's hex subtracts a flat block
    /// of HP from incoming damage after the multiplier stack; the tier picks the block dice.
    /// </summary>
    public enum TerrainBlockTier
    {
        None,    // Clear, Water (and Impassable — no combat there) : 0
        Light,   // Forest, MinorCity : 1d2
        Medium,  // Rough, Marsh      : 1d4
        Heavy    // MajorCity, Mountains : 1d4+2
    }

    /// <summary>
    /// Result of the defender-only post-damage stand check (HS_DesignDoc §7.9.5). Ordered by severity. The
    /// engine returns the outcome; the displacement/withdrawal it implies (retreat path, posture drop,
    /// Automatic Advance) is applied by the map layer.
    /// </summary>
    public enum StandOutcome
    {
        Hold,      // roll ≤ SV — stays put
        Retreat,   // SV < roll ≤ SV+RETREAT_GAP — falls back 1 hex
        Rout,      // SV+RETREAT_GAP < roll ≤ SV+ROUT_GAP — falls back 2 hexes, drops a dug-in tier
        Shatter    // roll > SV+ROUT_GAP — morale break (+extra damage, then quits the field or surrenders)
    }

    /// <summary>
    /// Result of the Surrender Check (HS_DesignDoc §7.9.6a) — a must-retreat-but-cannot resolution. Pure
    /// experience roll: hold in place at a cost, or be permanently destroyed.
    /// </summary>
    public enum SurrenderOutcome
    {
        HoldInPlace, // 1d20 ABOVE the check number — forced to bare Deployed, takes SURRENDER_SURVIVAL_LOSS
        Destroyed    // 1d20 AT OR BELOW — surrenders, removed from play (attacker gains ½ purchase cost)
    }

    /// <summary>
    /// Result of the binary air-unit stand check (HS_DesignDoc §7.9.8). Unlike the ground StandOutcome there is
    /// no rout/shatter tier (§7.9.8.4) — an aircraft either holds and stays in the engagement, or disengages and
    /// flies home (no hex displacement; the caller applies fixed-wing auto-return, §5.13.5).
    /// </summary>
    public enum AirStandOutcome
    {
        Hold,      // roll ≤ SV_air — remains available for further passes / its mission phase
        Disengage  // roll > SV_air — leaves the AOB and flies home; damage + action commitment stand
    }
}
