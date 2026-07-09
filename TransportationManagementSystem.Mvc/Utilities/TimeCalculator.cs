namespace TransportationManagementSystem.Mvc.Utilities
{
    public static class TimeCalculator
    {
        /// <summary>
        /// Formats a TimeSpan as "H:MM" (e.g. a 1-day-2-hour-5-minute span
        /// becomes "26:05"). Copied verbatim from the original.
        /// </summary>
        public static string FormatWeeklyTime(TimeSpan weeklyTime)
        {
            var hoursDay = weeklyTime.Days * 24;
            var hours = weeklyTime.Hours;
            var minutes = weeklyTime.Minutes;
            var strMinutes = minutes < 10 ? "0" + minutes : minutes.ToString();
            var totHours = hoursDay + hours;

            return $"{totHours}:{strMinutes}";
        }

    }
}
