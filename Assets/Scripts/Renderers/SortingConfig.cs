using UnityEngine;

namespace HammerAndSickle.Renderers
{
    /// <summary>
    /// Every distinct sprite-sorting concern in the battle map. One slot == one (sorting layer, base order).
    /// </summary>
    public enum SortSlot
    {
        // Map sorting layer (bottom -> top). HexSelect sits ABOVE all terrain features and icons
        // (rivers/roads/bridges/cities/impassable) so the selection ring is never occluded — only
        // MapText renders over it (Bob 2026-07-22).
        HexOutline,
        MapIcon,
        RiverBank,
        RiverWater,
        Road,
        BridgeIcon,
        CityIcon,
        Impassable,
        HexSelect,
        MapText,

        // Units sorting layer
        GroundUnit,
        AirUnit,

        // Overlay sorting layer
        Utility1,
        Utility2,
        MovementRange,
        MovementPath
    }

    /// <summary>
    /// THE single authority for sprite sorting on the battle map. Maps each <see cref="SortSlot"/> to its
    /// Unity sorting layer + base order, and stamps that onto any renderer. This OVERRIDES whatever sorting
    /// was baked into a prefab asset or serialized on a component — nothing else in the render code sets
    /// sortingLayerName / sortingOrder.
    ///
    /// Split of responsibility:
    ///   - Sorting LAYER + BASE ORDER of a concern  -> here (this file).
    ///   - A multi-part prefab's INTERNAL element order -> const offsets in that prefab's own script,
    ///     passed as <c>subOrder</c> to <see cref="Apply"/>.
    ///
    /// Base orders are spaced by 10 so each prefab has headroom for its element offsets before colliding
    /// with the next concern. Absolute numbers are arbitrary; only the relative order within a sorting layer
    /// matters (kept consistent with Claude_Project §3.5).
    /// </summary>
    public static class SortingConfig
    {
        /// <summary>Resolves a slot to its (sorting layer, base order).</summary>
        private static (HexSortingLayer layer, int order) Resolve(SortSlot slot) => slot switch
        {
            SortSlot.HexOutline    => (HexSortingLayer.Map, 0),
            SortSlot.MapIcon       => (HexSortingLayer.Map, 10),
            SortSlot.RiverBank     => (HexSortingLayer.Map, 20),
            SortSlot.RiverWater    => (HexSortingLayer.Map, 30),
            SortSlot.Road          => (HexSortingLayer.Map, 40),
            SortSlot.BridgeIcon    => (HexSortingLayer.Map, 50),
            SortSlot.CityIcon      => (HexSortingLayer.Map, 60),
            SortSlot.Impassable    => (HexSortingLayer.Map, 70),
            SortSlot.HexSelect     => (HexSortingLayer.Map, 80),
            SortSlot.MapText       => (HexSortingLayer.Map, 90),

            SortSlot.GroundUnit    => (HexSortingLayer.Units, 0),
            SortSlot.AirUnit       => (HexSortingLayer.Units, 10),

            SortSlot.Utility1      => (HexSortingLayer.Overlay, 0),
            SortSlot.Utility2      => (HexSortingLayer.Overlay, 10),
            SortSlot.MovementRange => (HexSortingLayer.Overlay, 20),
            SortSlot.MovementPath  => (HexSortingLayer.Overlay, 30),

            _ => (HexSortingLayer.Map, 0)
        };

        /// <summary>The Unity sorting layer name for a slot (e.g. "Map", "Overlay").</summary>
        public static string LayerName(SortSlot slot) => Resolve(slot).layer.ToString();

        /// <summary>The base sorting order for a slot.</summary>
        public static int Order(SortSlot slot) => Resolve(slot).order;

        /// <summary>
        /// Stamps a renderer onto a slot, overriding any baked sorting. <paramref name="subOrder"/> is the
        /// renderer's offset within its own prefab's internal stack (0 for single-renderer / direct stamps).
        /// Null-safe (a prefab's optional renderer may be absent).
        /// </summary>
        public static void Apply(Renderer renderer, SortSlot slot, int subOrder = 0)
        {
            if (renderer == null) return;
            var (layer, order) = Resolve(slot);
            renderer.sortingLayerName = layer.ToString();
            renderer.sortingOrder = order + subOrder;
        }
    }
}
