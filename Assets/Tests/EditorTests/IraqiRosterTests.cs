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
    public class IraqiRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.TANK_T72M_IQ, SpriteManager.IQ_T72, MovementMedium.Tracked)]
        [TestCase(WeaponType.HEL_MI8AT_IQ, SpriteManager.IQ_MI8AT_Frame0, MovementMedium.Helo)]
        [TestCase(WeaponType.RCN_BRDM2_IQ, SpriteManager.IQ_BRDM2, MovementMedium.Wheeled)]
        [TestCase(WeaponType.FGT_MIRAGEF1_IQ, SpriteManager.IQ_MirageF1, MovementMedium.FixedWing)]
        [TestCase(WeaponType.ART_LIGHT_IQ, SpriteManager.IQ_LightArt, MovementMedium.Foot)]
        [TestCase(WeaponType.ART_HEAVY_IQ, SpriteManager.IQ_HeavyArt, MovementMedium.Foot)]
        [TestCase(WeaponType.AAA_GEN_IQ, SpriteManager.IQ_AAA, MovementMedium.Foot)]
        [TestCase(WeaponType.TRK_GEN_IQ, SpriteManager.IQ_Truck, MovementMedium.Wheeled)]
        [TestCase(WeaponType.SAM_S75_IQ, SpriteManager.IQ_S75, MovementMedium.Foot)]
        public void NewEquipment_UsesNationalArtAndCorrectMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
        }

        [TestCase("IQ_TANK_REGIMENT_T72M", UnitClassification.TANK, WeaponType.TANK_T72M_IQ, WeaponType.NONE)]
        [TestCase("IQ_MI8AT_ATTACK_SQUADRON", UnitClassification.HELO, WeaponType.HEL_MI8AT_IQ, WeaponType.NONE)]
        [TestCase("IQ_RECON_REGIMENT_BRDM2", UnitClassification.RECON, WeaponType.RCN_BRDM2_IQ, WeaponType.NONE)]
        [TestCase("IQ_MIRAGEF1_FIGHTER_SQUADRON", UnitClassification.FGT, WeaponType.FGT_MIRAGEF1_IQ, WeaponType.NONE)]
        [TestCase("IQ_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART, WeaponType.ART_LIGHT_IQ, WeaponType.TRK_GEN_IQ)]
        [TestCase("IQ_TOWED_AAA_REGIMENT", UnitClassification.AAA, WeaponType.AAA_GEN_IQ, WeaponType.TRK_GEN_IQ)]
        [TestCase("IQ_TOWED_ARTILLERY_REGIMENT", UnitClassification.ART, WeaponType.ART_HEAVY_IQ, WeaponType.TRK_GEN_IQ)]
        [TestCase("IQ_SAM_REGIMENT", UnitClassification.SAM, WeaponType.SAM_S75_IQ, WeaponType.TRK_GEN_IQ)]
        [TestCase("IQ_INFANTRY_REGIMENT", UnitClassification.INF, WeaponType.INF_REG_IQ, WeaponType.TRK_GEN_IQ)]
        public void NewAndReassignedFormations_UseLegalNationalBays(string id, UnitClassification classification,
            WeaponType deployed, WeaponType mobile)
        {
            var unit = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(unit, Is.Not.Null, id);
            Assert.That(unit.Nationality, Is.EqualTo(Nationality.IQ));
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
        public void T72M_IsTheExportTankLine_WithoutChangingTheEarlierTankCounters()
        {
            var tank = P(WeaponType.TANK_T72M_IQ);
            var soviet = P(WeaponType.TANK_T72A_SV);
            Assert.That(tank.HardAttack, Is.EqualTo(soviet.HardAttack));
            Assert.That(tank.HardDefense, Is.EqualTo(soviet.HardDefense - 2));
            Assert.That(tank.SoftDefense, Is.EqualTo(soviet.SoftDefense - 1));
            Assert.That(tank.ICM, Is.EqualTo(soviet.ICM * 0.9f).Within(0.001f));
            Assert.That(tank.PrestigeCost, Is.EqualTo(soviet.PrestigeCost));
            Assert.That(tank.TurnAvailable, Is.EqualTo(528));
            Assert.That(tank.IntelReportStats[WeaponType.Personnel], Is.EqualTo(950));
            Assert.That(tank.IntelReportStats[WeaponType.TANK_T72M_IQ], Is.EqualTo(105));
            Assert.That(P(WeaponType.TANK_T55A_IQ).IntelReportStats[WeaponType.TANK_T55A_IQ], Is.EqualTo(105));
            Assert.That(P(WeaponType.TANK_T62A_IQ).IntelReportStats[WeaponType.TANK_T62A_IQ], Is.EqualTo(104));
            Assert.That(P(WeaponType.TANK_T55A_IQ).IntelReportStats[WeaponType.RCN_BRDM2_IQ], Is.EqualTo(12));
            Assert.That(P(WeaponType.TANK_T62A_IQ).IntelReportStats[WeaponType.RCN_BRDM2_IQ], Is.EqualTo(12));
            Assert.That(P(WeaponType.INF_REG_IQ).IntelReportStats[WeaponType.ART_HEAVY_IQ], Is.EqualTo(18));
        }

        [Test]
        public void ArmedMi8_IsARocketAttackCounter_WithLossCensusAndNoLiftRole()
        {
            var helo = P(WeaponType.HEL_MI8AT_IQ);
            Assert.That(helo.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(helo.HasCapability(WeaponCapability.NonCombatant), Is.False);
            Assert.That(helo.HardAttack, Is.EqualTo(7), "No inherited Mi-24 ATGM.");
            Assert.That(helo.SoftAttack, Is.EqualTo(11), "Helicopter baseline plus rocket pods.");
            Assert.That(helo.HardDefense, Is.EqualTo(6), "No inherited armoured cockpit.");
            Assert.That(helo.SoftDefense, Is.EqualTo(7));
            Assert.That(helo.GroundAirDefense, Is.EqualTo(10), "No inferred countermeasures.");
            Assert.That(helo.TurnAvailable, Is.EqualTo(348));
            Assert.That(helo.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
                { { WeaponType.HEL_MI8AT_IQ, 36 } }));
            Assert.That(helo.IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            var bays = CombatUnitDB.GetUnitTemplate("IQ_MI8AT_ATTACK_SQUADRON").EquipmentBays;
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.W), Is.EqualTo(SpriteManager.IQ_MI8AT_Frame0));
            Assert.That(bays.GetIntelReport().HEL, Is.EqualTo(36));
            Assert.That(bays.CanAccept(UnitClassification.AM, EquipmentBay.Embarked, WeaponType.HEL_MI8AT_IQ), Is.False);
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.HEL_MI8AT_IQ), Is.EqualTo(WeaponSoundFamily.HelicopterAttack));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("HEL_MI24_IQ"));
        }

        [Test]
        public void MirageF1_UsesAirExportResiduals_AndRetainsTheMultiroleStrikeLine()
        {
            var iraq = P(WeaponType.FGT_MIRAGEF1_IQ);
            var france = P(WeaponType.FGT_MIRAGEF1_FR);
            Assert.That(iraq.Dogfighting, Is.EqualTo(france.Dogfighting - 1));
            Assert.That(iraq.Survivability, Is.EqualTo(france.Survivability - 1));
            Assert.That(iraq.GroundAttack, Is.EqualTo(france.GroundAttack));
            Assert.That(iraq.ICM, Is.EqualTo(france.ICM), "The tank export trait must not affect aircraft.");
            Assert.That(iraq.TurnAvailable, Is.EqualTo(516));
            Assert.That(iraq.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
                { { WeaponType.FGT_MIRAGEF1_IQ, 36 } }));
        }

        [Test]
        public void S75_PreservesTheSiteSamEnvelope_AndSelectsIraqiArtInTheExistingTemplate()
        {
            var iraq = P(WeaponType.SAM_S75_IQ);
            Assert.That(iraq.IndirectRange, Is.EqualTo(6));
            Assert.That(iraq.GroundAirAttack, Is.EqualTo(15));
            Assert.That(iraq.MaxMovementPoints, Is.Zero);
            Assert.That(iraq.ICM, Is.EqualTo(P(WeaponType.SAM_S75_SV).ICM));
            Assert.That(iraq.HasCapability(WeaponCapability.AirDroppable), Is.True);
            Assert.That(iraq.HasCapability(WeaponCapability.HeloTransportable), Is.True);
            var bays = CombatUnitDB.GetUnitTemplate("IQ_SAM_REGIMENT").EquipmentBays;
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.E), Is.EqualTo(SpriteManager.IQ_S75));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.W), Is.EqualTo(SpriteManager.IQ_Truck));
            Assert.That(CombatUnitDB.GetUnitTemplate("USSR_SAM_S75").EquipmentBays.Deployed, Is.EqualTo(WeaponType.SAM_S75_SV));
        }

        [TestCase(WeaponType.ART_LIGHT_IQ, 700, 48, 6)]
        [TestCase(WeaponType.ART_HEAVY_IQ, 750, 36, 8)]
        [TestCase(WeaponType.AAA_GEN_IQ, 500, 18, 12)]
        [TestCase(WeaponType.SAM_S75_IQ, 750, 18, 21)]
        public void TowedSupport_HasNationalWeaponsAndPersonnel_WithoutEmbeddedCarriers(
            WeaponType type, int personnel, int weapons, int manpads)
        {
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, personnel }, { type, weapons }, { WeaponType.MANPAD_STRELA, manpads }
            }));
            Assert.That(P(WeaponType.TRK_GEN_IQ).IntelReportStats, Is.Empty);
            Assert.That(P(WeaponType.TRK_GEN_IQ).HasCapability(WeaponCapability.NonCombatant), Is.True);
        }

        [Test]
        public void IraqiCatalog_HasNationalBaysAndCensuses_WhileLegacyProfilesRemainRegistered()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.Nationality == Nationality.IQ).ToList();
            Assert.That(units.Count, Is.EqualTo(18));
            Assert.That(units.All(u => u.EquipmentBays.Embarked == WeaponType.NONE), Is.True,
                "The packet adds no Iraqi air-mobile force.");
            var profiles = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile })
                .Where(t => t != WeaponType.NONE).Distinct().ToList();
            Assert.That(profiles.All(t => t.ToString().EndsWith("_IQ", StringComparison.Ordinal)), Is.True);
            var foreignCensus = profiles.SelectMany(t => P(t).IntelReportStats.Keys)
                .Where(t => t.ToString().EndsWith("_SV", StringComparison.Ordinal) ||
                            t.ToString().EndsWith("_ARAB", StringComparison.Ordinal)).ToList();
            Assert.That(foreignCensus, Is.Empty, "Iraqi counters use their national support equipment.");
            Assert.That(P(WeaponType.RCN_BRDM2_IQ).HasCapability(WeaponCapability.Amphibious), Is.True);
            Assert.That(P(WeaponType.RCN_BRDM2_IQ).ICM, Is.EqualTo(P(WeaponType.RCN_BRDM2_SV).ICM));
            Assert.That(P(WeaponType.RCN_BRDM2_IQ).IntelReportStats[WeaponType.RCN_BRDM2_IQ], Is.EqualTo(36));
            Assert.That(WeaponProfileDB.HasWeaponProfile(WeaponType.ART_LIGHT_ARAB), Is.True);
            Assert.That(WeaponProfileDB.HasWeaponProfile(WeaponType.ART_HEAVY_ARAB), Is.True);
            Assert.That(WeaponProfileDB.HasWeaponProfile(WeaponType.TRK_GEN_ARAB), Is.True);
        }
    }
}
