using System;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// The outcome of a sprung ground ambush (§6.9), reported back to the Unity layer for the icon redraw,
    /// the printer dispatch and the movement halt.
    /// </summary>
    public struct AmbushOutcome
    {
        public bool Executed;                   // false → nothing was sprung (see Reason)
        public string Reason;

        public int DamageToMover;
        public StandOutcome MoverOutcome;

        public bool MoverMoved;                 // displaced by the stand result
        public Position2D MoverFinalPosition;
        public int MoverHexesRetreated;
        public bool MoverRemovedFromMap;        // shatter-quit OR a destruction → unregistered
        public bool MoverDestroyed;             // permanent loss (a shatter-quit survives, §7.9.6.5)

        public int PrestigeOwedToAmbusher;      // §18.2.3, reported only — no prestige economy yet
    }

    /// <summary>
    /// Orchestrates a sprung ground ambush end-to-end (§6.9) — the model-layer caller above the pure
    /// <see cref="CombatResolver.ResolveAmbush"/>, sibling to <see cref="GroundCombatAction"/> and
    /// <see cref="IndirectCombatAction"/>.
    /// </summary>
    /// <remarks>
    /// ⚠ WHY THIS EXISTS: UNTIL 2026-08-10 AN AMBUSH DEALT NO DAMAGE AT ALL. `ResolveAmbush` was written,
    /// tested and complete — and had ZERO live callers, while `MovementController` raised
    /// `EventManager.OnAmbushTriggered`, an event with ZERO subscribers. The mover was halted, a dispatch
    /// was printed, a sound played, and nothing else happened. §6.9 has been inert for the whole project.
    /// This class is the missing caller; the resolver's own doc-comment lists exactly the three duties it
    /// owes, and they are implemented below in order.
    ///
    /// ⚠ THE AMBUSHER SPENDS NOTHING. An ambush is sprung BY the mover blundering into it (§6.9), not
    /// ordered — so unlike <see cref="GroundCombatAction"/> there is no action-economy gate here. The
    /// mover's own move action was already spent getting into the hex, and its turn ends below (§6.9.6).
    ///
    /// ⚠ §7.15 DEGRADATION APPLIES TO THE VICTIM ONLY (ratified 2026-08-10). `GroundCombatAction` rolls it
    /// for both sides, but the ambusher takes no return fire (§6.9.5) and was never under pressure, so it
    /// has nothing to degrade from.
    /// </remarks>
    public static class AmbushAction
    {
        private const string CLASS_NAME = nameof(AmbushAction);

        /// <summary>
        /// Springs <paramref name="ambusher"/>'s ambush on <paramref name="mover"/>, which has just entered
        /// <paramref name="mover"/>.MapPos. Dice order: the ambush damage lane, the mover's stand roll, then
        /// displacement — so seeded tests are stable.
        /// </summary>
        public static AmbushOutcome Execute(CombatUnit ambusher, CombatUnit mover, HexMap map, ICombatRandom rng)
        {
            try
            {
                if (rng == null) return new AmbushOutcome { Executed = false, Reason = "No RNG." };
                if (ambusher == null) return new AmbushOutcome { Executed = false, Reason = "No ambusher." };
                if (mover == null) return new AmbushOutcome { Executed = false, Reason = "No mover." };
                if (map == null) return new AmbushOutcome { Executed = false, Reason = "No map." };

                mover.MarkFoughtThisTurn();
                ambusher.MarkFoughtThisTurn();

                // The mover is attacked as if on clear terrain (§7.5.6.7) — the resolver bypasses the block,
                // so the context terrain is informational for the stand check only.
                var ctx = new DirectAttackContext
                {
                    DefenderTerrain = map.GetHexAt(mover.MapPos)?.Terrain ?? TerrainType.Clear,
                    ContestedCrossing = false,
                };

                // (1) The ambush lane + the mover's stand check. Applies HP inside.
                AmbushResult res = CombatResolver.ResolveAmbush(ambusher, mover, ctx, rng);

                // (2) §6.9.3 — the ambusher is revealed to CONTACT by springing the trap. It fired, but from
                // concealment: the player learns where it is, not what it is.
                SpottingService.RevealToContact(ambusher);

                var outcome = new AmbushOutcome
                {
                    Executed = true,
                    DamageToMover = res.DamageToMover,
                    MoverOutcome = res.MoverOutcome,
                    MoverFinalPosition = mover.MapPos,
                };

                // (3) Board consequences for the mover.
                if (res.MoverDestroyed)
                {
                    outcome.MoverDestroyed = true;
                    outcome.MoverRemovedFromMap = true;
                    outcome.PrestigeOwedToAmbusher = PrestigeOnKill(mover);
                    Unregister(mover);
                }
                else if (res.MoverOutcome != StandOutcome.Hold)
                {
                    // Displaced with the AMBUSHER as the bearing reference — you fall back away from the
                    // hex the fire came from.
                    DisplacementResult disp =
                        RetreatResolver.ResolveDisplacement(ambusher, mover, res.MoverOutcome, map, rng);

                    outcome.MoverMoved = disp.Moved;
                    outcome.MoverFinalPosition = disp.FinalPosition;
                    outcome.MoverHexesRetreated = disp.HexesRetreated;

                    bool permanent = disp.Destroyed || disp.Surrendered || disp.StaticCollapsed;
                    if (disp.RemovedFromMap || permanent)
                    {
                        outcome.MoverRemovedFromMap = true;
                        outcome.MoverDestroyed = permanent;      // a shatter-quit survives (§7.9.6.5)
                        if (permanent) outcome.PrestigeOwedToAmbusher = PrestigeOnKill(mover);
                        Unregister(mover);
                    }
                }

                /* (4) §7.15 degradation — RATIFIED for ambush 2026-08-10. Applied to the VICTIM ONLY: the
                 * ambusher takes no return fire (§6.9.5) and was never under pressure, so it has nothing to
                 * degrade from. Skipped entirely if the mover has left the board. */
                ApplyCombatDegradation(mover, alive: !outcome.MoverRemovedFromMap, rng);

                return outcome;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Execute), e);
                return new AmbushOutcome { Executed = false, Reason = "Internal error resolving the ambush." };
            }
        }

        #region Helpers

        /// <summary>§18.2.3 — half the destroyed unit's purchase cost. Reported, never credited (no §18 economy).</summary>
        private static int PrestigeOnKill(CombatUnit killed)
        {
            int cost = killed.GetActiveWeaponProfile()?.PrestigeCost ?? 0;
            return cost / 2;
        }

        /// <summary>§7.15.3 Efficiency + §7.15.5 Supply loss, mirroring <see cref="GroundCombatAction"/>.</summary>
        private static void ApplyCombatDegradation(CombatUnit unit, bool alive, ICombatRandom rng)
        {
            if (!alive || unit == null) return;

            unit.SetEfficiencyLevel(
                DegradationCheck.ApplyCombatEfficiencyLoss(unit.EfficiencyLevel, unit.ExperienceLevel, rng));

            if (DegradationCheck.RollCombatSupplyLoss(unit.ExperienceLevel, rng))
                unit.ConsumeSupplies(1f);
        }

        private static void Unregister(CombatUnit unit)
        {
            if (unit == null) return;
            GameDataManager.Instance?.UnregisterCombatUnit(unit.UnitID);
        }

        #endregion // Helpers
    }
}
