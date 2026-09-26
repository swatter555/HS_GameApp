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
    public class BritishRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.ART_LIGHT_UK, SpriteManager.UK_LightArt, MovementMedium.Foot)]
        [TestCase(WeaponType.ART_HEAVY_UK, SpriteManager.UK_HeavyArt, MovementMedium.Foot)]
        [TestCase(WeaponType.AAA_GEN_UK, SpriteManager.UK_AAA, MovementMedium.Foot)]
        [TestCase(WeaponType.ATT_JAGUAR_UK, SpriteManager.UK_Jaguar, MovementMedium.FixedWing)]
        [TestCase(WeaponType.INF_AM_UK, SpriteManager.UK_AirMobile, MovementMedium.Foot)]
        [TestCase(WeaponType.HEL_PUMA_UK, SpriteManager.UK_Puma_Frame0, MovementMedium.Helo)]
        [TestCase(WeaponType.HEL_LYNX_UK, SpriteManager.UK_Lynx_Frame0, MovementMedium.Helo)]
        [TestCase(WeaponType.APC_FV432_UK, SpriteManager.UK_FV432, MovementMedium.Tracked)]
        [TestCase(WeaponType.TANK_CHIEFTAIN_UK, SpriteManager.UK_Chieftain, MovementMedium.Tracked)]
        [TestCase(WeaponType.FGT_F4_UK, SpriteManager.UK_F4, MovementMedium.FixedWing)]
        public void ApprovedEquipment_UsesNationalArtAndCorrectMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
        }

        [TestCase("UK_MECH_INFANTRY_REGIMENT_FV432", UnitClassification.MECH, WeaponType.INF_REG_UK, WeaponType.APC_FV432_UK, WeaponType.NONE)]
        [TestCase("UK_AIRMOBILE_REGIMENT", UnitClassification.AM, WeaponType.INF_AM_UK, WeaponType.TRK_GEN_NATO, WeaponType.HEL_PUMA_UK)]
        [TestCase("UK_LYNX_ATTACK_SQUADRON", UnitClassification.HELO, WeaponType.HEL_LYNX_UK, WeaponType.NONE, WeaponType.NONE)]
        [TestCase("UK_JAGUAR_ATTACK_SQUADRON", UnitClassification.ATT, WeaponType.ATT_JAGUAR_UK, WeaponType.NONE, WeaponType.NONE)]
        [TestCase("UK_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART, WeaponType.ART_LIGHT_UK, WeaponType.TRK_GEN_NATO, WeaponType.NONE)]
        [TestCase("UK_HEAVY_ARTILLERY_REGIMENT", UnitClassification.ART, WeaponType.ART_HEAVY_UK, WeaponType.TRK_GEN_NATO, WeaponType.NONE)]
        [TestCase("UK_TOWED_AAA_REGIMENT", UnitClassification.AAA, WeaponType.AAA_GEN_UK, WeaponType.TRK_GEN_NATO, WeaponType.NONE)]
        [TestCase("UK_ARMOURED_REGIMENT_CHIEFTAIN", UnitClassification.TANK, WeaponType.TANK_CHIEFTAIN_UK, WeaponType.NONE, WeaponType.NONE)]
        [TestCase("UK_F4_FIGHTER_SQUADRON", UnitClassification.FGT, WeaponType.FGT_F4_UK, WeaponType.NONE, WeaponType.NONE)]
        public void NewFormations_HaveLegalBaysAndBritishExperience(string id, UnitClassification classification,
            WeaponType deployed, WeaponType mobile, WeaponType embarked)
        {
            var unit = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(unit, Is.Not.Null, id);
            Assert.That(unit.Nationality, Is.EqualTo(Nationality.UK));
            Assert.That(unit.Side, Is.EqualTo(Side.AI));
            Assert.That(unit.Classification, Is.EqualTo(classification));
            Assert.That(unit.ExperienceLevel, Is.EqualTo(ExperienceLevel.Experienced));
            var bays = unit.EquipmentBays;
            Assert.That(bays.Deployed, Is.EqualTo(deployed));
            Assert.That(bays.Mobile, Is.EqualTo(mobile));
            Assert.That(bays.Embarked, Is.EqualTo(embarked));
            Assert.That(bays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE)
                Assert.That(bays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
            if (embarked != WeaponType.NONE)
                Assert.That(bays.CanAccept(classification, EquipmentBay.Embarked, embarked), Is.True);
        }

        [Test]
        public void Fv432_ActivatesTheExistingCensusKey_AsAnAlternativeCarrierWithoutChangingInfantry()
        {
            var carrier = P(WeaponType.APC_FV432_UK);
            Assert.That(carrier.HardAttack, Is.EqualTo(3));
            Assert.That(carrier.SoftAttack, Is.EqualTo(6), "Standard carrier, no inferred RARDEN turret.");
            Assert.That(carrier.ICM, Is.EqualTo(1f));
            Assert.That(carrier.MaxMovementPoints, Is.EqualTo(8));
            Assert.That(carrier.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.APC_FV432_UK, 45 } }));
            var alternative = CombatUnitDB.GetUnitTemplate("UK_MECH_INFANTRY_REGIMENT_FV432").EquipmentBays;
            var original = CombatUnitDB.GetUnitTemplate("UK_MECH_INFANTRY_REGIMENT").EquipmentBays;
            Assert.That(alternative.Deployed, Is.EqualTo(original.Deployed));
            Assert.That(original.Mobile, Is.EqualTo(WeaponType.IFV_WARRIOR_UK));
            Assert.That(alternative.GetIntelReport().APC, Is.EqualTo(45));
            Assert.That(P(WeaponType.TANK_CHALLENGER1_UK).IntelReportStats[WeaponType.APC_FV432_UK], Is.EqualTo(8));
            Assert.That(P(WeaponType.SPA_M109_UK).IntelReportStats[WeaponType.APC_FV432_UK], Is.EqualTo(48));
            Assert.That(P(WeaponType.INF_REG_UK).IntelReportStats[WeaponType.TANK_CHALLENGER1_UK], Is.EqualTo(28));
        }

        [Test]
        public void AirMobileFormation_SelectsInfantryTruckAndPuma_WithNoLiftAircraftLossCensus()
        {
            var bays = CombatUnitDB.GetUnitTemplate("UK_AIRMOBILE_REGIMENT").EquipmentBays;
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.W), Is.EqualTo(SpriteManager.UK_AirMobile));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.E), Is.EqualTo(SpriteManager.NATO_Truck));
            Assert.That(bays.GetIcon(DeploymentPosition.Embarked, HexDirection.NW), Is.EqualTo(SpriteManager.UK_Puma_Frame0));
            Assert.That(P(WeaponType.INF_AM_UK).HasCapability(WeaponCapability.MountainMovement), Is.True);
            Assert.That(P(WeaponType.INF_AM_UK).HasCapability(WeaponCapability.AirDroppable), Is.False);
            Assert.That(P(WeaponType.INF_AM_UK).GroundAirAttack, Is.EqualTo(6), "British basic MANPADS, not the Stinger line.");
            Assert.That(P(WeaponType.INF_AM_UK).ICM, Is.EqualTo(1f));
            Assert.That(bays.GetIntelReport().Personnel, Is.EqualTo(1860));
            Assert.That(bays.GetIntelReport().HEL, Is.Zero);
            Assert.That(bays.GetIntelReport().APC, Is.Zero);
        }

        [Test]
        public void Puma_IsOnlyOrganicLift_AndCannotBecomeAnAttackFormation()
        {
            var puma = P(WeaponType.HEL_PUMA_UK);
            var bays = CombatUnitDB.GetUnitTemplate("UK_AIRMOBILE_REGIMENT").EquipmentBays;
            Assert.That(puma.TransportCategory, Is.EqualTo(TransportCategory.HeloTransport));
            Assert.That(puma.HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(puma.IntelReportStats, Is.Empty);
            Assert.That(puma.PrestigeCost, Is.EqualTo(P(WeaponType.HEL_PUMA_FR).PrestigeCost));
            Assert.That(puma.TurnAvailable, Is.EqualTo(396));
            Assert.That(puma.IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Deployed, WeaponType.HEL_PUMA_UK), Is.False);
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Mobile, WeaponType.HEL_PUMA_UK), Is.False);
            Assert.That(CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Any(u => u.EquipmentBays.Deployed == WeaponType.HEL_PUMA_UK), Is.False);
        }

        [Test]
        public void Lynx_IsALightTowAttackCounter_WithoutCannonOrTransportRole()
        {
            var lynx = P(WeaponType.HEL_LYNX_UK);
            Assert.That(lynx.HardAttack, Is.EqualTo(11));
            Assert.That(lynx.SoftAttack, Is.EqualTo(10), "No cannon/rocket trait; inherited light-helicopter baseline.");
            Assert.That(lynx.HardDefense, Is.EqualTo(6));
            Assert.That(lynx.GroundAirDefense, Is.EqualTo(10));
            Assert.That(lynx.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(lynx.HasCapability(WeaponCapability.NonCombatant), Is.False);
            Assert.That(lynx.TurnAvailable, Is.EqualTo(516), "TOW-system 1981 anchor.");
            Assert.That(lynx.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.HEL_LYNX_UK, 54 } }));
            Assert.That(lynx.IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            Assert.That(CombatUnitDB.GetUnitTemplate("UK_AIRMOBILE_REGIMENT").EquipmentBays
                .CanAccept(UnitClassification.AM, EquipmentBay.Embarked, WeaponType.HEL_LYNX_UK), Is.False);
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.HEL_LYNX_UK), Is.EqualTo(WeaponSoundFamily.HelicopterAttack));
        }

        [Test]
        public void Chieftain_OffersEarlierCheaperArmour_WithoutReplacingChallenger()
        {
            var tank = P(WeaponType.TANK_CHIEFTAIN_UK);
            var challenger = P(WeaponType.TANK_CHALLENGER1_UK);
            Assert.That(tank.HardAttack, Is.EqualTo(12));
            Assert.That(tank.HardDefense, Is.EqualTo(8));
            Assert.That(tank.SpottingRange, Is.EqualTo(2), "No TOGS thermal sight.");
            Assert.That(tank.ICM, Is.EqualTo(1.05f * 1.05f * 1.10f).Within(0.001f));
            Assert.That(tank.PrestigeCost, Is.LessThan(challenger.PrestigeCost));
            Assert.That(tank.TurnAvailable, Is.LessThan(challenger.TurnAvailable));
            Assert.That(tank.IntelReportStats[WeaponType.TANK_CHIEFTAIN_UK], Is.EqualTo(58));
            Assert.That(tank.IntelReportStats[WeaponType.APC_FV432_UK], Is.EqualTo(21));
            Assert.That(tank.IntelReportStats.ContainsKey(WeaponType.IFV_WARRIOR_UK), Is.False);
            Assert.That(CombatUnitDB.GetUnitTemplate("UK_ARMOURED_REGIMENT").EquipmentBays.Deployed,
                Is.EqualTo(WeaponType.TANK_CHALLENGER1_UK));
        }

        [Test]
        public void Jaguar_PreservesTheSharedStrikeLine_AndHasAnAttackFormation()
        {
            var uk = P(WeaponType.ATT_JAGUAR_UK);
            var france = P(WeaponType.ATT_JAGUAR_FR);
            Assert.That(uk.GroundAttack, Is.EqualTo(8));
            Assert.That(france.GroundAttack, Is.EqualTo(8), "Extracting the shared definition must preserve France.");
            Assert.That(uk.OcSuppressionBonus, Is.EqualTo(20));
            Assert.That(uk.Dogfighting, Is.EqualTo(france.Dogfighting));
            Assert.That(uk.Maneuverability, Is.EqualTo(france.Maneuverability));
            Assert.That(uk.TopSpeed, Is.EqualTo(france.TopSpeed));
            Assert.That(uk.Survivability, Is.EqualTo(france.Survivability));
            Assert.That(uk.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.ATT_JAGUAR_UK, 36 } }));
            Assert.That(CombatUnitDB.GetUnitTemplate("UK_JAGUAR_ATTACK_SQUADRON").Role, Is.EqualTo(UnitRole.AirGroundAttack));
        }

        [Test]
        public void Phantom_IsTheApprovedFg1Interceptor_AndLeavesTornadoAssignmentIntact()
        {
            var phantom = P(WeaponType.FGT_F4_UK);
            Assert.That(phantom.Dogfighting, Is.EqualTo(10));
            Assert.That(phantom.TopSpeed, Is.EqualTo(12));
            Assert.That(phantom.Survivability, Is.EqualTo(8));
            Assert.That(phantom.GroundAttack, Is.EqualTo(2));
            Assert.That(phantom.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.FGT_F4_UK, 36 } }));
            Assert.That(CombatUnitDB.GetUnitTemplate("UK_F4_FIGHTER_SQUADRON").Role, Is.EqualTo(UnitRole.AirSuperiority));
            Assert.That(CombatUnitDB.GetUnitTemplate("UK_TORNADO_FIGHTER_SQUADRON").EquipmentBays.Deployed,
                Is.EqualTo(WeaponType.FGT_TORNADO_UK));
            Assert.That(P(WeaponType.FGT_TORNADO_UK).IconProfile.Icon, Is.EqualTo(SpriteManager.UK_Tornado));
        }

        [TestCase(WeaponType.ART_LIGHT_UK, WeaponType.ART_105MM_FG, 1050, 54)]
        [TestCase(WeaponType.ART_HEAVY_UK, WeaponType.ART_155MM_FG, 1050, 54)]
        [TestCase(WeaponType.AAA_GEN_UK, WeaponType.AAA_GEN_UK, 500, 18)]
        public void TowedSupport_UsesTheBritishCensus_WithoutEmbeddedCarriers(WeaponType type, WeaponType gun, int personnel, int count)
        {
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, personnel }, { gun, count }, { WeaponType.MANPAD_JAVELIN, 12 }
            }));
            Assert.That(P(type).ICM, Is.EqualTo(1f), "No formation-quality multiplier on paired towed bases.");
        }

        [Test]
        public void BritishCatalog_UsesNationalEquipmentAndSharedTruck_WhileKeepingTrackedRapier()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.Nationality == Nationality.UK).ToList();
            Assert.That(units.Count, Is.EqualTo(16));
            var profiles = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile, u.EquipmentBays.Embarked })
                .Where(t => t != WeaponType.NONE).Distinct();
            Assert.That(profiles.All(t => t == WeaponType.TRK_GEN_NATO || t.ToString().EndsWith("_UK", StringComparison.Ordinal)), Is.True);
            Assert.That(units.Count(u => u.Classification == UnitClassification.AM), Is.EqualTo(1));
            var rapier = P(WeaponType.SPSAM_RAPIER_UK);
            Assert.That(rapier.IconProfile.Icon, Is.EqualTo(SpriteManager.UK_Rapier));
            Assert.That(rapier.MovementMedium, Is.EqualTo(MovementMedium.Tracked));
            Assert.That(rapier.IndirectRange, Is.EqualTo(4));
            Assert.That(rapier.GroundAirAttack, Is.EqualTo(14));
            Assert.That(CombatUnitDB.GetUnitTemplate("UK_RAPIER_REGIMENT").EquipmentBays.Deployed,
                Is.EqualTo(WeaponType.SPSAM_RAPIER_UK));
            Assert.That(CombatUnitDB.GetAllTemplateIds(), Does.Not.Contain("UK_AIR_DEFENSE_REGIMENT"), "Deleted M163 template stays deleted.");
        }
    }
}
