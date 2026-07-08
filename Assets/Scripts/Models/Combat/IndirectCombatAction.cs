using System;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Outcome of an indirect-fire ACTION (the full §7.13 fire mission, not just the math). Mirrors
    /// <see cref="GroundCombatOutcome"/> for the direct path. Counter-battery (§7.13.5) is the only way the
    /// firer takes damage; the firer never rolls a stand check (§7.13.5.8). No Automatic Advance — §7.9.9 is
    /// a direct-engagement mechanic. Prestige on a kill is REPORTED, not credited (no §18 economy yet).
    /// </summary>
    public struct IndirectCombatOutcome
    {
        public bool Executed;                   // false → rejected before any dice/costs (see Reason)
        public string Reason;                   // rejection reason (null/empty on success)

        public int DamageToTarget;
        public int DamageToFirer;               // counter-battery return (0 if none fired)
        public bool CounterBatteryFired;
        public StandOutcome TargetOutcome;

        public bool TargetMoved;                // retreated/routed to a new hex
        public Position2D TargetFinalPosition;
        public int TargetHexesRetreated;        // 0 / 1 / 2
        public bool TargetRemovedFromMap;       // shatter-quit OR destruction → unregistered
        public bool TargetDestroyed;            // permanent loss (vs a shatter-quit survival, §7.9.6.5)
        public bool FirerDestroyed;             // killed by counter-battery

        public int PrestigeOwedToFirer;         // §18.2.3 half purchase cost on a KILL; reported only
    }

    /// <summary>
    /// Orchestrates a single INDIRECT fire mission end-to-end (§7.13) — the model-layer caller above the pure
    /// resolvers, mirroring <see cref="GroundCombatAction"/> for the direct path. ROUTING RULE (ratified
    /// 2026-07-06): an indirect-fire-class unit (ART/SPA/ROC/BM — <see cref="CombatResolver.IsIndirectFireClass"/>)
    /// ALWAYS attacks through this pipeline, even against an ADJACENT target (§7.13.1 range is [1, IR]); it never
    /// fights a direct engagement. Sequence: validate → spend the action economy (§8.2.1) → forward shot +
    /// simultaneous counter-battery + target stand check (CombatResolver.ResolveIndirectAttack) → CB economy on
    /// the target (1 OpportunityAction + flat-50% supply, §7.13.5.7/§7.15.6) → AI-firer auto-reveal (§7.13.5.4;
    /// a PLAYER firer's reveal is a v1 no-op — fog is one-directional, SpottedLevel lives on AI units) →
    /// displacement (§7.9, non-adjacent bearing) → efficiency degradation (§7.15.3; supply loss only for units
    /// that FIRED — the firer rolls §7.15.5, a non-CB target loses nothing) → leader reputation (§14.5).
    /// Dice order (seeded-test contract): forward lane → CB lane (if any) → target stand 1d10 → CB supply 1d100
    /// (if CB) → displacement dice → firer EL 1d100 → firer supply 1d100 → target EL 1d100.
    /// </summary>
    public static class IndirectCombatAction
    {
        private const string CLASS_NAME = nameof(IndirectCombatAction);

        /// <summary>
        /// Eligibility gate for an indirect fire mission. Returns null if legal, else the rejection reason.
        /// PUBLIC because the input layer's cursor feedback (§24.11.3) must run the SAME check the click runs —
        /// the cursor never lies. No dice, no costs.
        /// </summary>
        public static string CanExecute(CombatUnit firer, CombatUnit target, HexMap map)
        {
            if (firer == null) return "No firer.";
            if (target == null) return "No target.";
            if (map == null) return "No map.";
            if (firer.IsDestroyed()) return "Firer is destroyed.";
            if (target.IsDestroyed()) return "Target is already destroyed.";
            if (firer.Side == target.Side) return "Cannot attack a friendly unit.";
            if (firer.IsBase) return "Bases cannot initiate attacks.";
            if (target.IsBase) return "Use the base-combat path against a base (§11.7).";
            if (firer.DeploymentPosition == DeploymentPosition.Embarked) return "An embarked unit cannot fire.";

            // Fog is one-directional in v1 (SpottedLevel lives on AI units) — see GroundCombatAction.CanExecute.
            if (target.Side == Side.AI && target.SpottedLevel < SpottedLevel.Level1)
                return "Target is not spotted.";

            if (!CombatResolver.IsInIndirectRange(firer, target.MapPos))
                return "Target is out of indirect-fire range.";
            if (firer.GetCombatActions() < 1)
                return "Firer has no combat action available.";

            return null;
        }

        /// <summary>
        /// Executes an indirect fire mission by <paramref name="firer"/> against <paramref name="target"/> at
        /// range [1, IR]. Terrain for both hexes is read from the map; counter-battery is suppressed when the
        /// target has no OpportunityAction to pay for it (§7.13.5.7).
        /// </summary>
        public static IndirectCombatOutcome Execute(CombatUnit firer, CombatUnit target, HexMap map, ICombatRandom rng)
        {
            try
            {
                if (rng == null)
                    return new IndirectCombatOutcome { Executed = false, Reason = "No RNG." };
                string reason = CanExecute(firer, target, map);
                if (reason != null)
                    return new IndirectCombatOutcome { Executed = false, Reason = reason };

                // §8.2.1 — 1 CombatAction + 25% max MP; supply GATED here, rolled probabilistically below (§7.15.7.1).
                if (!firer.PerformCombatAction())
                    return new IndirectCombatOutcome { Executed = false, Reason = "Firer cannot afford the combat action." };

                firer.MarkFoughtThisTurn();
                target.MarkFoughtThisTurn();

                var ctx = new IndirectAttackContext
                {
                    TargetTerrain = TerrainAt(map, target.MapPos),
                    FirerTerrain = TerrainAt(map, firer.MapPos),
                    SuppressCounterBattery = target.GetOpportunityActions() < 1,
                };

                // (1) The shot: forward lane + simultaneous CB + target stand check. HP applied inside.
                IndirectAttackResult atk = CombatResolver.ResolveIndirectAttack(firer, target, ctx, rng);

                var outcome = new IndirectCombatOutcome
                {
                    Executed = true,
                    DamageToTarget = atk.DamageToTarget,
                    DamageToFirer = atk.DamageToFirer,
                    CounterBatteryFired = atk.CounterBatteryFired,
                    TargetOutcome = atk.TargetOutcome,
                    TargetFinalPosition = target.MapPos,
                };

                // (2) The CB shot's economy on the target: 1 OpportunityAction + flat-50% supply (§7.13.5.7 / §7.15.6).
                // Deliberately NOT PerformOpportunityAction() — that path charges the generic deterministic
                // 0.5 supply (§7.15.7.2), and counter-battery is the EXPLICIT exception (§7.12.2 / §15.2.5):
                // the flat-50% roll below REPLACES it.
                if (atk.CounterBatteryFired)
                {
                    target.OpportunityActions.DecrementCurrent();
                    if (DegradationCheck.RollCounterBatterySupplyLoss(rng))
                        target.ConsumeSupplies(1f);
                }

                // (3) Firing always exposes the battery (§7.13.5.4). Only an AI firer actually gains a level —
                // fog is one-directional in v1. SetSpottedLevel clamps (Level4 / Bunker cap §14.8.7).
                if (firer.Side == Side.AI)
                    firer.SetSpottedLevel(firer.SpottedLevel + 1);

                // (4) Firer killed by counter-battery — no stand check ever (§7.13.5.8), just removal.
                if (atk.FirerDestroyed)
                {
                    outcome.FirerDestroyed = true;
                    Unregister(firer);
                }

                // (5) Target board consequences (mirrors the direct path).
                if (atk.TargetDestroyed)
                {
                    outcome.TargetDestroyed = true;
                    outcome.TargetRemovedFromMap = true;
                    outcome.PrestigeOwedToFirer = PrestigeOnKill(target);
                    Unregister(target);
                }
                else if (atk.TargetOutcome != StandOutcome.Hold)
                {
                    DisplacementResult disp =
                        RetreatResolver.ResolveDisplacement(firer, target, atk.TargetOutcome, map, rng);

                    outcome.TargetMoved = disp.Moved;
                    outcome.TargetFinalPosition = disp.FinalPosition;
                    outcome.TargetHexesRetreated = disp.HexesRetreated;

                    bool permanent = disp.Destroyed || disp.Surrendered || disp.StaticCollapsed;
                    if (disp.RemovedFromMap || permanent)
                    {
                        outcome.TargetRemovedFromMap = true;
                        outcome.TargetDestroyed = permanent;            // a shatter-quit survives (§7.9.6.5)
                        if (permanent)
                            outcome.PrestigeOwedToFirer = PrestigeOnKill(target);
                        Unregister(target);
                    }
                }

                // (6) Degradation. EL loss both sides (§7.15.3). Supply models SHOOTING: the firer rolls §7.15.5;
                // the target's shot cost was the CB flat-50% in (2) — a shelled unit that fired nothing loses none.
                ApplyEfficiencyLoss(firer, alive: !outcome.FirerDestroyed, rng);
                if (!outcome.FirerDestroyed && DegradationCheck.RollCombatSupplyLoss(firer.ExperienceLevel, rng))
                    firer.ConsumeSupplies(1f);
                ApplyEfficiencyLoss(target, alive: !outcome.TargetRemovedFromMap, rng);

                // (7) Leader reputation (§14.5) — same triggers as the direct path.
                AwardFirerReputation(firer, target, outcome);

                return outcome;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Execute), e);
                return new IndirectCombatOutcome { Executed = false, Reason = "Internal error resolving the fire mission." };
            }
        }

        #region Helpers

        private static void ApplyEfficiencyLoss(CombatUnit unit, bool alive, ICombatRandom rng)
        {
            if (!alive || unit == null) return;
            unit.SetEfficiencyLevel(
                DegradationCheck.ApplyCombatEfficiencyLoss(unit.EfficiencyLevel, unit.ExperienceLevel, rng));
        }

        /// <summary>Mirrors GroundCombatAction.AwardAttackerReputation (§14.5): Combat always; ForcedRetreat on a
        /// displacement/withdrawal; UnitDestroyed on a permanent kill (×2 for an Elite kill); ×1.5 vet/elite firer.</summary>
        private static void AwardFirerReputation(CombatUnit firer, CombatUnit target, in IndirectCombatOutcome outcome)
        {
            try
            {
                if (outcome.FirerDestroyed) return;
                var leader = firer.GetAssignedLeader();
                if (leader == null) return;

                float unitMult = firer.ExperienceLevel >= ExperienceLevel.Veteran
                    ? GameData.REP_EXPERIENCE_MULTIPLIER : 1.0f;

                leader.AwardReputationForAction(GameData.ReputationAction.Combat, unitMult);

                if (outcome.TargetDestroyed)
                {
                    float killMult = target.ExperienceLevel == ExperienceLevel.Elite
                        ? unitMult * GameData.REP_ELITE_DIFFICULTY_BONUS : unitMult;
                    leader.AwardReputationForAction(GameData.ReputationAction.UnitDestroyed, killMult);
                }
                else if (outcome.TargetMoved || outcome.TargetRemovedFromMap)
                {
                    leader.AwardReputationForAction(GameData.ReputationAction.ForcedRetreat, unitMult);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AwardFirerReputation), e);
            }
        }

        private static TerrainType TerrainAt(HexMap map, Position2D pos) =>
            map.GetHexAt(pos)?.Terrain ?? TerrainType.Clear;

        /// <summary>§18.2.3 — half purchase cost, reported only (see GroundCombatAction.PrestigeOnKill).</summary>
        private static int PrestigeOnKill(CombatUnit killed)
        {
            int cost = killed.GetActiveWeaponProfile()?.PrestigeCost ?? 0;
            return cost / 2;
        }

        private static void Unregister(CombatUnit unit)
        {
            if (unit == null) return;
            GameDataManager.Instance?.UnregisterCombatUnit(unit.UnitID);
        }

        #endregion // Helpers
    }
}
