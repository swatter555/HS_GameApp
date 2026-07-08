using System;
using HammerAndSickle.Controllers;
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
        public bool Executed;                   // false → rejected before any dice/costs (see Reason)
        public string Reason;                   // rejection reason (null/empty on success)

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
        /// Dice come from <paramref name="rng"/> in a fixed order — engagement (§7.7.3) → displacement (§7.9) →
        /// degradation (attacker then defender, §7.15) — so seeded tests are stable. <paramref name="contestedCrossing"/>
        /// is supplied by the map-layer caller per §7.5.6.9.1 (river/bridge geometry); the defender's hex terrain is
        /// read from the map.
        /// </summary>
        public static GroundCombatOutcome Execute(
            CombatUnit attacker, CombatUnit defender, HexMap map, ICombatRandom rng, bool contestedCrossing = false)
        {
            try
            {
                if (rng == null)
                    return new GroundCombatOutcome { Executed = false, Reason = "No RNG." };
                string reason = CanExecute(attacker, defender, map);
                if (reason != null)
                    return new GroundCombatOutcome { Executed = false, Reason = reason };

                // §8.2.1 — spend 1 CombatAction + 25% max MP. Supply is GATED here, not consumed (§7.15.7.1);
                // the probabilistic combat-supply loss is rolled below (§7.15.5).
                if (!attacker.PerformCombatAction())
                    return new GroundCombatOutcome { Executed = false, Reason = "Attacker cannot afford the combat action." };

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

                var outcome = new GroundCombatOutcome
                {
                    Executed = true,
                    DamageToDefender = atk.DamageToDefender,
                    DamageToAttacker = atk.DamageToAttacker,
                    DefenderOutcome = atk.DefenderOutcome,
                    DefenderFinalPosition = defender.MapPos,
                    VacatedHex = defender.MapPos,
                };

                // (2) Attacker killed by return fire (§7.4.2.3) — no stand check, just removal.
                if (atk.AttackerDestroyed)
                {
                    outcome.AttackerDestroyed = true;
                    Unregister(attacker);
                }

                // (3) Defender board consequences.
                if (atk.DefenderDestroyed)
                {
                    // HP hit 0 in the engagement — no displacement, but the hex is vacated so AA opens (§7.9.9.2).
                    outcome.DefenderDestroyed = true;
                    outcome.DefenderRemovedFromMap = true;
                    outcome.AutomaticAdvanceAvailable = true;
                    outcome.PrestigeOwedToAttacker = PrestigeOnKill(defender);
                    Unregister(defender);
                }
                else if (atk.DefenderOutcome != StandOutcome.Hold)
                {
                    DisplacementResult disp =
                        RetreatResolver.ResolveDisplacement(attacker, defender, atk.DefenderOutcome, map, rng);

                    outcome.DefenderMoved = disp.Moved;
                    outcome.DefenderFinalPosition = disp.FinalPosition;
                    outcome.DefenderHexesRetreated = disp.HexesRetreated;
                    outcome.AutomaticAdvanceAvailable = disp.AutomaticAdvanceAvailable;
                    outcome.VacatedHex = disp.VacatedHex;

                    bool permanent = disp.Destroyed || disp.Surrendered || disp.StaticCollapsed;
                    if (disp.RemovedFromMap || permanent)
                    {
                        outcome.DefenderRemovedFromMap = true;
                        outcome.DefenderDestroyed = permanent;          // a shatter-quit survives (§7.9.6.5)
                        if (permanent)
                            outcome.PrestigeOwedToAttacker = PrestigeOnKill(defender);
                        Unregister(defender);
                    }
                }

                // (4) Combat degradation (§7.15.3 Efficiency + §7.15.5 Supply) — both sides, only while still in play.
                ApplyCombatDegradation(attacker, alive: !outcome.AttackerDestroyed, rng);
                ApplyCombatDegradation(defender, alive: !outcome.DefenderRemovedFromMap, rng);

                // (5) Leader reputation (§14.5) — attacker's leader earns for the action and its results.
                AwardAttackerReputation(attacker, defender, outcome);

                return outcome;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Execute), e);
                return new GroundCombatOutcome { Executed = false, Reason = "Internal error resolving the attack." };
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
        /// §18.2.3 — half the destroyed unit's purchase (prestige) cost. No prestige pool exists yet (§18), so this
        /// is reported for the future requisition system, not credited. A shatter-WITHDRAWAL pays nothing (§7.9.6.5);
        /// only callers that detect a permanent kill invoke this.
        /// </summary>
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
