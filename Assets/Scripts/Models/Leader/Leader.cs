using HammerAndSickle.Services;
using System;
using System.Runtime.Serialization;
using UnityEngine;

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
        #region Constants

        private const string CLASS_NAME = nameof(Leader);

        #endregion // Constants


        #region Properties

        public string ID { get; private set; }                               // Unique identifier for the officer
        public string Name { get; private set; }                             // Use random name generator
        public Side Side { get; private set; }                               // Player or AI
        public Nationality Nationality { get; private set; }                 // Nation of origin
        public CommandGrade CommandGrade { get; private set; }               // Rank of the officer
        public int ReputationPoints { get; private set; }                    // Points for the promotions and upgrades
        public string FormattedRank { get { return GetFormattedRank(); } }   // Real-world rank of the officer
        public CommandAbility CombatCommand { get; private set; }            // Direct combat modifier
        public bool IsAssigned { get; private set; }                         // Is the officer assigned to a unit?
        public string UnitID { get; private set; }                           // ID of the unit assigned to the officer

        #endregion // Properties


        #region Constructors

        public Leader(Side side, Nationality nationality)
        {
            try
            {
                
                // TODO: Implement
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
                // TODO Implement
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
                // TODO: Implement deserialization logic here
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "DeserializationConstructor", e);
                throw;
            }
        }

        #endregion // Constructors

 
        #region Public Methods
        
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
                // Create a new random number generator.
                System.Random random = new();

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

        /// <summary>
        /// Awards reputation to the leader based on action performed
        /// </summary>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="contextMultiplier">Additional multiplier based on context</param>
        //public void AwardReputation(ReputationAction actionType, float contextMultiplier = 1.0f)
        //{
        //    int baseREP = actionType switch
        //    {
        //        ReputationAction.Move => CUConstants.REP_PER_MOVE_ACTION,
        //        ReputationAction.MountDismount => CUConstants.REP_PER_MOUNT_DISMOUNT,
        //        ReputationAction.IntelGather => CUConstants.REP_PER_INTEL_GATHER,
        //        ReputationAction.Combat => CUConstants.REP_PER_COMBAT_ACTION,
        //        ReputationAction.AirborneJump => CUConstants.REP_PER_AIRBORNE_JUMP,
        //        ReputationAction.ForcedRetreat => CUConstants.REP_PER_FORCED_RETREAT,
        //        ReputationAction.UnitDestroyed => CUConstants.REP_PER_UNIT_DESTROYED,
        //        _ => 0
        //    };

        //    // Apply experience multiplier for veteran units
        //    if (_ExperienceLevel >= ExperienceLevel.Veteran)
        //    {
        //        contextMultiplier *= CUConstants.REP_EXPERIENCE_MULTIPLIER;
        //    }

        //    int finalREP = Mathf.RoundToInt(baseREP * contextMultiplier);

        //    if (finalREP > 0 && SkillTree != null)
        //    {
        //        SkillTree.AddReputation(finalREP);

        //        // Fire event for UI feedback
        //        OnReputationAwarded?.Invoke(actionType, finalREP);
        //    }
        //}

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


        #region ISerializable Implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try
            {
                // TODO: Implement serialization logic here
            }
            catch (Exception e)
            {
                AppService.Instance.HandleException(CLASS_NAME, "GetObjectData", e);
                throw;
            }
        }

        #endregion // ISerializable Implementation


        #region ICloneable Implementation

        public object Clone()
        {
            try
            {
               

                return null; // Implement deep copy logic here
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