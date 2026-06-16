using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Resolves a ProfileDef (archetype + deltas + traits) into final stats and a stored ICM:
    ///   finalStat = clamp(archetype + Σdeltas + Σ(live trait StatDeltas), 1, 25)
    ///   storedICM = Π(live IcmMultiply traits), clamped to [ICM_MIN, ICM_MAX], default 1.0
    /// StatFloor effects raise a stat to a minimum after deltas; Capability effects are collected;
    /// Dormant effects are skipped. (Appendix W §1.)
    /// </summary>
    public static class TraitResolver
    {
        #region Constants

        private const string CLASS_NAME = nameof(TraitResolver);
        private const int STAT_MIN = 1;
        private const int STAT_MAX = 25;

        /// <summary>
        /// The combat stats that ride the 11-band Δ ladder and are clamped to [1, 25]
        /// (Appendix W §1). The remaining ProfileStats — MMP, OL, STL, PR, IR, SR — are NOT
        /// band stats: they carry their own natural ranges (MMP 0 for static, 100 for fixed-wing;
        /// OL 6–16; ranges 0+), so the §1 "1..25" clamp must not apply to them. They are only
        /// floored at 0 to keep a malus from driving them negative.
        /// </summary>
        private static readonly HashSet<ProfileStat> BandStats = new HashSet<ProfileStat>
        {
            ProfileStat.HA,  ProfileStat.HD,  ProfileStat.SA,  ProfileStat.SD,
            ProfileStat.GAT, ProfileStat.GAD, ProfileStat.DF,  ProfileStat.MAN,
            ProfileStat.TS,  ProfileStat.SUR, ProfileStat.GA
        };

        #endregion // Constants

        #region Result

        /// <summary>Immutable output of a resolve.</summary>
        public readonly struct Result
        {
            public IReadOnlyDictionary<ProfileStat, int> Stats { get; }
            public float ICM { get; }
            public IReadOnlyCollection<WeaponCapability> Capabilities { get; }

            public Result(IReadOnlyDictionary<ProfileStat, int> stats, float icm,
                          IReadOnlyCollection<WeaponCapability> capabilities)
            {
                Stats = stats;
                ICM = icm;
                Capabilities = capabilities;
            }

            /// <summary>Reads a resolved stat, or 0 if the profile never touched it.</summary>
            public int Stat(ProfileStat stat) => Stats.TryGetValue(stat, out int v) ? v : 0;
        }

        #endregion // Result

        #region Public Methods

        public static Result Resolve(ProfileDef def)
            => Resolve(def.Archetype, def.Deltas, def.Traits);

        public static Result Resolve(Archetype archetype,
                                     IReadOnlyDictionary<ProfileStat, int> deltas,
                                     IEnumerable<WeaponTrait> traits)
        {
            try
            {
                var stats = new Dictionary<ProfileStat, int>();
                if (archetype.Stats != null)
                {
                    foreach (var kv in archetype.Stats)
                        stats[kv.Key] = kv.Value;
                }

                // One-off deltas (residue).
                if (deltas != null)
                {
                    foreach (var kv in deltas)
                        stats[kv.Key] = Get(stats, kv.Key) + kv.Value;
                }

                float icm = GameData.ICM_DEFAULT;
                var floors = new Dictionary<ProfileStat, int>();
                var caps = new HashSet<WeaponCapability>();

                if (traits != null)
                {
                    foreach (WeaponTrait trait in traits)
                    {
                        TraitDef def = WeaponTraitCatalog.Get(trait);
                        if (def == null) continue;

                        foreach (TraitEffect e in def.Effects)
                        {
                            if (e.Status == EffectStatus.Dormant) continue;

                            switch (e.Kind)
                            {
                                case EffectKind.StatDelta:
                                    stats[e.Stat] = Get(stats, e.Stat) + e.Amount;
                                    break;
                                case EffectKind.StatFloor:
                                    floors[e.Stat] = Math.Max(Get(floors, e.Stat), e.Amount);
                                    break;
                                case EffectKind.IcmMultiply:
                                    icm *= e.Multiplier;
                                    break;
                                case EffectKind.Capability:
                                    caps.Add(e.Capability);
                                    break;
                            }
                        }
                    }
                }

                // Floors apply after additive deltas, never lowering a stat.
                foreach (var kv in floors)
                    stats[kv.Key] = Math.Max(Get(stats, kv.Key), kv.Value);

                // Band stats clamp to the [1, 25] ladder; non-band stats (MMP/ranges/OL/STL)
                // only floor at 0 — see BandStats note.
                var keys = new List<ProfileStat>(stats.Keys);
                foreach (ProfileStat k in keys)
                    stats[k] = BandStats.Contains(k)
                        ? Math.Clamp(stats[k], STAT_MIN, STAT_MAX)
                        : Math.Max(0, stats[k]);

                icm = Math.Clamp(icm, GameData.ICM_MIN, GameData.ICM_MAX);

                return new Result(stats, icm, caps);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Resolve), e);
                throw;
            }
        }

        #endregion // Public Methods

        #region Private Methods

        private static int Get(Dictionary<ProfileStat, int> map, ProfileStat key)
            => map.TryGetValue(key, out int v) ? v : 0;

        #endregion // Private Methods
    }
}
