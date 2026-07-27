using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Services;
using System;
using System.Collections.Generic;

// ⚠ From inside HammerAndSickle.Core.UI the bare name `GameData` binds to the CHILD NAMESPACE
// HammerAndSickle.Core.GameData and can never reach the same-named constants class. Same trap that bit
// Prefab_CombatUnitIcon on 2026-07-24 — the alias is the fix.
using GameDataConst = HammerAndSickle.Core.GameData.GameData;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Builds and files HQ dispatches (§24.8.6). One place owns both the ratified body text AND the decision
    /// about whether a given event is worth printing, so call sites stay a single line and the two can never
    /// drift apart.
    ///
    /// THE VERBOSITY MODEL (2026-07-26). The printer competes with a map the player is already watching, so a
    /// dispatch has to carry something the display cannot. Three gates, of which a message needs one:
    ///   A — OUT OF VIEW: happened in the AI turn, or to a unit the player was not watching.
    ///   B — ATTRIBUTION: explains a state change whose cause is not on screen.
    ///   C — ASSESSMENT: a conclusion the player would otherwise have to work out.
    /// A message that only restates a number readable off the icon or unit panel fails all three.
    ///
    /// <see cref="Verbose"/> selects how strictly that is applied:
    ///   VERBOSE — every event files a dispatch, including routine attacks the player ordered and watched.
    ///   CONCISE — report by exception. Defensive reports always file (gate A). The player's OWN attacks file
    ///             only when something notable happened: losses Moderate or worse, the enemy's state changed,
    ///             or the attack cannot continue. A clean hit that moved nobody stays silent.
    /// The concise set is the design intent — a printer that narrates everything teaches the player to ignore
    /// it. Verbose exists so Bob can compare the two in play and, if they feel close, expose the choice.
    /// </summary>
    public static class PrinterDispatch
    {
        private const string CLASS_NAME = nameof(PrinterDispatch);

        #region Configuration

        /// <summary>
        /// True = file every event. False = report by exception (the design intent). Set from
        /// <see cref="PrinterControl"/>'s serialized toggle, and later from a player-facing option.
        /// </summary>
        public static bool Verbose { get; set; }

        #endregion // Configuration

        #region Event Wiring

        private static bool _attached;

        /// <summary>
        /// Subscribes the dispatches that come from BROADCAST events rather than from a specific caller —
        /// weather and spotting. Everything a controller does to a unit it owns (combat, objectives, ambush,
        /// promotion) is filed by a direct call at that site instead.
        ///
        /// Lifetime is owned by <see cref="PrinterControl"/>, which attaches in Initialize and detaches in
        /// OnDestroy. Keeping the subscriptions here rather than in PrinterControl means the whole dispatch
        /// domain — text, gating, and triggers — stays in one file.
        /// </summary>
        public static void Attach()
        {
            try
            {
                if (_attached || EventManager.Instance == null) return;
                _attached = true;

                // See EventManager
                EventManager.Instance.OnWeatherChanged += HandleWeatherChanged;
                EventManager.Instance.OnUnitSpottedLevelChanged += HandleSpottedLevelChanged;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Attach), e);
            }
        }

        public static void Detach()
        {
            try
            {
                if (!_attached) return;
                _attached = false;

                if (EventManager.Instance == null) return;

                EventManager.Instance.OnWeatherChanged -= HandleWeatherChanged;
                EventManager.Instance.OnUnitSpottedLevelChanged -= HandleSpottedLevelChanged;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Detach), e);
            }
        }

        #endregion // Event Wiring

        #region Combat Dispatches

        /// <summary>
        /// Files the dispatches for one resolved DIRECT attack (§7.7.3).
        ///
        /// Handles BOTH sides in one call: the attacker's report when the player owns the attacker, the
        /// defender's when the player owns the defender. Today only the first branch can fire, because
        /// GroundCombatAction is reached only from player input — the defensive branch goes live for free when
        /// the M13 AI turn starts calling the same orchestrator, with no extra wiring at the call site.
        /// </summary>
        /// <param name="contactHex">
        /// Where the engagement HAPPENED — the defender's hex BEFORE resolution. Must be captured at the call
        /// site prior to Execute: a defender that retreats or routs has already moved by the time the outcome
        /// comes back, and reporting its post-retreat hex would put the battle in the wrong place.
        /// </param>
        public static void ReportGroundCombat(
            CombatUnit attacker, CombatUnit defender, Position2D contactHex, GroundCombatOutcome o)
        {
            try
            {
                if (!o.Executed || attacker == null || defender == null) return;

                if (attacker.Side == Side.Player)
                {
                    FileAttackerReport(
                        attacker, contactHex,
                        damageTaken: o.DamageToAttacker,
                        selfDestroyed: o.AttackerDestroyed,
                        enemyOutcome: o.DefenderOutcome,
                        enemyDestroyed: o.DefenderDestroyed,
                        enemyLeftMap: o.DefenderRemovedFromMap);
                }

                if (defender.Side == Side.Player)
                {
                    FileDefenderReport(
                        defender, contactHex,
                        damageTaken: o.DamageToDefender,
                        selfDestroyed: o.DefenderDestroyed,
                        ownOutcome: o.DefenderOutcome,
                        leftMap: o.DefenderRemovedFromMap,
                        underBombardment: false);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportGroundCombat), e);
            }
        }

        /// <summary>
        /// Files the dispatches for one resolved INDIRECT fire mission (§7.13).
        ///
        /// ⚠ Called only AFTER the whole action completes, counter-battery included. The firer normally takes
        /// nothing back, so an early report would say "no losses" and then be contradicted by counter-battery
        /// killing tubes in the same exchange. One dispatch per completed action, never per sub-step.
        /// </summary>
        /// <param name="contactHex">The target's hex BEFORE resolution — see ReportGroundCombat.</param>
        public static void ReportIndirectCombat(
            CombatUnit firer, CombatUnit target, Position2D contactHex, IndirectCombatOutcome o)
        {
            try
            {
                if (!o.Executed || firer == null || target == null) return;

                if (firer.Side == Side.Player)
                {
                    FileAttackerReport(
                        firer, contactHex,
                        damageTaken: o.DamageToFirer,
                        selfDestroyed: o.FirerDestroyed,
                        enemyOutcome: o.TargetOutcome,
                        enemyDestroyed: o.TargetDestroyed,
                        enemyLeftMap: o.TargetRemovedFromMap,
                        bombarding: true,
                        counterBattery: o.CounterBatteryFired);
                }

                if (target.Side == Side.Player)
                {
                    FileDefenderReport(
                        target, contactHex,
                        damageTaken: o.DamageToTarget,
                        selfDestroyed: o.TargetDestroyed,
                        ownOutcome: o.TargetOutcome,
                        leftMap: o.TargetRemovedFromMap,
                        underBombardment: true);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportIndirectCombat), e);
            }
        }

        /// <summary>
        /// Files an ambush dispatch (§6.9). Both directions in one call, like the combat reports — today only
        /// the victim branch fires, since ambushes are checked as the PLAYER moves through enemy trigger
        /// geometry; the sprung-ambush branch goes live when AI units start moving in M13.
        ///
        /// Never gated by verbosity: an ambush is the textbook attribution case (gate B). Something opened fire
        /// from a hex the player had no contact on, and without the dispatch the only feedback is a halted move
        /// and an unexplained hit.
        /// </summary>
        public static void ReportAmbush(CombatUnit ambusher, CombatUnit victim, Position2D hex)
        {
            try
            {
                if (ambusher == null || victim == null) return;

                if (victim.Side == Side.Player)
                {
                    File(
                        new[]
                        {
                            $"Ambushed at {Hex(hex)}.",
                            "Enemy was not detected before contact.",
                            "Taking fire, advance halted."
                        },
                        victim.UnitName, PrinterCategory.Combat);
                }
                else if (ambusher.Side == Side.Player)
                {
                    File(
                        new[]
                        {
                            $"Ambush successful at {Hex(hex)}.",
                            "Enemy column caught in the open.",
                            "They did not return fire."
                        },
                        ambusher.UnitName, PrinterCategory.Combat);
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportAmbush), e);
            }
        }

        #endregion // Combat Dispatches

        #region Objective Dispatches

        /// <summary>
        /// "{place} is in our hands. Objective secured. {n} prestige credited to the front."
        /// Never gated — an objective changing hands is the most consequential event in a scenario.
        /// </summary>
        public static void ReportObjectiveCaptured(Position2D hex, int prestige)
        {
            try
            {
                var lines = new List<string>
                {
                    $"{PlaceName(hex)} is in our hands.",
                    "Objective secured."
                };

                if (prestige > 0)
                    lines.Add($"{prestige} prestige credited to the front.");

                File(lines, PrinterMessage.SourceDivisionalHQ, PrinterCategory.Combat);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportObjectiveCaptured), e);
            }
        }

        /// <summary>
        /// "{place} has fallen. Enemy forces hold the objective. Recapture is a priority."
        /// Gate A — losing an objective happens on the enemy's turn, by definition out of the player's hands.
        /// </summary>
        public static void ReportObjectiveLost(Position2D hex)
        {
            try
            {
                File(
                    new[]
                    {
                        $"{PlaceName(hex)} has fallen.",
                        "Enemy forces hold the objective.",
                        "Recapture is a priority."
                    },
                    PrinterMessage.SourceDivisionalHQ, PrinterCategory.Combat);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportObjectiveLost), e);
            }
        }

        #endregion // Objective Dispatches

        #region Personnel Dispatches

        /// <summary>
        /// "Sustained action has hardened the regiment. Troops are now rated Veteran."
        ///
        /// Gate C. The experience level is on the unit panel, but nothing announces the moment it changes, and
        /// a promotion earned three attacks ago is easy to miss entirely. Filed by the caller that awarded the
        /// experience — CombatUnit is a pure model and never touches EventManager.
        /// </summary>
        public static void ReportUnitHardened(CombatUnit unit)
        {
            try
            {
                if (unit == null || unit.Side != Side.Player) return;

                File(
                    new[]
                    {
                        "Sustained action has hardened the regiment.",
                        $"Troops are now rated {unit.ExperienceLevel}."
                    },
                    unit.UnitName, PrinterCategory.Personnel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ReportUnitHardened), e);
            }
        }

        #endregion // Personnel Dispatches

        #region Event Handlers

        /// <summary>
        /// Weather change (§24.8.6 Weather).
        ///
        /// ⚠ The ratified body text ends "Air operations suspended. Visibility poor." — BOTH of those are
        /// claims about mechanics that DO NOT EXIST yet: weather is single-state Clear in v1 (§3.3.6) and
        /// nothing keys air operations or spotting off it. Printing them would be an outright falsehood, so
        /// the dispatch reports only the change itself until those effects land, at which point the two
        /// sentences go back in.
        /// </summary>
        private static void HandleWeatherChanged(WeatherCondition condition)
        {
            try
            {
                File(
                    new[] { $"{condition} moving into the sector." },
                    PrinterMessage.SourceWeatherSection, PrinterCategory.General);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleWeatherChanged), e);
            }
        }

        /// <summary>
        /// Intel dispatches, driven off rung changes (§24.8.6 Intel, §12.2).
        ///
        /// FIRST CONTACT (Level0 → anything) always files: something appeared that was not there before, which
        /// is new information by definition and cannot be read off a panel the player does not know to click.
        ///
        /// The DEEPER rungs (L2–L5) file in VERBOSE ONLY, deliberately. Since 2026-07-25 a selected enemy shows
        /// its full rung-gated report on the unit panel, so a dispatch reciting posture and equipment counts is
        /// telling the player something they can already click for — it fails all three gates. Verbose keeps
        /// them so the full catalogue can be felt in play before we decide to drop them.
        /// </summary>
        private static void HandleSpottedLevelChanged(CombatUnit unit, SpottedLevel oldLevel, SpottedLevel newLevel)
        {
            try
            {
                if (unit == null || unit.Side == Side.Player) return;
                if (newLevel <= oldLevel) return;   // decay is not news

                // ⚠ Suppress during Deployment (turn 0). BattleManager.SetupBattleManagerData runs a full
                // RecomputeAllSpotting before the first icon draw, so without this the feed opens with a
                // "new contact" burst — one per enemy already in view — before the battle has begun. The
                // starting picture is the situation, not news; contacts made from turn 1 on are.
                if (PrinterMessage.CurrentTurn() <= 0) return;

                if (oldLevel == SpottedLevel.Level0)
                {
                    File(
                        new[]
                        {
                            "New contact.",
                            $"Enemy force sighted at {Hex(unit.MapPos)}.",
                            "No further detail at this range."
                        },
                        PrinterMessage.SourceDivisionalHQ, PrinterCategory.Intel);
                    return;
                }

                if (!Verbose) return;

                IntelReport report = unit.GetIntelReport(newLevel);
                var lines = new List<string>();

                switch (newLevel)
                {
                    case SpottedLevel.Level2:
                        lines.Add($"Enemy identified as the {report.UnitName}.");
                        lines.Add("No further detail at this range.");
                        break;

                    case SpottedLevel.Level3:
                        lines.Add($"Enemy: {report.UnitName}. {report.DeploymentPosition}.");
                        lines.Add("No strength estimate available.");
                        break;

                    default:
                        lines.Add($"Enemy: {report.UnitName}. {report.DeploymentPosition}.");
                        var entries = report.GetEquipmentEntries();
                        if (entries.Count > 0)
                            lines.AddRange(PrinterMessage.FlowIntoColumns(entries, 2));
                        if (newLevel >= SpottedLevel.Level5)
                            lines.Add($"Troops appear {report.UnitExperienceLevel}.");
                        lines.Add("Figures are estimates.");
                        break;
                }

                File(lines, PrinterMessage.SourceDivisionalHQ, PrinterCategory.Intel);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(HandleSpottedLevelChanged), e);
            }
        }

        #endregion // Event Handlers

        #region Report Builders

        /// <summary>
        /// "We are attacking enemy forces at 15,10. Losses are moderate. Enemy is holding."
        ///
        /// A destroyed attacker cannot file its own report, so HQ files the loss instead (§24.8.5a).
        /// </summary>
        private static void FileAttackerReport(
            CombatUnit attacker, Position2D hex, int damageTaken, bool selfDestroyed,
            StandOutcome enemyOutcome, bool enemyDestroyed, bool enemyLeftMap,
            bool bombarding = false, bool counterBattery = false)
        {
            if (selfDestroyed)
            {
                FileUnitLost(attacker, hex);
                return;
            }

            LossBand band = BandFor(damageTaken);
            bool enemyStateChanged = enemyDestroyed || enemyLeftMap || enemyOutcome != StandOutcome.Hold;
            // "We are halting the attack" is only printed when it is TRUE. GetCombatActions() folds in the MP
            // check, so this is the same question the input layer asks before allowing another attack — the
            // dispatch cannot claim the regiment is spent while the player can still order it to fire.
            bool cannotContinue = band >= LossBand.Heavy && attacker.GetCombatActions() < 1f;

            // CONCISE: an attack the player ordered and watched is only worth a slot if it was decisive in some
            // direction, or cost enough to matter. "We hit them, they are still there" is not news.
            if (!Verbose && band < LossBand.Moderate && !enemyStateChanged && !cannotContinue)
                return;

            // "strong enemy forces" is keyed off OUR OWN losses, not the enemy's actual strength — it is the
            // reporting unit's read of the fight, and sourcing it from real strength would leak intel the §12
            // ladder has not granted.
            string foe = band >= LossBand.Heavy ? "strong enemy forces" : "enemy forces";
            string verb = bombarding ? "bombarding" : "attacking";

            var lines = new List<string> { $"We are {verb} {foe} at {Hex(hex)}." };

            // Counter-battery is why an artillery regiment has losses at all, and the guns that fired are
            // usually off screen and unspotted — gate B, so it is worth naming as the cause.
            if (bombarding && counterBattery && band > LossBand.None)
                lines.Add($"Taking counter-battery fire. Losses are {BandText(band)}.");
            else
                lines.Add(LossSentence(band));

            lines.Add(cannotContinue
                ? $"{EnemyClause(enemyOutcome, enemyDestroyed, enemyLeftMap)} We are halting the attack."
                : EnemyClause(enemyOutcome, enemyDestroyed, enemyLeftMap));

            File(lines, attacker.UnitName, PrinterCategory.Combat);
        }

        /// <summary>
        /// "We are under attack at 8,14. Losses are heavy. We are withdrawing to the secondary line."
        ///
        /// ALWAYS files regardless of verbosity — gate A. Being attacked happens on the enemy's turn, which is
        /// the one phase the player does not drive and may not be watching.
        /// </summary>
        private static void FileDefenderReport(
            CombatUnit defender, Position2D hex, int damageTaken, bool selfDestroyed,
            StandOutcome ownOutcome, bool leftMap, bool underBombardment)
        {
            if (selfDestroyed)
            {
                FileUnitLost(defender, hex);
                return;
            }

            LossBand band = BandFor(damageTaken);
            string verb = underBombardment ? "under bombardment" : "under attack";

            var lines = new List<string>
            {
                $"We are {verb} at {Hex(hex)}.",
                LossSentence(band),
                OwnStandClause(ownOutcome, leftMap)
            };

            File(lines, defender.UnitName, PrinterCategory.Combat);
        }

        /// <summary>HQ files for a unit that can no longer file for itself (§24.8.6 Battle).</summary>
        private static void FileUnitLost(CombatUnit unit, Position2D hex)
        {
            File(
                new[] { $"Contact lost with {unit.UnitName} at {Hex(hex)}.", "Unit presumed destroyed." },
                PrinterMessage.SourceDivisionalHQ,
                PrinterCategory.Combat);
        }

        #endregion // Report Builders

        #region Text Helpers

        /// <summary>
        /// What happened to the enemy we attacked. Shatter is reported as breaking rather than as a specific
        /// mechanical fate — the reporting unit sees a formation come apart, not a §7.9.6 branch.
        /// </summary>
        private static string EnemyClause(StandOutcome outcome, bool destroyed, bool leftMap)
        {
            if (destroyed) return "Enemy destroyed, position cleared.";
            if (leftMap) return "Enemy has broken and quit the field.";

            return outcome switch
            {
                StandOutcome.Hold => "Enemy is holding.",
                StandOutcome.Retreat => "Enemy is retreating.",
                StandOutcome.Rout => "Enemy is falling back in disorder.",
                StandOutcome.Shatter => "Enemy formation has broken.",
                _ => "Enemy is holding."
            };
        }

        /// <summary>What happened to US, when we are the ones being attacked.</summary>
        private static string OwnStandClause(StandOutcome outcome, bool leftMap)
        {
            if (leftMap) return "We can no longer hold and are quitting the position.";

            return outcome switch
            {
                StandOutcome.Hold => "We are holding.",
                StandOutcome.Retreat => "Position untenable. Withdrawing to the secondary line.",
                StandOutcome.Rout => "We are falling back in disorder.",
                StandOutcome.Shatter => "The line has broken. Request support.",
                _ => "We are holding."
            };
        }

        private static string LossSentence(LossBand band) =>
            band == LossBand.None ? "We have taken no losses." : $"Losses are {BandText(band)}.";

        private static string BandText(LossBand band) => band switch
        {
            LossBand.VeryLight => "very light",
            LossBand.Light => "light",
            LossBand.Moderate => "moderate",
            LossBand.Heavy => "heavy",
            LossBand.VeryHeavy => "very heavy",
            _ => "negligible"
        };

        /// <summary>
        /// Hit points lost in this ONE exchange, banded for dispatch text (§24.8.6).
        ///
        /// Absolute, not a share of the unit's maximum: the bands describe how much was lost, not how much OF
        /// the unit was lost. Independent of current strength either way — a regiment already down to a third
        /// does not call a 2-point scratch catastrophic just because it has little left.
        /// </summary>
        private static LossBand BandFor(int damage)
        {
            if (damage <= 0) return LossBand.None;

            if (damage <= GameDataConst.LOSS_BAND_VERY_LIGHT_MAX) return LossBand.VeryLight;
            if (damage <= GameDataConst.LOSS_BAND_LIGHT_MAX) return LossBand.Light;
            if (damage <= GameDataConst.LOSS_BAND_MODERATE_MAX) return LossBand.Moderate;
            if (damage <= GameDataConst.LOSS_BAND_HEAVY_MAX) return LossBand.Heavy;

            return LossBand.VeryHeavy;
        }

        private static string Hex(Position2D pos) => $"{pos.IntX},{pos.IntY}";

        /// <summary>
        /// A hex's place name for dispatch text, falling back to its coordinates. Most hexes are unnamed, and
        /// "14,9 is in our hands" still reads correctly where "The objective at 14,9" would be wordier.
        /// </summary>
        private static string PlaceName(Position2D pos)
        {
            try
            {
                string label = GameDataManager.CurrentHexMap?.GetHexAt(pos)?.TileLabel;
                return string.IsNullOrWhiteSpace(label) ? Hex(pos) : label;
            }
            catch
            {
                return Hex(pos);
            }
        }

        private static void File(IReadOnlyList<string> lines, string source, PrinterCategory category)
        {
            var array = new string[lines.Count];
            for (int i = 0; i < lines.Count; i++) array[i] = lines[i];

            EventManager.Instance?.RaisePrinterMessage(new PrinterMessage(array, source, category));
        }

        #endregion // Text Helpers
    }
}
