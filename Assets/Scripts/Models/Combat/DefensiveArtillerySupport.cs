using System;
using System.Linq;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>A single reaction belonging to one committed direct attack; never an initiating order.</summary>
    public struct DefensiveSupportOutcome
    {
        public CombatUnit Battery;
        public Position2D TargetHex;
        public IndirectCombatOutcome Combat;
        public bool Selected => Battery != null;
        public bool Fired => Combat.ShotResolved;
    }

    public static class DefensiveArtillerySupport
    {
        public static bool IsGroundUnit(CombatUnit unit) => unit != null && !unit.IsBase &&
            unit.OccupiesDomain == Domain.Ground && !MovementModeService.IsAirborneNow(unit);

        /// <summary>Silent eligibility query: never spends, reveals, rolls, or prints a hidden battery's identity.</summary>
        public static bool IsEligible(CombatUnit battery, CombatUnit attacker, CombatUnit defender,
            HexMap map, GroundCombatContext context)
        {
            if (context == null || !IsGroundUnit(attacker) || !IsGroundUnit(defender) ||
                attacker.Side != context.PhasingSide || defender.Side == context.PhasingSide ||
                CombatResolver.IsIndirectFireClass(attacker.Classification) ||
                !context.IsOnMap(attacker, map) || !context.IsOnMap(defender, map)) return false;
            return battery != null && !ReferenceEquals(battery, defender) && battery.UnitID != defender.UnitID &&
                battery.Side == defender.Side && context.IsOnMap(battery, map) &&
                battery.DeploymentPosition != DeploymentPosition.Embarked && IsGroundUnit(battery) &&
                CombatResolver.IsIndirectFireClass(battery.Classification) &&
                battery.CanPerformOpportunityAction() &&
                CombatResolver.IsInIndirectRange(battery, attacker.MapPos) && context.CanObserve(battery.Side, attacker, map);
        }

        /// <summary>One uniform dN over unique, fully eligible batteries in stable identifier order. No retry.</summary>
        public static CombatUnit Select(CombatUnit attacker, CombatUnit defender, HexMap map,
            GroundCombatContext context, ICombatRandom rng)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            CombatUnit[] candidates = context.Units.Where(unit => IsEligible(unit, attacker, defender, map, context))
                .OrderBy(unit => unit.UnitID, StringComparer.Ordinal).ToArray();
            if (candidates.Length == 0) return null;
            if (candidates.Length == 1) return candidates[0];
            int roll = rng.RollDie(candidates.Length);
            if (roll < 1 || roll > candidates.Length) throw new InvalidOperationException("Invalid support-selection die result.");
            return candidates[roll - 1];
        }
    }
}
