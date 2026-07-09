namespace TransportationManagementSystem.Mvc.DomainModels
{
    public class DriverDay
    {
        public string Driver { get; set; }
        public DateTime TripDate { get; set; }
        public int WeekNumber { get; set; }
        public TimeSpan TripActualStart { get; set; }
        public TimeSpan TripActualEnd { get; set; }
        public List<TripSegment> Trips { get; set; } = new();

        public List<TripSegment> WorkingTrips =>
            Trips.Where(t => !t.IsNoShow).ToList();

        public TimeSpan Start
        {
            get
            {
                if (Trips.Count == 0) return TripActualStart;

                // Include no-shows -- the driver was scheduled and dispatched
                // regardless of whether the trip completed.
                var firstScheduledPickup = Trips.Min(t => t.ScheduledPickupTime);
                var scheduledMinusBuffer = firstScheduledPickup - TimeSpan.FromMinutes(30);

                return TripActualStart > scheduledMinusBuffer ? TripActualStart : scheduledMinusBuffer;
            }
        }

        public TimeSpan End
        {
            get
            {
                // No-shows have ActualDropoffTime = 0:00, so they'll never win this
                // Max() unless every trip that day was a no-show -- safe either way.
                var completedTrips = Trips.Where(t => t.ActualDropoffTime != TimeSpan.Zero).ToList();
                if (completedTrips.Count == 0) return TripActualEnd;

                var lastDropoff = completedTrips.Max(t => t.ActualDropoffTime);

                if (TripActualEnd == lastDropoff)
                {
                    return lastDropoff + TimeSpan.FromMinutes(30);
                }

                return TripActualEnd < lastDropoff + TimeSpan.FromMinutes(30)
                    ? TripActualEnd
                    : lastDropoff + TimeSpan.FromMinutes(30);
            }
        }

        public List<(TimeSpan Out, TimeSpan In)> Breaks
        {
            get
            {
                var trips = Trips
                    .OrderBy(t => t.ScheduledPickupTime)
                    .ToList();

                var breaks = new List<(TimeSpan Out, TimeSpan In)>();
                if (trips.Count == 0) return breaks;

                TimeSpan? busyUntil = null;

                foreach (var trip in trips)
                {
                    bool isNoShow = trip.ActualPickupTime == TimeSpan.Zero
                                    && trip.ActualDropoffTime == TimeSpan.Zero;

                    if (busyUntil == null)
                    {
                        busyUntil = isNoShow ? trip.PickupArrivalTime : trip.ActualDropoffTime;
                        continue;
                    }

                    if (isNoShow)
                    {
                        if (trip.PickupArrivalTime > busyUntil)
                            busyUntil = trip.PickupArrivalTime;
                        continue;
                    }

                    var rawGap = trip.ActualPickupTime - busyUntil.Value;

                    if (rawGap > TimeSpan.FromMinutes(60))
                    {
                        var buffer = (trip.ScheduledPickupTime > trip.PickupArrivalTime
                            ? trip.ScheduledPickupTime
                            : trip.PickupArrivalTime) - TimeSpan.FromMinutes(30);

                        var effectivePickup = trip.ActualPickupTime < buffer
                            ? trip.ActualPickupTime
                            : buffer;

                        breaks.Add((busyUntil.Value, effectivePickup));
                    }

                    if (trip.ActualDropoffTime > busyUntil)
                        busyUntil = trip.ActualDropoffTime;
                }

                return breaks;
            }
        }
    }
}
