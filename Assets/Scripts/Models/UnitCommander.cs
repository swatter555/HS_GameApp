using System;

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
    /// The officer in command of a BaseUnit.
    /// </summary>
    public class UnitCommander
    {
        // Fields.
        private static System.Random rand = new System.Random();

        //Properties.
        public string Name { get; private set; } = String.Empty;
        public Side Side { get; private set; } = Side.Player;
        public Nationality Nationality { get; private set; } = Nationality.USSR;
        public OfficerRanks OfficerRank { get; set; } = OfficerRanks.Colonel;
        public Command OfficerCommand { get; private set; } = Command.Average;
        public Initiative OfficerInitiative { get; private set; } = Initiative.Average;
        public bool IsAssigned { get; set; } = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UnitCommander(Side side, Nationality nationality)
        {
            this.Side = side;
            this.Nationality = nationality;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public UnitCommander(string name, Side side, Nationality nationality, Command command, Initiative initiative)
        {
            Name = name;
            Side = side;
            Nationality = nationality;
            OfficerCommand = command;
            OfficerInitiative = initiative;
        }

        /// <summary>
        /// Set OfficerCommand.
        /// </summary>
        public void SetOfficerCommandAbility(Command command)
        {
            OfficerCommand = command;
        }

        /// <summary>
        /// Set OfficerInitiative.
        /// </summary>
        public void SetOfficerInitiative(Initiative initiative)
        {
            OfficerInitiative = initiative;
        }

        /// <summary>
        /// Randomly generates a Commanding Officer with a male name, rank, and ratings for Command and Initiative based on a bell curve distribution.
        /// </summary>
        public void RandomlyGenerateMe()
        {
            // Generate a random male name
            //Name = RussianName.Generate(true);

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
        }

        /// <summary>
        /// Add some randomness to choosing a skill level.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="initiative"></param>
        private void AssignSkillLevel(Command command, Initiative initiative)
        {
            OfficerCommand = (rand.NextDouble() < 0.5 && command != Command.Poor) ? (Command)((int)command - 1) : command;
            OfficerInitiative = (rand.NextDouble() < 0.5 && initiative != Initiative.Poor) ? (Initiative)((int)initiative - 1) : initiative;
        }
    }
}