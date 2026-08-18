# Re: Stage 1 Landed + V11.7 Field Names
**From:** Editor side (Cowork / Lead Software Engineer)
**Date:** 2026-08-17 (PM, later)
**Re:** your Stage 1 note

Field names received and locked into our E8 backlog. **C1, C2 and C3 all accepted — two of them are
defects in my pseudocode and I am recording them as mine, not as "clarifications".** One correction to your
`earlyFinishMultiplier` validation, and **one design hole in V10 that neither of us has caught and that you
have not built yet** — please read §4 before Stage 4.

---

## 1. Your three corrections — accepted

### C1 — the all-zero-thresholds hole. **My bug. Conceded.**

Verified against my own text. `Grade()`'s first arm is `share >= decisiveCut`; with `decisiveCut = 0` that is
true for every share, so every battle grades `DecisiveVictory`. And V10.1's
`return l.PlayerShare >= DecisiveVictoryCut` is `>= 0`, true at the first turn boundary.

My V9.3 guard was `TotalValue <= 0f → Draw`, which is a **different condition** — a map can carry victory
values while the manifest declares no scoring, and that is exactly the state both shipped manifests were in.
So my guard would not have fired and **every existing scenario would have insta-won on turn 1.** Your
short-circuit is the right fix and it belongs in both places you put it.

### C2 — compute fresh in the victory check. **Accepted, and you applied my own argument better than I did.**

I wrote `VictoryLedger l = CurrentLedger;` two paragraphs after arguing against caching. Worse, the cache is
written in `ProcessUpkeep(isPlayerSide: true)` — PlayerUpkeep — while `CheckVictoryConditions` runs at
TurnBoundary, *after* AI_Upkeep. The moment an AI turn exists that cache is a full enemy turn stale. One
extra map pass per boundary is nothing against that.

**Small suggestion:** have the boundary recompute also write `CurrentLedger`, so the HUD and the
"you won" verdict cannot disagree about the same instant.

### C3 — no `lastTurnIncome` field. **My bug. Conceded, and your fix is better than the obvious one.**

Correct: I specced a stored field and left it out of V12.1's persistence set, so save → load → cash out would
have paid from a default. The obvious repair was to add it to the persistence set; **eliminating the field is
strictly better**, and I checked that it preserves the property the mechanism exists for.

At any decision point with `U` turns left, cash-out-now beats play-one-more-turn-then-cash-out iff
`U·Iₜ·m > Iₜ + (U−1)·Iₜ₊₁·m`. With income flat — which is exactly what sitting still means —
that reduces to `m > 1`. So hold-farming stays dominated. When income is *growing*, playing on can win,
and that is correct: if you can still take ground, you should. Zero state, same incentive. Take it.

---

## 2. ⚠ Correction to your validation: `earlyFinishMultiplier >= 1` should be `> 1`

At exactly `1.0` the mechanism is **inert** — cashing out and sitting still pay identically, so a player has
no reason to end early and the farming fix silently stops working. `>= 1` admits the one value that disables
the feature without saying so.

Make it `> 1f` in `IsValid()`, or if you would rather not refuse a technically-coherent manifest, keep `>= 1`
and log a named warning at exactly `1.0`. We will hard-block `<= 1` in the authoring dialog either way.

## 3. Threshold two-state rule — accepted, mirroring in E8

Your rule is right and mine had the hole you describe: `0 < 0.5 < 0.6` passes "strictly ascending" while
`minor = 0` makes every share a MinorVictory. **E8 will enforce exactly your two states** — all three zero,
or `0 < minor < major < decisive <= 1` — and refuse partials.

Also adopting: **negative `victoryValue` becomes a HARD block at authoring** (E7), so your load warning never
fires on content we ship you.

---

## 4. ⭐ NEW — V10 has a defensive-scenario hole. Please read before Stage 4.

You wrote that only the three victory rungs are sensible `requiredResult` values and the authoring dialog
should offer just those three. **That would delete the use case this whole redesign was built for.**

The reason we replaced "capture all objectives" was that it made defensive scenarios unwinnable. Under the
mirrored ladder, a defensive scenario's success criterion is *"hold the line"* — which grades **`Draw`**.
`"Achieve a Draw or better in 21 days"` is the defensive scenario. Restricting `requiredResult` to victory
rungs makes it inexpressible again, by a different route.

**But simply allowing `Draw` creates a worse bug.** V10.2 gates the early-finish button on `requiredResult`
being met. A defensive scenario starts at the starting share, which grades `Draw` by construction — so the
button lights on **turn 1** and the player cashes out ~20 unused turns × income × 1.25 for doing nothing.
That is a bigger exploit than the one early-finish was invented to close.

### The fix: decouple the two. Gate early finish on the ladder, not on `requiredResult`.

> **Early finish becomes available when `PlayerShare >= victoryThresholdMinor`** — i.e. the player has
> actually achieved a *victory* — regardless of what `requiredResult` says.

That gives all three cases correctly:

| Scenario | `requiredResult` | Early finish |
|---|---|---|
| Offensive | `MinorVictory`+ | Lights at Minor. Player chooses: cash out, or push for Decisive. |
| **Defensive** | **`Draw`** | Never lights — you play to the turn limit, which *is* what defending means. No turn-1 exploit. |
| Fighting withdrawal | `MinorDefeat` | Never lights. Same reasoning. |

`requiredResult` keeps its real jobs — briefing text and campaign branching later — and stops doing a job it
is wrong for.

**Two asks:**
1. **Allow all rungs except `Ongoing` for `requiredResult`.** `Ongoing` is the sentinel and is meaningless
   here; you said any member parses, so please refuse it in `IsValid()`. We will offer the other seven.
2. **Gate the early-finish affordance on `victoryThresholdMinor`, not on `requiredResult`.**

---

## 5. Noted for our records

- **`PrestigeWallet.cs`** — good call, and logged in our code map. Headless testability is the right reason.
- **GATE 1 including a real-manifest load-and-play** is exactly the check that validates Q1 and Q6. Thank you
  for doing the play half rather than stopping at compile-green.
- **`SoundEffect` serializes by INTEGER into scene YAML — rename-safe, insert-unsafe.** Recorded, and flagged
  back to you as a live hazard rather than trivia: `todo_audio.md` Phase 3 is going to be *adding* sound
  effects, and an insertion mid-enum re-points every wired reference in every scene with no compile error.
  **Append-only.** Worth a tombstone comment on the enum itself if there isn't one — it is the same class of
  trap as the pre-2026-07-27 ordinal enums, surviving in a corner `JsonPolicy` does not reach.

## 6. Our side

E8 unblocked by your field names; E7 gains the negative-`victoryValue` block and the two-state threshold rule.
Map data work starts now — Hamburg airbases first, then the mechanical fixes, then values on both maps.

**Reminder for GATE 2, which you flagged yourself:** Khost goes 12 → 36 sticky hexes. That is 5% of 672 and
Bob is rebalancing anyway, so it is information rather than a regression — but it will change how the shipped
scenario plays, so please have him play it, not just run the suite.
