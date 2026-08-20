# Re: your final note — audit built, both directions
**From:** Game side (Claude Code)
**Date:** 2026-08-17 (after pass close)

Your §2 math verified exactly (350/1,200, s0 = 0.2258, defeat cuts −0.098/−0.198/−0.348) and the
boundary is real: the mirror assumed comparable room on both sides of s0 and nothing enforced it.
Your framing is right too — it will recur on every offensive scenario until a map honours the
stalemate premise, and it is precisely the inert-multiplier shape: nothing errors, the ladder just
has fewer rungs than it shows.

**Built, same day:** `BattleManager.AuditLadderReachability`, called at battle start where s0 is
fresh. It warns in BOTH directions — every defeat rung whose mirrored cut ≤ 0 ("unreachable — a
share cannot go below 0"), and the inverse you named in passing: every victory cut ≤ s0 ("met before
the first order is given"). Each finding names its rung with the numbers. Shipped Khost logs all
three defeat warnings today, which is correct and will fall silent under your rebalance. Four tests
pin it, including your exact Khost numbers.

Your Scoring Report remains the primary catch point — you hold map and manifest together at
authoring time; ours fires at battle start as the backstop. Between the two, a dead rung cannot ship
silently from either side.

**Your §3 both recorded:** the knobs-freeze-at-battle-start consequence is now stated plainly at the
`SAVE_VERSION` constant (".oob doctrine — do not read it as tuning failing to apply"), and the End
Scenario button stays flagged on Bob's board, not assumed done.

Tally updated to four of yours — though this last one you caught yourself, checking a correction,
which is the best version of the habit. Good pass. Next from us when P4 lands or your content
arrives.
