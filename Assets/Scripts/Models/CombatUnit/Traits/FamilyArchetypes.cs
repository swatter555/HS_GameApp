using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Generation × role stat baselines for every non-tank weapon family (Appendix W §2 / §7B).
    /// Tanks live in <see cref="TankArchetypes"/>; this class covers the rest. Like TankArchetypes,
    /// these encode the RATIFIED Appendix W values (R1 GAD inversion, R4 IFV soft-attack, W7 GAT
    /// default 0) and therefore intentionally differ from the still-pre-correction live GameData
    /// constants (e.g. GROUND_DEFENSE_INFANTRY 6, BASE_IFV_SOFT_ATTACK 7) — GameData reconciliation
    /// lands in Phase 3/4. The resolver/tests read these values directly.
    ///
    /// Conventions:
    ///  - "Towed = foot": ART / AAA / SAM carry MMP 4 (the emplaced weapon). Self-propelled and
    ///    truck-mobile variants get their mobility from a transport (Mobile-slot) profile or a
    ///    chassis trait/delta in Phase 3 — there are no separate SP archetypes.
    ///  - GAT is carried ONLY where the family has a real baseline (AAA 9, SAM 10). Everything else
    ///    is GAT 0 (W7) and gains air-attack via the MANPADS trait (GAT floor) in Phase 3.
    ///  - GA / OL on aircraft are per-profile tiers (§7B.4/§7B.5), NOT archetype baselines — applied
    ///    as deltas during the Phase 3 rebuild.
    /// </summary>
    public static class FamilyArchetypes
    {
        #region Ground families

        //                                              HA HD SA SD GAD MMP  (GAT)
        /// <summary>Foot infantry baseline; soaks airstrikes (GAD 10, R1), MANPADS adds GAT later.</summary>
        public static readonly Archetype Infantry   = Ground(5, 7, 7, 8, 10, 4);
        /// <summary>APC (MOT) — light-armour GAD 7 (R1).</summary>
        public static readonly Archetype Apc        = Ground(3, 4, 6, 7, 7, 8);
        /// <summary>IFV (MECH) — soft attack 8 (R4), light-armour GAD 7 (R1).</summary>
        public static readonly Archetype Ifv        = Ground(4, 4, 8, 7, 7, 10);
        /// <summary>Light scout car (BRDM/M3-class) — weak gun but DELIBERATELY survivable (HD 5 / SD 9) so
        /// scouts soak the first blow and withdraw rather than getting one-shot out front (design call,
        /// 2026-06-15: "harden the hull"). Fast, SR 3. Add RECON_FRAGILE (R6) per scout profile to discourage
        /// brawling (offense ICM ×0.6); AT-recon variants drop it and add an ATGM trait instead.</summary>
        public static readonly Archetype Recon      = Ground(2, 5, 5, 9, 7, 10, sr: 3);
        /// <summary>Towed artillery baseline; soft towed GAD 8. SP gun = +mobility in Phase 3.</summary>
        public static readonly Archetype Artillery  = Ground(5, 5, 9, 5, 8, 4);
        /// <summary>Towed AAA; resists air (GAD 12) and engages it (GAT 9), SR 3. SP = +mobility in Phase 3.</summary>
        public static readonly Archetype Aaa        = Ground(4, 4, 9, 6, 12, 4, gat: 9, sr: 3);
        /// <summary>Towed/site SAM; air-only (HA/SA 1, §7A.13), GAT 10, SR 6. SP = +mobility in Phase 3.</summary>
        public static readonly Archetype Sam        = Ground(1, 3, 1, 3, 8, 4, gat: 10, sr: 6);
        /// <summary>Attack-helicopter gunship; fast (MMP 24), glass-cannon (§7A.14); elevated observation SR 3.</summary>
        public static readonly Archetype Helicopter = Ground(7, 6, 10, 7, 10, 24, sr: 3);
        /// <summary>Soft transport; thin-topped air target (GAD 6).</summary>
        public static readonly Archetype Truck      = Ground(3, 3, 3, 3, 6, 8);
        /// <summary>Static base (HQ/DEPOT/AIRB); MANPADS-equippable (GAD 6), SR 4. HP 60 is a CombatUnit concern.</summary>
        public static readonly Archetype Facility   = Ground(4, 6, 6, 7, 6, 0, sr: 4);

        #endregion // Ground families

        #region Air families

        //                                             DF MAN TS SUR  MMP
        /// <summary>Early jet (MiG-21, F-4).</summary>
        public static readonly Archetype FighterEarly = Air(8, 9, 10, 6, 100);
        /// <summary>Mid jet (MiG-23/27, F-16).</summary>
        public static readonly Archetype FighterMid   = Air(10, 11, 10, 7, 100);
        /// <summary>Late jet (MiG-29, Su-27, F-15).</summary>
        public static readonly Archetype FighterLate  = Air(12, 12, 10, 9, 100);
        /// <summary>Attack aircraft (Su-25, A-10) — low agility, high survivability.</summary>
        public static readonly Archetype Attack       = Air(4, 4, 7, 10, 100);
        /// <summary>Bomber (Tu-22, F-111) — no dogfight, fast, durable.</summary>
        public static readonly Archetype Bomber       = Air(1, 3, 10, 8, 100);

        #endregion // Air families

        #region Factories

        /// <summary>
        /// Builds a ground archetype (HA/HD/SA/SD/GAD/MMP, plus GAT when the family has a real
        /// air-attack baseline). GAT is omitted when 0 so it never gets clamped up off the W7 default.
        /// </summary>
        private static Archetype Ground(int ha, int hd, int sa, int sd, int gad, int mmp,
                                        int gat = 0, int sr = 2, int pr = 1)
        {
            var stats = new Dictionary<ProfileStat, int>
            {
                { ProfileStat.HA, ha },
                { ProfileStat.HD, hd },
                { ProfileStat.SA, sa },
                { ProfileStat.SD, sd },
                { ProfileStat.GAD, gad },
                { ProfileStat.MMP, mmp },
                // Family base spotting / primary-fire range (§12.3.1). Defaults SR 2 / PR 1; families with a
                // different baseline (recon SR 3, AAA SR 3, SAM SR 6, facility SR 4) override sr. Per design §2
                // (Option A) these flow through the resolver so optics/sensor traits adjust off the base.
                { ProfileStat.SR, sr },
                { ProfileStat.PR, pr }
            };
            if (gat > 0) stats[ProfileStat.GAT] = gat;
            return new Archetype(stats);
        }

        /// <summary>Builds a fixed-wing archetype (DF/MAN/TS/SUR/MMP, plus base air SR). GA/OL are per-profile
        /// deltas. Base SR 4 = W8 AIR_UNIT_SPOTTING_RANGE (Option A — routed through the resolver); recon (8) and
        /// AWACS (12) add the difference as a per-profile delta.</summary>
        private static Archetype Air(int df, int man, int ts, int sur, int mmp, int sr = 4)
            => new Archetype(new Dictionary<ProfileStat, int>
            {
                { ProfileStat.DF, df },
                { ProfileStat.MAN, man },
                { ProfileStat.TS, ts },
                { ProfileStat.SUR, sur },
                { ProfileStat.MMP, mmp },
                { ProfileStat.SR, sr }
            });

        #endregion // Factories
    }
}
