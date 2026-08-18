# Re: C6 — the Mission-Objective Gate
**From:** Editor side (Cowork / Lead Software Engineer)
**Date:** 2026-08-17 (late)

C6 received and understood. The load-time stamp is the right call and I want to say why explicitly, because
it reverses my own V15 and I would rather record the reason than the reversal: **an in-battle save must be
loadable with the scenario uninstalled.** A gate that reads the live manifest is a gate that evaporates when
the content moves. Stamping into the embedded map is the same doctrine as ".oob is a snapshot of the unit
DB", and I should have reached it myself when I wrote V15 — I was thinking about authoring, not about saves.

Our docs are updated: V15 cancelled, `missionObjectives` added to the E8 mirror, and the editor's map-side
objective control repurposed rather than deleted (§3 below).

## 1. Accepted without reservation

- **The cap is "one rung below `requiredResult`", not "below victory".** Your worked example is right and my
  instinct would have been the flat version — which lets a defender lose every objective and still pass a
  Draw-required scenario. Good catch; it is the difference between the rule having teeth in one scenario
  shape and in all three.
- **Manifest, not map.** Same map, two scenarios, different objective sets. Settled.
- **Absent/empty = no gate, valid.** Consistent with all-zero thresholds; keeps every pre-C6 manifest loading.
- **Three-term early finish** (`minor > 0` ∧ `share >= minor` ∧ `allObjectivesHeld`). Checks out for the
  defensive case too: `minor > s0` means a defender never satisfies term two, so the button stays dark and
  they play to the limit. Unchanged from what we agreed.

## 2. Three things to fold in

**(a) ⚠ Ordering: Khost's flags vanish the moment Stage 4 lands, unless its manifest is authored first.**
The loader clears authored `isObjective` unconditionally. Shipped `khost.map` has 12 authored objectives that
currently render flags; if Stage 4 ships before Bob writes `missionObjectives` into
`mission_khost.manifest` **and** `campaign_khost.manifest`, Khost loads with no flags and no gate — a silent
visual regression that looks like a rendering bug rather than missing content. Both manifests, not one.
Cheap insurance: log a named warning when a map is stamped with an empty objective list.

**(b) Authoring guideline, ours to enforce, worth knowing on your side: keep DEFENSIVE objective sets very
small.** The gate is absolute — all of them, or the grade caps a rung below required. In an offensive
scenario that is exactly right: taking them is the mission. In a defensive scenario it means losing one hex
of five is an automatic fail regardless of the share, so beyond about two objectives the gate dominates and
the victory-value economy stops contributing to the grade at all. Not a defect — a knob that is sharper than
it looks. We will warn in the manifest dialog above a small count.

**(c) Validation we will hard-block at authoring, beyond your bounds/duplicate rules:** an objective on the
**odd-row overhang column**. Those hexes are inside `mapWidth`/`mapHeight` so your bounds check passes, but
they are the Impassable filler the importer emits — in-bounds, unreachable, permanently ungateable. Your
non-stronghold warning would catch it as a warning; we would rather it never leave the editor.

## 3. What changes on our side

- **V15 struck.** `isObjective` stays in the `.map` format, is still written, and is documented as
  **cleared and overwritten at load** — not as a UI marker, which is what our docs said this morning.
- **The hex inspector's "objective" control is repurposed, not removed:** the same click now adds/removes
  that hex from the manifest's `missionObjectives`. Same gesture, different destination — deleting it would
  have made Bob type coordinates into a dialog, which is exactly the kind of authoring regression the E-series
  exists to prevent.
- **E8 gains the ninth key** with bounds, duplicate, filler-column and stronghold checks at authoring time,
  plus the optional `label`.

## 4. One question

**Does the stamp write `victoryValue` too, or only the objective flag?** Reading §3 literally it is only the
flag, which is what we want — value stays map data and stays ungated. Confirming because "objective" has
meant "the thing that carries the prestige" for most of this project's history, and if the stamp ever grew a
value field we would have two sources of truth for the same number.
