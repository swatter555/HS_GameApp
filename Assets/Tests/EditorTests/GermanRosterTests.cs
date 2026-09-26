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
    public class GermanRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.SPSAM_ROLAND_GE, SpriteManager.GE_Roland, MovementMedium.Tracked)]
        [TestCase(WeaponType.ATT_ALPHAJET_GE, SpriteManager.GE_AlphaJet, MovementMedium.FixedWing)]
        [TestCase(WeaponType.ROC_MLRS_GE, SpriteManager.GE_MLRS, MovementMedium.Tracked)]
        [TestCase(WeaponType.ART_LIGHT_GE, SpriteManager.GE_LightArt, MovementMedium.Foot)]
        [TestCase(WeaponType.ART_HEAVY_GE, SpriteManager.GE_HeavyArt, MovementMedium.Foot)]
        [TestCase(WeaponType.AAA_GEN_GE, SpriteManager.GE_AAA, MovementMedium.Foot)]
        public void NewEquipment_UsesNationalArtAndCorrectMovement(WeaponType type, string icon, MovementMedium medium)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            Assert.That(P(type).IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(P(type).MovementMedium, Is.EqualTo(medium));
        }

        [TestCase("GE_PANZERGRENADIER_REGIMENT_M113", UnitClassification.MECH, UnitRole.GroundCombat, WeaponType.INF_REG_GE, WeaponType.APC_M113_GE)]
        [TestCase("GE_ROLAND_REGIMENT", UnitClassification.SPSAM, UnitRole.AirDefenseArea, WeaponType.SPSAM_ROLAND_GE, WeaponType.NONE)]
        [TestCase("GE_ALPHAJET_ATTACK_SQUADRON", UnitClassification.ATT, UnitRole.AirGroundAttack, WeaponType.ATT_ALPHAJET_GE, WeaponType.NONE)]
        [TestCase("GE_MLRS_REGIMENT", UnitClassification.ROC, UnitRole.GroundCombat, WeaponType.ROC_MLRS_GE, WeaponType.NONE)]
        [TestCase("GE_LIGHT_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.ART_LIGHT_GE, WeaponType.TRK_GEN_NATO)]
        [TestCase("GE_HEAVY_ARTILLERY_REGIMENT", UnitClassification.ART, UnitRole.GroundCombat, WeaponType.ART_HEAVY_GE, WeaponType.TRK_GEN_NATO)]
        [TestCase("GE_TOWED_AAA_REGIMENT", UnitClassification.AAA, UnitRole.AirDefenseArea, WeaponType.AAA_GEN_GE, WeaponType.TRK_GEN_NATO)]
        public void NewFormations_HaveLegalBaysAndWestGermanNationality(string id, UnitClassification classification,
            UnitRole role, WeaponType deployed, WeaponType mobile)
        {
            var unit = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(unit, Is.Not.Null, id);
            Assert.That(unit.Nationality, Is.EqualTo(Nationality.FRG), "DE is Denmark, not Germany.");
            Assert.That(unit.Side, Is.EqualTo(Side.AI));
            Assert.That(unit.Classification, Is.EqualTo(classification));
            Assert.That(unit.Role, Is.EqualTo(role));
            Assert.That(unit.ExperienceLevel, Is.EqualTo(ExperienceLevel.Experienced));
            var bays = unit.EquipmentBays;
            Assert.That(bays.Deployed, Is.EqualTo(deployed));
            Assert.That(bays.Mobile, Is.EqualTo(mobile));
            Assert.That(bays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(bays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE)
                Assert.That(bays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
        }

        [Test]
        public void Roland_IsTrackedPointDefense_AndKeepsGepardAndHawkSeparate()
        {
            var p = P(WeaponType.SPSAM_ROLAND_GE);
            Assert.That(p.HardAttack, Is.EqualTo(1));
            Assert.That(p.SoftAttack, Is.EqualTo(1));
            Assert.That(p.GroundAirAttack, Is.EqualTo(14));
            Assert.That(p.IndirectRange, Is.EqualTo(4));
            Assert.That(p.IndirectRange, Is.LessThan(P(WeaponType.SAM_HAWK_US).IndirectRange));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(10));
            Assert.That(p.TargetClass, Is.EqualTo(TargetClass.Hard));
            Assert.That(p.TurnAvailable, Is.EqualTo(516));
            Assert.That(p.PrestigeCost, Is.EqualTo(P(WeaponType.SPSAM_RAPIER_UK).PrestigeCost));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 1100 }, { WeaponType.SPSAM_ROLAND_GE, 18 },
                { WeaponType.IFV_MARDER_GE, 24 }, { WeaponType.RCN_LUCHS_GE, 12 }
            }));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.SPSAM_ROLAND_GE), Is.EqualTo(WeaponSoundFamily.SurfaceToAirMissile));
            Assert.That(CombatUnitDB.GetUnitTemplate("GE_AIR_DEFENSE_REGIMENT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.SPAAA_GEPARD_GE));
            Assert.That(CombatUnitDB.GetUnitTemplate("GE_HAWK_REGIMENT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.SAM_HAWK_US));
        }

        [Test]
        public void AlphaJet_IsALightStrikeOption_BelowTheJaguarPayloadAndCost()
        {
            var alpha = P(WeaponType.ATT_ALPHAJET_GE);
            var jaguar = P(WeaponType.ATT_JAGUAR_UK);
            Assert.That(alpha.TopSpeed, Is.EqualTo(7));
            Assert.That(alpha.GroundAttack, Is.EqualTo(6));
            Assert.That(alpha.OrdinanceLoad, Is.EqualTo(6));
            Assert.That(alpha.GroundAttack, Is.LessThan(jaguar.GroundAttack));
            Assert.That(alpha.OrdinanceLoad, Is.LessThan(jaguar.OrdinanceLoad));
            Assert.That(alpha.PrestigeCost, Is.LessThan(jaguar.PrestigeCost));
            Assert.That(alpha.OcSuppressionBonus, Is.Zero, "No runway-cratering or precision-strike package.");
            Assert.That(alpha.GaBonusVsHard, Is.Zero);
            Assert.That(alpha.GaBonusVsBase, Is.Zero);
            Assert.That(alpha.TurnAvailable, Is.EqualTo(492));
            Assert.That(alpha.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.ATT_ALPHAJET_GE, 36 } }));
        }

        [Test]
        public void Mars_SharesTheMlrsCombatLine_WithGermanCensusAndLaterAvailability()
        {
            var german = P(WeaponType.ROC_MLRS_GE);
            var us = P(WeaponType.ROC_MLRS_US);
            Assert.That(german.HardAttack, Is.EqualTo(8));
            Assert.That(us.HardAttack, Is.EqualTo(8), "Extracting the shared definition must preserve the US profile.");
            Assert.That(german.SoftAttack, Is.EqualTo(us.SoftAttack));
            Assert.That(german.HardDefense, Is.EqualTo(us.HardDefense));
            Assert.That(german.SoftDefense, Is.EqualTo(us.SoftDefense));
            Assert.That(german.GroundAirDefense, Is.EqualTo(us.GroundAirDefense));
            Assert.That(german.IndirectRange, Is.EqualTo(us.IndirectRange));
            Assert.That(german.MaxMovementPoints, Is.EqualTo(us.MaxMovementPoints));
            Assert.That(german.ICM, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(german.PrestigeCost, Is.EqualTo(us.PrestigeCost));
            Assert.That(german.TurnAvailable, Is.EqualTo(624));
            Assert.That(german.TurnAvailable, Is.GreaterThan(us.TurnAvailable));
            Assert.That(german.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 950 }, { WeaponType.ROC_MLRS_GE, 18 },
                { WeaponType.IFV_MARDER_GE, 24 }, { WeaponType.MANPAD_STINGER, 12 }, { WeaponType.RCN_LUCHS_GE, 6 }
            }));
            Assert.That(us.IntelReportStats[WeaponType.ROC_MLRS_US], Is.EqualTo(18));
            Assert.That(us.IntelReportStats[WeaponType.APC_M113_US], Is.EqualTo(48));
        }

        [Test]
        public void MarsFormation_GetsTwoSalvosAndCounterBatteryAction_FromExistingRocketRules()
        {
            var unit = CombatUnitDB.CreateUnitFromTemplate("GE_MLRS_REGIMENT", "German rocket test");
            Assert.That(unit, Is.Not.Null);
            Assert.That(P(WeaponType.ROC_MLRS_GE).HasCapability(WeaponCapability.RocketArtillery), Is.True);
            Assert.That(unit.CombatActions.Max, Is.EqualTo(2));
            Assert.That(unit.OpportunityActions.Max, Is.EqualTo(1));
            Assert.That(unit.EquipmentBays.Mobile, Is.EqualTo(WeaponType.NONE));
            Assert.That(unit.EquipmentBays.GetIntelReport().ROC, Is.EqualTo(18));
            Assert.That(WeaponSoundClassifier.FamilyFor(WeaponType.ROC_MLRS_GE), Is.EqualTo(WeaponSoundFamily.RocketArtillery));
        }

        [Test]
        public void M113Option_ReusesTheExistingCarrier_AndDoesNotReplaceTheMarderFormation()
        {
            var alternative = CombatUnitDB.GetUnitTemplate("GE_PANZERGRENADIER_REGIMENT_M113").EquipmentBays;
            var original = CombatUnitDB.GetUnitTemplate("GE_PANZERGRENADIER_REGIMENT").EquipmentBays;
            Assert.That(alternative.Deployed, Is.EqualTo(original.Deployed));
            Assert.That(original.Mobile, Is.EqualTo(WeaponType.IFV_MARDER_GE));
            Assert.That(alternative.GetIntelReport().Personnel, Is.EqualTo(1300));
            Assert.That(alternative.GetIntelReport().TANK, Is.EqualTo(28));
            Assert.That(alternative.GetIntelReport().APC, Is.EqualTo(108), "Retain the previously approved M113 profile census.");
            Assert.That(alternative.GetIntelReport().IFV, Is.Zero);
            Assert.That(P(WeaponType.APC_M113_GE).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.APC_M113_GE, 108 } }));
            Assert.That(P(WeaponType.IFV_MARDER_GE).IntelReportStats[WeaponType.IFV_MARDER_GE], Is.EqualTo(54));
        }

        [Test]
        public void ExistingAirMobileFormation_SelectsNationalArtAcrossAllThreeBays_AndRetainsOrganicLift()
        {
            var unit = CombatUnitDB.GetUnitTemplate("GE_AIRMOBILE_BRIGADE");
            var bays = unit.EquipmentBays;
            Assert.That(unit.ExperienceLevel, Is.EqualTo(ExperienceLevel.Veteran));
            Assert.That(bays.GetIcon(DeploymentPosition.Deployed, HexDirection.W), Is.EqualTo(SpriteManager.GE_AirMobile));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.E), Is.EqualTo(SpriteManager.GE_M113));
            Assert.That(bays.GetIcon(DeploymentPosition.Embarked, HexDirection.NE), Is.EqualTo(SpriteManager.GE_UH1D_Frame0));
            Assert.That(P(WeaponType.HEL_UH1D_GE).IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            Assert.That(P(WeaponType.HEL_UH1D_GE).TransportCategory, Is.EqualTo(TransportCategory.HeloTransport));
            Assert.That(P(WeaponType.HEL_UH1D_GE).HasCapability(WeaponCapability.NonCombatant), Is.True);
            Assert.That(bays.GetIntelReport().Personnel, Is.EqualTo(1900));
            Assert.That(bays.GetIntelReport().HEL, Is.Zero);
            Assert.That(P(WeaponType.HEL_UH1D_GE).IntelReportStats, Is.Empty);
            Assert.That(CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Any(u => u.EquipmentBays.Deployed == WeaponType.HEL_UH1D_GE), Is.False);
        }

        [TestCase(WeaponType.ART_LIGHT_GE, WeaponType.ART_105MM_FG, 950, 48)]
        [TestCase(WeaponType.ART_HEAVY_GE, WeaponType.ART_155MM_FG, 950, 48)]
        [TestCase(WeaponType.AAA_GEN_GE, WeaponType.AAA_GEN_GE, 500, 18)]
        public void TowedSupport_UsesNationalCensus_WithoutEmbeddedCarriersOrFormationMultiplier(WeaponType type,
            WeaponType gun, int personnel, int count)
        {
            Assert.That(P(type).IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, personnel }, { gun, count }, { WeaponType.MANPAD_STINGER, 12 }
            }));
            Assert.That(P(type).ICM, Is.EqualTo(1f));
        }

        [Test]
        public void ExistingHawkBo105AndTornado_KeepTheirEquipmentAndRoles()
        {
            var hawk = CombatUnitDB.GetUnitTemplate("GE_HAWK_REGIMENT").EquipmentBays;
            Assert.That(hawk.Deployed, Is.EqualTo(WeaponType.SAM_HAWK_US));
            Assert.That(hawk.Mobile, Is.EqualTo(WeaponType.TRK_GEN_NATO));
            Assert.That(P(hawk.Deployed).IconProfile.Icon, Is.EqualTo(SpriteManager.US_Hawk));
            Assert.That(CombatUnitDB.GetUnitTemplate("GE_AVIATION_REGIMENT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.HEL_BO105_GE));
            var tornado = CombatUnitDB.GetUnitTemplate("GE_TORNADO_FIGHTER_SQUADRON");
            Assert.That(tornado.Classification, Is.EqualTo(UnitClassification.FGT));
            Assert.That(tornado.EquipmentBays.Deployed, Is.EqualTo(WeaponType.FGT_TORNADO_GE));
            Assert.That(P(WeaponType.FGT_TORNADO_GE).Dogfighting, Is.EqualTo(13));
            Assert.That(P(WeaponType.FGT_TORNADO_GE).OrdinanceLoad, Is.EqualTo(6));
        }

        [Test]
        public void GermanCatalog_UsesNationalEquipment_ExceptTheApprovedSharedHawkAndTruck()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.Nationality == Nationality.FRG).ToList();
            Assert.That(units.Count, Is.EqualTo(19));
            var profiles = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile, u.EquipmentBays.Embarked })
                .Where(t => t != WeaponType.NONE).Distinct();
            Assert.That(profiles.All(t => t == WeaponType.SAM_HAWK_US || t == WeaponType.TRK_GEN_NATO ||
                t.ToString().EndsWith("_GE", StringComparison.Ordinal)), Is.True);
            Assert.That(units.Count(u => u.Classification == UnitClassification.AM), Is.EqualTo(1));
            Assert.That(units.Count(u => u.Classification == UnitClassification.HELO), Is.EqualTo(1));
        }
    }
}
