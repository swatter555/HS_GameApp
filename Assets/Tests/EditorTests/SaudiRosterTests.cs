using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    public class SaudiRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.TANK_AMX30_SA, SpriteManager.SA_AMX30, MovementMedium.Tracked, 1f)]
        [TestCase(WeaponType.SPSAM_SHAHINE_SA, SpriteManager.SA_Shahine, MovementMedium.Tracked, 1f)]
        [TestCase(WeaponType.FGT_F15_SA, SpriteManager.SA_F15, MovementMedium.FixedWing, 1.1f)]
        [TestCase(WeaponType.FGT_F5_SA, SpriteManager.SA_F5, MovementMedium.FixedWing, 1f)]
        [TestCase(WeaponType.INF_MECH_SA, SpriteManager.SA_Regulars, MovementMedium.Foot, 1f)]
        [TestCase(WeaponType.INF_REG_SA, SpriteManager.SA_Regulars, MovementMedium.Foot, 1f)]
        [TestCase(WeaponType.INF_GUARD_SA, SpriteManager.SA_Guard, MovementMedium.Foot, 1f)]
        [TestCase(WeaponType.ART_LIGHT_SA, SpriteManager.SA_LightArt, MovementMedium.Foot, 1f)]
        [TestCase(WeaponType.ART_HEAVY_SA, SpriteManager.SA_HeavyArt, MovementMedium.Foot, 1f)]
        [TestCase(WeaponType.AAA_GEN_SA, SpriteManager.SA_AAA, MovementMedium.Foot, 1f)]
        [TestCase(WeaponType.SPA_AUF1_SA, SpriteManager.SA_AUF1, MovementMedium.Tracked, 1f)]
        [TestCase(WeaponType.APC_M113_SA, SpriteManager.SA_M113, MovementMedium.Tracked, 1f)]
        [TestCase(WeaponType.IFV_AMX10P_SA, SpriteManager.SA_AMX10P, MovementMedium.Tracked, 1f)]
        [TestCase(WeaponType.TRK_GEN_SA, SpriteManager.SA_Truck, MovementMedium.Wheeled, 1f)]
        public void NationalProfiles_UseApprovedArtMovementAndEquipmentQuality(
            WeaponType type, string picture, MovementMedium movement, float icm)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            var p = P(type);
            Assert.That(p.IconProfile.Icon, Is.EqualTo(picture));
            Assert.That(p.IconProfile.IconType, Is.EqualTo(RegimentIconType.Single));
            Assert.That(p.MovementMedium, Is.EqualTo(movement));
            Assert.That(p.ICM, Is.EqualTo(icm).Within(0.0001f));
        }

        [TestCase("SA_TANK_BRIGADE_AMX30", UnitClassification.TANK, UnitRole.GroundCombat, WeaponType.TANK_AMX30_SA, WeaponType.NONE, ExperienceLevel.Green)]
        [TestCase("SA_SHAHINE_SAM_REGIMENT", UnitClassification.SPSAM, UnitRole.AirDefenseArea, WeaponType.SPSAM_SHAHINE_SA, WeaponType.NONE, ExperienceLevel.Green)]
        [TestCase("SA_F15_FIGHTER_SQUADRON", UnitClassification.FGT, UnitRole.AirSuperiority, WeaponType.FGT_F15_SA, WeaponType.NONE, ExperienceLevel.Trained)]
        [TestCase("SA_F5_FIGHTER_SQUADRON", UnitClassification.FGT, UnitRole.AirSuperiority, WeaponType.FGT_F5_SA, WeaponType.NONE, ExperienceLevel.Trained)]
        [TestCase("SA_INFANTRY_BRIGADE", UnitClassification.INF, UnitRole.GroundCombat, WeaponType.INF_REG_SA, WeaponType.TRK_GEN_SA, ExperienceLevel.Green)]
        [TestCase("SA_NATIONAL_GUARD_BRIGADE", UnitClassification.INF, UnitRole.GroundCombat, WeaponType.INF_GUARD_SA, WeaponType.TRK_GEN_SA, ExperienceLevel.Green)]
        [TestCase("SA_MOT_INFANTRY_BRIGADE_M113", UnitClassification.MOT, UnitRole.GroundCombat, WeaponType.INF_MECH_SA, WeaponType.APC_M113_SA, ExperienceLevel.Green)]
        [TestCase("SA_MECH_INFANTRY_BRIGADE_AMX10P", UnitClassification.MECH, UnitRole.GroundCombat, WeaponType.INF_MECH_SA, WeaponType.IFV_AMX10P_SA, ExperienceLevel.Green)]
        [TestCase("SA_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.ART_LIGHT_SA, WeaponType.TRK_GEN_SA, ExperienceLevel.Green)]
        [TestCase("SA_HEAVY_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.ART_HEAVY_SA, WeaponType.TRK_GEN_SA, ExperienceLevel.Green)]
        [TestCase("SA_TOWED_AAA_REGIMENT", UnitClassification.AAA, UnitRole.AirDefenseArea, WeaponType.AAA_GEN_SA, WeaponType.TRK_GEN_SA, ExperienceLevel.Green)]
        [TestCase("SA_AUF1_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.SPA_AUF1_SA, WeaponType.NONE, ExperienceLevel.Green)]
        [TestCase("SA_HAWK_SAM_REGIMENT", UnitClassification.SAM, UnitRole.AirDefenseArea, WeaponType.SAM_HAWK_US, WeaponType.TRK_GEN_SA, ExperienceLevel.Green)]
        [TestCase("SA_M163_AIR_DEFENSE_REGIMENT", UnitClassification.SPAAA, UnitRole.AirDefenseArea, WeaponType.SPAAA_M163_US, WeaponType.NONE, ExperienceLevel.Green)]
        public void Formations_KeepNationalIdentityLegalBaysAndAuthoredCrew(
            string id, UnitClassification classification, UnitRole role, WeaponType deployed,
            WeaponType mobile, ExperienceLevel experience)
        {
            var u = CombatUnitDB.CreateUnitFromTemplate(id, "Saudi roster test");
            Assert.That(u, Is.Not.Null, id);
            Assert.That(u.Nationality, Is.EqualTo(Nationality.SAUD));
            Assert.That(u.Side, Is.EqualTo(Side.AI));
            Assert.That(u.Classification, Is.EqualTo(classification));
            Assert.That(u.Role, Is.EqualTo(role));
            Assert.That(u.ExperienceLevel, Is.EqualTo(experience));
            Assert.That(u.EquipmentBays.Deployed, Is.EqualTo(deployed));
            Assert.That(u.EquipmentBays.Mobile, Is.EqualTo(mobile));
            Assert.That(u.EquipmentBays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(u.EquipmentBays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE)
                Assert.That(u.EquipmentBays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
        }

        [TestCase(WeaponType.TANK_AMX30_SA, WeaponType.TANK_AMX30_FR)]
        [TestCase(WeaponType.SPSAM_SHAHINE_SA, WeaponType.SPSAM_CROTALE_FR)]
        [TestCase(WeaponType.IFV_AMX10P_SA, WeaponType.IFV_AMX10P_FR)]
        [TestCase(WeaponType.FGT_F5_SA, WeaponType.FGT_F5_IR)]
        public void SharedHardware_PreservesCombatStatisticsWithNationalArtAndCensus(
            WeaponType saudi, WeaponType existing)
        {
            AssertCombatEqual(P(saudi), P(existing));
            Assert.That(P(saudi).PrestigeCost, Is.EqualTo(P(existing).PrestigeCost));
            Assert.That(P(saudi).ICM, Is.EqualTo(P(existing).ICM));
            Assert.That(P(saudi).IconProfile.Icon, Is.Not.EqualTo(P(existing).IconProfile.Icon));
            Assert.That(P(saudi).IntelReportStats.ContainsKey(existing), Is.False);
        }

        private static void AssertCombatEqual(WeaponProfile actual, WeaponProfile expected)
        {
            Assert.That(actual.HardAttack, Is.EqualTo(expected.HardAttack));
            Assert.That(actual.HardDefense, Is.EqualTo(expected.HardDefense));
            Assert.That(actual.SoftAttack, Is.EqualTo(expected.SoftAttack));
            Assert.That(actual.SoftDefense, Is.EqualTo(expected.SoftDefense));
            Assert.That(actual.GroundAirAttack, Is.EqualTo(expected.GroundAirAttack));
            Assert.That(actual.GroundAirDefense, Is.EqualTo(expected.GroundAirDefense));
            Assert.That(actual.IndirectRange, Is.EqualTo(expected.IndirectRange));
            Assert.That(actual.MaxMovementPoints, Is.EqualTo(expected.MaxMovementPoints));
            Assert.That(actual.SpottingRange, Is.EqualTo(expected.SpottingRange));
            Assert.That(actual.Dogfighting, Is.EqualTo(expected.Dogfighting));
            Assert.That(actual.Maneuverability, Is.EqualTo(expected.Maneuverability));
            Assert.That(actual.TopSpeed, Is.EqualTo(expected.TopSpeed));
            Assert.That(actual.Survivability, Is.EqualTo(expected.Survivability));
            Assert.That(actual.GroundAttack, Is.EqualTo(expected.GroundAttack));
            Assert.That(actual.OrdinanceLoad, Is.EqualTo(expected.OrdinanceLoad));
        }

        [Test]
        public void ShahineAndAuf1_KeepSaudiChassisAndOrganizationDistinct()
        {
            Assert.That(P(WeaponType.SPSAM_SHAHINE_SA).MovementMedium, Is.EqualTo(MovementMedium.Tracked));
            Assert.That(P(WeaponType.SPSAM_CROTALE_FR).MovementMedium, Is.EqualTo(MovementMedium.Wheeled));
            Assert.That(P(WeaponType.SPSAM_SHAHINE_SA).TurnAvailable, Is.EqualTo(504));
            AssertCombatEqual(P(WeaponType.SPA_AUF1_SA), P(WeaponType.SPA_AUF1_FR));
            Assert.That(P(WeaponType.SPA_AUF1_SA).ICM, Is.EqualTo(1f));
            Assert.That(P(WeaponType.SPA_AUF1_FR).ICM, Is.EqualTo(1.05f).Within(0.0001f));
            Assert.That(P(WeaponType.SPA_AUF1_SA).IntelReportStats[WeaponType.SPA_AUF1_SA], Is.EqualTo(24));
            Assert.That(P(WeaponType.SPA_AUF1_SA).TurnAvailable, Is.EqualTo(516));
        }

        [Test]
        public void InfantryCarriersAndTruck_DoNotDoubleCountTheBrigade()
        {
            foreach (var id in new[] { "SA_INFANTRY_BRIGADE", "SA_MOT_INFANTRY_BRIGADE_M113", "SA_MECH_INFANTRY_BRIGADE_AMX10P" })
            {
                var u = CombatUnitDB.GetUnitTemplate(id);
                Assert.That(u.EquipmentBays.GetIntelReport().Personnel, Is.EqualTo(2400), id);
                Assert.That(u.EquipmentBays.GetIntelReport().TANK,
                    Is.EqualTo(id == "SA_INFANTRY_BRIGADE" ? 0 : 30), id);
            }
            foreach (var type in new[] { WeaponType.APC_M113_SA, WeaponType.IFV_AMX10P_SA })
                Assert.That(P(type).IntelReportStats,
                    Is.EquivalentTo(new Dictionary<WeaponType, int> { { type, 90 } }));
            Assert.That(P(WeaponType.IFV_AMX10P_SA).HasCapability(WeaponCapability.Amphibious), Is.True);
            var truck = P(WeaponType.TRK_GEN_SA);
            Assert.That(truck.IntelReportStats, Is.Empty);
            Assert.That(truck.HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(CombatUnitDB.GetUnitTemplate("SA_INFANTRY_BRIGADE").EquipmentBays
                .GetIcon(DeploymentPosition.Mobile, HexDirection.NW), Is.EqualTo(SpriteManager.SA_Truck));
            Assert.That(P(WeaponType.TANK_AMX30_SA).IntelReportStats[WeaponType.TANK_AMX30_SA], Is.EqualTo(60));
        }

        [Test]
        public void HawkAndVulcan_AreExplicitSharedProfiles_HawkUsesSaudiTruck()
        {
            var hawk = CombatUnitDB.GetUnitTemplate("SA_HAWK_SAM_REGIMENT");
            Assert.That(hawk.EquipmentBays.Deployed, Is.EqualTo(WeaponType.SAM_HAWK_US));
            Assert.That(hawk.EquipmentBays.Mobile, Is.EqualTo(WeaponType.TRK_GEN_SA));
            var vulcan = CombatUnitDB.GetUnitTemplate("SA_M163_AIR_DEFENSE_REGIMENT");
            Assert.That(vulcan.EquipmentBays.Deployed, Is.EqualTo(WeaponType.SPAAA_M163_US));
            Assert.That(vulcan.EquipmentBays.Mobile, Is.EqualTo(WeaponType.NONE));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("SAM_HAWK_SA"));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("SPAAA_M163_SA"));
        }

        [Test]
        public void EarlySaudiEagle_UsesSparrowLineAndAircraftOnlyCensus()
        {
            var eagle = P(WeaponType.FGT_F15_SA);
            Assert.That(eagle.Dogfighting, Is.EqualTo(14));
            Assert.That(eagle.Dogfighting, Is.EqualTo(P(WeaponType.FGT_F15_US).Dogfighting - 1));
            Assert.That(eagle.GroundAttack, Is.EqualTo(2));
            Assert.That(eagle.OrdinanceLoad, Is.EqualTo(6));
            Assert.That(eagle.TurnAvailable, Is.EqualTo(528));
            Assert.That(eagle.PrestigeCost, Is.EqualTo(P(WeaponType.FGT_F15_US).PrestigeCost));
            foreach (var type in new[] { WeaponType.FGT_F15_SA, WeaponType.FGT_F5_SA })
                Assert.That(P(type).IntelReportStats,
                    Is.EquivalentTo(new Dictionary<WeaponType, int> { { type, 24 } }));
        }

        [Test]
        public void NationalGuard_UsesSaudiTruckWithoutArmoredCensusOrV150()
        {
            var u = CombatUnitDB.CreateUnitFromTemplate("SA_NATIONAL_GUARD_BRIGADE", "Guard transport test");
            var bays = u.EquipmentBays;
            Assert.That(bays.GetIntelReport().Personnel, Is.EqualTo(2400));
            Assert.That(bays.GetIntelReport().TANK, Is.Zero);
            Assert.That(P(WeaponType.INF_GUARD_SA).IntelReportStats.Keys.Any(type =>
                type.ToString().StartsWith("APC_", StringComparison.Ordinal)
                || type.ToString().StartsWith("IFV_", StringComparison.Ordinal)), Is.False);
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.NW), Is.EqualTo(SpriteManager.SA_Guard));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.NW), Is.EqualTo(SpriteManager.SA_Truck));
            u.SetDeploymentPosition(DeploymentPosition.Mobile);
            Assert.That(u.GetActiveWeaponProfile().HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(P(bays.Mobile).IntelReportStats, Is.Empty);
            Assert.That(Enum.GetNames(typeof(WeaponType)).Any(name => name.Contains("V150")), Is.False);
        }

        [Test]
        public void SaudiCatalog_ConnectsApprovedNationalEquipment_WithoutAirMobileAdditions()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.Nationality == Nationality.SAUD).ToArray();
            Assert.That(units.Length, Is.EqualTo(14));
            Assert.That(units.All(u => u.EquipmentBays.Embarked == WeaponType.NONE), Is.True);
            Assert.That(units.Any(u => u.Classification == UnitClassification.HELO
                || u.Classification == UnitClassification.AM || u.Classification == UnitClassification.MAM), Is.False);
            var equipment = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile })
                .Where(t => t != WeaponType.NONE).Distinct().ToArray();
            var national = equipment.Where(t => t.ToString().EndsWith("_SA", StringComparison.Ordinal)).ToArray();
            Assert.That(national.Length, Is.EqualTo(14));
            Assert.That(national.Select(t => P(t).IconProfile.Icon).Distinct().Count(), Is.EqualTo(13));
            Assert.That(equipment.Except(national), Is.EquivalentTo(new[] { WeaponType.SAM_HAWK_US, WeaponType.SPAAA_M163_US }));
        }
    }
}
