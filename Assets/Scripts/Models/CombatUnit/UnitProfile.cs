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
            return Equals(left, right);
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
**UnitProfile** defines military unit organizational composition: men, tanks, 
artillery, aircraft, and equipment. Provides informational data for GUI display 
and tracks attrition throughout campaigns. Automatically scales current strength 
based on unit hit points and generates intelligence reports with fog-of-war effects.

Major Responsibilities
══════════════════════
• WeaponSystemEntry management with ProfileItem designation (Default/Deployed/Mounted)
• Intelligence report generation with 5-level fog-of-war and equipment categorization
• Real-time strength calculation based on hit point ratios with float precision
• Template system support with deep cloning and binary serialization

Design Highlights
═════════════════
• **WeaponSystemEntry Architecture**: Combines WeaponSystems enum with ProfileItem 
  to support unit upgrade system where deployed/mounted equipment can be upgraded 
  independently while preserving organizational structure.
• **Automatic Scaling**: Current equipment counts calculated from hit point ratios 
  for realistic attrition representation.
• **Intelligence Categorization**: 50+ weapon systems organized into intuitive 
  display categories (Men, Tanks, Artillery, etc.) with independent fog-of-war per bucket.
• **Float Precision**: Accumulates equipment counts as floats, rounds only at final 
  step to prevent cumulative rounding errors in large formations.

Public-Method Reference
═══════════════════════
  ── Equipment Management ───────────────────────────────────────────────────────
  AddWeaponSystem(entry, maxQuantity)       Adds weapon system entry with quantity.
  UpdateDeployedEntry(newWeaponSystem)       Updates deployed weapon system (upgrade).
  UpdateMountedEntry(newWeaponSystem)        Updates mounted weapon system (upgrade).

  ── Intelligence & Scaling ─────────────────────────────────────────────────────
  UpdateCurrentHP(currentHP)                Updates hit points for scaling calculations.
  GenerateIntelReport(name, state, xp, eff, spotted) Creates intelligence report 
                                             with fog-of-war and categorization.

  ── Cloning & Persistence ──────────────────────────────────────────────────────
  Clone()                                   Creates identical copy.
  Clone(newProfileID)                       Creates copy with new profile ID.
  Clone(newProfileID, newNationality)       Creates copy with new ID and nationality.

WeaponSystemEntry Architecture
══════════════════════════════
**Entry Structure**: Each entry combines WeaponSystems enum with ProfileItem designation:
• **Default**: Organizational equipment that doesn't change between combat states
• **Deployed**: Primary combat equipment when unit is deployed for battle  
• **Mounted**: Transport or alternative equipment configuration for Mobile state

**Upgrade Support**: UpdateDeployedEntry() and UpdateMountedEntry() enable safe weapon 
system replacement while preserving quantities and dictionary integrity.

Intelligence System
═══════════════════
**Spotted Level Effects**:
• Level 0: Full information (player units)
• Level 1: Unit name only  
• Level 2: Unit data with ±30% random error per bucket/weapon
• Level 3: Unit data with ±10% random error per bucket/weapon
• Level 4: Perfect accuracy
• Level 5: Perfect accuracy + movement history

**Equipment Categorization**: 20+ categories including Men (all infantry types), 
Tanks, IFVs, APCs, Artillery, SAMs, Aircraft, etc. Each bucket receives independent 
fog-of-war treatment. Buckets with final values < 1 are omitted from reports.

**Processing Pipeline**:
1. Accumulate weapon systems with float precision
2. Apply independent fog-of-war per weapon system (detailed data)
3. Categorize into buckets with float precision  
4. Apply independent fog-of-war per bucket and round to integers
5. Omit buckets with values < 1

Strength Scaling & Templates
════════════════════════════
**Automatic Attrition**: Equipment counts scale proportionally with hit points:
`Current Equipment = Maximum Equipment × (Current HP / 40)`

**Template System**: Supports base templates, unit instances, nationality variants, 
and campaign persistence through comprehensive cloning with parameter overrides.

**Serialization**: Binary serialization with ISerializable. WeaponSystemEntry objects 
decomposed into WeaponSystems + ProfileItem + quantity for reconstruction integrity.

───────────────────────────────────────────────────────────────────────────────
KEEP THIS COMMENT BLOCK IN SYNC WITH WEAPONSYSTEMENTRY AND UPGRADE CHANGES!
───────────────────────────────────────────────────────────────────────────── */
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
                throw;
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
                throw;
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

                // Calculate multiplier for current strength
                float currentMultiplier = currentHitPoints / CUConstants.MAX_HP;

                // Step 1: Accumulate weapon systems by type with float precision
                var weaponSystemAccumulators = new Dictionary<WeaponSystems, float>();

                foreach (var weaponSystemEntry in weaponSystemEntries)
                {
                    WeaponSystems weaponSystem = weaponSystemEntry.Key.WeaponSystem;
                    int maxQuantity = weaponSystemEntry.Value;
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