using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// The RECONA aircraft in a Recon Box mission (HS_DesignDoc §11.11). Its air stats drive the three opposed
    /// rolls; <see cref="QualityMult"/> is the Strength × Efficiency × Experience × ICM damage stack used only if
    /// the mission reaches the Roll-3 combat round and the recon is the winner (the caller builds it off the
    /// CombatUnit, exactly as for the dogfight engine). Experience also feeds the additive ratings via §7.9.4.
    /// </summary>
    public struct ReconInput
    {
        public int TopSpeed;          // TS
        public int Maneuverability;   // MAN
        public int Survivability;     // SUR
        public int Stealth;           // STL
        public ExperienceLevel Experience;
        public float QualityMult;     // Strength × Efficiency × Experience × ICM (Roll-3 damage stack)
    }

    /// <summary>
    /// The lone defender FGT that may engage a recon mission (§11.11.1 — at most one interceptor). Carries only the
    /// stats the recon rolls need (§11.11.8/.10) plus its damage stack for a Roll-3 win.
    /// </summary>
    public struct InterceptorInput
    {
        public int TopSpeed;          // TS
        public int Dogfighting;       // DF
        public ExperienceLevel Experience;
        public float QualityMult;     // Strength × Efficiency × Experience × ICM (Roll-3 damage stack)
    }

    /// <summary>
    /// Mission outcome tier (§11.11.8/.9/.10) — the % of intel the recon brought home. Drives the per-tier spotting
    /// application (§11.11.11), which is the CALLER's job (it sweeps the search-area units). Numeric values are the
    /// design-doc percentages for readability; they are NOT used as multipliers here.
    /// </summary>
    public enum ReconMissionTier
    {
        Marginal = 25,   // §11.11.10.6 — recon mauled in the combat round, kept ~25% of its data
        Partial = 50,    // §11.11.8.6/.9/.10.5 — recon lost the detection contest but escaped or fought off the interceptor
        Full = 100,      // §11.11.7/.8.5 — stealth bypass or won the detection contest: clean 100% run
    }

    /// <summary>
    /// Result of a recon mission resolution. The engine returns the tier and the HP each aircraft should lose; the
    /// CALLER applies the damage (TakeDamage), the §11.11.11 spotting sweep, and fixed-wing auto-return — mirroring
    /// how <see cref="AirCombatEngine"/> returns numbers without touching CombatUnits.
    /// </summary>
    public struct ReconMissionResult
    {
        public ReconMissionTier Tier;
        public bool StealthBypassed;     // §11.11.7 — the interceptor never engaged
        public bool ReachedCombat;       // §11.11.10 — Roll 3 ran (the only HP-exchange phase)
        public bool ReconWonCombat;      // Roll-3 winner (meaningful only when ReachedCombat)
        public int DamageToRecon;        // HP the recon should lose (caller applies)
        public int DamageToInterceptor;  // HP the interceptor should lose (caller applies)
    }

    /// <summary>
    /// The pure Recon Box resolution chain (HS_DesignDoc §11.11): stealth bypass (§11.11.7) → Roll 1 mission-success
    /// contest (§11.11.8) → Roll 2 escape 1d30 (§11.11.9) → Roll 3 combat round whose Δ surrogate feeds the §7.6
    /// band table, the winner damaging the loser through the §7.7.1 engine (§11.11.10). Pure — dice via
    /// <see cref="ICombatRandom"/>, no CombatUnit coupling and no damage applied. The §11.11.11 per-tier spotting
    /// sweep, the HP application, the CombatAction cost (§8.5.2), and fixed-wing auto-return are the caller's.
    /// The "no defender interceptor committed → auto 100%" case (§11.11.5) is trivial caller logic and is NOT
    /// modelled here — this resolver assumes one interceptor engages.
    /// </summary>
    public static class ReconMissionEngine
    {
        private const string CLASS_NAME = nameof(ReconMissionEngine);

        #region Ratings (§11.11.8 / §11.11.10)

        /// <summary>Roll-1 recon rating (§11.11.8.1): floor((TS×2 + MAN)/3) + Experience_mod.</summary>
        public static int ReconMissionRating(in ReconInput r) =>
            (r.TopSpeed * 2 + r.Maneuverability) / 3 + StandCheck.ExperienceStandMod(r.Experience);

        /// <summary>Roll-1 interceptor rating (§11.11.8.2): floor((TS + DF)/2) + Experience_mod.</summary>
        public static int InterceptorMissionRating(in InterceptorInput i) =>
            (i.TopSpeed + i.Dogfighting) / 2 + StandCheck.ExperienceStandMod(i.Experience);

        /// <summary>Roll-3 recon rating (§11.11.10.1): floor((TS×3 + SUR)/4) + Experience_mod.</summary>
        public static int ReconCombatRating(in ReconInput r) =>
            (r.TopSpeed * 3 + r.Survivability) / 4 + StandCheck.ExperienceStandMod(r.Experience);

        /// <summary>Roll-3 interceptor rating (§11.11.10.2): floor((DF + TS×2)/3) + Experience_mod.</summary>
        public static int InterceptorCombatRating(in InterceptorInput i) =>
            (i.Dogfighting + i.TopSpeed * 2) / 3 + StandCheck.ExperienceStandMod(i.Experience);

        #endregion // Ratings

        #region Resolution (§11.11.7–§11.11.10)

        /// <summary>
        /// Resolves the full mission chain. Dice order (so tests can script it): the stealth 1d100 (ONLY if recon
        /// STL &gt; 0, §11.5), then Roll 1 = recon 1d6 + interceptor 1d6, then Roll 2 = one 1d30, then Roll 3 =
        /// recon 1d6 + interceptor 1d6 followed by the winner's §7.6 band die(s). The chain short-circuits at the
        /// first decisive step, so later dice are consumed only if reached.
        /// </summary>
        public static ReconMissionResult Resolve(in ReconInput recon, in InterceptorInput interceptor, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                // §11.11.7 — stealth bypass: STL 0 short-circuits without a die (RollStealthAvoidance), so no roll is
                // wasted for a non-stealth recon. Success → the interceptor never engages, clean 100% mission.
                if (AirCombatEngine.RollStealthAvoidance(recon.Stealth, rng))
                    return new ReconMissionResult { Tier = ReconMissionTier.Full, StealthBypassed = true };

                // §11.11.8 — Roll 1 mission-success contest; tie favours the recon (§11.11.8.4). Win → 100%, done.
                int reconR1 = ReconMissionRating(recon) + rng.RollDie(6);
                int interceptorR1 = InterceptorMissionRating(interceptor) + rng.RollDie(6);
                if (reconR1 >= interceptorR1)
                    return new ReconMissionResult { Tier = ReconMissionTier.Full };

                // §11.11.9 — Roll 2 escape: 1d30 ≤ recon TS → break clean at the provisional 50%, no HP exchange.
                if (rng.RollDie(30) <= recon.TopSpeed)
                    return new ReconMissionResult { Tier = ReconMissionTier.Partial };

                // §11.11.10 — Roll 3 combat round; tie favours the recon (§11.11.10.7). Δ surrogate → §7.6 band, the
                // winner's pipeline damaging the loser. Recon win → 50% + interceptor hit; recon loss → 25% + recon hit.
                int reconR3 = ReconCombatRating(recon) + rng.RollDie(6);
                int interceptorR3 = InterceptorCombatRating(interceptor) + rng.RollDie(6);
                bool reconWins = reconR3 >= interceptorR3;

                int deltaSurrogate = Math.Abs(reconR3 - interceptorR3);
                int dmg = ResolveCombatRoundLane(deltaSurrogate, reconWins ? recon.QualityMult : interceptor.QualityMult, rng);

                return new ReconMissionResult
                {
                    Tier = reconWins ? ReconMissionTier.Partial : ReconMissionTier.Marginal,
                    ReachedCombat = true,
                    ReconWonCombat = reconWins,
                    DamageToInterceptor = reconWins ? dmg : 0,
                    DamageToRecon = reconWins ? 0 : dmg,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Resolve), e);
                return new ReconMissionResult { Tier = ReconMissionTier.Full };
            }
        }

        /// <summary>
        /// The Roll-3 damage path (§11.11.10.4): the rating Δ is fed straight to the band table as a Δ surrogate and
        /// the winner's pipeline deals the damage. Routed through the §7.7.1 engine with FirerAttack = the surrogate
        /// and TargetDefense = 0 so the band is DeltaBand(surrogate); FirerIsAir → AirBalanceMod, terrain bypassed
        /// (aloft), no OL and no deployment (fixed-wing, §10.3c.1).
        /// </summary>
        private static int ResolveCombatRoundLane(int deltaSurrogate, float winnerQualityMult, ICombatRandom rng)
        {
            var lane = new LaneInput
            {
                FirerAttack = deltaSurrogate,
                TargetDefense = 0,
                FirerQualityMult = winnerQualityMult,
                FirerIsAir = true,                 // → AirBalanceMod (§7.7.10)
                AttackType = AttackType.Direct,    // not Airstrike → no OL multiplier
                BypassTerrainBlock = true,         // aloft — no terrain block
            };
            return CombatEngine.ResolveLane(lane, rng);
        }

        #endregion // Resolution
    }
}
