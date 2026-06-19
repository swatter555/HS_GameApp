using HammerAndSickle.Core.GameData;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Pure hex facing-arc geometry (HS_DesignDoc §5.8.7 / §6.8.1) over the 6 <see cref="HexDirection"/> edges
    /// (NE=0, E=1, SE=2, SW=3, W=4, NW=5). A unit's FRONT arc is the facing edge and its two neighbours; the
    /// other three are the FLANK (rear) arc. The retreat rear arc is the three edges opposite the attacker.
    /// No map/state — just enum math, so it's trivially testable and reused by both flank detection and retreat.
    /// </summary>
    public static class HexArc
    {
        private const int DIRS = 6;

        /// <summary>The opposite edge (180°): NE↔SW, E↔W, SE↔NW.</summary>
        public static HexDirection Opposite(HexDirection d) => (HexDirection)(((int)d + 3) % DIRS);

        /// <summary>
        /// True if an attack arriving from <paramref name="incoming"/> hits the FRONT arc of a unit facing
        /// <paramref name="facing"/> — i.e. within one edge of the facing direction (§5.8.7).
        /// </summary>
        public static bool IsFrontArc(HexDirection facing, HexDirection incoming)
        {
            int diff = (((int)incoming - (int)facing) % DIRS + DIRS) % DIRS; // 0..5
            return diff == 0 || diff == 1 || diff == DIRS - 1;              // facing, +1, −1
        }

        /// <summary>
        /// True if a direct attack whose bearing (defender→attacker) is <paramref name="attackBearing"/> lands
        /// in the FLANK arc of a unit facing <paramref name="facing"/> (§5.8.7 / §7.9.4c).
        /// </summary>
        public static bool IsFlankAttack(HexDirection facing, HexDirection attackBearing) =>
            !IsFrontArc(facing, attackBearing);

        /// <summary>
        /// The three rear-arc directions a unit retreats toward — the edges opposite the attacker, given the
        /// attack bearing (defender→attacker). Order is {opposite−1, opposite, opposite+1}, used as the stable
        /// hex-index tiebreak for retreat-candidate ranking (§6.8.1 / §6.8.2).
        /// </summary>
        public static HexDirection[] RearArc(HexDirection attackBearing)
        {
            int opp = ((int)attackBearing + 3) % DIRS;
            return new[]
            {
                (HexDirection)((opp + DIRS - 1) % DIRS),
                (HexDirection)opp,
                (HexDirection)((opp + 1) % DIRS),
            };
        }
    }
}
