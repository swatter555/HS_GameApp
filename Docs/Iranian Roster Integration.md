# Iranian roster integration

This contract records `WeaponProfileDB.CreateIranianRosterAdditions`, the Iranian portion of `CombatUnitDB.CreateArabForces`, and [IranianRosterTests](../Assets/Tests/EditorTests/IranianRosterTests.cs). The packet adds nine profiles and six templates: 211 profiles / 201 templates overall, including 14 Iranian profiles / 14 Iranian templates. Work status and acceptance belong in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>).

| New profile | Definition and authored census basis |
|---|---|
| `TANK_CHIEFTAIN_IR` | Mk3/Mk5-era Gen2 tank with `GUN_120_RIFLED` and dormant two-plane stabilizer: HA12 / HD8 / SA7 / SD6 / GAD7, ICM1.05, movement10, spotting2. Gen2 tank cost. 1,100 personnel, 100 tanks, 52 Iranian M113s, 12 Iranian M113 scouts, 18 each 155mm guns/120mm mortars, 8 ATGM, 6 Stinger, 4 shared Soviet ZSU-23s. |
| `HEL_AH1_IR` | TOW-capable AH-1J representation using the established Cobra ATGM/cannon/rocket traits: HA11 / HD6 / SA13 / SD7 / GAD10, movement24, ICM1. Gen1 HEL cost; 54 aircraft only. Own `IR_AH1_Frame0..5`, combat HELO with no transport category. |
| `FGT_F5_IR` | F-5E: FighterEarly plus `AGILE_AIRFRAME`, `MULTIROLE_STRIKE`, TS−1. DF8 / MAN11 / TS9 / SUR6, GA6 / OL6, ICM1. Gen1 fighter cost; 48 aircraft, matching the ratified Iranian F-4/F-14 wing scale. No BVR, guided strike or later defensive/radar suite added. |
| `SPA_M109_IR` | Conventional 155mm M109: Artillery + SA1 / medium range + `SELF_PROPELLED`; HA5 / HD7 / SA10 / SD7 / GAD7, range5, movement10, ICM1. Gen2 SPA cost; 750 personnel, 36 M109s, 12 Iranian M113s, 8 Strela. No US Copperhead or NATO fires-network trait. |
| `RCN_M113_IR` | Recon archetype with movement8 to match the parent M113, tracked, spotting3. Gen1 recon cost; 600 personnel, 36 scouts, 12 Iranian M113s, 8 ATGM, 6 Strela. Dedicated `IR_M113Recon`; neither a new APC carrier nor the Dutch M113 C&V. |
| `ART_LIGHT_IR` | Shared light-gun definition; 700 personnel, 48 national guns, 6 Strela. |
| `ART_HEAVY_IR` | Shared heavy-gun definition; 750 personnel, 36 national guns, 8 Strela. |
| `AAA_GEN_IR` | Shared regular towed-AAA definition; 500 personnel, 18 national guns, 12 Strela. |
| `TRK_GEN_IR` | Shared non-combatant wheeled truck definition/cost; empty census. |

These quantities are game-counter baselines, not exact historical tables. The Chieftain counter follows the existing Iranian M60 scale with its own tank/scout keys; the M60's existing British scout and Soviet SPAAA support tokens remain unchanged. Both tank counters retain the established shared ZSU support rather than replacing it with a different towed weapon. M109 uses the regional 36-gun scale with carrier support; M113 recon is a smaller authored scout counter. The existing M113 APC keeps its own-platform-only census of 90. National towed artillery retains the old regional gun/personnel/MANPADS quantities while omitting embedded foreign carriers: the Mobile bay owns transport. Infantry's 18 heavy guns now use `ART_HEAVY_IR`, with all its quantities preserved.

| New template | Deployed / Mobile / Embarked |
|---|---|
| `IR_TANK_REGIMENT_CHIEFTAIN` | `TANK_CHIEFTAIN_IR` / none / none |
| `IR_AH1_ATTACK_SQUADRON` | `HEL_AH1_IR` / none / none |
| `IR_F5_FIGHTER_SQUADRON` | `FGT_F5_IR` / none / none |
| `IR_SP_ARTILLERY_REGIMENT_M109` | `SPA_M109_IR` / none / none |
| `IR_RECON_REGIMENT_M113` | `RCN_M113_IR` / none / none |
| `IR_HAWK_SAM_REGIMENT` | `SAM_HAWK_US` / `TRK_GEN_IR` / none |

New formations use Side.AI. Ground/SAM additions are Green; Cobra and F-5 use Experienced, following the existing Iranian aviation baseline. No new national formation-quality trait is applied. Existing IDs remain: `IR_INFANTRY_REGIMENT` selects the Iranian truck; `IR_HEAVY_ARTILLERY_REGIMENT` and `IR_LIGHT_ARTILLERY_REGIMENT` select national guns/trucks; `IR_AIR_DEFENSE_REGIMENT` switches from Soviet AAA to Iranian AAA/truck. The existing M60 and M113 infantry options remain available.

Hawk deliberately shares the entire `SAM_HAWK_US` profile, including US art, site-SAM range6/GAT15, existing lift capabilities and its 1,100-personnel/18-Hawk/4-M163/24-M113 census. The Iranian formation adds a national truck in Mobile. No `SAM_HAWK_IR` is created and no per-nationality artwork or census override is introduced. No Iranian S-75, UH-1 or air-mobile formation is restored. Cobra cannot occupy an Embarked bay.

All 231 prior `WeaponType` names/numeric values remain; nine values are appended. All prior template IDs, asset GUIDs, scene references and save version 11 remain unchanged. The three legacy `_ARAB` profiles remain registered for stored content and remaining consumers, including the Mujahideen truck. A template re-point does not migrate existing saved/OOB bay IDs. Editor/AI catalog refresh and a representative deliberate export/load are required before interchange acceptance.

Availability uses the monthly January 1938 epoch. Chieftain uses a 1971 game anchor (396), Cobra and F-5E a 1974 anchor (432). These compress the era into one profile each, not an exact delivery or modification timeline. M109 uses the established family anchor of 1963 (300); M113 recon inherits the carrier's 1960 anchor (264); national gun/AAA/truck anchors match the commodity definitions. No later Iranian upgrades are implied.

Source boundaries: the [Tank Museum's Chieftain record](https://tankmuseum.org/tank-nuts/tank-collection/chieftain) distinguishes later TOGS/Stillbrew upgrades from the original Mark5; this profile does not inherit those later features. The [Fort Worth Aviation Museum's F-5E record](https://fortworthaviationmuseum.com/f-5e-tiger-ii/) describes its agility, Mach1.63 speed and conventional armament; the trait/residual selection is a game interpretation. A [contemporary US delivery-planning memorandum](https://history.state.gov/historicaldocuments/frus1969-76ve04/d238) schedules Iranian F-5Es in 1973–74; 1974 is the game anchor, not a claim that deliveries could not begin earlier. The [FY1976–77 Defense report](https://history.defense.gov/Portals/70/Documents/annual_reports/1976-77_DoD_AR.pdf) records Iran's funding of improved AH-1J propulsion; the Cobra's TOW-capable game line comes from the existing approved weapon-family design, not an assertion that every Iranian airframe had identical equipment. All combat stats and census quantities above are authored game definitions.

[CommodityProfileTests](../Assets/Tests/EditorTests/CommodityProfileTests.cs) guards national gun/truck parity. [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs) validates imports, atlas reachability, six-frame sets and formation bays. Visual/facing/animation acceptance, Windows build and consumer adoption remain separate. No air-operation, supply or requisition system is completed by these registrations.
