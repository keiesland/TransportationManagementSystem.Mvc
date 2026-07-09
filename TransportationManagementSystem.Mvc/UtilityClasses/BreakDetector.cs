namespace TransportationManagementSystem.UtilityClasses
{
    public static class BreakDetector
    {
        /// <summary>
        /// Detects break periods (out/in pairs) between consecutive trips in a
        /// sorted group. A break is recorded whenever the gap between one
        /// trip's dropoff and the next trip's pickup exceeds roughly one hour.
        /// Logic is copied verbatim from the original — no business rule changes.
        /// </summary>
        public static List<TimeSpan> DetectBreaks(GroupAccumulators acc)
        {
            var inOutList = new List<TimeSpan>();
            var checkList = new List<TimeSpan>();

            for (int b = 0; b < acc.ActualPickup.Count; b++)
            {
                if (b == 0) continue;

                if (acc.ActualPickup[b] > acc.ActualDropOff[b - 1])
                {
                    var gap = acc.ActualPickup[b].Subtract(acc.ActualDropOff[b - 1]);
                    if (gap.Hours == 1 && gap.Minutes > 0 || gap.Hours > 1)
                    {
                        inOutList.Add(acc.ActualDropOff[b - 1]);

                        checkList.Clear();
                        checkList.Add(acc.PickupArrival[b]);
                        checkList.Add(acc.ScheduledPickup[b]);
                        checkList.Sort();

                        inOutList.Add(checkList[1].Subtract(TimeSpan.FromMinutes(30)));
                    }
                }
            }

            return inOutList;
        }
    }

}
