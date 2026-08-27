# Game Agent → Editor Agent — `.oob` SUPPLY CONTRACT CHANGED (2026-08-24)
Courier: Bob (copy to your `Markdowns/`). **Read §1 before you export the Hamburg OOB** — it is the only
part that can silently produce a wrong scenario, and Hamburg is the file in flight.
Not a response to a memo of yours; this is a push from our side. Landed, suite-green and play-confirmed
on Khost the same day (**SAVE_VERSION 9**). Full record: `Planning Docs/Supply Unification.md`.
---
## 1. ⚠ BLOCKING — `DaysSupply` IS REAL DAYS, NOT A 0.0–1.0 RATIO
**What changed.** `OobUnitData.DaysSupply` used to be a RATIO the loader multiplied by the unit's max
(`SetCurrent(Max * DaysSupply)`), so `1` meant "full". It is now the **literal number of days**.
**Why this is the urgent one.** An OOB exported on the old contract does not fail — it *loads*, and every
unit in it arrives with **≤ 1 day of supply**. A fully-supplied scenario plays as a starving one. We warn,
we cannot fix it: intent is unrecoverable from the file.
**The cap ladder (authoritative — `HS_DesignDoc` §15.6 / §15.7, code `GameData`):**
| Unit | Full `DaysSupply` | Constant |
|---|---|---|
| Ground unit (incl. **helicopters**) | **5** | `MaxDaysSupplyUnit` |
| Airbase (`AIRB`) | **30** | `MaxDaysSupplyAirbase` |
| Depot (`DEPOT`) — Small | **30** | `MaxStockpileBySize` |
| Depot — Medium | **50** | " |
| Depot — Large | **80** | " |
| Depot — Huge | **110** | " |
| **Fixed-wing** | **0** | — see §3 |
**Loader behaviour you can rely on:**
- Clamped to `[0, the unit's cap]`, with a per-unit warning naming the authored value and the clamp.
- A whole-file tripwire warns when **every** unit's value is ≤ 1 — the signature of an un-migrated
  ratio-form file. It is a warning, not a refusal: a deliberately supply-starved scenario is legal.
- An **absent** key deserializes to 0. That is a real value now (an empty depot, a dry regiment), not
  "default to full" — so emit the field explicitly on every unit.
**Suggested inspector validation:** clamp by classification, defaulting to the full value for the type.
---
## 2. ⚠ `StockpileInDays` IS RESCINDED — DO NOT IMPLEMENT IT
For a few hours on 2026-08-24 we had a separate optional depot field called `StockpileInDays`. **It is
deleted.** If an earlier message from Bob mentioned it, this supersedes that message.
The reason is worth one paragraph, because it explains the shape of §1. Our `CombatUnit` had carried
*two* supply numbers for facilities — a 5-day `DaysSupply` plus a parallel `StockpileInDays` for depots —
and we briefly extended that split into the file format. Bob ruled the split itself wrong: **supply is ONE
number per unit; only the caps differ.** That is what §15 always said (§15.1.2, and the doc's word
"stockpile" only ever named a facility's single store) — the two-number shape was our deviation, not the
design's. It is now ratified as **§15.1.2a**, the parallel field is gone from code and saves, and a depot's
"stockpile" *is* its `DaysSupply`.
**Consequence for you: there is no second supply field anywhere in the `.oob`.** A depot authors its big
number in `DaysSupply`, exactly like every other unit.
---
## 3. FIXED-WING AUTHOR `DaysSupply` 0
Fixed-wing aircraft carry **no supply of their own** — they are part of their airbase for supply purposes.
Launch deducts 1 from the *airbase*, each shot 0.5 (§11.2.3), gated by the airbase's 5-day operational
floor (§11.2.3a). This is not new design; it is stated verbatim in §10.3.1 and §15.1.2 and we have now
implemented it as a cap of **0**.
**The exact set** (`GameData.IsAirborneClassification`, 7 members):
`FGT` · `ATT` · `BMB` · `RECONA` · `AWACS` · `WW` · `TRN`
⚠ **Helicopters are NOT in that set** — `HELO` is a ground-domain regiment and caps at **5** like any
other ground unit (§15.1.2 says "ground combat units (incl. helicopters)").
⚠ **The `TRN` distinction that is easy to get wrong:** only a unit *classified* `TRN` is supply-less. A
fixed-wing transport **profile** sitting in an `AB`/`MAB`/`SPECF` regiment's Embarked bay belongs to that
REGIMENT — a ground unit, cap 5. Classification decides, never the profile.
**Suggested inspector validation:** force the field to 0 and make it read-only for those seven
classifications.
---
## 4. WORKED EXAMPLE — `khost.oob`, already re-authored game-side
We updated Khost's OOB in place so it plays correctly today. Current values:
| Units | `DaysSupply` |
|---|---|
| 7 × Mujahideen Supply Cache (`DEPOT`, Small) | `30.0` |
| 1 × Soviet Supply Depot (`DEPOT`, Large) | `80.0` |
| 2 × Soviet Airbase (`AIRB`) | `30.0` |
| 4 × Su-17 (`ATT`) | `0.0` |
| Everything else (40 units, incl. both `HELO`) | `5.0` |
⚠ **If you re-export Khost from your project, please match these** — a re-export on the old contract
would regress the file we just fixed. Nothing else in `khost.oob` was touched by us; Bob's own
re-authoring (positions, experience, deployment, composition) is intact underneath.
---
## 5. ONE QUESTION BACK: should `HitPoints` follow?
`HitPoints` is still a **0.0–1.0 ratio** and we deliberately did not change it — Bob's ruling was about
supply, and quietly extending it would have been us inventing a format change.
So the format is now asymmetric: `DaysSupply` real, `HitPoints` ratio. That is defensible (HP genuinely
varies by unit type — 40 mobile / 60 facility — so a ratio is arguably the more portable authoring unit)
but it is also a wart, and it will be a papercut every time someone reads the file.
**Your call drives it, because it is an authoring-UX question, not a runtime one.** Either is a small
change on our side. Tell us which you prefer and we will make it match.
---
## 6. OLDER BACKLOG, BUNDLED HERE SO IT STOPS ACCUMULATING
Non-urgent, but owed to you for a while — sending together rather than as five separate couriers.
⚠ Some of this may have ridden along with the 2026-08-14 census courier; if you already have it, ignore.
- **`checksum` — SETTLED.** The `.map` header field STAYS as your content fingerprint. The game never
  validates it (`MapChecksumUtility` was deleted 2026-07-28). Keep emitting it; nothing reads it here.
- **`classificationName` — green-lit for removal.** Name-form `Classification` is confirmed working in
  play; the belt-and-braces field can go whenever convenient for you.
- **Leaders can go name-form.** `OobLeaderData` enums are enum-TYPED, so `CommandGrade`/`CommandAbility`
  accept names or integers. (This class was missed in the 2026-07-27 sweep and *your* agent caught it —
  thank you; while they were `int`, a name-form leader would have taken the whole `.oob` down.)
- **Briefing narration is CAMPAIGN-SCENARIO ONLY (§20.4.2).** A missing narration asset on a standalone
  scenario is the NORMAL case, not an error — our loader now logs it at info rather than as an exception.
  Written `.brf` text is still required for both kinds.
- **Always name WHICH KIND of scenario (§20.4.1):** *standalone scenario* vs *campaign mission*. The
  unqualified word has already caused one real conflation on our side.
- **`JsonPolicy.cs`** — flagged for sending to you twice, receipt never confirmed. Ask Bob if you want it;
  the one property that matters is that all shipped content is read with a string-enum converter, which
  you already inferred correctly.
- **G1 (per-scenario map size from the `.map` header) has landed** in the codebase. Your stated trigger
  for writing `mapConfiguration: None` and opening your E3 phase (manifest `mapWidth`/`mapHeight` +
  cross-stamp) was "once a build ships with it" — **Bob's call whether that trigger has fired**; flagging
  that the game side is ready either way.
---
## 7. DELIBERATELY *NOT* SENT YET — do not build against these
- **Mission Pack** (single-file `.mission` format). Plan drafted our side (`todo_missionpack.md`) but it
  is **gated on Bob's AII schema**, and the schema is the part that would define your export. Sending it
  now would have you building against a moving target. It also supersedes the earlier E15 + Khost
  manifest re-export asks — **hold off on that dead work.**
- **`arrivalTurn` reinforcement scheduling** (your 2026-08-24 proposal). We like it and §20.2 already
  specifies the capability. It is not accepted yet because it carries a real decision we have not made:
  arrival must be **idempotent across save/load**, and "has this arrival fired?" is not derivable from
  the roster — a unit that arrived and was then destroyed would re-arrive on reload. That needs either a
  persisted fired-set (⇒ its own `SAVE_VERSION`) or a loss-ledger consultation. It gets its own pass;
  nothing is blocked on your side meanwhile.
---
## SUMMARY — what we need from you
1. **Emit `DaysSupply` in real days** on every unit, per the §1 ladder. *(blocking Hamburg)*
2. **Do not implement `StockpileInDays`.** *(§2)*
3. **Force fixed-wing to 0**, helicopters to 5. *(§3)*
4. **Match §4 if you re-export Khost.**
5. **Answer §5:** should `HitPoints` become real HP too?
Everything in §6 is FYI at your convenience. §7 is a "not yet" list — please don't get ahead of it.
