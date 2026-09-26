# British roster integration

This contract records `WeaponProfileDB.CreateBritishRosterAdditions`, additions to `CombatUnitDB.CreateBritishForces`, and [BritishRosterTests](../Assets/Tests/EditorTests/BritishRosterTests.cs). The packet adds ten profiles and nine formations: 221 profiles / 210 templates overall, including 18 British profiles / 16 British templates. Current verification and acceptance belong in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>).

| New profile | Definition and authored census basis |
|---|---|
| `ART_LIGHT_UK` | Shared light-gun definition; 1,050 personnel, 54 `ART_105MM_FG`, 12 Javelin. |
| `ART_HEAVY_UK` | Shared heavy-gun definition; 1,050 personnel, 54 `ART_155MM_FG`, 12 Javelin. |
| `AAA_GEN_UK` | Shared regular towed-AAA definition; 500 personnel, 18 national guns, 12 Javelin. |
| `ATT_JAGUAR_UK` | Shared `JaguarStrikeDef()` extracted unchanged from the French profile: DF8 / MAN9 / TS9 / SUR9, GA8 / OL9, stored runway-suppression bonus20. Gen2 attack cost; 36 aircraft. National art and attack formation. |
| `INF_AM_UK` | British light-infantry line with mountain training replacing parachute capability: HA9 / HD7 / SA7 / SD8 / GAD10 / GAT6, ICM1. 1,860 personnel, 18 each 120mm/82mm mortars, 48 ATGM, 24 Javelin. No embedded carriers. |
| `HEL_PUMA_UK` | Non-combatant helicopter with `HeloTransport` category, Gen1 HELT cost and empty census. Own six `UK_Puma` frames; organic Embarked lift only. |
| `HEL_LYNX_UK` | Lynx AH.1 TOW on the established light SACLOS helicopter line: HA11 / HD6 / SA10 / SD7 / GAD10, movement24, ICM1. Gen2 HEL cost; 54 aircraft. Own six `UK_Lynx` frames; no cannon, rocket, armour or transport trait. |
| `APC_FV432_UK` | The existing census-only enum member now has a playable standard tracked APC profile: HA3 / HD4 / SA6 / SD7 / GAD7, movement8, ICM1. Gen1 APC cost; own-platform-only census of 45 vehicles. No RARDEN/Bulldog variant or amphibious capability is inferred. |
| `TANK_CHIEFTAIN_UK` | Earlier Chieftain line: Gen2 + rifled120mm + laser rangefinder + dormant stabilizer + `NATO_FIRST_LINE`. HA12 / HD8 / SA7 / SD6 / GAD7, ICM1.21275, spotting2, movement10. Gen2 tank cost; no TOGS/Stillbrew/Challenger armour. Census: 1,000 personnel, 58 tanks, 21 FV432, 8 FV105, 15 ATGM, 8 Javelin, 18 M109, 9 81mm mortars. |
| `FGT_F4_UK` | Approved Phantom FG.1 interceptor on the established F-4 game line: DF10 / MAN9 / TS12 / SUR8, GA2 / OL6. Gen1 fighter cost; 36 aircraft. No strike or SEAD addition. |

Censuses represent authored game counters, not exact historical squadron/regiment tables. Towed guns use the existing British M109 support-counter personnel/gun scale, without embedded carrier tokens. Air-mobile retains the existing British airborne census, with its own capabilities/art. Chieftain follows the 58-tank British scale, replacing the newer Warrior support slice with FV432s (13 + 8 = 21). The FV432 carrier option uses the same 45-vehicle scale as Warrior; it does not add personnel or rewrite existing support counts elsewhere. Paired infantry/gun/carrier profiles receive no formation-quality multiplier.

| New template | Deployed / Mobile / Embarked |
|---|---|
| `UK_MECH_INFANTRY_REGIMENT_FV432` | `INF_REG_UK` / `APC_FV432_UK` / none |
| `UK_AIRMOBILE_REGIMENT` | `INF_AM_UK` / `TRK_GEN_NATO` / `HEL_PUMA_UK` |
| `UK_LYNX_ATTACK_SQUADRON` | `HEL_LYNX_UK` / none / none |
| `UK_JAGUAR_ATTACK_SQUADRON` | `ATT_JAGUAR_UK` / none / none |
| `UK_LIGHT_ARTILLERY_REGIMENT` | `ART_LIGHT_UK` / `TRK_GEN_NATO` / none |
| `UK_HEAVY_ARTILLERY_REGIMENT` | `ART_HEAVY_UK` / `TRK_GEN_NATO` / none |
| `UK_TOWED_AAA_REGIMENT` | `AAA_GEN_UK` / `TRK_GEN_NATO` / none |
| `UK_ARMOURED_REGIMENT_CHIEFTAIN` | `TANK_CHIEFTAIN_UK` / none / none |
| `UK_F4_FIGHTER_SQUADRON` | `FGT_F4_UK` / none / none |

New formations follow the existing British Side.AI / Experienced baseline. Jaguar is ATT / AirGroundAttack, Phantom is FGT / AirSuperiority, Lynx is HELO / GroundCombat. Puma is organic lift under the current transport contract and Robert's earlier Puma ruling; the historical standalone Puma-squadron proposal is not implemented. No British Gazelle or Huey is substituted. The deleted British M163 template stays deleted. All seven existing British templates remain unchanged, including Challenger, Warrior, Rapier and Tornado.

All 240 previous `WeaponType` names/numeric values remain intact. `APC_FV432_UK` stays at value 190; its previous support-census references remain valid. Nine other IDs are appended at 240–248. No save-version bump, scenario rewrite, asset rename, GUID change, scene mutation or catalog-specific nationality override is introduced. Existing saved/OOB units retain their stored bay selections. Editor/AI consumers must import the new registrations and deliberately export/load representative new formations before interchange acceptance.

Availability follows the monthly January 1938 epoch: guns/AAA turn 144 (1950 commodity anchor), FV432 300 (1963), Chieftain 336 (1966 family anchor), Phantom 372 (1969), Puma 396 (1971), Jaguar 420 (1973), Lynx TOW 516 (1981). These are consolidated game profiles, not year-by-year modification schedules. In particular, neither the Chieftain family anchor nor the Phantom/Jaguar introduction anchors assert that every later trait was fitted at initial delivery.

Source boundaries: the [Ministry of Defence's Puma history](https://www.gov.uk/government/news/raf-puma-celebrates-40th-anniversary) supports the 1971 lift anchor. The Army's [March 1981 Soldier report](https://soldier.army.mod.uk/media/flrotgil/march-1981-vol-37-no3.pdf) reports adoption of Lynx/TOW after trials; the attack profile therefore uses 1981 rather than the earlier airframe introduction. The [RAF historical account of Jaguar operations](https://www.raf.mod.uk/what-we-do/our-history/air-historical-branch/post-coldwar-studies/jaguar-operations/) records RAF service from October 1973; the shared game strike line is not a claim of identical national equipment fits. The [RAF Museum FG.1 record](https://www.rafmuseum.org.uk/research/collections/mcdonnell-douglas-phantom-fg1-nose-only/) supports the approved British variant. The [Tank Museum Chieftain record](https://tankmuseum.org/tank-nuts/tank-collection/chieftain) distinguishes later TOGS/Stillbrew upgrades, and its [FV432/Bulldog record](https://tankmuseum.org/tank-nuts/tank-collection/bulldog/) distinguishes the modern Bulldog. The profile stats and formation quantities above remain game definitions.

All 28 British PNGs are bound through their national profiles and the NATO atlas; the shared truck uses `NATO_Truck`. Rapier and Tornado had already received their national art in the shared integration; the obsolete Rapier placeholder comment is removed without changing its tracked SAM definition. [CommodityProfileTests](../Assets/Tests/EditorTests/CommodityProfileTests.cs) guards shared gun ballistics; [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs) audits imports, frames, saved atlases and formation bays. Visual/facing/animation checks, atlas settings/memory review, Windows build and consumer adoption remain separate. These registrations do not complete air operations, supply or requisition.
