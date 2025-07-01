using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{

   /// <summary>
   /// Represents an entry in a weapon system, associating a specific weapon system with its corresponding profile item.
   /// </summary>
   /// <remarks>This class is used to define the relationship between a weapon system and its associated
   /// profile item. It provides properties to access and modify the weapon system and profile item values.</remarks>
   public class WeaponSystemEntry
    {
        public WeaponSystems WeaponSystem { get; set; } = WeaponSystems.DEFAULT;
        public ProfileItem ProfileItem { get; set; } = ProfileItem.Default;

        public WeaponSystemEntry(WeaponSystems weaponSystem, ProfileItem profileItem = ProfileItem.Default)
        {
            WeaponSystem = weaponSystem;
            ProfileItem = profileItem;
        }

        #region Overrides

        /// <summary>
        /// Determines whether the specified object is equal to the current WeaponSystemEntry.
        /// Two entries are considered equal if they have the same WeaponSystem and ProfileItem.
        /// </summary>
        /// <param name="obj">The object to compare with the current entry</param>
        /// <returns>true if the specified object is equal to the current entry; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            return obj is WeaponSystemEntry other &&
                   WeaponSystem == other.WeaponSystem &&
                   ProfileItem == other.ProfileItem;
        }

        /// <summary>
        /// Returns a hash code for the current WeaponSystemEntry.
        /// The hash code is based on the combination of WeaponSystem and ProfileItem values.
        /// </summary>
        /// <returns>A hash code for the current entry</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(WeaponSystem, ProfileItem);
        }

        /// <summary>
        /// Determines whether two WeaponSystemEntry instances are equal.
        /// </summary>
        /// <param name="left">The first entry to compare</param>
        /// <param name="right">The second entry to compare</param>
        /// <returns>true if the entries are equal; otherwise, false</returns>
        public static bool operator ==(WeaponSystemEntry left, WeaponSystemEntry right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two WeaponSystemEntry instances are not equal.
        /// </summary>
        /// <param name="left">The first entry to compare</param>
        /// <param name="right">The second entry to compare</param>
        /// <returns>true if the entries are not equal; otherwise, false</returns>
        public static bool operator !=(WeaponSystemEntry left, WeaponSystemEntry right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns a string representation of the WeaponSystemEntry.
        /// </summary>
        /// <returns>A string in the format "WeaponSystem (ProfileItem)"</returns>
        public override string ToString()
        {
            return $"{WeaponSystem} ({ProfileItem})";
        }

        #endregion // Overrides
    }


    /*───────────────────────────────────────────────────────────────────────────────
    UnitProfile  —  organizational composition tracking and intelligence reporting
    ────────────────────────────────────────────────────────────────────────────────

    Overview
    ════════
    **UnitProfile** defines military unit organizational composition tracking men, 
    tanks, artillery, aircraft, and equipment through WeaponSystemEntry objects. 
    Provides informational data for GUI display, tracks real-time attrition based 
    on unit hit points, and generates sophisticated intelligence reports with 
    fog-of-war effects and equipment categorization.

    Major Responsibilities
    ══════════════════════
    • WeaponSystemEntry management with ProfileItem designation (Default/Deployed/Mounted)
    • Intelligence report generation with 5-level fog-of-war and 20+ equipment categories
    • Real-time strength scaling based on hit point ratios with float precision mathematics
    • Equipment upgrade support through safe weapon system replacement methods
    • Template system with comprehensive cloning capabilities and binary serialization

    Design Highlights
    ═════════════════
    • **WeaponSystemEntry Architecture**: Combines WeaponSystems enum with ProfileItem 
      to support upgrade system where deployed/mounted equipment can be independently 
      upgraded while preserving organizational structure and dictionary integrity.
    • **Automatic Scaling**: Current equipment counts calculated from hit point ratios 
      for realistic attrition representation without cumulative rounding errors.
    • **Intelligence Categorization**: 50+ weapon systems organized into intuitive 
      display categories (Men, Tanks, Artillery, SAMs, Aircraft, etc.) with independent 
      fog-of-war error application per bucket.
    • **Float Precision Processing**: Equipment counts accumulated as floats through 
      entire pipeline, rounded only at final step to prevent mathematical degradation.
    • **Upgrade System Integration**: UpdateDeployedEntry() and UpdateMountedEntry() 
      enable safe weapon system replacement while preserving quantities.

    Public-Method Reference
    ═══════════════════════
      ── Equipment Management ───────────────────────────────────────────────────────
      AddWeaponSystem(entry, maxQuantity)       Adds weapon system entry with quantity
      UpdateDeployedEntry(newWeaponSystem)       Updates deployed weapon system (upgrade)
      UpdateMountedEntry(newWeaponSystem)        Updates mounted weapon system (upgrade)

      ── Intelligence & Scaling ─────────────────────────────────────────────────────
      UpdateCurrentHP(currentHP)                Updates hit points for scaling calculations
      GenerateIntelReport(name, state, xp, eff, spotted) Creates intelligence report 
                                                 with fog-of-war and categorization

      ── Cloning & Persistence ──────────────────────────────────────────────────────
      Clone()                                   Creates identical copy with same ID
      Clone(newProfileID)                       Creates copy with new profile ID
      Clone(newProfileID, newNationality)       Creates copy with new ID and nationality

    WeaponSystemEntry Architecture
    ══════════════════════════════
    **Entry Structure**: Each WeaponSystemEntry combines WeaponSystems enum with 
    ProfileItem designation supporting complex unit configurations:

    • **Default**: Organizational equipment that remains constant across combat states
    • **Deployed**: Primary combat equipment when unit is deployed for battle  
    • **Mounted**: Transport or alternative equipment configuration for Mobile state

    **Upgrade Support**: Safe weapon system replacement preserves quantities while 
    maintaining dictionary key integrity. UpdateDeployedEntry() and UpdateMountedEntry() 
    perform atomic remove-and-replace operations for seamless equipment transitions.

    **Dictionary Management**: WeaponSystemEntry objects serve as composite keys 
    enabling units to have multiple configurations of the same weapon system type 
    (e.g., T-80B for deployed operations + BTR-80 for mounted transport).

    Intelligence System Architecture
    ════════════════════════════════
    **Spotted Level Effects** (Fog-of-War Implementation):
    • **Level 0**: Full information (player units, perfect intelligence)
    • **Level 1**: Unit name only (minimal contact, no composition data)
    • **Level 2**: Unit data with ±30% random error per weapon system and bucket
    • **Level 3**: Unit data with ±10% random error per weapon system and bucket  
    • **Level 4**: Perfect accuracy (excellent intelligence)
    • **Level 5**: Perfect accuracy + movement history (elite intelligence)

    **Equipment Categorization System**: 20+ display categories including:
    - **Personnel**: Men (all infantry types combined)
    - **Armored Vehicles**: Tanks, IFVs, APCs, Recon vehicles
    - **Artillery Systems**: Artillery, Rocket Artillery, SSMs
    - **Air Defense**: SAMs, AAA, MANPADs, ATGMs
    - **Aviation**: Attack/Transport Helicopters, Fighters, Multirole, Attack Aircraft, 
      Bombers, Transports, AWACS, Reconnaissance Aircraft

    **Processing Pipeline with Float Precision**:
    1. **Weapon System Accumulation**: Combine multiple entries of same weapon type 
       with float precision to prevent rounding errors
    2. **Strength Scaling**: Apply hit point ratio with float mathematics
    3. **Fog-of-War Application**: Independent random error per weapon system
    4. **Category Bucketing**: Group weapons into display categories with float precision  
    5. **Bucket Fog-of-War**: Independent random error per bucket category
    6. **Final Rounding**: Convert to integers only at final step
    7. **Omission Logic**: Buckets with final values < 1 excluded from report

    **Error Modeling**: Each weapon system and bucket receives independent random 
    error within spotted level bounds. Direction (positive/negative) and magnitude 
    randomly determined per item for realistic intelligence uncertainty.

    Strength Scaling & Attrition
    ═════════════════════════════
    **Automatic Attrition Calculation**: Equipment counts scale proportionally 
    with unit hit points using precise mathematical formula:
    `Current Equipment = Maximum Equipment × (Current HP / MAX_HP)`

    **Realistic Loss Representation**:
    - **100% HP**: Full equipment complement displayed
    - **75% HP**: 25% equipment losses shown across all categories  
    - **50% HP**: 50% equipment losses (moderate attrition)
    - **25% HP**: 75% equipment losses (heavy attrition)
    - **Near 0% HP**: Minimal equipment remaining (unit nearly destroyed)

    **Proportional Scaling**: All weapon systems scale uniformly representing 
    personnel casualties, vehicle losses, equipment abandonment, and operational 
    degradation throughout campaign progression.

    Template System & Persistence
    ══════════════════════════════
    **Profile Template Architecture**: Supports comprehensive template instantiation:
    - **Base Templates**: Master profiles with full equipment definitions
    - **Unit Instances**: Specific profile variants with unique identifiers
    - **Nationality Variants**: Same composition with different national equipment
    - **Campaign Persistence**: Maintains unit-specific state across scenarios

    **Clone Method Variants**:
    - **Clone()**: Exact duplication with identical ID (template copying)
    - **Clone(newProfileID)**: New identifier, same nationality (unit instantiation)  
    - **Clone(newProfileID, newNationality)**: Full parameterization (cross-national)

    **Binary Serialization**: ISerializable implementation with WeaponSystemEntry 
    decomposition into WeaponSystems + ProfileItem + quantity for reconstruction 
    integrity. Preserves all organizational data and current state across save/load.

    Upgrade System Integration
    ══════════════════════════
    **Safe Equipment Replacement**: UpdateDeployedEntry() and UpdateMountedEntry() 
    perform atomic operations maintaining dictionary integrity:

    1. **Locate Existing Entry**: Find current deployed/mounted WeaponSystemEntry
    2. **Preserve Quantity**: Extract quantity value from existing entry
    3. **Remove Old Entry**: Clean removal from dictionary  
    4. **Create New Entry**: Generate WeaponSystemEntry with new weapon system
    5. **Restore Quantity**: Apply preserved quantity to new entry
    6. **Validation**: Ensure operation success and data consistency

    **Use Cases**: Technology upgrades (T-72A → T-80B), doctrine changes (BMP-1 → BMP-2), 
    campaign progression rewards, and scenario-specific equipment modifications.

    ───────────────────────────────────────────────────────────────────────────────
    KEEP THIS COMMENT BLOCK SYNCHRONIZED WITH WEAPONSYSTEMENTRY AND UPGRADE SYSTEM!
    ───────────────────────────────────────────────────────────────────────────────*/
    [Serializable]
    public class UnitProfile : ISerializable, ICloneable
    {
        #region Constants

        private const string CLASS_NAME = nameof(UnitProfile);

        #endregion // Constants

        #region Fields

        private float currentHitPoints = CUConstants.MAX_HP;                     // Tracks current hit points for scaling.
        private readonly Dictionary<WeaponSystemEntry, int> weaponSystemEntries; // Tracks weapon system entries with max quantities.

        #endregion // Fields

        #region Properties

        public Nationality Nationality { get; private set; }              // Nationality for a profile.
        public UnitProfileTypes UnitProfileID { get; private set; }       // Unique identifier for this profile.
        public IntelReport LastIntelReport { get; private set; } = null;  // Last generated intel report for this profile.

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitProfile"/> class with the specified profile ID and
        /// nationality.
        /// </summary>
        /// <param name="profileID">The unique identifier for the unit profile, representing its type.</param>
        /// <param name="nationality">The nationality associated with the unit profile.</param>
        public UnitProfile(UnitProfileTypes profileID, Nationality nationality)
        {
            try
            {
                UnitProfileID = profileID;
                Nationality = nationality;
                weaponSystemEntries = new Dictionary<WeaponSystemEntry, int>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        #endregion // Constructors


        #region Public Methods

        /// <summary>
        /// Updates the current hit points, provided from parent unit.
        /// </summary>
        /// <param name="currentHP"></param>
        public void UpdateCurrentHP(float currentHP)
        {
            try
            {
                if (currentHP < 0 || currentHP > CUConstants.MAX_HP)
                    throw new ArgumentOutOfRangeException(nameof(currentHP), "Current HP must be between 0 and MAX_HP");

                currentHitPoints = currentHP;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "UpdateCurrentHP", e);
                throw;
            }
        }

        /// <summary>
        /// Adds a weapon system entry with specified maximum quantity to this unit profile.
        /// Each entry represents a specific weapon system in a particular role (Default/Deployed/Mounted).
        /// </summary>
        /// <param name="entry">The weapon system entry specifying both weapon type and role</param>
        /// <param name="maxQuantity">Maximum quantity of this weapon system (must be non-negative)</param>
        /// <exception cref="ArgumentNullException">Thrown when entry is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxQuantity is negative</exception>
        /// <exception cref="InvalidOperationException">Thrown when this exact weapon system and role combination already exists</exception>
        public void AddWeaponSystem(WeaponSystemEntry entry, int maxQuantity)
        {
            try
            {
                if (entry == null)
                    throw new ArgumentNullException(nameof(entry), "WeaponSystemEntry cannot be null");

                if (maxQuantity < 0)
                    throw new ArgumentOutOfRangeException(nameof(maxQuantity), "Max quantity cannot be negative");

                if (entry.WeaponSystem == WeaponSystems.DEFAULT)
                    throw new ArgumentException("Cannot add DEFAULT weapon system");

                if (!Enum.IsDefined(typeof(ProfileItem), entry.ProfileItem))
                    throw new ArgumentException("Invalid ProfileItem value");

                // Check if this exact entry already exists
                if (weaponSystemEntries.ContainsKey(entry))
                    throw new InvalidOperationException($"WeaponSystemEntry for {entry.WeaponSystem} with ProfileItem {entry.ProfileItem} already exists");

                weaponSystemEntries[entry] = maxQuantity;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(AddWeaponSystem), e);
                throw;
            }
        }

        /// <summary>
        /// Updates the deployed weapon system entry to use a different weapon system while preserving the original quantity.
        /// This method supports unit upgrades where deployed equipment can be replaced with improved versions.
        /// The unit must have an existing deployed entry for this operation to succeed.
        /// </summary>
        /// <param name="newWeaponSystem">The new weapon system to use for the deployed configuration</param>
        /// <returns>True if a deployed entry was found and successfully updated; false if no deployed entry exists</returns>
        /// <remarks>
        /// This method performs a safe remove-and-replace operation to maintain dictionary key integrity.
        /// The original quantity is preserved during the upgrade process.
        /// </remarks>
        public bool UpdateDeployedEntry(WeaponSystems newWeaponSystem)
        {
            try
            {
                // Find the existing deployed entry
                var deployedEntry = weaponSystemEntries.Keys.FirstOrDefault(entry => entry.ProfileItem == ProfileItem.Deployed);
                if (deployedEntry == null)
                {
                    return false; // No deployed entry to update
                }

                // Get the quantity from the old entry
                int quantity = weaponSystemEntries[deployedEntry];

                // Remove the old entry
                weaponSystemEntries.Remove(deployedEntry);

                // Add the new entry with the same quantity
                var newEntry = new WeaponSystemEntry(newWeaponSystem, ProfileItem.Deployed);
                weaponSystemEntries[newEntry] = quantity;

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateDeployedEntry), e);
                return false; // Indicate failure if an exception occurs
            }
        }

        /// <summary>
        /// Updates the mounted weapon system entry to use a different weapon system while preserving the original quantity.
        /// This method supports unit upgrades where mounted equipment can be replaced with improved versions.
        /// The unit must have an existing mounted entry for this operation to succeed.
        /// </summary>
        /// <param name="newWeaponSystem">The new weapon system to use for the mounted configuration</param>
        /// <returns>True if a mounted entry was found and successfully updated; false if no mounted entry exists</returns>
        /// <remarks>
        /// This method performs a safe remove-and-replace operation to maintain dictionary key integrity.
        /// The original quantity is preserved during the upgrade process.
        /// </remarks>
        public bool UpdateMountedEntry(WeaponSystems newWeaponSystem)
        {
            try
            {
                // Find the existing mounted entry
                var mountedEntry = weaponSystemEntries.Keys.FirstOrDefault(entry => entry.ProfileItem == ProfileItem.Mounted);
                if (mountedEntry == null)
                {
                    return false; // No mounted entry to update
                }

                // Get the quantity from the old entry
                int quantity = weaponSystemEntries[mountedEntry];

                // Remove the old entry
                weaponSystemEntries.Remove(mountedEntry);

                // Add the new entry with the same quantity
                var newEntry = new WeaponSystemEntry(newWeaponSystem, ProfileItem.Mounted);
                weaponSystemEntries[newEntry] = quantity;

                return true;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(UpdateMountedEntry), e);
                return false; // Indicate failure if an exception occurs
            }
        }

        #endregion // Public Methods


        #region IntelReports

        /// <summary>
        /// Generates an IntelReport object containing bucketed weapon system data and unit metadata.
        /// This provides structured data for the GUI to display unit intelligence information.
        /// Applies fog of war effects based on spotted level for AI units.
        /// Reports all weapon systems in the profile regardless of their ProfileItem designation.
        /// Buckets with values less than 1 are omitted from the final report.
        /// </summary>
        /// <param name="unitName">Display name of the unit</param>
        /// <param name="combatState">Current combat state of the unit</param>
        /// <param name="xpLevel">Experience level of the unit</param>
        /// <param name="effLevel">Efficiency level of the unit</param>
        /// <param name="spottedLevel">Intelligence level for AI units (default Level0 for player units)</param>
        /// <returns>IntelReport object with categorized weapon data and unit information</returns>
        public IntelReport GenerateIntelReport(string unitName, CombatState combatState, ExperienceLevel xpLevel, EfficiencyLevel effLevel, SpottedLevel spottedLevel = SpottedLevel.Level0)
        {
            try
            {
                // Create the intel report object
                var intelReport = new IntelReport();

                // Set unit metadata
                intelReport.UnitNationality = Nationality;
                intelReport.UnitName = unitName;
                intelReport.UnitState = combatState;
                intelReport.UnitExperienceLevel = xpLevel;
                intelReport.UnitEfficiencyLevel = effLevel;

                // Handle special spotted levels
                if (spottedLevel == SpottedLevel.Level1)
                {
                    // Level1: Only unit name is visible, skip all calculations
                    return intelReport;
                }

                // Calculate multiplier for current strength, guard against divide by zero.
                float safeHitPoints = currentHitPoints;
                if (safeHitPoints <= 0) safeHitPoints = 1;
                float currentMultiplier = safeHitPoints / CUConstants.MAX_HP;

                // Step 1: Accumulate weapon systems by type with float precision
                var weaponSystemAccumulators = new Dictionary<WeaponSystems, float>();

                foreach (var kvp in weaponSystemEntries)
                {
                    WeaponSystems weaponSystem = kvp.Key.WeaponSystem;
                    int maxQuantity = kvp.Value; // Safe - captured from iteration
                    float scaledValue = maxQuantity * currentMultiplier;

                    // Accumulate multiple entries for same weapon system (float precision)
                    if (weaponSystemAccumulators.ContainsKey(weaponSystem))
                    {
                        weaponSystemAccumulators[weaponSystem] += scaledValue;
                    }
                    else if (scaledValue > 0f)
                    {
                        weaponSystemAccumulators[weaponSystem] = scaledValue;
                    }
                }

                // Step 2: Categorize weapons into buckets with float precision
                var bucketAccumulators = new Dictionary<string, float>();

                foreach (var kvp in weaponSystemAccumulators)
                {
                    WeaponSystems weaponSystem = kvp.Key;
                    float weaponCount = kvp.Value;

                    string prefix = GetWeaponSystemPrefix(weaponSystem);
                    string bucketName = MapPrefixToBucket(prefix);

                    if (bucketName != null && weaponCount > 0f)
                    {
                        // Accumulate in bucket with float precision
                        if (bucketAccumulators.ContainsKey(bucketName))
                        {
                            bucketAccumulators[bucketName] += weaponCount;
                        }
                        else
                        {
                            bucketAccumulators[bucketName] = weaponCount;
                        }
                    }
                }

                // Step 3: Apply fog of war to buckets, round to final integer values, and omit buckets < 1
                foreach (var bucketKvp in bucketAccumulators)
                {
                    string bucketName = bucketKvp.Key;
                    float accumulatedValue = bucketKvp.Value;

                    // Calculate fog of war multiplier for this bucket
                    float bucketMultiplier = 1f;
                    if (spottedLevel == SpottedLevel.Level2 || spottedLevel == SpottedLevel.Level3)
                    {
                        // Each bucket gets its own independent fog-of-war direction and magnitude
                        bool isPositiveDirection = UnityEngine.Random.Range(0f, 1f) >= 0.5f;
                        float errorRangeMin = 1f;
                        float errorRangeMax = spottedLevel == SpottedLevel.Level2 ? 30f : 10f;
                        float errorPercent = UnityEngine.Random.Range(errorRangeMin, errorRangeMax);
                        bucketMultiplier = isPositiveDirection ? (1f + errorPercent / 100f) : (1f - errorPercent / 100f);
                    }

                    // Apply fog of war and round ONLY at the final step
                    int finalValue = (int)Math.Round(accumulatedValue * bucketMultiplier);

                    // Only assign non-zero values - omit buckets with values < 1
                    if (finalValue > 0)
                    {
                        // Assign to appropriate IntelReport property
                        switch (bucketName)
                        {
                            case "Men":
                                intelReport.Men = finalValue;
                                break;
                            case "Tanks":
                                intelReport.Tanks = finalValue;
                                break;
                            case "IFVs":
                                intelReport.IFVs = finalValue;
                                break;
                            case "APCs":
                                intelReport.APCs = finalValue;
                                break;
                            case "Recon":
                                intelReport.RCNs = finalValue;
                                break;
                            case "Artillery":
                                intelReport.ARTs = finalValue;
                                break;
                            case "Rocket Artillery":
                                intelReport.ROCs = finalValue;
                                break;
                            case "Surface To Surface Missiles":
                                intelReport.SSMs = finalValue;
                                break;
                            case "SAMs":
                                intelReport.SAMs = finalValue;
                                break;
                            case "Anti-aircraft Artillery":
                                intelReport.AAAs = finalValue;
                                break;
                            case "MANPADs":
                                intelReport.MANPADs = finalValue;
                                break;
                            case "ATGMs":
                                intelReport.ATGMs = finalValue;
                                break;
                            case "Attack Helicopters":
                                intelReport.HEL = finalValue;
                                break;
                            case "Transport Helicopters":
                                intelReport.HELTRAN = finalValue;
                                break;
                            case "Fighters":
                                intelReport.ASFs = finalValue;
                                break;
                            case "Multirole":
                                intelReport.MRFs = finalValue;
                                break;
                            case "Attack":
                                intelReport.ATTs = finalValue;
                                break;
                            case "Bombers":
                                intelReport.BMBs = finalValue;
                                break;
                            case "Transports":
                                intelReport.TRANs = finalValue;
                                break;
                            case "AWACS":
                                intelReport.AWACS = finalValue;
                                break;
                            case "Recon Aircraft":
                                intelReport.RCNAs = finalValue;
                                break;
                        }
                    }
                    // If finalValue <= 0, the bucket is omitted from the report
                }

                LastIntelReport = intelReport; // Store for later use
                return intelReport;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GenerateIntelReport), e);
                throw;
            }
        }

        /// <summary>
        /// Extracts the prefix from the name of a weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system whose name prefix is to be extracted.</param>
        /// <returns>A string containing the prefix of the weapon system's name. If the name does not contain an underscore, the
        /// entire name of the weapon system is returned.</returns>
        private string GetWeaponSystemPrefix(WeaponSystems weaponSystem)
        {
            string weaponName = weaponSystem.ToString();
            int underscoreIndex = weaponName.IndexOf('_');
            return underscoreIndex >= 0 ? weaponName.Substring(0, underscoreIndex) : weaponName;
        }

        /// <summary>
        /// Maps a given prefix to its corresponding bucket category.
        /// </summary>
        /// <remarks>The method uses a predefined mapping of prefixes to bucket categories. For example:
        /// <list type="bullet"> <item><description>Prefixes such as "REG", "AB", and "AM" map to the "Men"
        /// category.</description></item> <item><description>"TANK" maps to "Tanks".</description></item>
        /// <item><description>Prefixes like "SAM" and "SPSAM" map to "SAMs".</description></item> </list> If the prefix
        /// does not match any of the predefined mappings, the method returns <see langword="null"/>.</remarks>
        /// <param name="prefix">The prefix representing a specific category. This value determines the bucket to which it is mapped.</param>
        /// <returns>A string representing the bucket category corresponding to the provided prefix.  Returns <see
        /// langword="null"/> if the prefix does not match any known category.</returns>
        private string MapPrefixToBucket(string prefix)
        {
            return prefix switch
            {
                "REG" or "AB" or "AM" or "MAR" or "SPEC" or "ENG" => "Men",
                "TANK" => "Tanks",
                "IFV" => "IFVs",
                "APC" => "APCs",
                "RCN" => "Recon",
                "ART" or "SPA" => "Artillery",
                "ROC" => "Rocket Artillery",
                "SSM" => "Surface To Surface Missiles",
                "SAM" or "SPSAM" => "SAMs",
                "AAA" or "SPAAA" => "Anti-aircraft Artillery",
                "MANPAD" => "MANPADs",
                "ATGM" => "ATGMs",
                "HEL" => "Attack Helicopters",
                "HELTRAN" => "Transport Helicopters",
                "ASF" => "Fighters",
                "MRF" => "Multirole",
                "ATT" => "Attack",
                "BMB" => "Bombers",
                "TRAN" => "Transports",
                "AWACS" => "AWACS",
                "RCNA" => "Recon Aircraft",
                _ => null
            };
        }

        #endregion // IntelReports


        #region ISerializable Implementation

        /// <summary>
        /// Deserialization constructor for loading UnitProfile from saved data.
        /// Reconstructs the object state from serialized information.
        /// </summary>
        /// <param name="info">SerializationInfo containing the serialized data</param>
        /// <param name="context">StreamingContext for deserialization</param>
        protected UnitProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                UnitProfileID = (UnitProfileTypes)info.GetValue(nameof(UnitProfileID), typeof(UnitProfileTypes));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                currentHitPoints = info.GetSingle(nameof(currentHitPoints));

                // Initialize weapon system entries dictionary
                weaponSystemEntries = new Dictionary<WeaponSystemEntry, int>();

                // Deserialize weapon system entries
                int entryCount = info.GetInt32("WeaponSystemEntryCount");
                for (int i = 0; i < entryCount; i++)
                {
                    WeaponSystems weaponSystem = (WeaponSystems)info.GetValue($"WeaponSystem_{i}", typeof(WeaponSystems));
                    ProfileItem profileItem = (ProfileItem)info.GetValue($"ProfileItem_{i}", typeof(ProfileItem));
                    int quantity = info.GetInt32($"Quantity_{i}");

                    var entry = new WeaponSystemEntry(weaponSystem, profileItem);
                    weaponSystemEntries[entry] = quantity;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Serializes the UnitProfile object data for persistence.
        /// Stores all essential state information required to reconstruct the object.
        /// </summary>
        /// <param name="info">SerializationInfo to store the data</param>
        /// <param name="context">StreamingContext for serialization</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                info.AddValue(nameof(UnitProfileID), UnitProfileID);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(currentHitPoints), currentHitPoints);

                // Serialize weapon system entries
                info.AddValue("WeaponSystemEntryCount", weaponSystemEntries.Count);

                int index = 0;
                foreach (var kvp in weaponSystemEntries)
                {
                    info.AddValue($"WeaponSystem_{index}", kvp.Key.WeaponSystem);
                    info.AddValue($"ProfileItem_{index}", kvp.Key.ProfileItem);
                    info.AddValue($"Quantity_{index}", kvp.Value);
                    index++;
                }
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetObjectData), e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region ICloneable Implementation

        /// <summary>
        /// Creates a deep copy of this UnitProfile with identical properties and weapon systems.
        /// Used for template duplication where exact copies are needed.
        /// </summary>
        /// <returns>A new UnitProfile with identical configuration</returns>
        public object Clone()
        {
            try
            {
                return Clone(UnitProfileID, Nationality);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Clone), e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile with a new profile ID.
        /// Preserves nationality and all weapon system configurations while providing a unique identifier.
        /// </summary>
        /// <param name="newProfileID">The new profile ID for the cloned profile</param>
        /// <returns>A new UnitProfile with identical weapon systems but different ID</returns>
        public UnitProfile Clone(UnitProfileTypes newProfileID)
        {
            try
            {
                return Clone(newProfileID, Nationality);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone with ProfileID", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile with a new profile ID and nationality.
        /// Provides complete customization while preserving all weapon system configurations.
        /// Used for creating cross-national variants or scenario-specific unit templates.
        /// </summary>
        /// <param name="newProfileID">The new profile ID for the cloned profile</param>
        /// <param name="newNationality">The new nationality for the cloned profile</param>
        /// <returns>A new UnitProfile with identical weapon systems but different ID and nationality</returns>
        public UnitProfile Clone(UnitProfileTypes newProfileID, Nationality newNationality)
        {
            try
            {
                // Create new profile with specified parameters
                var clonedProfile = new UnitProfile(newProfileID, newNationality);

                // Copy current hit points
                clonedProfile.currentHitPoints = this.currentHitPoints;

                // Deep copy all weapon system entries
                foreach (var kvp in this.weaponSystemEntries)
                {
                    var originalEntry = kvp.Key;
                    int quantity = kvp.Value;

                    // Create new WeaponSystemEntry with same configuration
                    var clonedEntry = new WeaponSystemEntry(originalEntry.WeaponSystem, originalEntry.ProfileItem);
                    clonedProfile.weaponSystemEntries[clonedEntry] = quantity;
                }

                return clonedProfile;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone with ProfileID and Nationality", e);
                throw;
            }
        }

        #endregion // ICloneable Implementation        
    }
}