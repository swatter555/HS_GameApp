# Editor Status — authoring is feature-complete
**From:** Editor side (Cowork)
**Date:** 2026-08-17 (end of day)

Short version: **the editor can now author a complete scenario — `.map` + `.oob` + `.manifest` + `.brf` —
for both the standalone and campaign variants of the same map.** Nothing further is needed from you to
unblock authoring. Content work (Hamburg values/objectives, the Khost rebalance) starts next.

## Mirrored against your build

All **nine** manifest keys are in, exact casing, verified against your shipped `mission_khost.manifest` and
`campaign_khost.manifest` rather than against your note: the eight economy fields plus `missionObjectives`.
Enums write by name; `requiredResult` offers seven rungs with `Ongoing` excluded.

## Where we are deliberately stricter than your validation

So you are not surprised by content that is cleaner than your checks require — and so you can leave your
own rules as they are:

- **`earlyFinishMultiplier == 1.0` is refused**, not warned. At exactly 1 the incentive is inert while
  looking configured.
- **Thresholds must be all-zero or a complete ladder.** Partial sets refused.
- **All-zero thresholds with a `requiredResult` above `Draw` is refused** — unreachable by construction.
  You do not check this; we would rather it never reach you.
- **An objective on the odd-row filler column is an ERROR**, where you warn. In-bounds so your check passes,
  but unreachable, so the gate could never close.
- **Negative `victoryValue` is refused**, so your load warning should never fire on our content.

## One design decision worth knowing

**`missionObjectives` is per-variant, not per-map.** The standalone and campaign manifests carry independent
lists. That follows C6's own justification — one map serving scenarios with different objective sets — but it
means when you build the MapLoader stamp, two manifests over the same `.map` will legitimately stamp
different hexes. Nothing for you to change; just do not assume a map has one canonical objective set.

Related: **expect non-integer `victoryValue`.** It is `float` your side and now genuinely fractional-capable
ours (we had been truncating it to int by mistake). Scoring is a share, so fractions are meaningful.

## Waiting on you

- **Stage 3 (income).** Until it lands, capture still pays into the wallet on ~3× the hexes with no per-turn
  income — the capture-only model we rejected. Bob has been told not to read the prestige numbers yet.
- **Stage 4 (scoring, win condition, C6 gate + stamp).**
- ⚠ **The Khost flag reminder still stands:** when the stamp lands, `MapLoader` clears the 12 authored
  objective flags. Unless `missionObjectives` is authored into **both** khost manifests first, Khost renders
  no flags and runs no gate — which reads as a rendering bug rather than missing content. We will supply
  those with the rebalance; land them together.
- **Still open from our C6 reply:** does the stamp write `victoryValue` too, or only the objective flag? We
  read it as the flag only, which is what we want — otherwise two sources of truth for the same number.
