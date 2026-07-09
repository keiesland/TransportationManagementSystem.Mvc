using TransportationManagementSystem.Mvc.Entities;

namespace TransportationManagementSystem.Mvc.Utilities
{
    public static class SummaryBuilder
    {
        public static Summary Build(
            DriverDayGroup group,
            List<TripDate> dates,
            ref TimeSpan weeklyTime)
        {
            var day = group.DriverDay;
            var tripDate = dates.Find(d => d.TripDateId == group.TripDateId);

            var summary = new Summary
            {
                DriverId = group.DriverId,
                TripDateId = group.TripDateId,
                TripDate = tripDate,
                Start = day.Start,
                End = day.End
            };

            ApplyBreaks(summary, day.Breaks);

            var totalBreakTime = TimeSpan.FromMinutes(
                day.Breaks.Sum(b => (b.In - b.Out).TotalMinutes));
            summary.ActualTime = (summary.End - summary.Start) - totalBreakTime;

            weeklyTime = weeklyTime.Add(summary.ActualTime);
            summary.WeeklyTime = string.Empty;

            if (group.ForceWeeklyReset)
            {
                summary.WeeklyTime = TimeCalculator.FormatWeeklyTime(weeklyTime);
                weeklyTime = TimeSpan.Zero;
            }

            return summary;
        }

        private static void ApplyBreaks(Summary summary, List<(TimeSpan Out, TimeSpan In)> breaks)
        {
            for (var i = 0; i < breaks.Count && i < 4; i++)
            {
                switch (i)
                {
                    case 0: summary.Out1 = breaks[i].Out; summary.In1 = breaks[i].In; break;
                    case 1: summary.Out2 = breaks[i].Out; summary.In2 = breaks[i].In; break;
                    case 2: summary.Out3 = breaks[i].Out; summary.In3 = breaks[i].In; break;
                    case 3: summary.Out4 = breaks[i].Out; summary.In4 = breaks[i].In; break;
                }
            }
        }
    }

}
