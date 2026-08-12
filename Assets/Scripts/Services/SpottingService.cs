using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// One air-defence unit eligible to fire on a transiting aircraft this step (§11.8), paired with whether
    /// it was still UNSPOTTED when the aircraft arrived — the §6.10 air-ambush case.
    /// </summary>
    /// <remarks>
    /// (Replaced the old <c>AirAmbushResult</c> tri-state 2026-08-11. That enum could describe only ONE
    /// air-defence unit per hex and only the unspotted-ambush half of §11.8, which is why the path it served
    /// ended in a coin flip.)
    /// </remarks>
    public readonly struct TransitAirDefenseContact
    {
        /// <summary>The air-defence unit that may fire.</summary>
        public readonly CombatUnit Firer;

        /// <summary>
        /// True if <see cref="Firer"/> was at <see cref="SpottedLevel.Level0"/> when the aircraft entered.
        /// ONLY this case is a §6.10 air ambush, and only a FIXED-WING mover gets the §5.13.3.2 detection
        /// roll against it — §5.13.2.4 gives a helicopter no roll at all.
        /// </summary>
        public readonly bool WasUnspotted;

        public TransitAirDefenseContact(CombatUnit firer, bool wasUnspotted)
        {
            Firer = firer;
            WasUnspotted = wasUnspotted;
        }
    }

    /// <summary>
    /// Handles all spotting, fog-of-war, and ambush detection logic for the battle scene.
    /// Called by BattleManager at turn start/admin phase and by MovementController per hex step for the
    /// EVENT checks (ambush, air ambush) — the mover's own passive spotting applies once at move
    /// settlement (§12.4.4a), not per hex.
    /// </summary>
    public static class SpottingService
    {
        private const string CLASS_NAME = nameof(SpottingService);

        #region Core Spotting

        /// <summary>
        /// Full spotting pass: each player spotter checks all enemy units within range and applies its
        /// PASSIVE CONTACT CEILING (§12.4.2 — Level 1 at range, Level 2 adjacent, Level 3 for an adjacent
        /// RECON unit). Called at turn start.
        ///
        /// Passive spotting can no longer walk a unit up the whole ladder: repeated looks do not accumulate
        /// (§12.4.1). Equipment counts and morale come from combat and IntelActions, never from staring.
        /// </summary>
        public static void RecomputeAllSpotting()
        {
            try
            {
                var gdm = GameDataManager.Instance;
                var playerUnits = gdm.GetPlayerUnits();
                var enemyUnits = gdm.GetAIUnits();

                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    foreach (var enemy in enemyUnits)
                    {
                        if (enemy.IsDestroyed()) continue;

                        int range = SpottingRangeAgainst(spotter, enemy);
                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                        if (dist <= range)
                            RaiseToCeiling(enemy, PassiveContactCeiling(spotter, dist));
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecomputeAllSpotting), e);
            }
        }

        /// <summary>
        /// The MOVER's passive spotting for a whole move order, applied ONCE at settlement over every hex
        /// the unit entered plus its resting hex (§12.4.4a, ratified 2026-08-10). Returns the enemies that
        /// transitioned from Level0 (newly visible).
        /// </summary>
        /// <remarks>
        /// ⚠ THE MOVE IS COMMITTED BLIND — the Panzer General rule. This used to run per hex, and that is
        /// precisely what made §6.9 ground ambush STRUCTURALLY UNREACHABLE: a mover must stand at distance
        /// 2 before it can stand at distance 1, ground spotting is a deterministic 2, so the sweep raised
        /// every ambusher to Level1 one hex before adjacency and the trigger's Level0 requirement could
        /// never be met. Applied post-hoc, a hidden enemy is still hidden when you blunder past it. The
        /// EVENT-DRIVEN reveals (contact halt, ambush §6.9.3, air-ambush detection §12.4.8, firing
        /// §12.4.9) are the only mid-move reveals, and they stay where they are.
        ///
        /// ⚠ Each observed hex contributes its own distance ceiling, so a drive-past at adjacency earns
        /// the §12.4.2 adjacency ceiling even when the move ends far away — the column reports what it
        /// passed. Fleeting contacts then hold or decay by the ordinary §12.6 floor from the FINAL board
        /// position, which is the model working as intended, not a leak.
        ///
        /// ⚠ DO NOT "OPTIMISE" WITH A FIXED-WING SKIP AT THE CALL SITE. <see cref="SpottingRangeAgainst"/>
        /// already resolves a transiting jet to range 0 against ground targets (§12.3.7a) — but RECONA and
        /// AWACS keep their ratified 8-hex look-down THROUGH THIS SAME PATH, and a helo-borne unit sees
        /// the ground normally. The range function is the policy; the sweep must stay uniform.
        /// </remarks>
        public static List<CombatUnit> ApplyPostMoveSpotting(CombatUnit mover, IReadOnlyList<Position2D> observedFrom)
        {
            var newlySpotted = new List<CombatUnit>();
            try
            {
                if (mover == null || observedFrom == null || observedFrom.Count == 0)
                    return newlySpotted;

                var enemies = GameDataManager.Instance.GetAIUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(mover, enemy);

                    // Best ceiling across every hex the column observed from. Duplicates in the list are
                    // harmless — a hex contributes the same ceiling every time.
                    var best = SpottedLevel.Level0;
                    for (int i = 0; i < observedFrom.Count; i++)
                    {
                        int dist = HexMapUtil.GetHexDistance(observedFrom[i], enemy.MapPos);
                        if (dist > range) continue;

                        var ceiling = PassiveContactCeiling(mover, dist);
                        if (ceiling > best) best = ceiling;
                    }
                    if (best == SpottedLevel.Level0) continue;

                    var oldLevel = enemy.SpottedLevel;
                    RaiseToCeiling(enemy, best);
                    if (oldLevel == SpottedLevel.Level0)
                        newlySpotted.Add(enemy);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyPostMoveSpotting), e);
            }
            return newlySpotted;
        }

        /// <summary>
        /// Reverse spotting: checks all player spotters against a moved enemy unit.
        /// Used for AI-turn spotting.
        /// </summary>
        public static void CheckSpottingByStationary(CombatUnit movedEnemy)
        {
            try
            {
                if (movedEnemy == null || movedEnemy.IsDestroyed()) return;

                var playerUnits = GameDataManager.Instance.GetPlayerUnits();
                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(spotter, movedEnemy);
                    int dist = HexMapUtil.GetHexDistance(spotter.MapPos, movedEnemy.MapPos);
                    if (dist <= range)
                        RaiseToCeiling(movedEnemy, PassiveContactCeiling(spotter, dist));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckSpottingByStationary), e);
            }
        }

        /// <summary>
        /// Spotting decay (§3.3.4 / §12.6, REWRITTEN 2026-07-24) — runs once per side at Refresh, one step
        /// per turn. GRADUATED, not a collapse:
        ///
        ///  1. Re-derive the enemy's SUSTAINED FLOOR from the board (§12.6.2). Passive contact never decays.
        ///  2. At or below the floor: hold (and top up to the floor if something has pushed it lower).
        ///  3. Above the floor but ADJACENT to a friendly unit: hold (§12.6.3 — contact preserves intel).
        ///  4. Otherwise: drop exactly one rung, never below the floor, all the way down to Level0.
        ///
        /// The old model dropped anything at Level2+ straight to Level1, which on a six-rung ladder would
        /// erase three IntelActions of investment in a single Refresh. Decay must still be able to reach
        /// Level0 or units would stop disappearing and fog of war (§12.8) would break.
        /// </summary>
        public static void ProcessSpottingDecay()
        {
            try
            {
                var enemies = GameDataManager.Instance.GetAIUnits();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsDestroyed()) continue;

                    var floor = SustainedFloor(enemy);
                    var current = enemy.SpottedLevel;

                    if (current <= floor)
                    {
                        RaiseToCeiling(enemy, floor);   // no-op unless passive contact outranks the current level
                        continue;
                    }

                    if (IsAdjacentToPlayerUnit(enemy)) continue;

                    var newLevel = current - 1;
                    if (newLevel < floor) newLevel = floor;

                    ApplyLevel(enemy, newLevel);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ProcessSpottingDecay), e);
            }
        }

        /// <summary>
        /// Ground IntelAction application (§12.4.5): every ENEMY unit ADJACENT to <paramref name="actor"/>
        /// gains one rung, ceiling Level 5. Adjacency is required — there is no intel range — and all adjacent
        /// enemies are affected rather than a picked target.
        ///
        /// This is the ONLY route to Level 5, and the deliberate alternative to attacking: three IntelActions
        /// walk a unit from contact to a full picture without firing a shot, where combat buys Level 4 in one
        /// action but starts a fight. The caller is responsible for spending the action itself
        /// (CombatUnit.PerformIntelAction) — this method only applies the intel result.
        /// </summary>
        public static void ApplyGroundIntelAction(CombatUnit actor)
        {
            try
            {
                if (actor == null || actor.IsDestroyed()) return;

                var gdm = GameDataManager.Instance;
                foreach (var neighborPos in HexMapUtil.GetAllNeighborPositions(actor.MapPos))
                {
                    var target = gdm.GetGroundUnitAtHex(neighborPos);
                    if (target == null || target.IsDestroyed()) continue;
                    if (target.Side == actor.Side) continue;

                    RaiseByOneRung(target, SpottedLevel.Level5);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyGroundIntelAction), e);
            }
        }

        /// <summary>
        /// Direct-combat intel (§12.4.6): both participants are set to Level 4 against each other — you have
        /// been close enough to count what the other side is fighting with. Applied to the ENEMY side only,
        /// since CombatUnit.SpottedLevel is the player's view of AI units; the AI's reciprocal knowledge lives
        /// in its belief store.
        ///
        /// Scope note: this is DIRECT combat only. Indirect fire has its own explicit reveal rule for the
        /// FIRER (§12.4.9.2, +1 rung minimum Level 1); the doc says nothing about what an indirect firer
        /// learns about its target, so nothing is applied there rather than inventing a rule.
        /// </summary>
        public static void ApplyDirectCombatContact(CombatUnit attacker, CombatUnit defender)
        {
            try
            {
                if (attacker == null || defender == null) return;

                if (attacker.Side != Side.Player) RaiseToCeiling(attacker, SpottedLevel.Level4);
                if (defender.Side != Side.Player) RaiseToCeiling(defender, SpottedLevel.Level4);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ApplyDirectCombatContact), e);
            }
        }

        /// <summary>
        /// Checks if an enemy unit is currently within spotting range of any player unit.
        /// </summary>
        public static bool IsCurrentlySpotted(CombatUnit enemy)
        {
            try
            {
                var playerUnits = GameDataManager.Instance.GetPlayerUnits();
                foreach (var spotter in playerUnits)
                {
                    if (spotter.IsDestroyed()) continue;

                    int range = SpottingRangeAgainst(spotter, enemy);
                    int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                    if (dist <= range) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsCurrentlySpotted), e);
                return false;
            }
        }

        #endregion // Core Spotting

        #region AI-Side Perception (AI2b — symmetric sweep, AI-Design-Supplement Part 3)

        /// <summary>
        /// The AI-side mirror of <see cref="RecomputeAllSpotting"/>: every AI spotter checks every player
        /// unit under the same dual-domain ranges (§12.3), but hits feed the AI's BELIEF STORE — never
        /// CombatUnit.SpottedLevel, which remains the player's view of AI units. Run at AI_Refresh
        /// (§3.3.4 "per side"). Camouflage (§14.9.4) applies symmetrically via SpottingRangeAgainst.
        /// </summary>
        public static void RecomputeAIPerception(Models.AI.AIPerceptionState perception, int currentTurn)
        {
            try
            {
                if (perception == null) return;

                var gdm = GameDataManager.Instance;
                foreach (var spotter in gdm.GetAIUnits())
                {
                    if (spotter.IsDestroyed()) continue;

                    foreach (var target in gdm.GetPlayerUnits())
                    {
                        if (target.IsDestroyed()) continue;

                        int range = SpottingRangeAgainst(spotter, target);
                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, target.MapPos);
                        if (dist <= range)
                        {
                            // Same source-ceiling rule the player side uses (§12.4.2) — the AI does not get
                            // a different progression model, only a different store.
                            perception.RecordSpot(
                                target.UnitID, target.MapPos, currentTurn,
                                target.Classification,
                                ObservedHpPercent(target),
                                Mathf.Max(1, Mathf.RoundToInt(target.MovementPoints.Max)),
                                PassiveContactCeiling(spotter, dist));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecomputeAIPerception), e);
            }
        }

        /// <summary>
        /// The AI-side mirror of <see cref="ProcessSpottingDecay"/> (§12.6): player units currently inside
        /// some AI spotter's range hold their contact level; the rest decay one step in the belief store
        /// (ghosting at Level0). Run at AI_Refresh, after <see cref="RecomputeAIPerception"/>.
        /// </summary>
        public static void StepAIPerceptionDecay(Models.AI.AIPerceptionState perception, int currentTurn)
        {
            try
            {
                if (perception == null) return;

                var gdm = GameDataManager.Instance;
                var floors = new Dictionary<string, SpottedLevel>();
                var adjacent = new HashSet<string>();

                foreach (var target in gdm.GetPlayerUnits())
                {
                    if (target.IsDestroyed()) continue;

                    foreach (var spotter in gdm.GetAIUnits())
                    {
                        if (spotter.IsDestroyed()) continue;

                        int dist = HexMapUtil.GetHexDistance(spotter.MapPos, target.MapPos);
                        if (dist <= 1) adjacent.Add(target.UnitID);

                        int range = SpottingRangeAgainst(spotter, target);
                        if (dist > range) continue;

                        // The AI's SUSTAINED FLOOR, built from the same PassiveContactCeiling the player side
                        // uses (§12.6.2). It must be the best ceiling across ALL spotters, so this cannot
                        // break early on the first in-range hit the way a boolean in-range test could.
                        var ceiling = PassiveContactCeiling(spotter, dist);
                        if (!floors.TryGetValue(target.UnitID, out var best) || ceiling > best)
                            floors[target.UnitID] = ceiling;
                    }
                }

                perception.StepDecay(currentTurn, floors, adjacent);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(StepAIPerceptionDecay), e);
            }
        }

        private static int ObservedHpPercent(CombatUnit unit)
        {
            float max = unit.HitPoints.Max;
            return max <= 0f ? 0 : Mathf.RoundToInt(unit.HitPoints.Current / max * 100f);
        }

        #endregion // AI-Side Perception

        #region Ambush Detection

        /// <summary>
        /// Checks for ground ambush: returns the unspotted enemy whose ZoC the mover entered, or null.
        /// </summary>
        /// <param name="alreadySprung">
        /// ⚠ ANTI-DOGPILE (§11.8.6, extended to ambush 2026-08-10): unit IDs that have already ambushed
        /// THIS mover during THIS move order. An ambusher gets ONE bite per aircraft — without it a
        /// helicopter, which now flies ON through an ambush rather than halting, can be engaged repeatedly
        /// by the same regiment as it crosses several hexes in that regiment's reach, and Shock compounds
        /// until a defended line deletes a gunship in a single order. §11.8.6 already sets exactly this
        /// limit for air-defence fire; this is the same rule for the same reason.
        /// </param>
        public static CombatUnit CheckGroundAmbush(CombatUnit mover, Position2D newPos,
            ISet<string> alreadySprung = null)
        {
            try
            {
                var gdm = GameDataManager.Instance;
                var neighbors = HexMapUtil.GetAllNeighborPositions(newPos);

                foreach (var neighborPos in neighbors)
                {
                    var ground = gdm.GetGroundUnitAtHex(neighborPos);
                    if (ground == null) continue;
                    if (ground.Side == mover.Side) continue;
                    if (ground.SpottedLevel != SpottedLevel.Level0) continue;
                    if (!ground.ProjectsZoC) continue;

                    /* §6.9.9 — the classification half of the gate (ProjectsZoC above already rejects
                     * bases and Embarked). A hidden tube battery is the ambush VICTIM, never the
                     * ambusher: the mover passes it unmolested and learns of it at settlement
                     * (§12.4.4a). Enforced here since 2026-08-10 — it was checked nowhere before,
                     * invisible only because the trigger itself could never fire. */
                    if (!GameData.IsAmbushEligible(ground.Classification)) continue;

                    if (alreadySprung != null && alreadySprung.Contains(ground.UnitID)) continue;

                    return ground; // ambusher found
                }

                return null;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CheckGroundAmbush), e);
                return null;
            }
        }

        /// <summary>
        /// Every air-defence unit eligible to fire an opportunity shot at <paramref name="mover"/> as it
        /// enters <paramref name="newPos"/> (§11.8). This owns only the question of WHO may shoot; the caller
        /// resolves the shots, spends the budget and applies the consequences.
        /// </summary>
        /// <remarks>
        /// ⚠ ELIGIBILITY IS THE CLASSIFICATION — <see cref="GameData.IsAirDefenseClassification"/>, ruled by
        /// Bob 2026-08-11. Only SAM / SPSAM / AAA / SPAAA interdict a transiting aircraft at range. NOT GAT:
        /// restricting GAT to true air-defence units would break the stat-comparison paradigm (every unit
        /// needs every stat for a Δ to exist), and a `GAT ≥ 6` test does not even produce the intended set —
        /// `MANPADS_BASIC` floors infantry GAT at exactly 6, so it admits nearly every line regiment. GAT
        /// remains the ATTACK VALUE in the lane; it was never the right question for "who may shoot".
        /// ⚠ Infantry organic anti-air is not lost by this — it is the §11.8.11 overhead GAD rule, which
        /// fires when a helicopter crosses directly above a ground unit. Ranged interdiction belongs to
        /// dedicated batteries alone. (A brief 2026-08-11 GAT re-key is REVERTED; do not reinstate it.)
        ///
        /// ⚠ SPOTTED AIR DEFENCE FIRES TOO — THIS IS NOT AMBUSH-ONLY, and that is the second half of the
        /// 2026-08-11 fix. §11.8.4 makes the firing opportunity automatic for any eligible unit in range;
        /// §6.10 air ambush is the NARROWER case where the firer happened to be unspotted, which buys a
        /// fixed-wing mover one detection roll and nothing else. The old code returned on the first UNSPOTTED
        /// air-defence unit it found and never looked at the rest, so a SAM the player had already located was
        /// completely harmless — exactly backwards, since flying past a known SAM should be the informed risk.
        ///
        /// ⚠ SIDE-AGNOSTIC (<c>enemy.Side != mover.Side</c>), like <see cref="CheckGroundAmbush"/> rather than
        /// like the <c>GetAIUnits()</c> call it replaced: player air defence must engage AI aircraft the day
        /// the AI starts flying (M13), without anyone having to remember this file.
        /// </remarks>
        public static List<TransitAirDefenseContact> FindTransitAirDefense(CombatUnit mover, Position2D newPos)
        {
            var contacts = new List<TransitAirDefenseContact>();

            try
            {
                var gdm = GameDataManager.Instance;
                if (mover == null || gdm == null) return contacts;

                foreach (var enemy in gdm.GetAllCombatUnits())
                {
                    if (enemy == null || enemy.IsDestroyed()) continue;
                    if (enemy.Side == mover.Side) continue;

                    /* §11.8 is GROUND-to-air opportunity fire. Air-to-air belongs to the AOB/AIB venue
                     * (§11.4.8 / §11.8.10), which has its own slots and its own action currency. Asking the
                     * domain rather than the classification also lets an attack helo be excluded by its
                     * (empty) opportunity budget below rather than by a special case here. */
                    if (enemy.OccupiesDomain != Domain.Ground) continue;

                    // §11.8.2 — THE GATE: only a dedicated air-defence type interdicts at range.
                    if (!GameData.IsAirDefenseClassification(enemy.Classification)) continue;

                    // §11.8.8 — packed guns and radars do not shoot.
                    if (!GameData.PostureAllowsOpportunityFire(enemy.DeploymentPosition)) continue;

                    /* §11.8.3 — the per-turn shot METER, denominated in OpportunityActions (§8.5.4 grants
                     * exactly these four classes 2 apiece, for exactly this). ⚠ A METER, NOT A GATE: what
                     * makes a unit an air-defence shooter is the line above; this only bounds how OFTEN a
                     * genuine battery shoots, so a SAM cannot engage every aircraft on the map every turn.
                     * ASKED here, never SPENT — a scan that spent would charge every battery whose envelope
                     * an aircraft merely clipped. The non-announcing predicate is deliberate (see
                     * CanPerformOpportunityAction): this runs over ENEMY units and the spender narrates its
                     * refusals into the player's message log, which would leak an unspotted battery. */
                    if (!enemy.CanPerformOpportunityAction()) continue;

                    // §11.8.6 — one shot per aircraft per turn, however many hexes it crosses in reach.
                    if (enemy.HasEngagedAircraftThisTurn(mover.UnitID)) continue;

                    int engagementRange = Mathf.FloorToInt(enemy.ActivePrimaryRange);
                    if (engagementRange <= 0) engagementRange = 2;   // fallback: profile states no reach
                    if (HexMapUtil.GetHexDistance(newPos, enemy.MapPos) > engagementRange) continue;

                    contacts.Add(new TransitAirDefenseContact(enemy, enemy.SpottedLevel == SpottedLevel.Level0));
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(FindTransitAirDefense), e);
            }

            return contacts;
        }

        /// <summary>
        /// §6.10.3/.4 — the FIXED-WING air-ambush detection roll against an unspotted air-defence unit.
        /// True if the aircraft spots the threat first: the shot is averted and <paramref name="firer"/> is
        /// revealed at CONTACT (§12.4.8) without having fired.
        /// </summary>
        /// <remarks>
        /// ⚠ FIXED-WING ONLY (§5.13.3.2), and the name says so because the split is the whole point of this
        /// pass. A HELICOPTER GETS NO ROLL (§5.13.2.4): it takes the hit whenever the air-defence unit has
        /// shots available, and its way out is the §11.8.9 transit stand check AFTER the damage, not a chance
        /// to avoid it. Until 2026-08-11 every airborne mover rolled this, which handed helicopters an
        /// evasion the design never gave them.
        /// </remarks>
        public static bool RollFixedWingAmbushDetection(CombatUnit firer, CombatUnit mover)
        {
            try
            {
                if (firer == null || mover == null) return false;

                if (!AirAmbushCheck.RollDetection(mover.ExperienceLevel, new CombatRandom()))
                    return false;

                // Detected, no shot fired: revealed at Level1 (§12.4.8) — you found the radar, you did not
                // get shot at by it. The FIRING case reveals at Level4 (RevealByOpportunityFire).
                RaiseToCeiling(firer, SpottedLevel.Level1);
                EventManager.Instance?.RaiseAirAmbushDetected(firer, mover);
                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RollFixedWingAmbushDetection), e);
                return false;   // safe default: undetected, the caller resolves the shot
            }
        }

        /// <summary>
        /// §12.4.9.1 / §11.8.4 — a unit that FIRES from the open is revealed at Level4 (position AND
        /// equipment). The opportunity-fire counterpart to <see cref="RevealToContact"/>.
        /// </summary>
        /// <remarks>
        /// ⚠ LEVEL 4, NOT LEVEL 1, and the contrast with the ambusher is the rule rather than an
        /// inconsistency. §6.9.3 grants only a CONTACT because an ambusher fires from concealment; an
        /// air-defence unit engaging an aircraft has its radars hot and its position obvious, so the player
        /// learns what it is as well as where. This is the §11.8.4 "UI reveals the firing unit at Level 4".
        /// </remarks>
        public static void RevealByOpportunityFire(CombatUnit firer)
        {
            try
            {
                if (firer == null) return;
                RaiseToCeiling(firer, SpottedLevel.Level4);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RevealByOpportunityFire), e);
            }
        }

        /// <summary>
        /// Reveals a unit at CONTACT (Level 1) — the reveal for a unit whose presence became known without
        /// it firing from the open. Two callers: the §6.9.3 sprung ambusher (`AmbushAction`) and the
        /// contact-halt blocker (`MovementController`).
        /// </summary>
        /// <remarks>
        /// ⚠ LEVEL 1, NOT LEVEL 4, and the difference is which §12 rule applies. Opportunity and AD fire
        /// reveal at Level4 (§12.4.9.1 — radars hot and shooting from the open exposes WHAT you are). An
        /// ambusher fires FROM CONCEALMENT: §6.9.3 grants the victim a contact, not an identification —
        /// you know where the fire came from, not what is dug in there.
        ///
        /// (History: built 2026-08-04 for the flight-evasion halt, whose rule was RETIRED 2026-08-10 when
        /// helos started taking the ambush attack. The method outlived its first caller because §6.9.3 and
        /// the contact halt need exactly this reveal.)
        /// </remarks>
        public static void RevealToContact(CombatUnit ambusher)
        {
            try
            {
                if (ambusher == null) return;
                RaiseToCeiling(ambusher, SpottedLevel.Level1);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RevealToContact), e);
            }
        }

        #endregion // Ambush Detection

        #region Private Helpers

        /// <summary>
        /// The spotting range <paramref name="spotter"/> uses against <paramref name="target"/> under the
        /// dual-domain rule (§12.3): the spotter's AIR range vs an airborne target (any fixed-wing, or an
        /// AM/MAM air-assault lift in EmbarkedHelo state), its GROUND range otherwise. Classification-driven
        /// and decoupled from the profile SpottingRange (which is UI-only now). Attack helos fly NOE and are
        /// ground targets; air-defence platforms' long ranges are air-search only (a SAM reveals ground at 2).
        /// Superior Camouflage (§14.9.4) shortens the range against a led target — applied here, at the
        /// §12.3.10 comparison, so it affects the sweep, per-hex checks, and decay uniformly.
        /// </summary>
        private static int SpottingRangeAgainst(CombatUnit spotter, CombatUnit target)
        {
            // A fixed-wing aircraft in transit does not look at the ground (§12.3.7a, Bob 2026-08-10).
            if (!target.IsSeenAsAir && FliesPastTheGround(spotter)) return 0;

            int range = target.IsSeenAsAir
                ? spotter.ActiveAirSpottingRange
                : spotter.ActiveGroundSpottingRange;

            return Math.Max(0, range - target.EnemySpottingRangeReduction);
        }

        /// <summary>
        /// True for a unit travelling FIXED-WING that is not a dedicated look-down sensor platform — i.e. one
        /// that is merely crossing the map and sees nothing on the ground below it.
        /// </summary>
        /// <remarks>
        /// ⚠ THE RULING (Bob, 2026-08-10): "FW air units do not even spot ground units during a transit
        /// attempt." Fixed-wing assets only ever traverse the map on their way to the air ops box; they are
        /// not loitering observers. What CAN happen to them on the way is the reverse — an air defence unit
        /// fires on them and reveals ITSELF (<see cref="FindTransitAirDefense"/>). Ground troops neither see
        /// them nor are seen by them.
        ///
        /// ⚠ KEYED ON THE MEDIUM, NOT THE CLASSIFICATION, because a paratroop regiment riding an An-12 is
        /// classification AB — it would otherwise keep its ground-combat spotting range of 2 and go on
        /// spotting enemies from inside a transport aircraft.
        ///
        /// ⚠ RECONA AND AWACS ARE EXEMPT, AND THIS IS THE ONE JUDGEMENT CALL IN THE RULE. Both are ratified
        /// look-down platforms whose ground reach is load-bearing elsewhere: §12.3.8 gives RECONA 8 hexes
        /// and §11.11.3 builds the recon mission's whole search area out of it, while §12.3.9 gives AWACS 8
        /// and calls exploiting it near the front a deliberate player risk. Zeroing those would silently
        /// delete air reconnaissance, which is plainly not what the ruling was about. ⚠ DESIGN-DOC
        /// AMENDMENT OWED: §12.3.7 becomes 0 / 4 for FGT / ATT / BMB / WW / TRN.
        /// </remarks>
        private static bool FliesPastTheGround(CombatUnit spotter)
        {
            if (MovementModeService.CurrentMedium(spotter) != MovementMedium.FixedWing) return false;

            bool isLookDownPlatform =
                spotter.Classification is UnitClassification.RECONA or UnitClassification.AWACS;

            return !isLookDownPlatform;
        }

        /// <summary>
        /// The PASSIVE-CONTACT ceiling a spotter earns against a target (§12.4.2): Level 3 if the spotter is a
        /// RECON-class unit standing adjacent, Level 2 if any spotter is adjacent, Level 1 at range. Range only
        /// establishes CONTACT — how much that contact is worth is decided here, which is what stops a unit
        /// from learning everything by standing still and staring (§12.4.4).
        /// </summary>
        private static SpottedLevel PassiveContactCeiling(CombatUnit spotter, int distance)
        {
            if (distance > 1) return SpottedLevel.Level1;

            return spotter.Classification == UnitClassification.RECON
                ? SpottedLevel.Level3
                : SpottedLevel.Level2;
        }

        /// <summary>
        /// "Set to" source semantics (§12.4.3): raises the target UP TO <paramref name="ceiling"/> and never
        /// lowers an already-higher level. Used by passive spotting, combat, and the ambush reveals.
        /// </summary>
        private static void RaiseToCeiling(CombatUnit unit, SpottedLevel ceiling)
        {
            if (unit.SpottedLevel >= ceiling) return;
            ApplyLevel(unit, ceiling);
        }

        /// <summary>
        /// "+1 rung" source semantics (§12.4.3): adds exactly one level and stops at <paramref name="ceiling"/>.
        /// Used by IntelActions, the SIGINT sweep, and air recon.
        /// </summary>
        private static void RaiseByOneRung(CombatUnit unit, SpottedLevel ceiling)
        {
            if (unit.SpottedLevel >= ceiling) return;
            ApplyLevel(unit, unit.SpottedLevel + 1);
        }

        /// <summary>
        /// Commits a level change and raises the change event. SetSpottedLevel may clamp below the requested
        /// value (Concealed Operations Base, §14.8.7), so the event always reports the level that actually
        /// landed rather than the one asked for.
        /// </summary>
        private static void ApplyLevel(CombatUnit unit, SpottedLevel level)
        {
            var oldLevel = unit.SpottedLevel;
            unit.SetSpottedLevel(level);

            if (unit.SpottedLevel == oldLevel) return;

            if (EventManager.Instance != null)
                EventManager.Instance.RaiseUnitSpottedLevelChanged(unit, oldLevel, unit.SpottedLevel);
        }

        /// <summary>True when any live player unit stands adjacent to <paramref name="enemy"/> (§12.6.3 hold).</summary>
        private static bool IsAdjacentToPlayerUnit(CombatUnit enemy)
        {
            foreach (var spotter in GameDataManager.Instance.GetPlayerUnits())
            {
                if (spotter.IsDestroyed()) continue;
                if (HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos) <= 1) return true;
            }
            return false;
        }

        /// <summary>
        /// The SUSTAINED FLOOR (§12.6.2): the best passive ceiling any live player spotter currently earns
        /// against this enemy. Re-derived from the board every Refresh, and never decays — decay only eats the
        /// rungs that were bought with combat, IntelActions, or a sweep.
        /// </summary>
        private static SpottedLevel SustainedFloor(CombatUnit enemy)
        {
            var floor = SpottedLevel.Level0;

            foreach (var spotter in GameDataManager.Instance.GetPlayerUnits())
            {
                if (spotter.IsDestroyed()) continue;

                int range = SpottingRangeAgainst(spotter, enemy);
                int dist = HexMapUtil.GetHexDistance(spotter.MapPos, enemy.MapPos);
                if (dist > range) continue;

                var ceiling = PassiveContactCeiling(spotter, dist);
                if (ceiling > floor) floor = ceiling;
            }

            return floor;
        }

        #endregion // Private Helpers
    }
}
