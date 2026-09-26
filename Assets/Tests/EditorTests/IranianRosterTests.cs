using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Audio;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    public class IranianRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.TANK_CHIEFTAIN_IR, SpriteManager.IR_Chieftain, MovementMedium.Tracked)]
        [TestCase(WeaponType.HEL_AH1_IR, SpriteManager.IR_AH1_Frame0, MovementMedium.Helo)]
        [TestCase(WeaponType.FGT_F5_IR, SpriteManager.IR_F5, MovementMedium.FixedWing)]
        [TestCase(WeaponType.SPA_M109_IR, SpriteManager.IR_M109, MovementMedium.Tracked)]
        [TestCase(WeaponType.RCN_M113_IR, SpriteManager.IR_M113Recon, MovementMedium.Tracked)]
        [TestCase(WeaponType.ART_LIGHT_IR, SpriteManager.IR_LightArt, MovementMedium.Foot)]
        [TestCase(WeaponType.ART_HEAVY_IR, SpriteManager.IR_HeavyArt, MovementMedium.Foot)]
        [TestCase(WeaponType.AAA_GEN_IR, SpriteManager.IR_AAA, MovementMedium.Foot)]
        [TestCase(WeaponType.TRK_GEN_IR, SpriteManager.IR_Truck, MovementMedium.Wheeled)]
        public void NewEquipment_UsesNationalArtAndCorrectMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
        }

        [TestCase("IR_TANK_REGIMENT_CHIEFTAIN", UnitClassification.TANK, WeaponType.TANK_CHIEFTAIN_IR, WeaponType.NONE)]
        [TestCase("IR_AH1_ATTACK_SQUADRON", UnitClassification.HELO, WeaponType.HEL_AH1_IR, WeaponType.NONE)]
        [TestCase("IR_F5_FIGHTER_SQUADRON", UnitClassification.FGT, WeaponType.FGT_F5_IR, WeaponType.NONE)]
        [TestCase("IR_SP_ARTILLERY_REGIMENT_M109", UnitClassification.SPA, WeaponType.SPA_M109_IR, WeaponType.NONE)]
        [TestCase("IR_RECON_REGIMENT_M113", UnitClassification.RECON, WeaponType.RCN_M113_IR, WeaponType.NONE)]
        [TestCase("IR_HAWK_SAM_REGIMENT", UnitClassification.SAM, WeaponType.SAM_HAWK_US, WeaponType.TRK_GEN_IR)]
        [TestCase("IR_INFANTRY_REGIMENT", UnitClassification.INF, WeaponType.INF_REG_IR, WeaponType.TRK_GEN_IR)]
        [TestCase("IR_HEAVY_ARTILLERY_REGIMENT", UnitClassification.ART, WeaponType.ART_HEAVY_IR, WeaponType.TRK_GEN_IR)]
        [TestCase("IR_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART, WeaponType.ART_LIGHT_IR, WeaponType.TRK_GEN_IR)]
        [TestCase("IR_AIR_DEFENSE_REGIMENT", UnitClassification.AAA, WeaponType.AAA_GEN_IR, WeaponType.TRK_GEN_IR)]
        public void NewAndReassignedFormations_UseLegalBays(string id, UnitClassification classification,
            WeaponType deployed, WeaponType mobile)
        {
            var unit = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(unit, Is.Not.Null, id);
            Assert.That(unit.Nationality, Is.EqualTo(Nationality.IR));
            Assert.That(unit.Side, Is.EqualTo(Side.AI));
            Assert.That(unit.Classification, Is.EqualTo(classification));
            var bays = unit.EquipmentBays;
            Assert.That(bays.Deployed, Is.EqualTo(deployed));
            Assert.That(bays.Mobile, Is.EqualTo(mobile));
            Assert.That(bays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(bays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE)
                Assert.That(bays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
        }

        [Test]
        public void Chieftain_IsAnEarly120mmTank_AndLeavesTheExistingM60CounterIntact()
        {
            var tank = P(WeaponType.TANK_CHIEFTAIN_IR);
            Assert.That(tank.HardAttack, Is.EqualTo(12));
            Assert.That(tank.HardDefense, Is.EqualTo(8), "No later composite-armour package.");
            Assert.That(tank.SoftAttack, Is.EqualTo(7));
            Assert.That(tank.SoftDefense, Is.EqualTo(6));
            Assert.That(tank.ICM, Is.EqualTo(1.05f).Within(0.001f), "No NATO/second-line quality or thermal multiplier.");
            Assert.That(tank.SpottingRange, Is.EqualTo(2));
            Assert.That(tank.TurnAvailable, Is.EqualTo(396));
            Assert.That(tank.IntelReportStats[WeaponType.TANK_CHIEFTAIN_IR], Is.EqualTo(100));
            Assert.That(tank.IntelReportStats[WeaponType.Personnel], Is.EqualTo(1100));
            Assert.That(tank.IntelReportStats[WeaponType.RCN_M113_IR], Is.EqualTo(12));
            Assert.That(CombatUnitDB.GetUnitTemplate("IR_TANK_REGIMENT").EquipmentBays.Deployed,
                Is.EqualTo(WeaponType.TANK_M60A3_IR));
            Assert.That(P(WeaponType.TANK_M60A3_IR).IntelReportStats[WeaponType.RCN_FV105_UK], Is.EqualTo(12));
        }

        [Test]
        public void Cobra_UsesTheEstablishedTowGunshipLine_AndCannotProvideLift()
        {
            var cobra = P(WeaponType.HEL_AH1_IR);
            Assert.That(cobra.HardAttack, Is.EqualTo(P(WeaponType.HEL_AH1_US).HardAttack));
            Assert.That(cobra.SoftAttack, Is.EqualTo(P(WeaponType.HEL_AH1_US).SoftAttack));
            Assert.That(cobra.HardDefense, Is.EqualTo(6));
            Assert.That(cobra.GroundAirDefense, Is.EqualTo(10));
            Assert.That(cobra.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(cobra.HasCapability(WeaponCapability.NonCombatant), Is.False);
            Assert.That(cobra.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.HEL_AH1_IR, 54 } }));
            Assert.That(cobra.IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            var bays = CombatUnitDB.GetUnitTemplate("IR_AH1_ATTACK_SQUADRON").EquipmentBays;
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Embarked, WeaponType.HEL_AH1_IR), Is.False);
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.W), Is.EqualTo(SpriteManager.IR_AH1_Frame0));
            Assert.That(bays.GetIntelReport().HEL, Is.EqualTo(54));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.HEL_AH1_IR), Is.EqualTo(WeaponSoundFamily.HelicopterAttack));
        }

        [Test]
        public void F5_IsAnAgileEarlyFighter_WithUnguidedStrikeAndNoBvrBonus()
        {
            var fighter = P(WeaponType.FGT_F5_IR);
            Assert.That(fighter.Dogfighting, Is.EqualTo(8));
            Assert.That(fighter.Maneuverability, Is.EqualTo(11));
            Assert.That(fighter.TopSpeed, Is.EqualTo(9));
            Assert.That(fighter.Survivability, Is.EqualTo(6));
            Assert.That(fighter.GroundAttack, Is.EqualTo(6));
            Assert.That(fighter.ICM, Is.EqualTo(1f));
            Assert.That(fighter.TurnAvailable, Is.EqualTo(432));
            Assert.That(fighter.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.FGT_F5_IR, 48 } }));
            Assert.That(P(WeaponType.FGT_F4_IR).IntelReportStats[WeaponType.FGT_F4_IR], Is.EqualTo(48));
            Assert.That(P(WeaponType.FGT_F14_IR).IntelReportStats[WeaponType.FGT_F14_IR], Is.EqualTo(48));
        }

        [Test]
        public void M109_SharesTheConventionalGunLine_WithoutCopperheadOrNatoFiresQuality()
        {
            var iran = P(WeaponType.SPA_M109_IR);
            var germany = P(WeaponType.SPA_M109_GE);
            Assert.That(iran.HardAttack, Is.EqualTo(germany.HardAttack));
            Assert.That(iran.SoftAttack, Is.EqualTo(germany.SoftAttack));
            Assert.That(iran.IndirectRange, Is.EqualTo(5));
            Assert.That(iran.MaxMovementPoints, Is.EqualTo(10));
            Assert.That(iran.ICM, Is.EqualTo(1f));
            Assert.That(iran.HardAttack, Is.LessThan(P(WeaponType.SPA_M109_US).HardAttack));
            Assert.That(iran.IntelReportStats[WeaponType.SPA_M109_IR], Is.EqualTo(36));
            Assert.That(iran.IntelReportStats[WeaponType.APC_M113_IR], Is.EqualTo(12));
        }

        [Test]
        public void M113Recon_HasObservationAndOwnArt_WithoutTurningTheCarrierIntoAFormation()
        {
            var recon = P(WeaponType.RCN_M113_IR);
            var carrier = P(WeaponType.APC_M113_IR);
            Assert.That(recon.SpottingRange, Is.EqualTo(3));
            Assert.That(recon.MaxMovementPoints, Is.EqualTo(carrier.MaxMovementPoints));
            Assert.That(recon.HasCapability(WeaponCapability.Amphibious), Is.EqualTo(carrier.HasCapability(WeaponCapability.Amphibious)));
            Assert.That(recon.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(recon.IntelReportStats[WeaponType.Personnel], Is.EqualTo(600));
            Assert.That(recon.IntelReportStats[WeaponType.RCN_M113_IR], Is.EqualTo(36));
            Assert.That(carrier.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.APC_M113_IR, 90 } }));
            Assert.That(carrier.IconProfile.Icon, Is.EqualTo(SpriteManager.IR_M113));
            Assert.That(CombatUnitDB.GetUnitTemplate("IR_ARMORED_INFANTRY_REGIMENT").EquipmentBays.Mobile,
                Is.EqualTo(WeaponType.APC_M113_IR));
        }

        [Test]
        public void Hawk_ReusesTheSharedProfileAndCensus_WithAnIranianTruck()
        {
            var bays = CombatUnitDB.GetUnitTemplate("IR_HAWK_SAM_REGIMENT").EquipmentBays;
            var hawk = P(bays.Deployed);
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.E), Is.EqualTo(SpriteManager.US_Hawk));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.E), Is.EqualTo(SpriteManager.IR_Truck));
            Assert.That(hawk.IndirectRange, Is.EqualTo(6));
            Assert.That(hawk.GroundAirAttack, Is.EqualTo(15));
            Assert.That(hawk.MaxMovementPoints, Is.Zero);
            Assert.That(hawk.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 1100 }, { WeaponType.SAM_HAWK_US, 18 },
                { WeaponType.SPAAA_M163_US, 4 }, { WeaponType.APC_M113_US, 24 }
            }));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("SAM_HAWK_IR"));
        }

        [TestCase(WeaponType.ART_LIGHT_IR, 700, 48, 6)]
        [TestCase(WeaponType.ART_HEAVY_IR, 750, 36, 8)]
        [TestCase(WeaponType.AAA_GEN_IR, 500, 18, 12)]
        public void TowedSupport_HasNationalWeaponsAndPersonnel_WithoutEmbeddedCarriers(
            WeaponType type, int personnel, int weapons, int manpads)
        {
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, personnel }, { type, weapons }, { WeaponType.MANPAD_STRELA, manpads }
            }));
            Assert.That(P(WeaponType.TRK_GEN_IR).IntelReportStats, Is.Empty);
            Assert.That(P(WeaponType.TRK_GEN_IR).HasCapability(WeaponCapability.NonCombatant), Is.True);
        }

        [Test]
        public void IranianCatalog_UsesApprovedEquipment_WithoutRestoringWithdrawnAirMobileOrS75()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.Nationality == Nationality.IR).ToList();
            Assert.That(units.Count, Is.EqualTo(14));
            Assert.That(units.All(u => u.Classification != UnitClassification.AM && u.EquipmentBays.Embarked == WeaponType.NONE), Is.True);
            var profiles = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile })
                .Where(t => t != WeaponType.NONE).Distinct().ToList();
            Assert.That(profiles.All(t => t == WeaponType.SAM_HAWK_US || t.ToString().EndsWith("_IR", StringComparison.Ordinal)), Is.True);
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("HEL_UH1_IR"));
            Assert.That(P(WeaponType.INF_REG_IR).IntelReportStats[WeaponType.ART_HEAVY_IR], Is.EqualTo(18));
            Assert.That(P(WeaponType.INF_REG_IR).IntelReportStats.ContainsKey(WeaponType.ART_HEAVY_ARAB), Is.False);
            Assert.That(WeaponProfileDB.HasWeaponProfile(WeaponType.ART_HEAVY_ARAB), Is.True, "Existing stored content retains its keys.");
        }
    }
}
