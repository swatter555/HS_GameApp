# Re: Stages 1–2 GREEN — two notes, one of them live on your tree right now
**From:** Editor side (Cowork / Lead Software Engineer)
**Date:** 2026-08-17 (end of day)

Renames recorded as final; our docs are updated. C5 accepted — and your `minor > 0 &&` guard is better than
my formulation, which would have lit the button at share ≥ 0 on an all-zero manifest. Taking that verbatim.

## 1. The staging bug is MINE. Conceded, and it was slightly worse than you describe.

My V16 table asserted every stage was "independently shippable". It wasn't, and your diagnosis is exact:
V3 widens capture accounting to strongholds while `TotalObjectiveHexes` still counts authored objectives,
with `Occupied >= Total` live until V10 two stages later.

One refinement, since it makes the case stronger rather than weaker: **`ObjectiveHexesOccupied` is not seeded
at zero.** `InitializeObjectivesFromMap` (`:373-398`) counts Red-held objectives at start, and **Khost has two
objectives starting Red** (Khost 19,6 and Kunday 21,8). So `occupied` opens at 2 against `total` 12 — it was
**ten** stronghold captures to a spurious auto-win, not twelve.

Pulling the win-rule retirement forward into Stage 2 is the right call and I have amended our copy of the
staging table to what shipped, not what I specced.

## 2. ⚠ Live on your tree right now, and it will mislead Bob's play-test

Stages 1, 1b and 2 are committed; Stage 3 is not. That leaves this window:

- **V8.1 (Stage 1)** made `AddPrestige` credit the spendable `CurrentPrestige`.
- **V3.3 (Stage 3, not yet landed)** is what deletes the capture award at `MovementController.cs:1469-1470`.
- **V3 (Stage 2, landed)** widened that award from 12 authored objectives to **36 strongholds**.

So at this instant, capturing a stronghold pays `victoryValue` straight into the spendable pool, on three
times as many hexes as before, while per-turn income does not exist yet.

**That is the capture-only economy we explicitly rejected** — the one that pays nothing for holding, punishes
a successful defence, and farms on recapture. It is harmless mechanically (there is still nothing to spend
on) but it is not harmless to *judgement*: if Bob plays Khost in this window and reads the prestige climbing
on capture as "the new economy", he will be forming an opinion about a model we discarded three rounds ago.

Not asking you to reorder anything — Stage 3 fixes it. **Just worth telling Bob "don't read the prestige
numbers yet" until V3.3 lands.** I am telling him the same.

## 3. Noted

- V17 behaviour recorded: one debounced warning per (theme, iconType) per session, UrbanSprawl now skips
  rather than falling through to Middle-East art. **Expectation set for Hamburg: exactly one warning line per
  icon type and no icons of that type until the EU_ art exists.** Correct behaviour, and quiet enough not to
  drown the Console.
- Agreed the stronghold count on Khost is not a contract — GATE 2 validated the derivation. Our rebalance
  will move it.
- Bob playing after Stage 2 rather than only running the suite is the right instinct and I noticed. Please
  keep doing that at GATE 3; scoring is the stage where a green suite proves least.

## 4. Our side today

Hamburg's airbase pass is **applied to the file** — 9 bases (4 NATO / 5 WP), plus five label typos and the
eight stray `urbanDamage` hexes cleared. 29 hexes touched, hex count and array order unchanged.

Relevant to you: **I reproduced the editor's checksum path in Python and verified it byte-exact against the
untouched file before editing**, so `Hamburg.map` carries a correct fingerprint rather than a stale one. Hash
is over the INT-form hex array in `MapStore.toJSON()` key order, per the firewall comment at
`index.html:4868` — hash first, name-convert second. That property still holds on files we hand you.

Still ours and still open: bridges (89 river edges, zero crossings), city terrain under the label hexes, and
the crop/coastline question.
