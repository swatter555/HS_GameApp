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
    public class ChineseRosterTests : BaseTestFixture
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
            if (!CombatUnitDB.IsInitialized) CombatUnitDB.Initialize();
        }

        private static WeaponProfile P(WeaponType type) => WeaponProfileDB.GetWeaponProfile(type);

        [TestCase(WeaponType.TANK_TYPE62_CH, SpriteManager.CH_Type62, MovementMedium.Tracked, 0.9f)]
        [TestCase(WeaponType.FGT_J6_CH, SpriteManager.CH_J6, MovementMedium.FixedWing, 0.9f)]
        [TestCase(WeaponType.AAA_GEN_CH, SpriteManager.CH_AAA, MovementMedium.Foot, 0.9f)]
        [TestCase(WeaponType.SAM_HQ2_CH, SpriteManager.CH_HQ2, MovementMedium.Foot, 0.9f)]
        [TestCase(WeaponType.APC_TYPE63_CH, SpriteManager.CH_Type63, MovementMedium.Tracked, 1f)]
        [TestCase(WeaponType.TRK_GEN_CH, SpriteManager.CH_Truck, MovementMedium.Wheeled, 1f)]
        public void NewEquipment_HasNationalArtAndMovement_WithFormationQualityOnlyOnDeployedProfiles(
            WeaponType type, string icon, MovementMedium movement, float icm)
        {
            Assert.That(WeaponProfileDB.HasWeaponProfile(type), Is.True);
            var p = P(type);
            Assert.That(p.IconProfile.Icon, Is.EqualTo(icon));
            Assert.That(p.IconProfile.IconType, Is.EqualTo(RegimentIconType.Single));
            Assert.That(p.MovementMedium, Is.EqualTo(movement));
            Assert.That(p.ICM, Is.EqualTo(icm).Within(0.0001f));
        }

        [TestCase("CH_TANK_REGIMENT_TYPE62", UnitClassification.TANK, UnitRole.GroundCombat, WeaponType.TANK_TYPE62_CH, WeaponType.NONE)]
        [TestCase("CH_MOT_INFANTRY_REGIMENT_TYPE63", UnitClassification.MOT, UnitRole.GroundCombat, WeaponType.INF_REG_CH, WeaponType.APC_TYPE63_CH)]
        [TestCase("CH_J6_FIGHTER_SQUADRON", UnitClassification.FGT, UnitRole.AirSuperiority, WeaponType.FGT_J6_CH, WeaponType.NONE)]
        [TestCase("CH_TOWED_AAA_REGIMENT", UnitClassification.AAA, UnitRole.AirDefenseArea, WeaponType.AAA_GEN_CH, WeaponType.TRK_GEN_CH)]
        [TestCase("CH_TYPE83_ARTILLERY_REGIMENT", UnitClassification.SPA, UnitRole.GroundCombatIndirect, WeaponType.SPA_TYPE83_CH, WeaponType.NONE)]
        public void NewRegularFormations_UseLegalBaysAndExistingNationalExperience(string id,
            UnitClassification classification, UnitRole role, WeaponType deployed, WeaponType mobile)
        {
            var u = CombatUnitDB.GetUnitTemplate(id);
            Assert.That(u, Is.Not.Null, id);
            Assert.That(u.Nationality, Is.EqualTo(Nationality.China));
            Assert.That(u.Side, Is.EqualTo(Side.AI));
            Assert.That(u.Classification, Is.EqualTo(classification));
            Assert.That(u.Role, Is.EqualTo(role));
            Assert.That(u.ExperienceLevel, Is.EqualTo(ExperienceLevel.Trained));
            Assert.That(u.EquipmentBays.Deployed, Is.EqualTo(deployed));
            Assert.That(u.EquipmentBays.Mobile, Is.EqualTo(mobile));
            Assert.That(u.EquipmentBays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(u.EquipmentBays.CanAccept(classification, EquipmentBay.Deployed, deployed), Is.True);
            if (mobile != WeaponType.NONE)
                Assert.That(u.EquipmentBays.CanAccept(classification, EquipmentBay.Mobile, mobile), Is.True);
        }

        [TestCase("CH_INFANTRY_REGIMENT")]
        [TestCase("CH_AIRBORNE_REGIMENT")]
        [TestCase("CH_LIGHT_ARTILLERY_REGIMENT")]
        [TestCase("CH_HEAVY_ARTILLERY_REGIMENT")]
        [TestCase("CH_SAM_REGIMENT")]
        public void ExistingTruckBays_UseNationalTruckWithoutChangingTransportRules(string id)
        {
            var bays = CombatUnitDB.GetUnitTemplate(id).EquipmentBays;
            Assert.That(bays.Mobile, Is.EqualTo(WeaponType.TRK_GEN_CH));
            Assert.That(bays.GetIcon(DeploymentPosition.Mobile, HexDirection.NW), Is.EqualTo(SpriteManager.CH_Truck));
            Assert.That(P(bays.Mobile).IntelReportStats, Is.Empty);
            Assert.That(P(bays.Mobile).HasCapability(WeaponCapability.NonCombatant), Is.True);
        }

        [Test]
        public void Hq2_ReplacesTheBorrowedS125_WithNationalS75FamilyAndCensus()
        {
            var u = CombatUnitDB.GetUnitTemplate("CH_SAM_REGIMENT");
            Assert.That(u.EquipmentBays.Deployed, Is.EqualTo(WeaponType.SAM_HQ2_CH));
            Assert.That(u.UnitName, Does.Contain("HQ-2"));
            var hq2 = P(WeaponType.SAM_HQ2_CH);
            var s75 = P(WeaponType.SAM_S75_SV);
            Assert.That(hq2.GroundAirAttack, Is.EqualTo(s75.GroundAirAttack));
            Assert.That(hq2.IndirectRange, Is.EqualTo(s75.IndirectRange));
            Assert.That(hq2.MaxMovementPoints, Is.Zero);
            Assert.That(hq2.PrestigeCost, Is.EqualTo(s75.PrestigeCost));
            Assert.That(hq2.TurnAvailable, Is.EqualTo(348));
            Assert.That(hq2.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 750 }, { WeaponType.SAM_HQ2_CH, 18 }, { WeaponType.MANPAD_STRELA, 21 }
            }));
            Assert.That(s75.ICM, Is.EqualTo(1f), "Chinese formation quality must not leak into the shared definition.");
            Assert.That(P(WeaponType.SAM_S75_IQ).ICM, Is.EqualTo(1f));
        }

        [Test]
        public void InfantryCensus_IncreasesTo2900_WithoutLosingItsOrganicArmorOrInflatingCarrierCensus()
        {
            var inf = P(WeaponType.INF_REG_CH);
            Assert.That(inf.IntelReportStats[WeaponType.Personnel], Is.EqualTo(2900));
            Assert.That(inf.IntelReportStats[WeaponType.TANK_TYPE59_CH], Is.EqualTo(40));
            foreach (var id in new[] { "CH_INFANTRY_REGIMENT", "CH_MECH_INFANTRY_REGIMENT", "CH_MOT_INFANTRY_REGIMENT_TYPE63" })
            {
                var report = CombatUnitDB.GetUnitTemplate(id).EquipmentBays.GetIntelReport();
                Assert.That(report.Personnel, Is.EqualTo(2900), id);
                Assert.That(report.TANK, Is.EqualTo(40), id);
            }
            Assert.That(P(WeaponType.APC_TYPE63_CH).IntelReportStats,
                Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.APC_TYPE63_CH, 90 } }));
            Assert.That(P(WeaponType.IFV_TYPE86_CH).IntelReportStats,
                Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.IFV_TYPE86_CH, 90 } }));
            Assert.That(P(WeaponType.INF_AB_CH).IntelReportStats[WeaponType.Personnel], Is.EqualTo(1800));
        }

        [Test]
        public void Type62_IsALightTank_NotAnAmphibiousTankOrAnAtgmScout()
        {
            var p = P(WeaponType.TANK_TYPE62_CH);
            Assert.That(p.HardAttack, Is.EqualTo(6));
            Assert.That(p.HardDefense, Is.EqualTo(4));
            Assert.That(p.SoftAttack, Is.EqualTo(4));
            Assert.That(p.SoftDefense, Is.EqualTo(7));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(12));
            Assert.That(p.SpottingRange, Is.EqualTo(2));
            Assert.That(p.HasCapability(WeaponCapability.Amphibious), Is.False);
            Assert.That(p.IntelReportStats[WeaponType.TANK_TYPE62_CH], Is.EqualTo(80));
            Assert.That(p.IntelReportStats.ContainsKey(WeaponType.TANK_TYPE59_CH), Is.False);
            Assert.That(P(WeaponType.APC_TYPE63_CH).HasCapability(WeaponCapability.Amphibious), Is.True);
        }

        [Test]
        public void J6_IsAnEarlyGunFighter_WithAircraftOnlyCensus()
        {
            var p = P(WeaponType.FGT_J6_CH);
            Assert.That(p.Dogfighting, Is.EqualTo(7));
            Assert.That(p.TopSpeed, Is.EqualTo(8));
            Assert.That(p.TopSpeed, Is.LessThan(P(WeaponType.FGT_J7_CH).TopSpeed));
            Assert.That(p.GroundAttack, Is.EqualTo(2));
            Assert.That(p.PrestigeCost, Is.EqualTo(P(WeaponType.FGT_J7_CH).PrestigeCost));
            Assert.That(p.TurnAvailable, Is.EqualTo(312));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int> { { WeaponType.FGT_J6_CH, 36 } }));
        }

        [Test]
        public void ChineseDisplayNames_KeepApprovedIdentifiersArtAndUnitTypes()
        {
            Assert.That(P(WeaponType.SPA_TYPE83_CH).ShortName, Is.EqualTo("Type 83"));
            Assert.That(P(WeaponType.SPA_TYPE83_CH).IconProfile.Icon, Is.EqualTo(SpriteManager.CH_Type83));
            Assert.That(P(WeaponType.SPA_TYPE83_CH).SoftAttack, Is.EqualTo(10), "Approved artillery-family SA +1; Type 83 retains its 2S1-equivalent ratings.");
            Assert.That(P(WeaponType.HEL_Z9_CH).ShortName, Is.EqualTo("Z-9"));
            Assert.That(P(WeaponType.HEL_Z9_CH).IconProfile.Icon, Is.EqualTo(SpriteManager.CH_Z9_Frame0));
            Assert.That(P(WeaponType.HEL_Z9_CH).IconProfile.IconType, Is.EqualTo(RegimentIconType.Helo_Animation));
            Assert.That(CombatUnitDB.GetUnitTemplate("CH_AVIATION_REGIMENT").UnitName, Does.Contain("Z-9"));
            Assert.That(CombatUnitDB.GetUnitTemplate("CH_SP_ARTILLERY_REGIMENT").EquipmentBays.Deployed, Is.EqualTo(WeaponType.ROC_PHZ89_CH));
            Assert.That(P(WeaponType.ROC_PHZ89_CH).HasCapability(WeaponCapability.RocketArtillery), Is.True);
            Assert.That(P(WeaponType.SPAAA_TYPE53_CH).ShortName, Is.EqualTo("Type 53"));
            Assert.That(CombatUnitDB.GetUnitTemplate("CH_AIR_DEFENSE_REGIMENT").Classification, Is.EqualTo(UnitClassification.SPAAA));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("TANK_TYPE69_CH"));
        }

        [Test]
        public void Type83_Matches2S1WeaponsAndPrice_WhileKeepingChineseCensusAndQuality()
        {
            var p = P(WeaponType.SPA_TYPE83_CH);
            var soviet = P(WeaponType.SPA_2S1_SV);
            Assert.That(p.HardAttack, Is.EqualTo(soviet.HardAttack));
            Assert.That(p.HardDefense, Is.EqualTo(soviet.HardDefense));
            Assert.That(p.SoftAttack, Is.EqualTo(soviet.SoftAttack));
            Assert.That(p.SoftDefense, Is.EqualTo(soviet.SoftDefense));
            Assert.That(p.GroundAirAttack, Is.EqualTo(soviet.GroundAirAttack));
            Assert.That(p.GroundAirDefense, Is.EqualTo(soviet.GroundAirDefense));
            Assert.That(p.MaxMovementPoints, Is.EqualTo(soviet.MaxMovementPoints));
            Assert.That(p.MovementMedium, Is.EqualTo(soviet.MovementMedium));
            Assert.That(p.SpottingRange, Is.EqualTo(soviet.SpottingRange));
            Assert.That(p.IndirectRange, Is.EqualTo(soviet.IndirectRange));
            Assert.That(p.PrestigeCost, Is.EqualTo(soviet.PrestigeCost));
            Assert.That(p.ICM, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(p.IconProfile.Icon, Is.EqualTo(SpriteManager.CH_Type83));
            Assert.That(p.IntelReportStats, Is.EquivalentTo(new Dictionary<WeaponType, int>
            {
                { WeaponType.Personnel, 700 }, { WeaponType.SPA_TYPE83_CH, 36 },
                { WeaponType.IFV_TYPE86_CH, 12 }, { WeaponType.MANPAD_STRELA, 8 }
            }));
        }

        [TestCase("CH_TANK_REGIMENT_TYPE59_SECOND_LINE", "CH_TANK_REGIMENT_TYPE59", ExperienceLevel.Green)]
        [TestCase("CH_MOT_INFANTRY_REGIMENT_TYPE63_SECOND_LINE", "CH_MOT_INFANTRY_REGIMENT_TYPE63", ExperienceLevel.Raw)]
        [TestCase("CH_J6_FIGHTER_SQUADRON_SECOND_LINE", "CH_J6_FIGHTER_SQUADRON", ExperienceLevel.Green)]
        public void SecondLineVariants_ChangeCrewExperience_WhileReusingRegularEquipmentAndCensus(
            string secondLineId, string regularId, ExperienceLevel approvedExperience)
        {
            var second = CombatUnitDB.CreateUnitFromTemplate(secondLineId, "Second-line roster test");
            var regular = CombatUnitDB.GetUnitTemplate(regularId);
            Assert.That(second.ExperienceLevel, Is.EqualTo(approvedExperience));
            Assert.That(regular.ExperienceLevel, Is.EqualTo(ExperienceLevel.Trained));
            Assert.That(second.Nationality, Is.EqualTo(Nationality.China));
            Assert.That(second.Classification, Is.EqualTo(regular.Classification));
            Assert.That(second.Role, Is.EqualTo(regular.Role));
            Assert.That(second.EquipmentBays.Deployed, Is.EqualTo(regular.EquipmentBays.Deployed));
            Assert.That(second.EquipmentBays.Mobile, Is.EqualTo(regular.EquipmentBays.Mobile));
            Assert.That(second.EquipmentBays.Embarked, Is.EqualTo(WeaponType.NONE));
            Assert.That(P(second.EquipmentBays.Deployed).ICM, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(second.EquipmentBays.GetIntelReport().Personnel,
                Is.EqualTo(regular.EquipmentBays.GetIntelReport().Personnel));
            Assert.That(second.EquipmentBays.GetIntelReport().TANK,
                Is.EqualTo(regular.EquipmentBays.GetIntelReport().TANK));
            if (second.EquipmentBays.Mobile != WeaponType.NONE)
                Assert.That(P(second.EquipmentBays.Mobile).ICM, Is.EqualTo(1f));
        }

        [Test]
        public void Type88_DisplayCorrection_PreservesType80KeysArtworkCensusAndRatings()
        {
            var p = P(WeaponType.TANK_TYPE80_CH);
            Assert.That(p.LongName, Is.EqualTo("Type 88 Main Battle Tank"));
            Assert.That(p.ShortName, Is.EqualTo("Type 88"));
            Assert.That(p.IconProfile.Icon, Is.EqualTo(SpriteManager.CH_Type80));
            Assert.That(p.IntelReportStats[WeaponType.TANK_TYPE80_CH], Is.EqualTo(80));
            Assert.That(p.HardAttack, Is.EqualTo(10));
            Assert.That(p.ICM, Is.EqualTo(1.05f * 0.9f).Within(0.0001f));
            Assert.That(p.TurnAvailable, Is.EqualTo(CampaignDateCalendar.DateToTurn(011986)));
            Assert.That(new CampaignDateCalendar(121985, 011986).IsWeaponSystemAvailable(p), Is.False);
            Assert.That(new CampaignDateCalendar(011986, 011986).IsWeaponSystemAvailable(p), Is.True);
            Assert.That(p.PrestigeCost, Is.EqualTo(65));
            var u = CombatUnitDB.GetUnitTemplate("CH_TANK_REGIMENT_TYPE80");
            Assert.That(u.UnitName, Is.EqualTo("Chinese Tank Regiment (Type 88)"));
            Assert.That(u.EquipmentBays.Deployed, Is.EqualTo(WeaponType.TANK_TYPE80_CH));
            Assert.That(Enum.GetNames(typeof(WeaponType)), Does.Not.Contain("TANK_TYPE88_CH"));
        }

        [Test]
        public void ChineseCatalog_UsesAllTwentyTwoNationalProfilesAndTwentySevenPictures()
        {
            var units = CombatUnitDB.GetAllTemplateIds().Select(CombatUnitDB.GetUnitTemplate)
                .Where(u => u.Nationality == Nationality.China).ToArray();
            Assert.That(units.Length, Is.EqualTo(24));
            var types = units.SelectMany(u => new[] { u.EquipmentBays.Deployed, u.EquipmentBays.Mobile, u.EquipmentBays.Embarked })
                .Where(t => t != WeaponType.NONE).Distinct().ToArray();
            Assert.That(types.Length, Is.EqualTo(22));
            Assert.That(types.All(t => t.ToString().EndsWith("_CH", StringComparison.Ordinal)), Is.True);
            var pictures = types.SelectMany(t => P(t).IconProfile.IconType == RegimentIconType.Helo_Animation
                ? Enumerable.Range(0, 6).Select(frame => P(t).IconProfile.Icon.Replace("_Frame0", "_Frame" + frame))
                : new[] { P(t).IconProfile.Icon }).Distinct().ToArray();
            Assert.That(pictures.Length, Is.EqualTo(27));
            Assert.That(pictures.All(picture => picture.StartsWith("CH_", StringComparison.Ordinal)), Is.True);
        }
    }
}
