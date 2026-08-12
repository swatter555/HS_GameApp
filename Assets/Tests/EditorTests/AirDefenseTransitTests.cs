using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// D2 — the TRANSIT air-defence path: who may shoot at an aircraft crossing the map (§11.8), the
    /// fixed-wing/helicopter detection-roll split (§5.13.2.4 vs §5.13.3.2), and overhead fire (the ratified
    /// GAD rule). Covers the caller-side gates the pure resolvers deliberately left out — the §11.8.2 GAT
    /// threshold, the §11.8.8 posture gate, the §11.8.3 shot budget and the §11.8.6 anti-dogpile cap.
    ///
    /// ⚠ The pure damage primitives are covered elsewhere (<c>AirDefenseFireResolverTests</c>,
    /// <c>HeloTransitStandCheckTests</c>, <c>AirAmbushCheckTests</c>). This fixture is about ELIGIBILITY and
    /// WIRING, which is where D2's defects lived: before 2026-08-11 the path filtered on a hardcoded
    /// SAM/AAA classification list, rolled the fixed-wing detection check for helicopters too, and then
    /// resolved the whole engagement with <c>UnityEngine.Random.Range(0, 2)</c>.
    /// </summary>
    [TestFixture]
    public class AirDefenseTransitTests : BaseTestFixture
    {
        private const int ROW_Y = 5;

        #region Helpers

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        public override void SetUp()
        {
            base.SetUp();
            GameManager.ClearAll();
            GameManager.InvalidateOccupancy();
            GameDataManager.CurrentHexMap = CreateClearMap();
        }

        private HexMap CreateClearMap(int width = 20, int height = 12)
        {
            var map = new HexMap("TestMap", MapConfig.Small);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var hex = new HexTile(new Vector2Int(x, y));
                    hex.SetTerrain(TerrainType.Clear);
                    map.SetHexAt(hex);
                }
            map.BuildNeighborRelationships();
            return map;
        }

        private CombatUnit Unit(string name, UnitClassification cls, UnitRole role, WeaponType deployed,
            Side side, int x, ExperienceLevel xp = ExperienceLevel.Trained)
        {
            var u = new CombatUnit(name, cls, role, side,
                side == Side.Player ? Nationality.USSR : Nationality.MJ);
            u.EquipmentBays.InitializeEquipmentBays(name, deployed, WeaponType.NONE, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(xp);
            u.SetPosition(new Position2D(x, ROW_Y));
            GameManager.RegisterCombatUnit(u);
            return u;
        }

        /// <summary>An AI-side S-75 battery — the reference air-defence firer.</summary>
        private CombatUnit Sam(int x) =>
            Unit("SAM", UnitClassification.SAM, UnitRole.AirDefenseArea, WeaponType.SAM_S75_SV, Side.AI, x);

        /// <summary>A player attack helicopter — the reference transiting helo.</summary>
        private CombatUnit Helo(int x, ExperienceLevel xp = ExperienceLevel.Trained) =>
            Unit("Helo", UnitClassification.HELO, UnitRole.GroundCombat, WeaponType.HEL_MI24V_SV,
                Side.Player, x, xp);

        /// <summary>A player Su-25 — the reference transiting fixed-wing.</summary>
        private CombatUnit Jet(int x, ExperienceLevel xp = ExperienceLevel.Trained) =>
            Unit("Jet", UnitClassification.ATT, UnitRole.AirGroundAttack, WeaponType.ATT_SU25_SV,
                Side.Player, x, xp);

        private static bool Finds(List<TransitAirDefenseContact> contacts, CombatUnit firer)
        {
            foreach (var c in contacts)
                if (c.Firer == firer) return true;
            return false;
        }

        /// <summary>The hex a firer sits on, which is always inside its own engagement envelope.</summary>
        private static Position2D At(CombatUnit u) => u.MapPos;

        /// <summary>
        /// The firer's engagement reach, read the way the scan reads it so a profile change moves both.
        /// </summary>
        private static int Reach(CombatUnit firer)
        {
            int r = Mathf.FloorToInt(firer.ActivePrimaryRange);
            return r <= 0 ? 2 : r;
        }

        /// <summary>
        /// An UNOCCUPIED hex adjacent to <paramref name="firer"/> — inside its envelope, but not its own hex,
        /// so a test of ranged air-defence fire cannot accidentally be measuring overhead fire instead.
        /// </summary>
        private static Position2D InRangeEmptyHex(CombatUnit firer)
        {
            Assert.GreaterOrEqual(Reach(firer), 1, "precondition: the firer reaches at least an adjacent hex");
            return new Position2D(firer.MapPos.IntX - 1, ROW_Y);
        }

        #endregion // Helpers

        #region §11.8.2 — eligibility is the CLASSIFICATION

        [Test]
        public void FindTransitAirDefense_FindsEachDedicatedAirDefenceType()
        {
            /* The four dedicated types, and these four only, interdict a transit at range (§11.8.2). */
            var helo = Helo(2);
            var types = new[]
            {
                (UnitClassification.SAM,   WeaponType.SAM_S75_SV),
                (UnitClassification.SPSAM, WeaponType.SPSAM_2K12_SV),
                (UnitClassification.AAA,   WeaponType.AAA_GEN_SV),
                (UnitClassification.SPAAA, WeaponType.SPAAA_ZSU23_SV),
            };

            // Spaced along the row so each battery holds its own hex.
            for (int i = 0; i < types.Length; i++)
            {
                var (cls, profile) = types[i];
                Assert.IsTrue(GameData.IsAirDefenseClassification(cls), $"{cls} is an air-defence type");

                var battery = Unit($"{cls}", cls, UnitRole.AirDefenseArea, profile, Side.AI, 6 + i * 2);
                Assert.IsTrue(Finds(SpottingService.FindTransitAirDefense(helo, At(battery)), battery),
                    $"{cls} engages a transiting helo");
            }
        }

        [Test]
        public void FindTransitAirDefense_ExcludesANonAirDefenceClassification()
        {
            var art = Unit("Guns", UnitClassification.ART, UnitRole.GroundCombatIndirect,
                WeaponType.ART_HEAVY_SV, Side.AI, 6);
            var helo = Helo(2);

            Assert.IsFalse(GameData.IsAirDefenseClassification(art.Classification));
            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, At(art)), art),
                "an artillery regiment does not interdict aircraft, whatever its GAT says");
        }

        [Test]
        public void ManpadsInfantry_HasRealGat_ButIsStillNotAnAirDefenceUnit()
        {
            /* ⚠ THE TEST THAT PINS BOB'S 2026-08-11 RULING, AND THE REASON FOR IT. `MANPADS_BASIC` floors
             * infantry GAT at EXACTLY 6, so a `GAT ≥ 6` eligibility test would admit essentially every line
             * regiment on both sides — the opposite of the intent. The stat is real and stays real (every
             * unit carries every stat, or a Δ cannot be formed); it is simply not the question being asked.
             * WHO MAY SHOOT is the classification.
             *
             * ⚠ Infantry organic anti-air is not lost — it is the §11.8.11 overhead GAD rule. What infantry
             * may not do is reach UP at range to interdict a transit. */
            var inf = Unit("Riflemen", UnitClassification.INF, UnitRole.GroundCombat,
                WeaponType.INF_REG_SV, Side.AI, 6);
            var helo = Helo(2);

            Assert.GreaterOrEqual(inf.ActiveGroundAirAttack, GameData.GAT_INTERDICT_THRESHOLD,
                "MANPADS_BASIC floors infantry GAT at 6 — which is exactly why GAT cannot be the gate");
            Assert.IsFalse(GameData.IsAirDefenseClassification(inf.Classification),
                "infantry are not an air-defence type");

            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, At(inf)), inf),
                "MANPADS infantry never interdict a transiting aircraft at range");
        }

        [Test]
        public void FindTransitAirDefense_IgnoresFriendlyAndDestroyedUnits()
        {
            var friendly = Unit("Ours", UnitClassification.SAM, UnitRole.AirDefenseArea,
                WeaponType.SAM_S75_SV, Side.Player, 6);
            var helo = Helo(2);

            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, At(friendly)), friendly),
                "a battery never engages its own side's aircraft");
        }

        [Test]
        public void FindTransitAirDefense_EngagesToItsReachAndNoFurther()
        {
            /* Driven off the profile's own stated range rather than a literal, so retuning the S-75 moves the
             * test with it instead of failing for the wrong reason. */
            var sam = Sam(12);
            var helo = Helo(2);
            int reach = Reach(sam);

            var inside = new Position2D(sam.MapPos.IntX - reach, ROW_Y);
            var outside = new Position2D(sam.MapPos.IntX - (reach + 1), ROW_Y);

            Assert.IsTrue(Finds(SpottingService.FindTransitAirDefense(helo, inside), sam),
                "at the edge of the envelope the battery engages");
            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, outside), sam),
                "one hex beyond it does not");
        }

        #endregion // §11.8.2

        #region §11.8.8 — the towed posture gate

        [Test]
        public void PostureAllowsOpportunityFire_BarsLimberedAndEmbarkedOnly()
        {
            /* ⚠ The doc states this as a towed/self-propelled CLASSIFICATION split; the code states it as a
             * posture test, because a self-propelled type has no Mobile bay and can never be limbered. The
             * two agree today — this pins the posture half so the equivalence is checkable. */
            Assert.IsTrue(GameData.PostureAllowsOpportunityFire(DeploymentPosition.Deployed));
            Assert.IsTrue(GameData.PostureAllowsOpportunityFire(DeploymentPosition.HastyDefense));
            Assert.IsTrue(GameData.PostureAllowsOpportunityFire(DeploymentPosition.Entrenched));
            Assert.IsTrue(GameData.PostureAllowsOpportunityFire(DeploymentPosition.Fortified));

            Assert.IsFalse(GameData.PostureAllowsOpportunityFire(DeploymentPosition.Mobile),
                "§11.8.8 — limbered guns and packed radars do not shoot");
            Assert.IsFalse(GameData.PostureAllowsOpportunityFire(DeploymentPosition.Embarked),
                "§11.8.8 — nothing op-fires from Embarked, self-propelled or not");
        }

        [Test]
        public void FindTransitAirDefense_ExcludesALimberedBattery()
        {
            var sam = Sam(6);
            var helo = Helo(2);

            sam.SetDeploymentPosition(DeploymentPosition.Mobile);

            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, At(sam)), sam),
                "a battery on the road cannot engage (§11.8.8)");

            sam.SetDeploymentPosition(DeploymentPosition.Entrenched);

            Assert.IsTrue(Finds(SpottingService.FindTransitAirDefense(helo, At(sam)), sam),
                "dug in, it engages again");
        }

        #endregion // §11.8.8

        #region §11.8.3 / §11.8.6 — shot budget and anti-dogpile

        [Test]
        public void FindTransitAirDefense_ExcludesABatteryOutOfOpportunityActions()
        {
            var sam = Sam(6);
            var helo = Helo(2);

            while (sam.OpportunityActions.Current >= 1)
                Assert.IsTrue(sam.PerformOpportunityAction(), "draining the §11.8.3 budget");

            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, At(sam)), sam),
                "no shots left in the turn's budget → no engagement");
        }

        [Test]
        public void FindTransitAirDefense_OneShotPerAircraftPerTurn_ButOtherAircraftStillEngaged()
        {
            /* §11.8.6 — a battery with budget to spare may engage a SECOND aircraft, never the same one
             * twice. Without this a helicopter crossing several hexes of one envelope is engaged at each,
             * and accumulating Shock (§11.8.9) breaks the sortie on geometry alone. */
            var sam = Sam(6);
            var helo = Helo(2);
            var jet = Jet(3);

            Assert.IsTrue(Finds(SpottingService.FindTransitAirDefense(helo, At(sam)), sam),
                "precondition: the battery would engage this helo");

            sam.MarkAircraftEngaged(helo.UnitID);

            Assert.IsFalse(Finds(SpottingService.FindTransitAirDefense(helo, At(sam)), sam),
                "§11.8.6 — one shot per aircraft per turn");
            Assert.IsTrue(Finds(SpottingService.FindTransitAirDefense(jet, At(sam)), sam),
                "§11.8.6 caps per AIRCRAFT, not per turn — a different target is still engaged");
        }

        [Test]
        public void RefreshAllActions_ClearsTheAntiDogpileRecord()
        {
            var sam = Sam(6);
            var helo = Helo(2);

            sam.MarkAircraftEngaged(helo.UnitID);
            Assert.IsTrue(sam.HasEngagedAircraftThisTurn(helo.UnitID));

            sam.RefreshAllActions();

            Assert.IsFalse(sam.HasEngagedAircraftThisTurn(helo.UnitID),
                "the per-turn record resets with the budget it guards");
        }

        #endregion // §11.8.3 / §11.8.6

        #region §11.8.4 — a SPOTTED battery fires too

        [Test]
        public void FindTransitAirDefense_IncludesASpottedBattery_FlaggedAsNotAnAmbush()
        {
            /* ⚠ The 2026-08-11 widening. §11.8.4 makes the firing opportunity automatic for any eligible
             * battery; §6.10 air AMBUSH is the narrower case where the firer was still unspotted. The old
             * code required Level0, so a SAM the player had already located was completely harmless —
             * exactly backwards, since flying past a KNOWN SAM should be the informed risk. */
            var sam = Sam(6);
            var helo = Helo(2);
            sam.SetSpottedLevel(SpottedLevel.Level3);

            var contacts = SpottingService.FindTransitAirDefense(helo, At(sam));

            Assert.IsTrue(Finds(contacts, sam), "a spotted battery still engages (§11.8.4)");
            foreach (var c in contacts)
                if (c.Firer == sam)
                    Assert.IsFalse(c.WasUnspotted, "already located → not the §6.10 ambush case, no detection roll");
        }

        [Test]
        public void FindTransitAirDefense_FlagsAnUnspottedBatteryAsTheAmbushCase()
        {
            var sam = Sam(6);
            var helo = Helo(2);
            sam.SetSpottedLevel(SpottedLevel.Level0);

            foreach (var c in SpottingService.FindTransitAirDefense(helo, At(sam)))
                if (c.Firer == sam)
                    Assert.IsTrue(c.WasUnspotted, "unspotted → the §6.10 case a fixed-wing mover may detect");
        }

        #endregion // §11.8.4

        #region The detection-roll split (§5.13.2.4 vs §5.13.3.2)

        [Test]
        public void ResolveTransitFire_Helo_TakesTheHitWithNoDetectionRoll()
        {
            /* ⚠ THE REGRESSION THIS PASS EXISTS TO PREVENT. §5.13.2.4: helicopters get NO 1d6 detection roll
             * — they take the hit if the battery has shots available. An ELITE helo is the sharp case: its
             * detection threshold would be 1 on 1d6, i.e. a guaranteed avert, so if the roll were still being
             * applied this test would see an unspent battery. Their escape is the §11.8.9 stand check AFTER
             * the damage, never a chance to dodge it. */
            var sam = Sam(6);
            var helo = Helo(2, ExperienceLevel.Elite);
            sam.SetSpottedLevel(SpottedLevel.Level0);

            float budget0 = sam.OpportunityActions.Current;

            // An EMPTY hex inside the battery's envelope, so nothing here can be confused with overhead fire.
            MovementController.ResolveTransitFire(helo, InRangeEmptyHex(sam), isFixedWing: false,
                new HashSet<string>(), hpLostThisMove: 0);

            Assert.Less(sam.OpportunityActions.Current, budget0,
                "the battery fired — an elite HELO gets no roll to avert it");
            Assert.AreEqual(SpottedLevel.Level4, sam.SpottedLevel,
                "§11.8.4 / §12.4.9.1 — firing from the open identifies it, not merely locates it");
        }

        [Test]
        public void ResolveTransitFire_FixedWing_EliteDetectsAndAvertsTheShot()
        {
            /* §5.13.3.2 + §6.10.4 — an ELITE aircraft detects on a threshold of 1, so on 1d6 it always spots
             * the battery first. The shot is averted, no action is spent, and the battery is revealed at
             * CONTACT only (§12.4.8): you found the radar, it did not shoot at you. */
            var sam = Sam(6);
            var jet = Jet(2, ExperienceLevel.Elite);
            sam.SetSpottedLevel(SpottedLevel.Level0);

            float budget0 = sam.OpportunityActions.Current;

            MovementController.ResolveTransitFire(jet, InRangeEmptyHex(sam), isFixedWing: true,
                new HashSet<string>(), hpLostThisMove: 0);

            Assert.AreEqual(budget0, sam.OpportunityActions.Current,
                "detection averts the shot, so no §11.8.3 budget is spent");
            Assert.AreEqual(SpottedLevel.Level1, sam.SpottedLevel,
                "§12.4.8 — detected without being fired at is a CONTACT, not an identification");
        }

        #endregion // The detection-roll split

        #region Overhead fire — the GAD rule

        [Test]
        public void BuildOverheadFireLane_IsGadVersusGad()
        {
            /* ⚠ GAD is used as the ATTACK value here on purpose — it already encodes how much organic
             * anti-air a regiment carries. Do not "correct" it to GAT; GAT is the separate §11.8 dedicated
             * path. The DEFENDING side is the helo's own GAD (§7A.14 — its air stats are zero). */
            var tanks = Unit("Tanks", UnitClassification.TANK, UnitRole.GroundCombat,
                WeaponType.TANK_T72A_SV, Side.AI, 6);
            var helo = Helo(6);

            var lane = CombatResolver.BuildOverheadFireLane(tanks, helo);

            Assert.AreEqual(tanks.ActiveGroundAirDefense, lane.FirerAttack,
                "the ground unit attacks on its GAD");
            Assert.AreEqual(helo.ActiveGroundAirDefense, lane.TargetDefense,
                "the helo defends on its GAD (§7A.14)");
            Assert.IsFalse(lane.FirerIsAir, "the firer is a ground unit → GroundBalanceMod (§7.7.10)");
            Assert.IsTrue(lane.BypassTerrainBlock, "the target is aloft — no terrain term");
        }

        [Test]
        public void ResolveTransitFire_HeloOverflight_EngagesTheUnitDirectlyBelow()
        {
            var tanks = Unit("Tanks", UnitClassification.TANK, UnitRole.GroundCombat,
                WeaponType.TANK_T72A_SV, Side.AI, 6);
            var helo = Helo(6);
            var engaged = new HashSet<string>();

            MovementController.ResolveTransitFire(helo, At(tanks), isFixedWing: false, engaged, 0);

            Assert.IsTrue(engaged.Contains(tanks.UnitID),
                "a helo crossing the hex is fired on by whatever is under it");
        }

        [Test]
        public void ResolveTransitFire_OverheadFire_IsSameHexOnly()
        {
            /* ⚠ THE NARROWNESS IS THE SAFETY PROPERTY. Same hex, never adjacency and never a radius:
             * overflight is avoidable by ROUTING, which makes the flight path a decision instead of making
             * flight suicidal. A radius version would break every sortie on accumulated Shock alone. */
            var tanks = Unit("Tanks", UnitClassification.TANK, UnitRole.GroundCombat,
                WeaponType.TANK_T72A_SV, Side.AI, 6);
            var helo = Helo(4);
            var engaged = new HashSet<string>();

            MovementController.ResolveTransitFire(helo, new Position2D(5, ROW_Y), isFixedWing: false, engaged, 0);

            Assert.IsFalse(engaged.Contains(tanks.UnitID),
                "adjacent is not overhead — only the unit's own hex triggers the GAD rule");
        }

        [Test]
        public void ResolveTransitFire_FixedWingOverflight_IsExempt()
        {
            /* Fixed-wing flies too high for organic weapons; only the §11.8 dedicated GAT path reaches it. */
            var tanks = Unit("Tanks", UnitClassification.TANK, UnitRole.GroundCombat,
                WeaponType.TANK_T72A_SV, Side.AI, 6);
            var jet = Jet(6);
            var engaged = new HashSet<string>();

            MovementController.ResolveTransitFire(jet, At(tanks), isFixedWing: true, engaged, 0);

            Assert.IsFalse(engaged.Contains(tanks.UnitID),
                "the GAD rule is helicopter-only");
        }

        [Test]
        public void ResolveTransitFire_OverheadFire_RespectsTheAntiDogpileSet()
        {
            /* Shared with ambush: a regiment that has already sprung on this helo does not also shoot at it
             * overhead — "one engagement per unit per move". */
            var tanks = Unit("Tanks", UnitClassification.TANK, UnitRole.GroundCombat,
                WeaponType.TANK_T72A_SV, Side.AI, 6);
            var helo = Helo(6);
            float hp0 = helo.HitPoints.Current;

            var engaged = new HashSet<string> { tanks.UnitID };
            MovementController.ResolveTransitFire(helo, At(tanks), isFixedWing: false, engaged, 0);

            Assert.AreEqual(hp0, helo.HitPoints.Current,
                "already engaged this move → no second bite");
        }

        #endregion // Overhead fire
    }
}
