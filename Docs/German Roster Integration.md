# German roster integration

This contract records `WeaponProfileDB.CreateGermanRosterAdditions`, additions to `CombatUnitDB.CreateGermanForces`, and [GermanRosterTests](../Assets/Tests/EditorTests/GermanRosterTests.cs). The packet adds six profiles and seven formations: 227 profiles / 217 templates overall, including 20 German profiles / 19 German templates. Current verification and acceptance belong in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>).

| New profile | Definition and authored census basis |
|---|---|
| `SPSAM_ROLAND_GE` | Tracked command-guided point SAM on the established Rapier stat line: HA1 / HD5 / SA1 / SD5 / GAD7 / GAT14, movement10, indirect range4, spotting6. Gen2 SPSAM cost; 1,100 personnel, 18 launchers, 24 Marder, 12 Luchs. |
| `ATT_ALPHAJET_GE` | Alpha Jet A light strike line: early-jet archetype, TS−3 and `MULTIROLE_STRIKE`. DF8 / MAN9 / TS7 / SUR6, GA6 / OL6. Gen1 ATT cost, 36 aircraft; no precision-weapon, heavy-payload, armour or runway-cratering package. |
| `ROC_MLRS_GE` | MARS I shares `MlrsDef()` with the existing US M270. HA8 / HD7 / SA11 / SD7 / GAD7, movement10, indirect range6, ICM1.05 and rocket action capability. Gen3 ROC cost; 950 personnel, 18 launchers, 24 Marder, 12 Stinger, 6 Luchs. |
| `ART_LIGHT_GE` | Shared light-gun definition; 950 personnel, 48 `ART_105MM_FG`, 12 Stinger. |
| `ART_HEAVY_GE` | Shared heavy-gun definition; 950 personnel, 48 `ART_155MM_FG`, 12 Stinger. |
| `AAA_GEN_GE` | Shared regular towed-AAA definition; 500 personnel, 18 national guns, 12 Stinger. |

These censuses describe authored game counters, not exact historical tables of organization. Roland follows the existing German Gepard counter scale. MARS uses the German M109 support slice with the established 18-launcher rocket count. Towed guns use the German M109 personnel/gun scale without embedded carriers; their Mobile bay supplies the shared NATO truck. Paired gun bases remain at ICM1.0. Extracting `MlrsDef()` leaves all US MLRS statistics, census, availability and artwork unchanged; the smart-munition trait retains the game's existing blended submunition interpretation and does not add MARS II/GMLRS functionality.

| New template | Classification / role | Deployed / Mobile / Embarked |
|---|---|---|
| `GE_PANZERGRENADIER_REGIMENT_M113` | MECH / GroundCombat | `INF_REG_GE` / `APC_M113_GE` / none |
| `GE_ROLAND_REGIMENT` | SPSAM / AirDefenseArea | `SPSAM_ROLAND_GE` / none / none |
| `GE_ALPHAJET_ATTACK_SQUADRON` | ATT / AirGroundAttack | `ATT_ALPHAJET_GE` / none / none |
| `GE_MLRS_REGIMENT` | ROC / GroundCombat | `ROC_MLRS_GE` / none / none |
| `GE_LIGHT_ARTILLERY_REGIMENT` | ART / GroundCombat | `ART_LIGHT_GE` / `TRK_GEN_NATO` / none |
| `GE_HEAVY_ARTILLERY_REGIMENT` | ART / GroundCombat | `ART_HEAVY_GE` / `TRK_GEN_NATO` / none |
| `GE_TOWED_AAA_REGIMENT` | AAA / AirDefenseArea | `AAA_GEN_GE` / `TRK_GEN_NATO` / none |

All new templates use Nationality.FRG, Side.AI and Experienced; Nationality.DE means Denmark. MARS obtains its two combat actions and one counter-battery opportunity action from the existing ROC rules. Roland and Alpha Jet add options; the twelve existing German templates remain unchanged. Hawk still shares the complete US profile/census/art, with the NATO truck. Bo 105 remains the attack helicopter. Neither Tornado's profile nor its existing FGT-class template is reassigned.

The M113 infantry option reuses the previously approved carrier profile and its own-only census of 108 M113s. The original Marder option retains 54 Marders. Both use the unchanged 1,300-person infantry base with 28 organic Leopard 1s. This packet does not silently rescale the existing carrier or air-mobile census. The already-built Veteran air-mobile formation retains `INF_AM_GE` / `APC_M113_GE` / `HEL_UH1D_GE`, 1,900 personnel and organic non-combatant lift with an empty aircraft census. No standalone transport or German Huey gunship is added.

All 249 previous `WeaponType` names/numeric values remain intact; the six new keys are appended at 249–254 in the table's order. Save version remains 11. No asset rename, GUID change, scene mutation, scenario rewrite or stored-bay migration is introduced. Editor/AI consumers must import the new registrations and deliberately export/load representative formations before interchange acceptance.

Availability uses the monthly January 1938 epoch: guns/AAA turn144 (1950 commodity anchor), Alpha Jet492 (1979), Roland516 (1981), MARS624 (1990). These are game availability anchors, not year-by-year equipment fits. The [Bundeswehr aircraft history](https://www.bundeswehr.de/de/selbstverstaendnis/geschichte-bundeswehr/geschichte-luftwaffe/flugzeugtechnik-geschichte-luftwaffe) supplies Alpha Jet's 1979 introduction anchor; [Dassault's account](https://www.dassault-aviation.com/en/passion/aircraft/military-dassault-aircraft/alpha-jet/) identifies the German tactical-support variant but dates its deliveries from 1980, so those accounts differ. The [Bundeswehr weapon summary](https://www.bundeswehr.de/de/mediathek/kampfflugzeug-alpha-jet-60-sekunden-classix-5668574) describes cannon, rockets and bombs. The [Bundestag research report](https://www.bundestag.de/resource/blob/576770/c5359790a0d6d943e4ccb2900cc6d3a1/wd-2-070-18-pdf-data.pdf), printed pages 11 and 15, supports Marder-based Roland service from 1981 and German MARS use from 1990, distinct from the US 1983 anchor. Stats and counter quantities remain game definitions.

All 30 German PNGs are assigned through national profiles in the NATO atlas, including both six-frame helicopter sets. Air-mobile/M113/UH-1D, F-4 and infantry filenames were connected in the shared integration; the stale air-mobile missing-art comment is removed. [CommodityProfileTests](../Assets/Tests/EditorTests/CommodityProfileTests.cs) guards shared gun ballistics; [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs) audits actual imports, helicopter frames, saved atlases and formation bays. Visual/facing/animation checks, atlas settings/memory review, Windows build and consumer adoption remain separate. These registrations do not complete air operations, supply or requisition.
