using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// The derived-capacity rules of the profile-slot rebuild (todo_profiles §4.1–§4.3, ratified
    /// 2026-08-07/08): which bays exist (physics + doctrine, never declared), what may fill them,
    /// the mutation funnel, and the naval transient state.
    /// </summary>
    /// <remarks>
    /// ⚠ The template audits walk the REAL database, exactly like MovementMediumTests — the failure
    /// they guard against is "someone authored a template that contradicts the physics", and a
    /// fixture list would carry the same blind spot as the thing it checks. Under derived capacity
    /// these audits escalate in importance: a wrong medium no longer just mis-sounds a unit, it
    /// opens or closes a purchase bay.
    /// </remarks>
    [TestFixture]
    public class EquipmentBaysTests
    {
        #region Setup

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        private static CombatUnit Make(string name, UnitClassification cls,
            WeaponType deployed, WeaponType mobile = WeaponType.NONE, WeaponType embarked = WeaponType.NONE) =>
            new CombatUnit(name, cls, UnitRole.GroundCombat, Side.Player, Nationality.USSR,
                deployedProfile: deployed, mobileProfile: mobile, embarkedProfile: embarked);

        #endregion // Setup

        #region Template audit — the §4.3 invariants over every authored template

        [Test]
        public void NoTemplate_CarriesMobileContents_WithAClosedMobileBay()
        {
            /* Invariant 5 — the S-300 class of error, machine-checked: a populated Mobile bay on a unit
             * whose fighting kit is NOT foot means the data contradicts itself (the unit IS its
             * vehicles, so what is the "transport" carrying?). */
            var offenders = new List<string>();

            foreach (string templateId in CombatUnitDB.GetAllTemplateIds())
            {
                CombatUnit template = CombatUnitDB.GetUnitTemplate(templateId);
                if (template?.GetMobileProfile() == null) continue;

                if (!template.EquipmentBays.IsMobileBayOpen())
                    offenders.Add($"{templateId}: deployed {template.EquipmentBays.Deployed} " +
                                  $"({template.GetDeployedProfile()?.MovementMedium}) with mobile " +
                                  $"{template.EquipmentBays.Mobile}");
            }

            Assert.That(offenders, Is.Empty,
                "These templates carry ground transport on a fighting kit that is not foot-mobile — " +
                "either the deployed medium is wrong (towed vs self-propelled?) or the Mobile bay " +
                "content is a mistake:\n  " + string.Join("\n  ", offenders));
        }

        [Test]
        public void EveryMobileBayContent_IsAGroundVehicle()
        {
            // Invariant 2: Mobile bay contents are Wheeled/Tracked non-transports. (The transport half
            // is also pinned by MovementMediumTests.NoAirTransport_SitsInAGroundTransportSlot.)
            var offenders = new List<string>();

            foreach (string templateId in CombatUnitDB.GetAllTemplateIds())
            {
                WeaponProfile mobile = CombatUnitDB.GetUnitTemplate(templateId)?.GetMobileProfile();
                if (mobile == null) continue;

                bool groundVehicle = mobile.MovementMedium is MovementMedium.Wheeled or MovementMedium.Tracked;
                if (!groundVehicle || mobile.TransportCategory != TransportCategory.None)
                    offenders.Add($"{templateId}: {mobile.WeaponType} ({mobile.MovementMedium}, {mobile.TransportCategory})");
            }

            Assert.That(offenders, Is.Empty,
                "Mobile bay contents must be Wheeled/Tracked ground vehicles:\n  " + string.Join("\n  ", offenders));
        }

        [Test]
        public void EveryEmbarkedBayContent_IsARealAirTransport()
        {
            /* Invariant 3: the Embarked bay holds AIR LIFT only — helo or fixed-wing transport. Naval
             * lift is a transient state, never a possession (§9.10.6, finally enforced in P1 — the two
             * Naval Infantry templates that authored TRN_NAVAL into this bay violated the ratified doc). */
            var offenders = new List<string>();

            foreach (string templateId in CombatUnitDB.GetAllTemplateIds())
            {
                WeaponProfile embarked = CombatUnitDB.GetUnitTemplate(templateId)?.GetEmbarkedProfile();
                if (embarked == null) continue;

                if (embarked.TransportCategory == TransportCategory.None)
                    offenders.Add($"{templateId}: {embarked.WeaponType}");
            }

            Assert.That(offenders, Is.Empty,
                "Embarked bay contents must declare a real TransportCategory (helo/fixed-wing lift). " +
                "Naval lift is never authored into a bay:\n  " + string.Join("\n  ", offenders));
        }

        #endregion // Template audit

        #region Capacity — the bay exists because of physics, not declaration

        [Test]
        public void MobileBay_OpensForFootKit_AndClosesForVehicleKit()
        {
            var rifles = Make("Rifles", UnitClassification.MOT, WeaponType.INF_REG_SV);
            var tank = Make("Tanks", UnitClassification.TANK, WeaponType.TANK_T55A_SV);

            Assert.That(rifles.EquipmentBays.IsMobileBayOpen(), Is.True, "foot infantry may buy a ride");
            Assert.That(tank.EquipmentBays.IsMobileBayOpen(), Is.False, "a tank IS its vehicles");
        }

        [Test]
        public void S300_MobileBayClosed_S75_MobileBayOpen_ByMediumAlone()
        {
            /* Box 1, with zero rules: the S-300 (Wheeled — it rides its own TELs) closes its bay by
             * physics; the towed S-75 (Foot crews) keeps the normal un-purchased upgrade target. The
             * scenario editor caught the S-300 authored Foot — this pins the correction. */
            var s300 = Make("S-300", UnitClassification.SAM, WeaponType.SAM_S300_SV);
            var s75 = Make("S-75", UnitClassification.SAM, WeaponType.SAM_S75_SV);

            Assert.That(s300.EquipmentBays.IsMobileBayOpen(), Is.False, "self-contained — no ground-transport bay");
            Assert.That(s75.EquipmentBays.IsMobileBayOpen(), Is.True, "towed battery — may buy trucks");
        }

        #endregion // Capacity

        #region Eligibility — identity route + equipment-tag route

        [Test]
        public void HeloLift_InfantryByIdentity_TowedTubesByTag_TanksNever()
        {
            var rifles = Make("Rifles", UnitClassification.MOT, WeaponType.INF_REG_SV);
            var lightGuns = Make("Lt Guns", UnitClassification.ART, WeaponType.ART_LIGHT_SV);
            var s75 = Make("S-75", UnitClassification.SAM, WeaponType.SAM_S75_SV);
            var tank = Make("Tanks", UnitClassification.TANK, WeaponType.TANK_T55A_SV);

            Assert.That(rifles.EquipmentBays.MayCarryHeloLift(rifles.Classification), Is.True,
                "infantry family — identity route");
            Assert.That(lightGuns.EquipmentBays.MayCarryHeloLift(lightGuns.Classification), Is.True,
                "light towed tubes sling-load — HeloTransportable tag route (ratified 2026-08-08)");
            Assert.That(s75.EquipmentBays.MayCarryHeloLift(s75.Classification), Is.False,
                "an untagged towed SAM battery has neither route");
            Assert.That(tank.EquipmentBays.MayCarryHeloLift(tank.Classification), Is.False,
                "tanks never");
        }

        [Test]
        public void FixedWingLift_ParatroopersByIdentity_AirDroppableKitByTag()
        {
            var vdv = Make("VDV", UnitClassification.AB, WeaponType.INF_AB_SV);
            var vdvArt = Make("VDV Art", UnitClassification.ART, WeaponType.ART_LIGHT_SV);
            var vdvSup = Make("VDV Sup", UnitClassification.TANK, WeaponType.RCN_BRDM2AT_SV);
            var rifles = Make("Rifles", UnitClassification.MOT, WeaponType.INF_REG_SV);

            Assert.That(vdv.EquipmentBays.MayCarryFixedWingLift(vdv.Classification), Is.True,
                "AB — identity route");
            Assert.That(vdvArt.EquipmentBays.MayCarryFixedWingLift(vdvArt.Classification), Is.True,
                "airborne artillery — AirDroppable kit route (box 9 Option A). Identity stays ART so it " +
                "still fires §7.13 indirect; eligibility comes from the equipment");
            Assert.That(vdvSup.EquipmentBays.MayCarryFixedWingLift(vdvSup.Classification), Is.True,
                "the VDV support shape (TANK class, BRDM-2AT kit) is expressible for the first time");
            Assert.That(rifles.EquipmentBays.MayCarryFixedWingLift(rifles.Classification), Is.False,
                "plain infantry has no fixed-wing route");
        }

        [Test]
        public void CapabilityTags_AreAuthoredWhereRatified()
        {
            // The two exception tags, pinned at the source so a trait-list edit cannot silently unbook a ruling.
            WeaponProfile lightArt = WeaponProfileDB.GetWeaponProfile(WeaponType.ART_LIGHT_SV);
            WeaponProfile brdm2at = WeaponProfileDB.GetWeaponProfile(WeaponType.RCN_BRDM2AT_SV);
            WeaponProfile bmd2 = WeaponProfileDB.GetWeaponProfile(WeaponType.IFV_BMD2_SV);

            Assert.That(lightArt.HasCapability(WeaponCapability.AirDroppable), Is.True);
            Assert.That(lightArt.HasCapability(WeaponCapability.HeloTransportable), Is.True);
            Assert.That(brdm2at.HasCapability(WeaponCapability.AirDroppable), Is.True);
            Assert.That(bmd2.HasCapability(WeaponCapability.AirDroppable), Is.True, "pre-existing T31 on the BMDs");
        }

        #endregion // Eligibility

        #region CanAccept + the mutation funnel

        [Test]
        public void CanAccept_AdmitsByBayRules_AndRefusesCrossBayNonsense()
        {
            var rifles = Make("Rifles", UnitClassification.MOT, WeaponType.INF_REG_SV);
            var bays = rifles.EquipmentBays;
            var id = rifles.Classification;

            Assert.That(bays.CanAccept(id, EquipmentBay.Mobile, WeaponType.APC_BTR80_SV), Is.True, "an APC is a ride");
            Assert.That(bays.CanAccept(id, EquipmentBay.Mobile, WeaponType.HEL_MI8T_SV), Is.False,
                "a helicopter can NEVER be a ground posture — the Spetsnaz bug, now structurally impossible");
            Assert.That(bays.CanAccept(id, EquipmentBay.Embarked, WeaponType.HEL_MI8T_SV), Is.True, "helo lift for infantry");
            Assert.That(bays.CanAccept(id, EquipmentBay.Embarked, WeaponType.TRN_AN8_SV), Is.False,
                "no fixed-wing route for plain infantry");
            Assert.That(bays.CanAccept(id, EquipmentBay.Embarked, WeaponType.APC_BTR80_SV), Is.False,
                "an APC is not lift");
        }

        [Test]
        public void TrySetSlot_Mutates_RebuildsIntel_AndRefusalsLeaveNoTrace()
        {
            var rifles = Make("Rifles", UnitClassification.MOT, WeaponType.INF_REG_SV);
            var bays = rifles.EquipmentBays;
            int intelBefore = bays.TotalIntelStats.Count;

            bool ok = bays.TrySetSlot(rifles.Classification, EquipmentBay.Mobile, WeaponType.APC_BTR80_SV, out string err);
            Assert.That(ok, Is.True, err);
            Assert.That(bays.Mobile, Is.EqualTo(WeaponType.APC_BTR80_SV));
            Assert.That(bays.TotalIntelStats.Count, Is.GreaterThan(intelBefore),
                "a purchase changes the intel/loss footprint immediately");

            bool refused = bays.TrySetSlot(rifles.Classification, EquipmentBay.Mobile, WeaponType.HEL_MI8T_SV, out string refuseMsg);
            Assert.That(refused, Is.False);
            Assert.That(refuseMsg, Is.Not.Empty, "refusals explain themselves");
            Assert.That(bays.Mobile, Is.EqualTo(WeaponType.APC_BTR80_SV), "a refusal mutates nothing");
        }

        [Test]
        public void TryClearSlot_SellsPurchases_ButNeverTheFightingKit()
        {
            var mam = Make("Air Assault", UnitClassification.MAM,
                WeaponType.INF_AM_SV, WeaponType.APC_MTLB_SV, WeaponType.HEL_MI8T_SV);
            var bays = mam.EquipmentBays;

            Assert.That(bays.TryClearSlot(EquipmentBay.Mobile, out _), Is.True, "selling the carriers is legal");
            Assert.That(bays.Mobile, Is.EqualTo(WeaponType.NONE));

            Assert.That(bays.TryClearSlot(EquipmentBay.Deployed, out string err), Is.False,
                "a regiment always has its fighting kit");
            Assert.That(err, Is.Not.Empty);
            Assert.That(bays.Deployed, Is.EqualTo(WeaponType.INF_AM_SV));
        }

        #endregion // CanAccept + mutation

        #region Naval — a state, never a slot

        [Test]
        public void NavalLift_CanNeverOccupyABay_AndDrawsAsATransientState()
        {
            var marines = Make("Marines", UnitClassification.MMAR, WeaponType.INF_MAR_SV, WeaponType.APC_BTR70_SV);

            Assert.That(marines.EquipmentBays.CanAccept(marines.Classification, EquipmentBay.Embarked, WeaponType.TRN_NAVAL),
                Is.False, "TRN_NAVAL carries TransportCategory.None — refused by construction, not by a special case");

            // The transient state (written by the P2 naval path; exercised directly here at model level):
            marines.SetNavalEmbarked(true);
            marines.SetDeploymentPosition(DeploymentPosition.Embarked);

            Assert.That(marines.GetActiveWeaponProfile()?.WeaponType, Is.EqualTo(WeaponType.TRN_NAVAL),
                "while naval-embarked the SHARED sealift profile is drawn — never owned");
            Assert.That(HammerAndSickle.Services.MovementModeService.CurrentMedium(marines),
                Is.EqualTo(MovementMedium.Naval), "movement/audio/spotting all see 'on water'");

            marines.SetNavalEmbarked(false);
            marines.SetDeploymentPosition(DeploymentPosition.Deployed);
            Assert.That(marines.GetActiveWeaponProfile()?.WeaponType, Is.EqualTo(WeaponType.INF_MAR_SV),
                "debarked — back on its own kit");
        }

        #endregion // Naval

        #region D1 — the dead EmbarkmentState readers, re-pointed and finally live

        [Test]
        public void AirborneSpottingTarget_TracksTheActiveMedium_NotTheDeadState()
        {
            var mam = Make("Air Assault", UnitClassification.MAM,
                WeaponType.INF_AM_SV, WeaponType.NONE, WeaponType.HEL_MI8T_SV);

            Assert.That(mam.IsAirborneSpottingTarget, Is.False, "dismounted rider = ground target");

            mam.SetDeploymentPosition(DeploymentPosition.Embarked);
            Assert.That(mam.IsAirborneSpottingTarget, Is.True,
                "riding helo lift = air target (§12.3 — dead until P1/D1 re-pointed it)");

            var gunship = Make("Gunship", UnitClassification.HELO, WeaponType.HEL_MI24V_SV);
            Assert.That(gunship.IsAirborneSpottingTarget, Is.False,
                "an attack helo flies NoE and is spotted on the GROUND range — the HELO exclusion holds");
        }

        #endregion // D1
    }
}
