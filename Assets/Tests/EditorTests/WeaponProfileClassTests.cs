using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// W1/W2 validation against the live WeaponProfileDB: the Hard/Soft target-class default-by-prefix
    /// (§7.4.1.2), the per-profile armored-car recon overrides, and the TransportCategory flags (§10.3.13).
    /// </summary>
    [TestFixture]
    public class WeaponProfileClassTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(WeaponProfileClassTests);

        #region Setup

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            if (!WeaponProfileDB.IsInitialized) WeaponProfileDB.Initialize();
        }

        #endregion // Setup

        #region Helpers

        private static TargetClass ClassOf(WeaponType wt) => WeaponProfileDB.GetWeaponProfile(wt).TargetClass;
        private static TransportCategory TransportOf(WeaponType wt) => WeaponProfileDB.GetWeaponProfile(wt).TransportCategory;

        /// <summary>A bare, all-zero-stat profile — exercises the constructor's prefix default with no DB.</summary>
        private static WeaponProfile Bare(WeaponType wt)
            => new WeaponProfile("t", "t", wt, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, false);

        #endregion // Helpers

        #region W1 — default logic (DB-free, constructor only)

        [Test]
        public void W1_PrefixDefault_FromConstructor()
        {
            try
            {
                Assert.AreEqual(TargetClass.Hard, Bare(WeaponType.TANK_T55A_SV).TargetClass, "TANK");
                Assert.AreEqual(TargetClass.Hard, Bare(WeaponType.HEL_MI24V_SV).TargetClass, "HEL");
                Assert.AreEqual(TargetClass.Hard, Bare(WeaponType.SPAAA_ZSU57_SV).TargetClass, "SPAAA");
                Assert.AreEqual(TargetClass.Soft, Bare(WeaponType.SPA_2S1_SV).TargetClass, "SPA");
                Assert.AreEqual(TargetClass.Soft, Bare(WeaponType.INF_AB_SV).TargetClass, "INF");
                Assert.AreEqual(TargetClass.Soft, Bare(WeaponType.FGT_MIG29_SV).TargetClass, "FGT");
                Assert.AreEqual(TransportCategory.None, Bare(WeaponType.TANK_T55A_SV).TransportCategory, "TransportCategory defaults to None");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W1_PrefixDefault_FromConstructor), ex); throw; }
        }

        #endregion // W1 — default logic (DB-free, constructor only)

        #region W1 — Hard/Soft default by prefix

        [Test]
        public void W1_HardPrefixes_DefaultHard()
        {
            try
            {
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.TANK_T55A_SV), "TANK");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.IFV_BMP1_SV), "IFV");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.APC_BTR70_SV), "APC");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.HEL_MI24V_SV), "HEL (gunship)");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.SPAAA_ZSU57_SV), "SPAAA");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.SPSAM_2K12_SV), "SPSAM");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W1_HardPrefixes_DefaultHard), ex); throw; }
        }

        [Test]
        public void W1_SoftPrefixes_DefaultSoft()
        {
            try
            {
                Assert.AreEqual(TargetClass.Soft, ClassOf(WeaponType.INF_AB_SV), "INF");
                Assert.AreEqual(TargetClass.Soft, ClassOf(WeaponType.SAM_S75_SV), "SAM");
                Assert.AreEqual(TargetClass.Soft, ClassOf(WeaponType.RCN_BRDM2_SV), "RCN (plain scout)");
                Assert.AreEqual(TargetClass.Soft, ClassOf(WeaponType.FGT_MIG29_SV), "FGT (inert for aircraft)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W1_SoftPrefixes_DefaultSoft), ex); throw; }
        }

        [Test]
        public void W1_Spa_IsSoft_NotConfusedWithSpaaaSpsam()
        {
            // The prefix split must treat SPA / SPAAA / SPSAM as distinct first tokens.
            try
            {
                Assert.AreEqual(TargetClass.Soft, ClassOf(WeaponType.SPA_2S1_SV), "SPA stays Soft");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.SPAAA_ZSU57_SV), "SPAAA Hard");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.SPSAM_2K12_SV), "SPSAM Hard");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W1_Spa_IsSoft_NotConfusedWithSpaaaSpsam), ex); throw; }
        }

        #endregion // W1 — Hard/Soft default by prefix

        #region W1 — armored-car recon overrides

        [Test]
        public void W1_ArmoredCarRecon_OverridesToHard()
        {
            try
            {
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.RCN_BRDM2AT_SV), "BRDM-2 AT");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.RCN_M3_US), "M3 Bradley");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.RCN_LUCHS_GE), "Luchs");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.RCN_ERC90_FR), "ERC-90");
                Assert.AreEqual(TargetClass.Hard, ClassOf(WeaponType.RCN_FV105_UK), "FV105 (UK)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W1_ArmoredCarRecon_OverridesToHard), ex); throw; }
        }

        #endregion // W1 — armored-car recon overrides

        #region W2 — TransportCategory

        [Test]
        public void W2_Transports_AreFlagged()
        {
            try
            {
                Assert.AreEqual(TransportCategory.HeloTransport, TransportOf(WeaponType.HEL_MI8T_SV), "Mi-8T");
                Assert.AreEqual(TransportCategory.HeloTransport, TransportOf(WeaponType.HEL_UH60_US), "UH-60");
                Assert.AreEqual(TransportCategory.FixedWingTransport, TransportOf(WeaponType.TRN_AN8_SV), "An-12");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W2_Transports_AreFlagged), ex); throw; }
        }

        [Test]
        public void W2_NonTransports_AreNone()
        {
            try
            {
                Assert.AreEqual(TransportCategory.None, TransportOf(WeaponType.TANK_T55A_SV), "tank");
                Assert.AreEqual(TransportCategory.None, TransportOf(WeaponType.HEL_MI24V_SV), "gunship helo (HEL_ but not a transport)");
                Assert.AreEqual(TransportCategory.None, TransportOf(WeaponType.TRN_NAVAL), "naval sealift (outside the helo/fixed-wing enum)");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(W2_NonTransports_AreNone), ex); throw; }
        }

        #endregion // W2 — TransportCategory
    }
}
