using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models.Combat;
using NUnit.Framework;

namespace HammerAndSickle.Tests
{
    /// <summary>
    /// M0 validation: the Δ→band ladder (§7.6.1–§7.6.11), the band-shift clamp (§7.7.2), the band/terrain
    /// dice expressions (§7.6 / §7.5.6.2), and round-half-up (§7.7.6). Pure math — no GameDataManager needed.
    /// </summary>
    [TestFixture]
    public class CombatMathTests
    {
        #region Δ → Band ladder boundaries (§7.6)

        [Test]
        public void DeltaBand_NegativeBands_MapAtBoundaries()
        {
            Assert.AreEqual(DamageBand.Hopeless,      CombatMath.DeltaBand(-100), "Δ ≤ −13 deep");
            Assert.AreEqual(DamageBand.Hopeless,      CombatMath.DeltaBand(-13),  "Δ = −13");
            Assert.AreEqual(DamageBand.Forlorn,       CombatMath.DeltaBand(-12),  "Δ = −12 (Forlorn lo)");
            Assert.AreEqual(DamageBand.Forlorn,       CombatMath.DeltaBand(-11),  "Δ = −11 (Forlorn hi)");
            Assert.AreEqual(DamageBand.Difficult,     CombatMath.DeltaBand(-10),  "Δ = −10 (Difficult lo)");
            Assert.AreEqual(DamageBand.Difficult,     CombatMath.DeltaBand(-8),   "Δ = −8 (Difficult hi)");
            Assert.AreEqual(DamageBand.Grim,          CombatMath.DeltaBand(-7),   "Δ = −7 (Grim lo)");
            Assert.AreEqual(DamageBand.Grim,          CombatMath.DeltaBand(-5),   "Δ = −5 (Grim hi)");
            Assert.AreEqual(DamageBand.Disadvantaged, CombatMath.DeltaBand(-4),   "Δ = −4 (Disadv lo)");
            Assert.AreEqual(DamageBand.Disadvantaged, CombatMath.DeltaBand(-2),   "Δ = −2 (Disadv hi)");
        }

        [Test]
        public void DeltaBand_EvenAndPositiveBands_MapAtBoundaries()
        {
            Assert.AreEqual(DamageBand.Even,       CombatMath.DeltaBand(-1), "Δ = −1 (Even lo)");
            Assert.AreEqual(DamageBand.Even,       CombatMath.DeltaBand(0),  "Δ = 0 (Even mid)");
            Assert.AreEqual(DamageBand.Even,       CombatMath.DeltaBand(1),  "Δ = +1 (Even hi)");
            Assert.AreEqual(DamageBand.Favorable,  CombatMath.DeltaBand(2),  "Δ = +2 (Favorable lo)");
            Assert.AreEqual(DamageBand.Favorable,  CombatMath.DeltaBand(4),  "Δ = +4 (Favorable hi)");
            Assert.AreEqual(DamageBand.Advantaged, CombatMath.DeltaBand(5),  "Δ = +5 (Advantaged lo)");
            Assert.AreEqual(DamageBand.Advantaged, CombatMath.DeltaBand(7),  "Δ = +7 (Advantaged hi)");
            Assert.AreEqual(DamageBand.Strong,     CombatMath.DeltaBand(8),  "Δ = +8 (Strong lo)");
            Assert.AreEqual(DamageBand.Strong,     CombatMath.DeltaBand(10), "Δ = +10 (Strong hi)");
            Assert.AreEqual(DamageBand.Commanding, CombatMath.DeltaBand(11), "Δ = +11 (Commanding lo)");
            Assert.AreEqual(DamageBand.Commanding, CombatMath.DeltaBand(13), "Δ = +13 (Commanding hi)");
            Assert.AreEqual(DamageBand.Crushing,   CombatMath.DeltaBand(14), "Δ = +14 (Crushing lo)");
            Assert.AreEqual(DamageBand.Crushing,   CombatMath.DeltaBand(99), "Δ ≥ +14 deep");
        }

        #endregion // Δ → Band ladder boundaries

        #region Band shift clamp (§7.7.2)

        [Test]
        public void ShiftBand_StepsAndClampsAtEnds()
        {
            Assert.AreEqual(DamageBand.Favorable,     CombatMath.ShiftBand(DamageBand.Even, 1),       "Even +1 = Favorable");
            Assert.AreEqual(DamageBand.Disadvantaged, CombatMath.ShiftBand(DamageBand.Favorable, -2), "Favorable −2 = Disadvantaged");
            Assert.AreEqual(DamageBand.Crushing,      CombatMath.ShiftBand(DamageBand.Crushing, 1),   "Crushing +1 clamps to Crushing");
            Assert.AreEqual(DamageBand.Hopeless,      CombatMath.ShiftBand(DamageBand.Hopeless, -1),  "Hopeless −1 clamps to Hopeless");
        }

        #endregion // Band shift clamp

        #region Band dice min/max (§7.6)

        [Test]
        public void RollBandDamage_MaxRolls_HitBandCeilings()
        {
            Assert.AreEqual(0,  CombatMath.RollBandDamage(DamageBand.Hopeless,      new FixedRollRandom(8)),  "Hopeless = 0");
            Assert.AreEqual(1,  CombatMath.RollBandDamage(DamageBand.Forlorn,       new FixedRollRandom(2)),  "Forlorn max (1d2−1)");
            Assert.AreEqual(2,  CombatMath.RollBandDamage(DamageBand.Difficult,     new FixedRollRandom(3)),  "Difficult max (1d3−1)");
            Assert.AreEqual(3,  CombatMath.RollBandDamage(DamageBand.Grim,          new FixedRollRandom(3)),  "Grim max (1d3)");
            Assert.AreEqual(4,  CombatMath.RollBandDamage(DamageBand.Disadvantaged, new FixedRollRandom(4)),  "Disadvantaged max (1d4)");
            Assert.AreEqual(7,  CombatMath.RollBandDamage(DamageBand.Even,          new FixedRollRandom(8)),  "Even max (1d8−1)");
            Assert.AreEqual(8,  CombatMath.RollBandDamage(DamageBand.Favorable,     new FixedRollRandom(6)),  "Favorable max (1d6+2)");
            Assert.AreEqual(11, CombatMath.RollBandDamage(DamageBand.Advantaged,    new FixedRollRandom(8)),  "Advantaged max (1d8+3)");
            Assert.AreEqual(14, CombatMath.RollBandDamage(DamageBand.Strong,        new FixedRollRandom(10)), "Strong max (1d10+4)");
            Assert.AreEqual(17, CombatMath.RollBandDamage(DamageBand.Commanding,    new FixedRollRandom(6)),  "Commanding max (2d6+5)");
            Assert.AreEqual(22, CombatMath.RollBandDamage(DamageBand.Crushing,      new FixedRollRandom(8)),  "Crushing max (2d8+6)");
        }

        [Test]
        public void RollBandDamage_MinRolls_HitBandFloors()
        {
            Assert.AreEqual(0, CombatMath.RollBandDamage(DamageBand.Forlorn,       new FixedRollRandom(1)), "Forlorn min (1d2−1)");
            Assert.AreEqual(0, CombatMath.RollBandDamage(DamageBand.Difficult,     new FixedRollRandom(1)), "Difficult min (1d3−1)");
            Assert.AreEqual(1, CombatMath.RollBandDamage(DamageBand.Grim,          new FixedRollRandom(1)), "Grim min (1d3)");
            Assert.AreEqual(1, CombatMath.RollBandDamage(DamageBand.Disadvantaged, new FixedRollRandom(1)), "Disadvantaged min (1d4)");
            Assert.AreEqual(0, CombatMath.RollBandDamage(DamageBand.Even,          new FixedRollRandom(1)), "Even min (1d8−1) = natural 0");
            Assert.AreEqual(3, CombatMath.RollBandDamage(DamageBand.Favorable,     new FixedRollRandom(1)), "Favorable min (1d6+2)");
            Assert.AreEqual(8, CombatMath.RollBandDamage(DamageBand.Crushing,      new FixedRollRandom(1)), "Crushing min (2d8+6)");
        }

        #endregion // Band dice min/max

        #region Terrain block (§7.5.6.2)

        [Test]
        public void BlockTier_MapsTerrainToTiers()
        {
            Assert.AreEqual(TerrainBlockTier.None,   CombatMath.BlockTier(TerrainType.Clear),      "Clear = None");
            Assert.AreEqual(TerrainBlockTier.None,   CombatMath.BlockTier(TerrainType.Water),      "Water = None");
            Assert.AreEqual(TerrainBlockTier.None,   CombatMath.BlockTier(TerrainType.Impassable), "Impassable = None");
            Assert.AreEqual(TerrainBlockTier.Light,  CombatMath.BlockTier(TerrainType.Forest),     "Forest = Light");
            Assert.AreEqual(TerrainBlockTier.Light,  CombatMath.BlockTier(TerrainType.MinorCity),  "MinorCity = Light");
            Assert.AreEqual(TerrainBlockTier.Medium, CombatMath.BlockTier(TerrainType.Rough),      "Rough = Medium");
            Assert.AreEqual(TerrainBlockTier.Medium, CombatMath.BlockTier(TerrainType.Marsh),      "Marsh = Medium");
            Assert.AreEqual(TerrainBlockTier.Heavy,  CombatMath.BlockTier(TerrainType.MajorCity),  "MajorCity = Heavy");
            Assert.AreEqual(TerrainBlockTier.Heavy,  CombatMath.BlockTier(TerrainType.Mountains),  "Mountains = Heavy");
        }

        [Test]
        public void RollTerrainBlock_ProducesTierDice()
        {
            Assert.AreEqual(0, CombatMath.RollTerrainBlock(TerrainType.Clear,     new FixedRollRandom(4)), "Clear = 0");
            Assert.AreEqual(2, CombatMath.RollTerrainBlock(TerrainType.Forest,    new FixedRollRandom(2)), "Forest max (1d2)");
            Assert.AreEqual(1, CombatMath.RollTerrainBlock(TerrainType.MinorCity, new FixedRollRandom(1)), "MinorCity min (1d2)");
            Assert.AreEqual(4, CombatMath.RollTerrainBlock(TerrainType.Rough,     new FixedRollRandom(4)), "Rough max (1d4)");
            Assert.AreEqual(6, CombatMath.RollTerrainBlock(TerrainType.MajorCity, new FixedRollRandom(4)), "MajorCity max (1d4+2)");
            Assert.AreEqual(3, CombatMath.RollTerrainBlock(TerrainType.Mountains, new FixedRollRandom(1)), "Mountains min (1d4+2)");
        }

        #endregion // Terrain block

        #region Rounding (§7.7.6)

        [Test]
        public void RoundHalfUp_RoundsAwayFromZeroAtMidpoint()
        {
            Assert.AreEqual(7, CombatMath.RoundHalfUp(6.5), "6.5 → 7");
            Assert.AreEqual(3, CombatMath.RoundHalfUp(2.5), "2.5 → 3 (not banker's 2)");
            Assert.AreEqual(2, CombatMath.RoundHalfUp(2.4), "2.4 → 2");
            Assert.AreEqual(7, CombatMath.RoundHalfUp(6.9), "6.9 → 7");
        }

        #endregion // Rounding
    }
}
