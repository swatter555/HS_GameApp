using System;
using System.Collections.Generic;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models.Combat
{
    /// <summary>
    /// Air Operations Box child type (HS_DesignDoc §11.1). The first five are voluntary operative-flipped boxes;
    /// AIB is the reactive sixth (§11.8.10) — included for completeness but never drag-placed, so it is not part
    /// of the pre-filter. <see cref="None"/> = the unit is not an operative (escort/WW/interceptor) and flips nothing.
    /// </summary>
    public enum AOBType { None, ASB, AAB, RB, AEWB, SB, AIB }

    /// <summary>
    /// ASB mission sub-type, chosen by the TARGET once the box has flipped to ASB (§11.7). SEAD (§11.7.4) is an
    /// explicit player choice on a SAM target, not auto-selected — a SAM resolves as <see cref="GroundStrike"/>
    /// unless the player opts into suppression.
    /// </summary>
    public enum AsbMissionType { None, GroundStrike, BaseStrike, BridgeStrike, SeadSuppression }

    /// <summary>
    /// Classification of the hex the AOB was dropped on — the pre-filter input (§11.1.1a). The CALLER derives this
    /// from the HexTile + its occupants (so the resolver stays pure: no map / CombatUnit coupling). <see cref="Invalid"/>
    /// (the default) yields an empty candidate set.
    /// </summary>
    public enum AOBTargetCategory { Invalid, EnemyUnit, EnemyBase, Bridge, FriendlyUnit, OpenHex }

    /// <summary>
    /// Pure AOB mission/type resolution (HS_DesignDoc §11.1.1 + §11.1.1a + §11.7). Two authoritative layers plus a
    /// drop-time pre-filter:
    ///   • <see cref="ResolveBoxType"/> — the operative's class flips the box (§11.1.1). AUTHORITATIVE.
    ///   • <see cref="CandidateBoxTypes"/> — the drag-drop target NARROWS which boxes (and operatives) are legal
    ///     (§11.1.1a); the UI greys out the rest. It never overrides the operative — it only constrains.
    ///   • <see cref="IsOperativeLegalForTarget"/> — composes the two: the operative's box type must be a candidate.
    ///   • <see cref="ResolveAsbMission"/> — for an ASB, the target picks the §11.7 mission sub-type.
    /// Pure — operates on enums only, no map/unit/RNG coupling — so the whole matrix is unit-testable. The AIB
    /// (§11.8.10) is reactive and excluded.
    /// </summary>
    public static class AOBMissionResolver
    {
        private const string CLASS_NAME = nameof(AOBMissionResolver);

        // §11.1.1a pre-filter matrix. RB is legal on EVERY category (recon can search any hex); AAB and AEWB are
        // OpenHex-only (a paradrop / an early-warning operating point both need clear ground — and AAB onto an
        // occupied hex is blocked anyway, §11.12.6.6). One-line-tunable per the flagged judgment calls.
        private static readonly AOBType[] _enemyUnit    = { AOBType.ASB, AOBType.RB };
        private static readonly AOBType[] _enemyBase    = { AOBType.ASB, AOBType.RB };
        private static readonly AOBType[] _bridge       = { AOBType.ASB, AOBType.RB };
        private static readonly AOBType[] _friendlyUnit = { AOBType.SB, AOBType.RB };
        private static readonly AOBType[] _openHex      = { AOBType.AAB, AOBType.AEWB, AOBType.RB };
        private static readonly AOBType[] _none         = Array.Empty<AOBType>();

        /// <summary>
        /// The operative's class flips the box (§11.1.1). A TRN is disambiguated by payload (§11.9.7): carrying an
        /// Embarked para → AAB, otherwise (supply load) → SB. Non-operative classes (FGT escort/interceptor, WW,
        /// HELO, ground units) return <see cref="AOBType.None"/> — they flip nothing.
        /// </summary>
        public static AOBType ResolveBoxType(UnitClassification operative, bool transportCarryingPara)
        {
            try
            {
                return operative switch
                {
                    UnitClassification.BMB or UnitClassification.ATT => AOBType.ASB,
                    UnitClassification.RECONA                        => AOBType.RB,
                    UnitClassification.AWACS                         => AOBType.AEWB,
                    UnitClassification.TRN                           => transportCarryingPara ? AOBType.AAB : AOBType.SB,
                    _                                                => AOBType.None,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveBoxType), e);
                return AOBType.None;
            }
        }

        /// <summary>
        /// The box types legal for a dropped-on target category (§11.1.1a pre-filter). An <see cref="AOBTargetCategory.Invalid"/>
        /// target yields an empty set. Returned lists are shared read-only — do not mutate.
        /// </summary>
        public static IReadOnlyList<AOBType> CandidateBoxTypes(AOBTargetCategory target)
        {
            try
            {
                return target switch
                {
                    AOBTargetCategory.EnemyUnit    => _enemyUnit,
                    AOBTargetCategory.EnemyBase    => _enemyBase,
                    AOBTargetCategory.Bridge       => _bridge,
                    AOBTargetCategory.FriendlyUnit => _friendlyUnit,
                    AOBTargetCategory.OpenHex      => _openHex,
                    _                              => _none,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CandidateBoxTypes), e);
                return _none;
            }
        }

        /// <summary>
        /// Composes the two layers (§11.1.1a): true iff this operative's box type (§11.1.1) is in the target's
        /// candidate set. A non-operative class is never legal. The UI uses this to grey out illegal operatives at drop.
        /// </summary>
        public static bool IsOperativeLegalForTarget(UnitClassification operative, bool transportCarryingPara, AOBTargetCategory target)
        {
            try
            {
                AOBType box = ResolveBoxType(operative, transportCarryingPara);
                if (box == AOBType.None) return false;

                IReadOnlyList<AOBType> candidates = CandidateBoxTypes(target);
                for (int i = 0; i < candidates.Count; i++)
                    if (candidates[i] == box) return true;

                return false;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsOperativeLegalForTarget), e);
                return false;
            }
        }

        /// <summary>
        /// For an ASB, the target picks the §11.7 mission sub-type: enemy unit → Ground Strike (§11.7.1), base →
        /// Base/Strategic (§11.7.2/§11.7.5), bridge → Bridge Strike (§11.7.3). SEAD (§11.7.4) is a separate explicit
        /// choice on a SAM target, not returned here. Non-ASB targets return <see cref="AsbMissionType.None"/>.
        /// </summary>
        public static AsbMissionType ResolveAsbMission(AOBTargetCategory target)
        {
            try
            {
                return target switch
                {
                    AOBTargetCategory.EnemyUnit => AsbMissionType.GroundStrike,
                    AOBTargetCategory.EnemyBase => AsbMissionType.BaseStrike,
                    AOBTargetCategory.Bridge    => AsbMissionType.BridgeStrike,
                    _                           => AsbMissionType.None,
                };
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(ResolveAsbMission), e);
                return AsbMissionType.None;
            }
        }
    }
}
