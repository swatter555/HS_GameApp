using System.Collections.Generic;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// A generation × role stat baseline (Appendix W §2). Carries ~80% of a profile's statline;
    /// deltas and traits adjust off it. Phase 1 ships the tank gen table only (see
    /// <see cref="TankArchetypes"/>); other families are formalised in Phase 2.
    /// </summary>
    public readonly struct Archetype
    {
        public IReadOnlyDictionary<ProfileStat, int> Stats { get; }

        public Archetype(IReadOnlyDictionary<ProfileStat, int> stats)
        {
            Stats = stats;
        }
    }

    /// <summary>
    /// The four tank generation archetypes, exactly as published in Appendix W §16 (SA spread + GAD
    /// inversion applied — the R1/R2 corrections). These ARE the source of truth for tank stats; the old
    /// pre-migration GameData baseline consts were removed in Phase 4. The resolver/test read these §16 values.
    /// </summary>
    public static class TankArchetypes
    {
        //                                  HA  HD  SA  SD  GAD  MMP
        public static readonly Archetype Gen1 = Make(7, 5, 5, 6, 7, 10);   // T-55, T-62
        public static readonly Archetype Gen2 = Make(10, 8, 7, 6, 7, 10);  // T-64A, M60, Leo1-late
        public static readonly Archetype Gen3 = Make(13, 11, 9, 6, 7, 10); // T-64B/72B/80, M1, Leo2, Chally1
        public static readonly Archetype Gen4 = Make(16, 14, 10, 6, 7, 10);// T-80U/BV, M1A1HA

        private static Archetype Make(int ha, int hd, int sa, int sd, int gad, int mmp)
            => new Archetype(new Dictionary<ProfileStat, int>
            {
                { ProfileStat.HA, ha },
                { ProfileStat.HD, hd },
                { ProfileStat.SA, sa },
                { ProfileStat.SD, sd },
                { ProfileStat.GAD, gad },
                { ProfileStat.MMP, mmp },
                // Family base spotting/primary range (§12.3.1 — SR 2 / PR 1 defaults). Per design §2
                // (Option A) these flow through the resolver, so OPTICS/THERMAL SR+1 and GUN_LAUNCHED_ATGM
                // PR+1 (standoff) adjust off this base.
                { ProfileStat.SR, 2 },
                { ProfileStat.PR, 1 }
            });
    }

    /// <summary>
    /// A complete profile definition: archetype + one-off deltas + trait list. The shape the
    /// resolver validates in Phase 1 and the Phase 3 DB rebuild will reuse per profile.
    /// </summary>
    public readonly struct ProfileDef
    {
        public Archetype Archetype { get; }
        public IReadOnlyDictionary<ProfileStat, int> Deltas { get; }
        public IReadOnlyList<WeaponTrait> Traits { get; }

        public ProfileDef(Archetype archetype,
                          IReadOnlyDictionary<ProfileStat, int> deltas,
                          IReadOnlyList<WeaponTrait> traits)
        {
            Archetype = archetype;
            Deltas = deltas;
            Traits = traits;
        }
    }
}
