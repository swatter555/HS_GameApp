using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Guards the COMMODITY BLUEPRINTS — the four stat lines in <c>WeaponProfileDB</c> that many national
    /// profiles share: light towed artillery, heavy towed artillery, towed AAA, and the transport truck.
    ///
    /// ⚠ WHY THIS FIXTURE EXISTS. Bob's rule gives every nation its own ARTWORK for these, and in this
    /// codebase art lives on the profile — so unique art means a profile per nation, and eleven nations
    /// means eleven profiles of the same gun. Before the RE-3 refactor three of them were already
    /// byte-identical copies whose own comments admitted it. Copies drift silently: someone retunes the
    /// NATO howitzer for a Hamburg playtest, the Iraqi and Chinese ones stay put, and nothing anywhere
    /// says the same weapon now has three different stat lines.
    ///
    /// The blueprint methods make that structurally impossible. THIS fixture is what stops the blueprint
    /// being quietly bypassed later by a hand-authored ProfileDef that "just needed one small change".
    ///
    /// ⚠ NATIONALITY MAY CHANGE THREE THINGS AND NO OTHERS: the sprite, the census, and the availability
    /// turn. It may never change the ballistics — a towed howitzer is the same gun for anyone.
    ///
    /// ⚠ THE MUJAHIDEEN ARE DELIBERATELY EXCLUDED. <c>ART_LIGHT_MJ</c> and <c>AAA_GEN_MJ</c> are authored
    /// IRREGULAR variants — captured, worn, badly served kit (SA-3/GAD+2/IR MINIMUM and GAT-2/GAD-2
    /// respectively). They are not commodity equipment and folding them in here would erase the very
    /// thing that makes them Mujahideen.
    ///
    /// On a failure: FIX THE PROFILE, NOT THE GUARD. A failure means a national copy stopped matching its
    /// blueprint, which is exactly the drift this exists to catch.
    /// </summary>
    [TestFixture]
    public class CommodityProfileTests : BaseTestFixture
    {
        #region Setup

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        private static WeaponProfile P(WeaponType t) => WeaponProfileDB.GetWeaponProfile(t);

        /// <summary>
        /// The six ground-combat stats plus the two ranges that a blueprint fixes.
        /// ⚠ ICM IS DELIBERATELY NOT IN HERE. A blueprint fixes the BALLISTICS; the formation-quality
        /// ICM is national by definition, and since RE-4 the Chinese guns carry SECOND_LINE_FORMATION
        /// (×0.9) while firing the identical shell. Adding ICM to this string would make the two
        /// concepts collide and the family tests fail for a correct reason.
        /// </summary>
        private static string StatLine(WeaponType t)
        {
            var p = P(t);
            return $"HA{(int)p.HardAttack} HD{(int)p.HardDefense} " +
                   $"SA{(int)p.SoftAttack} SD{(int)p.SoftDefense} " +
                   $"GAT{(int)p.GroundAirAttack} GAD{(int)p.GroundAirDefense} " +
                   $"IR{(int)p.IndirectRange} MMP{(int)p.MaxMovementPoints}";
        }

        /// <summary>
        /// Asserts every registered member of a family resolves to one identical stat line. Members that
        /// are not registered are skipped, so this stays green as RE-3b mints the remaining nations.
        /// </summary>
        private static void AssertOneStatLine(string family, params WeaponType[] members)
        {
            var present = members.Where(WeaponProfileDB.HasWeaponProfile).ToList();

            Assert.That(present, Is.Not.Empty, $"{family}: no member of this family is registered at all.");

            var byLine = present
                .GroupBy(StatLine)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (byLine.Count == 1) return;

            var detail = string.Join("\n  ", byLine.Select(g =>
                $"{g.Key}   <-- {string.Join(", ", g)}"));

            Assert.Fail(
                $"{family}: {present.Count} registered profiles resolved to {byLine.Count} DIFFERENT stat " +
                $"lines. A national copy has drifted off the blueprint — fix the profile, not this test.\n  " +
                detail);
        }

        #endregion // Setup

        #region Blueprint Conformance

        [Test]
        public void LightTowedArtillery_IsOneGunForEveryNation()
        {
            AssertOneStatLine("Light towed artillery",
                WeaponType.ART_LIGHT_SV, WeaponType.ART_LIGHT_NATO,
                WeaponType.ART_LIGHT_ARAB, WeaponType.ART_LIGHT_CH);
        }

        [Test]
        public void HeavyTowedArtillery_IsOneGunForEveryNation()
        {
            AssertOneStatLine("Heavy towed artillery",
                WeaponType.ART_HEAVY_SV, WeaponType.ART_HEAVY_NATO,
                WeaponType.ART_HEAVY_ARAB, WeaponType.ART_HEAVY_CH);
        }

        [Test]
        public void TransportTrucks_AreOneVehicleForEveryNation()
        {
            AssertOneStatLine("Transport truck",
                WeaponType.TRK_GEN_SV, WeaponType.TRK_GEN_NATO, WeaponType.TRK_GEN_ARAB);
        }

        /// <summary>
        /// The light line is air-droppable and helo-liftable; the heavy line is neither. That split is
        /// the whole reason two blueprints exist rather than one with a delta, so it is pinned here —
        /// and it is load-bearing: <c>EquipmentBays.CanAccept</c> routes airborne artillery into a
        /// fixed-wing lift by exactly these two capability tags.
        /// </summary>
        [Test]
        public void OnlyTheLightGun_Flies()
        {
            var wrong = new List<string>();

            foreach (var t in new[] { WeaponType.ART_LIGHT_SV, WeaponType.ART_LIGHT_NATO,
                                      WeaponType.ART_LIGHT_ARAB, WeaponType.ART_LIGHT_CH })
            {
                if (!WeaponProfileDB.HasWeaponProfile(t)) continue;
                var p = P(t);
                if (!p.HasCapability(WeaponCapability.AirDroppable)) wrong.Add($"{t} lost AirDroppable");
                if (!p.HasCapability(WeaponCapability.HeloTransportable)) wrong.Add($"{t} lost HeloTransportable");
            }

            foreach (var t in new[] { WeaponType.ART_HEAVY_SV, WeaponType.ART_HEAVY_NATO,
                                      WeaponType.ART_HEAVY_ARAB, WeaponType.ART_HEAVY_CH })
            {
                if (!WeaponProfileDB.HasWeaponProfile(t)) continue;
                var p = P(t);
                if (p.HasCapability(WeaponCapability.AirDroppable)) wrong.Add($"{t} gained AirDroppable");
                if (p.HasCapability(WeaponCapability.HeloTransportable)) wrong.Add($"{t} gained HeloTransportable");
            }

            Assert.That(wrong, Is.Empty,
                "The light/heavy lift split has broken — light guns fly, heavy guns do not:\n  " +
                string.Join("\n  ", wrong));
        }

        #endregion // Blueprint Conformance

        #region Irregular Variants Stay Irregular

        /// <summary>
        /// The Mujahideen kit must NOT match the commodity line. If someone "tidies" it onto a blueprint
        /// this fails — which is the point: their artillery is deliberately worse than everyone else's.
        /// </summary>
        [Test]
        public void MujahideenArtillery_IsNotTheCommodityGun()
        {
            if (!WeaponProfileDB.HasWeaponProfile(WeaponType.ART_LIGHT_MJ) ||
                !WeaponProfileDB.HasWeaponProfile(WeaponType.ART_LIGHT_SV))
                Assert.Ignore("Both profiles must exist for this comparison.");

            Assert.That(StatLine(WeaponType.ART_LIGHT_MJ),
                Is.Not.EqualTo(StatLine(WeaponType.ART_LIGHT_SV)),
                "Mujahideen light artillery has been folded onto the commodity blueprint. It is an " +
                "IRREGULAR variant — captured, worn, badly served — and that difference is authored on " +
                "purpose (SA-3, GAD+2, IR MINIMUM).");
        }

        [Test]
        public void MujahideenAaa_IsNotTheCommodityGun()
        {
            if (!WeaponProfileDB.HasWeaponProfile(WeaponType.AAA_GEN_MJ) ||
                !WeaponProfileDB.HasWeaponProfile(WeaponType.AAA_GEN_SV))
                Assert.Ignore("Both profiles must exist for this comparison.");

            Assert.That(StatLine(WeaponType.AAA_GEN_MJ),
                Is.Not.EqualTo(StatLine(WeaponType.AAA_GEN_SV)),
                "Mujahideen AAA has been folded onto the commodity blueprint. Its GAT-2/GAD-2 penalty is " +
                "authored on purpose.");
        }

        #endregion // Irregular Variants Stay Irregular

        #region National Traits Ride On Top (RE-4)

        /// <summary>
        /// The blueprints take per-nation traits (<c>LightTowedArtilleryDef(SECOND_LINE_FORMATION)</c>),
        /// and this pins what that is allowed to change: the ICM moves, the gun does not. It is the guard
        /// on the whole commodity design — if a future national trait carries a stat delta, the family
        /// test above fails and this one explains why.
        /// </summary>
        [Test]
        public void ChineseTowedGuns_ShareTheLineButNotTheIcm()
        {
            if (!WeaponProfileDB.HasWeaponProfile(WeaponType.ART_LIGHT_CH) ||
                !WeaponProfileDB.HasWeaponProfile(WeaponType.ART_LIGHT_SV))
                Assert.Ignore("Both profiles must exist for this comparison.");

            Assert.That(StatLine(WeaponType.ART_LIGHT_CH),
                Is.EqualTo(StatLine(WeaponType.ART_LIGHT_SV)),
                "A towed howitzer is the same gun for anyone — SECOND_LINE_FORMATION must not touch " +
                "ballistics.");

            Assert.AreEqual(0.90f, P(WeaponType.ART_LIGHT_CH).ICM, 0.01f,
                "Chinese light gun carries the second-line formation factor.");
            Assert.AreEqual(1.00f, P(WeaponType.ART_LIGHT_SV).ICM, 0.01f,
                "Soviet light gun does not — the trait argument must not leak into the shared blueprint.");
        }

        #endregion // National Traits Ride On Top
    }
}
