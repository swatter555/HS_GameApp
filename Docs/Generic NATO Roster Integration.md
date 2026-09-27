# Generic NATO roster integration

This contract records `WeaponProfileDB.CreateGenericNatoRosterAdditions`, the national census corrections in `CreateLowlandsProfiles`, and changes to `CombatUnitDB.CreateDutchForces`, `CreateBelgianForces` and `CreateDanishForces`. Seven profiles and eleven formations bring the catalog to 234 profiles / 228 templates. The shared NATO plus Dutch/Belgian/Danish subset has 17 profiles and 27 templates. [GenericNatoRosterTests](../Assets/Tests/EditorTests/GenericNatoRosterTests.cs) guards these contracts; verification and acceptance status belong in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>).

| New profile / enum value | Definition and authored census |
|---|---|
| `IFV_YPR765_NATO` / 255 | Tracked IFV with 25mm `AUTOCANNON_LIGHT`; HA4 / HD4 / SA9 / SD7 / GAD7, movement10, ICM1.0. Gen1 IFV cost55. Carrier-only census: 102 YPR/AIFV, no personnel, tanks or support. |
| `SPA_M109_NATO` / 256 | Conventional NATO M109 with `SELF_PROPELLED` and `FIRE_DIRECTION_NET`, without the US-only smart-munition trait. HA5 / HD7 / SA10 / SD7 / GAD7, movement10, indirect range5, ICM1.05. Cost190; 950 personnel, 54 guns, 12 NATO M113. |
| `RCN_M113CV_NATO` / 257 | Dutch 25mm tracked scout; Recon with `AUTOCANNON_LIGHT` and `AMPHIBIOUS`. HA2 / HD5 / SA6 / SD9 / GAD7, movement10, spotting3, Soft target under the existing skirmisher contract. Cost105; 600 personnel, 36 scouts, 12 NATO M113, 8 ATGM, 6 Stinger. |
| `SPAAA_PRTL_NATO` / 258 | Tracked radar-directed twin-35mm gun using the established Gepard combat definition and separate Dutch radar art. HA4 / HD6 / SA9 / SD8 / GAD11 / GAT13, movement10, indirect range4. Cost255; 1,100 personnel, 18 PRTL, 24 YPR, 12 M113 C&V. |
| `TANK_CENTURION_NATO` / 259 | Robert approved the 105mm Mk 5/2 before thermal/laser modernization, 60 tanks and cost55 on 2026-09-26. Gen1 with HA+1 / HD+1 / SA+1 / movement−2 resolves HA8 / HD6 / SA6 / SD6 / GAD7, movement8, spotting2, ICM1.0. Uses the existing Danish brigade support quantities. |
| `AAA_GEN_NATO` / 260 | Shared regular towed-AAA definition, cost70; 500 personnel, 18 guns and no embedded carriers or nation-specific MANPADS. |
| `FGT_F16_NATO` / 261 | Established US F-16 multirole combat definition, NATO art and 36-aircraft census. Cost270; DF12 / MAN13 / TS11 / SUR9, GA9 / OL6 and ICM1.10. |

These are game-counter censuses, not exact historical tables of organization. The shared M109 uses the existing generic towed-artillery scale with a small M113 support slice; it does not borrow US aircraft, recon or infantry equipment. PRTL follows the established radar-AAA counter scale with Dutch support. Centurion follows the Danish Leopard brigade: 1,600 personnel, 60 tanks, 48 M113, 10 existing British recon proxies, 24 ATGM, 12 Stinger, 12 NATO M109 and 12 mortars. Its early platform availability does not date every item in that blended support census.

`NatoM109Def()` is shared with the unchanged German/British M109 definitions; `RadarSpAaaDef()` preserves Gepard; `F16MultiroleDef()` preserves the US F-16. The extractions change no existing combat statistics, prices, availability or censuses. The Centurion exception is centralized as `GameData.PRESTIGE_CENTURION_NATO`: existing Leopard 1 prices remain 65, so the approved cheaper tank cannot use the same Gen1+TANK formula.

| Existing template, stable key | Corrected assignment |
|---|---|
| `NL_ARMOURED_INFANTRY_BRIGADE` | Mobile: `APC_M113_NATO` → `IFV_YPR765_NATO` |
| `BE_MECH_INFANTRY_BRIGADE` | Mobile: `APC_M113_NATO` → `IFV_YPR765_NATO` |
| `NL_RECON_UNIT` | Deployed: `RCN_FV105_UK` → `RCN_M113CV_NATO`; display name identifies M113 C&V |
| `NL_AIR_DEFENSE_REGIMENT` | Deployed: `SPAAA_GEPARD_GE` → `SPAAA_PRTL_NATO` |
| `NL_F16_FIGHTER_SQUADRON` | Deployed: `FGT_F16_US` → `FGT_F16_NATO` |

The three existing national Leopard and three mechanized-infantry profiles retain all quantities and combat definitions. Their M109 census key becomes `SPA_M109_NATO`. Dutch/Belgian armored-brigade carriers become YPR/AIFV at the original 60/55 counts; Dutch armored recon becomes M113 C&V at the original 12 count. These key changes alter reporting categories where an IFV replaces the old APC token, without changing the total carrier count. Danish M113 quantities, national infantry bases and experience remain intact.

| New template | Classification / role | Deployed / Mobile |
|---|---|---|
| `NL_ARMOURED_INFANTRY_BRIGADE_M113` | MECH / GroundCombat | `INF_MECH_NL` / `APC_M113_NATO` |
| `BE_MECH_INFANTRY_BRIGADE_M113` | MECH / GroundCombat | `INF_MECH_BE` / `APC_M113_NATO` |
| `NL_M109_ARTILLERY_REGIMENT`, `BE_M109_ARTILLERY_REGIMENT`, `DK_M109_ARTILLERY_REGIMENT` | SPA / GroundCombatIndirect | `SPA_M109_NATO` / none |
| `NL_LIGHT_ARTILLERY_REGIMENT`, `BE_LIGHT_ARTILLERY_REGIMENT`, `DK_LIGHT_ARTILLERY_REGIMENT` | ART / GroundCombatIndirect | `ART_LIGHT_NATO` / `TRK_GEN_NATO` |
| `NL_TOWED_AAA_REGIMENT`, `BE_TOWED_AAA_REGIMENT` | AAA / AirDefenseArea | `AAA_GEN_NATO` / `TRK_GEN_NATO` |
| `DK_CENTURION_BRIGADE` | TANK / GroundCombat | `TANK_CENTURION_NATO` / none |

Every Embarked bay remains empty. All new formations use Side.AI; Dutch/Danish formations remain Experienced and Belgian formations Trained. Nationality.NE is Netherlands, BE is Belgium and DE is Denmark; Germany uses FRG. The catalog now has 11 Dutch, 9 Belgian and 7 Danish templates. Existing heavy towed formations stay available, and M113 alternatives preserve the prior carrier choice. The new YPR carrier contributes only its own 102 vehicles when the bay census is summed.

M113 C&V and PRTL are Dutch assignments; Centurion is Danish. Shared artwork does not grant universal availability. Belgium retains its established Scimitar proxy (`RCN_FV105_UK`) and German Gepard profile; Denmark retains its existing borrowed recon and deliberately has no air-defense formation. Those recon proxies remain an explicit later review, not a claim of historical accuracy or a newly approved replacement. Dutch Hawk still shares the complete US profile/census/art and NATO truck. Existing generic light/heavy gun censuses, including their legacy Humvee support entries, are preserved; they are not silently rewritten as part of new profile registration.

All 255 previous enum names/values and all 217 old template IDs remain valid. New keys append at 255–261; save version remains 11 and `JsonPolicy` is unchanged. Changing template defaults does not migrate persisted OOB/save bay selections: old M113, British recon, German Gepard or US F-16 selections keep resolving to those profiles. Consumers must adopt the registrations and deliberately re-export intended formations. Census-token corrections on existing national profiles take effect wherever those profiles are used.

Availability uses months from January 1938: AAA144, M109300 (existing family anchor), Centurion312, M113 C&V432, YPR/PRTL468, F-16492. These are authored game anchors rather than precise delivery schedules. The [Danish Armor Museum](https://www.pansermuseet.dk/udstillingen/kampvogne/centurion/) describes 105mm conversions in the early/mid-1960s and later thermal/laser modernization; 1964 is the chosen anchor for the older version. The [Dutch military museum](https://www.nmm.nl/nl/stories/ypr-era-pantserrups/) records YPR service from 1977, and its [PRTL collection entry](https://collectie.nmm.nl/nl/collectie/detail/315776/) dates the vehicle to 1977. The [Boreel regiment history](https://huzarenvanboreel.nl/index.php/2025/06/14/tanks-binnen-de-koninklijke-landmacht/) records the 1974 order for 25mm C&V weapons; the scout uses that modernization anchor. [Dutch Defence](https://www.defensie.nl/actueel/nieuws/2024/09/27/f-16-uit-militaire-inventaris-gehaald) records first F-16 arrivals in 1979. Retaining the existing F-16 capability package is a game abstraction, not a claim that its later missiles were present on delivery.

All thirteen `NATO_` PNGs have registered profile owners and formation paths through the existing NATO atlas. This packet changes no image, atlas, asset GUID, saved scene or renderer. [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs) audits imported art and actual atlas/bay reachability; visual/facing inspection, atlas memory/settings, Windows build and Editor/AI export/load remain separate acceptance work.

The approved `HEL_UH1_NATO` / `HEL_UH1C_NATO` shared-US-art follow-up remains open with the US Huey packet. It needs its transport/formation assignments settled against the organic Embarked-lift contract; these profiles are not registered or withdrawn here. No Iranian/Saudi air-mobile formation is reintroduced. Equipment registrations do not complete air operations, supply or requisition.
