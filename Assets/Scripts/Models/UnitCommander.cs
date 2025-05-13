using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    /// <summary>
    /// Represents the military rank grade of a commander.
    /// </summary>
    public enum CommandGrade
    {
        JuniorGrade,    // Lieutenant Colonel equivalent
        SeniorGrade,    // Colonel equivalent
        TopGrade        // Major General equivalent
    }

    /// <summary>
    /// The command ability of an officer.
    /// </summary>
    public enum CommandAbilites
    {
        Poor = -2,
        BelowAverage = -1,
        Average = 0,
        Good = 1,
        Superior = 2,
        Genius = 3
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
        public CommandGrade CommandGrade { get; set; }

        /// <summary>
        /// The officer's real world rank equilivalent.
        /// </summary>
        public string FormattedRank { get { return GetFormattedRank(); } }

        /// <summary>
        /// The officer's command ability, affecting unit performance.
        /// </summary>
        public CommandAbilites CombatCommand { get; private set; }

        /// <summary>
        /// The officer's initiative, affecting action priority and response.
        /// </summary>
        public CommandAbilites CombatInitiative { get; private set; }

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
                CommandGrade = CommandGrade.JuniorGrade;
                CombatCommand = CommandAbilites.BelowAverage;
                CombatInitiative = CommandAbilites.BelowAverage;
                IsAssigned = false;

                // Initialize with a placeholder name instead of empty string
                Name = $"Officer-{Guid.NewGuid().ToString()[..8]}";

                // Optionally, automatically generate a name if NameGenService is available
                if (NameGenService.Instance != null)
                {
                    try
                    {
                        string generatedName = NameGenService.Instance.GenerateMaleName(nationality);
                        if (!string.IsNullOrEmpty(generatedName))
                        {
                            Name = generatedName;
                        }
                    }
                    catch
                    {
                        // Silently fail and keep the placeholder name
                    }
                }
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
        public UnitCommander(string name, Side side, Nationality nationality, CommandAbilites command, CommandAbilites initiative)
        {
            try
            {
                Name = name;
                Side = side;
                Nationality = nationality;
                CommandGrade = CommandGrade.JuniorGrade;
                CombatCommand = command;
                CombatInitiative = initiative;
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
                CommandGrade = (CommandGrade)info.GetValue(nameof(CommandGrade), typeof(CommandGrade));
                CombatCommand = (CommandAbilites)info.GetValue(nameof(CombatCommand), typeof(CommandAbilites));
                CombatInitiative = (CommandAbilites)info.GetValue(nameof(CombatInitiative), typeof(CommandAbilites));
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
        public void SetOfficerCommandAbility(CommandAbilites command)
        {
            try
            {
                CombatCommand = command;
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
        public void SetOfficerInitiative(CommandAbilites initiative)
        {
            try
            {
                CombatInitiative = initiative;
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
        public void RandomlyGenerateMe(Nationality nationality)
        {
            try
            {
                // Check if NameGenService is available
                if (NameGenService.Instance == null)
                {
                    throw new InvalidOperationException("NameGenService is not available");
                }

                // Update the officer's nationality
                this.Nationality = nationality;

                // Generate a random name based on nationality
                Name = NameGenService.Instance.GenerateMaleName(nationality);

                // Ensure name is valid
                if (string.IsNullOrEmpty(Name))
                {
                    Name = $"Officer-{Guid.NewGuid().ToString()[..8]}";
                }

                // Generate command ability and initiative on a bell curve
                // Using a bell curve approximation with 3d6-10 to get values between -4 and 8
                int commandValue = (random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7)) - 10;
                int initiativeValue = (random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7)) - 10;

                // Limit to the enum range of -2 to 3
                commandValue = Math.Clamp(commandValue, -2, 3);
                initiativeValue = Math.Clamp(initiativeValue, -2, 3);

                // Ensure values are within one of each other
                if (Math.Abs(commandValue - initiativeValue) > 1)
                {
                    // If too far apart, bring them closer
                    if (commandValue > initiativeValue)
                    {
                        initiativeValue = commandValue - 1;
                    }
                    else
                    {
                        initiativeValue = commandValue + 1;
                    }
                }

                // Set the properties using the calculated values
                CombatCommand = (CommandAbilites)commandValue;
                CombatInitiative = (CommandAbilites)initiativeValue;

                // Assign an appropriate grade based on abilities
                int totalRating = (int)CombatCommand + (int)CombatInitiative;
                if (totalRating >= 4)
                {
                    CommandGrade = CommandGrade.TopGrade;
                }
                else if (totalRating >= 0)
                {
                    CommandGrade = CommandGrade.SeniorGrade;
                }
                else
                {
                    CommandGrade = CommandGrade.JuniorGrade;
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
                    Nationality.USSR => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Lieutenant Colonel",
                        CommandGrade.SeniorGrade => "Colonel",
                        CommandGrade.TopGrade => "Major General",
                        _ => "Officer",
                    },// Return Soviet-styled ranks
                    Nationality.USA or Nationality.UK or Nationality.IQ or Nationality.IR or Nationality.SAUD  => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Lieutenant Colonel",
                        CommandGrade.SeniorGrade => "Colonel",
                        CommandGrade.TopGrade => "Brigadier General",
                        _ => "Officer",
                    },// Return US/UK-styled ranks
                    Nationality.FRG => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Oberst",
                        CommandGrade.SeniorGrade => "Generalmajor",
                        CommandGrade.TopGrade => "Generalleutnant",
                        _ => "Offizier",
                    },// Return German-styled ranks
                    Nationality.FRA => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => "Colonel",
                        CommandGrade.SeniorGrade => "Général de Brigade",
                        CommandGrade.TopGrade => "Général de Division",
                        _ => "Officier",
                    },// Return French-styled ranks
                    Nationality.MJ => CommandGrade switch
                    {
                        CommandGrade.JuniorGrade => " Amir al-Fawj",
                        CommandGrade.SeniorGrade => "Amir al-Mintaqa",
                        CommandGrade.TopGrade => "Amir al-Jihad",
                        _ => "Commander",
                    },// Return French-styled ranks
                    _ => CommandGrade.ToString(),// Default formatting
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
                int totalRating = (int)CombatCommand + (int)CombatInitiative;

                // Use non-overlapping ranges for clear categorization
                if (totalRating >= 5)
                    return "Outstanding";
                else if (totalRating >= 3 && totalRating < 5)
                    return "Above Average";
                else if (totalRating >= 1 && totalRating < 3)
                    return "Average";
                else if (totalRating >= -1 && totalRating < 1)
                    return "Below Average";
                else if (totalRating >= -4 && totalRating < -1)
                    return "Poor";
                else
                    return "Incompetent"; // For extremely low ratings (though they shouldn't happen with current limits)
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetPerformanceRating", e);
                return "Undetermined";
            }
        }

        /// <summary>
        /// Validates and sets an officer's name
        /// </summary>
        /// <param name="name">The name to set</param>
        /// <returns>True if the name was set successfully, false otherwise</returns>
        public bool SetOfficerName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return false;
                }

                Name = name;
                return true;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "SetOfficerName", e);
                return false;
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
                info.AddValue(nameof(CommandGrade), CommandGrade);
                info.AddValue(nameof(CombatCommand), CombatCommand);
                info.AddValue(nameof(CombatInitiative), CombatInitiative);
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
                UnitCommander clone = new(Name, Side, Nationality, CombatCommand, CombatInitiative)
                {
                    // Copy additional properties
                    CommandGrade = this.CommandGrade,
                    IsAssigned = this.IsAssigned
                };

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