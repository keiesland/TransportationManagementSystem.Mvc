using TransportationManagementSystem.UtilityClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransportationManagementSystem.Tests.Unit.Utilities
{
    public class BreakDetectorTests
    {
        private static GroupAccumulators MakeAccumulators(
            (string actualPickup, string actualDropoff, string pickupArrival, string scheduledPickup)[] rows)
        {
            var acc = new GroupAccumulators();

            foreach (var row in rows)
            {
                acc.ActualPickup.Add(TimeSpan.Parse(row.actualPickup));
                acc.ActualDropOff.Add(TimeSpan.Parse(row.actualDropoff));
                acc.PickupArrival.Add(TimeSpan.Parse(row.pickupArrival));
                acc.ScheduledPickup.Add(TimeSpan.Parse(row.scheduledPickup));
            }

            return acc;
        }

        [Fact]
        public void DetectBreaks_SingleTrip_ReturnsEmptyList()
        {
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00")
            });

            var result = BreakDetector.DetectBreaks(acc);

            Assert.Empty(result);
        }

        [Fact]
        public void DetectBreaks_SmallGapUnderOneHour_NoBreakDetected()
        {
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00"),
                ("09:30:00", "10:00:00", "09:25:00", "09:30:00") // 30 min gap
            });

            var result = BreakDetector.DetectBreaks(acc);

            Assert.Empty(result);
        }

        [Fact]
        public void DetectBreaks_ExactlyOneHourGapWithNoExtraMinutes_NoBreakDetected()
        {
            // Boundary case: gap.Hours==1 && gap.Minutes>0 is false here (Minutes==0),
            // and gap.Hours>1 is also false — so exactly 1:00:00 should NOT register
            // as a break under the original threshold logic.
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00"),
                ("10:00:00", "10:30:00", "09:55:00", "10:00:00") // exactly 1hr gap
            });

            var result = BreakDetector.DetectBreaks(acc);

            Assert.Empty(result);
        }

        [Fact]
        public void DetectBreaks_OneHourOneMinuteGap_BreakDetected()
        {
            // Just over the threshold: gap.Hours==1 && gap.Minutes>0 is true
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00"),
                ("10:01:00", "10:30:00", "09:56:00", "10:01:00") // 1hr 1min gap
            });

            var result = BreakDetector.DetectBreaks(acc);

            Assert.Equal(2, result.Count); // one Out/In pair
        }

        [Fact]
        public void DetectBreaks_BreakDetected_OutEqualsThePriorDropoff()
        {
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00"),
                ("11:00:00", "12:00:00", "10:55:00", "11:00:00") // 2hr gap
            });

            var result = BreakDetector.DetectBreaks(acc);

            Assert.Equal(2, result.Count);
            Assert.Equal(TimeSpan.Parse("09:00:00"), result[0]); // Out = prior dropoff
        }

        [Fact]
        public void DetectBreaks_BreakDetected_InIsThirtyMinutesBeforeLaterOfPickupArrivalAndScheduled()
        {
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00"),
                // pickupArrival (10:55) is later than scheduledPickup (10:45) for the second row
                ("11:00:00", "12:00:00", "10:55:00", "10:45:00")
            });

            var result = BreakDetector.DetectBreaks(acc);

            // In should be 30 min before the LATER of pickupArrival/scheduledPickup (10:55 here)
            Assert.Equal(TimeSpan.Parse("10:25:00"), result[1]);
        }

        [Fact]
        public void DetectBreaks_MultipleBreaksInOneDay_ReturnsMultiplePairs()
        {
            var acc = MakeAccumulators(new[]
            {
                ("08:30:00", "09:00:00", "08:25:00", "08:30:00"),
                ("11:00:00", "12:00:00", "10:55:00", "11:00:00"), // break 1 (2hr gap)
                ("14:30:00", "15:00:00", "14:25:00", "14:30:00")  // break 2 (2.5hr gap)
            });

            var result = BreakDetector.DetectBreaks(acc);

            Assert.Equal(4, result.Count); // two Out/In pairs
        }
    }

}
