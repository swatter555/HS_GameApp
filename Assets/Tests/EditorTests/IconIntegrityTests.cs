using HammerAndSickle.Core.GameData;
using HammerAndSickle.Core.Map;
using HammerAndSickle.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// Guards UNIT ICON RESOLUTION after the top-down pass (2026-08-29): one sprite per profile, rotated
    /// to facing, with the per-direction and firing variants deleted outright.
    ///
    /// ⚠ WHY THIS FIXTURE EXISTS. Before it, NOTHING under Assets/Tests referenced <c>IconProfile</c>,
    /// <c>GetSpriteNameForUnit</c> or <c>RegimentIconType</c> — zero hits. The pass rewrote all 184 icon
    /// declarations mechanically, and the failure mode of a botched bulk edit is a single profile silently
    /// losing its art: the unit renders the mismatch placeholder, nothing throws, nothing logs, and it is
    /// visible only by finding that one unit in play. That is precisely the defect a suite can catch and
    /// a human cannot.
    ///
    /// ⚠ THE HELO CONTRACT IS THE SUBTLE ONE. <c>RegimentIconProfile</c> holds no frame array — the
    /// flipbook in <c>Prefab_CombatUnitIcon</c> derives frames 1-5 by swapping
    /// <see cref="GameData.ICON_MOTION_FRAME0_SUFFIX"/> onto the icon's NAME. So a Helo_Animation profile
    /// whose icon does not end in "_Frame0" animates nothing at all, and rests on a sprite that may not
    /// even exist. The name IS the contract, which is why it is asserted here rather than trusted.
    ///
    /// On a failure: FIX THE DATA, NOT THE GUARD. A failure names a profile the conversion missed.
    /// </summary>
    [TestFixture]
    public class IconIntegrityTests : BaseTestFixture
    {
        #region Setup

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            if (!WeaponProfileDB.IsInitialized)
                WeaponProfileDB.Initialize();
        }

        /// <summary>Every WeaponType that actually resolves to a registered profile.</summary>
        private static IEnumerable<WeaponType> RegisteredProfileTypes() =>
            Enum.GetValues(typeof(WeaponType))
                .Cast<WeaponType>()
                .Where(t => t != WeaponType.NONE)
                .Where(WeaponProfileDB.HasWeaponProfile);

        #endregion // Setup

        #region Icon Profile Integrity

        [Test]
        public void EveryRegisteredProfile_HasAnIconProfile()
        {
            var missing = RegisteredProfileTypes()
                .Where(t => WeaponProfileDB.GetWeaponProfile(t)?.IconProfile == null)
                .ToList();

            Assert.That(missing, Is.Empty,
                "These profiles have no IconProfile at all, so every unit holding one renders the " +
                "mismatch placeholder:\n  " + string.Join("\n  ", missing));
        }

        [Test]
        public void EveryIconProfile_PassesItsOwnValidation()
        {
            var failures = new List<string>();

            foreach (var type in RegisteredProfileTypes())
            {
                var icon = WeaponProfileDB.GetWeaponProfile(type)?.IconProfile;
                if (icon == null) continue;

                if (!icon.Validate(out string error))
                    failures.Add($"{type}: {error}");
            }

            Assert.That(failures, Is.Empty,
                "IconProfile.Validate rejected these:\n  " + string.Join("\n  ", failures));
        }

        [Test]
        public void EveryIconProfile_HasANonEmptyIcon()
        {
            var blank = RegisteredProfileTypes()
                .Where(t =>
                {
                    var icon = WeaponProfileDB.GetWeaponProfile(t)?.IconProfile;
                    return icon != null && string.IsNullOrWhiteSpace(icon.Icon);
                })
                .ToList();

            Assert.That(blank, Is.Empty,
                "These profiles carry an empty icon name — the single most likely outcome of a botched " +
                "bulk edit across the 184 declarations:\n  " + string.Join("\n  ", blank));
        }

        /// <summary>
        /// The flipbook contract. Ten profiles are Helo_Animation and every one of them must name its
        /// FRAME 0 sprite, because that name is the only thing carrying frames 1-5.
        /// </summary>
        [Test]
        public void EveryHeloAnimationProfile_NamesItsFrameZeroSprite()
        {
            var wrong = new List<string>();

            foreach (var type in RegisteredProfileTypes())
            {
                var icon = WeaponProfileDB.GetWeaponProfile(type)?.IconProfile;
                if (icon == null || icon.IconType != RegimentIconType.Helo_Animation) continue;

                if (!icon.Icon.EndsWith(GameData.ICON_MOTION_FRAME0_SUFFIX, StringComparison.Ordinal))
                    wrong.Add($"{type} -> \"{icon.Icon}\"");
            }

            Assert.That(wrong, Is.Empty,
                $"These Helo_Animation profiles do not end in \"{GameData.ICON_MOTION_FRAME0_SUFFIX}\", " +
                "so their motion flipbook resolves nothing:\n  " + string.Join("\n  ", wrong));
        }

        /// <summary>
        /// Nothing outside the two surviving members may appear. The deleted Directional and
        /// Directional_Fire members took the whole variant-art path with them; a profile reintroducing
        /// one would not compile, but a NEW member added later without a renderer arm would render blank.
        /// </summary>
        [Test]
        public void IconTypes_AreOnlyTheTwoSurvivingMembers()
        {
            var members = Enum.GetNames(typeof(RegimentIconType)).OrderBy(n => n).ToArray();

            Assert.That(members, Is.EqualTo(new[] { "Helo_Animation", "Single" }),
                "RegimentIconType gained or lost a member. The top-down pass left exactly two; any new " +
                "member needs a resolution arm in EquipmentBays.GetIcon and a rotation rule in " +
                "GameIconRenderer before it is safe to author.");
        }

        #endregion // Icon Profile Integrity

        #region Facing Rotation

        /// <summary>
        /// The pure facing→degrees rule. Six facings, 60° apart, measured counter-clockwise from the
        /// canonical west heading — so an east-facing unit is the same sprite turned 180° rather than
        /// the horizontal mirror the deleted path used.
        /// </summary>
        [Test]
        public void EachFacing_MapsToItsSixtyDegreeStep()
        {
            var expected = new (HexDirection Facing, float Degrees)[]
            {
                (HexDirection.W, 0f),
                (HexDirection.SW, 60f),
                (HexDirection.SE, 120f),
                (HexDirection.E, 180f),
                (HexDirection.NE, 240f),
                (HexDirection.NW, 300f)
            };

            var wrong = expected
                .Select(e => new { e.Facing, e.Degrees, Actual = GameIconRenderer.IconRotationDegrees(e.Facing, false) })
                .Where(r => r.Actual != r.Degrees)
                .Select(r => $"{r.Facing}: expected {r.Degrees}°, got {r.Actual}°")
                .ToList();

            Assert.That(wrong, Is.Empty,
                "Facing rotations drifted off the 60° ladder:\n  " + string.Join("\n  ", wrong));
        }

        [Test]
        public void EveryFacing_ProducesADistinctRotation()
        {
            var facings = new[]
            {
                HexDirection.W, HexDirection.NW, HexDirection.NE,
                HexDirection.E, HexDirection.SE, HexDirection.SW
            };

            var degrees = facings.Select(f => GameIconRenderer.IconRotationDegrees(f, false)).ToList();

            Assert.That(degrees.Distinct().Count(), Is.EqualTo(6),
                "Two facings resolved to the same rotation, so those two headings are indistinguishable " +
                "on the map.");
        }

        /// <summary>
        /// §R5 — bases never rotate. HQs, depots and airbases are structures, and the CombatUnit
        /// constructor hands them a side-derived Facing that carries no meaning; honouring it would tilt
        /// every airbase on the map by whatever direction its owner happened to imply.
        /// </summary>
        [Test]
        public void ABase_NeverRotates_WhateverItsFacing()
        {
            var facings = new[]
            {
                HexDirection.W, HexDirection.NW, HexDirection.NE,
                HexDirection.E, HexDirection.SE, HexDirection.SW
            };

            var tilted = facings
                .Select(f => new { Facing = f, Degrees = GameIconRenderer.IconRotationDegrees(f, true) })
                .Where(r => r.Degrees != 0f)
                .Select(r => $"base facing {r.Facing} rotated {r.Degrees}°")
                .ToList();

            Assert.That(tilted, Is.Empty,
                "Bases must stay upright at every facing:\n  " + string.Join("\n  ", tilted));
        }

        #endregion // Facing Rotation
    }
}
