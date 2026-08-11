using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;

namespace HammerAndSickle.Services
{
    /// <summary>
    /// The single authority on <b>how a regiment is physically moving right now</b>.
    ///
    /// Every consumer — terrain cost, zone of control, ambush branching, animation pacing, audio —
    /// asks this rather than deriving its own answer.
    /// </summary>
    /// <remarks>
    /// ⚠ WHY THIS EXISTS. The question used to be answered in five places and four of them were wrong.
    /// `MaxMovementPoints` read the ACTIVE PROFILE and was right; the classification tests, the
    /// `isAir` terrain/ZoC/ambush branch and the movement sound all keyed on `UnitClassification` and
    /// were wrong for exactly the units that have more than one way to travel. An air-assault regiment
    /// riding its Mi-8s is not `UnitClassification.HELO`, so it correctly received 24 movement points
    /// and then spent them paying ground terrain costs and being halted by zones of control it was
    /// flying over. Classification says what a regiment IS; only the active profile says what is
    /// CARRYING it.
    ///
    /// ⚠ PURE AND HEADLESS-SAFE. No scene state, no singletons, no events — safe from model code, from
    /// audio, and from EditorTests. Keep it that way: the moment this touches a manager it stops being
    /// callable from the places that most need it.
    ///
    /// ⚠ NOTHING HERE READS A BALANCE CONSTANT. Movement-point tables and terrain costs are dials Bob
    /// retunes; behaviour keyed to their VALUES would silently change meaning on every rebalance. The
    /// medium is an explicit declared fact for exactly that reason.
    /// </remarks>
    public static class MovementModeService
    {
        #region Public API

        /// <summary>
        /// How the unit is travelling in its CURRENT posture, read from the profile that is actually
        /// carrying it. <see cref="MovementMedium.None"/> when unknown — treat as "no opinion", never as
        /// a default.
        /// </summary>
        /// <remarks>
        /// ⚠ This is the whole fix in one line. A Motor Rifle regiment is `INF_REG_SV` deployed and
        /// `APC_BTR80_SV` mobile; an air-assault regiment adds `HEL_MI8T_SV` embarked. Reading the active
        /// profile gives foot / wheeled / helo with no posture special-case anywhere — the earlier
        /// "is it dismounted?" rule was a symptom of asking the wrong object.
        /// </remarks>
        public static MovementMedium CurrentMedium(CombatUnit unit) =>
            unit?.GetActiveWeaponProfile()?.MovementMedium ?? MovementMedium.None;

        /// <summary>
        /// True when the unit is off the ground RIGHT NOW — a helicopter, an aircraft, or an infantry
        /// regiment riding either.
        /// </summary>
        /// <remarks>
        /// ⚠ THIS IS WHAT `IsFixedWing` AND `IsHelicopter` GET WRONG, and the difference is the entire
        /// air-assault bug: both are classification tests, so an air-assault regiment in flight reports
        /// false and is treated as walking. Use this for terrain cost, zone of control, ambush branching
        /// and animation pacing. `IsFixedWing`/`IsHelicopter` still answer a different and legitimate
        /// question — "is this fundamentally an air unit" — for things like stacking and icon layers.
        /// </remarks>
        public static bool IsAirborneNow(CombatUnit unit) =>
            CurrentMedium(unit) is MovementMedium.Helo or MovementMedium.FixedWing;

        /// <summary>True when the unit is on the ground and moving under its own or a vehicle's power.</summary>
        public static bool IsGroundborneNow(CombatUnit unit) =>
            CurrentMedium(unit) is MovementMedium.Foot or MovementMedium.Wheeled or MovementMedium.Tracked;

        /// <summary>
        /// True while the unit is aboard naval sealift, whose movement is the INSTANT §5.4.2 port-to-port
        /// jump rather than per-hex traversal. Such a unit has no walking movement of any kind.
        /// </summary>
        /// <remarks>
        /// ⚠ THIS IS A PROHIBITION, NOT A TRAVERSAL MODE, and the distinction is the whole point.
        /// §5.4.2.3 abstracts the sea passage away — "movement is instant, naval intermediate hexes
        /// abstracted" — so there is no per-hex sea movement to implement, and §5.4.2.6 defers everything
        /// finer than port-to-port. The destination is chosen with the §24.7a.3 Naval Movement Marker, an
        /// input mode that does not exist yet.
        ///
        /// ⚠ WITHOUT THIS the unit falls through to the GROUND rules, because `Naval` is neither airborne
        /// nor groundborne — and ground rules block water while permitting land, so a regiment that boarded
        /// ships at a port could WALK INLAND still aboard them. Unreachable in Khost (no ports), live
        /// everywhere else the moment a map has one.
        /// </remarks>
        public static bool IsSealiftedNow(CombatUnit unit) =>
            CurrentMedium(unit) is MovementMedium.Naval;

        /// <summary>
        /// The movement-point ceiling for the unit's CURRENT posture. Already correct in the existing
        /// code — recorded here so every "how does this unit move" question has one address.
        /// </summary>
        public static int MaxMovementPoints(CombatUnit unit) =>
            unit?.GetActiveWeaponProfile()?.MaxMovementPoints ?? 0;

        /// <summary>
        /// Re-scales movement points across a posture change, preserving the FRACTION already spent
        /// (§ deployment ruling, Bob 2026-08-04).
        /// </summary>
        /// <remarks>
        /// ⚠ Proportional, not absolute, and it fixes a live defect in both directions. Today only the
        /// ceiling is updated, so a foot regiment with 2 of 4 points that boards helicopters keeps its 2
        /// against a new ceiling of 24 — it flies two hexes. Half your foot movement spent must mean half
        /// your air movement remains.
        /// ⚠ Deliberately free of any particular cost constant, since action and movement costs are being
        /// rebalanced. It only needs the two ceilings and the current value.
        /// </remarks>
        public static float ScaleMovementPoints(float current, float oldMax, float newMax)
        {
            if (oldMax <= 0f) return newMax;          // no meaningful fraction to preserve

            // Clamped by hand rather than via Mathf, so this file stays free of UnityEngine entirely.
            float fraction = current / oldMax;
            if (fraction < 0f) fraction = 0f;
            if (fraction > 1f) fraction = 1f;

            return fraction * newMax;
        }

        #endregion // Public API
    }
}
