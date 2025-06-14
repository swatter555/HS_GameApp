using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using HammerAndSickle.Services;
using System.Linq;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// UnitProfile is a mechanism to define a unit in terms of men, tanks, artillery, etc.,
    /// while the combat values in the WeaponSystemProfile are used to resolve combat. This
    /// mechanism is meant only for informational purposes, displayed in the GUI to the user.
    /// This allows for the tracking of losses during a scenario and/or campaign.
    /// 
    /// Methods:
    /// - Constructor: Creates new unit profiles with validation
    /// - SetWeaponSystemValue: Configures maximum values for weapon systems
    /// - UpdateCurrentProfile: Recalculates current strength based on hit points
    /// - Clone: Creates deep copies with optional profileID/nationality changes
    /// - Serialization: Complete ISerializable implementation for save/load
    /// 
    /// Key Features:
    /// - Tracks both maximum and current weapon system counts
    /// - Automatically scales current values based on unit hit points
    /// - Supports deep cloning with parameter overrides
    /// - Comprehensive validation and error handling
    /// - Efficient dictionary-based storage for weapon systems
    /// </summary>
    [Serializable]
    public class UnitProfile : ISerializable, ICloneable
    {
        #region Constants

        private const string CLASS_NAME = nameof(UnitProfile);

        #endregion // Constants

        #region Fields

        private readonly Dictionary<WeaponSystems, int> weaponSystems; // Maximum values for each weapon system in this profile.
        private float currentHitPoints = CUConstants.MAX_HP;           // Tracks current hit points for scaling.

        #endregion // Fields

        #region Properties

        public string UnitProfileID { get; private set; }
        public Nationality Nationality { get; private set; }
        public IntelReport LastIntelReport { get; private set; } = null; // Last generated intel report for this profile.

        // The current profile, reflecting the paper strength of the unit
        public Dictionary<WeaponSystems, int> CurrentProfile { get; private set; }

        #endregion // Properties


        #region Constructors

        /// <summary>
        /// Creates a new instance of the UnitProfile class with validation.
        /// </summary>
        /// <param name="profileID">The profileID of the unit profile</param>
        /// <param name="nationality">The nationality of the unit</param>
        public UnitProfile(string profileID, Nationality nationality)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(profileID))
                    throw new ArgumentException("Profile name cannot be null or empty", nameof(profileID));

                UnitProfileID = profileID;
                Nationality = nationality;
                weaponSystems = new Dictionary<WeaponSystems, int>();
                CurrentProfile = new Dictionary<WeaponSystems, int>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy of an existing profile.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        private UnitProfile(UnitProfile source)
        {
            try
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));

                UnitProfileID = source.UnitProfileID;
                Nationality = source.Nationality;
                currentHitPoints = source.currentHitPoints;

                // Deep copy the dictionaries
                weaponSystems = new Dictionary<WeaponSystems, int>(source.weaponSystems);
                CurrentProfile = new Dictionary<WeaponSystems, int>(source.CurrentProfile);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CopyConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy with a new profileID.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        /// <param name="newName">The new profileID for the profile</param>
        private UnitProfile(UnitProfile source, string newName) : this(source)
        {
            try
            {
                if (string.IsNullOrEmpty(newName))
                    throw new ArgumentException("New name cannot be null or empty", nameof(newName));

                UnitProfileID = newName;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CopyWithNameConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of UnitProfile as a copy with a new profileID and nationality.
        /// </summary>
        /// <param name="source">The UnitProfile to copy from</param>
        /// <param name="newName">The new profileID for the profile</param>
        /// <param name="newNationality">The new nationality for the profile</param>
        private UnitProfile(UnitProfile source, string newName, Nationality newNationality) : this(source, newName)
        {
            try
            {
                Nationality = newNationality;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "CopyWithNameAndNationalityConstructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected UnitProfile(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                UnitProfileID = info.GetString(nameof(UnitProfileID));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                currentHitPoints = info.GetSingle(nameof(currentHitPoints));

                // Deserialize weapon systems dictionary
                int weaponSystemCount = info.GetInt32("WeaponSystemCount");
                weaponSystems = new Dictionary<WeaponSystems, int>();

                for (int i = 0; i < weaponSystemCount; i++)
                {
                    WeaponSystems weaponSystem = (WeaponSystems)info.GetValue($"WeaponSystem_{i}", typeof(WeaponSystems));
                    int maxValue = info.GetInt32($"WeaponSystemValue_{i}");
                    weaponSystems[weaponSystem] = maxValue;
                }

                // Initialize CurrentProfile (will be populated when intel reports are generated)
                CurrentProfile = new Dictionary<WeaponSystems, int>();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "DeserializationConstructor", e);
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
        /// Sets the maximum value for a specific weapon system in this unit profile.
        /// Creates a new entry if the weapon system doesn't exist in this profile.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to configure</param>
        /// <param name="maxValue">The maximum number of this weapon system in the unit</param>
        public void SetWeaponSystemValue(WeaponSystems weaponSystem, int maxValue)
        {
            try
            {
                if (maxValue < 0)
                    throw new ArgumentException("Max value cannot be negative", nameof(maxValue));

                weaponSystems[weaponSystem] = maxValue;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "SetWeaponSystemValue", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the maximum value for a specific weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to query</param>
        /// <returns>The maximum value, or 0 if not found</returns>
        public int GetWeaponSystemMaxValue(WeaponSystems weaponSystem)
        {
            try
            {
                return weaponSystems.TryGetValue(weaponSystem, out int value) ? value : 0;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "GetWeaponSystemMaxValue", e);
                return 0;
            }
        }

        /// <summary>
        /// Removes a weapon system from this profile entirely.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to remove</param>
        /// <returns>True if the weapon system was removed, false if it wasn't found</returns>
        public bool RemoveWeaponSystem(WeaponSystems weaponSystem)
        {
            try
            {
                bool removedMax = weaponSystems.Remove(weaponSystem);
                bool removedCurrent = CurrentProfile.Remove(weaponSystem);
                return removedMax || removedCurrent;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "RemoveWeaponSystem", e);
                return false;
            }
        }

        /// <summary>
        /// Checks if this profile contains a specific weapon system.
        /// </summary>
        /// <param name="weaponSystem">The weapon system to check for</param>
        /// <returns>True if the weapon system is present</returns>
        public bool HasWeaponSystem(WeaponSystems weaponSystem)
        {
            return weaponSystems.ContainsKey(weaponSystem);
        }

        /// <summary>
        /// Gets all weapon systems in this profile.
        /// </summary>
        /// <returns>Collection of weapon systems</returns>
        public IEnumerable<WeaponSystems> GetWeaponSystems()
        {
            return weaponSystems.Keys;
        }

        /// <summary>
        /// Gets the total number of weapon systems in this profile.
        /// </summary>
        /// <returns>Count of weapon systems</returns>
        public int GetWeaponSystemCount()
        {
            return weaponSystems.Count;
        }

        /// <summary>
        /// Clears all weapon systems from this profile.
        /// </summary>
        public void Clear()
        {
            try
            {
                weaponSystems.Clear();
                CurrentProfile.Clear();
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clear", e);
            }
        }

        #endregion // Public Methods


        #region IntelReports

        /// <summary>
        /// Generates an IntelReport object containing bucketed weapon system data and unit metadata.
        /// This provides structured data for the GUI to display unit intelligence information.
        /// Applies fog of war effects based on spotted level for AI units.
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
                foreach (var item in weaponSystems)
                {
                    float scaledValue = item.Value * currentMultiplier;
                    int currentValue = (int)Math.Round(scaledValue);

                    if (currentValue > 0)
                    {
                        currentValues[item.Key] = currentValue;
                    }

                    // Always add to detailed data (even if 0) for complete information
                    intelReport.DetailedWeaponSystemsData[item.Key] = scaledValue;
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
                AppService.HandleException(CLASS_NAME, "GenerateIntelReport", e);
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Basic properties
                info.AddValue(nameof(UnitProfileID), UnitProfileID);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(currentHitPoints), currentHitPoints);

                // Serialize weapon systems dictionary
                info.AddValue("WeaponSystemCount", weaponSystems.Count);

                int index = 0;
                foreach (var kvp in weaponSystems)
                {
                    info.AddValue($"WeaponSystem_{index}", kvp.Key);
                    info.AddValue($"WeaponSystemValue_{index}", kvp.Value);
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

        public object Clone()
        {
            try
            {
                return new UnitProfile(this);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(Clone), e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile with a new profile ID.
        /// </summary>
        /// <param name="newProfileID">The new profile ID for the cloned profile</param>
        /// <returns>A new UnitProfile with identical weapon systems but different ID</returns>
        public UnitProfile Clone(string newProfileID)
        {
            try
            {
                return new UnitProfile(this, newProfileID);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a deep copy of this UnitProfile with a new profile ID and nationality.
        /// </summary>
        /// <param name="newProfileID">The new profile ID for the cloned profile</param>
        /// <param name="newNationality">The new nationality for the cloned profile</param>
        /// <returns>A new UnitProfile with identical weapon systems but different ID and nationality</returns>
        public UnitProfile Clone(string newProfileID, Nationality newNationality)
        {
            try
            {
                return new UnitProfile(this, newProfileID, newNationality);
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }

        #endregion // ICloneable Implementation        
    }
}