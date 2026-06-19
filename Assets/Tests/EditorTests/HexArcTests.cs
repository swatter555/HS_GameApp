using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M2 facing geometry (§5.8.7 / §6.8.1): front/flank arc classification and the retreat rear arc. Pure
    /// enum math over the 6 directions (NE=0, E=1, SE=2, SW=3, W=4, NW=5).
    /// </summary>
    [TestFixture]
    public class HexArcTests
    {
        [Test]
        public void Opposite_Is180Degrees()
        {
            Assert.AreEqual(HexDirection.SW, HexArc.Opposite(HexDirection.NE), "NE↔SW");
            Assert.AreEqual(HexDirection.W,  HexArc.Opposite(HexDirection.E),  "E↔W");
            Assert.AreEqual(HexDirection.NW, HexArc.Opposite(HexDirection.SE), "SE↔NW");
            Assert.AreEqual(HexDirection.NE, HexArc.Opposite(HexDirection.SW), "SW↔NE");
        }

        [Test]
        public void IsFrontArc_FacingEast_CoversNE_E_SE()
        {
            Assert.IsTrue(HexArc.IsFrontArc(HexDirection.E, HexDirection.NE), "NE in front");
            Assert.IsTrue(HexArc.IsFrontArc(HexDirection.E, HexDirection.E),  "E in front");
            Assert.IsTrue(HexArc.IsFrontArc(HexDirection.E, HexDirection.SE), "SE in front");
            Assert.IsFalse(HexArc.IsFrontArc(HexDirection.E, HexDirection.SW), "SW is flank");
            Assert.IsFalse(HexArc.IsFrontArc(HexDirection.E, HexDirection.W),  "W is flank");
            Assert.IsFalse(HexArc.IsFrontArc(HexDirection.E, HexDirection.NW), "NW is flank");
        }

        [Test]
        public void IsFlankAttack_IsComplementOfFront()
        {
            Assert.IsFalse(HexArc.IsFlankAttack(HexDirection.E, HexDirection.E),  "frontal");
            Assert.IsTrue(HexArc.IsFlankAttack(HexDirection.E, HexDirection.W),   "rear");
            Assert.IsTrue(HexArc.IsFlankAttack(HexDirection.W, HexDirection.E),   "rear (facing W)");
            Assert.IsFalse(HexArc.IsFlankAttack(HexDirection.W, HexDirection.NW), "front (facing W)");
        }

        [Test]
        public void RearArc_IsThreeEdgesOppositeTheAttacker()
        {
            // Attacker bearing E → retreat toward the opposite trio {SW, W, NW}.
            CollectionAssert.AreEqual(
                new[] { HexDirection.SW, HexDirection.W, HexDirection.NW },
                HexArc.RearArc(HexDirection.E), "rear of an E attack");

            // Attacker bearing NE → opposite trio {SE, SW, W}.
            CollectionAssert.AreEqual(
                new[] { HexDirection.SE, HexDirection.SW, HexDirection.W },
                HexArc.RearArc(HexDirection.NE), "rear of an NE attack");

            // Attacker bearing W → opposite trio {NE, E, SE}.
            CollectionAssert.AreEqual(
                new[] { HexDirection.NE, HexDirection.E, HexDirection.SE },
                HexArc.RearArc(HexDirection.W), "rear of a W attack");
        }
    }
}
