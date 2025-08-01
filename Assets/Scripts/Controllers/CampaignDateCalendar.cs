using HammerAndSickle.Models;
using HammerAndSickle.Services;
using System;

namespace HammerAndSickle.Utils
{
    public class CampaignDateCalendar
    {
        #region Constants

        private const string CLASS_NAME = nameof(CampaignDateCalendar);
        private const int BASE_YEAR = 1938;
        private const int BASE_MONTH = 1; // January
        private const int END_YEAR = 2025;
        private const int END_MONTH = 1; // January
        private const int MONTHS_PER_YEAR = 12;

        // Total turns from Jan 1938 to Jan 2025 inclusive
        private const int MAX_TURNS = ((END_YEAR - BASE_YEAR) * MONTHS_PER_YEAR) + (END_MONTH - BASE_MONTH);

        #endregion //Constants


        #region Properties

        /// <summary>
        /// Current turn number (0-based, where 0 = January 1938).
        /// </summary>
        public int CurrentTurn { get; private set; }

        /// <summary>
        /// Campaign start turn number.
        /// </summary>
        public int CampaignStartTurn { get; private set; }

        /// <summary>
        /// Campaign end turn number.
        /// </summary>
        public int CampaignEndTurn { get; private set; }

        /// <summary>
        /// Whether the campaign is currently active (current turn within bounds).
        /// </summary>
        public bool IsActive => CurrentTurn >= CampaignStartTurn && CurrentTurn <= CampaignEndTurn;

        #endregion //Properties


        #region Constructor

        /// <summary>
        /// Initializes a new campaign calendar with specified date range.
        /// </summary>
        /// <param name="startDateMMYYYY">Start date in MMYYYY format (e.g., 061941 for June 1941)</param>
        /// <param name="endDateMMYYYY">End date in MMYYYY format</param>
        public CampaignDateCalendar(int startDateMMYYYY, int endDateMMYYYY)
        {
            try
            {
                CampaignStartTurn = DateToTurn(startDateMMYYYY);
                CampaignEndTurn = DateToTurn(endDateMMYYYY);

                if (CampaignStartTurn > CampaignEndTurn)
                {
                    throw new ArgumentException($"Start date ({startDateMMYYYY}) must be before end date ({endDateMMYYYY})");
                }

                CurrentTurn = CampaignStartTurn;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(CampaignDateCalendar), e);
                throw;
            }
        }

        #endregion //Constructor


        #region Public Methods

        /// <summary>
        /// Converts a MMYYYY date to absolute turn number.
        /// </summary>
        public static int DateToTurn(int dateMMYYYY)
        {
            try
            {
                var (month, year) = ParseDate(dateMMYYYY);

                if (!IsValidDate(month, year))
                {
                    throw new ArgumentOutOfRangeException(nameof(dateMMYYYY),
                        $"Date {dateMMYYYY} is outside valid range (011938-012025)");
                }

                int yearsSinceBase = year - BASE_YEAR;
                int monthsSinceBase = (month - BASE_MONTH);
                int turn = (yearsSinceBase * MONTHS_PER_YEAR) + monthsSinceBase;

                return turn;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(DateToTurn), e);
                throw;
            }
        }

        /// <summary>
        /// Converts an absolute turn number to MMYYYY date format.
        /// </summary>
        public static int TurnToDate(int turn)
        {
            try
            {
                if (turn < 0 || turn > MAX_TURNS)
                {
                    throw new ArgumentOutOfRangeException(nameof(turn),
                        $"Turn {turn} is outside valid range (0-{MAX_TURNS})");
                }

                int totalMonths = turn;
                int year = BASE_YEAR + (totalMonths / MONTHS_PER_YEAR);
                int month = BASE_MONTH + (totalMonths % MONTHS_PER_YEAR);

                // Handle month overflow
                if (month > MONTHS_PER_YEAR)
                {
                    year++;
                    month -= MONTHS_PER_YEAR;
                }

                return (month * 10000) + year;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(TurnToDate), e);
                throw;
            }
        }

        /// <summary>
        /// Sets the current turn with bounds checking.
        /// </summary>
        public void SetCurrentTurn(int turn)
        {
            try
            {
                if (turn < CampaignStartTurn || turn > CampaignEndTurn)
                {
                    throw new ArgumentOutOfRangeException(nameof(turn),
                        $"Turn {turn} is outside campaign range ({CampaignStartTurn}-{CampaignEndTurn})");
                }

                CurrentTurn = turn;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(SetCurrentTurn), e);
                throw;
            }
        }

        /// <summary>
        /// Advances to the next turn if within campaign bounds.
        /// </summary>
        public void IncrementTurn()
        {
            try
            {
                if (CurrentTurn >= CampaignEndTurn)
                {
                    AppService.CaptureUiMessage("Campaign has reached its end date");
                    return;
                }

                CurrentTurn++;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IncrementTurn), e);
                throw;
            }
        }

        /// <summary>
        /// Moves to the previous turn if within campaign bounds.
        /// </summary>
        public void DecrementTurn()
        {
            try
            {
                if (CurrentTurn <= CampaignStartTurn)
                {
                    AppService.CaptureUiMessage("Cannot go before campaign start date");
                    return;
                }

                CurrentTurn--;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(DecrementTurn), e);
                throw;
            }
        }

        /// <summary>
        /// Checks if a weapon system is available at the current campaign turn.
        /// </summary>
        public bool IsWeaponSystemAvailable(WeaponSystemProfile profile)
        {
            try
            {
                if (profile == null)
                {
                    throw new ArgumentNullException(nameof(profile));
                }

                return CurrentTurn >= profile.TurnAvailable;
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(IsWeaponSystemAvailable), e);
                throw;
            }
        }

        /// <summary>
        /// Returns the number of turns remaining in the campaign.
        /// </summary>
        public int GetRemainingTurns()
        {
            return Math.Max(0, CampaignEndTurn - CurrentTurn);
        }

        /// <summary>
        /// Returns the current turn as a formatted date string.
        /// </summary>
        public string GetCurrentDateString()
        {
            try
            {
                int dateMMYYYY = TurnToDate(CurrentTurn);
                var (month, year) = ParseDate(dateMMYYYY);

                string[] monthNames = { "", "January", "February", "March", "April", "May", "June",
                                      "July", "August", "September", "October", "November", "December" };

                return $"{monthNames[month]} {year}";
            }
            catch (Exception e)
            {
                AppService.HandleException(CLASS_NAME, nameof(GetCurrentDateString), e);
                return "Unknown Date";
            }
        }

        #endregion //Public Methods


        #region Private Methods

        /// <summary>
        /// Parses MMYYYY format into month and year components.
        /// </summary>
        private static (int month, int year) ParseDate(int dateMMYYYY)
        {
            int month = dateMMYYYY / 10000;
            int year = dateMMYYYY % 10000;

            if (month < 1 || month > 12)
            {
                throw new ArgumentException($"Invalid month {month} in date {dateMMYYYY}");
            }

            return (month, year);
        }

        /// <summary>
        /// Validates that a date falls within the supported range.
        /// </summary>
        private static bool IsValidDate(int month, int year)
        {
            if (year < BASE_YEAR || year > END_YEAR)
                return false;

            if (year == BASE_YEAR && month < BASE_MONTH)
                return false;

            if (year == END_YEAR && month > END_MONTH)
                return false;

            return true;
        }

        #endregion //Private Methods
    }
}