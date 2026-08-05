using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;

namespace HammerAndSickle.Audio
{
    /// <summary>
    /// Decides whether a unit-attributed sound may reach the player. Ratified: HS_DesignDoc §27.7.4.
    ///
    /// ⚠ SOUND IS THE THIRD INTEL CHANNEL. The player learns about enemy units through exactly three
    /// surfaces — the unit ICON (§24.3.2), the HQ dispatch feed (§24.8.3), and AUDIO — and the first two
    /// are already gated by the §12 ladder. An ungated third defeats the other two: if an unspotted enemy
    /// tank fires and the player hears a tank gun, they have learned there is a tank there, and no amount
    /// of icon or dispatch gating compensates for a leak that bypasses both.
    ///
    /// ⚠ ATTRIBUTION IS THE MECHANISM (§27.7.4.2), and it is why no "generic substitute sound" concept is
    /// needed. The FIRING sound is attributed to the firer; the IMPACT sound is attributed to the target.
    /// An unspotted enemy battery shelling a player regiment therefore produces no gun report and a full
    /// impact — the player hears themselves being hit without learning what hit them. Call sites carry the
    /// whole responsibility here: pass the unit the sound BELONGS to, not the unit that happens to be
    /// selected.
    /// </summary>
    public static class AudioFogPolicy
    {
        #region Public API

        /// <summary>
        /// True if a sound attributed to <paramref name="source"/> may be played for the player.
        /// </summary>
        /// <remarks>
        /// ⚠ THRESHOLD IS Level1 AND THAT IS DELIBERATE (§27.7.4.3). §24.3.2.1 already renders unit art
        /// and nationality from Level 1, so the player can see it is a tank; a tank-gun sound at Level 1
        /// leaks nothing the icon has not already given away. This is what keeps audio a SINGLE threshold
        /// rather than growing a six-rung ladder of its own.
        ///
        /// ⚠ FAILS CLOSED — a null source is silent, never audible. Contrast
        /// <c>DefaultDialog_Scene1.IsScreenPointOverUI</c>, which fails OPEN and produced a live defect
        /// (a right-click reaching the map through an unassigned panel, issuing a move order). For a fog
        /// gate the safe direction is silence: a missing sound is a cosmetic bug, a leaked one is a
        /// gameplay bug the player can exploit.
        /// </remarks>
        public static bool CanHear(CombatUnit source)
        {
            if (source == null) return false;

            // The player always hears their own forces, at any spotting level — SpottedLevel describes
            // what the PLAYER knows about an ENEMY and carries no meaning for an owned unit.
            if (source.Side == Side.Player) return true;

            return source.SpottedLevel >= SpottedLevel.Level1;
        }

        /// <summary>
        /// Convenience for the common two-unit case: a sound caused by <paramref name="firer"/> that lands
        /// on <paramref name="target"/>. Returns whether each half may be heard, so a caller resolving one
        /// engagement does not have to remember which half is attributed to whom.
        /// </summary>
        /// <remarks>
        /// ⚠ The impact half is gated on the TARGET, not the firer. That asymmetry IS §27.7.4.2 — it is
        /// what lets an unseen battery hurt the player audibly without identifying itself.
        /// </remarks>
        public static (bool fire, bool impact) CanHearEngagement(CombatUnit firer, CombatUnit target) =>
            (CanHear(firer), CanHear(target));

        #endregion // Public API
    }
}
