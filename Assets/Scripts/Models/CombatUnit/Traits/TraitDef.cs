using System.Collections.Generic;
using System.Linq;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Definition of a single weapon trait: its catalog identity plus the bundle of effects it
    /// applies. Held in <see cref="WeaponTraitCatalog"/>. Immutable.
    /// </summary>
    public sealed class TraitDef
    {
        #region Properties

        public WeaponTrait Id { get; }
        public TraitCategory Category { get; }
        public string Note { get; }
        public IReadOnlyList<TraitEffect> Effects { get; }

        /// <summary>True when every effect is Dormant (catalogued but inert in Phase 1).</summary>
        public bool IsFullyDormant => Effects.All(e => e.Status == EffectStatus.Dormant);

        #endregion // Properties

        #region Construction

        public TraitDef(WeaponTrait id, TraitCategory category, string note, params TraitEffect[] effects)
        {
            Id = id;
            Category = category;
            Note = note;
            Effects = effects ?? System.Array.Empty<TraitEffect>();
        }

        #endregion // Construction
    }
}
