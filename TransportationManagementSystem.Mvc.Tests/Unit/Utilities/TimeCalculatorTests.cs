using TransportationManagementSystem.Mvc.Utilities;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Utilities
{
    public class TimeCalculatorTests
    {
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
