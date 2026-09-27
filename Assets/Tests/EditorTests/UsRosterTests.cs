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
    public class UsRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.ART_LIGHT_US, SpriteManager.US_LightArt, MovementMedium.Foot)]
        [TestCase(WeaponType.ART_HEAVY_US, SpriteManager.US_HeavyArt, MovementMedium.Foot)]
        [TestCase(WeaponType.AAA_GEN_US, SpriteManager.US_AAA, MovementMedium.Foot)]
        [TestCase(WeaponType.TRK_GEN_US, SpriteManager.US_Truck, MovementMedium.Wheeled)]
        public void NationalSupport_HasItsOwnPictureAndCorrectMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
            Assert.That(P(type).IconProfile.IconType, Is.EqualTo(RegimentIconType.Single));
        }

        [TestCase("US_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.ART_LIGHT_US, WeaponType.TRK_GEN_US, WeaponType.NONE, ExperienceLevel.Experienced)]
        [TestCase("US_HEAVY_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.ART_HEAVY_US, WeaponType.TRK_GEN_US, WeaponType.NONE, ExperienceLevel.Experienced)]
        [TestCase("US_TOWED_AAA_REGIMENT", UnitClassification.AAA, UnitRole.AirDefenseArea, WeaponType.AAA_GEN_US, WeaponType.TRK_GEN_US, WeaponType.NONE, ExperienceLevel.Trained)]
        [TestCase("US_COBRA_AVIATION_BRIGADE", UnitClassification.HELO, UnitRole.GroundCombat, WeaponType.HEL_AH1_US, WeaponType.NONE, WeaponType.NONE, ExperienceLevel.Experienced)]
        [TestCase("US_UH1C_AVIATION_BRIGADE", UnitClassification.HELO, UnitRole.GroundCombat, WeaponType.HEL_UH1C_US, WeaponType.NONE, WeaponType.NONE, ExperienceLevel.Experienced)]
        [TestCase("US_AIRMOBILE_BRIGADE_UH1", UnitClassification.AM, UnitRole.GroundCombat, WeaponType.INF_AM_US, WeaponType.APC_HUMVEE_US, WeaponType.HEL_UH1_US, ExperienceLevel.Veteran)]
        public void NewUsFormations_HaveLegalBaysAndExpectedRoles(string id, UnitClassification classification, UnitRole role,
            WeaponType deployed, WeaponType mobile, WeaponType embarked, ExperienceLevel experience)
        {
            var u = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(u, Is.Not.Null, id);
            Assert.That(u.Nationality, Is.EqualTo(Nationality.USA));
            Assert.That(u.Side, Is.EqualTo(Side.AI));
            Assert.That(u.Classification, Is.EqualTo(classification));
            Assert.That(u.Role, Is.EqualTo(role));
            Assert.That(u.ExperienceLevel, Is.EqualTo(experience));
            var bays = u.EquipmentBays;
            Assert.That(bays.Deployed, Is.EqualTo(deployed));
            Assert.That(bays.Mobile, Is.EqualTo(mobile));
            Assert.That(bays.Embarked, Is.EqualTo(embarked));
            Assert.That(bays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE) Assert.That(bays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
            if (embarked != WeaponType.NONE) Assert.That(bays.CanAccept(classification, EquipmentBay.Embarked, embarked), Is.True);
        }

        [TestCase(WeaponType.ART_LIGHT_US, WeaponType.ART_105MM_FG, 1050, 54)]
        [TestCase(WeaponType.ART_HEAVY_US, WeaponType.ART_155MM_FG, 1050, 54)]
        [TestCase(WeaponType.AAA_GEN_US, WeaponType.AAA_GEN_US, 500, 18)]
        public void TowedSupport_CensusOwnsGunsAndPeople_WithoutEmbeddedCarriers(WeaponType type, WeaponType gun, int men, int guns)
        {
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, men }, { gun, guns }, { WeaponType.MANPAD_STINGER, 12 }
            }));
            Assert.That(P(type).ICM, Is.EqualTo(1f));
        }

        [TestCase(WeaponType.HEL_UH1_US)]
        [TestCase(WeaponType.HEL_UH1_NATO)]
        public void HueyTransport_IsOrganicOnlyUncountedLift_AndCheaperThanBlackHawk(WeaponType type)
        {
            var p = P(type);
            Assert.That(p.TransportCategory, Is.EqualTo(TransportCategory.HeloTransport));
            Assert.That(p.HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(p.IntelReportStats, Is.Empty);
            Assert.That(p.IconProfile.Icon, Is.EqualTo(SpriteManager.US_UH1_Frame0));
            Assert.That(p.IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(P(WeaponType.HEL_UH1D_GE).MaxMovementPoints));
            Assert.That(p.PrestigeCost, Is.EqualTo(90));
            Assert.That(p.PrestigeCost, Is.LessThan(P(WeaponType.HEL_UH60_US).PrestigeCost));
            Assert.That(p.TurnAvailable, Is.EqualTo(300));
            var bays = CombatUnitDB.GetUnitTemplate("US_AIRMOBILE_BRIGADE_UH1").EquipmentBays;
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Embarked, type), Is.True);
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Mobile, type), Is.False);
            Assert.That(bays.CanAccept(UnitClassification.HELO, EquipmentBay.Deployed, type), Is.False);
        }

        [TestCase(WeaponType.HEL_UH1C_US)]
        [TestCase(WeaponType.HEL_UH1C_NATO)]
        public void HueyGunship_IsRocketArmedCombatAircraft_WithApprovedPriceAndNoLiftRole(WeaponType type)
        {
            var p = P(type);
            Assert.That(p.HardAttack, Is.EqualTo(7), "No TOW/ATGM package.");
            Assert.That(p.SoftAttack, Is.EqualTo(11), "Rocket pods plus the family gun baseline; no cannon trait.");
            Assert.That(p.HardDefense, Is.EqualTo(6));
            Assert.That(p.SoftDefense, Is.EqualTo(7));
            Assert.That(p.GroundAirDefense, Is.EqualTo(10));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(24));
            Assert.That(p.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(p.HasCapability(WeaponCapability.NonCombatant), Is.False);
            Assert.That(p.IconProfile.Icon, Is.EqualTo(SpriteManager.US_UH1C_Frame0));
            Assert.That(p.IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            Assert.That(p.PrestigeCost, Is.EqualTo(100));
            Assert.That(p.PrestigeCost, Is.LessThan(P(WeaponType.HEL_AH1_US).PrestigeCost));
            Assert.That(p.TurnAvailable, Is.EqualTo(324));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { type, 54 } }));
            Assert.That(WeaponSoundClassifier.FamilyFor(type), Is.EqualTo(WeaponSoundFamily.HelicopterAttack));
            var bays = CombatUnitDB.GetUnitTemplate("US_AIRMOBILE_BRIGADE_UH1").EquipmentBays;
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Embarked, type), Is.False);
        }

        [Test]
        public void OlderUsLiftOption_PreservesInfantryAndGroundCarrier_WithoutReplacingBlackHawk()
        {
            var huey = CombatUnitDB.GetUnitTemplate("US_AIRMOBILE_BRIGADE_UH1").EquipmentBays;
            var original = CombatUnitDB.GetUnitTemplate("US_AIRMOBILE_BRIGADE").EquipmentBays;
            Assert.That(huey.Deployed, Is.EqualTo(original.Deployed));
            Assert.That(huey.Mobile, Is.EqualTo(original.Mobile));
            Assert.That(original.Embarked, Is.EqualTo(WeaponType.HEL_UH60_US));
            Assert.That(huey.GetIntelReport().Personnel, Is.EqualTo(2040));
            Assert.That(huey.GetIntelReport().HEL, Is.Zero);
            Assert.That(huey.GetIntelReport().TANK, Is.Zero);
            Assert.That(huey.GetIcon(DeploymentPosition.Deployed, HexDirection.W), Is.EqualTo(SpriteManager.US_AirMobile));
            Assert.That(huey.GetIcon(DeploymentPosition.Embarked, HexDirection.E), Is.EqualTo(SpriteManager.US_UH1_Frame0));
        }

        [Test]
        public void CobraGetsAFormation_WhileApacheAndCobraDefinitionsStayIntact()
        {
            Assert.That(CombatUnitDB.GetUnitTemplate("US_AVIATION_BRIGADE").EquipmentBays.Deployed, Is.EqualTo(WeaponType.HEL_AH64_US));
            var cobra = P(WeaponType.HEL_AH1_US);
            Assert.That(cobra.IconProfile.Icon, Is.EqualTo(SpriteManager.US_AH1_Frame0));
            Assert.That(cobra.HardAttack, Is.EqualTo(11));
            Assert.That(cobra.SoftAttack, Is.EqualTo(13));
            Assert.That(cobra.PrestigeCost, Is.EqualTo(130));
            Assert.That(cobra.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.HEL_AH1_US, 54 } }));
            var unit = CombatUnitDB.CreateUnitFromTemplate("US_UH1C_AVIATION_BRIGADE", "Huey combat test");
            Assert.That(unit.DeploymentActions.Max, Is.Zero, "Gunships use the existing no-dig-in HELO rules.");
            Assert.That(unit.OpportunityActions.Max, Is.Zero);
            Assert.That(unit.CombatActions.Max, Is.EqualTo(1));
        }

        [Test]
        public void UsHawk_UsesNationalTruck_WhileEuropeanHawksKeepSharedNatoTruck()
        {
            var us = CombatUnitDB.GetUnitTemplate("US_HAWK_REGIMENT").EquipmentBays;
            Assert.That(us.Deployed, Is.EqualTo(WeaponType.SAM_HAWK_US));
            Assert.That(us.Mobile, Is.EqualTo(WeaponType.TRK_GEN_US));
            Assert.That(us.GetIcon(DeploymentPosition.Mobile, HexDirection.SW), Is.EqualTo(SpriteManager.US_Truck));
            Assert.That(P(WeaponType.TRK_GEN_US).IntelReportStats, Is.Empty);
            Assert.That(P(WeaponType.TRK_GEN_US).HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(P(WeaponType.TRK_GEN_US).PrestigeCost, Is.EqualTo(P(WeaponType.TRK_GEN_NATO).PrestigeCost));
            foreach (var id in new[] { "GE_HAWK_REGIMENT", "NL_HAWK_REGIMENT" })
                Assert.That(CombatUnitDB.GetUnitTemplate(id).EquipmentBays.Mobile, Is.EqualTo(WeaponType.TRK_GEN_NATO));
        }

        [Test]
        public void AllTransportHelicopters_StayOutOfStandaloneAndMobileBays()
        {
            foreach (var u in CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate))
            {
                var bays = u.EquipmentBays;
                Assert.That(P(bays.Deployed)?.TransportCategory, Is.Not.EqualTo(TransportCategory.HeloTransport), u.UnitName);
                Assert.That(P(bays.Mobile)?.TransportCategory, Is.Not.EqualTo(TransportCategory.HeloTransport), u.UnitName);
            }
        }
    }
}
