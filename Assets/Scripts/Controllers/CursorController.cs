using HammerAndSickle.Core;
using HammerAndSickle.Core.GameData;
using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HammerAndSickle.Controllers
{
    /// <summary>
    /// Live cursor feedback (§24.11.3) — the mouse cursor is an input-state readout: default arrow in normal
    /// selection, CROSSHAIR while Ctrl is held over a legal combat target, DENIED while Ctrl is held over
    /// anything illegal. Legality comes from MovementController.AttackLegality — the SAME gate the click runs,
    /// so the cursor never lies. Poll-based (no EventManager subscriptions — survives scene transitions and
    /// ClearAllSubscriptions): inert whenever no battle map is loaded. Per-mode cursors for future input modes
    /// (unit-pick §24.5.5, AOB placement §24.7a.1) slot in here as those modes land.
    /// Cursor art: assign textures on a scene instance to use real sprites; a bootstrap-created instance falls
    /// back to procedural placeholder textures so the play-test has visible feedback before art exists.
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

        private enum CursorVisual { Default, Crosshair, Denied }

        [Header("Cursor Art (optional — procedural placeholders used when empty)")]
        [SerializeField] private Texture2D crosshairCursor;
        [SerializeField] private Texture2D deniedCursor;

        private Texture2D _fallbackCrosshair;
        private Texture2D _fallbackDenied;
        private CursorVisual _current = CursorVisual.Default;

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
                _fallbackCrosshair = BuildCrosshairTexture();
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
                CursorVisual desired = DetermineVisual();
                if (desired != _current)
                {
                    Apply(desired);
                    _current = desired;
                }
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
        /// §5.10.6 semantics: the crosshair/denied pair exists ONLY while Ctrl (the combat modifier) is held on
        /// a live battle map; everything else is the default arrow. With Ctrl held: legal target → crosshair,
        /// anything else (no unit selected, empty hex, friendly, out of range, no action) → denied.
        /// </summary>
        private CursorVisual DetermineVisual()
        {
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

            return mc.AttackLegality(target) == null ? CursorVisual.Crosshair : CursorVisual.Denied;
        }

        private void Apply(CursorVisual visual)
        {
            Vector2 center = new(TEX_SIZE / 2f, TEX_SIZE / 2f);
            switch (visual)
            {
                case CursorVisual.Crosshair:
                    Cursor.SetCursor(crosshairCursor != null ? crosshairCursor : _fallbackCrosshair, center, CursorMode.Auto);
                    break;
                case CursorVisual.Denied:
                    Cursor.SetCursor(deniedCursor != null ? deniedCursor : _fallbackDenied, center, CursorMode.Auto);
                    break;
                default:
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
            }
        }

        #endregion // Cursor State

        #region Placeholder Textures

        /// <summary>White crosshair with a black outline and an open center — readable on any terrain.</summary>
        private static Texture2D BuildCrosshairTexture()
        {
            var tex = NewCursorTexture();
            int c = TEX_SIZE / 2;
            for (int i = 2; i < TEX_SIZE - 2; i++)
            {
                if (Mathf.Abs(i - c) < 4) continue;      // open center
                PlotOutlined(tex, i, c, Color.white);    // horizontal arm
                PlotOutlined(tex, c, i, Color.white);    // vertical arm
            }
            tex.Apply();
            return tex;
        }

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

        /// <summary>Plots a pixel with a 1px black outline around it (drawn first, overwritten by the core color).</summary>
        private static void PlotOutlined(Texture2D tex, int x, int y, Color core)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int px = x + dx, py = y + dy;
                    if (px < 0 || py < 0 || px >= TEX_SIZE || py >= TEX_SIZE) continue;
                    if (tex.GetPixel(px, py).a < 0.5f)
                        tex.SetPixel(px, py, Color.black);
                }
            tex.SetPixel(x, y, core);
        }

        #endregion // Placeholder Textures
    }
}
