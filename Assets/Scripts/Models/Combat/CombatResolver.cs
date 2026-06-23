using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Context the map/turn layer supplies for a direct attack — the bits the engine can't read off the units
    /// themselves (the defender's hex terrain, whether the vector is a flank or a contested river crossing, and
    /// the defender's leader stand bonus). Defaults are the neutral case (Clear, no flank, no crossing, no leader).
    /// </summary>
    public struct DirectAttackContext
    {
        /// <summary>Terrain of the defender's hex — the flat-HP block on the forward lane (§7.5.6).</summary>
        public TerrainType DefenderTerrain;

        /// <summary>True if the direct-fire vector crosses a river edge — adds the Contested Crossing Block (§7.5.6.9).</summary>
        public bool ContestedCrossing;

        /// <summary>Defender's skill-tier Leader_mod for the stand check (§14.13), capped +3. Supply 0 until
        /// Leader.StandValueContribution is implemented (the leader-skill pass).</summary>
        public int DefenderLeaderStandMod;
    }

    /// <summary>
    /// Outcome of a resolved direct attack. HP is already applied to both units; the stand outcome and
    /// destruction flags are handed back for the map/turn layer to act on (displacement, Automatic Advance,
    /// removal, prestige, degradation rolls).
    /// </summary>
    public struct DirectAttackResult
    {
        public int DamageToDefender;
        public int DamageToAttacker;
        public int DefenderStandValue;
        public StandOutcome DefenderOutcome;
        public bool DefenderDestroyed;
        public bool AttackerDestroyed;
    }

    /// <summary>
    /// Outcome of a ground ambush (§6.9). One-way: the ambusher hits the mover, the mover does NOT return fire
    /// (§6.9.5). HP is applied; the mover's stand outcome is handed back for the caller to displace (via
    /// RetreatResolver, with the ambusher as the bearing reference) and to end the mover's turn (§6.9.6).
    /// </summary>
    public struct AmbushResult
    {
        public int DamageToMover;
        public int MoverStandValue;
        public StandOutcome MoverOutcome;
        public bool MoverDestroyed;
    }

    /// <summary>
    /// Context for an indirect attack: the terrain of both hexes (the target hex blocks the forward shot; the
    /// firer hex blocks any counter-battery return) and the target's leader stand bonus.
    /// </summary>
    public struct IndirectAttackContext
    {
        public TerrainType TargetTerrain;
        public TerrainType FirerTerrain;
        public int TargetLeaderStandMod;
    }

    /// <summary>
    /// Outcome of an indirect attack (§7.13). HP is applied to both units. Counter-battery (§7.13.5) fires only
    /// if the target is an artillery class whose IndirectRange reaches the firer; the firer takes that damage but
    /// rolls NO stand check (§7.13.5.8). The target rolls the standard stand check.
    /// </summary>
    public struct IndirectAttackResult
    {
        public int DamageToTarget;
        public int DamageToFirer;          // counter-battery (0 if none)
        public bool CounterBatteryFired;
        public int TargetStandValue;
        public StandOutcome TargetOutcome;
        public bool TargetDestroyed;
        public bool FirerDestroyed;
        public bool FirerRevealed;         // §7.13.5.4 — firing always exposes the battery
    }

    /// <summary>
    /// Context for a fixed-wing air-to-ground strike (§11.6): the target hex terrain (the flat-HP block applies to
    /// air-to-ground per §7.5.6.3) and whether a Wild Weasel is still alive at the ground-attack phase (§11.4.8.6 /
    /// §11.1.2.4 — its SEAD effect shifts the strike's damage band up one). No contested-crossing block (§7.5.6.9.3).
    /// </summary>
    public struct AirStrikeContext
    {
        public TerrainType TargetTerrain;
        public bool WildWeaselAlive;
    }

    /// <summary>
    /// Outcome of an air-to-ground strike. HP is applied to the target. Airstrikes are DAMAGE-ONLY (Bob, 2026-06-22):
    /// no stand check, no forced movement — the target's "suppression" is carried by the §7.15 efficiency
    /// degradation the caller rolls, NOT by a retreat/rout. <see cref="GadIgnored"/> flags the
    /// STANDOFF_CRUISE_MISSILE GAD-ignore branch (§11.6.1.1).
    /// </summary>
    public struct AirStrikeResult
    {
        public int DamageToTarget;
        public bool GadIgnored;
        public bool TargetDestroyed;
    }

    /// <summary>
    /// Adapter between <see cref="CombatUnit"/> and the pure damage engine. Builds the two damage lanes and the
    /// defender stand input from unit state + context, runs <see cref="CombatEngine.ResolveDirectEngagement"/>
    /// (universal return fire §6.12 / §7.7.3), and applies the resulting HP. It does NOT do the map-coupled
    /// follow-ups — displacement/Automatic Advance (§7.9.5/.9), efficiency &amp; supply degradation (§7.15, via
    /// DegradationCheck), or action-economy costs (§8.2.1, via CombatUnit.PerformCombatAction) — those are the
    /// caller's, run off the returned <see cref="DirectAttackResult"/>.
    /// </summary>
    public static class CombatResolver
    {
        private const string CLASS_NAME = nameof(CombatResolver);

        /// <summary>
        /// Resolves a direct ground attack between two units, applies HP to both, and returns the outcome.
        /// Dice are consumed in the engine's order: forward lane, return lane, then the defender's 1d10 stand roll.
        /// </summary>
        public static DirectAttackResult ResolveDirectAttack(
            CombatUnit attacker, CombatUnit defender, in DirectAttackContext ctx, ICombatRandom rng)
        {
            try
            {
                if (attacker == null) throw new ArgumentNullException(nameof(attacker));
                if (defender == null) throw new ArgumentNullException(nameof(defender));
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                bool flank = ComputeFlank(attacker, defender);
                LaneInput forward = BuildForwardLane(attacker, defender, ctx, flank);
                LaneInput returnLane = BuildReturnLane(attacker, defender);
                StandValueInput defenderStand = BuildDefenderStand(attacker, defender, ctx, flank);

                DirectEngagementResult eng =
                    CombatEngine.ResolveDirectEngagement(forward, returnLane, defenderStand, rng);

                defender.TakeDamage(eng.DamageToDefender);
                attacker.TakeDamage(eng.DamageToAttacker);

                return new DirectAttackResult
                {
                    DamageToDefender = eng.DamageToDefender,
                    DamageToAttacker = eng.DamageToAttacker,
                    DefenderStandValue = eng.DefenderStandValue,
                    DefenderOutcome = eng.DefenderOutcome,
                    DefenderDestroyed = defender.HitPoints.Current <= 0f,
                    AttackerDestroyed = attacker.HitPoints.Current <= 0f,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveDirectAttack), e);
                return default;
            }
        }

        #region Lane / stand builders (public for testing the adapter contract)

        /// <summary>
        /// Forward lane (attacker → defender). Axis is the DEFENDER's class (§7.4.1); attacker gets NO deployment
        /// multiplier (§7.5.2); the defender's hex terrain blocks; an embarked defender takes the +1-band ×2.0
        /// embarkment malus (§7.10.1); a flank attack adds ×FLANK_DAMAGE_MULT (§7.5.5.9); a BM attacker bypasses
        /// terrain (§7.5.6.6). The embarkment and flank scalars multiply.
        /// </summary>
        public static LaneInput BuildForwardLane(CombatUnit attacker, CombatUnit defender, in DirectAttackContext ctx, bool flank)
        {
            TargetClass axis = defender.ActiveTargetClass;
            bool defenderEmbarked = defender.DeploymentPosition == DeploymentPosition.Embarked;
            float postScalar = (defenderEmbarked ? 2.0f : 1.0f) * (flank ? GameData.FLANK_DAMAGE_MULT : 1.0f);
            return new LaneInput
            {
                FirerAttack = attacker.GetAttackStatVsClass(axis),
                TargetDefense = defender.GetDefenseStatVsClass(axis),
                FirerQualityMult = attacker.GetCombatQualityMultiplier(),
                FirerIsDefender = false,
                AttackType = AttackType.Direct,
                FirerIsAir = attacker.IsFixedWingAirUnit,
                TargetTerrain = ctx.DefenderTerrain,
                BypassTerrainBlock = attacker.Classification == UnitClassification.BM,
                ContestedCrossing = ctx.ContestedCrossing,
                BandShift = defenderEmbarked ? 1 : 0,
                PostStackScalar = postScalar,
            };
        }

        /// <summary>
        /// Return lane (defender → attacker, universal return fire §6.12). Axis is the ATTACKER's class; the
        /// defender's deployment COMBAT_MOD applies (§7.5.2); the attacker's hex terrain is inert on return fire
        /// (§7.5.6.5), so the terrain block is bypassed.
        /// </summary>
        public static LaneInput BuildReturnLane(CombatUnit attacker, CombatUnit defender)
        {
            TargetClass axis = attacker.ActiveTargetClass;
            return new LaneInput
            {
                FirerAttack = defender.GetAttackStatVsClass(axis),
                TargetDefense = attacker.GetDefenseStatVsClass(axis),
                FirerQualityMult = defender.GetCombatQualityMultiplier(),
                FirerDeploymentMod = defender.GetDeploymentCombatMod(),
                FirerIsDefender = true,
                AttackType = AttackType.Direct,
                FirerIsAir = defender.IsFixedWingAirUnit,
                BypassTerrainBlock = true,
            };
        }

        /// <summary>Builds the defender's stand input (§7.9.1). HpDealtThisAttack is filled by the engine from the forward lane.</summary>
        public static StandValueInput BuildDefenderStand(CombatUnit attacker, CombatUnit defender, in DirectAttackContext ctx, bool flank)
        {
            return new StandValueInput
            {
                Deployment = defender.DeploymentPosition,
                Terrain = ctx.DefenderTerrain,
                Experience = defender.ExperienceLevel,
                LeaderMod = ctx.DefenderLeaderStandMod,
                DefenderCommand = CommandValue(defender),
                AttackerCommand = CommandValue(attacker),
                FlankAttack = flank,
            };
        }

        /// <summary>The unit's leader CommandAbility as an SV term value 0..3 (§7.9.4a/b); 0 if unled.</summary>
        private static int CommandValue(CombatUnit unit)
        {
            var leader = unit.GetAssignedLeader();
            return leader != null ? (int)leader.CombatCommand : 0;
        }

        /// <summary>
        /// Detects whether the attack lands in the defender's flank arc (§5.8.7), from the units' positions and
        /// the defender's facing. False if the attacker is not an adjacent neighbour (e.g. indirect fire).
        /// </summary>
        public static bool ComputeFlank(CombatUnit attacker, CombatUnit defender)
        {
            HexDirection? bearing = HexMapUtil.GetDirectionBetween(defender.MapPos, attacker.MapPos);
            return bearing.HasValue && HexArc.IsFlankAttack(defender.Facing, bearing.Value);
        }

        #endregion // Lane / stand builders

        #region Ambush (§6.9)

        /// <summary>
        /// Resolves a ground ambush (§6.9): the ambusher delivers one full attack against the moving unit at
        /// ×AMBUSH_BONUS_MULT (§6.9.4), the mover takes NO return fire (§6.9.5), and the mover rolls a stand
        /// check. The mover is attacked as if on clear terrain (§7.5.6.7). The caller (MovementController)
        /// displaces the mover via RetreatResolver using the ambusher as the bearing reference, reveals the
        /// ambusher to Level 1 (§6.9.3), and ends the mover's turn (§6.9.6) — none of which is done here.
        /// Dice order: the ambush damage lane, then the mover's 1d10 stand roll.
        /// </summary>
        public static AmbushResult ResolveAmbush(CombatUnit ambusher, CombatUnit mover, in DirectAttackContext ctx, ICombatRandom rng)
        {
            try
            {
                if (ambusher == null) throw new ArgumentNullException(nameof(ambusher));
                if (mover == null) throw new ArgumentNullException(nameof(mover));
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                LaneInput lane = BuildAmbushLane(ambusher, mover);
                int dmg = CombatEngine.ResolveLane(lane, rng);
                mover.TakeDamage(dmg);

                StandValueInput stand = BuildAmbushStand(ambusher, mover, ctx, dmg);
                int sv = StandCheck.ComputeStandValue(stand);
                StandOutcome outcome = StandCheck.ResolveStand(sv, rng);

                return new AmbushResult
                {
                    DamageToMover = dmg,
                    MoverStandValue = sv,
                    MoverOutcome = outcome,
                    MoverDestroyed = mover.HitPoints.Current <= 0f,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveAmbush), e);
                return default;
            }
        }

        /// <summary>
        /// The ambush damage lane (§6.9.4). Axis by the mover's class; ambusher quality; ×AMBUSH_BONUS_MULT;
        /// terrain bypassed (ambushed = clear, §7.5.6.7); an embarked mover additionally takes the embarkment
        /// malus (+1 band ×2, §9.10.7.2 / §7.10.1). The ambusher DOES get its deployment COMBAT_MOD ("its current
        /// Deployment" in §6.9.4): an ambush is a prepared strike from a defensive position, so a dug-in ambusher
        /// hits harder — the punishment for outrunning recon into a fortified hex. (Modelled by FirerIsDefender,
        /// the lane's deployment-applies flag; tunable via AMBUSH_BONUS_MULT if it proves too strong.)
        /// </summary>
        public static LaneInput BuildAmbushLane(CombatUnit ambusher, CombatUnit mover)
        {
            TargetClass axis = mover.ActiveTargetClass;
            bool moverEmbarked = mover.DeploymentPosition == DeploymentPosition.Embarked;
            return new LaneInput
            {
                FirerAttack = ambusher.GetAttackStatVsClass(axis),
                TargetDefense = mover.GetDefenseStatVsClass(axis),
                FirerQualityMult = ambusher.GetCombatQualityMultiplier(),
                FirerDeploymentMod = ambusher.GetDeploymentCombatMod(),
                FirerIsDefender = true,   // ambush hits from a defensive posture → deployment COMBAT_MOD applies
                AttackType = AttackType.Direct,
                FirerIsAir = ambusher.IsFixedWingAirUnit,
                BypassTerrainBlock = true,
                BandShift = moverEmbarked ? 1 : 0,
                PostStackScalar = GameData.AMBUSH_BONUS_MULT * (moverEmbarked ? 2.0f : 1.0f),
            };
        }

        /// <summary>The mover's stand input for an ambush — flank is never checked (§7.5.5.9); the ambusher's command still bites (§7.9.4a).</summary>
        public static StandValueInput BuildAmbushStand(CombatUnit ambusher, CombatUnit mover, in DirectAttackContext ctx, int dmg)
        {
            return new StandValueInput
            {
                Deployment = mover.DeploymentPosition,
                Terrain = ctx.DefenderTerrain,
                Experience = mover.ExperienceLevel,
                LeaderMod = ctx.DefenderLeaderStandMod,
                DefenderCommand = CommandValue(mover),
                AttackerCommand = CommandValue(ambusher),
                FlankAttack = false,
                HpDealtThisAttack = dmg,
            };
        }

        #endregion // Ambush

        #region Indirect fire + counter-battery (§7.13)

        /// <summary>
        /// Resolves an indirect attack (§7.13): a one-way forward shot at the target, an optional simultaneous
        /// counter-battery return, then the target's stand check. Both lanes read pre-damage stats; damage is
        /// applied after both resolve. The firer takes no stand check from counter-battery (§7.13.5.8). Range and
        /// spotting gates are the caller's (see <see cref="IsInIndirectRange"/>); auto-reveal is reported, not applied.
        /// Dice order: forward lane, counter-battery lane (if any), target 1d10 stand roll.
        /// </summary>
        public static IndirectAttackResult ResolveIndirectAttack(
            CombatUnit firer, CombatUnit target, in IndirectAttackContext ctx, ICombatRandom rng)
        {
            try
            {
                if (firer == null) throw new ArgumentNullException(nameof(firer));
                if (target == null) throw new ArgumentNullException(nameof(target));
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                int toTarget = CombatEngine.ResolveLane(BuildIndirectForwardLane(firer, target, ctx), rng);

                bool cb = IsCounterBatteryEligible(firer, target);
                int toFirer = cb ? CombatEngine.ResolveLane(BuildCounterBatteryLane(firer, target, ctx), rng) : 0;

                // Simultaneous (§7.13.5.5) — apply after both lanes computed on pre-damage stats.
                target.TakeDamage(toTarget);
                if (cb) firer.TakeDamage(toFirer);

                StandValueInput stand = BuildIndirectTargetStand(firer, target, ctx, toTarget);
                int sv = StandCheck.ComputeStandValue(stand);
                StandOutcome outcome = StandCheck.ResolveStand(sv, rng);

                return new IndirectAttackResult
                {
                    DamageToTarget = toTarget,
                    DamageToFirer = toFirer,
                    CounterBatteryFired = cb,
                    TargetStandValue = sv,
                    TargetOutcome = outcome,
                    TargetDestroyed = target.HitPoints.Current <= 0f,
                    FirerDestroyed = cb && firer.HitPoints.Current <= 0f,
                    FirerRevealed = true,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveIndirectAttack), e);
                return default;
            }
        }

        /// <summary>
        /// Forward indirect lane (firer → target, §7.7.4): axis by target class; firer attacking so NO deployment
        /// mult; terrain blocks the target hex; a BM attacker (Scud) bypasses terrain (§7.5.6.6). Arc-agnostic —
        /// no flank, no contested crossing (§7.5.6.9.3).
        /// </summary>
        public static LaneInput BuildIndirectForwardLane(CombatUnit firer, CombatUnit target, in IndirectAttackContext ctx)
        {
            TargetClass axis = target.ActiveTargetClass;
            return new LaneInput
            {
                FirerAttack = firer.GetAttackStatVsClass(axis),
                TargetDefense = target.GetDefenseStatVsClass(axis),
                FirerQualityMult = firer.GetCombatQualityMultiplier(),
                FirerIsDefender = false,
                AttackType = AttackType.Indirect,
                TargetTerrain = ctx.TargetTerrain,
                BypassTerrainBlock = firer.Classification == UnitClassification.BM,
            };
        }

        /// <summary>
        /// Counter-battery lane (target → firer, §7.13.5): the targeted artillery returns fire. Axis by the
        /// firer's class; it IS a return so the target's deployment applies; terrain blocks the FIRER's hex;
        /// full effect (§7.13.5.6 — no scaling).
        /// </summary>
        public static LaneInput BuildCounterBatteryLane(CombatUnit firer, CombatUnit target, in IndirectAttackContext ctx)
        {
            TargetClass axis = firer.ActiveTargetClass;
            return new LaneInput
            {
                FirerAttack = target.GetAttackStatVsClass(axis),
                TargetDefense = firer.GetDefenseStatVsClass(axis),
                FirerQualityMult = target.GetCombatQualityMultiplier(),
                FirerDeploymentMod = target.GetDeploymentCombatMod(),
                FirerIsDefender = true,
                AttackType = AttackType.Indirect,
                TargetTerrain = ctx.FirerTerrain,
                BypassTerrainBlock = target.Classification == UnitClassification.BM,
            };
        }

        /// <summary>Target stand input for indirect fire (§7.9): arc-agnostic (no flank, §7.9.4c); the firer's command still applies (§7.9.4a).</summary>
        public static StandValueInput BuildIndirectTargetStand(CombatUnit firer, CombatUnit target, in IndirectAttackContext ctx, int dmg)
        {
            return new StandValueInput
            {
                Deployment = target.DeploymentPosition,
                Terrain = ctx.TargetTerrain,
                Experience = target.ExperienceLevel,
                LeaderMod = ctx.TargetLeaderStandMod,
                DefenderCommand = CommandValue(target),
                AttackerCommand = CommandValue(firer),
                FlankAttack = false,
                HpDealtThisAttack = dmg,
            };
        }

        /// <summary>
        /// Counter-battery eligibility (§7.13.5.1/.2): the target is an artillery class (ART/SPA/ROC/BM) with
        /// IndirectRange &gt; 0 that reaches the firer's hex. A deep-rear Scud sits outside normal CB range (§7A.11).
        /// </summary>
        public static bool IsCounterBatteryEligible(CombatUnit firer, CombatUnit target)
        {
            if (!IsArtillery(target.Classification)) return false;
            float ir = target.ActiveIndirectRange;
            if (ir <= 0f) return false;
            return HexMapUtil.GetHexDistance(target.MapPos, firer.MapPos) <= ir;
        }

        /// <summary>True if <paramref name="firer"/> can range an indirect shot onto <paramref name="targetHex"/> — distance in [1, IR] (§7.13.1).</summary>
        public static bool IsInIndirectRange(CombatUnit firer, Position2D targetHex)
        {
            float ir = firer.ActiveIndirectRange;
            if (ir <= 0f) return false;
            int dist = HexMapUtil.GetHexDistance(firer.MapPos, targetHex);
            return dist >= 1 && dist <= ir;
        }

        private static bool IsArtillery(UnitClassification c) =>
            c == UnitClassification.ART || c == UnitClassification.SPA ||
            c == UnitClassification.ROC || c == UnitClassification.BM;

        #endregion // Indirect fire + counter-battery

        #region Air-to-ground strike (§11.6)

        /// <summary>
        /// Resolves a fixed-wing air-to-ground strike (§11.6): one forward GA-vs-GAD shot through the §7.7.1 engine
        /// in its Airstrike configuration (OL/9 §11.6.1.2, target-hex terrain block, AirBalanceMod, fixed-wing skip
        /// deployment §10.3c.1). A STANDOFF_CRUISE_MISSILE strike ignores target GAD (§11.6.1.1); a surviving Wild
        /// Weasel shifts the damage band up one (§11.4.8.6). HP is applied to the target.
        ///
        /// DAMAGE-ONLY (Bob, 2026-06-22): an airstrike does NOT run a stand check and never forces movement — the
        /// firing aircraft never advances (§7.9.9.3) and the target is not displaced. Post-strike "suppression" is
        /// the §7.15 efficiency-degradation roll the caller applies, not a retreat/rout.
        ///
        /// SCOPE: fixed-wing strike aircraft only — helicopters hit ground as ground units with NO OL (§11.6.1.5 /
        /// §7A.14). The CALLER (air pipeline) supplies the surviving-strike list, in-hex ground fire (§11.4.8.5),
        /// egress fire (§11.4.8.7), and the efficiency-degradation roll. Dice: the strike damage lane only.
        /// </summary>
        public static AirStrikeResult ResolveAirStrike(CombatUnit strike, CombatUnit target, in AirStrikeContext ctx, ICombatRandom rng)
        {
            try
            {
                if (strike == null) throw new ArgumentNullException(nameof(strike));
                if (target == null) throw new ArgumentNullException(nameof(target));
                if (rng == null) throw new ArgumentNullException(nameof(rng));

                LaneInput lane = BuildAirStrikeLane(strike, target, ctx);
                int dmg = CombatEngine.ResolveLane(lane, rng);
                target.TakeDamage(dmg);

                return new AirStrikeResult
                {
                    DamageToTarget = dmg,
                    GadIgnored = strike.IgnoresAirDefense,
                    TargetDestroyed = target.HitPoints.Current <= 0f,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveAirStrike), e);
                return default;
            }
        }

        /// <summary>
        /// The air-strike forward lane (§11.6.1): Δ = effGA − target GAD, where effGA carries the Rule-B
        /// target-class/base riders (§11.6.1.1) and GAD is the single defense stat (never split). A
        /// STANDOFF_CRUISE_MISSILE strike zeroes the GAD term. Airstrike config applies OL/9; the target hex
        /// terrain blocks; FirerIsDefender stays false so fixed-wing skip the deployment mult. Band shifts stack:
        /// a surviving WW (§11.4.8.6) and an EMBARKED target caught in transit (§7.10.1.1) each add +1; an embarked
        /// target also takes the ×2 embarkment scalar (§7.10.1.2). An embarked air-mobile unit is a ground unit
        /// caught airborne — a fixed-wing strike can catch it if the lift was too far to debark (Bob, 2026-06-22).
        /// </summary>
        public static LaneInput BuildAirStrikeLane(CombatUnit strike, CombatUnit target, in AirStrikeContext ctx)
        {
            bool ignoreGad = strike.IgnoresAirDefense;
            bool targetEmbarked = target.DeploymentPosition == DeploymentPosition.Embarked;
            return new LaneInput
            {
                FirerAttack = strike.GetEffectiveGroundAttack(target.ActiveTargetClass, target.IsBase),
                TargetDefense = ignoreGad ? 0 : target.ActiveGroundAirDefense,
                FirerQualityMult = strike.GetCombatQualityMultiplier(),
                OrdnanceLoad = strike.ActiveOrdnanceLoad,
                AttackType = AttackType.Airstrike,
                FirerIsAir = true,
                TargetTerrain = ctx.TargetTerrain,
                BandShift = (ctx.WildWeaselAlive ? 1 : 0) + (targetEmbarked ? 1 : 0),
                PostStackScalar = targetEmbarked ? 2.0f : 1.0f,
            };
        }

        #endregion // Air-to-ground strike
    }
}
