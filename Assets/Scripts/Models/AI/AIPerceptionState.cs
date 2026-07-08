using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.AI
{
    /// <summary>A live AI-side contact: what the AI legitimately knows about one player unit (Part 3.3).</summary>
    public sealed class ContactRecord
    {
        public string UnitId;
        public SpottedLevel Level;            // AI-side mirror of the §12.2 ladder — NOT CombatUnit.SpottedLevel
        public Position2D LastKnownPos;
        public int LastSeenTurn;
        public UnitClassification Classification; // meaningful at Level1+ (§12.2.2)
        public int ObservedHpPercent;         // coarse strength read at last contact (intel-error handling is the reader's)
        public int EstimatedMpPerTurn;        // drives ghost uncertainty growth after contact is lost
    }

    /// <summary>
    /// A lost contact (Part 3.4): "a tank battalion was here two turns ago." Position uncertainty grows
    /// by the unit's plausible movement each turn; expires after GhostLifetimeTurns.
    /// </summary>
    public sealed class GhostContact
    {
        public string UnitId;
        public Position2D LastKnownPos;
        public int TurnLost;
        public UnitClassification Classification;
        public int EstimatedMpPerTurn;

        /// <summary>Plausible-position radius in hexes at <paramref name="currentTurn"/>.</summary>
        public int UncertaintyRadius(int currentTurn) =>
            Math.Max(0, currentTurn - TurnLost) * Math.Max(1, EstimatedMpPerTurn);
    }

    /// <summary>
    /// The AI's belief store (AI-Design-Supplement Part 3 — Option B, RATIFIED): AI-side SpottedLevels,
    /// live contacts, and decayed ghosts, kept OFF CombatUnit (whose SpottedLevel remains "player's view
    /// of AI units"). The SpottingService symmetric sweep (AI2b) feeds RecordSpot/StepDecay; §12.6 decay
    /// semantics are mirrored exactly (in-range holds; out-of-range Level2+→Level1, Level1→Level0→ghost).
    /// DIAL-READY (Part 12.3): DecayGraceTurns (R2 "remembers longer") and BeliefIsTruth (R3 — consumers
    /// read true state instead; the store keeps running so the dial can be turned back). Serialization
    /// into GameStateSnapshot + SAVE_VERSION bump land with the AI2b wiring. Own-exposure reading (Q9)
    /// needs no code here — the AI reads its own units' CombatUnit.SpottedLevel directly.
    /// </summary>
    public sealed class AIPerceptionState
    {
        private const string CLASS_NAME = nameof(AIPerceptionState);

        private readonly Dictionary<string, ContactRecord> _contacts = new Dictionary<string, ContactRecord>();
        private readonly Dictionary<string, GhostContact> _ghosts = new Dictionary<string, GhostContact>();

        #region Dials (cheat-ladder hooks, Part 12.3)

        /// <summary>Turns a ghost persists before expiring (knob; default 6).</summary>
        public int GhostLifetimeTurns = 6;

        /// <summary>R2 dial: extra out-of-range turns before decay steps (0 = honest §12.6 cadence).</summary>
        public int DecayGraceTurns = 0;

        /// <summary>R3 dial: consumers bypass this store and read true state. The store keeps updating
        /// regardless so difficulty can be changed mid-campaign without a cold start.</summary>
        public bool BeliefIsTruth = false;

        #endregion // Dials

        #region Queries

        public IReadOnlyCollection<ContactRecord> Contacts => _contacts.Values;

        public IReadOnlyCollection<GhostContact> Ghosts => _ghosts.Values;

        public SpottedLevel LevelOf(string unitId) =>
            _contacts.TryGetValue(unitId, out ContactRecord c) ? c.Level : SpottedLevel.Level0;

        public ContactRecord GetContact(string unitId) =>
            _contacts.TryGetValue(unitId, out ContactRecord c) ? c : null;

        public GhostContact GetGhost(string unitId) =>
            _ghosts.TryGetValue(unitId, out GhostContact g) ? g : null;

        #endregion // Queries

        #region Updates

        /// <summary>
        /// Registers a spotting hit (§12.4 incremental +1, cap Level4) or refreshes an existing contact's
        /// position/turn/strength. A ghosted unit re-acquired here loses its ghost (§12.6.6 reset).
        /// </summary>
        public void RecordSpot(
            string unitId, Position2D pos, int currentTurn,
            UnitClassification classification, int observedHpPercent, int estimatedMpPerTurn)
        {
            try
            {
                if (string.IsNullOrEmpty(unitId)) throw new ArgumentException("unitId required", nameof(unitId));

                _ghosts.Remove(unitId);
                if (!_contacts.TryGetValue(unitId, out ContactRecord c))
                {
                    c = new ContactRecord { UnitId = unitId, Level = SpottedLevel.Level0 };
                    _contacts[unitId] = c;
                }

                if (c.Level < SpottedLevel.Level4) c.Level = c.Level + 1;
                c.LastKnownPos = pos;
                c.LastSeenTurn = currentTurn;
                c.Classification = classification;
                c.ObservedHpPercent = observedHpPercent;
                c.EstimatedMpPerTurn = estimatedMpPerTurn;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(RecordSpot), e);
            }
        }

        /// <summary>Refreshes contact position/turn WITHOUT a level increment (still-in-range track updates).</summary>
        public void RefreshContact(string unitId, Position2D pos, int currentTurn)
        {
            if (!_contacts.TryGetValue(unitId, out ContactRecord c)) return;
            c.LastKnownPos = pos;
            c.LastSeenTurn = currentTurn;
        }

        /// <summary>
        /// The §12.6 decay sweep, run once per AI Refresh: contacts in sensor range hold; out-of-range
        /// contacts drop Level2+→Level1 or Level1→Level0 (becoming ghosts), subject to the R2 grace dial.
        /// Expired ghosts are culled. <paramref name="inRangeUnitIds"/> comes from the AI2b sweep.
        /// </summary>
        public void StepDecay(int currentTurn, HashSet<string> inRangeUnitIds)
        {
            try
            {
                if (inRangeUnitIds == null) inRangeUnitIds = new HashSet<string>();

                foreach (ContactRecord c in _contacts.Values.ToList())
                {
                    if (inRangeUnitIds.Contains(c.UnitId)) continue;                 // §12.6.2 — contact held
                    if (currentTurn - c.LastSeenTurn <= DecayGraceTurns) continue;   // R2 dial (0 = honest)

                    if (c.Level >= SpottedLevel.Level2)
                    {
                        c.Level = SpottedLevel.Level1;                               // §12.6.3 — one visible step
                    }
                    else if (c.Level == SpottedLevel.Level1)
                    {
                        _contacts.Remove(c.UnitId);                                  // §12.6.4 — gone dark
                        _ghosts[c.UnitId] = new GhostContact
                        {
                            UnitId = c.UnitId,
                            LastKnownPos = c.LastKnownPos,
                            TurnLost = currentTurn,
                            Classification = c.Classification,
                            EstimatedMpPerTurn = c.EstimatedMpPerTurn,
                        };
                    }
                }

                foreach (GhostContact g in _ghosts.Values.ToList())
                    if (currentTurn - g.TurnLost > GhostLifetimeTurns)
                        _ghosts.Remove(g.UnitId);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(StepDecay), e);
            }
        }

        /// <summary>Removes a destroyed/withdrawn unit from both stores (no ghost — the AI saw it die, or it left play).</summary>
        public void RemoveUnit(string unitId)
        {
            _contacts.Remove(unitId);
            _ghosts.Remove(unitId);
        }

        /// <summary>Full reset (scenario load / ClearAll).</summary>
        public void Clear()
        {
            _contacts.Clear();
            _ghosts.Clear();
        }

        #endregion // Updates
    }
}
