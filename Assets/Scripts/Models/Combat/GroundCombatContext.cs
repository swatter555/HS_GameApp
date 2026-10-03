using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Per-order board and knowledge supplied by the turn owner. No scene lookup or persistent reaction state.
    /// The roster contains only units currently on the board; removal also invalidates the owner's occupancy cache.
    /// </summary>
    public sealed class GroundCombatContext
    {
        private readonly Dictionary<string, CombatUnit> _units;
        private readonly Action<CombatUnit> _removeUnit;
        public Side PhasingSide { get; }
        public AIPerceptionState AIPerception { get; }
        public int CurrentTurn { get; }
        public IEnumerable<CombatUnit> Units => _units.Values;

        public GroundCombatContext(Side phasingSide, IEnumerable<CombatUnit> units,
            AIPerceptionState aiPerception, int currentTurn, Action<CombatUnit> removeUnit)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            PhasingSide = phasingSide;
            AIPerception = aiPerception ?? throw new ArgumentNullException(nameof(aiPerception));
            CurrentTurn = currentTurn;
            _removeUnit = removeUnit ?? throw new ArgumentNullException(nameof(removeUnit));
            _units = new Dictionary<string, CombatUnit>(StringComparer.Ordinal);
            foreach (CombatUnit unit in units)
            {
                if (unit == null || string.IsNullOrEmpty(unit.UnitID))
                    throw new ArgumentException("The combat roster contains an invalid unit.", nameof(units));
                if (_units.TryGetValue(unit.UnitID, out CombatUnit existing) && !ReferenceEquals(existing, unit))
                    throw new ArgumentException("The combat roster contains conflicting unit identifiers.", nameof(units));
                _units[unit.UnitID] = unit;
            }
        }

        public bool IsOnMap(CombatUnit unit, HexMap map) =>
            unit != null && !unit.IsDestroyed() && map?.GetHexAt(unit.MapPos) != null &&
            _units.TryGetValue(unit.UnitID, out CombatUnit registered) && ReferenceEquals(unit, registered);

        internal void Remove(CombatUnit unit)
        {
            _removeUnit(unit);
            _units.Remove(unit.UnitID);
        }

        /// <summary>
        /// Player intel and AI intel have different stores. A current passive observer may establish contact
        /// without an omniscient reveal; stale AI contact positions and ghosts cannot authorize a shot.
        /// </summary>
        public bool CanObserve(Side observerSide, CombatUnit target, HexMap map)
        {
            if (!IsOnMap(target, map) || target.Side == observerSide) return false;
            if (observerSide == Side.Player && target.SpottedLevel >= SpottedLevel.Level1) return true;
            ContactRecord contact = observerSide == Side.AI ? AIPerception.GetContact(target.UnitID) : null;
            if (contact != null && contact.Level >= SpottedLevel.Level1 && contact.LastKnownPos == target.MapPos)
                return true;
            return Units.Any(observer => observer.Side == observerSide && IsOnMap(observer, map) &&
                HexMapUtil.GetHexDistance(observer.MapPos, target.MapPos) <= SpottingService.SpottingRangeAgainst(observer, target));
        }

        internal void RevealSupportBattery(CombatUnit battery)
        {
            if (battery.Side == Side.AI)
            {
                battery.SetSpottedLevel((SpottedLevel)Math.Min((int)SpottedLevel.Level5, (int)battery.SpottedLevel + 1));
                return;
            }
            SpottedLevel level = (SpottedLevel)Math.Min((int)SpottedLevel.Level5, (int)AIPerception.LevelOf(battery.UnitID) + 1);
            if (battery.GetAssignedLeader()?.HasUndergroundBunker == true && level > SpottedLevel.Level3)
                level = SpottedLevel.Level3;
            AIPerception.RecordSpot(battery.UnitID, battery.MapPos, CurrentTurn, battery.Classification,
                battery.HitPoints.Max > 0 ? (int)Math.Round(100f * battery.HitPoints.Current / battery.HitPoints.Max) : 0,
                Math.Max(1, (int)battery.MovementPoints.Max), level);
        }
    }
}
