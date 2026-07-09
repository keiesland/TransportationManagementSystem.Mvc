namespace TransportationManagementSystem.UtilityClasses
{
    public static class TimeCalculator
    {
        /// <summary>
        /// Calculates the day's start time: 30 minutes before the earliest
        /// scheduled pickup, unless the earliest actual start time is even
        /// earlier than that, in which case the actual start wins.
        ///
        /// NOTE: the original had a second branch here that read a
        /// "zeroSaveStart" variable which was never set to anything but
        /// TimeSpan.Zero anywhere in the original method — meaning that
        /// branch was confirmed dead code. It's been removed here. Flagging
        /// this explicitly: if you ever find code elsewhere that was meant to
        /// set zeroSaveStart and never got wired up, that's a separate gap
        /// worth investigating — this removal assumes that branch was simply
        /// unreachable, not that it represented intended-but-broken behavior.
        /// </summary>
        public static TimeSpan CalculateStartTime(GroupAccumulators acc)
        {
            var earliestScheduledMinus30 = acc.ScheduledPickup[0].Subtract(TimeSpan.FromMinutes(30));
            var earliestActualStart = acc.TripActualStart[0];

            return earliestScheduledMinus30 > earliestActualStart
                ? earliestScheduledMinus30
                : earliestActualStart;
        }

        /// <summary>
        /// Calculates the day's end time: the latest dropoff plus 30 minutes,
        /// capped against (but not exceeding) the recorded clock-out time —
        /// unless the clock-out time is earlier than the latest dropoff itself
        /// (bad data), in which case it's ignored and dropoff+30 is used as
        /// the effective clock-out for the comparison. Logic copied verbatim
        /// from the original.
        /// </summary>
        public static TimeSpan CalculateEndTime(GroupAccumulators acc)
        {
            var clockOut = acc.TripActualEnd[0]; // same value regardless of index
            var maxDropOff = acc.ActualDropOff[^1]; // largest dropoff after sort

            var validClockOut = clockOut > maxDropOff
                ? clockOut
                : maxDropOff.Add(TimeSpan.FromMinutes(30));

            var checkEnd = maxDropOff.Add(TimeSpan.FromMinutes(30));
            return checkEnd < validClockOut ? checkEnd : validClockOut;
        }

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
