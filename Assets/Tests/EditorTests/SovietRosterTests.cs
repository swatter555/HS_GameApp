using System;
using System.Collections.Generic;
using System.Linq;
using HammerAndSickle.Controllers;
using HammerAndSickle.Core.Campaign;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    public class SovietRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.TANK_T55MV_SV, WeaponType.TANK_T55A_SV, 7)]
        [TestCase(WeaponType.TANK_T62MV_SV, WeaponType.TANK_T62A_SV, 8)]
        public void MvTanks_AddLightEra_WithoutChangingTheirBaseWeaponsOrMobility(
            WeaponType type, WeaponType original, int hardAttack)
        {
            var p = P(type);
            var b = P(original);
            Assert.That(p, Is.Not.Null);
            Assert.That(p.HardAttack, Is.EqualTo(hardAttack));
            Assert.That(p.HardAttack, Is.EqualTo(b.HardAttack));
            Assert.That(p.HardDefense, Is.EqualTo(b.HardDefense + 2));
            Assert.That(p.HardDefense, Is.EqualTo(8));
            Assert.That(p.SoftAttack, Is.EqualTo(b.SoftAttack));
            Assert.That(p.SoftDefense, Is.EqualTo(b.SoftDefense));
            Assert.That(p.GroundAirAttack, Is.EqualTo(b.GroundAirAttack));
            Assert.That(p.GroundAirDefense, Is.EqualTo(b.GroundAirDefense));
            Assert.That(p.PrimaryRange, Is.EqualTo(b.PrimaryRange));
            Assert.That(p.IndirectRange, Is.EqualTo(b.IndirectRange));
            Assert.That(p.SpottingRange, Is.EqualTo(b.SpottingRange));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(b.MaxMovementPoints));
            Assert.That(p.MovementMedium, Is.EqualTo(MovementMedium.Tracked));
            Assert.That(p.TargetClass, Is.EqualTo(TargetClass.Hard));
            Assert.That(p.UpgradePath, Is.EqualTo(UpgradePath.TANK));
            Assert.That(p.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(p.ICM, Is.EqualTo(b.ICM).Within(0.0001f));
            foreach (WeaponCapability capability in Enum.GetValues(typeof(WeaponCapability)))
                Assert.That(p.HasCapability(capability), Is.EqualTo(b.HasCapability(capability)), capability.ToString());
        }

        [TestCase(WeaponType.TANK_T55MV_SV)]
        [TestCase(WeaponType.TANK_T62MV_SV)]
        public void MvTanks_KeepGen1Price_AndBecomeAvailableInJanuary1985(WeaponType type)
        {
            var p = P(type);
            Assert.That(p.PrestigeCost, Is.EqualTo(65));
            Assert.That(p.TurnAvailable, Is.EqualTo(CampaignDateCalendar.DateToTurn(011985)));
            Assert.That(new CampaignDateCalendar(121984, 011985).IsWeaponSystemAvailable(p), Is.False);
            Assert.That(new CampaignDateCalendar(011985, 011985).IsWeaponSystemAvailable(p), Is.True);
        }

        [TestCase(WeaponType.TANK_T55MV_SV, WeaponType.TANK_T55A_SV)]
        [TestCase(WeaponType.TANK_T62MV_SV, WeaponType.TANK_T62A_SV)]
        public void MvCensus_ReplacesOnlyTheRegimentsOwn94Tanks(WeaponType type, WeaponType original)
        {
            var expected = new Dictionary<WeaponType, int>(P(original).IntelReportStats);
            Assert.That(expected.Remove(original), Is.True);
            expected.Add(type, 94);
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(expected));
            Assert.That(P(type).IntelReportStats[WeaponType.Personnel], Is.EqualTo(1143));
        }

        [TestCase("USSR_TR_T55MV", WeaponType.TANK_T55MV_SV, SpriteManager.SV_T55MV)]
        [TestCase("USSR_TR_T62MV", WeaponType.TANK_T62MV_SV, SpriteManager.SV_T62MV)]
        public void MvRegiments_UseTrainedSovietTankDefaultsAndTheirOwnArt(
            string id, WeaponType type, string sprite)
        {
            var u = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(u, Is.Not.Null);
            Assert.That(u.Classification, Is.EqualTo(UnitClassification.TANK));
            Assert.That(u.Role, Is.EqualTo(UnitRole.GroundCombat));
            Assert.That(u.Side, Is.EqualTo(Side.Player));
            Assert.That(u.Nationality, Is.EqualTo(Nationality.USSR));
            Assert.That(u.ExperienceLevel, Is.EqualTo(ExperienceLevel.Trained));
            Assert.That(u.EquipmentBays.Deployed, Is.EqualTo(type));
            Assert.That(u.EquipmentBays.Mobile, Is.EqualTo(WeaponType.NONE));
            Assert.That(u.EquipmentBays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(u.EquipmentBays.CanAccept(u.Classification, EquipmentBay.Deployed, type), Is.True);
            Assert.That(u.EquipmentBays.CanAccept(u.Classification, EquipmentBay.Mobile, WeaponType.TRK_GEN_SV), Is.False);
            Assert.That(u.EquipmentBays.TotalIntelStats, Is.EquivalentTo(P(type).IntelReportStats));
            Assert.That(P(type).IconProfile.IconType, Is.EqualTo(RegimentIconType.Single));
            foreach (HexDirection facing in Enum.GetValues(typeof(HexDirection)))
                Assert.That(u.EquipmentBays.GetIcon(DeploymentPosition.Deployed, facing), Is.EqualTo(sprite));
        }

        [TestCase(WeaponType.TANK_T55MV_SV)]
        [TestCase(WeaponType.TANK_T62MV_SV)]
        public void MvTankFormations_AreSovietOnly(WeaponType type)
        {
            var users = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.EquipmentBays.Deployed == type).ToList();
            Assert.That(users.Count, Is.EqualTo(1));
            Assert.That(users.Single().Nationality, Is.EqualTo(Nationality.USSR));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain(type.ToString().Replace("_SV", "_IQ")));
        }

        [TestCase("USSR_TR_T55", WeaponType.TANK_T55A_SV, SpriteManager.SV_T55A)]
        [TestCase("USSR_TR_T62A", WeaponType.TANK_T62A_SV, SpriteManager.SV_T62)]
        public void OriginalTankOptions_KeepTheirProfileCensusAndArt(string id, WeaponType type, string sprite)
        {
            Assert.That(CombatUnitDB.GetUnitTemplate(id).EquipmentBays.Deployed, Is.EqualTo(type));
            Assert.That(P(type).HardDefense, Is.EqualTo(6));
            Assert.That(P(type).IntelReportStats[type], Is.EqualTo(94));
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(sprite));
        }

        [Test]
        public void Existing2S5_AlreadyHasItsArtAndFormation()
        {
            var u = CombatUnitDB.GetUnitTemplate("USSR_SPA_2S5");
            Assert.That(u.Classification, Is.EqualTo(UnitClassification.SPA));
            Assert.That(u.EquipmentBays.Deployed, Is.EqualTo(WeaponType.SPA_2S5_SV));
            Assert.That(P(WeaponType.SPA_2S5_SV).IconProfile.Icon, Is.EqualTo(SpriteManager.SV_2S5));
            Assert.That(P(WeaponType.SPA_2S5_SV).IntelReportStats[WeaponType.SPA_2S5_SV], Is.GreaterThan(0));
        }
    }
}
