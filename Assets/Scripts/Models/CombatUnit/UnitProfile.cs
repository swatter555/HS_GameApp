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
    }
 


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
        /// Constructor for creating a new UnitProfile
        /// </summary>
        /// <param name="profileID">UntiProfileTypes</param>
        /// <param name="nationality">Nationality</param>
        /// <param name="deployed">Depolyed WeaponSystemProfile</param>
        /// <param name="mounted">Mounted WeaponSystemProfile</param>
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

                // Calculate current values for each weapon system and populate detailed data
                var currentValues = new Dictionary<WeaponSystems, int>();
                foreach (var weaponSystemEntry in weaponSystemEntries)
                {
                    WeaponSystems weaponSystem = weaponSystemEntry.Key.WeaponSystem;
                    int maxQuantity = weaponSystemEntry.Value;

                    float scaledValue = maxQuantity * currentMultiplier;
                    int currentValue = (int)Math.Round(scaledValue);

                    // Add to current values (accumulate if multiple entries for same weapon system)
                    if (currentValues.ContainsKey(weaponSystem))
                    {
                        currentValues[weaponSystem] += currentValue;
                    }
                    else if (currentValue > 0)
                    {
                        currentValues[weaponSystem] = currentValue;
                    }

                    // Add to detailed data (accumulate if multiple entries for same weapon system)
                    if (intelReport.DetailedWeaponSystemsData.ContainsKey(weaponSystem))
                    {
                        intelReport.DetailedWeaponSystemsData[weaponSystem] += scaledValue;
                    }
                    else
                    {
                        intelReport.DetailedWeaponSystemsData[weaponSystem] = scaledValue;
                    }
                }

                // Determine fog of war parameters
                bool isPositiveDirection = true;
                float errorRangeMin = 1f;
                float errorRangeMax = 1f;

                if (spottedLevel == SpottedLevel.Level2)
                {
                    isPositiveDirection = UnityEngine.Random.Range(0f, 1f) >= 0.5f;
                    errorRangeMin = 1f;
                    errorRangeMax = 30f;
                }
                else if (spottedLevel == SpottedLevel.Level3)
                {
                    isPositiveDirection = UnityEngine.Random.Range(0f, 1f) >= 0.5f;
                    errorRangeMin = 1f;
                    errorRangeMax = 10f;
                }

                // Apply fog of war to detailed data
                if (spottedLevel == SpottedLevel.Level2 || spottedLevel == SpottedLevel.Level3)
                {
                    var foggedDetailedData = new Dictionary<WeaponSystems, float>();
                    foreach (var kvp in intelReport.DetailedWeaponSystemsData)
                    {
                        float errorPercent = UnityEngine.Random.Range(errorRangeMin, errorRangeMax);
                        float multiplier = isPositiveDirection ? (1f + errorPercent / 100f) : (1f - errorPercent / 100f);
                        foggedDetailedData[kvp.Key] = kvp.Value * multiplier;
                    }
                    intelReport.DetailedWeaponSystemsData = foggedDetailedData;
                }

                // Categorize weapon systems into buckets
                foreach (var item in currentValues)
                {
                    string prefix = GetWeaponSystemPrefix(item.Key);
                    string bucketName = MapPrefixToBucket(prefix);

                    if (bucketName != null)
                    {
                        // Calculate fog of war multiplier for this bucket (each bucket gets its own error percentage)
                        float bucketMultiplier = 1f;
                        if (spottedLevel == SpottedLevel.Level2 || spottedLevel == SpottedLevel.Level3)
                        {
                            float errorPercent = UnityEngine.Random.Range(errorRangeMin, errorRangeMax);
                            bucketMultiplier = isPositiveDirection ? (1f + errorPercent / 100f) : (1f - errorPercent / 100f);
                        }

                        int foggedValue = (int)Math.Round(item.Value * bucketMultiplier);

                        // Map bucket names to IntelReport properties
                        switch (bucketName)
                        {
                            case "Men":
                                intelReport.Men += foggedValue;
                                break;
                            case "Tanks":
                                intelReport.Tanks += foggedValue;
                                break;
                            case "IFVs":
                                intelReport.IFVs += foggedValue;
                                break;
                            case "APCs":
                                intelReport.APCs += foggedValue;
                                break;
                            case "Recon":
                                intelReport.RCNs += foggedValue;
                                break;
                            case "Artillery":
                                intelReport.ARTs += foggedValue;
                                break;
                            case "Rocket Artillery":
                                intelReport.ROCs += foggedValue;
                                break;
                            case "Surface To Surface Missiles":
                                intelReport.SSMs += foggedValue;
                                break;
                            case "SAMs":
                                intelReport.SAMs += foggedValue;
                                break;
                            case "Anti-aircraft Artillery":
                                intelReport.AAAs += foggedValue;
                                break;
                            case "MANPADs":
                                intelReport.MANPADs += foggedValue;
                                break;
                            case "ATGMs":
                                intelReport.ATGMs += foggedValue;
                                break;
                            case "Attack Helicopters":
                                intelReport.HEL += foggedValue;
                                break;
                            case "Transport Helicopters":
                                intelReport.HELTRAN += foggedValue;
                                break;
                            case "Fighters":
                                intelReport.ASFs += foggedValue;
                                break;
                            case "Multirole":
                                intelReport.MRFs += foggedValue;
                                break;
                            case "Attack":
                                intelReport.ATTs += foggedValue;
                                break;
                            case "Bombers":
                                intelReport.BMBs += foggedValue;
                                break;
                            case "Transports":
                                intelReport.TRANs += foggedValue;
                                break;
                            case "AWACS":
                                intelReport.AWACS += foggedValue;
                                break;
                            case "Recon Aircraft":
                                intelReport.RCNAs += foggedValue;
                                break;
                        }
                    }
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

       
        #endregion // ISerializable Implementation


        #region ICloneable Implementation


        #endregion // ICloneable Implementation        
    }
}