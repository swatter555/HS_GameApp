using System.Collections.Generic;
using static HammerAndSickle.Models.TraitEffect;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// The definitive trait catalogue (WeaponTrait_Catalog_01). Maps every <see cref="WeaponTrait"/>
    /// to its effects, category, and a short historical note. ~26 traits carry Dormant effects that
    /// the resolver skips until their host system ships (Appendix W §14).
    ///
    /// Convention reminders:
    ///  - Plain gun-calibre traits (GUN_105/120/125) only adjust OFF-NORM cases; the generation
    ///    archetype already bakes in that era's standard gun. GUN_105_RIFLED is therefore an empty
    ///    (note-only) baseline; an under-/up-gunned profile expresses that as a residual delta.
    ///  - Fire control goes to ICM (helps HA and SA); armour goes to HD; never double-count.
    /// </summary>
    public static class WeaponTraitCatalog
    {
        #region Storage

        private static readonly Dictionary<WeaponTrait, TraitDef> _defs = Build();

        public static IReadOnlyDictionary<WeaponTrait, TraitDef> All => _defs;
        public static int Count => _defs.Count;

        public static TraitDef Get(WeaponTrait trait)
            => _defs.TryGetValue(trait, out TraitDef def) ? def : null;

        public static bool Has(WeaponTrait trait) => _defs.ContainsKey(trait);

        #endregion // Storage

        #region Catalogue

        private static Dictionary<WeaponTrait, TraitDef> Build()
        {
            var d = new Dictionary<WeaponTrait, TraitDef>();
            void Add(TraitDef def) => d[def.Id] = def;

            #region §2 Tank — Protection
            Add(new TraitDef(WeaponTrait.ERA_LIGHT, TraitCategory.TankProtection,
                "Kontakt-1 ERA; defeats HEAT only.", Delta(ProfileStat.HD, 2)));
            Add(new TraitDef(WeaponTrait.ERA_HEAVY, TraitCategory.TankProtection,
                "Kontakt-5; integrated, defeats APFSDS.", Delta(ProfileStat.HD, 3)));
            Add(new TraitDef(WeaponTrait.ERA_RELIKT, TraitCategory.TankProtection,
                "Relikt/Kontakt-7 (3rd-gen ERA, T-80BVM/T-90M); defeats tandem HEAT + trims APFSDS.",
                Delta(ProfileStat.HD, 4)));
            Add(new TraitDef(WeaponTrait.COMPOSITE_CERAMIC, TraitCategory.TankProtection,
                "Chobham/Burlington ceramic.", Delta(ProfileStat.HD, 2)));
            Add(new TraitDef(WeaponTrait.COMPOSITE_DU, TraitCategory.TankProtection,
                "M1A1 depleted-uranium mesh layer.", Delta(ProfileStat.HD, 3)));
            Add(new TraitDef(WeaponTrait.SPACED_ARMOR, TraitCategory.TankProtection,
                "Leopard 2 perforated/spaced array.", Delta(ProfileStat.HD, 1)));
            Add(new TraitDef(WeaponTrait.LOW_PROFILE, TraitCategory.TankProtection,
                "Low Soviet silhouette (re-adds removed silhouette as a defensive trait).",
                Delta(ProfileStat.HD, 1), Delta(ProfileStat.SD, 1)));
            Add(new TraitDef(WeaponTrait.BELLY_ARMOR, TraitCategory.TankProtection,
                "DORMANT → mined-hex (7.5.5.8): negate mine penalty."));
            Add(new TraitDef(WeaponTrait.SMOKE_DISCHARGERS, TraitCategory.TankProtection,
                "DORMANT → LoS/screen layer: ×0.9 incoming first attack.",
                Cap(WeaponCapability.NightObscurant, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.AUTO_FIRE_SUPPRESSION, TraitCategory.TankProtection,
                "Halon crew/engine systems (NATO lead).", Delta(ProfileStat.SD, 1)));
            Add(new TraitDef(WeaponTrait.ACTIVE_PROTECTION_HARDKILL, TraitCategory.TankProtection,
                "Hard-kill APS (Arena-M/Afganit; NATO Trophy) — intercepts ATGM/HEAT. HD+1 live; the explicit " +
                "intercept mechanic is DORMANT.",
                Delta(ProfileStat.HD, 1), Cap(WeaponCapability.ActiveProtection, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.AMMO_CAROUSEL_VULN, TraitCategory.TankProtection,
                "DORMANT → crit/cook-off: higher destroy-on-kill malus."));
            #endregion

            #region §3 Tank — Firepower
            Add(new TraitDef(WeaponTrait.GUN_105_RIFLED, TraitCategory.TankFirepower,
                "L7/M68 105mm — NATO Gen1-2 standard (baked into archetype; off-norm via delta)."));
            Add(new TraitDef(WeaponTrait.GUN_120_SMOOTH, TraitCategory.TankFirepower,
                "Rheinmetall Rh-120 / M256 (off-norm up-gun).", Delta(ProfileStat.HA, 2)));
            Add(new TraitDef(WeaponTrait.GUN_120_RIFLED, TraitCategory.TankFirepower,
                "British L11/L30 (Chieftain/Challenger).", Delta(ProfileStat.HA, 2), Icm(1.05f)));
            Add(new TraitDef(WeaponTrait.GUN_125_SMOOTH, TraitCategory.TankFirepower,
                "Soviet 2A46 125mm + autoloader (off-norm up-gun).", Delta(ProfileStat.HA, 2)));
            Add(new TraitDef(WeaponTrait.APFSDS_ADVANCED, TraitCategory.TankFirepower,
                "Long-rod penetrators; M829 DU (1985).", Delta(ProfileStat.HA, 2)));
            Add(new TraitDef(WeaponTrait.GUN_LAUNCHED_ATGM, TraitCategory.TankFirepower,
                "Kobra/Svir/Refleks/Bastion gun-launched ATGM.",
                Delta(ProfileStat.HA, 2), Delta(ProfileStat.PR, 1)));
            Add(new TraitDef(WeaponTrait.THERMOBARIC, TraitCategory.TankFirepower,
                "TOS-1 FAE — anti-soft devastation.", Delta(ProfileStat.SA, 5)));
            Add(new TraitDef(WeaponTrait.CANISTER_HE, TraitCategory.TankFirepower,
                "Beehive/canister & good HE-Frag.", Delta(ProfileStat.SA, 1)));
            #endregion

            #region §3b Artillery munitions
            Add(new TraitDef(WeaponTrait.SMART_MUNITION, TraitCategory.ArtilleryMunition,
                "Copperhead/Krasnopol/SADARM/DPICM — anti-armour artillery bite.",
                Delta(ProfileStat.HA, 3), Delta(ProfileStat.SA, 1)));
            #endregion

            #region §4 Fire Control & Optics
            Add(new TraitDef(WeaponTrait.LASER_RANGEFINDER, TraitCategory.FireControlOptics,
                "First-round-hit ranging (M60A3, T-72B).", Icm(1.05f)));
            Add(new TraitDef(WeaponTrait.BALLISTIC_COMPUTER, TraitCategory.FireControlOptics,
                "Digital fire-control solution.", Icm(1.05f)));
            Add(new TraitDef(WeaponTrait.OPTICS_GEN2, TraitCategory.FireControlOptics,
                "Improved day sights.", Delta(ProfileStat.SR, 1), Icm(1.05f)));
            Add(new TraitDef(WeaponTrait.OPTICS_GEN3, TraitCategory.FireControlOptics,
                "Best Western day optics.", Delta(ProfileStat.SR, 1), Icm(1.10f)));
            Add(new TraitDef(WeaponTrait.THERMAL_IMAGER, TraitCategory.FireControlOptics,
                "TIS/TTS — NATO 1980s all-weather edge; ICM ramps in poor weather.",
                Delta(ProfileStat.SR, 1), IcmWeather(1.10f, 1.20f)));
            Add(new TraitDef(WeaponTrait.HUNTER_KILLER, TraitCategory.FireControlOptics,
                "DORMANT → multi-engage: commander's independent thermal.",
                Icm(1.10f, EffectStatus.Dormant), Cap(WeaponCapability.MultiEngage, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.NVG_PASSIVE, TraitCategory.FireControlOptics,
                "DORMANT → night pass (4.5.5): image-intensifier night sights.",
                Icm(1.10f, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.ACTIVE_IR_SEARCHLIGHT, TraitCategory.FireControlOptics,
                "DORMANT → night pass: older Soviet IR, reveals position malus."));
            Add(new TraitDef(WeaponTrait.GUN_STABILIZER_2PLANE, TraitCategory.FireControlOptics,
                "DORMANT → fire-on-move: ×1.05 ICM attacking from Mobile.",
                Icm(1.05f, EffectStatus.Dormant)));
            #endregion

            #region §5 Mobility & Survivability — ground
            Add(new TraitDef(WeaponTrait.GAS_TURBINE, TraitCategory.GroundMobility,
                "M1 AGT1500 / T-80 GTD — high power/accel.", Delta(ProfileStat.MMP, 2)));
            Add(new TraitDef(WeaponTrait.DEEP_WADING_SNORKEL, TraitCategory.GroundMobility,
                "OPVT MBT river-fording kit.", Cap(WeaponCapability.DeepWading)));
            Add(new TraitDef(WeaponTrait.AMPHIBIOUS, TraitCategory.GroundMobility,
                "Full swim (BMP/BTR/BMD/AAV7).", Cap(WeaponCapability.Amphibious)));
            Add(new TraitDef(WeaponTrait.AIR_DROPPABLE, TraitCategory.GroundMobility,
                "BMD/Sheridan airborne deploy.", Cap(WeaponCapability.AirDroppable)));
            Add(new TraitDef(WeaponTrait.EXTENDED_RANGE_FUEL, TraitCategory.GroundMobility,
                "Soviet long-range drums — supply ×0.9.", Cap(WeaponCapability.ExtendedRange)));
            Add(new TraitDef(WeaponTrait.HIGH_GROUND_PRESSURE, TraitCategory.GroundMobility,
                "DORMANT → per-class terrain move: heavy MBTs bog in soft terrain."));
            Add(new TraitDef(WeaponTrait.SELF_PROPELLED, TraitCategory.GroundMobility,
                "Tracked self-propelled chassis (SP guns, tracked SP-AAA/SAM) — mobility off the MMP-4 towed " +
                "baseline + an armoured hull; a vehicle is a slightly bigger air target than a dispersed towed battery.",
                Delta(ProfileStat.MMP, 6), Delta(ProfileStat.HD, 2), Delta(ProfileStat.SD, 2), Delta(ProfileStat.GAD, -1)));
            Add(new TraitDef(WeaponTrait.TRUCK_MOUNTED, TraitCategory.GroundMobility,
                "Wheeled launch/transport chassis (MRLs, Scud TEL, wheeled SP-SAM) — mobility on a soft-skinned " +
                "truck that is markedly air-vulnerable.",
                Delta(ProfileStat.MMP, 4), Delta(ProfileStat.GAD, -2)));
            #endregion

            #region §6 IFV / APC
            Add(new TraitDef(WeaponTrait.AUTOCANNON_LIGHT, TraitCategory.IfvApc,
                "20-25mm (Bradley/Marder).", Delta(ProfileStat.SA, 1)));
            Add(new TraitDef(WeaponTrait.AUTOCANNON_HEAVY, TraitCategory.IfvApc,
                "30mm (BMP-2), 30+100mm (BMP-3).", Delta(ProfileStat.SA, 1), Delta(ProfileStat.HA, 1)));
            Add(new TraitDef(WeaponTrait.ATGM_RAIL, TraitCategory.IfvApc,
                "Mounted ATGM (Konkurs rail, M2 TOW).", Delta(ProfileStat.HA, 4)));
            Add(new TraitDef(WeaponTrait.FIRING_PORTS, TraitCategory.IfvApc,
                "DORMANT → mounted-fire rule (BMP-1, M2).",
                Cap(WeaponCapability.MountedFire, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.THIN_TOP, TraitCategory.IfvApc,
                "Open/light vehicles extra air-vulnerable.", Delta(ProfileStat.GAD, -1)));
            #endregion

            #region §7 Infantry
            Add(new TraitDef(WeaponTrait.ATGM_LIGHT, TraitCategory.Infantry,
                "Dragon / 9K111 Fagot — short man-portable AT.", Delta(ProfileStat.HA, 2)));
            Add(new TraitDef(WeaponTrait.ATGM_MEDIUM, TraitCategory.Infantry,
                "MILAN / 9M113 Konkurs.", Delta(ProfileStat.HA, 3)));
            Add(new TraitDef(WeaponTrait.ATGM_HEAVY, TraitCategory.Infantry,
                "TOW / HOT — heavy long-range.", Delta(ProfileStat.HA, 5)));
            Add(new TraitDef(WeaponTrait.MANPADS_BASIC, TraitCategory.Infantry,
                "9K32 Strela-2 / Blowpipe — early IR/SACLOS.",
                Floor(ProfileStat.GAT, 6), Cap(WeaponCapability.EngageAir)));
            Add(new TraitDef(WeaponTrait.MANPADS_STINGER, TraitCategory.Infantry,
                "FIM-92 Stinger — fire-and-forget all-aspect IR (MJ standout).",
                Floor(ProfileStat.GAT, 8), Icm(1.05f),
                Cap(WeaponCapability.EngageAir), Cap(WeaponCapability.FireAndForget)));
            Add(new TraitDef(WeaponTrait.MANPADS_IGLA, TraitCategory.Infantry,
                "9K38 Igla — best Soviet, IFF + decoy-resistant.",
                Floor(ProfileStat.GAT, 8), Cap(WeaponCapability.EngageAir)));
            Add(new TraitDef(WeaponTrait.RPG_LAW, TraitCategory.Infantry,
                "RPG-7/18, LAW, Carl Gustav — point-blank AT.", Delta(ProfileStat.HA, 1)));
            Add(new TraitDef(WeaponTrait.BODY_ARMOR, TraitCategory.Infantry,
                "Flak vests / early composite (late-80s).", Delta(ProfileStat.SD, 1)));
            Add(new TraitDef(WeaponTrait.SPECIAL_FORCES, TraitCategory.Infantry,
                "Spetsnaz/SAS/Rangers/MJ elite — training package.",
                Delta(ProfileStat.SA, 2), Delta(ProfileStat.HA, 2), Delta(ProfileStat.SD, 1), Icm(1.10f)));
            Add(new TraitDef(WeaponTrait.MOUNTAIN_TRAINED, TraitCategory.Infantry,
                "Alpine/airmobile terrain skill — ×0.8 move non-clear.",
                Cap(WeaponCapability.MountainMovement)));
            Add(new TraitDef(WeaponTrait.NBC_PROTECTED, TraitCategory.Infantry,
                "MOPP/OZK NBC kit — NBC-zone ICM ×0.75(Gen1)/×1.0(Gen2); no effect outside NBC zones."));
            #endregion

            #region §8 Helicopters
            Add(new TraitDef(WeaponTrait.ATGM_HELO_SACLOS, TraitCategory.Helicopter,
                "TOW (Cobra) / 9K114 Shturm — must hold lock.", Delta(ProfileStat.HA, 4)));
            Add(new TraitDef(WeaponTrait.ATGM_HELO_FNF, TraitCategory.Helicopter,
                "Hellfire / 9M120 Ataka — lock-after-launch.", Delta(ProfileStat.HA, 5), Icm(1.05f)));
            Add(new TraitDef(WeaponTrait.CANNON_HELO, TraitCategory.Helicopter,
                "GSh-30 (Mi-24/28) / M230 (Apache).", Delta(ProfileStat.SA, 2)));
            Add(new TraitDef(WeaponTrait.ROCKET_PODS, TraitCategory.Helicopter,
                "S-8/S-5 / Hydra-70 — area anti-soft.", Delta(ProfileStat.SA, 1)));
            Add(new TraitDef(WeaponTrait.MAST_MOUNTED_SIGHT, TraitCategory.Helicopter,
                "DORMANT → LoS/defilade: OH-58D/Apache Longbow over-rotor sight.",
                Delta(ProfileStat.SR, 1, EffectStatus.Dormant), Icm(1.10f, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.HELO_COUNTERMEASURES, TraitCategory.Helicopter,
                "Flare/chaff/IRCM vs MANPADS.", Delta(ProfileStat.GAD, 2)));
            Add(new TraitDef(WeaponTrait.ARMORED_COCKPIT, TraitCategory.Helicopter,
                "Mi-24/28 titanium tub / Apache crashworthiness.",
                Delta(ProfileStat.SD, 1), Delta(ProfileStat.HD, 1)));
            Add(new TraitDef(WeaponTrait.NOE_FLIGHT, TraitCategory.Helicopter,
                "DORMANT → LoS: nap-of-earth terrain masking (×0.9 defensive).",
                Cap(WeaponCapability.NightObscurant, EffectStatus.Dormant)));
            #endregion

            #region §9 Fixed-Wing
            Add(new TraitDef(WeaponTrait.RWR, TraitCategory.FixedWing,
                "Radar-warning receiver.", Delta(ProfileStat.SUR, 1)));
            Add(new TraitDef(WeaponTrait.ECM_JAMMER, TraitCategory.FixedWing,
                "Self-protection jammer; SUR+1 live, full jam DORMANT (EW layer).",
                Delta(ProfileStat.SUR, 1)));
            Add(new TraitDef(WeaponTrait.CHAFF_FLARE, TraitCategory.FixedWing,
                "Dispensers vs radar/IR missiles.", Delta(ProfileStat.SUR, 1)));
            Add(new TraitDef(WeaponTrait.TERRAIN_FOLLOW_RADAR, TraitCategory.FixedWing,
                "DORMANT → strike-route AD: low-level penetration (SUR+2 vs ground AD).",
                Delta(ProfileStat.SUR, 2, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.TARGETING_POD, TraitCategory.FixedWing,
                "Pave Tack / LANTIRN — precision strike.", Delta(ProfileStat.GA, 2), Icm(1.10f)));
            Add(new TraitDef(WeaponTrait.LASER_GUIDED_MUNITIONS, TraitCategory.FixedWing,
                "Paveway LGBs / Kh-29.", Delta(ProfileStat.GA, 2)));
            Add(new TraitDef(WeaponTrait.STEALTH_RAM, TraitCategory.FixedWing,
                "F-117 radar-absorbent — evade SAM/intercept.", Cap(WeaponCapability.Stealth)));
            Add(new TraitDef(WeaponTrait.HIGH_MACH_DASH, TraitCategory.FixedWing,
                "MiG-25/31, SR-71 — high top speed (set via archetype/TS constants)."));
            Add(new TraitDef(WeaponTrait.BVR_RADAR_MISSILE, TraitCategory.FixedWing,
                "Semi-active radar BVR (AIM-7 Sparrow, R-27 Alamo) — standard 4th-gen long-range A2A.",
                Delta(ProfileStat.DF, 2)));
            Add(new TraitDef(WeaponTrait.ACTIVE_RADAR_AAM, TraitCategory.FixedWing,
                "Active fire-and-forget BVR (AIM-120 AMRAAM, AIM-54 Phoenix, R-77/AA-12 Adder) — the step above " +
                "semi-active: launch-and-leave, multiple simultaneous engagements. Top-tier A2A. Use INSTEAD of " +
                "BVR_RADAR_MISSILE, not on top of it.",
                Delta(ProfileStat.DF, 3)));
            Add(new TraitDef(WeaponTrait.HIGH_OFF_BORESIGHT_IR, TraitCategory.FixedWing,
                "High-off-boresight IR missile + helmet-mounted sight (R-73/AA-11 Archer, later AIM-9X) — decisive " +
                "close-in dogfight edge: shoot before you point. A standout WVR weapon of the era.",
                Delta(ProfileStat.DF, 1)));
            Add(new TraitDef(WeaponTrait.AGILE_AIRFRAME, TraitCategory.FixedWing,
                "F-16/MiG-29/Mirage 2000 — high-AoA dogfighter.", Delta(ProfileStat.MAN, 2)));
            Add(new TraitDef(WeaponTrait.LOOKDOWN_SHOOTDOWN, TraitCategory.FixedWing,
                "Pulse-Doppler radar suite (F-15 APG-63, MiG-31 Zaslon, Su-27 N001) — clutter-rejecting look-down/" +
                "shoot-down fire control. The air analog of a tank's FCS traits → ICM ×1.10. (The LoS/altitude " +
                "sub-effect vs low fliers stays for the future LoS pass.)",
                Icm(1.10f)));
            Add(new TraitDef(WeaponTrait.AERIAL_REFUEL, TraitCategory.FixedWing,
                "DORMANT → fuel model: endurance/range."));
            Add(new TraitDef(WeaponTrait.ESCAPE_PROFILE, TraitCategory.FixedWing,
                "Tu-22M engineered to outrun, not dogfight.",
                Delta(ProfileStat.TS, 1), Delta(ProfileStat.MAN, 3)));
            Add(new TraitDef(WeaponTrait.HARDENED_STRIKE, TraitCategory.FixedWing,
                "F-111/F-117 heavy precision punch.", Delta(ProfileStat.GA, 3), Delta(ProfileStat.OL, 2)));
            #endregion

            #region §9b Air-to-Ground munitions & roles (Rule A/B)
            // Flat GA/OL/SUR/PR deltas are LIVE. The numeric strike riders (GaVsHard/Soft/Base, OcSuppression,
            // ParkedHit) resolve LIVE so they are STORED on the WeaponProfile rider fields, but NO combat path
            // consumes them yet — that downstream wiring is the per-rider "extra plumbing" in Claude_TODO.md.
            // Capability / interaction hooks (avoid-GAD, loiter re-attack, the conditional maluses) stay Dormant.
            Add(new TraitDef(WeaponTrait.HEAVY_AG_CANNON, TraitCategory.FixedWing,
                "A-10 GAU-8 / Su-25 GSh-30 — dedicated tank-busting gun.",
                Delta(ProfileStat.GA, 2), Delta(ProfileStat.GaVsHard, 2)));
            Add(new TraitDef(WeaponTrait.AT_GUIDED_AIR, TraitCategory.FixedWing,
                "AGM-65 Maverick / Kh-25/29 / Vikhr — air-launched precision AT.",
                Delta(ProfileStat.GA, 3), Delta(ProfileStat.GaVsHard, 1)));
            Add(new TraitDef(WeaponTrait.CAS_ARMORED, TraitCategory.FixedWing,
                "A-10 'bathtub' / Su-25 armoured tub — CAS survivability.", Delta(ProfileStat.SUR, 2)));
            Add(new TraitDef(WeaponTrait.LOITER_PERSISTENCE, TraitCategory.FixedWing,
                "Slow-CAS endurance — multiple passes (DORMANT → CAS re-attack hook).",
                Cap(WeaponCapability.LoiterReattack, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.LOW_LEVEL_STRAFE, TraitCategory.FixedWing,
                "Treetop gun/rocket runs — GA+1 live; exposure malus (defender GAT +1) DORMANT.",
                Delta(ProfileStat.GA, 1)));
            Add(new TraitDef(WeaponTrait.HEAVY_PAYLOAD, TraitCategory.FixedWing,
                "Large bomb load (Su-24 / F-111 / Tornado).", Delta(ProfileStat.OL, 3)));
            Add(new TraitDef(WeaponTrait.STANDOFF_PGM, TraitCategory.FixedWing,
                "Kh-25/59, AGM-130 — stand-off precision; PR+2 live, −1 incoming GAT DORMANT.",
                Delta(ProfileStat.PR, 2)));
            Add(new TraitDef(WeaponTrait.CARPET_BOMBING, TraitCategory.FixedWing,
                "Saturation dumb-bomb stick — area anti-soft.",
                Delta(ProfileStat.GA, 1), Delta(ProfileStat.GaVsSoft, 3)));
            Add(new TraitDef(WeaponTrait.STRATEGIC_PAYLOAD, TraitCategory.FixedWing,
                "Maximum tonnage (Tu-22 / Tu-95 / B-52) → OL XLarge.", Delta(ProfileStat.OL, 4)));
            Add(new TraitDef(WeaponTrait.STANDOFF_CRUISE_MISSILE, TraitCategory.FixedWing,
                "Kh-22 heavy supersonic cruise missile — massive warhead (GA+3, LIVE) on a strike that also ignores " +
                "target GAD (avoid-GAD capability DORMANT → hook). Tu-22/Tu-22M3 only.",
                Delta(ProfileStat.GA, 3), Cap(WeaponCapability.IgnoreAirDefense, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.HIGH_ALTITUDE_BOMBER, TraitCategory.FixedWing,
                "High-level bombing — SUR+2 vs gun-AAA/MANPADS, malus vs radar SAM; conditional, DORMANT → altitude/AD layer.",
                Delta(ProfileStat.SUR, 2, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.MULTIROLE_STRIKE, TraitCategory.FixedWing,
                "Strike-fighter A2G fit — the dual-role lever off the fighter GA-2 floor.", Delta(ProfileStat.GA, 4)));
            Add(new TraitDef(WeaponTrait.RUNWAY_CRATERING, TraitCategory.FixedWing,
                "Durandal / JP233 / BetAB — anti-airfield denial (rider stored).",
                Delta(ProfileStat.OcSuppression, 20)));
            Add(new TraitDef(WeaponTrait.BUNKER_PENETRATOR, TraitCategory.FixedWing,
                "BLU-109/GBU-28 / KAB-1500 — hardened-target penetration (rider stored).",
                Delta(ProfileStat.GaVsBase, 4)));
            Add(new TraitDef(WeaponTrait.RAMP_STRIKE, TraitCategory.FixedWing,
                "CBU-87 / RBK-500 cluster vs parked aircraft (rider stored).",
                Delta(ProfileStat.ParkedHit, 1)));
            #endregion

            #region §10 Air Defense (SAM / AAA)
            Add(new TraitDef(WeaponTrait.RADAR_GUIDED_GUN, TraitCategory.AirDefense,
                "ZSU-23-4 / Gepard / Tunguska — radar-directed flak.", Delta(ProfileStat.GAT, 2)));
            Add(new TraitDef(WeaponTrait.GUN_MISSILE_COMBO, TraitCategory.AirDefense,
                "2K22 Tunguska gun+SAM.", Delta(ProfileStat.GAT, 2), Delta(ProfileStat.IR, 2)));
            Add(new TraitDef(WeaponTrait.SARH_LONG_RANGE, TraitCategory.AirDefense,
                "SA-2/3/6, Hawk, Patriot — radar-illuminated long reach.", Delta(ProfileStat.GAT, 3)));
            Add(new TraitDef(WeaponTrait.IR_HOMING, TraitCategory.AirDefense,
                "SA-9/13, Chaparral — fire-and-forget, no warning.",
                Delta(ProfileStat.GAT, 1), Cap(WeaponCapability.FireAndForget)));
            Add(new TraitDef(WeaponTrait.COMMAND_GUIDANCE, TraitCategory.AirDefense,
                "SA-2/3, Roland, Rapier — jammable (jam DORMANT).", Delta(ProfileStat.GAT, 2)));
            Add(new TraitDef(WeaponTrait.TVM_GUIDANCE, TraitCategory.AirDefense,
                "Patriot — track-via-missile, high-end.", Delta(ProfileStat.GAT, 4)));
            Add(new TraitDef(WeaponTrait.MOBILE_SHOOT_SCOOT, TraitCategory.AirDefense,
                "Tracked SA-6/8/11 — relocate after firing.", Cap(WeaponCapability.ShootScoot)));
            Add(new TraitDef(WeaponTrait.WILD_WEASEL, TraitCategory.AirDefense,
                "SEAD: HARM/Shrike/Kh-58 (folds IsWildWeasel → WW class).",
                Cap(WeaponCapability.WildWeasel)));
            #endregion

            #region §11 Engineering & Utility
            Add(new TraitDef(WeaponTrait.BRIDGELAYER, TraitCategory.Engineering,
                "MTU/AVLB/Biber — span a river edge.", Cap(WeaponCapability.Bridgelayer)));
            Add(new TraitDef(WeaponTrait.MINE_PLOW_ROLLER, TraitCategory.Engineering,
                "DORMANT → mined-hex (7.5.5.8): clear/ignore a mined hex.",
                Cap(WeaponCapability.MineClearing, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.FIELD_FORTIFICATION, TraitCategory.Engineering,
                "Combat-engineer entrench — build IsFort (9.8.6).",
                Cap(WeaponCapability.FieldFortification)));
            Add(new TraitDef(WeaponTrait.RIVER_ASSAULT, TraitCategory.Engineering,
                "Assault-crossing engineers — ×1.4 attack across river (7.5.6.9.6).",
                Cap(WeaponCapability.RiverAssault)));
            Add(new TraitDef(WeaponTrait.OBSTACLE_BREACH, TraitCategory.Engineering,
                "DORMANT → obstacle layer: breach wire/AT ditches.",
                Cap(WeaponCapability.ObstacleBreach, EffectStatus.Dormant)));
            Add(new TraitDef(WeaponTrait.ARV_RECOVERY, TraitCategory.Engineering,
                "DORMANT → repair retired (15.8): field-repair adjacent unit.",
                Cap(WeaponCapability.FieldRepair, EffectStatus.Dormant)));
            #endregion

            #region §12 Universal / economy
            Add(new TraitDef(WeaponTrait.NON_COMBATANT, TraitCategory.Economy,
                "Trucks/unarmed transport — cannot initiate attack (folds IsAttackCapable=false).",
                Cap(WeaponCapability.NonCombatant)));
            Add(new TraitDef(WeaponTrait.ROCKET_ARTILLERY, TraitCategory.Economy,
                "MRL salvo — +1 CombatAction (folds IsDoubleFire/ROC, per 7.14).",
                Cap(WeaponCapability.RocketArtillery)));
            Add(new TraitDef(WeaponTrait.EXPORT_DOWNGRADE, TraitCategory.Economy,
                "Monkey-model export — thinner armour, simpler FCS.",
                Delta(ProfileStat.HD, -2), Delta(ProfileStat.SD, -1), Icm(0.9f)));
            Add(new TraitDef(WeaponTrait.RECON_FRAGILE, TraitCategory.Economy,
                "Scout 'don't brawl' discouragement (Appendix W R6) — preserves the old recon ICM penalty. " +
                "Magnitude ×0.6 is the doc's proposed value (still open to confirm).",
                Icm(0.6f)));
            #endregion

            return d;
        }

        #endregion // Catalogue
    }
}
