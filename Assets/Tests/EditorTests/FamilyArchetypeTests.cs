using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using NUnit.Framework;
using System;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Phase 2 validation: every <see cref="FamilyArchetypes"/> family resolves (bare, no traits)
    /// to its ratified Appendix W / §7B statline, plus mechanics guards — the GAT/W7 default, the
    /// MANPADS GAT-floor wiring, and the resolver's band-vs-non-band clamp (MMP must survive at 0
    /// for facilities and 100 for fixed-wing, not be clamped to [1, 25]).
    /// </summary>
    [TestFixture]
    public class FamilyArchetypeTests : BaseTestFixture
    {
        private const string CLASS_NAME = nameof(FamilyArchetypeTests);
        private const float ICM_TOL = 0.001f;

        #region Helpers

        /// <summary>Resolves an archetype with optional traits and no one-off deltas.</summary>
        private static TraitResolver.Result Resolve(Archetype a, params WeaponTrait[] traits)
            => TraitResolver.Resolve(a, null, traits);

        /// <summary>Asserts a full ground line: HA/HD/SA/SD/GAD/MMP (+ GAT, default 0).</summary>
        private static void AssertGround(TraitResolver.Result r,
            int ha, int hd, int sa, int sd, int gad, int mmp, int gat = 0)
        {
            Assert.AreEqual(ha, r.Stat(ProfileStat.HA), "HA");
            Assert.AreEqual(hd, r.Stat(ProfileStat.HD), "HD");
            Assert.AreEqual(sa, r.Stat(ProfileStat.SA), "SA");
            Assert.AreEqual(sd, r.Stat(ProfileStat.SD), "SD");
            Assert.AreEqual(gad, r.Stat(ProfileStat.GAD), "GAD");
            Assert.AreEqual(gat, r.Stat(ProfileStat.GAT), "GAT");
            Assert.AreEqual(mmp, r.Stat(ProfileStat.MMP), "MMP");
            Assert.AreEqual(GameData.ICM_DEFAULT, r.ICM, ICM_TOL, "ICM (bare = default)");
        }

        /// <summary>Asserts a full air line: DF/MAN/TS/SUR/MMP.</summary>
        private static void AssertAir(TraitResolver.Result r,
            int df, int man, int ts, int sur, int mmp)
        {
            Assert.AreEqual(df, r.Stat(ProfileStat.DF), "DF");
            Assert.AreEqual(man, r.Stat(ProfileStat.MAN), "MAN");
            Assert.AreEqual(ts, r.Stat(ProfileStat.TS), "TS");
            Assert.AreEqual(sur, r.Stat(ProfileStat.SUR), "SUR");
            Assert.AreEqual(mmp, r.Stat(ProfileStat.MMP), "MMP");
            Assert.AreEqual(GameData.ICM_DEFAULT, r.ICM, ICM_TOL, "ICM (bare = default)");
        }

        #endregion // Helpers

        #region Ground archetype lines

        [Test]
        public void Infantry_ResolvesRatifiedLine()
        {
            try { AssertGround(Resolve(FamilyArchetypes.Infantry), 5, 7, 7, 8, 10, 4); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Apc_ResolvesRatifiedLine()
        {
            try { AssertGround(Resolve(FamilyArchetypes.Apc), 3, 4, 6, 7, 7, 8); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Apc_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Ifv_ResolvesRatifiedLine()
        {
            // SA 8 (R4), GAD 7 (R1).
            try { AssertGround(Resolve(FamilyArchetypes.Ifv), 4, 4, 8, 7, 7, 10); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Ifv_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Artillery_ResolvesRatifiedLine()
        {
            // Towed = foot (MMP 4).
            try { AssertGround(Resolve(FamilyArchetypes.Artillery), 5, 5, 9, 5, 8, 4); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Artillery_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Aaa_ResolvesRatifiedLine()
        {
            // GAT 11 (2026-06-18 GAT rebalance, 7/10 lethality), GAD 12, towed = foot (MMP 4).
            try { AssertGround(Resolve(FamilyArchetypes.Aaa), 4, 4, 9, 6, 12, 4, gat: 11); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Aaa_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Sam_ResolvesRatifiedLine()
        {
            // Air-only (HA/SA 1, §7A.13), GAT 12 (2026-06-18 GAT rebalance), towed = foot (MMP 4).
            try { AssertGround(Resolve(FamilyArchetypes.Sam), 1, 3, 1, 3, 8, 4, gat: 12); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Sam_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Helicopter_ResolvesRatifiedLine()
        {
            try { AssertGround(Resolve(FamilyArchetypes.Helicopter), 7, 6, 10, 7, 10, 24); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Helicopter_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Truck_ResolvesRatifiedLine()
        {
            try { AssertGround(Resolve(FamilyArchetypes.Truck), 3, 3, 3, 3, 6, 8); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Truck_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Facility_ResolvesRatifiedLine()
        {
            // MMP 0 must survive the resolver (static, not clamped to 1).
            try { AssertGround(Resolve(FamilyArchetypes.Facility), 4, 6, 6, 7, 6, 0); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Facility_ResolvesRatifiedLine), ex); throw; }
        }

        #endregion // Ground archetype lines

        #region Air archetype lines

        [Test]
        public void FighterEarly_ResolvesRatifiedLine()
        {
            try { AssertAir(Resolve(FamilyArchetypes.FighterEarly), 8, 9, 10, 6, 100); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(FighterEarly_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void FighterMid_ResolvesRatifiedLine()
        {
            try { AssertAir(Resolve(FamilyArchetypes.FighterMid), 10, 11, 10, 7, 100); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(FighterMid_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void FighterLate_ResolvesRatifiedLine()
        {
            try { AssertAir(Resolve(FamilyArchetypes.FighterLate), 12, 12, 10, 9, 100); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(FighterLate_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Attack_ResolvesRatifiedLine()
        {
            try { AssertAir(Resolve(FamilyArchetypes.Attack), 4, 4, 7, 10, 100); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Attack_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Bomber_ResolvesRatifiedLine()
        {
            try { AssertAir(Resolve(FamilyArchetypes.Bomber), 1, 3, 10, 8, 100); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Bomber_ResolvesRatifiedLine), ex); throw; }
        }

        [Test]
        public void Air_GA_OL_Baselines_PerRuleA()
        {
            // Rule A (2026-06-16): GA + OL are now air-archetype baselines, not per-profile tier deltas.
            // Fighters carry the dual-role GA floor 2 / OL 6; Attack 10 / 9; Bomber 8 / 12. GA is a band
            // stat (clamp 1..25); OL floors at 0.
            try
            {
                foreach (Archetype f in new[] { FamilyArchetypes.FighterEarly, FamilyArchetypes.FighterMid, FamilyArchetypes.FighterLate })
                {
                    TraitResolver.Result r = Resolve(f);
                    Assert.AreEqual(2, r.Stat(ProfileStat.GA), "fighter GA floor");
                    Assert.AreEqual(6, r.Stat(ProfileStat.OL), "fighter OL");
                }

                TraitResolver.Result att = Resolve(FamilyArchetypes.Attack);
                Assert.AreEqual(10, att.Stat(ProfileStat.GA), "Attack GA");
                Assert.AreEqual(9,  att.Stat(ProfileStat.OL), "Attack OL");

                TraitResolver.Result bmb = Resolve(FamilyArchetypes.Bomber);
                Assert.AreEqual(8,  bmb.Stat(ProfileStat.GA), "Bomber GA");
                Assert.AreEqual(12, bmb.Stat(ProfileStat.OL), "Bomber OL");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Air_GA_OL_Baselines_PerRuleA), ex); throw; }
        }

        #endregion // Air archetype lines

        #region Mechanics guards

        [Test]
        public void FixedWing_MMP_NotClampedTo25()
        {
            // The §1 [1,25] clamp is band-stats only; FIXEDWING MMP 100 must pass through intact.
            try { Assert.AreEqual(100, Resolve(FamilyArchetypes.Bomber).Stat(ProfileStat.MMP)); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(FixedWing_MMP_NotClampedTo25), ex); throw; }
        }

        [Test]
        public void Facility_MMP_StaysZero()
        {
            // Static MMP 0 must NOT be floored up to the band minimum of 1.
            try { Assert.AreEqual(0, Resolve(FamilyArchetypes.Facility).Stat(ProfileStat.MMP)); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Facility_MMP_StaysZero), ex); throw; }
        }

        [Test]
        public void Infantry_BaselineGAT_IsZero()
        {
            // W7: no air-attack baseline; the stat is absent until a MANPADS trait grants it.
            try { Assert.AreEqual(0, Resolve(FamilyArchetypes.Infantry).Stat(ProfileStat.GAT)); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_BaselineGAT_IsZero), ex); throw; }
        }

        [Test]
        public void Infantry_WithBasicManpads_FloorsGatTo6()
        {
            try { Assert.AreEqual(6, Resolve(FamilyArchetypes.Infantry, WeaponTrait.MANPADS_BASIC).Stat(ProfileStat.GAT)); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_WithBasicManpads_FloorsGatTo6), ex); throw; }
        }

        [Test]
        public void Infantry_WithStinger_FloorsGatTo8_AndAddsIcm()
        {
            try
            {
                TraitResolver.Result r = Resolve(FamilyArchetypes.Infantry, WeaponTrait.MANPADS_STINGER);
                Assert.AreEqual(8, r.Stat(ProfileStat.GAT), "Stinger GAT floor");
                Assert.AreEqual(1.05f, r.ICM, ICM_TOL, "Stinger ICM ×1.05");
            }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Infantry_WithStinger_FloorsGatTo8_AndAddsIcm), ex); throw; }
        }

        [Test]
        public void Manpads_DoesNotTouchGad()
        {
            // The settled mechanic: MANPADS = GAT (return fire), NOT GAD; infantry GAD stays 10 (R1).
            try { Assert.AreEqual(10, Resolve(FamilyArchetypes.Infantry, WeaponTrait.MANPADS_BASIC).Stat(ProfileStat.GAD)); }
            catch (Exception ex) { AppService.HandleException(CLASS_NAME, nameof(Manpads_DoesNotTouchGad), ex); throw; }
        }

        #endregion // Mechanics guards
    }
}
