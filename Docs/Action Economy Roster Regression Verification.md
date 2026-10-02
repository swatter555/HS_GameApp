# Action economy roster regression verification - 2026-10-02

Robert reported two failed roster suites after an in-game check. He reports the other behavior exercised seems to work and artillery feels more effective; balance acceptance is still under evaluation. Air transports remain unwired/untested; he plans to wire their buttons tomorrow. This is user-reported partial Play acceptance, not exhaustive scenario/UI/build verification.

## Diagnosed failures and fix

Current-source managed reproduction ran every assertion case in both suites and reproduced exactly one failure in each:

| Child test | Failed assertion | Approved expectation |
|---|---|---|
| `ChineseRosterTests.ChineseDisplayNames_KeepApprovedIdentifiersArtAndUnitTypes` | Type 83 soft attack expected 9, actual 10 | 10 |
| `GenericNatoRosterTests.M109_UsesConventionalNatoFires_WithoutUsPrecisionOrForeignSupportEquipment` | NATO M109 soft attack expected 10, actual 11 | 11 |

Both expectations were missed in the artillery-family soft attack +1 change. `FamilyArchetypes.Artillery` now supplies SA10; Type 83 has no SA delta and retains its approved 2S1-equivalent ratings. `NatoM109Def` retains its +1 SA profile adjustment, producing 11. The [complete catalog comparison](<Weapon Profile Movement Audit.tsv>) already records Type 83 9 -> 10 and NATO M109 10 -> 11. Robert's approved design says every ART/SPA/ROC/Scud profile gains one SA while preserving profile adjustments.

The fix updates only those two exact numeric assertions and gives each an explanation. Other identity, artwork, movement, capabilities, census, price, quality and formation assertions are unchanged. No production code, scenario, scene, prefab, asset, metadata, dependency or shared contract changes.

## Verification and limits

Before the fix: Chinese **26 passed / 1 failed**, generic NATO **34 passed / 1 failed**. After the fix: Chinese **27/27 passed**, generic NATO **35/35 passed**; broader regression selection **276 passed / 0 failed** (the earlier 214 managed cases plus all 62 roster cases). Main and EditorTests compile with zero diagnostics. The full 264-profile runtime catalog remains identical to the audited gameplay milestone.

The managed harness initializes the actual WeaponProfileDB and CombatUnitDB and executes every `[Test]`/`[TestCase]` assertion in both roster suites. It runs outside Unity and omits BaseTestFixture lifecycle/native APIs; these counts are not Unity Test Runner results. The one NATO native asset assertion remains explicitly excluded from the broader selection. The available Editor.log/Editor-prev.log are older and contain no relevant child-test failures; no current native result XML was found in permitted project Temp/Logs locations. The existing Editor discovery denial was respected without bypass.

Publication verification extracts every Assets C# compiler input from the exact focused commit into isolated temporary outputs and repeats compilation, both roster suites, the broader selection and catalog equality. References/generated cache are local Unity **6000.6.3f1 / URP 17.6.0**; committed pins remain **6000.2.6f2 / URP 17.2.0**. This is not a native clean checkout at the committed pins. No CI workflow exists.

Native acceptance still owed: let Unity recompile; rerun both complete roster suites (and full EditorTests as available), save child results/totals, then exercise the separate air buttons once authored. No new build, native tests or air-transport playtest was run by the agent.

Evidence is retained in `C:/Users/coder/AppData/Local/Temp/hs-roster-regressions-20261002`: baseline hashes/status, original test sources, before/after managed output, compiler responses and final committed-source output. Initial gameplay commits: `1dbc29b34bbc4aac621ee4c8b81522e777b8146a` and `67df29ace515b35d0f19c2a5627a962fd9be2de4`. Only the two roster test files and this verification record belong to the correction; all unrelated work remains unstaged.
