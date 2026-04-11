using System.Collections.Generic;
using UnityEngine;

namespace HammerAndSickle.Renderers
{
    /// <summary>
    /// The sorting layers used by the hex grid rendering system.
    /// </summary>
    public enum HexSortingLayer
    {
        Map,
        Units,
        Overlay
    }

    /// <summary>
    /// Manages a collection of child SpriteRenderer GameObjects on a single sorting layer.
    /// Each layer represents one visual concern (hex outlines, selection, movement range, etc.).
    /// </summary>
    public class HexLayer : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private HexSortingLayer sortingLayer = HexSortingLayer.Map;
        [SerializeField] private int sortingOrder;

        #endregion // Serialized Fields

        #region Private Helpers

        /// <summary>Converts the enum to the sorting layer name string Unity expects.</summary>
        private string SortingLayerName => sortingLayer.ToString();

        #endregion // Private Helpers

        #region Fields

        private readonly Dictionary<string, GameObject> _children = new();

        #endregion // Fields

        #region Properties

        /// <summary>Number of active child sprites in this layer.</summary>
        public int Count => _children.Count;

        #endregion // Properties

        #region Public Methods

        /// <summary>
        /// Creates or updates a child SpriteRenderer at the given world position.
        /// </summary>
        /// <param name="key">Unique key identifying this sprite (e.g. "hex_3_7").</param>
        /// <param name="sprite">Sprite to render.</param>
        /// <param name="worldPos">World position for the child GameObject.</param>
        /// <param name="color">Tint color for the SpriteRenderer.</param>
        public void SetSprite(string key, Sprite sprite, Vector3 worldPos, Color color)
        {
            if (_children.TryGetValue(key, out var existing))
            {
                existing.transform.position = worldPos;
                var sr = existing.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                return;
            }

            // Pool hook: replace Instantiate with pool.Get() here
            var go = new GameObject(key);
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = sortingOrder;

            _children[key] = go;
        }

        /// <summary>
        /// Removes and destroys a child sprite by key.
        /// </summary>
        /// <param name="key">Key of the sprite to remove.</param>
        public void RemoveSprite(string key)
        {
            if (!_children.Remove(key, out var go)) return;

            // Pool hook: replace Destroy with pool.Return() here
            Destroy(go);
        }

        /// <summary>
        /// Gets the SpriteRenderer for a given key, or null if not found.
        /// </summary>
        /// <param name="key">Key to look up.</param>
        /// <returns>SpriteRenderer or null.</returns>
        public SpriteRenderer GetSprite(string key)
        {
            return _children.TryGetValue(key, out var go) ? go.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>
        /// Destroys all child sprites managed by this layer.
        /// </summary>
        public void Clear()
        {
            foreach (var go in _children.Values)
            {
                // Pool hook: replace Destroy with pool.Return() here
                Destroy(go);
            }
            _children.Clear();
        }

        #endregion // Public Methods
    }
}
