using System;
using System.Runtime.Serialization;
using HammerAndSickle.Services;

namespace HammerAndSickle.Models
{
    

    /// <summary>
    /// Represents a military officer who commands a unit.
    /// Provides functionality for generating commanders with appropriate names and abilities
    /// based on nationality, along with serialization support.
    /// </summary>
    [Serializable]
    public class Leader : ISerializable, ICloneable
    {
        //====== Constants ======
        #region Constants
        //=======================

        private const string CLASS_NAME = nameof(Leader);
        private static readonly Random random = new();
        #endregion // Constants

        //====== Properties ======
        #region Properties
        //========================

        public string Name { get; private set; }
        public Side Side { get; private set; }
        public Nationality Nationality { get; private set; }
        public CommandGrade CommandGrade { get; private set; }
        public string FormattedRank { get { return GetFormattedRank(); } }
        public CommandAbility CombatCommand { get; private set; }
        public bool IsAssigned { get; private set; }
        #endregion // Properties

        //====== Constructors ======
        #region Constructors
        //==========================

        public Leader(Side side, Nationality nationality)
        {
            try
            {
                Side = side;
                Nationality = nationality;
                CommandGrade = CommandGrade.JuniorGrade;
                CombatCommand = CommandAbility.BelowAverage;
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

        public Leader(string name, Side side, Nationality nationality, CommandAbility command)
        {
            try
            {
                Name = name;
                Side = side;
                Nationality = nationality;
                CommandGrade = CommandGrade.JuniorGrade;
                CombatCommand = command;
                IsAssigned = false;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "Constructor", e);
                throw;
            }
        }

        // Deserialization constructor
        protected Leader(SerializationInfo info, StreamingContext context)
        {
            try
            {
                Name = info.GetString(nameof(Name));
                Side = (Side)info.GetValue(nameof(Side), typeof(Side));
                Nationality = (Nationality)info.GetValue(nameof(Nationality), typeof(Nationality));
                CommandGrade = (CommandGrade)info.GetValue(nameof(CommandGrade), typeof(CommandGrade));
                CombatCommand = (CommandAbility)info.GetValue(nameof(CombatCommand), typeof(CommandAbility));
                IsAssigned = info.GetBoolean(nameof(IsAssigned));
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }
        #endregion // Constructors

        //====== Public Methods ======
        #region Public Methods
        //============================
        
        public void SetOfficerCommandAbility(CommandAbility command)
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

                // Using a bell curve approximation with 3d6-10 to get values between -4 and 8
                int commandValue = (random.Next(1, 7) + random.Next(1, 7) + random.Next(1, 7)) - 10;

                // Limit to the enum range of -2 to 3
                commandValue = Math.Clamp(commandValue, -2, 3);

                // Set the properties using the calculated values
                CombatCommand = (CommandAbility)commandValue;
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "RandomlyGenerateMe", e);
                throw;
            }
        }

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
        #endregion // Public Methods

        //====== ISerializable Implementation ======
        #region ISerializable Implementation
        //==========================================

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
                info.AddValue(nameof(IsAssigned), IsAssigned);
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }
        #endregion // ISerializable Implementation

        //====== ICloneable Implementation ======
        #region ICloneable Implementation
        //========================================
 
        public object Clone()
        {
            try
            {
                // Create a new commander with the same attributes
                Leader clone = new(Name, Side, Nationality, CombatCommand)
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
        #endregion // ICloneable Implementation
    }
}