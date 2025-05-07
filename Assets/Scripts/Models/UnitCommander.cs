using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Possible ranks for Soviet Officers.
    /// </summary>
    public enum OfficerRanks
    {
        Colonel,
        MajorGeneral,
        LieutenantGeneral,
        ColonelGeneral
    }

    /// <summary>
    /// The command ability of an officer.
    /// </summary>
    public enum Command
    {
        Poor = -2,
        BelowAverage = -1,
        Average = 0,
        Good = 1,
        Superior = 2,
    }

    /// <summary>
    /// The aggressiveness of an officer.
    /// </summary>
    public enum Initiative
    {
        Poor = -2,
        BelowAverage = -1,
        Average = 0,
        Good = 1,
        Superior = 2,
    }

    /// <summary>
    /// Represents a military officer who commands a unit.
    /// Provides functionality for generating commanders with appropriate names and abilities
    /// based on nationality, along with serialization support.
    /// </summary>
    [Serializable]
    public class UnitCommander : ISerializable, ICloneable
    {
        #region Constants
        private const string CLASS_NAME = nameof(UnitCommander);
        private static readonly Random random = new();
        #endregion

        #region Private Fields
        private static readonly System.Random rand = random;
        #endregion

        #region Properties
        /// <summary>
        /// The officer's full name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The side this officer belongs to (Player or AI).
        /// </summary>
        public Side Side { get; private set; }

        /// <summary>
        /// The nationality of this officer, which determines naming conventions.
        /// </summary>
        public Nationality Nationality { get; private set; }

        /// <summary>
        /// The officer's military rank.
        /// </summary>
        public OfficerRanks OfficerRank { get; set; }

        /// <summary>
        /// The officer's command ability, affecting unit performance.
        /// </summary>
        public Command OfficerCommand { get; private set; }

        /// <summary>
        /// The officer's initiative, affecting action priority and response.
        /// </summary>
        public Initiative OfficerInitiative { get; private set; }

        /// <summary>
        /// Indicates whether this officer is assigned to a unit.
        /// </summary>
        public bool IsAssigned { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new officer with default values for the specified side and nationality.
        /// </summary>
        /// <param name="side">The side this officer belongs to (Player or AI)</param>
        /// <param name="nationality">The nationality of this officer</param>
        public UnitCommander(Side side, Nationality nationality)
        {
            try
            {
                Side = side;
                Nationality = nationality;
                OfficerRank = OfficerRanks.Colonel;
                OfficerCommand = Command.Average;
                OfficerInitiative = Initiative.Average;
                IsAssigned = false;
                Name = string.Empty;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a new officer with the specified attributes.
        /// </summary>
        /// <param name="name">The officer's name</param>
        /// <param name="side">The side this officer belongs to</param>
        /// <param name="nationality">The nationality of this officer</param>
        /// <param name="command">The officer's command ability</param>
        /// <param name="initiative">The officer's initiative</param>
        public UnitCommander(string name, Side side, Nationality nationality, Command command, Initiative initiative)
        {
            try
            {
                Name = name;
                Side = side;
                Nationality = nationality;
                OfficerRank = OfficerRanks.Colonel;
                OfficerCommand = command;
                OfficerInitiative = initiative;
                IsAssigned = false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected UnitCommander(SerializationInfo info, StreamingContext context)
        {
            try
            {
                Name = info.GetString(nameof(Name));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                OfficerRank = (OfficerRanks)info.GetValue(nameof(OfficerRank), typeof(OfficerRanks));
                OfficerCommand = (Command)info.GetValue(nameof(OfficerCommand), typeof(Command));
                OfficerInitiative = (Initiative)info.GetValue(nameof(OfficerInitiative), typeof(Initiative));
                IsAssigned = info.GetBoolean(nameof(IsAssigned));
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the officer's command ability.
        /// </summary>
        /// <param name="command">The new command ability value</param>
        public void SetOfficerCommandAbility(Command command)
        {
            try
            {
                OfficerCommand = command;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetOfficerCommandAbility", e);
                throw;
            }
        }

        /// <summary>
        /// Sets the officer's initiative level.
        /// </summary>
        /// <param name="initiative">The new initiative value</param>
        public void SetOfficerInitiative(Initiative initiative)
        {
            try
            {
                OfficerInitiative = initiative;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetOfficerInitiative", e);
                throw;
            }
        }

        /// <summary>
        /// Randomly generates an officer with appropriate name, rank, and abilities.
        /// Uses the NameGenService to create culturally appropriate names.
        /// </summary>
        public void RandomlyGenerateMe()
        {
            try
            {
                // Generate a random name based on nationality
                Name = NameGenService.Instance.GenerateMaleName(Nationality);

                // Determine overall ability based on a bell curve distribution
                double randomValue = rand.NextDouble();
                if (randomValue < 0.02)
                {
                    AssignSkillLevel(Command.Superior, Initiative.Superior);
                }
                else if (randomValue < 0.15)
                {
                    AssignSkillLevel(Command.Good, Initiative.Good);
                }
                else if (randomValue < 0.85)
                {
                    AssignSkillLevel(Command.Average, Initiative.Average);
                }
                else if (randomValue < 0.98)
                {
                    AssignSkillLevel(Command.BelowAverage, Initiative.BelowAverage);
                }
                else
                {
                    OfficerCommand = Command.Poor;
                    OfficerInitiative = Initiative.Poor;
                }

                // Randomly assign a rank (weighted towards lower ranks)
                randomValue = rand.NextDouble();
                if (randomValue < 0.70)
                {
                    OfficerRank = OfficerRanks.Colonel;
                }
                else if (randomValue < 0.90)
                {
                    OfficerRank = OfficerRanks.MajorGeneral;
                }
                else if (randomValue < 0.98)
                {
                    OfficerRank = OfficerRanks.LieutenantGeneral;
                }
                else
                {
                    OfficerRank = OfficerRanks.ColonelGeneral;
                }
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RandomlyGenerateMe", e);
                throw;
            }
        }

        /// <summary>
        /// Gets the officer's rank as a formatted string.
        /// </summary>
        /// <returns>The officer's rank as a string</returns>
        public string GetFormattedRank()
        {
            try
            {
                return Nationality switch
                {
                    Nationality.USSR => OfficerRank switch
                    {
                        OfficerRanks.Colonel => "Colonel",
                        OfficerRanks.MajorGeneral => "Major General",
                        OfficerRanks.LieutenantGeneral => "Lieutenant General",
                        OfficerRanks.ColonelGeneral => "Colonel General",
                        _ => "Officer",
                    },// Return Soviet-styled ranks
                    Nationality.USA or Nationality.UK => OfficerRank switch
                    {
                        OfficerRanks.Colonel => "Colonel",
                        OfficerRanks.MajorGeneral => "Major General",
                        OfficerRanks.LieutenantGeneral => "Lieutenant General",
                        OfficerRanks.ColonelGeneral => "General",
                        _ => "Officer",
                    },// Return US/UK-styled ranks
                    Nationality.FRG => OfficerRank switch
                    {
                        OfficerRanks.Colonel => "Oberst",
                        OfficerRanks.MajorGeneral => "Generalmajor",
                        OfficerRanks.LieutenantGeneral => "Generalleutnant",
                        OfficerRanks.ColonelGeneral => "General",
                        _ => "Offizier",
                    },// Return German-styled ranks
                    Nationality.FRA => OfficerRank switch
                    {
                        OfficerRanks.Colonel => "Colonel",
                        OfficerRanks.MajorGeneral => "Général de Brigade",
                        OfficerRanks.LieutenantGeneral => "Général de Division",
                        OfficerRanks.ColonelGeneral => "Général d'Armée",
                        _ => "Officier",
                    },// Return French-styled ranks
                    _ => OfficerRank.ToString(),// Default formatting
                };
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetFormattedRank", e);
                return "Officer";
            }
        }

        /// <summary>
        /// Gets a brief performance rating for this officer.
        /// </summary>
        /// <returns>A string describing the officer's abilities</returns>
        public string GetPerformanceRating()
        {
            try
            {
                int totalRating = (int)OfficerCommand + (int)OfficerInitiative;

                if (totalRating >= 3)
                    return "Outstanding";
                else if (totalRating >= 1)
                    return "Above Average";
                else if (totalRating >= -1)
                    return "Average";
                else if (totalRating >= -3)
                    return "Below Average";
                else
                    return "Poor";
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetPerformanceRating", e);
                return "Undetermined";
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Assigns skill levels to the officer with some randomness.
        /// </summary>
        /// <param name="command">The base command level to assign</param>
        /// <param name="initiative">The base initiative level to assign</param>
        private void AssignSkillLevel(Command command, Initiative initiative)
        {
            try
            {
                OfficerCommand = (rand.NextDouble() < 0.5 && command != Command.Poor) ? (Command)((int)command - 1) : command;
                OfficerInitiative = (rand.NextDouble() < 0.5 && initiative != Initiative.Poor) ? (Initiative)((int)initiative - 1) : initiative;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "AssignSkillLevel", e);
                throw;
            }
        }
        #endregion

        #region ISerializable Implementation
        /// <summary>
        /// Serializes this UnitCommander instance.
        /// </summary>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // Store basic properties
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(Side), Side);
                info.AddValue(nameof(Nationality), Nationality);
                info.AddValue(nameof(OfficerRank), OfficerRank);
                info.AddValue(nameof(OfficerCommand), OfficerCommand);
                info.AddValue(nameof(OfficerInitiative), OfficerInitiative);
                info.AddValue(nameof(IsAssigned), IsAssigned);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }
        #endregion

        #region ICloneable Implementation
        /// <summary>
        /// Creates a deep copy of this UnitCommander.
        /// </summary>
        /// <returns>A new UnitCommander with identical values</returns>
        public object Clone()
        {
            try
            {
                // Create a new commander with the same attributes
                UnitCommander clone = new(
                    Name,
                    Side,
                    Nationality,
                    OfficerCommand,
                    OfficerInitiative
                );

                // Copy additional properties
                clone.OfficerRank = this.OfficerRank;
                clone.IsAssigned = this.IsAssigned;

                return clone;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Clone", e);
                throw;
            }
        }
        #endregion
    }
}