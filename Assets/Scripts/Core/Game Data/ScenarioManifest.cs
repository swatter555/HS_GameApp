using HammerAndSickle.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace HammerAndSickle.Core.GameData
{
    /// <summary>
    /// One mission objective (C6): a hex the player must control before the scenario's requiredResult
    /// is reachable. Authored PER-VARIANT in the manifest — the same .map legitimately carries
    /// different sets in its standalone and campaign scenarios, which is why this is not map data.
    /// MapLoader clear-then-stamps these onto hex.IsObjective at scenario load; gameplay, UI and
    /// saves read the STAMPED flag, never this list (§7.3 — an in-battle save must load with its
    /// scenario uninstalled).
    /// </summary>
    [Serializable]
    public class MissionObjective
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        /// <summary>Optional display name ("Khost airfield") for briefing/HUD/dispatch surfaces.</summary>
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// Serializable data structure representing a scenario manifest file.
    /// Lists all files required to load a scenario and provides metadata for UI display.
    /// Uses System.Text.Json for serialization/deserialization.
    /// </summary>
    [Serializable]
    public class ScenarioManifest
    {
        #region JSON Properties

        [JsonPropertyName("scenarioId")]
        public string ScenarioId { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("thumbnailFilename")]
        public string ThumbnailFilename { get; set; } = string.Empty;

        [JsonPropertyName("mapFilename")]
        public string MapFilename { get; set; } = string.Empty;

        [JsonPropertyName("oobFilename")]
        public string OobFilename { get; set; } = string.Empty;

        [JsonPropertyName("aiiFilename")]
        public string AiiFilename { get; set; } = string.Empty;

        [JsonPropertyName("briefingFilename")]
        public string BriefingFilename { get; set; } = string.Empty;

        // ⚠ NO `contentVersion` HERE, DELIBERATELY (added then removed 2026-07-28, Bob's call).
        // A per-scenario content version cannot earn its place while content ships INSIDE the build:
        // StreamingAssets is replaced wholesale by a Steam patch, so content and exe move together and a
        // scenario's version could never legitimately disagree with the `gameVersion` already recorded in
        // the save header. Modding is designed out, so there is no foreign content to reconcile either,
        // and the .map header's editor-maintained `checksum` is a better content identity than a
        // hand-kept string — automatic and tamper-evident, where a forgotten version stamp asserts
        // something false. An always-empty field that LOOKS meaningful is the MapChecksumUtility mistake
        // (§7.1); this one was caught before it shipped a claim.
        // ⚠ The CAMPAIGN manifest is a genuinely different case and stays open — see todo.md Phase 2.1.

        [JsonPropertyName("prestigePool")]
        public int PrestigePool { get; set; } = 0;

        [JsonPropertyName("isCampaignScenario")]
        public bool IsCampaignScenario { get; set; } = false;

        [JsonPropertyName("mapTheme")]
        public MapTheme MapTheme { get; set; } = MapTheme.MiddleEast;

        [JsonPropertyName("difficultyLevel")]
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Colonel;

        [JsonPropertyName("maxTurns")]
        public int MaxTurns { get; set; } = 0;

        // Per-scenario point budget the player may spend fielding units during Deployment
        // (§20.1 / §35.4). Replaces the RETIRED maxCoreUnits/maxDeployLand/maxDeployAir fields;
        // the campaign-wide ownership cap (coreForcePointCap) lives in the .cmp, not here.
        [JsonPropertyName("deploymentPointCap")]
        public int DeploymentPointCap { get; set; } = 0;

        [JsonPropertyName("mapWidth")]
        public int MapWidth { get; set; } = 0;

        [JsonPropertyName("mapHeight")]
        public int MapHeight { get; set; } = 0;

        // ────────────────────────────────────────────────────────────────────────────────────────────
        // SCORING + ECONOMY (V11, 2026-08-17 prestige/victory pass — §17.3 / §18.2).
        // A scenario declares the prestige income of held victory value and the share-of-total-value
        // thresholds the BattleResult ladder grades against. ⚠ ALL THREE THRESHOLDS AT 0 IS VALID —
        // "this scenario declares no scoring" — and is what an absent key deserializes to, which is what
        // keeps pre-V11 manifests loading. The scoring path grades an unscored scenario Draw and never
        // ends it early (C1). Values are placeholders until the P4 spend sink exists — nothing shipped
        // before then is balanced (V14 calibration note).
        // ────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Flat prestige per turn, independent of held value — the anti-death-spiral floor (V7.1).</summary>
        [JsonPropertyName("prestigeStipend")]
        public int PrestigeStipend { get; set; } = 0;

        /// <summary>Prestige per turn per point of held victory value.</summary>
        [JsonPropertyName("prestigeIncomeRate")]
        public float PrestigeIncomeRate { get; set; } = 0f;

        /// <summary>Bonus per point of NEW value above the battle's high-water mark (V7.2). 0 disables.</summary>
        [JsonPropertyName("prestigeProgressBonusRate")]
        public float PrestigeProgressBonusRate { get; set; } = 0f;

        /// <summary>Multiplier on unusedTurns × current income when the player ends early (V10.2). ≥ 1.</summary>
        [JsonPropertyName("earlyFinishMultiplier")]
        public float EarlyFinishMultiplier { get; set; } = 1.25f;

        /// <summary>Player share of total victory value required for MinorVictory (0 = no scoring declared).</summary>
        [JsonPropertyName("victoryThresholdMinor")]
        public float VictoryThresholdMinor { get; set; } = 0f;

        /// <summary>... MajorVictory.</summary>
        [JsonPropertyName("victoryThresholdMajor")]
        public float VictoryThresholdMajor { get; set; } = 0f;

        /// <summary>... DecisiveVictory.</summary>
        [JsonPropertyName("victoryThresholdDecisive")]
        public float VictoryThresholdDecisive { get; set; } = 0f;

        /// <summary>
        /// The rung this scenario demands — "achieve a Major Victory within 18 days" (briefing + campaign
        /// branching + the V10.2 early-finish gate). Persisted BY NAME via JsonPolicy.Content, so
        /// <see cref="BattleResult"/> members are rename-frozen from the moment this ships (CLAUDE.md §2.11).
        /// </summary>
        [JsonPropertyName("requiredResult")]
        public BattleResult RequiredResult { get; set; } = BattleResult.MinorVictory;

        /// <summary>
        /// C7 — the FRACTION of <see cref="MissionObjectives"/> that must be Red-controlled for the
        /// gate to be met: required = ceil(total × fraction), clamped to [1, total]. 1.0 (the default)
        /// is C6's original all-or-nothing rule and is what an ABSENT key deserializes to, so every
        /// pre-C7 manifest keeps its exact behaviour. Range (0, 1] — <see cref="IsValid"/> refuses the
        /// rest. A COUNT gate, deliberately not value-weighted (editor C7 §5): cheap objectives are
        /// cheap on purpose, so a poor corner of the map still costs the player a detachment.
        /// ⚠ Mirrored into ScenarioData (V11.6 rationale): the stamped objective FLAGS ride the
        /// embedded map, but the required fraction has no other carrier and the gate is evaluated
        /// every turn boundary on a save that restores without its manifest (§7.3).
        /// </summary>
        [JsonPropertyName("missionObjectiveFraction")]
        public float MissionObjectiveFraction { get; set; } = 1.0f;

        /// <summary>
        /// C6 mission objectives — the victory GATE: the player cannot reach <see cref="RequiredResult"/>
        /// until at least ceil(count × <see cref="MissionObjectiveFraction"/>) of these are
        /// Red-controlled (the grade is capped one rung below it otherwise; C7 made the gate
        /// fractional — at the default fraction 1.0 this is the original ALL-of-them rule).
        /// Absent/empty = no gate declared, VALID (authoring convention says every scenario has at
        /// least one; the schema does not enforce it — that is what keeps pre-C6 manifests loading).
        /// Defensive scenarios put them on the player's side (hold); offensive on the AI side (take).
        /// </summary>
        [JsonPropertyName("missionObjectives")]
        public List<MissionObjective> MissionObjectives { get; set; } = new List<MissionObjective>();

        #endregion // JSON Properties

        #region Constructors

        /// <summary>
        /// Parameterless — System.Text.Json populates via the property setters (the §15.2 "Large" pattern;
        /// this class is 24 serializable properties). ⚠ REPLACED the 16-positional-parameter
        /// [JsonConstructor] on 2026-08-17 (V11.1): under that shape, a property added WITHOUT a matching
        /// constructor parameter was silently left at its default on every load — no exception, no log.
        /// The positional ctor is DELETED rather than kept as a convenience (a second public ctor with no
        /// [JsonConstructor] is exactly what §15.2 rule 3 warns about); programmatic construction uses an
        /// object initializer, which never changes when a field is added.
        /// </summary>
        public ScenarioManifest()
        {
        }

        #endregion // Constructors

        #region Public Methods

        /// <summary>
        /// Validates that the manifest contains all required data.
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ScenarioId))
                return false;

            if (string.IsNullOrWhiteSpace(DisplayName))
                return false;

            if (string.IsNullOrWhiteSpace(MapFilename))
                return false;

            if (string.IsNullOrWhiteSpace(OobFilename))
                return false;

            if (PrestigePool < 0)
                return false;

            /* A manifest must state its map size explicitly (>= 10x10). ⚠ This became a real gate on
             * 2026-08-12 (G3): GetMapDimensions used to assume 32x21 for a manifest with no dimensions, so
             * this check could never fail. Both shipped manifests carry explicit values, so nothing in
             * content breaks — but a legacy manifest without them is now REFUSED rather than silently
             * assigned someone else's geometry. */
            var dims = GetMapDimensions();
            if (dims.x < 10 || dims.y < 10)
                return false;

            // Scoring/economy gates (V11.3). None of these can be tripped by a pre-V11 manifest —
            // every default passes, which is what keeps shipped content loading unchanged (Q6).
            if (PrestigeStipend < 0 || PrestigeIncomeRate < 0f || PrestigeProgressBonusRate < 0f)
                return false;

            /* < 1 is refused (cashing out would pay WORSE than sitting still — backwards). Exactly 1.0
             * LOADS BUT WARNS (editor note 2026-08-17 PM): at par the anti-farm dominance is no longer
             * strict, so the mechanism is inert without saying so. It stays loadable because a par-payout
             * manifest is coherent, refusal here makes the scenario silently vanish from the menu, and
             * the editor hard-blocks <= 1 at authoring anyway — this warning can only fire on hand-edited
             * content. */
            if (EarlyFinishMultiplier < 1f)
                return false;

            if (EarlyFinishMultiplier == 1f)
                UnityEngine.Debug.LogWarning(
                    $"ScenarioManifest '{ScenarioId}': earlyFinishMultiplier is exactly 1.0 — early finish " +
                    "pays par with sitting still, so the V10.2 farming disincentive is inert.");

            /* Ongoing is the in-progress sentinel, not an outcome — meaningless as a demand. Every OTHER
             * rung is legal, and that breadth is load-bearing (editor note 2026-08-17 PM): Draw IS the
             * defensive scenario ("hold the line for 21 days") and the defeat rungs are fighting
             * withdrawals. Restricting to victory rungs would re-create the unwinnable-defensive-scenario
             * hole this pass exists to close. ⚠ requiredResult does NOT gate early finish — that gate is
             * the threshold ladder (todo_prestige Stage 4), or a defensive scenario would light the
             * cash-out button on turn 1. */
            if (RequiredResult == BattleResult.Ongoing)
                return false;

            /* Thresholds: either ALL zero — "this scenario declares no scoring", the pre-V11 default and
             * a VALID state (refusing it would stop both shipped manifests loading) — or a full strictly
             * ascending ladder in (0, 1]. A PARTIAL declaration (some zero, some not) is refused: a zero
             * minor cut would make any share a MinorVictory, the same degenerate shape the C1 guard
             * exists to keep out of the scoring path. */
            bool declaresScoring = VictoryThresholdMinor != 0f || VictoryThresholdMajor != 0f ||
                                   VictoryThresholdDecisive != 0f;
            if (declaresScoring)
            {
                if (VictoryThresholdMinor <= 0f || VictoryThresholdDecisive > 1f)
                    return false;

                if (VictoryThresholdMinor >= VictoryThresholdMajor ||
                    VictoryThresholdMajor >= VictoryThresholdDecisive)
                    return false;
            }

            /* C7 gate fraction: (0, 1] only. A zero fraction is a gate that is always met, which is
             * indistinguishable from declaring no objectives and is better said that way; above 1 is
             * unsatisfiable. Both are authoring errors, not content states. ⚠ Deliberately NOT refused:
             * an empty objective list WITH a non-default fraction — meaningless but harmless
             * (ceil(0 × f) == 0, gate vacuously met), and refusing it would make a manifest vanish
             * from the menu over a field nobody set on purpose. The editor hard-blocks it at
             * authoring instead (E15). */
            if (MissionObjectiveFraction <= 0f || MissionObjectiveFraction > 1f)
                return false;

            /* C6 mission objectives: absent/empty = no gate, valid. Entries must be unique and inside
             * the declared rectangle. The AUTHORITATIVE checks live in the MapLoader stamp — it knows
             * the real map (missing hex → refused load; non-stronghold → loud warning); this only
             * knows the rectangle. */
            if (MissionObjectives != null && MissionObjectives.Count > 0)
            {
                var seen = new HashSet<(int, int)>();
                foreach (var obj in MissionObjectives)
                {
                    if (obj == null)
                        return false;

                    if (obj.X < 0 || obj.X >= dims.x || obj.Y < 0 || obj.Y >= dims.y)
                        return false;

                    if (!seen.Add((obj.X, obj.Y)))
                        return false;   // duplicate objective is authoring garbage
                }
            }

            return true;
        }

        /// <summary>
        /// The manifest's declared map dimensions, or <see cref="UnityEngine.Vector2Int.zero"/> when it does
        /// not declare any. Used by consumers that need a size BEFORE the `.map` is parsed (menu display,
        /// pre-flight validation) and as a cross-check against the header at load.
        /// </summary>
        /// <remarks>
        /// ⚠ THE `.map` HEADER IS AUTHORITATIVE, NOT THIS. This is a convenience copy; `MapLoader` warns
        /// loudly if the two disagree and uses the header, because that is the file whose geometry is
        /// actually being loaded.
        /// ⚠ RETURNS ZERO RATHER THAN ASSUMING 32x21 (G3, 2026-08-12). It used to fall back to the "Small"
        /// size, which is the same silent-wrong-answer pattern that made every non-32x21 map load truncated
        /// — a manifest that does not state its size would have confidently reported someone else's.
        /// Zero fails <see cref="IsValid"/>, so such a manifest is refused with a reason instead.
        /// </remarks>
        public UnityEngine.Vector2Int GetMapDimensions()
        {
            if (MapWidth >= 10 && MapHeight >= 10)
                return new UnityEngine.Vector2Int(MapWidth, MapHeight);

            return UnityEngine.Vector2Int.zero;
        }

        /// <summary>
        /// Gets the path in the assets/resources folder to the thumbnail image. Must use resource load.
        /// </summary>
        public string GetThumbnailPath()
        {
            if (string.IsNullOrWhiteSpace(ThumbnailFilename))
                return string.Empty;
            
            return Path.Combine(AppService.ScenarioThumbnailPath, ThumbnailFilename);
        }

        // ────────────────────────────────────────────────────────────────────────────────────────────
        // CONTENT PATHS (rewritten 2026-07-27, content pipeline Phase 1).
        //
        // ⚠ This REPLACED two parallel method families — GetMapFilePath() resolving to Documents/My Games
        // and GetMapFilePath_GDP() resolving to Assets/Generated Data — which MapLoader and BattleManager
        // chose between on IsCampaignScenario. That welded a GAMEPLAY concept (campaign vs standalone) to
        // a STORAGE one (which folder), so a campaign could not be standalone-tested and the two copies
        // silently diverged. Now there is one family, and it resolves against the folder the manifest was
        // loaded from, so where a scenario lives is nobody's business but the loader's.
        // ────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Directory this manifest was loaded from — its scenario's self-contained content folder.
        /// Set by the loader at read time; transient, never serialized (the manifest must not carry a
        /// machine-specific absolute path into a save or into shipped content).
        /// </summary>
        [JsonIgnore]
        public string ContentRoot { get; set; } = string.Empty;

        /// <summary>Full path to the map file, or empty if unnamed.</summary>
        public string GetMapFilePath() => ResolveContentFile(MapFilename);

        /// <summary>Full path to the OOB file, or empty if unnamed.</summary>
        public string GetOobFilePath() => ResolveContentFile(OobFilename);

        /// <summary>
        /// Full path to the AII (AI hints) file, or empty if unnamed.
        /// ⚠ No .aii files exist yet — the AI pass will author them (Bob, 2026-07-27). Callers must treat
        /// a missing AII as a clean no-op, NOT an error.
        /// </summary>
        public string GetAiiFilePath() => ResolveContentFile(AiiFilename);

        /// <summary>Full path to the briefing file, or empty if unnamed.</summary>
        public string GetBriefingFilePath() => ResolveContentFile(BriefingFilename);

        /// <summary>
        /// Resolves a filename against this scenario's own content folder.
        /// Returns empty for an unnamed file so callers can treat "not specified" and "not found" alike.
        /// </summary>
        private string ResolveContentFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(ContentRoot))
                return string.Empty;

            return Path.Combine(ContentRoot, filename);
        }

        #endregion // Public Methods
    }
}
