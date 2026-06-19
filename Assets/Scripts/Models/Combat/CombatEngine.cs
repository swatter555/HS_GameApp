using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Inputs for one attack LANE of the damage engine (HS_DesignDoc §7.7.1). A "lane" is one direction of
    /// fire: the attacker→defender forward lane, or the defender→attacker return lane (§7.7.3), each resolved
    /// by a separate <see cref="CombatEngine.ResolveLane"/> call.
    ///
    /// The stats are the RAW active-WeaponProfile values on the already-resolved axis (caller picks Hard/Soft
    /// by the TARGET's class, §7.4.1). The multiplier pieces are kept separate so the engine assembles step 4
    /// itself and the formula lives in one place. Unset numeric fields default to a neutral value (see notes),
    /// so a caller only sets what a given lane needs.
    /// </summary>
    public struct LaneInput
    {
        /// <summary>Firer's raw attack stat on the resolved axis (HA/SA/GA/GAT…), engine step 2.</summary>
        public int FirerAttack;

        /// <summary>Target's raw defense stat on the resolved axis (HD/SD/GAD…), engine step 2.</summary>
        public int TargetDefense;

        /// <summary>Firer's Experience × Strength × Efficiency × ICM product (engine step 4 core). 0 = treat as 1.0.</summary>
        public float FirerQualityMult;

        /// <summary>Firer's deployment COMBAT_MOD (1.0–1.3); applied ONLY on a return lane (§7.5.2). 0 = treat as 1.0.</summary>
        public float FirerDeploymentMod;

        /// <summary>True if this is a defender's return-fire lane — the only case the deployment mult applies (§7.5.2).</summary>
        public bool FirerIsDefender;

        /// <summary>Firer's Ordnance Load; applied as OL/9 ONLY when <see cref="AttackType"/> is Airstrike (§11.6.1).</summary>
        public int OrdnanceLoad;

        /// <summary>Which engine configuration (direct / indirect / airstrike).</summary>
        public AttackType AttackType;

        /// <summary>True if the FIRER is an air unit — selects AirBalanceMod over GroundBalanceMod (§7.7.10).</summary>
        public bool FirerIsAir;

        /// <summary>Terrain of the TARGET's hex; supplies the flat-HP block (§7.5.6). Defender's hex only.</summary>
        public TerrainType TargetTerrain;

        /// <summary>Skip the terrain (and crossing) block entirely — BM-class attacker (§7.5.6.6) or ambushed victim (§7.5.6.7).</summary>
        public bool BypassTerrainBlock;

        /// <summary>Direct-fire attack across a river edge — adds the Contested Crossing Block (1d4, §7.5.6.9). Direct only.</summary>
        public bool ContestedCrossing;

        /// <summary>Band shift in rungs: +1 embarkment malus (§7.10.1.1) or WW survival (§11.1.2.4); 0 normally.</summary>
        public int BandShift;

        /// <summary>Post-stack damage scalar: ambush 1.5 (§6.9.4), night-ambush 2.0, embarkment 2.0 (§7.10.1.2). 0 = treat as 1.0.</summary>
        public float PostStackScalar;
    }

    /// <summary>
    /// Outcome of a full direct engagement (HS_DesignDoc §7.7.3 / §7.9). Holds the HP each side dealt and the
    /// defender's stand result. The caller applies the HP to both units and carries out the displacement the
    /// outcome implies (retreat path, posture drop, Automatic Advance) at the map layer.
    /// </summary>
    public struct DirectEngagementResult
    {
        /// <summary>HP the attacker's forward lane dealt to the defender.</summary>
        public int DamageToDefender;

        /// <summary>HP the defender's return lane dealt to the attacker (§6.12 universal return fire). Attacker takes no stand check (§7.4.2.3).</summary>
        public int DamageToAttacker;

        /// <summary>Shock the defender's stand check used (§7.9.1.1), derived from <see cref="DamageToDefender"/>.</summary>
        public int DefenderShock;

        /// <summary>Stand Value the defender's check used (§7.9.1).</summary>
        public int DefenderStandValue;

        /// <summary>The defender's stand outcome (§7.9.5).</summary>
        public StandOutcome DefenderOutcome;
    }

    /// <summary>
    /// The damage engine (HS_DesignDoc §7.7.1) — one pure function, three configurations (direct / indirect /
    /// airstrike). Given a single <see cref="LaneInput"/> and a dice source it returns the HP that lane deals.
    /// Direct combat (§7.7.3) is two ResolveLane calls (forward + return) on pre-damage stats; the caller
    /// applies both results afterward. No CombatUnit / GameDataManager coupling — fully unit-testable.
    /// </summary>
    public static class CombatEngine
    {
        private const string CLASS_NAME = nameof(CombatEngine);

        /// <summary>
        /// Resolves one attack lane to HP dealt, running the §7.7.1 pipeline:
        ///   1. axis Δ = FirerAttack − TargetDefense                    (axis already chosen by caller)
        ///   2. band = DeltaBand(Δ), then apply BandShift               (§7.6 / §7.7.2)
        ///   3. baseHP = RollBandDamage(band)                           (direct HP)
        ///   4. dmg = baseHP × quality [× deployment if defender] [× OL/9 if airstrike] × postStackScalar
        ///   5. dmg = round(dmg) − terrainBlock − crossingBlock         (skipped if BypassTerrainBlock)
        ///   6. dmg = round(dmg × BalanceMod)                           (ground or air, §7.7.10)
        ///   7. floor: a connecting hit (baseHP &gt; 0) lands ≥ 1; a natural 0 stays 0 (§7.5.6.4)
        /// </summary>
        public static int ResolveLane(in LaneInput input, ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                // Steps 1–2: delta → band → optional shift.
                int delta = input.FirerAttack - input.TargetDefense;
                DamageBand band = CombatMath.DeltaBand(delta);
                if (input.BandShift != 0)
                    band = CombatMath.ShiftBand(band, input.BandShift);

                // Step 3: roll direct HP. A natural 0 is a miss — remembered for the step-7 floor.
                int baseHP = CombatMath.RollBandDamage(band, rng);

                // Step 4: multiplier stack.
                float quality    = input.FirerQualityMult <= 0f ? 1f : input.FirerQualityMult;
                float deployment = (input.FirerIsDefender)
                                       ? (input.FirerDeploymentMod <= 0f ? 1f : input.FirerDeploymentMod)
                                       : 1f;
                float ol         = (input.AttackType == AttackType.Airstrike) ? input.OrdnanceLoad / 9f : 1f;
                float postScalar = input.PostStackScalar <= 0f ? 1f : input.PostStackScalar;

                float dmg = baseHP * quality * deployment * ol * postScalar;

                // Step 5: round, then subtract the flat terrain (and contested-crossing) block.
                int afterStack = CombatMath.RoundHalfUp(dmg);
                if (!input.BypassTerrainBlock)
                {
                    int block = CombatMath.RollTerrainBlock(input.TargetTerrain, rng);
                    if (input.ContestedCrossing)
                        block += rng.RollDie(4); // §7.5.6.9.2, direct fire only
                    afterStack -= block;
                }

                // Step 6: global balance dial (per the firer's domain).
                float balanceMod = input.FirerIsAir ? GameData.AIR_BALANCE_MOD : GameData.GROUND_BALANCE_MOD;
                int afterBalance = CombatMath.RoundHalfUp(afterStack * balanceMod);

                // Step 7: floor. Connecting hit always lands ≥ 1; a natural 0 deals 0; negatives clamp to 0.
                if (baseHP > 0)
                    return Math.Max(afterBalance, 1);
                return 0;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveLane), e);
                return 0;
            }
        }

        /// <summary>
        /// Resolves a full direct engagement with universal return fire (§7.7.3 / §6.12). Both lanes run on
        /// PRE-damage stats — the caller builds <paramref name="forwardLane"/> (attacker→defender) and
        /// <paramref name="returnLane"/> (defender→attacker, with FirerIsDefender = true) from the units' state
        /// at the start of the engagement — then damage is applied AFTER both resolve. Only the defender takes a
        /// stand check (§7.4.2.3); the attacker eats the return-fire damage with no retreat trigger. The
        /// defender's Shock is taken from the forward-lane damage, so <paramref name="defenderStand"/> need not
        /// pre-fill <see cref="StandValueInput.HpDealtThisAttack"/>.
        ///
        /// Dice are consumed in order: forward lane, return lane, then the defender's 1d10 stand roll.
        /// </summary>
        public static DirectEngagementResult ResolveDirectEngagement(
            in LaneInput forwardLane,
            in LaneInput returnLane,
            in StandValueInput defenderStand,
            ICombatRandom rng)
        {
            try
            {
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int toDefender = ResolveLane(forwardLane, rng);
                int toAttacker = ResolveLane(returnLane, rng);

                StandValueInput stand = defenderStand;
                stand.HpDealtThisAttack = toDefender;
                int sv = StandCheck.ComputeStandValue(stand);
                StandOutcome outcome = StandCheck.ResolveStand(sv, rng);

                return new DirectEngagementResult
                {
                    DamageToDefender = toDefender,
                    DamageToAttacker = toAttacker,
                    DefenderShock = StandCheck.Shock(toDefender),
                    DefenderStandValue = sv,
                    DefenderOutcome = outcome,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveDirectEngagement), e);
                return default;
            }
        }
    }
}
