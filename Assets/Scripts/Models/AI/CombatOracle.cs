using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.AI
{
    /// <summary>Exact expected-value forecast of one attack lane (the analytic twin of CombatEngine.ResolveLane).</summary>
    public struct LaneForecast
    {
        /// <summary>Exact distribution of HP this lane deals.</summary>
        public Pmf Damage;

        /// <summary>E[HP dealt].</summary>
        public double ExpectedDamage;

        /// <summary>P(natural 0 — the lane misses entirely; §7.5.6.4 floor means every other outcome is ≥ 1).</summary>
        public double MissChance;
    }

    /// <summary>
    /// Map/state context the oracle cannot see for the defender-fate tree (AI-Design-Supplement Part 5.2).
    /// The caller (which has the map) supplies retreat-path validity per §6.8.2a; the oracle supplies the odds.
    /// </summary>
    public struct DefenderFateContext
    {
        /// <summary>Defender's current HP — damage at/above this destroys outright, no stand check.</summary>
        public float DefenderCurrentHP;

        /// <summary>True if at least one §6.8.2a-valid retreat candidate exists (caller-computed from the map).</summary>
        public bool RetreatPathExists;

        /// <summary>True if the defender is at Static efficiency — enables catastrophic collapse (§7.9.7).</summary>
        public bool DefenderIsStatic;
    }

    /// <summary>
    /// The full probabilistic fate of a defender under one attack lane: destroyed / vacates hex / stays,
    /// marginalized over the exact damage distribution and the §7.9 stand-check + §7.9.6/.6a/.7 displacement tree.
    /// PVacatesHex is the number offense planning keys on (feeds Automatic Advance, §7.9.9).
    /// </summary>
    public struct DefenderFateForecast
    {
        /// <summary>Exact damage distribution of the forward lane.</summary>
        public Pmf Damage;

        /// <summary>E[HP dealt].</summary>
        public double ExpectedDamage;

        /// <summary>P(defender permanently destroyed): killed by fire, surrendered, static-collapsed, or bled out holding.</summary>
        public double PDestroyed;

        /// <summary>P(the defender's hex is empty afterward) — destruction, retreat/rout, or shatter-quit (§7.9.9.2).</summary>
        public double PVacatesHex;

        /// <summary>P(shatter-withdrawal survival — unit quits the field but is NOT destroyed; no kill prestige, §7.9.6.5).</summary>
        public double PQuitsField;

        /// <summary>P(defender still occupies the hex) — held, or passed a Surrender Check and survived the loss.</summary>
        public double PStaysInHex;
    }

    /// <summary>Stand-check outcome probabilities (§7.9.5) marginalized over a damage distribution.</summary>
    public struct StandForecast
    {
        public double PHold;
        public double PRetreat;
        public double PRout;
        public double PShatter;
    }

    /// <summary>Both lanes of a direct engagement (§7.7.3) forecast together.</summary>
    public struct EngagementForecast
    {
        public DefenderFateForecast DefenderFate;

        /// <summary>Exact return-fire damage distribution (§6.12 universal return fire).</summary>
        public Pmf DamageToAttacker;

        /// <summary>P(the attacker is destroyed by return fire).</summary>
        public double PAttackerDestroyed;
    }

    /// <summary>
    /// The AI's exact-odds combat forecaster (AI-Design-Supplement Part 5 / S1). Mirrors the §7.7.1 pipeline
    /// analytically over the same LaneInput/StandValueInput structs the live engine rolls — build specs with the
    /// public CombatResolver lane builders and the forecast prices exactly what execution will fire. Band dice,
    /// terrain blocks, the 1d10 stand check, and the surrender/collapse rolls are all small discrete
    /// distributions, so results are EXACT (no sampling). Pure and side-effect free; resolvers are not modified.
    /// ⚠ DRIFT GUARD: CombatOracleTests enumerates the real ResolveLane against these forecasts — if the engine
    /// pipeline changes, those tests fail and this mirror must be updated in the same pass.
    /// </summary>
    public static class CombatOracle
    {
        private const string CLASS_NAME = nameof(CombatOracle);

        #region Band & Terrain Distributions

        /// <summary>Exact Pmf of a damage band's dice (mirror of CombatMath.RollBandDamage, §7.6).</summary>
        public static Pmf BandDamagePmf(DamageBand band) => band switch
        {
            DamageBand.Hopeless      => Pmf.Constant(0),
            DamageBand.Forlorn       => Pmf.DicePlus(1, 2, -1),
            DamageBand.Difficult     => Pmf.DicePlus(1, 3, -1),
            DamageBand.Grim          => Pmf.DicePlus(1, 3, 0),
            DamageBand.Disadvantaged => Pmf.DicePlus(1, 4, 0),
            DamageBand.Even          => Pmf.DicePlus(1, 8, -1),
            DamageBand.Favorable     => Pmf.DicePlus(1, 6, 2),
            DamageBand.Advantaged    => Pmf.DicePlus(1, 8, 3),
            DamageBand.Strong        => Pmf.DicePlus(1, 10, 4),
            DamageBand.Commanding    => Pmf.DicePlus(2, 6, 5),
            DamageBand.Crushing      => Pmf.DicePlus(2, 8, 6),
            _                        => Pmf.Constant(0),
        };

        /// <summary>Exact Pmf of a hex's terrain block (mirror of CombatMath.RollTerrainBlock, §7.5.6.2).</summary>
        public static Pmf TerrainBlockPmf(TerrainType terrain) => CombatMath.BlockTier(terrain) switch
        {
            TerrainBlockTier.Light  => Pmf.Die(2),
            TerrainBlockTier.Medium => Pmf.Die(4),
            TerrainBlockTier.Heavy  => Pmf.DicePlus(1, 4, 2),
            _                       => Pmf.Constant(0),
        };

        #endregion // Band & Terrain Distributions

        #region Lane Forecast

        /// <summary>
        /// Exact damage distribution of one attack lane — the analytic mirror of CombatEngine.ResolveLane
        /// (§7.7.1). The float multiplication order and rounding points replicate the engine line-for-line so
        /// the distribution matches the rolled outcomes EXACTLY, not just in expectation.
        /// </summary>
        public static LaneForecast ForecastLane(in LaneInput input)
        {
            try
            {
                // Steps 1–2: delta → command mitigation → band → optional shift (mirror of ResolveLane).
                int delta = input.FirerAttack - input.TargetDefense;
                if (input.FirerCommand > 0 && delta < -1)
                    delta = Math.Min(delta + input.FirerCommand, -1);

                DamageBand band = CombatMath.DeltaBand(delta);
                if (input.BandShift != 0)
                    band = CombatMath.ShiftBand(band, input.BandShift);

                Pmf basePmf = BandDamagePmf(band);

                // Step 4 stack pieces — identical defaulting to the engine.
                float quality    = input.FirerQualityMult <= 0f ? 1f : input.FirerQualityMult;
                float deployment = (input.FirerIsDefender)
                                       ? (input.FirerDeploymentMod <= 0f ? 1f : input.FirerDeploymentMod)
                                       : 1f;
                float ol         = (input.AttackType == AttackType.Airstrike) ? input.OrdnanceLoad / 9f : 1f;
                float postScalar = input.PostStackScalar <= 0f ? 1f : input.PostStackScalar;
                float balanceMod = input.FirerIsAir ? GameData.AIR_BALANCE_MOD : GameData.GROUND_BALANCE_MOD;

                // Step 5 block distribution: terrain (+ crossing d4), or a point mass at 0 when bypassed.
                Pmf blockPmf = input.BypassTerrainBlock ? Pmf.Constant(0) : TerrainBlockPmf(input.TargetTerrain);
                if (!input.BypassTerrainBlock && input.ContestedCrossing)
                    blockPmf = blockPmf.Add(Pmf.Die(4));

                var damage = new Dictionary<int, double>();
                foreach (KeyValuePair<int, double> b in basePmf.Outcomes)
                {
                    int baseHP = b.Key;
                    if (baseHP == 0) // natural 0 — a miss regardless of everything downstream (step 7)
                    {
                        Pmf.Accumulate(damage, 0, b.Value);
                        continue;
                    }

                    // Steps 4–5a: multiplier stack (float, engine order), round-half-up.
                    float dmg = baseHP * quality * deployment * ol * postScalar;
                    int afterStack = CombatMath.RoundHalfUp(dmg);

                    // Steps 5b–7 per block outcome: subtract, balance-round, floor a connecting hit at 1.
                    foreach (KeyValuePair<int, double> blk in blockPmf.Outcomes)
                    {
                        int afterBlock = afterStack - blk.Key;
                        int afterBalance = CombatMath.RoundHalfUp(afterBlock * balanceMod);
                        Pmf.Accumulate(damage, Math.Max(afterBalance, 1), b.Value * blk.Value);
                    }
                }

                Pmf pmf = Pmf.FromWeights(damage);
                return new LaneForecast
                {
                    Damage = pmf,
                    ExpectedDamage = pmf.ExpectedValue,
                    MissChance = pmf.Prob(0),
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ForecastLane), e);
                return new LaneForecast { Damage = Pmf.Constant(0), ExpectedDamage = 0, MissChance = 1 };
            }
        }

        #endregion // Lane Forecast

        #region Stand Forecast

        /// <summary>
        /// Stand-check outcome probabilities (§7.9.5) marginalized over a damage distribution: for each possible
        /// damage the Shock-adjusted SV is computed via StandCheck.ComputeStandValue (single source of truth),
        /// then the 1d10 is split analytically. Ignores outright destruction — use ForecastDefenderFate for the
        /// full tree.
        /// </summary>
        public static StandForecast ForecastStand(in StandValueInput stand, Pmf damage)
        {
            try
            {
                if (damage == null) throw new ArgumentNullException(nameof(damage));

                var result = new StandForecast();
                foreach (KeyValuePair<int, double> kv in damage.Outcomes)
                {
                    StandValueInput svInput = stand;
                    svInput.HpDealtThisAttack = kv.Key;
                    int sv = StandCheck.ComputeStandValue(svInput);

                    result.PHold    += kv.Value * PRollAtMost(sv);
                    result.PRetreat += kv.Value * (PRollAtMost(sv + GameData.STAND_RETREAT_GAP) - PRollAtMost(sv));
                    result.PRout    += kv.Value * (PRollAtMost(sv + GameData.STAND_ROUT_GAP) - PRollAtMost(sv + GameData.STAND_RETREAT_GAP));
                    result.PShatter += kv.Value * (1.0 - PRollAtMost(sv + GameData.STAND_ROUT_GAP));
                }
                return result;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ForecastStand), e);
                return new StandForecast { PHold = 1.0 };
            }
        }

        /// <summary>P(1d10 ≤ x).</summary>
        private static double PRollAtMost(int x) => Math.Min(Math.Max(x, 0), 10) / 10.0;

        #endregion // Stand Forecast

        #region Defender Fate

        /// <summary>Convenience overload: forecasts the forward lane first, then runs the fate tree.</summary>
        public static DefenderFateForecast ForecastDefenderFate(
            in LaneInput forwardLane, in StandValueInput defenderStand, in DefenderFateContext ctx)
        {
            LaneForecast lane = ForecastLane(forwardLane);
            return ForecastDefenderFate(lane.Damage, defenderStand, ctx);
        }

        /// <summary>
        /// The full defender-fate tree, exactly mirroring RetreatResolver.ResolveDisplacement (§7.9.5/.6/.6a/.7):
        /// damage ≥ HP destroys outright; otherwise the stand check splits; retreat/rout with a path vacates
        /// (Static units roll catastrophic collapse first); with no path the Surrender Check destroys or holds
        /// in place at −SURRENDER_SURVIVAL_LOSS (which can itself destroy); shatter takes SHATTER_EXTRA_DAMAGE
        /// first, then quits the field with a path (collapse-checked if Static) or surrender-checks without one.
        /// </summary>
        public static DefenderFateForecast ForecastDefenderFate(
            Pmf damage, in StandValueInput defenderStand, in DefenderFateContext ctx)
        {
            try
            {
                if (damage == null) throw new ArgumentNullException(nameof(damage));

                double pDestroyed = 0, pVacates = 0, pQuits = 0, pStays = 0;

                double surrenderDestroy =
                    Math.Min(Math.Max(SurrenderCheck.SurrenderCheckNumber(defenderStand.Experience), 0), 20) / 20.0;
                double collapse = ctx.DefenderIsStatic
                    ? Math.Min(Math.Max(SurrenderCheck.StaticCollapseThreshold(defenderStand.Experience), 0), 100) / 100.0
                    : 0.0;

                foreach (KeyValuePair<int, double> kv in damage.Outcomes)
                {
                    int d = kv.Key;
                    double p = kv.Value;

                    // Killed by the fire itself — no stand check.
                    if (d >= ctx.DefenderCurrentHP)
                    {
                        pDestroyed += p;
                        pVacates += p;
                        continue;
                    }

                    StandValueInput svInput = defenderStand;
                    svInput.HpDealtThisAttack = d;
                    int sv = StandCheck.ComputeStandValue(svInput);

                    double pHold    = PRollAtMost(sv);
                    double pRetreat = PRollAtMost(sv + GameData.STAND_RETREAT_GAP) - pHold;
                    double pRout    = PRollAtMost(sv + GameData.STAND_ROUT_GAP) - PRollAtMost(sv + GameData.STAND_RETREAT_GAP);
                    double pShatter = 1.0 - PRollAtMost(sv + GameData.STAND_ROUT_GAP);

                    pStays += p * pHold;

                    // Retreat + Rout share the displacement tree (§6.8 / §7.9.6a / §7.9.7).
                    double displaced = p * (pRetreat + pRout);
                    if (displaced > 0)
                    {
                        if (ctx.RetreatPathExists)
                        {
                            pDestroyed += displaced * collapse;   // §7.9.7 — destroyed in place, hex still vacated
                            pVacates   += displaced;              // collapse or clean retreat: the hex empties
                        }
                        else
                        {
                            double destroyed = displaced * surrenderDestroy;
                            pDestroyed += destroyed;
                            pVacates   += destroyed;

                            double held = displaced - destroyed;  // passed the check: forced Deployed, −survival loss
                            if (ctx.DefenderCurrentHP - d <= GameData.SURRENDER_SURVIVAL_LOSS)
                            {
                                pDestroyed += held;
                                pVacates   += held;
                            }
                            else
                            {
                                pStays += held;                   // holds the hex — no Automatic Advance (§7.9.6a.3)
                            }
                        }
                    }

                    // Shatter (§7.9.6): extra damage first, then quit-field / surrender.
                    double shattered = p * pShatter;
                    if (shattered > 0)
                    {
                        if (ctx.DefenderCurrentHP - d <= GameData.SHATTER_EXTRA_DAMAGE)
                        {
                            pDestroyed += shattered;              // §7.9.6.2 — the extra damage kills
                            pVacates   += shattered;
                        }
                        else if (!ctx.RetreatPathExists)
                        {
                            double destroyed = shattered * surrenderDestroy;
                            pDestroyed += destroyed;
                            pVacates   += destroyed;

                            double held = shattered - destroyed;
                            if (ctx.DefenderCurrentHP - d - GameData.SHATTER_EXTRA_DAMAGE <= GameData.SURRENDER_SURVIVAL_LOSS)
                            {
                                pDestroyed += held;
                                pVacates   += held;
                            }
                            else
                            {
                                pStays += held;
                            }
                        }
                        else
                        {
                            pDestroyed += shattered * collapse;           // §7.9.6.6 — collapse while quitting
                            pVacates   += shattered;                      // either way the field is quit
                            pQuits     += shattered * (1.0 - collapse);   // survives as WITHDRAWN (§7.9.6.4/.5)
                        }
                    }
                }

                return new DefenderFateForecast
                {
                    Damage = damage,
                    ExpectedDamage = damage.ExpectedValue,
                    PDestroyed = pDestroyed,
                    PVacatesHex = pVacates,
                    PQuitsField = pQuits,
                    PStaysInHex = pStays,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ForecastDefenderFate), e);
                return new DefenderFateForecast { Damage = Pmf.Constant(0), PStaysInHex = 1.0 };
            }
        }

        #endregion // Defender Fate

        #region Engagement Forecast

        /// <summary>
        /// Full direct-engagement forecast (§7.7.3): the defender's fate under the forward lane plus the exact
        /// return-fire distribution and the attacker's destruction odds. Lanes are independent (both fire on
        /// pre-damage stats, §7.4.2.2), so the joint picture is the product of the two forecasts.
        /// </summary>
        public static EngagementForecast ForecastDirectEngagement(
            in LaneInput forwardLane, in LaneInput returnLane, in StandValueInput defenderStand,
            in DefenderFateContext ctx, float attackerCurrentHP)
        {
            try
            {
                DefenderFateForecast fate = ForecastDefenderFate(forwardLane, defenderStand, ctx);
                LaneForecast ret = ForecastLane(returnLane);
                return new EngagementForecast
                {
                    DefenderFate = fate,
                    DamageToAttacker = ret.Damage,
                    // Kill iff damage ≥ current HP; for integer damage that is d ≥ ceil(HP).
                    PAttackerDestroyed = ret.Damage.ProbAtLeast((int)Math.Ceiling(attackerCurrentHP)),
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ForecastDirectEngagement), e);
                return new EngagementForecast
                {
                    DefenderFate = new DefenderFateForecast { Damage = Pmf.Constant(0), PStaysInHex = 1.0 },
                    DamageToAttacker = Pmf.Constant(0),
                };
            }
        }

        #endregion // Engagement Forecast

        #region Degradation Odds (§7.15 — thin probability views over DegradationCheck's tables)

        /// <summary>P(a combat resolution drops this side's Efficiency tier) (§7.15.3).</summary>
        public static double CombatEfficiencyLossChance(ExperienceLevel exp) =>
            DegradationCheck.CombatEfficiencyThreshold(exp) / 100.0;

        /// <summary>P(a hex of movement drops an Efficiency tier) (§7.15.2).</summary>
        public static double MoveEfficiencyLossChance(ExperienceLevel exp) =>
            DegradationCheck.MoveEfficiencyThreshold(exp) / 100.0;

        /// <summary>P(a combat resolution burns 1 DaysSupply) (§7.15.5).</summary>
        public static double CombatSupplyLossChance(ExperienceLevel exp) =>
            DegradationCheck.CombatSupplyThreshold(exp) / 100.0;

        /// <summary>P(a hex of movement burns 1 DaysSupply) (§7.15.4).</summary>
        public static double MoveSupplyLossChance(ExperienceLevel exp) =>
            DegradationCheck.MoveSupplyThreshold(exp) / 100.0;

        /// <summary>P(a counter-battery shot burns 1 DaysSupply) — flat, no experience term (§7.15.6).</summary>
        public static double CounterBatterySupplyLossChance() =>
            DegradationCheck.COUNTER_BATTERY_SUPPLY_CHANCE / 100.0;

        #endregion // Degradation Odds
    }
}
