using TransportationManagementSystem.UtilityClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransportationManagementSystem.Tests.Unit.Utilities
{
    public class TimeCalculatorTests
    {
        private static GroupAccumulators MakeAccForStartTime(string actualStart, string scheduledPickup)
        {
            var acc = new GroupAccumulators();
            acc.TripActualStart.Add(TimeSpan.Parse(actualStart));
            acc.ScheduledPickup.Add(TimeSpan.Parse(scheduledPickup));
            return acc;
        }

        private static GroupAccumulators MakeAccForEndTime(string clockOut, string maxDropOff)
        {
            var acc = new GroupAccumulators();
            acc.TripActualEnd.Add(TimeSpan.Parse(clockOut));
            acc.ActualDropOff.Add(TimeSpan.Parse(maxDropOff));
            return acc;
        }

        // ---------- CalculateStartTime ----------

        [Fact]
        public void CalculateStartTime_ScheduledMinus30IsLaterThanActualStart_ReturnsScheduledMinus30()
        {
            // ScheduledPickup 08:30 - 30min = 08:00, which is later than ActualStart 07:30
            var acc = MakeAccForStartTime(actualStart: "07:30:00", scheduledPickup: "08:30:00");

            var result = TimeCalculator.CalculateStartTime(acc);

            Assert.Equal(TimeSpan.Parse("08:00:00"), result);
        }

        [Fact]
        public void CalculateStartTime_ActualStartIsLaterThanScheduledMinus30_ReturnsActualStart()
        {
            // ScheduledPickup 08:30 - 30min = 08:00, but ActualStart is 08:15 (later)
            var acc = MakeAccForStartTime(actualStart: "08:15:00", scheduledPickup: "08:30:00");

            var result = TimeCalculator.CalculateStartTime(acc);

            Assert.Equal(TimeSpan.Parse("08:15:00"), result);
        }

        // ---------- CalculateEndTime ----------

        [Fact]
        public void CalculateEndTime_ClockOutBeforeMaxDropoff_BadData_UsesDropoffPlusThirty()
        {
            // Clock-out (16:00) is before the latest dropoff (17:00) — treated as bad data
            var acc = MakeAccForEndTime(clockOut: "16:00:00", maxDropOff: "17:00:00");

            var result = TimeCalculator.CalculateEndTime(acc);

            Assert.Equal(TimeSpan.Parse("17:30:00"), result);
        }

        [Fact]
        public void CalculateEndTime_ClockOutWellAfterMaxDropoff_CapsAtDropoffPlusThirty()
        {
            // Clock-out (20:00) is far later than dropoff+30 (17:30) — capped
            var acc = MakeAccForEndTime(clockOut: "20:00:00", maxDropOff: "17:00:00");

            var result = TimeCalculator.CalculateEndTime(acc);

            Assert.Equal(TimeSpan.Parse("17:30:00"), result);
        }

        [Fact]
        public void CalculateEndTime_ClockOutBetweenDropoffAndDropoffPlusThirty_UsesClockOut()
        {
            // Clock-out (17:15) is after dropoff (17:00) but before dropoff+30 (17:30)
            var acc = MakeAccForEndTime(clockOut: "17:15:00", maxDropOff: "17:00:00");

            var result = TimeCalculator.CalculateEndTime(acc);

            Assert.Equal(TimeSpan.Parse("17:15:00"), result);
        }

        // ---------- FormatWeeklyTime ----------

        [Fact]
        public void FormatWeeklyTime_UnderTenMinutes_PadsWithLeadingZero()
        {
            var result = TimeCalculator.FormatWeeklyTime(TimeSpan.Parse("7:05:00"));

            Assert.Equal("7:05", result);
        }

        [Fact]
        public void FormatWeeklyTime_TenOrMoreMinutes_NoPadding()
        {
            var result = TimeCalculator.FormatWeeklyTime(TimeSpan.Parse("7:45:00"));

            Assert.Equal("7:45", result);
        }

        [Fact]
        public void FormatWeeklyTime_OverTwentyFourHours_AddsDaysAsHours()
        {
            // 1 day, 2 hours, 5 minutes => 24 + 2 = 26 hours
            var result = TimeCalculator.FormatWeeklyTime(new TimeSpan(1, 2, 5, 0));

            Assert.Equal("26:05", result);
        }
    }

}
