# Iraqi roster integration

This contract records `WeaponProfileDB.CreateIraqiRosterAdditions`, the Iraqi portion of `CombatUnitDB.CreateArabForces`, and [IraqiRosterTests](../Assets/Tests/EditorTests/IraqiRosterTests.cs). Status and remaining acceptance belong in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>). The packet adds nine profiles and six templates: 202 profiles / 195 templates overall, including 20 Iraqi profiles / 18 Iraqi templates.

Robert replaced the planned Iraqi Mi-24 with an **armed Mi-8AT** on 2026-09-26. `HEL_MI8AT_IQ` is a rocket-armed combat helicopter in a HELO formation, with `IQ_MI8AT_Frame0..5` animation and a 36-aircraft loss census. It has no transport category and cannot occupy an Embarked bay. No Iraqi Mi-24, separate unarmed Mi-8 or Iraqi air-mobile force is registered.

| New profile | Definition / authored census basis |
|---|---|
| `TANK_T72M_IQ` | Existing Soviet T-72A game line plus `EXPORT_DOWNGRADE`: HA12 / HD7 / SA7 / SD5 / GAD7, ICM 0.945. Gen2 tank cost; no ERA/MV variant. Uses the existing Iraqi T-55 counter's 950 personnel / 105 tanks and national support quantities, with its own tank and scout keys. |
| `HEL_MI8AT_IQ` | Helicopter archetype plus `ROCKET_PODS`: HA7 / HD6 / SA11 / SD7 / GAD10, movement 24. Gen1 HEL cost; 36 aircraft, no personnel. No inferred ATGM, cannon, armoured-cockpit or countermeasure traits. |
| `RCN_BRDM2_IQ` | Existing amphibious wheeled BRDM-2 scout mechanics, with no tank export penalty. Recon-counter scale: 800 personnel, 36 scouts, 12 Iraqi T-62s, 21 MT-LBs, 2 ZSU-57s, 2 Kub systems, 6 2S1s, 4 each 81mm/120mm mortars, 24 ATGM and 12 Strela. |
| `FGT_MIRAGEF1_IQ` | French Mirage F1 multirole line with the approved export difference represented by DF −1 / SUR −1 residuals, not the tank-only trait. DF9 / MAN10 / TS11 / SUR7, GA6 / OL6, ICM1. Gen2 fighter cost; 36 aircraft. |
| `ART_LIGHT_IQ` | Shared light-gun definition; 700 personnel, 48 national light guns, 6 Strela. |
| `ART_HEAVY_IQ` | Shared heavy-gun definition; 750 personnel, 36 national heavy guns, 8 Strela. |
| `AAA_GEN_IQ` | Shared regular towed-AAA definition; 500 personnel, 18 national guns, 12 Strela. |
| `TRK_GEN_IQ` | Shared non-combatant wheeled truck definition/cost; empty census. |
| `SAM_S75_IQ` | Shared `S75SiteDef()` with the Soviet profile: range 6 / GAT15 / emplaced movement 0; existing lift capabilities retained. 750 personnel, 18 national SAMs, 21 Strela. |

Census quantities are authored game-counter baselines, not claims of exact Iraqi historical tables. Towed artillery retains the existing regional personnel/gun/MANPADS quantities; the new open-bay support bases omit embedded MT-LB/BTR carrier counts. Their Mobile bay owns transport. Recon retains the established scout-counter scale with national support substitutions. No formation-quality trait or Republican Guard experience bonus is introduced.

| New template | Deployed / Mobile / Embarked |
|---|---|
| `IQ_TANK_REGIMENT_T72M` | `TANK_T72M_IQ` / none / none |
| `IQ_MI8AT_ATTACK_SQUADRON` | `HEL_MI8AT_IQ` / none / none |
| `IQ_RECON_REGIMENT_BRDM2` | `RCN_BRDM2_IQ` / none / none |
| `IQ_MIRAGEF1_FIGHTER_SQUADRON` | `FGT_MIRAGEF1_IQ` / none / none |
| `IQ_LIGHT_ARTILLERY_REGIMENT` | `ART_LIGHT_IQ` / `TRK_GEN_IQ` / none |
| `IQ_TOWED_AAA_REGIMENT` | `AAA_GEN_IQ` / `TRK_GEN_IQ` / none |

New templates retain the Iraqi AI/Green baseline. Existing template keys remain stable: `IQ_INFANTRY_REGIMENT` switches to the Iraqi truck; `IQ_TOWED_ARTILLERY_REGIMENT` uses Iraqi heavy guns/truck; `IQ_SAM_REGIMENT` uses Iraqi S-75/truck. The T-55/T-62 censuses now name the Iraqi BRDM-2, and regular infantry names Iraqi heavy artillery. Their quantities remain unchanged. All Iraqi formation bays and national census equipment now select Iraqi profiles; shared generic weapons such as mortars and MANPADS remain shared.

All previous `WeaponType` names/numeric values remain intact; nine values are appended. The three legacy `_ARAB` profiles remain registered for other templates and existing content, with their explicitly pending art exceptions. No save-version bump, alias, scenario rewrite, asset rename or GUID change is introduced. Existing saved/OOB units retain their stored bay IDs: national reassignments above apply to templates for new authoring, and do not silently migrate old content. Editor/AI catalog refresh and a representative export/load remain required before claiming interchange acceptance.

Availability uses the existing monthly equipment calendar from January 1938. T-72M uses the approved roster plan's 1982 anchor (528); Mirage F1 uses Iraqi 1981 deliveries (516). Mi-8AT uses the existing Mi-8 family's 1967 game anchor (348), not a verified Iraqi AT-subvariant delivery date. BRDM-2, guns, truck, AAA and S-75 inherit the corresponding existing platform/commodity anchors. All stats and counter quantities above are game definitions, not historical measurements.

Source boundaries: the [US Army's Soviet equipment manual](https://www.trngcmd.marines.mil/Portals/207/Docs/MCIS/ITEP/RITC-East/FM%20100-2-3.pdf) describes Mi-8T rocket-pod armament; that supports the chosen family loadout, not every Mi-8 variant's capabilities. [Dassault](https://www.dassault-aviation.com/en/passion/aircraft/military-dassault-aircraft/mirage-f1/) records Iraqi Mirage F1 use, and a [contemporary February 1981 report preserved by CIA](https://www.cia.gov/readingroom/docs/CIA-RDP82-00850R000300090039-7.pdf) records initial deliveries. The approved export downgrade is a game distinction; the [CAEA's Iraqi EQ-6 record](https://www.caea.fr/avions/mirage-f1-eq/mirage-f1-eq-lappareil-du-caea/) describes a later advanced configuration, so this packet does not claim every Iraqi Mirage was technically inferior to every French variant.

[CommodityProfileTests](../Assets/Tests/EditorTests/CommodityProfileTests.cs) guards shared gun/truck statistics; [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs) checks imported art, saved atlases, frames and all formation bays. Visual/facing/animation acceptance, Windows build, consumer adoption and the remaining national packets stay separate from code verification. This packet adds no air-operation, supply or requisition system.
