using System;
using System.Collections.Generic;
using HammerAndSickle.Models;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Snapshot of the current Air Operations Box state (HS_DesignDoc §24.7a.7), produced by the AOB framework
    /// (WP5) and consumed by the AOB Status Panel via <c>EventManager.OnAOBStateChanged</c>. Carries everything
    /// the panel needs to render at a glance: the header/type, the §24.7a.6 sub-type indicator states (derived
    /// from <see cref="TargetCategory"/> + <see cref="WwPrelocked"/>), the §11.7 ASB mission line, and the §11.3
    /// slot occupancy. Holds LIVE CombatUnit references per slot — the panel reads name/classification off them
    /// for the icon, exactly as <c>Prefab_UnitPanel</c> does. A null/empty slot renders as "pending/empty".
    /// </summary>
    public class AOBStatus
    {
        /// <summary>The flipped child type, or <see cref="AOBType.None"/> while still a generic pre-flip marker (§11.1.1).</summary>
        public AOBType Type;

        /// <summary>A WW arrived first and pre-locked the box to ASB/AAB/SB (§11.1.2.1); <see cref="Type"/> may still be None.</summary>
        public bool WwPrelocked;

        /// <summary>Category of the hex the box sits on — drives the §11.1.1a sub-icon greying.</summary>
        public AOBTargetCategory TargetCategory;

        /// <summary>The target hex.</summary>
        public Position2D TargetHex;

        /// <summary>For an ASB, the §11.7 mission sub-type; <see cref="AsbMissionType.None"/> otherwise.</summary>
        public AsbMissionType AsbMission;

        /// <summary>The Wild Weasel in the WW slot, or null (ASB/AAB/SB only).</summary>
        public CombatUnit WildWeasel;

        /// <summary>Operative-slot occupants: bombers (ASB ×2) / AWACS (AEWB ×2) / single operative (AAB/SB) / RECONA (RB).</summary>
        public IReadOnlyList<CombatUnit> Operatives = Array.Empty<CombatUnit>();

        /// <summary>Escort-slot occupants (FGT, up to 2; none for RB).</summary>
        public IReadOnlyList<CombatUnit> Escorts = Array.Empty<CombatUnit>();

        /// <summary>Defender interceptor-slot occupants (FGT, up to 2).</summary>
        public IReadOnlyList<CombatUnit> Interceptors = Array.Empty<CombatUnit>();

        /// <summary>Whether the attacker has signalled "done" committing aircraft (gates Resolve, §11.4.6).</summary>
        public bool AttackerCommitted;

        /// <summary>Whether the defender has signalled "done" committing interceptors (§11.4.6).</summary>
        public bool DefenderCommitted;

        /// <summary>True when Resolve is available (both sides committed).</summary>
        public bool CanResolve;
    }
}
