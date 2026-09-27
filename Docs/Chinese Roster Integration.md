# Chinese roster integration

This contract records the Chinese additions in `WeaponProfileDB.CreateChineseRosterAdditions` and `CombatUnitDB.CreateChineseForces`. Six profiles and eight formations are implemented (248 profiles / 242 templates overall; 22 / 24 Chinese). Robert approved display-only Type 88 naming and Green Type 59/J-6 plus Raw Type 63 infantry second-line crews on 2026-09-26. Source/tests compile and agent-run Unity verification passes 26/26 Chinese and 870/870 full EditorTests after Robert’s refresh, including all six asset checks and two general clone regressions. See the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>) for verification status and the [consumer packet](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-26 HS Game Chinese roster integration.md>) for adoption.

| Appended profile / value | Authored definition and census |
|---|---|
| `TANK_TYPE62_CH` / 270 | Gen1 light tank, HA−1 / HD−2 / SA−1 / movement+2, LOW_PROFILE and SECOND_LINE_FORMATION. Resolves HA6 / HD4 / SA4 / SD7 / GAD7, movement12, spotting2, ICM0.9. No amphibious or modern fire-control trait. Gen1 tank cost65; turn300 (1963). Existing tank-regiment scale: 1,050 personnel, 80 Type 62, 40 Type 63 APC, 18 Type 83, 18 × 122mm guns, 12 ATGM, 6 legacy Type 53, 6 × 20mm AAA and 18 Strela. |
| `FGT_J6_CH` / 271 | FighterEarly with DF−1 / TS−2 and SECOND_LINE_FORMATION: DF7 / MAN9 / TS8 / SUR6, GA2 / OL6, movement100, ICM0.9. No radar-missile or modern avionics trait. Gen1 fighter price, turn312 (1964 authored anchor), 36 aircraft. |
| `AAA_GEN_CH` / 272 | Shared towed-AAA blueprint plus SECOND_LINE_FORMATION; same gun statistics as other regular nations, ICM0.9. Foot, cost70, turn144. 700 personnel, 36 national AAA and 24 Strela; no embedded carrier. |
| `SAM_HQ2_CH` / 273 | Shared S-75/HQ-2 site-SAM blueprint plus SECOND_LINE_FORMATION: GAT15, indirect range6, movement0, Foot, ICM0.9. Cost145, turn348 (1967). 750 personnel, 18 national HQ-2 and 21 Strela; no embedded carrier. |
| `APC_TYPE63_CH` / 274 | Tracked, amphibious YW531 APC; bare APC statistics, ICM1.0. Gen1 APC price, turn348 (late-1960s authored anchor). Carrier-only census: 90 Type 63 APC, no personnel, tanks or support. |
| `TRK_GEN_CH` / 275 | Shared non-combatant wheeled truck, cost20, ICM1.0, empty census. |

All use the corresponding `CH_Type62`, `CH_J6`, `CH_AAA`, `CH_HQ2`, `CH_Type63` and `CH_Truck` single-sprite artwork. The 27 Chinese PNGs remain in the Chinese atlas. Profile registrations do not change creation status or certify visual acceptance.

These are authored game-counter ratings/censuses, not exact historical tables of organization. The Type 62 is a smaller 85mm light tank with weaker protection than Type 59, not the amphibious Type 63 tank. Type 63 here is the separate troop carrier. J-6 uses a slower, less capable early gun-fighter line than J-7. The existing national formation-quality factor is separate from template crew experience. Carrier and truck profiles must not receive it a second time.

`S75SiteDef(params WeaponTrait[] extraTraits)` now accepts national formation traits. Existing Soviet/Iraqi calls pass none and preserve their definitions. Its existing transport-capability flags remain under the established game contract; this packet does not redesign SAM transport eligibility or missile-guidance traits.

| New regular template | Classification / role | Deployed / Mobile |
|---|---|---|
| `CH_TANK_REGIMENT_TYPE62` | TANK / GroundCombat | `TANK_TYPE62_CH` / NONE |
| `CH_MOT_INFANTRY_REGIMENT_TYPE63` | MOT / GroundCombat | `INF_REG_CH` / `APC_TYPE63_CH` |
| `CH_J6_FIGHTER_SQUADRON` | FGT / AirSuperiority | `FGT_J6_CH` / NONE |
| `CH_TOWED_AAA_REGIMENT` | AAA / AirDefenseArea | `AAA_GEN_CH` / `TRK_GEN_CH` |
| `CH_TYPE83_ARTILLERY_REGIMENT` | ART / GroundCombat | `SPA_TYPE83_CH` / NONE |

These five regular formations retain existing national defaults: China, AI, Trained, Secondary/Small and empty Embarked. Three additional second-line variants reuse their regular equipment and census; only template crew experience differs, as Robert approved:

| Second-line template | Regular equipment source | Experience |
|---|---|---|
| `CH_TANK_REGIMENT_TYPE59_SECOND_LINE` | `CH_TANK_REGIMENT_TYPE59` | Green |
| `CH_MOT_INFANTRY_REGIMENT_TYPE63_SECOND_LINE` | `CH_MOT_INFANTRY_REGIMENT_TYPE63` | Raw |
| `CH_J6_FIGHTER_SQUADRON_SECOND_LINE` | `CH_J6_FIGHTER_SQUADRON` | Green |

The shared deployed profiles retain the national ICM0.9; the Type 63 carrier remains ICM1.0. Existing trained versions remain available. No parallel equipment profiles or new experience mechanics implement the split. The first live tests exposed a pre-existing creation bug: CreateTemplateClone constructed every new unit as Raw. The repair retains the source template’s crew level and initializes XP to its minimum through SetExperienceLevel. This applies to all nations created through CombatUnitDB/CreateTemplateClone, without changing stored scenario/save units. Fresh identity, health and other constructor state remain. CombatUnitIntegrationTests checks all catalog starting levels/XP and verifies that cloning does not copy earned XP or damage.

Existing infantry, airborne, light artillery, heavy artillery and SAM templates change their Soviet truck to `TRK_GEN_CH`. `CH_SAM_REGIMENT` also changes Deployed from the borrowed S-125 to HQ-2 and updates its display name. `INF_REG_CH` personnel increases from 2,200 to the approved 2,900 while retaining all 40 organic Type 59 tanks and existing support quantities. Both existing Type 86 and new Type 63 carriers own only their 90 vehicles; Chinese airborne stays at 1,800 personnel.

The Type 83 profile's stale Type 82/122mm display/comments become Type 83/152mm without changing its existing stats. Its new formation leaves `CH_SP_ARTILLERY_REGIMENT` and PHZ-89 rocket behavior intact. Z-9 replaces stale H-9 display strings and local variables, retaining `HEL_Z9_CH`, `CH_AVIATION_REGIMENT` and the existing six `CH_Z9_Frame0..5` sprites. HQ-7 comments now agree with its already-wheeled movement. Type 53 remains under Robert's earlier explicit instruction; its historical designation/adoption caveat remains in the [accuracy review](<C:/Users/coder/Desktop/Codex Projects/HS Game/PLA Sprite Accuracy Review.md>). Withdrawn Type 69 and Type 95 are not restored.

All 270 earlier enum names/values and 234 earlier template IDs remain. Save version stays11; no JsonPolicy, asset, GUID, scene or scenario/OOB change has been made. Template changes do not migrate stored bays. The infantry census correction applies to every use of `INF_REG_CH`; consumers must adopt the same definitions. Type 88 is a display-only correction: `TANK_TYPE80_CH`, `CH_TANK_REGIMENT_TYPE80`, the Type80 sprite constant/filename and its 80-tank census key remain. The profile/template displays say Type 88. Existing combat values, cost65 and availability turn564 are unchanged; historical availability alignment remains a separate review. Do not create a second Type88 key or assume an identifier/save migration.

Validation is split between [ChineseRosterTests](../Assets/Tests/EditorTests/ChineseRosterTests.cs), the existing [Chinese rating guards](../Assets/Tests/EditorTests/WeaponProfileChineseTests.cs), shared commodity/census guards and the six asset/reachability checks. Static comparisons cover old identifiers, allowed old-profile/template changes, unchanged assets, checklist IDs and byte-identical creation status. Initial live runs passed 24/26 Chinese and 866/868 full EditorTests; both failures were the template-cloning bug described above. After the repair and Robert’s second refresh, agent-run tests pass 26/26 Chinese and 870/870 full EditorTests, with no failures/skips, including the two new clone regressions and all six asset checks. Visual/animation checks, Windows build and consumer export/load remain owed.

Historical references: [US Army ODIN on the Type 59/62 family](https://odin.t2com.army.mil/WEG/Asset/b361384b4259815533a9c215f58c80d8) describes Type 62's smaller 85mm design and thin protection; [ODIN's Type 63 APC entry](https://odin.t2com.army.mil/WEG/Asset/964fc565231a7b7e9e0d8ef75c2c688e) and the [1990 Army armored-family survey](https://asc.army.mil/docs/pubs/alt/archives/1990/Jul-Aug_1990.PDF) describe its tracked amphibious layout and late-1960s introduction. The [Chinese Ministry of Defense HQ-2 history](https://www.mod.gov.cn/gfbw/gfjy_index/js_214151/4853804.html) records its S-75/HQ-1 development lineage and 1967 fielding. [US Air Force museum MiG-19 history](https://www.nationalmuseum.af.mil/Visit/Museum-Exhibits/Fact-Sheets/Display/Article/198064/mikoyan-gurevich-mig-19s/) supports the early gun-fighter lineage. Numerical game ratings are authoring judgments rather than values supplied by these sources.
