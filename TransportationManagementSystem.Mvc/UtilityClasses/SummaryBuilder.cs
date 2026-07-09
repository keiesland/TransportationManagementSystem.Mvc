using TransportationManagementSystem.Models;

namespace TransportationManagementSystem.UtilityClasses
{
    public static class SummaryBuilder
    {
        /// <summary>
        /// Builds one Summary for a driver/day group. Accumulates this group's
        /// worked time into the running weeklyTime total (passed by ref so it
        /// carries across consecutive calls for the same driver/week), and
        /// formats/resets it when group.ForceWeeklyReset is true.
        /// </summary>
        public static Summary Build(
            TripGrouping.DriverDayGroup group,
            GroupAccumulators acc,
            List<Driver> drivers,
            List<TripDate> dates,
            ref TimeSpan weeklyTime)
        {
            var inOutList = BreakDetector.DetectBreaks(acc);

            var driver = drivers.Find(d => d.DriverId == group.DriverId);
            var tripDate = dates.Find(d => d.TripDateId == group.TripDateId);

            var summary = new Summary
            {
                DriverId = group.DriverId,
                Driver = driver,
                TripDateId = group.TripDateId,
                TripDate = tripDate,
                Start = TimeCalculator.CalculateStartTime(acc)
            };

            ApplyBreaks(summary, inOutList);

            summary.End = TimeCalculator.CalculateEndTime(acc);
            summary.ActualTime = CalculateActualTime(summary, inOutList.Count);

            weeklyTime = weeklyTime.Add(summary.ActualTime);
            summary.WeeklyTime = string.Empty;

            if (group.ForceWeeklyReset)
            {
                summary.WeeklyTime = TimeCalculator.FormatWeeklyTime(weeklyTime);
                weeklyTime = TimeSpan.Zero;
            }

            return summary;
        }

        private static void ApplyBreaks(Summary summary, List<TimeSpan> inOutList)
        {
            for (var m = 0; m < inOutList.Count; m++)
            {
                switch (m)
                {
                    case 0: summary.Out1 = inOutList[m]; break;
                    case 1: summary.In1 = inOutList[m]; break;
                    case 2: summary.Out2 = inOutList[m]; break;
                    case 3: summary.In2 = inOutList[m]; break;
                    case 4: summary.Out3 = inOutList[m]; break;
                    case 5: summary.In3 = inOutList[m]; break;
                    case 6: summary.Out4 = inOutList[m]; break;
                    case 7: summary.In4 = inOutList[m]; break;
                }
            }
        }

        /// <summary>
        /// Sums the worked-time segments between Start/Out/In/End, depending on
        /// how many breaks occurred. Copied verbatim from the original switch.
        /// </summary>
        private static TimeSpan CalculateActualTime(Summary summary, int breakCount)
        {
            var sum1 = TimeSpan.Zero;
            var sum2 = TimeSpan.Zero;
            var sum3 = TimeSpan.Zero;
            var sum4 = TimeSpan.Zero;
            var sum5 = TimeSpan.Zero;

            switch (breakCount)
            {
                case 0:
                    sum1 = summary.End.Subtract(summary.Start);
                    break;
                case 2:
                    sum1 = summary.Out1.Subtract(summary.Start);
                    sum2 = summary.End.Subtract(summary.In1);
                    break;
                case 4:
                    sum1 = summary.Out1.Subtract(summary.Start);
                    sum2 = summary.Out2.Subtract(summary.In1);
                    sum3 = summary.End.Subtract(summary.In2);
                    break;
                case 6:
                    sum1 = summary.Out1.Subtract(summary.Start);
                    sum2 = summary.Out2.Subtract(summary.In1);
                    sum3 = summary.Out3.Subtract(summary.In2);
                    sum4 = summary.End.Subtract(summary.In3);
                    break;
                case 8:
                    sum1 = summary.Out1.Subtract(summary.Start);
                    sum2 = summary.Out2.Subtract(summary.In1);
                    sum3 = summary.Out3.Subtract(summary.In2);
                    sum4 = summary.Out4.Subtract(summary.In3);
                    sum5 = summary.End.Subtract(summary.In4);
                    break;
            }

            return sum1.Add(sum2).Add(sum3).Add(sum4).Add(sum5);
        }
    }

}
