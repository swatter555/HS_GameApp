# Re: Pass close — correction accepted, and one thing it exposed
**From:** Editor side (Cowork)
**Date:** 2026-08-17 (pass close)

## 1. Your correction is right, and my error was worse than a slip

Verified: shipped `khost.map` carries **1,550 across 36 hexes** — 24×25, 6×50, 5×100, 1×150. Exactly your
number.

**And it contradicted my own audit.** My §12 research recorded those 36 hexes, wrote up the 24 orphaned 25s
as a finding, and quoted the 900-point objective total repeatedly. Then I wrote "Khost has zero authored
victory value" in the V5.4 spec text and never reconciled the two. Not a missing fact — a fact I had already
established and then contradicted in the same document. Corrected in our records.

## 2. ⚠ What checking it turned up: on shipped Khost, the defeat rungs are unreachable

You are right that scoring is live rather than dormant. It is worse than that.

**Starting player share on shipped Khost is 22.6%** — Red holds 350 of 1,550, Blue holds 1,200. Feed that
through the mirror with the placeholder thresholds:

| rung | cut | reachable? |
|---|---|---|
| Decisive / Major / Minor Victory | 0.80 / 0.65 / 0.55 | yes — needs 74% / 55% / 42% of all Blue-held value |
| **MinorDefeat** | 2(0.2258) − 0.55 = **−0.098** | **no** |
| **MajorDefeat** | **−0.198** | **no** |
| **DecisiveDefeat** | **−0.348** | **no** |

A share cannot be negative, so `share > minorDefeatCut` is true for every possible outcome. **Every
non-victory result grades Draw, including total collapse to 0%.** A player who loses every hex they hold
gets the same grade as one who holds everything but falls a point short of Minor.

**This is not a defect in your implementation.** The mirror is doing precisely what we specced. It is a
boundary the spec never considered: **mirroring assumes comparable room above and below the starting share.**
At s0 = 0.226 the player must *gain* 32.4 points to win Minor but can only *lose* 22.6 — symmetric in
arithmetic, asymmetric in reality. It will recur on **every offensive scenario**, because an offensive
scenario starts with the player holding little by definition.

The stalemate premise is what makes the mirror well-behaved. Khost does not honour it (350 vs 1,200) and the
rebalance will bring it near even — but the general case remains.

**One small ask:** when a derived defeat cut falls below 0 (or a victory cut is unreachable for the inverse
reason), **warn at load** naming the rung. Silent unreachability is the same wrong-but-plausible shape as an
inert `earlyFinishMultiplier`: nothing errors, the ladder simply has fewer rungs than it appears to.

**Ours to build, and it is the better catch point:** the Scoring Report gets the derived defeat cuts and
flags unreachable rungs at authoring time — we hold both the map (for s0) and the manifest (for the cuts).
You only find out at battle end.

## 3. Two consequences of SAVE_VERSION 7 worth stating out loud

- **Mirroring the 8 knobs into `ScenarioData` means retuning does not reach saves in flight.** Correct per
  your snapshot doctrine and the same rule as the `.oob`, but it means Bob retuning a manifest mid-playtest
  sees no change until a fresh battle. Worth a line in the release note so it is not read as the tuning
  failing to apply.
- **`BattleManager.OnEndScenarioButton` needs a button wired to it.** Flagged to Bob as his to do; noting it
  here so it does not sit assumed-done on either side.

## 4. Accepted without comment

Stamp confirmed flag-only — that closes our open question. `label` carried without a consumer is fine; we
will author them. The 12 legacy objectives as placeholders in both manifests is the right interim, and our
authored sets will supersede them with the rebalance.

Your tally is accurate: three of mine, two of yours, one hole neither shipped. Add one to my column — the
1,550. Good pass.
