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
        #region Sorting

        /// <summary>
        /// This layer's sorting slot. Assigned once in code via <see cref="Configure"/> (HexGridRenderer at
        /// startup) — sorting is NOT authored on this component anymore; SortingConfig is the single authority.
        /// </summary>
        public SortSlot Slot { get; private set; }

        /// <summary>Assigns this layer's sorting slot. Call before any SetSprite.</summary>
        public void Configure(SortSlot slot) => Slot = slot;

        #endregion // Sorting

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
        /// <param name="flipX">If true, mirror the sprite horizontally.</param>
        /// <param name="flipY">If true, mirror the sprite vertically.</param>
        /// <param name="scale">Optional local scale (e.g. fit-to-cell stretch); null = unscaled.</param>
        public void SetSprite(string key, Sprite sprite, Vector3 worldPos, Color color, bool flipX = false, bool flipY = false, Vector2? scale = null)
        {
            var localScale = scale.HasValue ? new Vector3(scale.Value.x, scale.Value.y, 1f) : Vector3.one;

            if (_children.TryGetValue(key, out var existing))
            {
                existing.transform.position = worldPos;
                existing.transform.localScale = localScale;
                var sr = existing.GetComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.flipX = flipX;
                sr.flipY = flipY;
                return;
            }

            // Pool hook: replace Instantiate with pool.Get() here
            var go = new GameObject(key);
            // Inherit the host's Unity layer. New GameObjects default to layer 0 (Default), but the URP
            // Forward Renderer's transparent mask EXCLUDES layer 7 ("No Volume Layer"), which the
            // NoVolumeRendering RenderObjects feature redraws AFTER post-processing — so a layer-0 stamp
            // draws in the EARLY pass and sits under every layer-7 prefab icon regardless of sorting layer.
            // The map hierarchy (and every HexLayer host) lives on layer 7; stamps must too.
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;
            go.transform.localScale = localScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.flipX = flipX;
            renderer.flipY = flipY;
            SortingConfig.Apply(renderer, Slot);

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
