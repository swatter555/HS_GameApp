using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.AI;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    public readonly struct SigintContactChange
    {
        public readonly CombatUnit Unit;
        public readonly SpottedLevel OldLevel;
        public readonly SpottedLevel NewLevel;
        public SigintContactChange(CombatUnit unit, SpottedLevel oldLevel, SpottedLevel newLevel)
        { Unit = unit; OldLevel = oldLevel; NewLevel = newLevel; }
    }

    public sealed class SigintSweepResult
    {
        public bool Executed { get; internal set; }
        public string RefusalReason { get; internal set; }
        public IReadOnlyList<SigintContactChange> Changes => _changes;
        internal readonly List<SigintContactChange> _changes = new List<SigintContactChange>();
    }

    /// <summary>Ordinary HQ SIGINT (§12.7). Same payment, chance and radio-silence filter for either side.</summary>
    public static class SigintAction
    {
        public static SigintSweepResult Execute(CombatUnit hq, IEnumerable<CombatUnit> population,
            ICombatRandom rng, AIPerceptionState aiPerception = null, int currentTurn = 0)
        {
            var result = new SigintSweepResult();
            try
            {
                if (hq == null || population == null || rng == null || (hq.Side == Side.AI && aiPerception == null))
                {
                    result.RefusalReason = "Intelligence sweep context is unavailable.";
                    return result;
                }
                if (hq.GetIntelActions() < 1f)
                {
                    result.RefusalReason = "No eligible HQ intelligence sweep remains.";
                    return result;
                }
                if (!hq.PerformIntelAction())
                {
                    result.RefusalReason = "Intelligence sweep payment failed.";
                    return result;
                }
                result.Executed = true;
                // Independent map-wide rolls. Empty/silent maps still spend the sweep; no hidden contact count leaks.
                foreach (var target in population)
                {
                    if (target == null || target.Side == hq.Side || target.IsDestroyed() || !target.EmittedDuringLastOwnTurn) continue;
                    if (rng.RollDie(100) > GameData.HQ_SIGINT_SUCCESS_PERCENT) continue;
                    var oldLevel = hq.Side == Side.Player ? target.SpottedLevel : aiPerception.LevelOf(target.UnitID);
                    if (oldLevel >= SpottedLevel.Level3) continue; // Never lower a stronger observation.
                    var newLevel = oldLevel + 1;
                    if (hq.Side == Side.Player) target.SetSpottedLevel(newLevel);
                    else aiPerception.RecordSpot(target.UnitID, target.MapPos, currentTurn, target.Classification,
                        target.HitPoints.Max <= 0f ? 0 : (int)Math.Round(target.HitPoints.Current / target.HitPoints.Max * 100f),
                        Math.Max(1, (int)Math.Round(target.MovementPoints.Max)), newLevel);
                    result._changes.Add(new SigintContactChange(target, oldLevel, newLevel));
                }
                return result;
            }
            catch (Exception e)
            {
                AppService.HandleException(nameof(SigintAction), nameof(Execute), e);
                result.RefusalReason = "Intelligence sweep could not complete.";
                return result;
            }
        }
    }
}
