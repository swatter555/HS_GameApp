using HammerAndSickle.Controllers;
using HammerAndSickle.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Persistence
{
    /// <summary>
    /// Maps the live <see cref="GameDataManager"/> runtime state to a flat <see cref="GameStateSnapshot"/>
    /// object (and back again).  This is the *only* place that translation logic should live so that
    /// the snapshot format remains stable and version‑controlled.
    /// </summary>
    public static class SnapshotMapper
    {
        private const int CURRENT_SAVE_VERSION = 1;

        // ──────────────────────────────────────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────────────────────────────────────
        /// <summary>
        ///   Take a point‑in‑time copy of every mutable runtime object that should land in a save file.
        ///   The copy is shallow (live references) by design – we only call this immediately before
        ///   serialisation, so no further mutations should occur.
        /// </summary>
        public static GameStateSnapshot ToSnapshot(GameDataManager mgr)
        {
            if (mgr == null) throw new ArgumentNullException(nameof(mgr));

            var snapshot = new GameStateSnapshot
            {
                Campaign = mgr.CurrentCampaignData,           // value‑type graph, safe to reuse
                Scenario = mgr.CurrentScenarioData,           // might be null (e.g. strategic layer only)
                SaveVersion = CURRENT_SAVE_VERSION,

                // Pull fresh enumerations from the manager so we don’t rely on its internal dictionary layout.
                Units = mgr.GetAllCombatUnits().ToDictionary(u => u.UnitID, u => u, StringComparer.Ordinal),
                Leaders = mgr.GetAllLeaders().ToDictionary(l => l.LeaderID, l => l, StringComparer.Ordinal)
            };

            return snapshot;
        }

        /// <summary>
        ///   Wipes <paramref name="mgr"/> and re‑hydrates it from <paramref name="snap"/>.
        ///   A second pass restores object links that cannot be represented by IDs alone.
        /// </summary>
        public static void ApplySnapshot(GameStateSnapshot snap, GameDataManager mgr)
        {
            if (snap == null) throw new ArgumentNullException(nameof(snap));
            if (mgr == null) throw new ArgumentNullException(nameof(mgr));

            // Guard: prevent loading a future save format we can't understand.
            if (snap.SaveVersion > CURRENT_SAVE_VERSION)
                throw new InvalidOperationException($"Save file version {snap.SaveVersion} is newer than supported {CURRENT_SAVE_VERSION}.");

            // 1) Purge all current runtime state.
            mgr.ClearAll();

            // 2) Re-hydrate top-level objects.
            mgr.CurrentCampaignData= snap.Campaign ?? new CampaignData();
            mgr.CurrentScenarioData = snap.Scenario;      // may be null for campaign-only saves

            // 3) Re-populate lookup dictionaries using the manager’s own add/helpers to keep invariants.
            foreach (var unit in snap.Units.Values) mgr.RegisterCombatUnit(unit);
            foreach (var leader in snap.Leaders.Values) mgr.RegisterLeader(leader);

            // 4) Second pass – stitch leader ↔ unit bidirectional links.
            foreach (var unit in mgr.GetAllCombatUnits())
            {
                if (string.IsNullOrWhiteSpace(unit.LeaderID)) continue;

                // Find the leader by ID and update its properties.
                Leader leader = mgr.GetLeader(unit.LeaderID!);
                if (leader != null)
                {
                    leader.UnitID = unit.UnitID;  // assume property names – adjust if different
                    leader.IsAssigned = true;     // mark as assigned to a unit
                }
            }

            // 5) Re-build any transient caches / indices.
            mgr.RebuildTransientCaches();   // helper method to rebuild path‑finding maps, etc. (implement as needed)
        }
    }
}