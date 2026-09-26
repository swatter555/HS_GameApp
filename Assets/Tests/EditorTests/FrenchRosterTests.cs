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
    public class FrenchRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.ART_LIGHT_FR, SpriteManager.FR_LightArt, MovementMedium.Foot)]
        [TestCase(WeaponType.ART_HEAVY_FR, SpriteManager.FR_HeavyArt, MovementMedium.Foot)]
        [TestCase(WeaponType.AAA_GEN_FR, SpriteManager.FR_AAA, MovementMedium.Foot)]
        [TestCase(WeaponType.IFV_AMX10P_FR, SpriteManager.FR_AMX10P, MovementMedium.Tracked)]
        [TestCase(WeaponType.INF_AM_FR, SpriteManager.FR_AirMobile, MovementMedium.Foot)]
        [TestCase(WeaponType.HEL_PUMA_FR, SpriteManager.FR_Puma_Frame0, MovementMedium.Helo)]
        [TestCase(WeaponType.HEL_GAZELLE_FR, SpriteManager.FR_Gazelle_Frame0, MovementMedium.Helo)]
        public void ApprovedEquipment_IsRegisteredWithNationalArtAndMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True, type.ToString());
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
        }

        [TestCase("FR_MECH_INFANTRY_BRIGADE_AMX10P", UnitClassification.MECH,
            WeaponType.INF_REG_FR, WeaponType.IFV_AMX10P_FR, WeaponType.NONE)]
        [TestCase("FR_AIRMOBILE_BRIGADE", UnitClassification.AM,
            WeaponType.INF_AM_FR, WeaponType.APC_VAB_FR, WeaponType.HEL_PUMA_FR)]
        [TestCase("FR_GAZELLE_ATTACK_SQUADRON", UnitClassification.HELO,
            WeaponType.HEL_GAZELLE_FR, WeaponType.NONE, WeaponType.NONE)]
        [TestCase("FR_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART,
            WeaponType.ART_LIGHT_FR, WeaponType.TRK_GEN_NATO, WeaponType.NONE)]
        [TestCase("FR_HEAVY_ARTILLERY_REGIMENT", UnitClassification.ART,
            WeaponType.ART_HEAVY_FR, WeaponType.TRK_GEN_NATO, WeaponType.NONE)]
        [TestCase("FR_TOWED_AAA_REGIMENT", UnitClassification.AAA,
            WeaponType.AAA_GEN_FR, WeaponType.TRK_GEN_NATO, WeaponType.NONE)]
        public void NewFormations_HaveLegalBaysAndFrenchNationality(string id, UnitClassification classification,
            WeaponType deployed, WeaponType mobile, WeaponType embarked)
        {
            var unit = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(unit, Is.Not.Null, id);
            Assert.That(unit.Nationality, Is.EqualTo(Nationality.FRA));
            Assert.That(unit.Side, Is.EqualTo(Side.AI));
            Assert.That(unit.Classification, Is.EqualTo(classification));
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
        public void Amx10P_AddsAnAmphibiousAutocannonCarrier_WithoutReplacingVabOrChangingTheBaseCensus()
        {
            var amx = P(WeaponType.IFV_AMX10P_FR);
            Assert.That(amx.HasCapability(WeaponCapability.Amphibious), Is.True);
            Assert.That(amx.HardAttack, Is.EqualTo(4), "No Marder ATGM rail on the carrier.");
            Assert.That(amx.SoftAttack, Is.EqualTo(9));
            Assert.That(amx.MaxMovementPoints, Is.EqualTo(10));
            Assert.That(amx.PrestigeCost, Is.EqualTo(115));
            Assert.That(amx.TurnAvailable, Is.EqualTo(420));
            Assert.That(amx.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
                { { WeaponType.IFV_AMX10P_FR, 135 } }));
            var original = CombatUnitDB.GetUnitTemplate("FR_MECH_INFANTRY_BRIGADE");
            var alternative = CombatUnitDB.GetUnitTemplate("FR_MECH_INFANTRY_BRIGADE_AMX10P");
            Assert.That(original.EquipmentBays.Mobile, Is.EqualTo(WeaponType.APC_VAB_FR));
            Assert.That(alternative.EquipmentBays.Deployed, Is.EqualTo(original.EquipmentBays.Deployed));
            Assert.That(P(WeaponType.INF_REG_FR).IntelReportStats[WeaponType.TANK_AMX30_FR], Is.EqualTo(40));
            Assert.That(P(WeaponType.TANK_AMX30_FR).IntelReportStats[WeaponType.IFV_AMX10P], Is.EqualTo(36),
                "The existing census-only token must remain supported.");
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.IFV_AMX10P_FR), Is.EqualTo(WeaponSoundFamily.Autocannon));
        }

        [Test]
        public void AirMobileFormation_SelectsInfantryVabAndPuma_WithoutCountingLiftLosses()
        {
            var unit = CombatUnitDB.GetUnitTemplate("FR_AIRMOBILE_BRIGADE");
            var bays = unit.EquipmentBays;
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.W), Is.EqualTo(SpriteManager.FR_AirMobile));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.E), Is.EqualTo(SpriteManager.FR_VAB));
            Assert.That(bays.GetIcon(DeploymentPosition.Embarked, HexDirection.NW), Is.EqualTo(SpriteManager.FR_Puma_Frame0));
            Assert.That(P(WeaponType.INF_AM_FR).HasCapability(WeaponCapability.MountainMovement), Is.True);
            Assert.That(P(WeaponType.INF_AM_FR).HasCapability(WeaponCapability.AirDroppable), Is.False);
            var census = bays.GetIntelReport();
            Assert.That(census.Personnel, Is.EqualTo(2200));
            Assert.That(census.APC, Is.EqualTo(135));
            Assert.That(census.HEL, Is.Zero, "Organic lift never contributes to equipment losses.");
        }

        [Test]
        public void Puma_IsNonCombatantOrganicLift_AndGazelleIsAnIndependentAttackProfile()
        {
            var puma = P(WeaponType.HEL_PUMA_FR);
            var gazelle = P(WeaponType.HEL_GAZELLE_FR);
            var bays = CombatUnitDB.GetUnitTemplate("FR_AIRMOBILE_BRIGADE").EquipmentBays;
            Assert.That(puma.TransportCategory, Is.EqualTo(TransportCategory.HeloTransport));
            Assert.That(puma.HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(puma.IntelReportStats, Is.Empty);
            Assert.That(puma.PrestigeCost, Is.EqualTo(90));
            Assert.That(puma.TurnAvailable, Is.EqualTo(372));
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Deployed, WeaponType.HEL_PUMA_FR), Is.False);
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Mobile, WeaponType.HEL_PUMA_FR), Is.False);
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Embarked, WeaponType.HEL_GAZELLE_FR), Is.False);
            Assert.That(gazelle.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(gazelle.HasCapability(WeaponCapability.NonCombatant), Is.False);
            Assert.That(gazelle.HardAttack, Is.EqualTo(P(WeaponType.HEL_BO105_GE).HardAttack));
            Assert.That(gazelle.PrestigeCost, Is.EqualTo(P(WeaponType.HEL_BO105_GE).PrestigeCost));
            Assert.That(gazelle.TurnAvailable, Is.EqualTo(492));
            Assert.That(gazelle.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
                { { WeaponType.HEL_GAZELLE_FR, 54 } }));
            Assert.That(CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Any(u => u.EquipmentBays.Deployed == WeaponType.HEL_PUMA_FR), Is.False,
                "Robert retained organic-only Puma lift; do not add a standalone transport template.");
        }

        [TestCase(WeaponType.ART_LIGHT_FR, WeaponType.ART_105MM_FG)]
        [TestCase(WeaponType.ART_HEAVY_FR, WeaponType.ART_155MM_FG)]
        public void FrenchGuns_KeepNationalCensusAndExcludeGroundCarriers(WeaponType type, WeaponType gun)
        {
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 1050 }, { gun, 48 }, { WeaponType.MANPAD_MISTRAL, 12 }
            }));
        }
    }
}
