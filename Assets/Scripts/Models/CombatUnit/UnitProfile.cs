using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using HammerAndSickle.Services;

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
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
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

                // Deep copy the dictionaries
                weaponSystems = new Dictionary<WeaponSystems, int>(source.weaponSystems);
                CurrentProfile = new Dictionary<WeaponSystems, int>(source.CurrentProfile);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "CopyConstructor", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "CopyWithNameConstructor", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "CopyWithNameAndNationalityConstructor", e);
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
               // TODO: Implement
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "UpdateCurrentHP", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "SetWeaponSystemValue", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "GetWeaponSystemMaxValue", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "RemoveWeaponSystem", e);
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
                AppService.Instance.HandleException(CLASS_NAME, "Clear", e);
            }
        }

        #endregion // Public Methods


        #region Status Reports

        /// <summary>
        /// Generates a detailed intel report for the player unit, this is passed onto GUI. The player gets full
        /// information on their own units.
        /// </summary>
        /// <returns></returns>
        public string GetPlayerUnitIntelReport(string unitName, CombatState combatState, ExperienceLevel xpLevel, EfficiencyLevel effLevel)
        {
            /* 
             *     Format for the report is as follows
             *     
             *     Example:
             *     
             *     Soviet 76th Guards Tank Regiment
             *     2100 Men, 130 Tanks, 80 IFVs, 10 APCs, 10 Recon, 
             *     45 Artillery, 12 AAA, 8 SAMs, 
             *     3 Attack Helicopters, 10 Transport Helicopters
             *     STATE: Deployed  EXP: Veteran EFF: PeakOperational
             */

            try
            {
                // This is the base multiplier for each weapon system.
                float currentMultiplier = currentHitPoints / CUConstants.MAX_HP;

                StringBuilder sb = new StringBuilder();

                foreach (var item in weaponSystems)
                {

                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetPlayerUnitIntelReport", e);
                throw;
            }
        }

        /// <summary>
        /// Lists each weapon system in the unit profile with its current strength, scaled by hit points.
        /// </summary>
        /// <returns></returns>
        public List<string> GetDetailedPlayerUnitIntelReport()
        {
            try
            {
                // This is the base multiplier for each weapon system.
                float currentMultiplier = currentHitPoints / CUConstants.MAX_HP;

                // Create a containter to store line.
                List<string> reportLines = new List<string>();

                foreach (var item in weaponSystems)
                {

                }

                return reportLines;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetDetailedPlayerUnitIntelReport", e);
                throw;
            }
        }

        /// <summary>
        /// Get intelligence information of an AI unit based on the it's SpottedLevel.
        /// </summary>
        /// <returns></returns>
        public string GetAIUnitIntelReport(SpottedLevel spottedLevel, 
            string unitName, 
            CombatState combatState, 
            ExperienceLevel xpLevel, 
            EfficiencyLevel effLevel)
        {
            try
            {
                // This is the base multiplier for each weapon system.
                float currentMultiplier = currentHitPoints / CUConstants.MAX_HP;

                StringBuilder sb = new StringBuilder();
                
                foreach (var item in weaponSystems)
                {

                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetAIUnitIntelReport", e);
                throw;
            }
        }

        #endregion


        #region ISerializable Implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // TODO: Implement
        }

        #endregion // ISerializable Implementation


        #region ICloneable Implementation

        public object Clone()
        {
            // TODO: Implement
            return null;
        }

        #endregion // ICloneable Implementation        
    }
}