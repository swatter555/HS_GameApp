using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HammerAndSickle.EditorTools.UI
{
    /// <summary>
    /// Editor-only checks for the Inspector-owns-the-wiring rule ratified 2026-07-27 (Claude_Project §3.6b,
    /// CLAUDE.md §2.13). Since code no longer calls onClick.AddListener anywhere, a button's behaviour lives
    /// entirely in scene data — which the compiler cannot see. These two commands are the detection half of
    /// that trade:
    ///
    ///   Tools/UI/Audit Button Wiring          — buttons that are broken or do nothing
    ///   Tools/UI/Find Unwired Button Callbacks — callbacks that nothing wires
    ///
    /// Both are on-demand, Editor-only, and never ship.
    /// </summary>
    public static class ButtonWiringAudit
    {
        #region Constants

        private const string CLASS_NAME = nameof(ButtonWiringAudit);

        // A UnityEvent persistent call is stored in the scene/prefab YAML as "m_MethodName: Foo".
        // Asset Serialization is ForceText (ProjectSettings/EditorSettings.asset, m_SerializationMode: 2),
        // so this is readable without opening a single scene.
        private static readonly Regex MethodNameInYaml = new(@"m_MethodName:\s*(\w+)", RegexOptions.Compiled);

        // The naming convention for an Inspector-wired callback: OnEndTurnButton, OnPrinterNextButton, …
        private const string CALLBACK_PREFIX = "On";
        private const string CALLBACK_SUFFIX = "Button";

        #endregion // Constants

        #region Audit — buttons that are broken or do nothing

        /// <summary>
        /// Walks every Button in the currently OPEN scenes (inactive objects included) and reports the four
        /// ways Inspector wiring fails silently: nothing wired, a method that no longer exists, a listener
        /// switched Off, and the same method wired twice.
        /// Every finding is logged with the Button as the log context, so clicking the console entry pings
        /// the offending object in the hierarchy.
        /// </summary>
        [MenuItem("Tools/UI/Audit Button Wiring")]
        public static void AuditOpenScenes()
        {
            try
            {
                int buttonCount = 0;
                int findingCount = 0;
                var scenesAudited = new List<string>();

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    scenesAudited.Add(scene.name);

                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        // includeInactive: a button on a hidden panel is still wired (or not).
                        foreach (Button button in root.GetComponentsInChildren<Button>(true))
                        {
                            buttonCount++;
                            findingCount += AuditButton(button);
                        }
                    }
                }

                if (scenesAudited.Count == 0)
                {
                    Debug.LogWarning($"{CLASS_NAME}: no loaded scenes to audit.");
                    return;
                }

                string scenes = string.Join(", ", scenesAudited);
                string summary = $"{CLASS_NAME}: audited {buttonCount} button(s) across {scenes} — " +
                                 $"{findingCount} finding(s).";

                if (findingCount == 0) Debug.Log(summary);
                else Debug.LogWarning(summary + " See the warnings above; click one to select the button.");
            }
            catch (Exception e)
            {
                Debug.LogError($"{CLASS_NAME}.{nameof(AuditOpenScenes)} failed: {e}");
            }
        }

        /// <summary>
        /// Checks one Button and returns the number of findings, logging each against the button itself.
        /// </summary>
        private static int AuditButton(Button button)
        {
            string path = HierarchyPath(button.transform);
            int listenerCount = button.onClick.GetPersistentEventCount();

            // (1) Nothing wired at all. The single most likely failure now that the Inspector owns wiring.
            if (listenerCount == 0)
            {
                Debug.LogWarning($"{CLASS_NAME}: '{path}' has NO onClick listeners — clicking it does nothing.", button);
                return 1;
            }

            int findings = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < listenerCount; i++)
            {
                UnityEngine.Object target = button.onClick.GetPersistentTarget(i);
                string method = button.onClick.GetPersistentMethodName(i);

                if (target == null)
                {
                    Debug.LogWarning($"{CLASS_NAME}: '{path}' listener {i} has NO TARGET OBJECT — the object it " +
                                     "pointed at was deleted or unassigned.", button);
                    findings++;
                    continue;
                }

                if (string.IsNullOrEmpty(method))
                {
                    Debug.LogWarning($"{CLASS_NAME}: '{path}' listener {i} targets {target.GetType().Name} but has " +
                                     "NO METHOD SELECTED.", button);
                    findings++;
                    continue;
                }

                // (2) The rename detector — the reason this tool exists. A UnityEvent binds by method-name
                // STRING, so renaming a callback in code kills the wiring with no compile error.
                if (!HasPublicMethod(target.GetType(), method))
                {
                    Debug.LogWarning($"{CLASS_NAME}: '{path}' is wired to {target.GetType().Name}.{method}(), which " +
                                     "NO LONGER EXISTS — renamed or deleted in code. The button is dead.", button);
                    findings++;
                }

                // (3) Wired but switched off.
                if (button.onClick.GetPersistentListenerState(i) == UnityEventCallState.Off)
                {
                    Debug.LogWarning($"{CLASS_NAME}: '{path}' listener {i} ({method}) is set to OFF and will never " +
                                     "fire.", button);
                    findings++;
                }

                // (4) Same target+method twice — fires twice per press. This project has eaten a double-fire
                // before, which is exactly why one wiring mechanism replaced the old split.
                if (!seen.Add($"{target.GetInstanceID()}.{method}"))
                {
                    Debug.LogWarning($"{CLASS_NAME}: '{path}' wires {method}() MORE THAN ONCE — it will fire once " +
                                     "per duplicate on every press.", button);
                    findings++;
                }
            }

            return findings;
        }

        #endregion // Audit

        #region Reverse check — callbacks nothing wires

        /// <summary>
        /// Reports public On*Button() methods that no scene or prefab references. These have no callers in
        /// code by design, so they read as dead to a cleanup pass — this is what proves they are not.
        ///
        /// Works by scanning the scene/prefab YAML for persistent-call method names rather than opening
        /// scenes, so it covers the WHOLE project without disturbing what is currently open.
        /// </summary>
        [MenuItem("Tools/UI/Find Unwired Button Callbacks")]
        public static void FindUnwiredCallbacks()
        {
            try
            {
                HashSet<string> wired = CollectWiredMethodNames(out int filesScanned);
                List<MethodInfo> callbacks = FindCallbackMethods();

                var unwired = callbacks
                    .Where(m => !wired.Contains(m.Name))
                    .OrderBy(m => m.DeclaringType?.Name)
                    .ThenBy(m => m.Name)
                    .ToList();

                foreach (MethodInfo m in unwired)
                {
                    Debug.LogWarning($"{CLASS_NAME}: {m.DeclaringType?.Name}.{m.Name}() is NOT WIRED to any button " +
                                     "in any scene or prefab. Either a button is missing its onClick, or the " +
                                     "callback is genuinely dead and can go.");
                }

                string summary = $"{CLASS_NAME}: {callbacks.Count} callback(s) matching {CALLBACK_PREFIX}*" +
                                 $"{CALLBACK_SUFFIX}(), {wired.Count} distinct method name(s) wired across " +
                                 $"{filesScanned} scene/prefab file(s) — {unwired.Count} unwired.";

                if (unwired.Count == 0) Debug.Log(summary);
                else Debug.LogWarning(summary);

                // Stated plainly because both limits can produce a misleading result:
                Debug.Log($"{CLASS_NAME}: note — the scan matches by METHOD NAME only, so two classes sharing a " +
                          "callback name are indistinguishable, and a name wired on the WRONG object still counts " +
                          "as wired. It also cannot see wiring created at runtime, which the §3.6b rule forbids " +
                          "anyway.");
            }
            catch (Exception e)
            {
                Debug.LogError($"{CLASS_NAME}.{nameof(FindUnwiredCallbacks)} failed: {e}");
            }
        }

        /// <summary>
        /// Every method name referenced by a persistent UnityEvent call anywhere under Assets/.
        /// </summary>
        private static HashSet<string> CollectWiredMethodNames(out int filesScanned)
        {
            var wired = new HashSet<string>(StringComparer.Ordinal);

            string[] files = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories))
                .ToArray();

            foreach (string file in files)
            {
                foreach (Match match in MethodNameInYaml.Matches(File.ReadAllText(file)))
                {
                    wired.Add(match.Groups[1].Value);
                }
            }

            filesScanned = files.Length;
            return wired;
        }

        /// <summary>
        /// Public instance methods named On*Button() on any HammerAndSickle type — the callback convention.
        /// </summary>
        private static List<MethodInfo> FindCallbackMethods()
        {
            var results = new List<MethodInfo>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in SafeGetTypes(assembly))
                {
                    if (type.Namespace == null || !type.Namespace.StartsWith("HammerAndSickle", StringComparison.Ordinal))
                        continue;

                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (method.Name.StartsWith(CALLBACK_PREFIX, StringComparison.Ordinal)
                            && method.Name.EndsWith(CALLBACK_SUFFIX, StringComparison.Ordinal))
                        {
                            results.Add(method);
                        }
                    }
                }
            }

            return results;
        }

        #endregion // Reverse check

        #region Helpers

        /// <summary>Any public instance method of that name, regardless of signature (property setters included).</summary>
        private static bool HasPublicMethod(Type type, string methodName) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Any(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));

        /// <summary>A type list that survives assemblies which cannot fully load.</summary>
        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
            catch (Exception)
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>Slash-separated hierarchy path, so a finding names something findable.</summary>
        private static string HierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform t = transform; t != null; t = t.parent)
            {
                parts.Push(t.name);
            }
            return string.Join("/", parts);
        }

        #endregion // Helpers
    }
}
