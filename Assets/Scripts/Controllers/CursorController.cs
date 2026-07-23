using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Renderers;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Live combat-input feedback (§24.11.3, amended 2026-07-22) — while Ctrl is held over a LEGAL combat
    /// target, the TargetPickOutline hex stamp appears on the target hex (via HexGridRenderer) and the cursor
    /// stays the default arrow; Ctrl over anything illegal shows the DENIED cursor (the old crosshair cursor
    /// is retired). Legality comes from MovementController.AttackLegality — the SAME gate the click runs, so
    /// the feedback never lies. Poll-based (no EventManager subscriptions — survives scene transitions and
    /// ClearAllSubscriptions): inert whenever no battle map is loaded. Per-mode cursors for future input modes
    /// (unit-pick §24.5.5, AOB placement §24.7a.1) slot in here as those modes land.
    /// Cursor art: assign a denied texture on a scene instance to use a real sprite; a bootstrap-created
    /// instance falls back to a procedural placeholder.
    /// </summary>
    public class CursorController : MonoBehaviour
    {
        private const string CLASS_NAME = nameof(CursorController);
        private const int TEX_SIZE = 32;

        #region Singleton

        private static CursorController _instance;
        public static CursorController Instance => _instance;

        /// <summary>Ensures a controller exists even when no scene object was placed (placeholder-art path).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance == null)
            {
                var go = new GameObject("CursorController");
                go.AddComponent<CursorController>();
            }
        }

        #endregion // Singleton

        #region Fields

        private enum CursorVisual { Default, Denied }

        [Header("Cursor Art (optional — procedural placeholder used when empty)")]
        [SerializeField] private Texture2D deniedCursor;

        private Texture2D _fallbackDenied;
        private CursorVisual _current = CursorVisual.Default;
        private Position2D? _stampedTarget;   // hex currently wearing the TargetPickOutline stamp

        #endregion // Fields

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                _fallbackDenied = BuildDeniedTexture();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Awake), e);
            }
        }

        private void Update()
        {
            try
            {
                CursorVisual desired = DetermineVisual(out Position2D? legalTarget);
                if (desired != _current)
                {
                    Apply(desired);
                    _current = desired;
                }
                UpdateTargetStamp(legalTarget);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Update), e);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                _instance = null;
            }
        }

        #endregion // Unity Lifecycle

        #region Cursor State

        /// <summary>
        /// §5.10.6 semantics (amended 2026-07-22): the feedback pair exists ONLY while Ctrl (the combat
        /// modifier) is held on a live battle map; everything else is the default arrow. With Ctrl held:
        /// legal target → default arrow + TargetPickOutline stamp on the target hex (via legalTarget),
        /// anything else (no unit selected, empty hex, friendly, out of range, no action) → denied.
        /// </summary>
        private CursorVisual DetermineVisual(out Position2D? legalTarget)
        {
            legalTarget = null;

            // Input System API (the project runs Input System-only — legacy UnityEngine.Input throws).
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return CursorVisual.Default;

            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (!ctrl) return CursorVisual.Default;

            if (GameDataManager.CurrentHexMap == null) return CursorVisual.Default;   // not in a battle

            var mc = MovementController.Instance;
            if (mc == null || mc.State != MovementState.UnitSelected || mc.CurrentUnit == null)
                return CursorVisual.Denied;

            if (HexGridSystem.Instance == null || Camera.main == null)
                return CursorVisual.Denied;

            Vector2 mousePos = mouse.position.ReadValue();
            Position2D hex = HexGridSystem.Instance.ScreenToHex(new Vector3(mousePos.x, mousePos.y, 0f), Camera.main);
            if (!HexGridSystem.Instance.IsInBounds(hex))
                return CursorVisual.Denied;

            var gdm = GameDataManager.Instance;
            if (gdm == null) return CursorVisual.Denied;
            var target = gdm.GetGroundUnitAtHex(hex) ?? gdm.GetAirUnitAtHex(hex);
            if (target == null || target.Side == Side.Player)
                return CursorVisual.Denied;

            if (mc.AttackLegality(target) == null)
            {
                legalTarget = hex;
                return CursorVisual.Default;
            }
            return CursorVisual.Denied;
        }

        private void Apply(CursorVisual visual)
        {
            Vector2 center = new(TEX_SIZE / 2f, TEX_SIZE / 2f);
            switch (visual)
            {
                case CursorVisual.Denied:
                    Cursor.SetCursor(deniedCursor != null ? deniedCursor : _fallbackDenied, center, CursorMode.Auto);
                    break;
                default:
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
            }
        }

        /// <summary>
        /// Keeps the TargetPickOutline stamp in sync with the Ctrl-hover legality poll: stamps the hex when a
        /// legal target is hovered, clears it the moment there isn't one. Only re-stamps on hex CHANGE.
        /// </summary>
        private void UpdateTargetStamp(Position2D? target)
        {
            if (Nullable.Equals(target, _stampedTarget)) return;

            // No live battle map → the renderer (and any stamp) went down with the scene; just drop the
            // bookkeeping. Guard BEFORE touching the singleton so its lazy find never runs outside battle.
            if (GameDataManager.CurrentHexMap == null)
            {
                _stampedTarget = null;
                return;
            }

            var renderer = HexGridRenderer.Instance;
            if (renderer != null)
            {
                if (target.HasValue) renderer.ShowCombatTargetPick(target.Value);
                else renderer.ClearCombatTargetPick();
            }
            _stampedTarget = target;
        }

        #endregion // Cursor State

        #region Placeholder Textures

        /// <summary>Red ring with a diagonal slash — the universal "no".</summary>
        private static Texture2D BuildDeniedTexture()
        {
            var tex = NewCursorTexture();
            int c = TEX_SIZE / 2;
            float radius = TEX_SIZE / 2f - 4f;
            for (int x = 0; x < TEX_SIZE; x++)
                for (int y = 0; y < TEX_SIZE; y++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    bool ring = Mathf.Abs(d - radius) <= 1.5f;
                    bool slash = d < radius && Mathf.Abs((x - c) + (y - c)) <= 1.5f;
                    if (ring || slash) tex.SetPixel(x, y, Color.red);
                }
            tex.Apply();
            return tex;
        }

        private static Texture2D NewCursorTexture()
        {
            var tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < TEX_SIZE; x++)
                for (int y = 0; y < TEX_SIZE; y++)
                    tex.SetPixel(x, y, clear);
            return tex;
        }

        #endregion // Placeholder Textures
    }
}
