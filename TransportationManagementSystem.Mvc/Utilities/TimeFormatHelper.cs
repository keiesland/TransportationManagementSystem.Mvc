namespace TransportationManagementSystem.Mvc.Utilities
{
    public static class TimeFormatHelper
    {
        /// <summary>
        /// Formats a TimeSpan as HH:MM:SS using total elapsed hours,
        /// rather than .NET's default "d.hh:mm:ss" format — avoids
        /// showing "1.02:53:00" for a 26-hour, 53-minute total.
        /// </summary>
        public static string FormatTimeSpan(TimeSpan value)
        {
            var totalHours = (int)value.TotalHours;
            return $"{totalHours:00}:{value.Minutes:00}";
        }

        /// <summary>
        /// Nullable overload -- returns an empty string when there's no value
        /// (e.g. WeeklyTime on days that aren't the last day of the week).
        /// </summary>
        public static string FormatTimeSpan(TimeSpan? value) =>
            value.HasValue ? FormatTimeSpan(value.Value) : string.Empty;
    }
}
