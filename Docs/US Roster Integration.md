# US roster and shared NATO Huey integration

This contract records `WeaponProfileDB.CreateUsRosterAdditions` and additions to `CombatUnitDB.CreateUSForces`. Eight profiles and six US formations bring the catalog to 242 profiles / 234 templates; the US subset has 34 profiles / 28 templates and 58 PNGs. [UsRosterTests](../Assets/Tests/EditorTests/UsRosterTests.cs) guards these contracts. Verification and acceptance status belong in the [vault TODO](<C:/Users/coder/Desktop/Codex Projects/HS Game/HS Game TODO.md>).

NATO Huey equipment is registered with shared US artwork. **Robert rejected Huey air-mobile and gunship formations for the Netherlands, Belgium and Denmark on 2026-09-26 because they lack a realistic national basis.** Keep both NATO Huey entries unassigned in the formation catalog. No NATO air-mobile infantry profile is added, and foreign lift must not recreate the rejected formations. The registrations do not establish historical national use or universal availability.

| New profile / enum value | Definition and authored census |
|---|---|
| `ART_LIGHT_US` / 262 | Shared light towed artillery; Foot, cost90, 1,050 personnel, 54 × 105mm guns and 12 Stinger. Uses `US_LightArt`. |
| `ART_HEAVY_US` / 263 | Shared heavy towed artillery; Foot, cost90, 1,050 personnel, 54 × 155mm guns and 12 Stinger. Uses `US_HeavyArt`. |
| `AAA_GEN_US` / 264 | Shared regular towed AAA; Foot, cost70, 500 personnel, 18 national AAA and 12 Stinger. Uses `US_AAA`. |
| `TRK_GEN_US` / 265 | Shared non-combatant wheeled truck, cost20, empty census. Uses `US_Truck`. |
| `HEL_UH1_US` / 266 | UH-1D/H organic lift, cost90, turn300 (1963 anchor), empty census. Uses `US_UH1_Frame0..5`. |
| `HEL_UH1C_US` / 267 | Rocket/minigun gunship, cost100, turn324 (1965 anchor), 54 aircraft under its own key. Uses `US_UH1C_Frame0..5`. |
| `HEL_UH1_NATO` / 268 | Same lift definition, price and availability as the US Huey; empty census and shared US transport frames. |
| `HEL_UH1C_NATO` / 269 | Same gunship definition, price and availability as the US UH-1C; 54 aircraft under its own NATO key and shared US gunship frames. |

These are authored game-counter quantities. The US artillery uses the existing US M109's personnel/gun scale; the open-bay towed profiles exclude embedded carriers. Towed AAA follows the established regular support scale. The 1950 support-profile anchor does not date every item in its blended Stinger-equipped census. Transport belongs in Mobile and owns no duplicate personnel/gun census.

`OrganicHeloLiftDef()` extracts the existing Black Hawk/German UH-1D definition unchanged: Helicopter family plus `NON_COMBATANT`. New Huey lift uses `TransportCategory.HeloTransport` in an eligible unit's Embarked bay, never as a standalone HELO formation or Mobile profile. It has no separate helicopter count in the formation census. The older 90-prestige lift option leaves the Black Hawk's 150-prestige tier intact.

`Uh1cGunshipDef()` uses the Helicopter family plus `ROCKET_PODS`. Its family gun baseline represents miniguns; it has no `CANNON_HELO`, guided anti-tank missile or lift trait. Resolved values are HA7 / HD6 / SA11 / SD7 / GAD10, movement24, spotting3 and ICM1.0. It uses the existing helicopter-attack sound family and HELO action rules. Robert approved the explicit 100-prestige price on 2026-09-26, centralized as `GameData.PRESTIGE_UH1C_GUNSHIP`; the existing Cobra stays at 130.

| New US template | Deployed / Mobile / Embarked | Experience |
|---|---|---|
| `US_LIGHT_ARTILLERY_REGIMENT` | `ART_LIGHT_US` / `TRK_GEN_US` / NONE | Experienced |
| `US_HEAVY_ARTILLERY_REGIMENT` | `ART_HEAVY_US` / `TRK_GEN_US` / NONE | Experienced |
| `US_TOWED_AAA_REGIMENT` | `AAA_GEN_US` / `TRK_GEN_US` / NONE | Trained |
| `US_COBRA_AVIATION_BRIGADE` | `HEL_AH1_US` / NONE / NONE | Experienced |
| `US_UH1C_AVIATION_BRIGADE` | `HEL_UH1C_US` / NONE / NONE | Experienced |
| `US_AIRMOBILE_BRIGADE_UH1` | `INF_AM_US` / `APC_HUMVEE_US` / `HEL_UH1_US` | Veteran |

The new formations follow existing US defaults: Nationality.USA, Side.AI, Secondary/Small metadata. Towed AAA uses AirDefenseArea; artillery, aviation and air-mobile use GroundCombat. Existing `US_AVIATION_BRIGADE` (Apache) and `US_AIRMOBILE_BRIGADE` (Black Hawk) remain. Cobra's profile, price and six national frames remain unchanged; this packet gives it a formation. The Huey air-mobile option retains the US infantry/Humvee census of 2,040 personnel, with no tanks or counted organic helicopters.

The only changed old template is `US_HAWK_REGIMENT`: Mobile now selects `TRK_GEN_US` instead of `TRK_GEN_NATO`. Its Hawk profile is unchanged. German/Dutch Hawk formations retain their shared NATO truck. All other old templates and all old profile definitions are preserved.

All 262 previous `WeaponType` names/values and all 228 previous template IDs remain. New enum values are appended at 262–269. Save version stays 11; JsonPolicy, assets, GUIDs, atlases, scenes and scenario/OOB files are unchanged. A template default change does not rewrite stored bay selections; consumers must intentionally adopt/re-export and verify the result. New keys require matching consumer catalogs. See the [Editor/AI packet](<C:/Users/coder/Desktop/Codex Projects/Agent Correspondence/2026-09-26 HS Game US and NATO Huey integration.md>).

The 58 US PNGs use the existing NATO atlas. Both NATO Hueys share the corresponding six US frames through profile-owned art, without renderer nationality overrides. The checklist still contains 347 PNGs; its created status is independent of runtime, visual and build acceptance. New registrations do not complete air operations, supply or requisition. Iranian/Saudi air-mobile restrictions, German transport-only Huey and British Puma/Lynx choices remain. C-130, engineers and special forces are outside this packet.

Historical anchors: the [US Army helicopter history](https://history.redstone.army.mil/avi-systems.html) and [Army account of Huey/Cobra use](https://www.army.mil/article/281353/it_wasnt_just_napalm_a_tale_of_the_huey_and_cobra_in_vietnam) support the transport/gunship distinction and rocket/minigun role; the [NASA helicopter chronology](https://ntrs.nasa.gov/api/citations/19790025923/downloads/19790025923.pdf) records 1963 UH-1D and 1965 UH-1C entries. Availability is an authored game anchor, not a country-by-country service-date guarantee.
