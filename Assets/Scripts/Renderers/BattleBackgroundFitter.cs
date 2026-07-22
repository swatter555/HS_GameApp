using System;
using HammerAndSickle.Core;
using HammerAndSickle.Services;
using UnityEngine;

namespace HammerAndSickle.Renderers
{
    /// <summary>
    /// Scales and positions the battle-scene background room sprite ("Background Room") so the
    /// map window baked into the art — the glowing table surface inside the green tube border —
    /// frames the loaded hex map at any map size. Moves and scales the background only; the map
    /// itself never moves. Called by BattleManager during scenario setup, after
    /// HexGridSystem.Initialize.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BattleBackgroundFitter : MonoBehaviour
    {
        #region Constants

        private const string CLASS_NAME = nameof(BattleBackgroundFitter);

        #endregion // Constants

        #region Inspector Fields

        // Calibration: where the hex map's drawn footprint sits inside the background image, in
        // normalized image coordinates (fractions of image size). Derived 2026-07-22 by
        // reverse-engineering Bob's hand-tuned 32x21 Khost setup (Background Room at world
        // (40.1971, 18.4457), scale (2.5294, 2.9052), sprite 76.8 x 43.2 world units):
        //   windowSize   = map drawn extent / background world footprint
        //   windowOffset = (map center - background center) / background world footprint
        // Feeding 32x21 through FitToMap reproduces that transform exactly; every other map size
        // generalizes from the same window. Nudge these only to reposition the map within the
        // table art for ALL map sizes at once. The green tube border + glow padding live in the
        // image OUTSIDE this window, so they are preserved automatically.
        [Header("Image Calibration (fractions of image size)")]

        [SerializeField]
        [Tooltip("Map-window center offset from the image center, as a fraction of image size.")]
        private Vector2 windowCenterOffset = new Vector2(-0.0026619f, 0.0296766f);

        [SerializeField]
        [Tooltip("Map-window size as a fraction of image size.")]
        private Vector2 windowSize = new Vector2(0.4217073f, 0.3768509f);

        #endregion // Inspector Fields

        #region Public Methods

        /// <summary>
        /// Fits the background so its map window coincides with the drawn extents of a
        /// mapWidth x mapHeight hex grid. Preserves the sprite's Z depth.
        /// </summary>
        /// <param name="mapWidth">Map width in hex columns.</param>
        /// <param name="mapHeight">Map height in hex rows.</param>
        public void FitToMap(int mapWidth, int mapHeight)
        {
            try
            {
                if (mapWidth <= 0 || mapHeight <= 0)
                {
                    Debug.LogWarning($"{CLASS_NAME}.FitToMap: invalid map size {mapWidth}x{mapHeight} — background left unchanged.");
                    return;
                }

                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null || spriteRenderer.sprite == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}.FitToMap: no sprite assigned — background left unchanged.");
                    return;
                }

                // Natural sprite size in world units at scale 1 (texture pixels / PPU).
                Vector2 naturalSize = spriteRenderer.sprite.bounds.size;
                if (naturalSize.x <= 0f || naturalSize.y <= 0f ||
                    windowSize.x <= 0f || windowSize.y <= 0f)
                {
                    Debug.LogWarning($"{CLASS_NAME}.FitToMap: degenerate sprite or window size — background left unchanged.");
                    return;
                }

                // Drawn extents of the hex grid in world units. Horizontal: even rows span the
                // full width (odd rows are inset half a hex and one column short), so
                // edge-to-edge width = cols * HEX_WIDTH. Vertical: row centers span
                // (rows - 1) * VERTICAL_SPACING plus one full point-to-point hex height across
                // the outer rows.
                float mapW = mapWidth * HexGridSystem.HEX_WIDTH;
                float mapH = ((mapHeight - 1) * HexGridSystem.VERTICAL_SPACING) + HexGridSystem.HEX_HEIGHT;

                // Hex (0,0)'s center sits at world (0,0) per HexGridSystem.HexToWorld, so the
                // drawn rect starts half a hex left of / below it.
                var mapCenter = new Vector2(
                    -HexGridSystem.HALF_HEX_WIDTH + (mapW * 0.5f),
                    (-HexGridSystem.HEX_HEIGHT * 0.5f) + (mapH * 0.5f));

                // Per-axis scale so the image's map window matches the map extents exactly.
                float scaleX = mapW / (windowSize.x * naturalSize.x);
                float scaleY = mapH / (windowSize.y * naturalSize.y);

                // Position so the window center lands on the map center. The offset is measured
                // from the image center, so it scales with the image.
                float bgCenterX = mapCenter.x - (windowCenterOffset.x * naturalSize.x * scaleX);
                float bgCenterY = mapCenter.y - (windowCenterOffset.y * naturalSize.y * scaleY);

                transform.position = new Vector3(bgCenterX, bgCenterY, transform.position.z);

                // Convert the desired WORLD scale to local scale. Parents are identity-scaled
                // today ("Background" node offsets position only), but stay correct if that
                // ever changes.
                Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
                if (parentScale.x == 0f || parentScale.y == 0f)
                {
                    Debug.LogWarning($"{CLASS_NAME}.FitToMap: parent has zero scale — background left unchanged.");
                    return;
                }

                transform.localScale = new Vector3(scaleX / parentScale.x, scaleY / parentScale.y, 1f);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(FitToMap), e);
            }
        }

        #endregion // Public Methods
    }
}
