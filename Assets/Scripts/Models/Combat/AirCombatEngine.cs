using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// One participant in an air-to-air dogfight pass (HS_DesignDoc §11.4.8.2). The air stats drive the
    /// offense/defense ratings; <see cref="QualityMult"/> is the firer's Strength × Efficiency × Experience ×
    /// ICM damage stack (the caller builds it off the CombatUnit, exactly as for ground). Experience also feeds
    /// the additive ratings (pairing, breakthrough, stand) via the §7.9.4 mod.
    /// </summary>
    public struct DogfighterInput
    {
        public int Dogfighting;        // DF
        public int Maneuverability;    // MAN
        public int TopSpeed;           // TS
        public int Survivability;      // SUR
        public ExperienceLevel Experience;
        public float QualityMult;      // Strength × Efficiency × Experience × ICM
    }

    /// <summary>
    /// Outcome of one MUTUAL dogfight pass: the HP each side took and each side's binary air stand result.
    /// "A" and "B" are the two paired aircraft (escort↔interceptor, or interceptor↔bomber) — the pass is
    /// symmetric, so the labels carry no attacker/defender asymmetry.
    /// </summary>
    public struct DogfightPassResult
    {
        public int DamageToA;
        public int DamageToB;
        public AirStandOutcome StandA;
        public AirStandOutcome StandB;
    }

    /// <summary>
    /// The pure air-to-air engine (HS_DesignDoc §11.4.8): the dogfight rating formulas, a mutual dogfight pass
    /// that REUSES the §7.7.1 damage engine (<see cref="CombatEngine.ResolveLane"/> with FirerIsAir → AirBalanceMod,
    /// terrain bypassed, no deployment, no OL), the post-pass air stand checks (§11.4.8.2a), the breakthrough
    /// opposed roll (§11.4.8.2.1), and the stealth avoidance roll (§11.5). Pure — dice via <see cref="ICombatRandom"/>,
    /// no CombatUnit coupling. Best-vs-best PAIRING and the AOB box orchestration (who fights whom, in what order)
    /// are the caller's job (the movement/turn-loop layer).
    /// </summary>
    public static class AirCombatEngine
    {
        private const string CLASS_NAME = nameof(AirCombatEngine);

        #region Dogfight ratings (§11.4.8.2)

        /// <summary>Dogfight OFFENSE rating (§11.4.8.2): floor((DF + MAN) / 2).</summary>
        public static int DogfightOffense(int df, int man) => (df + man) / 2;

        /// <summary>Dogfight DEFENSE rating (§11.4.8.2): floor((MAN × 2 + SUR) / 3).</summary>
        public static int DogfightDefense(int man, int sur) => (man * 2 + sur) / 3;

        /// <summary>
        /// Best-vs-best PAIRING metric (§11.4.8.2): DogfightOffense + Experience_mod. The caller sorts escorts
        /// and interceptors by this descending and pairs top-of-list 1:1.
        /// </summary>
        public static int PairingMetric(in DogfighterInput d) =>
            DogfightOffense(d.Dogfighting, d.Maneuverability) + StandCheck.ExperienceStandMod(d.Experience);

        #endregion // Dogfight ratings

        #region Dogfight pass (§11.4.8.2 + §11.4.8.2a)

        /// <summary>
        /// Resolves one MUTUAL dogfight pass: each aircraft's offense vs the other's defense through the §7.7.1
        /// engine (FirerIsAir → AirBalanceMod, terrain bypassed, no deployment, no OL), damage applied to both,
        /// then a binary air stand check for each on the HP it took THIS pass (§11.4.8.2a).
        ///
        /// Dice order: lane A→B band die(s), lane B→A band die(s), A's 1d10 stand, B's 1d10 stand.
        /// </summary>
        public static DogfightPassResult ResolveDogfightPass(in DogfighterInput a, in DogfighterInput b, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int dmgToB = ResolveDogfightLane(a, b, rng);
                int dmgToA = ResolveDogfightLane(b, a, rng);

                AirStandOutcome standA = AirStandCheck.ResolveStand(
                    AirStandCheck.ComputeStandValue(new AirStandInput
                    {
                        TopSpeed = a.TopSpeed,
                        Maneuverability = a.Maneuverability,
                        Experience = a.Experience,
                        HpLostThisPass = dmgToA,
                    }), rng);

                AirStandOutcome standB = AirStandCheck.ResolveStand(
                    AirStandCheck.ComputeStandValue(new AirStandInput
                    {
                        TopSpeed = b.TopSpeed,
                        Maneuverability = b.Maneuverability,
                        Experience = b.Experience,
                        HpLostThisPass = dmgToB,
                    }), rng);

                return new DogfightPassResult
                {
                    DamageToA = dmgToA,
                    DamageToB = dmgToB,
                    StandA = standA,
                    StandB = standB,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveDogfightPass), e);
                return default;
            }
        }

        /// <summary>One direction of a dogfight: firer offense vs target defense through the air pipeline.</summary>
        private static int ResolveDogfightLane(in DogfighterInput firer, in DogfighterInput target, ICombatRandom rng)
        {
            var lane = new LaneInput
            {
                FirerAttack = DogfightOffense(firer.Dogfighting, firer.Maneuverability),
                TargetDefense = DogfightDefense(target.Maneuverability, target.Survivability),
                FirerQualityMult = firer.QualityMult,
                FirerIsAir = true,                 // → AirBalanceMod (§7.7.10)
                AttackType = AttackType.Direct,    // not Airstrike → no OL multiplier
                BypassTerrainBlock = true,         // air-to-air: no terrain block
            };
            return CombatEngine.ResolveLane(lane, rng);
        }

        #endregion // Dogfight pass

        #region Breakthrough (§11.4.8.2.1)

        /// <summary>
        /// The breakthrough opposed roll (§11.4.8.2.1), run ONLY when both aircraft held their stand checks (if
        /// the escort retreated the interceptor auto-breaks; if the interceptor retreated there is no breakthrough).
        ///   interceptor rating = floor((TS + MAN) / 2) + Experience_mod − floor(interceptorDmgTakenPct / 25)
        ///   escort screen rating = floor((DF + MAN) / 2) + Experience_mod
        /// Each adds 1d6; higher total wins; a TIE favors the interceptor. Returns true if the interceptor breaks
        /// through to the bomber pass (§11.4.8.3).
        ///
        /// Dice order: interceptor 1d6, escort 1d6.
        /// </summary>
        public static bool ResolveBreakthrough(
            in DogfighterInput interceptor,
            int interceptorDmgTakenPct,
            in DogfighterInput escort,
            ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int interceptorRating = (interceptor.TopSpeed + interceptor.Maneuverability) / 2
                                        + StandCheck.ExperienceStandMod(interceptor.Experience)
                                        - Math.Max(0, interceptorDmgTakenPct) / 25;
                int escortRating = (escort.Dogfighting + escort.Maneuverability) / 2
                                   + StandCheck.ExperienceStandMod(escort.Experience);

                int interceptorTotal = interceptorRating + rng.RollDie(6);
                int escortTotal = escortRating + rng.RollDie(6);

                return interceptorTotal >= escortTotal; // tie favors the interceptor
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveBreakthrough), e);
                return false;
            }
        }

        #endregion // Breakthrough

        #region Stealth avoidance (§11.5)

        /// <summary>
        /// Stealth avoidance chance % by STL rating (§11.5): 0→0, 1-2→15, 3-4→30, 5-6→45, 7-8→60, 9→75, 10→85.
        /// </summary>
        public static int StealthAvoidanceChance(int stl)
        {
            if (stl <= 0) return 0;
            if (stl <= 2) return 15;
            if (stl <= 4) return 30;
            if (stl <= 6) return 45;
            if (stl <= 8) return 60;
            if (stl <= 9) return 75;
            return 85; // STL 10
        }

        /// <summary>
        /// Rolls the stealth avoidance check (§11.4.8.1 / §11.5): 1d100 ≤ StealthAvoidanceChance(stl) means the
        /// strike aircraft bypasses interception this engagement. STL 0 short-circuits to false (no dice). Dice: one 1d100.
        /// </summary>
        public static bool RollStealthAvoidance(int stl, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int chance = StealthAvoidanceChance(stl);
                if (chance <= 0) return false;
                return rng.RollDie(100) <= chance;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollStealthAvoidance), e);
                return false;
            }
        }

        #endregion // Stealth avoidance
    }
}
