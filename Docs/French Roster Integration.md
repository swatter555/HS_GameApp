# French roster integration

This contract records the French additions to `WeaponProfileDB.CreateFrenchRosterAdditions` and `CombatUnitDB.CreateFrenchForces`. Current acceptance status belongs in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>); the executable guards are [FrenchRosterTests](../Assets/Tests/EditorTests/FrenchRosterTests.cs), [CommodityProfileTests](../Assets/Tests/EditorTests/CommodityProfileTests.cs) and [UnitIconAssetTests](../Assets/Tests/EditorTests/UnitIconAssetTests.cs).

Robert confirmed on 2026-09-26 that Puma is attached transport in the infantry's Embarked bay. Gazelle is a separate attack-helicopter formation. The historical plan's separate Puma formation is superseded; no standalone helicopter-transport class or pickup system is introduced.

| New profile | Definition and census basis |
|---|---|
| `ART_LIGHT_FR` | Shared light-gun definition; 1,050 personnel, 48 × 105mm guns, 12 Mistral. |
| `ART_HEAVY_FR` | Shared heavy-gun definition; 1,050 personnel, 48 × 155mm guns, 12 Mistral. |
| `AAA_GEN_FR` | Shared regular towed-AAA definition; 500 personnel, 18 guns, 12 Mistral. |
| `IFV_AMX10P_FR` | IFV archetype plus light autocannon and amphibious capability; no vehicle ATGM-rail trait. Carrier-only census: 135 AMX-10Ps. |
| `INF_AM_FR` | Existing French airborne infantry line/census, replacing AirDroppable with MountainMovement, as in the US/German AM distinction. 2,200 personnel, 36 mortars, 36 ATGM and 24 Mistral; no carriers or lift aircraft in the base census. |
| `HEL_PUMA_FR` | Existing non-combatant helicopter-lift definition; HeloTransport category, empty census, HELT/Gen1 cost. |
| `HEL_GAZELLE_FR` | Existing Bo 105 light SACLOS/HOT combat line; no lift category; 54 Gazelles, no personnel, HEL/Gen2 cost. |

These are authored game-counter baselines, not claims of exact historical regimental tables. French gun counters follow the existing AUF1 personnel/tube scale; AMX-10P carrier count matches the VAB option for the same infantry base; the Gazelle count follows the current NATO helicopter-counter convention. National gun ballistics come from the shared helpers rather than copied deltas. Trucks remain `TRK_GEN_NATO` in Mobile bays and contribute no census.

| New template | Deployed / Mobile / Embarked |
|---|---|
| `FR_MECH_INFANTRY_BRIGADE_AMX10P` | `INF_REG_FR` / `IFV_AMX10P_FR` / none |
| `FR_AIRMOBILE_BRIGADE` | `INF_AM_FR` / `APC_VAB_FR` / `HEL_PUMA_FR` |
| `FR_GAZELLE_ATTACK_SQUADRON` | `HEL_GAZELLE_FR` / none / none |
| `FR_LIGHT_ARTILLERY_REGIMENT` | `ART_LIGHT_FR` / `TRK_GEN_NATO` / none |
| `FR_HEAVY_ARTILLERY_REGIMENT` | `ART_HEAVY_FR` / `TRK_GEN_NATO` / none |
| `FR_TOWED_AAA_REGIMENT` | `AAA_GEN_FR` / `TRK_GEN_NATO` / none |

All six templates are French/AI defaults. Ground formations retain the existing French Trained baseline; Gazelle uses the existing aviation Experienced baseline. The original VAB infantry and AMX-30 division templates stay intact. The existing `IFV_AMX10P` census token remains distinct from the new selectable carrier profile. Seven enum members are appended, preserving old numeric values and serialized names; no save-version change or scenario rewrite is needed. Editor/AI adoption of new names still requires catalog and export validation.

Service-year anchors use the existing monthly calendar from January 1938: AMX-10P 1973 → turn 420, Army Puma 1969 → 372, Gazelle SA 342M HOT 1979 → 492. Historical evidence supports platform characteristics and year anchors, not game combat statistics or census quantities: the [French Army's AMX-10 trials and dismounted MILAN section](https://www.terre.defense.gouv.fr/centac/mieux-nous-connaitre/lhistoire-du-1er-bcp), [Senate AMX-10P technical description](https://www.senat.fr/rap/1982-1983/i1982_1983_0382.pdf), [Senate Army helicopter report](https://www.senat.fr/rap/r01-350/r01-3503.html), and [ALAT museum's SA 342M HOT entry](https://museealathelicopteredax.fr/le-musee/). AMX-10P initial trials/deliveries and series production span 1973–74; the profile uses the 1973 early-fielding year.

Puma/Gazelle frame names are integrated but their current artwork remains approved provisional sharing, pending Robert's improved frames. Tests and the reachability inventory preserve this qualification. This packet does not implement air operations, requisition, supply or standalone helicopter transport.
