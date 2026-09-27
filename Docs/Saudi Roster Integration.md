# Saudi roster integration

The Saudi package adds fourteen national profiles and fourteen formations (262 profiles / 256 templates overall). Robert directed truck transport for the National Guard and withdrew V-150 on 2026-09-26. All 13 Saudi pictures now have registered formation owners. After Robert refreshed, agent-run Unity tests passed 38/38 Saudi and 908/908 full EditorTests, with no failures, skips or inconclusive results, including all six asset checks. See the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>) and [consumer packet](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-26 HS Game Saudi roster integration.md>) for status.

| Profile / appended value | Definition and authored census |
|---|---|
| `TANK_AMX30_SA` / 276 | Existing French AMX-30 hardware and Gen2 tank price, Saudi art, turn 420 (1973). 1,500 personnel, 60 AMX-30, 36 AMX-10P, 18 M113, 18 AUF1, 18 × 120mm mortars, 12 ATGM, 6 shared M163. |
| `SPSAM_SHAHINE_SA` / 277 | Crotale-family weapon statistics and Gen2 SPSAM price on a **tracked** chassis; French Crotale stays wheeled. Turn504 (1980). 500 personnel, 18 Shahine, 12 M113, 6 national AAA. |
| `FGT_F15_SA` / 278 | Early F-15C: FighterLate + TS2, Sparrow BVR_RADAR_MISSILE, AGILE_AIRFRAME, LOOKDOWN_SHOOTDOWN, RWR, ECM_JAMMER and CHAFF_FLARE. DF14 / MAN14 / TS12 / SUR12, ICM1.1, GA2 / OL6; Gen3 fighter price, turn 528 (1982), 24 aircraft only. |
| `FGT_F5_SA` / 279 | Existing Iranian F-5E hardware/Gen1 fighter price, Saudi art, turn 432. 24 aircraft only. |
| `INF_REG_SA` / 280 | Infantry + RPG_LAW + ATGM_MEDIUM; ICM1.0, Gen1 infantry price. 2,400 personnel, 36 × 81mm mortars, 18 national light guns, 24 ATGM. No embedded tanks or carriers. |
| `INF_GUARD_SA` / 281 | Saudi infantry definition/Gen1 price and own Guard art. 2,400 personnel,36 ×81mm mortars,18 national light guns and 24 ATGM. No organic tanks or armored carriers; transport belongs to the truck bay. |
| `ART_LIGHT_SA` / 282 | Shared light towed gun definition and Gen1 ART price, Foot, turn 144. 600 personnel, 24 × 105mm guns, 6 ATGM. |
| `ART_HEAVY_SA` / 283 | Shared heavy towed gun definition and Gen1 ART price, Foot, turn 144. 700 personnel, 24 × 155mm guns, 6 ATGM. |
| `AAA_GEN_SA` / 284 | Shared towed-AAA definition and Gen1 AAA price, Foot, turn 144. 500 personnel, 24 national guns. |
| `SPA_AUF1_SA` / 285 | French AUF1 gun/chassis stats and Gen2 SPA price, with Saudi art/census and ICM1.0. No French FIRE_DIRECTION_NET factor. Tracked, turn 516. 650 personnel, 24 AUF1, 12 M113, 6 ATGM. |
| `APC_M113_SA` / 286 | Bare tracked APC, Gen1 price, turn 264, ICM1.0. Carrier-only census: 90 own M113. |
| `IFV_AMX10P_SA` / 287 | Existing French AMX-10P hardware/Gen2 IFV price, including light autocannon and amphibious capability. Turn444, ICM1.0. Carrier-only census: 90 own AMX-10P. |
| `TRK_GEN_SA` / 288 | Shared wheeled, non-combatant truck definition/Gen1 price, empty census, national art. |
| `INF_MECH_SA` / 289 | Same Saudi infantry combat definition, price and regulars artwork; separate combined-arms census: 2,400 personnel, 30 AMX-30, 36 × 81mm mortars, 18 national light guns, 24 ATGM. |

Counters and supporting censuses are authored game formations, not national inventory totals or claims of exact historical establishments. Availability turns are roster anchors; F-5/M113 use the existing family anchors, and AMX-10P/AUF1 use mid-1970s/early-1980s Saudi anchors. No new MANPADS identity is invented and no unsupported Stinger/Mistral/Strela census is attached to these Saudi profiles. Aircraft retain the established minimum ground-attack values without acquiring precision-strike traits. The unchanged US F-15 keeps its existing active-radar missile abstraction; the new Saudi line uses the period Sparrow trait.

`Amx30Def`, `CrotaleFamilyDef`, `Amx10pDef` and `F5TigerDef` now supply two national consumers each. `Auf1Def` accepts optional national quality: France passes FIRE_DIRECTION_NET, Saudi Arabia does not. These extractions preserve every prior profile’s resolved definition, census, price and artwork. The existing light/heavy guns, AAA and truck helpers remain the national commodity authority. Saudi infantry hardware is separate from template crew experience.

| New template | Class / role | Deployed / Mobile | Crew |
|---|---|---|---|
| `SA_TANK_BRIGADE_AMX30` | TANK / GroundCombat | AMX-30 / NONE | Green |
| `SA_SHAHINE_SAM_REGIMENT` | SPSAM / AirDefenseArea | Shahine / NONE | Green |
| `SA_F15_FIGHTER_SQUADRON` | FGT / AirSuperiority | F-15C / NONE | Trained |
| `SA_F5_FIGHTER_SQUADRON` | FGT / AirSuperiority | F-5E / NONE | Trained |
| `SA_INFANTRY_BRIGADE` | INF / GroundCombat | Regulars / Saudi truck | Green |
| `SA_NATIONAL_GUARD_BRIGADE` | INF / GroundCombat | National Guard / Saudi truck | Green |
| `SA_MOT_INFANTRY_BRIGADE_M113` | MOT / GroundCombat | Combined-arms infantry / M113 | Green |
| `SA_MECH_INFANTRY_BRIGADE_AMX10P` | MECH / GroundCombat | Combined-arms infantry / AMX-10P | Green |
| `SA_LIGHT_ARTILLERY_REGIMENT` | ART / GroundCombat | Light guns / Saudi truck | Green |
| `SA_HEAVY_ARTILLERY_REGIMENT` | ART / GroundCombat | Heavy guns / Saudi truck | Green |
| `SA_TOWED_AAA_REGIMENT` | AAA / AirDefenseArea | Towed AAA / Saudi truck | Green |
| `SA_AUF1_ARTILLERY_REGIMENT` | ART / GroundCombat | AUF1 / NONE | Green |
| `SA_HAWK_SAM_REGIMENT` | SAM / AirDefenseArea | `SAM_HAWK_US` / Saudi truck | Green |
| `SA_M163_AIR_DEFENSE_REGIMENT` | SPAAA / AirDefenseArea | `SPAAA_M163_US` / NONE | Green |

All use existing `Nationality.SAUD`, AI side, Secondary/Small and empty Embarked. Ground crew experience is authored Green and fighter crew Trained; it does not introduce a new national ICM factor. The foot brigade uses INF_REG_SA without tanks; the M113/AMX-10P brigades use INF_MECH_SA with a 30-tank AMX-30 battalion. Both retain 2,400 personnel and the same support counts. This follows design §10.7.9’s distinct-formation rule without copying the existing IQ/IR/CH shared-base exception or implementing P4 mount transitions. Carrier profiles still count only their own 90 vehicles. Shared Hawk and M163 retain their existing US profiles, art and censuses as explicitly allowed by the approved roster; no Saudi duplicate of either is registered.

Robert directed the Guard to use TRK_GEN_SA in Mobile, with INF_GUARD_SA Deployed and empty Embarked. It remains an INF formation: truck transport does not add armor or change its infantry census. V-150 is withdrawn, not a deferred requirement; no V-150 art, profile or later mounted variant is owed. Saudi helicopter/air-mobile formations remain withdrawn; optional Lightning/Apache, Kuwait expansion and legacy regional-profile retirement are outside this packet.

All 276 prior WeaponType values and 242 prior templates are preserved. Save version remains 11; JsonPolicy, serialized fields, asset GUIDs, atlases, scenes and scenario/OOB files are unchanged. `CreateSaudiProfiles` and `CreateSaudiForces` extend the normal initialization paths. The thirteen national pictures use the existing Regional atlas; Hawk/M163 continue through NATO. The helper still has 347 rows and byte-identical status at revision 361; thirteen ownership rows change, 334 rows remain unchanged, and its tests pass 4/4.

[SaudiRosterTests](../Assets/Tests/EditorTests/SaudiRosterTests.cs) covers national identity/art, legal bays, crew retention, shared hardware parity, tracked Shahine, Saudi AUF1 quality, carrier census, shared Hawk/M163, early F-15 and the no-air-mobile restriction. Commodity and full asset guards supplement it. After Robert refreshed, agent-run Unity tests passed 38/38 Saudi and 908/908 full EditorTests, with no failures, skips or inconclusive results, including all six asset checks. These results include the repaired combined-arms census and the Guard truck/art/census checks. Visual/facing checks, Windows build and consumer export/load remain owed.

Historical context: [US Army ODIN’s Crotale/Shahine entry](https://odin.t2com.army.mil/WEG/Asset/8cc41921d25f6c002af32c93e40934b8) supports Shahine’s AMX-30 carrier and 1980 service anchor. The [1981 Congressional Record’s Saudi F-15 armament account](https://www.govinfo.gov/content/pkg/GPO-CRECB-1981-pt4/pdf/GPO-CRECB-1981-pt4-6-3.pdf) describes Sparrow/Sidewinder air-defense armament; [the USAF Sparrow fact sheet](https://www.af.mil/About-Us/Fact-Sheets/Display/Article/104575/aim-7-sparrow/) identifies Sparrow as semi-active radar guidance. The [Library of Congress Saudi country study](https://tile.loc.gov/storage-services/master/frd/frdcstdy/sa/saudiarabiacount00metz_0/saudiarabiacount00metz_0.pdf) distinguishes the Guard and its wheeled V-150 equipment; its later survey does not supply the game-counter census. Robert’s truck choice intentionally supersedes the historical V-150 plan. Ratings and experience tiers above are game authoring judgments.
