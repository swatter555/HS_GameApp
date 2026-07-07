using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Models.Combat;
using HammerAndSickle.Models.Map;
using HammerAndSickle.Services;
using NUnit.Framework;
using UnityEngine;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M14 safe-slice tests: the leader→combat channels wired 2026-07-03.
    /// (1) Leader.StandValueContribution (§14.13) — +1 per distinct unlocked tier across the 7 Doctrines
    ///     + Combined Arms, cap +3 — and its consumption by CombatResolver.BuildDefenderStand.
    /// (2) The doctrine combat bonuses as Δ-side stat deltas (+2, §14.10.4) on the lane accessors.
    /// (3) The silhouette re-homes: Underground Bunker = enemy SpottedLevel capped at 3 (§14.8.7);
    ///     Superior Camouflage = enemy spotting range −1 at the §12.3.10 comparison (§14.9.4).
    /// </summary>
    [TestFixture]
    public class LeaderSkillCombatTests : BaseTestFixture
    {
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

        #region Helpers

        private HexMap CreateClearMap(int width = 16, int height = 12)
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

        private Leader MakeLeader(Side side = Side.Player, int reputation = 5000,
            CommandAbility command = CommandAbility.Average)
        {
            var nat = side == Side.Player ? Nationality.USSR : Nationality.MJ;
            var leader = new Leader("Test Leader", side, nat, command);
            leader.AwardReputation(reputation);
            GameManager.RegisterLeader(leader);
            return leader;
        }

        /// <summary>Unlocks the Leadership spine up to SeniorGrade (T1 + Senior promotion).</summary>
        private static void PromoteToSenior(Leader leader)
        {
            Assert.IsTrue(leader.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1), "Leadership T1");
            Assert.IsTrue(leader.UnlockSkill(LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion), "Senior promotion");
        }

        /// <summary>Unlocks the Leadership spine up to TopGrade (T1 → Senior → T3 → Top).</summary>
        private static void PromoteToTop(Leader leader)
        {
            PromoteToSenior(leader);
            Assert.IsTrue(leader.UnlockSkill(LeadershipFoundation.SeniorOfficerTraining_CommandTier2), "Leadership T3");
            Assert.IsTrue(leader.UnlockSkill(LeadershipFoundation.PromotionToTopGrade_TopPromotion), "Top promotion");
        }

        private CombatUnit BuildUnit(UnitClassification cls, WeaponType deployed, Side side, Position2D pos)
        {
            var nat = side == Side.Player ? Nationality.USSR : Nationality.MJ;
            var u = new CombatUnit("U", cls, UnitRole.GroundCombat, side, nat);
            u.RegimentProfile.InitializeRegimentProfile("U", RegimentProfileType.DEP,
                WeaponType.NONE, deployed, WeaponType.NONE);
            u.SetDeploymentPosition(DeploymentPosition.Deployed);
            u.SetExperienceLevel(ExperienceLevel.Trained);
            u.SetEfficiencyLevel(EfficiencyLevel.FullOperations);
            u.SetPosition(pos);
            u.SetSpottedLevel(SpottedLevel.Level0); // ctor defaults to Level1 — reset like the other suites do
            u.MovementPoints.SetMax(12);
            u.MovementPoints.SetCurrent(12);
            u.DaysSupply.SetMax(5f);
            u.DaysSupply.SetCurrent(5f);
            GameManager.RegisterCombatUnit(u);
            return u;
        }

        private CombatUnit Tank(Side side = Side.Player, int x = 5, int y = 5) =>
            BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV, side, new Position2D(x, y));

        private CombatUnit Infantry(Side side, int x, int y) =>
            BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV, side, new Position2D(x, y));

        private void Assign(Leader leader, CombatUnit unit) =>
            Assert.IsTrue(GameManager.AssignLeaderToUnit(leader.LeaderID, unit.UnitID), "Leader assignment");

        #endregion // Helpers

        #region StandValueContribution (§14.13)

        [Test]
        public void StandValue_NoSkills_IsZero()
        {
            var leader = MakeLeader();
            Assert.AreEqual(0, leader.StandValueContribution);
        }

        [Test]
        public void StandValue_FoundationTiers_DoNotContribute()
        {
            // Leadership is a Foundation branch — excluded per §14.13.2.
            var leader = MakeLeader();
            PromoteToTop(leader);

            Assert.AreEqual(0, leader.StandValueContribution,
                "Foundation tiers must not contribute to the §14.13 Leader_mod");
        }

        [Test]
        public void StandValue_DoctrineTiers_CountOnePerTier()
        {
            var leader = MakeLeader();
            PromoteToSenior(leader);

            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1");
            Assert.AreEqual(1, leader.StandValueContribution, "One doctrine tier → +1");

            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.HullDownExpert_HardDefense), "Armored T2");
            Assert.AreEqual(2, leader.StandValueContribution, "Two doctrine tiers → +2");

            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.PursuitDoctrine_Breakthrough), "Armored T3");
            Assert.AreEqual(3, leader.StandValueContribution, "Three doctrine tiers → +3");
        }

        [Test]
        public void StandValue_SameTierSkills_CountOnce()
        {
            // Combined Arms has multiple T4 nodes — the same (branch, tier) counts once (§14.13.3).
            var leader = MakeLeader();
            PromoteToTop(leader);

            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.AviationAssets_SpottingRange), "CA T4 #1");
            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.ExpertStaff_MovementAction), "CA T4 #2");

            Assert.AreEqual(1, leader.StandValueContribution,
                "Two skills in the same tier of the same branch count as ONE tier");
        }

        [Test]
        public void StandValue_CapsAtThree()
        {
            // Armored T1+T2+T3 (3 tiers) + Combined Arms T4 (1 tier) = 4 distinct tiers → clamped to +3.
            var leader = MakeLeader();
            PromoteToTop(leader);

            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1");
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.HullDownExpert_HardDefense), "Armored T2");
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.PursuitDoctrine_Breakthrough), "Armored T3");
            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.AviationAssets_SpottingRange), "CA T4");

            Assert.AreEqual(GameData.LEADER_STAND_MOD_CAP, leader.StandValueContribution,
                "Contribution is hard-capped at LEADER_STAND_MOD_CAP (+3)");
        }

        [Test]
        public void Doctrine_OwnBranchProgresses_OtherDoctrineStillBlocked()
        {
            // Regression guard for the IsBranchAvailable fix (2026-07-03): starting a doctrine must not
            // dead-end its own deeper tiers, while OTHER doctrines stay mutually excluded.
            var leader = MakeLeader();
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1");

            Assert.IsTrue(leader.IsBranchAvailable(SkillBranch.ArmoredDoctrine),
                "The started doctrine remains available for its own deeper tiers");
            Assert.IsFalse(leader.IsBranchAvailable(SkillBranch.InfantryDoctrine),
                "A second doctrine branch is mutually excluded");
            Assert.IsFalse(leader.UnlockSkill(InfantryDoctrine.InfantryAssaultTactics_SoftAttack),
                "Unlocking into a second doctrine is rejected");
        }

        [Test]
        public void Resolver_DefenderStand_ReadsLeaderMod()
        {
            var attacker = Tank(Side.AI, 5, 5);
            var defender = Tank(Side.Player, 6, 5);

            var leader = MakeLeader();
            PromoteToSenior(leader);
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1");
            Assign(leader, defender);

            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };
            StandValueInput stand = CombatResolver.BuildDefenderStand(attacker, defender, ctx, flank: false);

            Assert.AreEqual(1, stand.LeaderMod, "BuildDefenderStand reads the defender leader's §14.13 contribution");
        }

        #endregion // StandValueContribution

        #region Doctrine Δ-deltas (§14.10.4, +2)

        [Test]
        public void Delta_HardAttack_PlusTwo_HardAxisOnly()
        {
            var led = Tank(Side.Player, 5, 5);
            var unled = Tank(Side.Player, 7, 7);

            var leader = MakeLeader();
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1");
            Assign(leader, led);

            Assert.AreEqual(unled.GetAttackStatVsClass(TargetClass.Hard) + GameData.HARD_ATTACK_BONUS_VAL,
                led.GetAttackStatVsClass(TargetClass.Hard), "HardAttack delta applies on the hard axis");
            Assert.AreEqual(unled.GetAttackStatVsClass(TargetClass.Soft),
                led.GetAttackStatVsClass(TargetClass.Soft), "Soft axis is untouched by the HardAttack skill");
            Assert.AreEqual(2, GameData.HARD_ATTACK_BONUS_VAL, "Retuned magnitude is +2 (ratified 2026-07-03)");
        }

        [Test]
        public void Delta_HardDefense_PlusTwo_HardAxisOnly()
        {
            var led = Tank(Side.Player, 5, 5);
            var unled = Tank(Side.Player, 7, 7);

            var leader = MakeLeader();
            PromoteToSenior(leader);
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1");
            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.HullDownExpert_HardDefense), "Armored T2");
            Assign(leader, led);

            Assert.AreEqual(unled.GetDefenseStatVsClass(TargetClass.Hard) + GameData.HARD_DEFENSE_BONUS_VAL,
                led.GetDefenseStatVsClass(TargetClass.Hard), "HardDefense delta applies on the hard axis");
            Assert.AreEqual(unled.GetDefenseStatVsClass(TargetClass.Soft),
                led.GetDefenseStatVsClass(TargetClass.Soft), "Soft defense is untouched");
        }

        [Test]
        public void Delta_GatGad_PlusTwo_OnAirDefenseClass()
        {
            // Class gate (§14.8): AirDefense doctrine deltas apply only on SAM/SPSAM/AAA/SPAAA.
            var led = BuildUnit(UnitClassification.SPAAA, WeaponType.SPAAA_ZSU23_SV, Side.Player, new Position2D(5, 5));
            var unled = BuildUnit(UnitClassification.SPAAA, WeaponType.SPAAA_ZSU23_SV, Side.Player, new Position2D(7, 7));

            var leader = MakeLeader();
            PromoteToSenior(leader);
            Assert.IsTrue(leader.UnlockSkill(AirDefenseDoctrine.OffensiveAirDefense_AirAttack), "AirDef T1");
            Assert.IsTrue(leader.UnlockSkill(AirDefenseDoctrine.IntegratedAirDefenseSystem_AirDefense), "AirDef T2");
            Assign(leader, led);

            Assert.AreEqual(unled.ActiveGroundAirAttack + GameData.AIR_ATTACK_BONUS_VAL,
                led.ActiveGroundAirAttack, "GAT delta applies on an AD-class unit");
            Assert.AreEqual(unled.ActiveGroundAirDefense + GameData.AIR_DEFENSE_BONUS_VAL,
                led.ActiveGroundAirDefense, "GAD delta applies on an AD-class unit");
        }

        [Test]
        public void Delta_ClassGate_InfantryDoctrineOnTank_Inert()
        {
            // The pillar guard (§14.8 / 1.8.4): Infantry Assault Tactics on a TANK grants nothing.
            var led = Tank(Side.Player, 5, 5);
            var unled = Tank(Side.Player, 7, 7);

            var leader = MakeLeader();
            Assert.IsTrue(leader.UnlockSkill(InfantryDoctrine.InfantryAssaultTactics_SoftAttack), "Infantry T1");
            Assign(leader, led);

            Assert.AreEqual(unled.GetAttackStatVsClass(TargetClass.Soft),
                led.GetAttackStatVsClass(TargetClass.Soft),
                "SoftAttack delta is class-gated — inert on a TANK");
        }

        #endregion // Doctrine Δ-deltas

        #region Silhouette re-homes (§14.8.7 / §14.9.4)

        [Test]
        public void Bunker_CapsEnemyIntelAtLevel3()
        {
            var led = Tank(Side.Player, 5, 5);

            var leader = MakeLeader();
            PromoteToSenior(leader);
            Assert.IsTrue(leader.UnlockSkill(IntelligenceDoctrine.EnhancedIntelligenceCollection_ImprovedGathering), "Intel T1");
            Assert.IsTrue(leader.UnlockSkill(IntelligenceDoctrine.ConcealedOperationsBase_UndergroundBunker), "Intel T2");
            Assign(leader, led);

            led.SetSpottedLevel(SpottedLevel.Level4);
            Assert.AreEqual(SpottedLevel.Level3, led.SpottedLevel,
                "Underground Bunker caps enemy intel at Level 3 (§14.8.7)");

            led.SetSpottedLevel(SpottedLevel.Level2);
            Assert.AreEqual(SpottedLevel.Level2, led.SpottedLevel, "Levels at or below the cap pass through");
        }

        [Test]
        public void Bunker_Unled_Level4Unaffected()
        {
            var unit = Tank(Side.Player, 5, 5);
            unit.SetSpottedLevel(SpottedLevel.Level4);
            Assert.AreEqual(SpottedLevel.Level4, unit.SpottedLevel, "No leader → no cap");
        }

        [Test]
        public void Camouflage_ReducesEnemySpottingRangeByOne()
        {
            // AI infantry spotter, ground range 2. Two AI-facing player targets, both at hex distance 2:
            // the camouflaged one is outside the reduced range (2−1=1) and stays invisible.
            var spotter = BuildUnit(UnitClassification.INF, WeaponType.INF_REG_SV, Side.Player, new Position2D(2, 5));

            var unledTarget = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV, Side.AI, new Position2D(4, 5));
            var ledTarget = BuildUnit(UnitClassification.TANK, WeaponType.TANK_T55A_SV, Side.AI, new Position2D(0, 5));

            var leader = MakeLeader(Side.AI);
            PromoteToTop(leader);
            Assert.IsTrue(leader.UnlockSkill(SpecialForcesSpecialization.TerrainExpert_TerrainMastery), "SF T4 #1");
            Assert.IsTrue(leader.UnlockSkill(SpecialForcesSpecialization.InfiltrationTactics_InfiltrationMovement), "SF T4 #2");
            Assert.IsTrue(leader.UnlockSkill(SpecialForcesSpecialization.SuperiorCamouflage_ConcealedPositions), "SF T4 #3");
            Assign(leader, ledTarget);

            Assert.AreEqual(1, ledTarget.EnemySpottingRangeReduction, "Superior Camouflage → enemy spotting −1 hex");

            SpottingService.RecomputeAllSpotting();

            Assert.AreEqual(SpottedLevel.Level1, unledTarget.SpottedLevel,
                "Unled target at distance 2 is inside the ground range (2) and gets spotted");
            Assert.AreEqual(SpottedLevel.Level0, ledTarget.SpottedLevel,
                "Camouflaged target at distance 2 is outside the reduced range (1) and stays invisible");
        }

        #endregion // Silhouette re-homes

        #region EffectiveCommand + Command Mitigation (§14.4.2 / §7.7.12)

        [Test]
        public void EffectiveCommand_TrainingClosesTalentGap_CappedAtGenius()
        {
            // Average (0) + full Leadership training (T1/T3/T5 = +3) = 3; a Genius stays 3 (cap).
            var average = MakeLeader();
            Assert.AreEqual(0, average.EffectiveCommand, "Untrained Average officer → 0");

            Assert.IsTrue(average.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1), "T1");
            Assert.AreEqual(1, average.EffectiveCommand, "Average + T1 training → 1");

            Assert.IsTrue(average.UnlockSkill(LeadershipFoundation.PromotionToSeniorGrade_SeniorPromotion), "Sr");
            Assert.IsTrue(average.UnlockSkill(LeadershipFoundation.SeniorOfficerTraining_CommandTier2), "T3");
            Assert.IsTrue(average.UnlockSkill(LeadershipFoundation.PromotionToTopGrade_TopPromotion), "Top");
            Assert.IsTrue(average.UnlockSkill(LeadershipFoundation.GeneralStaffTraining_CommandTier3), "T5");
            Assert.AreEqual(3, average.EffectiveCommand, "Average + full training → Genius-equivalent 3");

            var genius = MakeLeader(command: CommandAbility.Genius);
            Assert.AreEqual(3, genius.EffectiveCommand, "Genius is already at the cap");
            Assert.IsTrue(genius.UnlockSkill(LeadershipFoundation.JuniorOfficerTraining_CommandTier1), "Genius T1");
            Assert.AreEqual(3, genius.EffectiveCommand, "Training never exceeds the Genius cap");
        }

        [Test]
        public void Mitigation_LiftsHopelessMatchup_TowardEven()
        {
            // Δ −13 is Hopeless (always 0 HP). Command 3 lifts to Δ −10 (Difficult, 1d3−1) — a led unit
            // can now connect where an unled one never could.
            var lane = new LaneInput
            {
                FirerAttack = 1,
                TargetDefense = 14,
                FirerQualityMult = 1f,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
                FirerCommand = 3,
            };
            int dmg = CombatEngine.ResolveLane(lane, new QueueRollRandom(3)); // 1d3 → 3 → baseHP 2
            Assert.AreEqual(2, dmg, "Command Mitigation lifted Hopeless (auto-0) into a rollable band");
        }

        [Test]
        public void Mitigation_ClampsAtEvenFloor_NeverFavorable()
        {
            // Δ −2 with command 3 would naively reach +1; the clamp holds it at −1 → EVEN (1d8−1),
            // never Favorable (1d6+2). A scripted 8 yields 7 (Even) — Favorable would yield 10.
            var lane = new LaneInput
            {
                FirerAttack = 8,
                TargetDefense = 10,
                FirerQualityMult = 1f,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
                FirerCommand = 3,
            };
            int dmg = CombatEngine.ResolveLane(lane, new QueueRollRandom(8));
            Assert.AreEqual(7, dmg, "Δ−2 + command 3 clamps at Even's floor (1d8−1), not Favorable");
        }

        [Test]
        public void Mitigation_InertAtEvenOrBetter()
        {
            // Δ −1 is already Even — command adds nothing (same band, same dice).
            var lane = new LaneInput
            {
                FirerAttack = 9,
                TargetDefense = 10,
                FirerQualityMult = 1f,
                AttackType = AttackType.Direct,
                BypassTerrainBlock = true,
                FirerCommand = 3,
            };
            int dmg = CombatEngine.ResolveLane(lane, new QueueRollRandom(8));
            Assert.AreEqual(7, dmg, "At Even the lane resolves exactly as unled (1d8−1)");
        }

        [Test]
        public void Resolver_ForwardLane_CarriesAttackerCommand()
        {
            var attacker = Tank(Side.Player, 5, 5);
            var defender = Tank(Side.AI, 6, 5);

            var leader = MakeLeader(command: CommandAbility.Superior);
            Assign(leader, attacker);

            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };
            LaneInput lane = CombatResolver.BuildForwardLane(attacker, defender, ctx, flank: false);

            Assert.AreEqual(2, lane.FirerCommand, "Forward lane carries the attacker's EffectiveCommand (Superior = 2)");
        }

        #endregion // EffectiveCommand + Command Mitigation

        #region Ambush ladder (§6.9.4) + NCO immunity (§14.9.1)

        [Test]
        public void Ambush_Scalar_Ladder()
        {
            var mover = Tank(Side.AI, 6, 5);
            var plainAmbusher = Tank(Side.Player, 5, 5);
            Assert.AreEqual(GameData.AMBUSH_BONUS_MULT,
                CombatResolver.BuildAmbushLane(plainAmbusher, mover).PostStackScalar, 0.001f,
                "Unled ambusher uses the universal 1.5×");

            var sfAmbusher = Tank(Side.Player, 5, 7);
            var sfLeader = MakeLeader();
            PromoteToTop(sfLeader);
            Assert.IsTrue(sfLeader.UnlockSkill(SpecialForcesSpecialization.TerrainExpert_TerrainMastery), "SF T4 #1");
            Assert.IsTrue(sfLeader.UnlockSkill(SpecialForcesSpecialization.InfiltrationTactics_InfiltrationMovement), "SF T4 #2");
            Assert.IsTrue(sfLeader.UnlockSkill(SpecialForcesSpecialization.SuperiorCamouflage_ConcealedPositions), "SF T4 #3");
            Assert.IsTrue(sfLeader.UnlockSkill(SpecialForcesSpecialization.AmbushTactics_AmbushTactics), "SF T5");
            Assign(sfLeader, sfAmbusher);
            Assert.AreEqual(GameData.AMBUSH_TACTICS_MULT,
                CombatResolver.BuildAmbushLane(sfAmbusher, mover).PostStackScalar, 0.001f,
                "Ambush Tactics REPLACES the scalar with 1.75×");

            var caAmbusher = Tank(Side.Player, 5, 9);
            var caLeader = MakeLeader();
            PromoteToTop(caLeader);
            Assert.IsTrue(caLeader.UnlockSkill(CombinedArmsSpecialization.AviationAssets_SpottingRange), "CA T4 #1");
            Assert.IsTrue(caLeader.UnlockSkill(CombinedArmsSpecialization.ExpertStaff_MovementAction), "CA T4 #2");
            Assert.IsTrue(caLeader.UnlockSkill(CombinedArmsSpecialization.TacticalGenius_CombatAction), "CA T4 #3");
            Assert.IsTrue(caLeader.UnlockSkill(CombinedArmsSpecialization.NightCombatOperations_NightCombat), "CA T5");
            Assign(caLeader, caAmbusher);
            Assert.AreEqual(GameData.NIGHT_COMBAT_AMBUSH_MULT,
                CombatResolver.BuildAmbushLane(caAmbusher, mover).PostStackScalar, 0.001f,
                "Night Combat Operations REPLACES the scalar with 2.0×");
        }

        [Test]
        public void Ambush_NcoVictim_TakesZeroDamage()
        {
            var ambusher = Tank(Side.AI, 5, 5);
            var mover = Tank(Side.Player, 6, 5);
            float hpBefore = mover.HitPoints.Current;

            var leader = MakeLeader();
            PromoteToTop(leader);
            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.AviationAssets_SpottingRange), "CA T4 #1");
            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.ExpertStaff_MovementAction), "CA T4 #2");
            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.TacticalGenius_CombatAction), "CA T4 #3");
            Assert.IsTrue(leader.UnlockSkill(CombinedArmsSpecialization.NightCombatOperations_NightCombat), "CA T5");
            Assign(leader, mover);

            var ctx = new DirectAttackContext { DefenderTerrain = TerrainType.Clear };
            AmbushResult result = CombatResolver.ResolveAmbush(ambusher, mover,
                ctx, new QueueRollRandom(8, 5)); // band die, stand d10

            Assert.AreEqual(0, result.DamageToMover, "NCO-led victim is immune to ambush damage (§14.9.1)");
            Assert.AreEqual(hpBefore, mover.HitPoints.Current, 0.001f, "HP unchanged");
        }

        #endregion // Ambush ladder + NCO immunity

        #region State-desync fixes (REP balance + grade sync)

        [Test]
        public void Desync_ReputationPoints_DecreaseOnSpend()
        {
            var leader = MakeLeader(reputation: 500);
            Assert.AreEqual(500, leader.ReputationPoints, "Awarded REP visible on the Leader");

            Assert.IsTrue(leader.UnlockSkill(ArmoredDoctrine.ShockTankCorps_HardAttack), "Armored T1 (60)");
            Assert.AreEqual(440, leader.ReputationPoints,
                "Leader-side REP reflects the spend (was frozen at lifetime-earned before the 2026-07-03 fix)");
        }

        [Test]
        public void Desync_CommandGrade_SyncsOnPromotion()
        {
            var leader = MakeLeader();
            Assert.AreEqual(CommandGrade.JuniorGrade, leader.CommandGrade);

            PromoteToSenior(leader);
            Assert.AreEqual(CommandGrade.SeniorGrade, leader.CommandGrade,
                "Leader-side grade (rank strings, snapshots) syncs on promotion");

            Assert.IsTrue(leader.UnlockSkill(LeadershipFoundation.SeniorOfficerTraining_CommandTier2), "T3");
            Assert.IsTrue(leader.UnlockSkill(LeadershipFoundation.PromotionToTopGrade_TopPromotion), "Top");
            Assert.AreEqual(CommandGrade.TopGrade, leader.CommandGrade, "Top promotion syncs too");
        }

        #endregion // State-desync fixes

        #region REP earn hooks (§14.5)

        [Test]
        public void RepHook_CombatAward_OnAttack()
        {
            var attacker = Tank(Side.Player, 5, 5);
            var defender = Infantry(Side.AI, 6, 5);
            defender.SetSpottedLevel(SpottedLevel.Level2);

            var leader = MakeLeader(reputation: 0);
            Assign(leader, attacker);

            var r = GroundCombatAction.Execute(attacker, defender, GameDataManager.CurrentHexMap, new FixedRollRandom(1));

            Assert.IsTrue(r.Executed, "Attack executed");
            Assert.GreaterOrEqual(leader.ReputationPoints, GameData.REP_PER_COMBAT_ACTION,
                "Attacker's leader earns at least the Combat award (3) — plus any result awards");
        }

        #endregion // REP earn hooks
    }
}
