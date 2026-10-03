using System;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Outcome of a ground direct-attack ACTION (the full §7 engagement, not just the math). Reports everything
    /// the turn/UI layer needs: damage to both sides, the defender's stand outcome and board displacement,
    /// removals, Automatic-Advance availability, and the prestige owed on a kill. NOTE: no prestige economy
    /// exists yet (§18), so <see cref="PrestigeOwedToAttacker"/> is REPORTED for the future requisition system,
    /// not credited here. On a rejected attack (see <see cref="Executed"/> / <see cref="Reason"/>) no dice are
    /// rolled and no costs are paid.
    /// </summary>
    public struct GroundCombatOutcome
    {
        public bool MainAttackResolved;         // distinguishes a fired engagement from a committed, cancelled order
        public bool ResolutionFailed;
        public DefensiveSupportOutcome Support;
        public bool AttackerRemovedFromMap;
        public Position2D AttackerFinalPosition;
        public bool Executed;                   // initiating CombatAction paid; may be interrupted by Support
        public string Reason;                   // rejection, cancellation or failure explanation

        public int DamageToDefender;
        public int DamageToAttacker;            // universal return fire (§6.12)
        public StandOutcome DefenderOutcome;

        public bool DefenderMoved;              // retreated/routed to a new hex
        public Position2D DefenderFinalPosition;
        public int DefenderHexesRetreated;      // 0 / 1 / 2
        public bool DefenderRemovedFromMap;     // shatter-quit OR a destruction → unregistered
        public bool DefenderDestroyed;          // permanent loss (vs a shatter-quit survival, §7.9.6.5)
        public bool AttackerDestroyed;          // killed by return fire (§7.4.2.3)

        public bool AutomaticAdvanceAvailable;  // §7.9.9 — caller's optional free advance into VacatedHex
        public Position2D VacatedHex;

        public int PrestigeOwedToAttacker;      // §18.2.3 half purchase cost on a KILL (0 for shatter-quit); reported only
    }

    /// <summary>
    /// Orchestrates a single DIRECT ground attack end-to-end — the model-layer "caller" that sits above the pure
    /// resolvers (<see cref="CombatResolver"/> / <see cref="RetreatResolver"/> / <see cref="DegradationCheck"/>).
    /// It validates eligibility, spends the action economy (§8.2.1), runs the engagement (§7.7.3 universal return
    /// fire), rolls the probabilistic combat Efficiency (§7.15.3) and Supply (§7.15.5) loss for both sides,
    /// applies board displacement (§7.9 via RetreatResolver), unregisters removed/destroyed units, and reports the
    /// result. The Unity layer (MovementController / BattleManager) calls <see cref="Execute"/> on player input and
    /// reacts to the outcome (icon redraw, printer message, the optional Automatic Advance move). Indirect (§7.13),
    /// air-to-ground (§11.6), ground ambush (§6.9), and base combat (§11.7) have their own paths.
    /// </summary>
    public static class GroundCombatAction
    {
        private const string CLASS_NAME = nameof(GroundCombatAction);

        /// <summary>
        /// Executes a direct attack by <paramref name="attacker"/> against an adjacent enemy <paramref name="defender"/>.
        /// Dice come from <paramref name="rng"/> in a fixed order: optional support selection/fire/displacement/
        /// degradation, then engagement (§7.7.3) → displacement (§7.9) →
        /// degradation (attacker then defender, §7.15) — so seeded tests are stable. <paramref name="contestedCrossing"/>
        /// is supplied by the map-layer caller per §7.5.6.9.1 (river/bridge geometry); the defender's hex terrain is
        /// read from the map.
        /// </summary>
        public static GroundCombatOutcome Execute(
            CombatUnit attacker, CombatUnit defender, HexMap map, ICombatRandom rng,
            GroundCombatContext context, bool contestedCrossing = false)
        {
            var outcome = new GroundCombatOutcome
            {
                AttackerFinalPosition = attacker?.MapPos ?? Position2D.Zero,
                DefenderFinalPosition = defender?.MapPos ?? Position2D.Zero,
                VacatedHex = defender?.MapPos ?? Position2D.Zero,
            };
            try
            {
                if (rng == null)
                    return new GroundCombatOutcome { Executed = false, Reason = "No RNG." };
                string reason = CanExecute(attacker, defender, map);
                if (reason != null)
                    return new GroundCombatOutcome { Executed = false, Reason = reason };
                if (context == null || attacker.Side != context.PhasingSide ||
                    !context.IsOnMap(attacker, map) || !context.IsOnMap(defender, map))
                    return new GroundCombatOutcome { Reason = "Invalid turn or on-map combat context." };
                if (CombatResolver.IsIndirectFireClass(attacker.Classification))
                    return new GroundCombatOutcome { Reason = "Artillery initiates attacks through indirect combat." };
                if (!context.CanObserve(attacker.Side, defender, map))
                    return new GroundCombatOutcome { Reason = "Target is not spotted." };

                // §8.2.1 — spend 1 CombatAction + no MP fee. Supply is GATED here, not consumed (§7.15.7.1);
                // the probabilistic combat-supply loss is rolled below (§7.15.5).
                if (!attacker.PerformCombatAction())
                    return new GroundCombatOutcome { Executed = false, Reason = "Attacker cannot afford the combat action." };

                // Executed means the initiating order committed. A later interruption never refunds its
                // CombatAction or offensive lock, and must not be presented as a pre-payment rejection.
                outcome.Executed = true;
                rng = new CombatActionRandom(rng);

                attacker.Facing = HexMapUtil.GetGeneralDirection(attacker.MapPos, defender.MapPos);

                // The only support trigger. Neither the indirect kernel nor return fire can re-enter it.
                CombatUnit battery = DefensiveArtillerySupport.Select(attacker, defender, map, context, rng);
                if (battery != null)
                {
                    outcome.Support = new DefensiveSupportOutcome { Battery = battery, TargetHex = attacker.MapPos };
                    if (!DefensiveArtillerySupport.IsEligible(battery, attacker, defender, map, context))
                        throw new InvalidOperationException("Selected support battery is no longer eligible.");
                    outcome.Support.Combat = IndirectCombatAction.ExecuteSupport(battery, attacker, map, rng, context);
                    outcome.AttackerFinalPosition = attacker.MapPos;
                    outcome.AttackerDestroyed = outcome.Support.Combat.TargetDestroyed;
                    outcome.AttackerRemovedFromMap = outcome.Support.Combat.TargetRemovedFromMap;
                    if (!outcome.Support.Combat.Executed)
                        throw new InvalidOperationException("The support mission did not complete; the main attack was stopped.");
                    if (outcome.AttackerDestroyed || outcome.AttackerRemovedFromMap ||
                        outcome.Support.Combat.TargetOutcome != StandOutcome.Hold || !context.IsOnMap(attacker, map) ||
                        !context.IsOnMap(defender, map) || !HexMapUtil.GetDirectionBetween(attacker.MapPos, defender.MapPos).HasValue)
                    {
                        outcome.Reason = "Defensive artillery support stopped the attack.";
                        return outcome;
                    }
                }

                attacker.MarkFoughtThisTurn();
                defender.MarkFoughtThisTurn();

                var ctx = new DirectAttackContext
                {
                    DefenderTerrain = TerrainAt(map, defender.MapPos),
                    ContestedCrossing = contestedCrossing,
                    // §14.13 Leader_mod is read off the defender's leader inside the resolver.
                };

                // (1) Engagement — applies HP to both units, returns the defender's stand outcome (§7.7.3).
                DirectAttackResult atk = CombatResolver.ResolveDirectAttack(attacker, defender, ctx, rng);
                CombatActionRandom.ThrowIfFailed(rng);
                if (!atk.Resolved) throw new InvalidOperationException("Direct combat did not resolve.");
                outcome.MainAttackResolved = true;

                // (1a) Direct-combat intel (§12.4.6): closing to contact reveals what the other side is
                // fighting with — both participants are set to Level 4. Applied AFTER the engagement so a
                // unit destroyed in the exchange is not left visible by a reveal it did not survive; the
                // removal path below unregisters it either way.
                SpottingService.ApplyDirectCombatContact(attacker, defender);

                outcome.DamageToDefender = atk.DamageToDefender;
                outcome.DamageToAttacker = atk.DamageToAttacker;
                outcome.DefenderOutcome = atk.DefenderOutcome;
                outcome.DefenderFinalPosition = defender.MapPos;
                outcome.VacatedHex = defender.MapPos;

                // (2) Attacker killed by return fire (§7.4.2.3) — no stand check, just removal.
                if (atk.AttackerDestroyed || attacker.IsDestroyed())
                {
                    outcome.AttackerDestroyed = true;
                    context.Remove(attacker);
                    outcome.AttackerRemovedFromMap = true;
                }

                // (3) Defender board consequences.
                if (atk.DefenderDestroyed || defender.IsDestroyed())
                {
                    // HP hit 0 in the engagement — no displacement, but the hex is vacated so AA opens (§7.9.9.2).
                    outcome.DefenderDestroyed = true;
                    outcome.AutomaticAdvanceAvailable = true;
                    outcome.PrestigeOwedToAttacker = PrestigeOnKill(defender);
                    context.Remove(defender);
                    outcome.DefenderRemovedFromMap = true;
                }
                else if (atk.DefenderOutcome != StandOutcome.Hold)
                {
                    DisplacementResult disp =
                        RetreatResolver.ResolveDisplacement(attacker, defender, atk.DefenderOutcome, map, rng,
                            failOnError: true, combatContext: context);
                    CombatActionRandom.ThrowIfFailed(rng);

                    outcome.DefenderMoved = disp.Moved;
                    outcome.DefenderFinalPosition = disp.FinalPosition;
                    outcome.DefenderHexesRetreated = disp.HexesRetreated;
                    outcome.AutomaticAdvanceAvailable = disp.AutomaticAdvanceAvailable;
                    outcome.VacatedHex = disp.VacatedHex;

                    bool permanent = disp.Destroyed || disp.Surrendered || disp.StaticCollapsed;
                    if (disp.RemovedFromMap || permanent)
                    {
                        outcome.DefenderDestroyed = permanent;          // a shatter-quit survives (§7.9.6.5)
                        if (permanent)
                            outcome.PrestigeOwedToAttacker = PrestigeOnKill(defender);
                        context.Remove(defender);
                        outcome.DefenderRemovedFromMap = true;
                    }
                }

                // (4) Combat degradation (§7.15.3 Efficiency + §7.15.5 Supply) — both sides, only while still in play.
                ApplyCombatDegradation(attacker, alive: !outcome.AttackerDestroyed, rng);
                ApplyCombatDegradation(defender, alive: !outcome.DefenderRemovedFromMap, rng);
                CombatActionRandom.ThrowIfFailed(rng);

                // (5) Leader reputation (§14.5) — attacker's leader earns for the action and its results.
                AwardAttackerReputation(attacker, defender, outcome);

                return outcome;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Execute), e);
                outcome.ResolutionFailed = true;
                outcome.Reason = "Combat resolution failed; the attack has stopped. Committed costs are retained.";
                outcome.AttackerFinalPosition = attacker?.MapPos ?? Position2D.Zero;
                outcome.AttackerDestroyed |= attacker != null && attacker.IsDestroyed();
                if (defender != null)
                {
                    outcome.DefenderFinalPosition = defender.MapPos;
                    outcome.DefenderMoved |= defender.MapPos != outcome.VacatedHex;
                    outcome.DefenderDestroyed |= defender.IsDestroyed();
                    if (outcome.DefenderMoved)
                        outcome.DefenderHexesRetreated = Math.Max(outcome.DefenderHexesRetreated,
                            HexMapUtil.GetHexDistance(outcome.VacatedHex, defender.MapPos));
                }
                return outcome;
            }
        }

        #region Helpers

        /// <summary>
        /// Eligibility gate for a direct ground attack. Returns null if legal, else the rejection reason.
        /// PUBLIC because the input layer's cursor feedback (§24.11.3) must run the SAME check the click runs —
        /// the cursor never lies. No dice, no costs.
        /// </summary>
        public static string CanExecute(CombatUnit attacker, CombatUnit defender, HexMap map)
        {
            if (attacker == null) return "No attacker.";
            if (defender == null) return "No target.";
            if (map == null) return "No map.";
            if (attacker.IsDestroyed()) return "Attacker is destroyed.";
            if (defender.IsDestroyed()) return "Target is already destroyed.";
            if (attacker.Side == defender.Side) return "Cannot attack a friendly unit.";
            if (attacker.IsBase) return "Bases cannot initiate attacks.";
            if (defender.IsBase) return "Use the base-combat path against a base (§11.7).";
            if (attacker.DeploymentPosition == DeploymentPosition.Embarked) return "An embarked unit cannot attack.";

            // Fog is one-directional in v1 (SpottedLevel lives on AI units), so only a player attack on an AI unit
            // requires spotting — "cannot strike what you cannot see". An AI attack on a player unit is unrestricted.
            if (defender.Side == Side.AI && defender.SpottedLevel < SpottedLevel.Level1)
                return "Target is not spotted.";

            if (!HexMapUtil.GetDirectionBetween(attacker.MapPos, defender.MapPos).HasValue)
                return "Target is not adjacent (direct fire only).";
            if (attacker.GetCombatActions() < 1)
                return "Attacker has no combat action available.";

            return null;
        }

        /// <summary>
        /// Awards the attacker's leader reputation for this attack (§14.5, wired 2026-07-03 — the earn side
        /// of the REP economy). Combat (3) always; ForcedRetreat (5) when the defender was displaced or quit
        /// the field; UnitDestroyed (8, ×2 for an Elite kill) on a permanent destruction. Veteran/Elite
        /// attacker units earn ×1.5 on every award (§14.5.10). No-op if the attacker is unled or dead.
        /// </summary>
        private static void AwardAttackerReputation(CombatUnit attacker, CombatUnit defender, in GroundCombatOutcome outcome)
        {
            try
            {
                if (outcome.AttackerDestroyed) return;
                var leader = attacker.GetAssignedLeader();
                if (leader == null) return;

                float unitMult = attacker.ExperienceLevel >= ExperienceLevel.Veteran
                    ? GameData.REP_EXPERIENCE_MULTIPLIER : 1.0f;

                leader.AwardReputationForAction(GameData.ReputationAction.Combat, unitMult);

                if (outcome.DefenderDestroyed)
                {
                    float killMult = defender.ExperienceLevel == ExperienceLevel.Elite
                        ? unitMult * GameData.REP_ELITE_DIFFICULTY_BONUS : unitMult;
                    leader.AwardReputationForAction(GameData.ReputationAction.UnitDestroyed, killMult);
                }
                else if (outcome.DefenderMoved || outcome.DefenderRemovedFromMap)
                {
                    leader.AwardReputationForAction(GameData.ReputationAction.ForcedRetreat, unitMult);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AwardAttackerReputation), e);
            }
        }

        /// <summary>Rolls combat Efficiency (§7.15.3) then Supply (§7.15.5) loss for a unit still in play.</summary>
        private static void ApplyCombatDegradation(CombatUnit unit, bool alive, ICombatRandom rng)
        {
            if (!alive || unit == null) return;

            unit.SetEfficiencyLevel(
                DegradationCheck.ApplyCombatEfficiencyLoss(unit.EfficiencyLevel, unit.ExperienceLevel, rng));

            if (DegradationCheck.RollCombatSupplyLoss(unit.ExperienceLevel, rng))
                unit.ConsumeSupplies(1f);
        }

        private static TerrainType TerrainAt(HexMap map, Position2D pos) =>
            map.GetHexAt(pos)?.Terrain ?? TerrainType.Clear;

        /// <summary>
        /// §18.2.3 — PRESTIGE_KILL_FRACTION of the destroyed unit's purchase value (V19: the fraction
        /// is Bob's tuning dial in GameData; away-from-zero rounding so 0.5 of an odd cost is consistent).
        /// Basis is CombatUnit.PurchaseCost — the Σ of populated bays (§18.3.1, prestige pass 2026-08-22,
        /// item 9; REPLACES the active-profile read, whose bounty changed with the victim's posture).
        /// Reported, not credited — crediting lands with the M13 wallet wiring. A shatter-WITHDRAWAL
        /// pays nothing (§7.9.6.5); only callers that detect a permanent kill invoke this.
        /// </summary>
        private static int PrestigeOnKill(CombatUnit killed)
        {
            int cost = killed.PurchaseCost;
            return (int)Math.Round(cost * GameData.PRESTIGE_KILL_FRACTION, MidpointRounding.AwayFromZero);
        }

        #endregion // Helpers
    }
}
