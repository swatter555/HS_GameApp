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
    public class GenericNatoRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.IFV_YPR765_NATO, SpriteManager.NATO_YPR765, MovementMedium.Tracked)]
        [TestCase(WeaponType.SPA_M109_NATO, SpriteManager.NATO_M109, MovementMedium.Tracked)]
        [TestCase(WeaponType.RCN_M113CV_NATO, SpriteManager.NATO_M113CV, MovementMedium.Tracked)]
        [TestCase(WeaponType.SPAAA_PRTL_NATO, SpriteManager.NATO_PRTL, MovementMedium.Tracked)]
        [TestCase(WeaponType.TANK_CENTURION_NATO, SpriteManager.NATO_Centurion, MovementMedium.Tracked)]
        [TestCase(WeaponType.AAA_GEN_NATO, SpriteManager.NATO_AAA, MovementMedium.Foot)]
        [TestCase(WeaponType.FGT_F16_NATO, SpriteManager.NATO_F16, MovementMedium.FixedWing)]
        public void NewProfiles_OwnTheirApprovedArtAndMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).IconProfile.IconType, Is.EqualTo(RegimentIconType.Single));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
        }

        [TestCase("NL_ARMOURED_INFANTRY_BRIGADE", Nationality.NE, UnitClassification.MECH, WeaponType.INF_MECH_NL, WeaponType.IFV_YPR765_NATO)]
        [TestCase("BE_MECH_INFANTRY_BRIGADE", Nationality.BE, UnitClassification.MECH, WeaponType.INF_MECH_BE, WeaponType.IFV_YPR765_NATO)]
        [TestCase("NL_RECON_UNIT", Nationality.NE, UnitClassification.RECON, WeaponType.RCN_M113CV_NATO, WeaponType.NONE)]
        [TestCase("NL_AIR_DEFENSE_REGIMENT", Nationality.NE, UnitClassification.SPAAA, WeaponType.SPAAA_PRTL_NATO, WeaponType.NONE)]
        [TestCase("NL_F16_FIGHTER_SQUADRON", Nationality.NE, UnitClassification.FGT, WeaponType.FGT_F16_NATO, WeaponType.NONE)]
        [TestCase("NL_ARMOURED_INFANTRY_BRIGADE_M113", Nationality.NE, UnitClassification.MECH, WeaponType.INF_MECH_NL, WeaponType.APC_M113_NATO)]
        [TestCase("BE_MECH_INFANTRY_BRIGADE_M113", Nationality.BE, UnitClassification.MECH, WeaponType.INF_MECH_BE, WeaponType.APC_M113_NATO)]
        [TestCase("NL_M109_ARTILLERY_REGIMENT", Nationality.NE, UnitClassification.SPA, WeaponType.SPA_M109_NATO, WeaponType.NONE)]
        [TestCase("BE_M109_ARTILLERY_REGIMENT", Nationality.BE, UnitClassification.SPA, WeaponType.SPA_M109_NATO, WeaponType.NONE)]
        [TestCase("DK_M109_ARTILLERY_REGIMENT", Nationality.DE, UnitClassification.SPA, WeaponType.SPA_M109_NATO, WeaponType.NONE)]
        [TestCase("NL_LIGHT_ARTILLERY_REGIMENT", Nationality.NE, UnitClassification.ART, WeaponType.ART_LIGHT_NATO, WeaponType.TRK_GEN_NATO)]
        [TestCase("BE_LIGHT_ARTILLERY_REGIMENT", Nationality.BE, UnitClassification.ART, WeaponType.ART_LIGHT_NATO, WeaponType.TRK_GEN_NATO)]
        [TestCase("DK_LIGHT_ARTILLERY_REGIMENT", Nationality.DE, UnitClassification.ART, WeaponType.ART_LIGHT_NATO, WeaponType.TRK_GEN_NATO)]
        [TestCase("NL_TOWED_AAA_REGIMENT", Nationality.NE, UnitClassification.AAA, WeaponType.AAA_GEN_NATO, WeaponType.TRK_GEN_NATO)]
        [TestCase("BE_TOWED_AAA_REGIMENT", Nationality.BE, UnitClassification.AAA, WeaponType.AAA_GEN_NATO, WeaponType.TRK_GEN_NATO)]
        [TestCase("DK_CENTURION_BRIGADE", Nationality.DE, UnitClassification.TANK, WeaponType.TANK_CENTURION_NATO, WeaponType.NONE)]
        public void ChangedAndNewFormations_HaveLegalBaysAndNationalExperience(string id, Nationality nationality,
            UnitClassification classification, WeaponType deployed, WeaponType mobile)
        {
            var u = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(u, Is.Not.Null, id);
            Assert.That(u.Nationality, Is.EqualTo(nationality));
            Assert.That(u.Side, Is.EqualTo(Side.AI));
            Assert.That(u.Classification, Is.EqualTo(classification));
            Assert.That(u.ExperienceLevel, Is.EqualTo(nationality == Nationality.BE ? ExperienceLevel.Trained : ExperienceLevel.Experienced));
            var bays = u.EquipmentBays;
            Assert.That(bays.Deployed, Is.EqualTo(deployed));
            Assert.That(bays.Mobile, Is.EqualTo(mobile));
            Assert.That(bays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(bays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE)
                Assert.That(bays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
            UnitRole expected = classification == UnitClassification.SPAAA || classification == UnitClassification.AAA
                ? UnitRole.AirDefenseArea : classification == UnitClassification.FGT ? UnitRole.AirSuperiority
                : classification == UnitClassification.ART || classification == UnitClassification.SPA
                    ? UnitRole.GroundCombatIndirect : UnitRole.GroundCombat;
            Assert.That(u.Role, Is.EqualTo(expected));
        }

        [Test]
        public void Ypr_Uses25mmWithoutAtgm_AndBothCarrierOptionsCountOnlyTheirOwnVehicles()
        {
            var ypr = P(WeaponType.IFV_YPR765_NATO);
            Assert.That(ypr.HardAttack, Is.EqualTo(4), "No ATGM rail on this carrier.");
            Assert.That(ypr.SoftAttack, Is.EqualTo(9));
            Assert.That(ypr.MaxMovementPoints, Is.EqualTo(10));
            Assert.That(ypr.ICM, Is.EqualTo(1f));
            Assert.That(ypr.TurnAvailable, Is.EqualTo(468));
            Assert.That(ypr.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.IFV_YPR765_NATO, 102 } }));
            Assert.That(P(WeaponType.APC_M113_NATO).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.APC_M113_NATO, 102 } }));
            foreach (var id in new[] { "NL_ARMOURED_INFANTRY_BRIGADE", "BE_MECH_INFANTRY_BRIGADE" })
            {
                var yprReport = CombatUnitDB.GetUnitTemplate(id).EquipmentBays.GetIntelReport();
                var m113Report = CombatUnitDB.GetUnitTemplate(id + "_M113").EquipmentBays.GetIntelReport();
                Assert.That(yprReport.IFV, Is.EqualTo(102));
                Assert.That(yprReport.APC, Is.Zero);
                Assert.That(m113Report.APC, Is.EqualTo(102));
                Assert.That(m113Report.IFV, Is.Zero);
                Assert.That(m113Report.Personnel, Is.EqualTo(yprReport.Personnel));
                Assert.That(m113Report.TANK, Is.EqualTo(yprReport.TANK));
            }
        }

        [TestCase(WeaponType.TANK_LEOPARD1_NL, WeaponType.INF_MECH_NL, WeaponType.IFV_YPR765_NATO, 2000, 84, 60, 2150, 32, 18)]
        [TestCase(WeaponType.TANK_LEOPARD1_BE, WeaponType.INF_MECH_BE, WeaponType.IFV_YPR765_NATO, 1900, 72, 55, 2050, 28, 18)]
        [TestCase(WeaponType.TANK_LEOPARD1_DK, WeaponType.INF_MECH_DK, WeaponType.APC_M113_NATO, 1600, 60, 48, 1850, 24, 12)]
        public void ExistingNationalCensuses_KeepTheirQuantitiesAndUseTheCorrectEquipment(WeaponType tank, WeaponType infantry,
            WeaponType carrier, int tankMen, int tanks, int carriers, int infantryMen, int organicTanks, int guns)
        {
            Assert.That(P(tank).IntelReportStats[WeaponType.Personnel], Is.EqualTo(tankMen));
            Assert.That(P(tank).IntelReportStats[tank], Is.EqualTo(tanks));
            Assert.That(P(tank).IntelReportStats[carrier], Is.EqualTo(carriers));
            Assert.That(P(infantry).IntelReportStats[WeaponType.Personnel], Is.EqualTo(infantryMen));
            Assert.That(P(infantry).IntelReportStats[tank], Is.EqualTo(organicTanks));
            foreach (var type in new[] { tank, infantry })
            {
                Assert.That(P(type).IntelReportStats[WeaponType.SPA_M109_NATO], Is.EqualTo(guns));
                Assert.That(P(type).IntelReportStats.ContainsKey(WeaponType.SPA_M109_US), Is.False);
            }
            Assert.That(P(tank).IconProfile.Icon, Is.EqualTo(SpriteManager.NATO_Leopard1));
            Assert.That(P(tank).PrestigeCost, Is.EqualTo(65));
            Assert.That(P(tank).ICM, Is.EqualTo(1.05f * 1.05f).Within(0.0001f), "Existing optics and laser multipliers combine multiplicatively.");
        }

        [Test]
        public void M109_UsesConventionalNatoFires_WithoutUsPrecisionOrForeignSupportEquipment()
        {
            var p = P(WeaponType.SPA_M109_NATO);
            Assert.That(p.HardAttack, Is.EqualTo(5));
            Assert.That(p.SoftAttack, Is.EqualTo(10));
            Assert.That(p.IndirectRange, Is.EqualTo(GameData.INDIRECT_RANGE_MEDIUM));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(10));
            Assert.That(p.ICM, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(p.PrestigeCost, Is.EqualTo(P(WeaponType.SPA_M109_GE).PrestigeCost));
            Assert.That(p.HardAttack, Is.LessThan(P(WeaponType.SPA_M109_US).HardAttack));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 950 }, { WeaponType.SPA_M109_NATO, 54 }, { WeaponType.APC_M113_NATO, 12 }
            }));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.SPA_M109_NATO), Is.EqualTo(WeaponSoundFamily.ArtilleryGun));
        }

        [Test]
        public void DutchRecon_IsDistinctFromIranianM113AndDoesNotBecomeAnInfantryCarrier()
        {
            var p = P(WeaponType.RCN_M113CV_NATO);
            Assert.That(p.TargetClass, Is.EqualTo(TargetClass.Soft));
            Assert.That(p.SpottingRange, Is.EqualTo(3));
            Assert.That(p.HardAttack, Is.EqualTo(2));
            Assert.That(p.SoftAttack, Is.EqualTo(6));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(10));
            Assert.That(p.HasCapability(WeaponCapability.Amphibious), Is.True);
            Assert.That(p.TransportCategory, Is.EqualTo(TransportCategory.None));
            Assert.That(P(WeaponType.RCN_M113_IR).MaxMovementPoints, Is.EqualTo(8));
            Assert.That(P(WeaponType.TANK_LEOPARD1_NL).IntelReportStats[WeaponType.RCN_M113CV_NATO], Is.EqualTo(12));
            Assert.That(p.IntelReportStats[WeaponType.RCN_M113CV_NATO], Is.EqualTo(36));
            Assert.That(p.IntelReportStats.Keys.Any(t => t.ToString().EndsWith("_UK", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void Prtl_HasRadarGunFireAndDutchSupport_WhileHawkKeepsItsSeparateRole()
        {
            var p = P(WeaponType.SPAAA_PRTL_NATO);
            Assert.That(p.GroundAirAttack, Is.EqualTo(13));
            Assert.That(p.IndirectRange, Is.EqualTo(GameData.INDIRECT_RANGE_SHORT));
            Assert.That(p.HardAttack, Is.EqualTo(4));
            Assert.That(p.SoftAttack, Is.EqualTo(9));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 1100 }, { WeaponType.SPAAA_PRTL_NATO, 18 },
                { WeaponType.IFV_YPR765_NATO, 24 }, { WeaponType.RCN_M113CV_NATO, 12 }
            }));
            var hawk = CombatUnitDB.GetUnitTemplate("NL_HAWK_REGIMENT").EquipmentBays;
            Assert.That(hawk.Deployed, Is.EqualTo(WeaponType.SAM_HAWK_US));
            Assert.That(hawk.Mobile, Is.EqualTo(WeaponType.TRK_GEN_NATO));
            Assert.That(P(hawk.Deployed).IndirectRange, Is.GreaterThan(p.IndirectRange));
        }

        [Test]
        public void Centurion_IsTheApprovedOlderCheaperDanishBrigade_WithNoModernFireControl()
        {
            var p = P(WeaponType.TANK_CENTURION_NATO);
            var leopard = P(WeaponType.TANK_LEOPARD1_DK);
            Assert.That(p.HardAttack, Is.EqualTo(8));
            Assert.That(p.HardDefense, Is.EqualTo(6));
            Assert.That(p.SoftAttack, Is.EqualTo(6));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(8));
            Assert.That(p.SpottingRange, Is.EqualTo(2));
            Assert.That(p.ICM, Is.EqualTo(1f));
            Assert.That(p.TurnAvailable, Is.EqualTo(312));
            Assert.That(p.PrestigeCost, Is.EqualTo(55));
            Assert.That(p.PrestigeCost, Is.LessThan(leopard.PrestigeCost));
            var expected = new Dictionary<WeaponType, int>(leopard.IntelReportStats);
            expected.Remove(WeaponType.TANK_LEOPARD1_DK);
            expected.Add(WeaponType.TANK_CENTURION_NATO, 60);
            Assert.That(p.IntelReportStats, Is.EquivalentTo(expected));
        }

        [Test]
        public void F16_RetainsEstablishedCombatCapability_WithNatoCensusAndArt()
        {
            var p = P(WeaponType.FGT_F16_NATO);
            var us = P(WeaponType.FGT_F16_US);
            Assert.That(p.Dogfighting, Is.EqualTo(us.Dogfighting));
            Assert.That(p.Maneuverability, Is.EqualTo(us.Maneuverability));
            Assert.That(p.TopSpeed, Is.EqualTo(us.TopSpeed));
            Assert.That(p.Survivability, Is.EqualTo(us.Survivability));
            Assert.That(p.GroundAttack, Is.EqualTo(us.GroundAttack));
            Assert.That(p.OrdinanceLoad, Is.EqualTo(us.OrdinanceLoad));
            Assert.That(p.GaBonusVsHard, Is.EqualTo(us.GaBonusVsHard));
            Assert.That(p.ICM, Is.EqualTo(us.ICM));
            Assert.That(p.PrestigeCost, Is.EqualTo(us.PrestigeCost));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.FGT_F16_NATO, 36 } }));
        }

        [Test]
        public void NationalAssignments_RemainExplicit_AndAllThirteenNatoPicturesAreReachable()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate).ToList();
            Assert.That(units.Count(u => u.Nationality == Nationality.NE), Is.EqualTo(11));
            Assert.That(units.Count(u => u.Nationality == Nationality.BE), Is.EqualTo(9));
            Assert.That(units.Count(u => u.Nationality == Nationality.DE), Is.EqualTo(7));
            foreach (var type in new[] { WeaponType.SPAAA_PRTL_NATO, WeaponType.RCN_M113CV_NATO })
                Assert.That(units.Where(u => u.EquipmentBays.Deployed == type).All(u => u.Nationality == Nationality.NE), Is.True);
            Assert.That(units.Where(u => u.EquipmentBays.Deployed == WeaponType.TANK_CENTURION_NATO).All(u => u.Nationality == Nationality.DE), Is.True);
            Assert.That(units.Where(u => u.Nationality == Nationality.DE).Any(u => GameData.IsAirDefenseClassification(u.Classification)), Is.False);
            Assert.That(CombatUnitDB.GetUnitTemplate("BE_AIR_DEFENSE_REGIMENT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.SPAAA_GEPARD_GE));
            Assert.That(CombatUnitDB.GetUnitTemplate("BE_RECON_UNIT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.RCN_FV105_UK));
            Assert.That(CombatUnitDB.GetUnitTemplate("DK_RECON_UNIT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.RCN_FV105_UK));
            var pictures = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile, u.EquipmentBays.Embarked })
                .Where(t => t != WeaponType.NONE).Select(t => P(t).IconProfile.Icon)
                .Where(icon => icon.StartsWith("NATO_", StringComparison.Ordinal)).Distinct().ToList();
            Assert.That(pictures.Count, Is.EqualTo(13));
        }
    }
}
