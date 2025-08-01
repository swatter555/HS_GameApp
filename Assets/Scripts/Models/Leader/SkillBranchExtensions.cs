using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HammerAndSickle.Models
{
    public static class SkillBranchExtensions
    {
        #region Private Fields

        // Cache for performance - built once on first access using reflection
        private static readonly Dictionary<SkillBranch, BranchType> _branchTypeCache = new Dictionary<SkillBranch, BranchType>();
        private static readonly object _cacheLock = new object();
        private static bool _cacheInitialized = false;

        #endregion


        #region Cache Management

        /// <summary>
        /// Initialize the branch type cache using reflection on the SkillBranch enum.
        /// This runs once and caches all branch type information for fast lookup.
        /// Thread-safe with double-checked locking pattern.
        /// </summary>
        private static void InitializeBranchTypeCache()
        {
            if (_cacheInitialized) return;

            lock (_cacheLock)
            {
                if (_cacheInitialized) return; // Double-check after acquiring lock

                try
                {
                    var enumType = typeof(SkillBranch);

                    foreach (SkillBranch branch in Enum.GetValues(typeof(SkillBranch)).Cast<SkillBranch>())
                    {
                        if (branch == SkillBranch.None)
                        {
                            _branchTypeCache[branch] = BranchType.Foundation; // Default for None
                            continue;
                        }

                        // Use reflection to get the BranchTypeAttribute for this enum value
                        var fieldInfo = enumType.GetField(branch.ToString());
                        var attribute = fieldInfo?.GetCustomAttribute<BranchTypeAttribute>();

                        if (attribute != null)
                        {
                            _branchTypeCache[branch] = attribute.Type;
                        }
                        else
                        {
                            // Fallback: log warning and default to Foundation to prevent crashes
                            Debug.LogWarning($"SkillBranch.{branch} missing BranchTypeAttribute, defaulting to Foundation");
                            _branchTypeCache[branch] = BranchType.Foundation;
                        }
                    }

                    _cacheInitialized = true;

                    // Log cache initialization in debug builds
                    #if UNITY_EDITOR || DEBUG
                    Debug.Log($"SkillBranchExtensions: Initialized cache with {_branchTypeCache.Count} branches");
                    #endif
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SkillBranchExtensions: Failed to initialize branch type cache: {ex.Message}");
                    // Ensure we don't get stuck with partially initialized cache
                    _branchTypeCache.Clear();
                    throw;
                }
            }
        }

        /// <summary>
        /// Clears the internal cache. Only needed if branches are modified at runtime (very unlikely).
        /// Only available in editor builds.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _branchTypeCache.Clear();
                _cacheInitialized = false;
                Debug.Log("SkillBranchExtensions: Cache cleared");
            }
        }

        #endregion


        #region Core Extension Methods

        /// <summary>
        /// Gets the branch type for a given skill branch using cached reflection data
        /// </summary>
        /// <param name="branch">The skill branch to classify</param>
        /// <returns>The branch type (Foundation, Doctrine, or Specialization)</returns>
        public static BranchType GetBranchType(this SkillBranch branch)
        {
            InitializeBranchTypeCache();
            return _branchTypeCache.TryGetValue(branch, out var type) ? type : BranchType.Foundation;
        }

        /// <summary>
        /// Returns true when the branch is a Foundation (can stack with others).
        /// Foundation branches like Leadership and Political can be taken together.
        /// </summary>
        public static bool IsFoundation(this SkillBranch branch)
        {
            return branch.GetBranchType() == BranchType.Foundation;
        }

        /// <summary>
        /// Returns true when the branch is a Doctrine (mutually exclusive group).
        /// Only one doctrine branch can be started per leader.
        /// </summary>
        public static bool IsDoctrine(this SkillBranch branch)
        {
            return branch.GetBranchType() == BranchType.Doctrine;
        }

        /// <summary>
        /// Returns true when the branch is a Specialization (mutually exclusive group).
        /// Only one specialization branch can be started per leader.
        /// </summary>
        public static bool IsSpecialization(this SkillBranch branch)
        {
            return branch.GetBranchType() == BranchType.Specialization;
        }

        #endregion


        #region Query Methods

        /// <summary>
        /// Gets all branches of a specific type.
        /// Useful for UI systems that need to group branches by category.
        /// </summary>
        /// <param name="branchType">The type of branches to retrieve</param>
        /// <returns>Collection of branches matching the type</returns>
        public static IEnumerable<SkillBranch> GetBranchesByType(BranchType branchType)
        {
            InitializeBranchTypeCache();
            return _branchTypeCache.Where(kvp => kvp.Value == branchType && kvp.Key != SkillBranch.None)
                                  .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Gets count of branches by type for validation or UI display
        /// </summary>
        /// <param name="branchType">The branch type to count</param>
        /// <returns>Number of branches of that type</returns>
        public static int GetBranchCountByType(BranchType branchType)
        {
            return GetBranchesByType(branchType).Count();
        }

        /// <summary>
        /// Gets all foundation branches (Leadership, Political)
        /// </summary>
        public static IEnumerable<SkillBranch> GetFoundationBranches()
        {
            return GetBranchesByType(BranchType.Foundation);
        }

        /// <summary>
        /// Gets all doctrine branches (combat specializations)
        /// </summary>
        public static IEnumerable<SkillBranch> GetDoctrineBranches()
        {
            return GetBranchesByType(BranchType.Doctrine);
        }

        /// <summary>
        /// Gets all specialization branches (advanced capabilities)
        /// </summary>
        public static IEnumerable<SkillBranch> GetSpecializationBranches()
        {
            return GetBranchesByType(BranchType.Specialization);
        }

        #endregion


        #region Validation Methods

        /// <summary>
        /// Validates that the branch classification system is properly configured.
        /// Call this in editor/debug builds to ensure all branches have proper attributes.
        /// Will log warnings for any missing or unexpected configurations.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void ValidateBranchClassification()
        {
            InitializeBranchTypeCache();

            var foundationBranches = GetFoundationBranches().ToList();
            var doctrineBranches = GetDoctrineBranches().ToList();
            var specializationBranches = GetSpecializationBranches().ToList();

            Debug.Log($"SkillBranch Classification Summary:");
            Debug.Log($"  Foundation: {foundationBranches.Count} branches - {string.Join(", ", foundationBranches)}");
            Debug.Log($"  Doctrine: {doctrineBranches.Count} branches - {string.Join(", ", doctrineBranches)}");
            Debug.Log($"  Specialization: {specializationBranches.Count} branches - {string.Join(", ", specializationBranches)}");

            // Validate expected counts based on current design
            const int EXPECTED_FOUNDATION = 2;    // Leadership, Political
            const int EXPECTED_DOCTRINE = 7;      // Armored, INF, Artillery, AirDefense, Airborne, AirMobile, Intelligence
            const int EXPECTED_SPECIALIZATION = 4; // CombinedArms, SignalIntel, Engineering, SpecialForces

            bool hasErrors = false;

            if (foundationBranches.Count != EXPECTED_FOUNDATION)
            {
                Debug.LogWarning($"Expected {EXPECTED_FOUNDATION} Foundation branches, found {foundationBranches.Count}");
                hasErrors = true;
            }

            if (doctrineBranches.Count != EXPECTED_DOCTRINE)
            {
                Debug.LogWarning($"Expected {EXPECTED_DOCTRINE} Doctrine branches, found {doctrineBranches.Count}");
                hasErrors = true;
            }

            if (specializationBranches.Count != EXPECTED_SPECIALIZATION)
            {
                Debug.LogWarning($"Expected {EXPECTED_SPECIALIZATION} Specialization branches, found {specializationBranches.Count}");
                hasErrors = true;
            }

            // Check for any branches that might be missing attributes
            var totalClassified = foundationBranches.Count + doctrineBranches.Count + specializationBranches.Count;
            var totalEnumValues = Enum.GetValues(typeof(SkillBranch)).Length - 1;

            if (totalClassified != totalEnumValues)
            {
                Debug.LogError($"Branch classification mismatch: {totalClassified} classified vs {totalEnumValues} total enum values");
                hasErrors = true;
            }

            if (!hasErrors)
            {
                Debug.Log("✓ SkillBranch classification validation passed!");
            }
        }

        #endregion
    }
}