using TransportationManagementSystem.Models;

namespace TransportationManagementSystem.UtilityClasses
{
    /// <summary>
    /// The seven parallel time lists for one driver/day group, sorted and
    /// ready for break-detection and start/end-time calculation.
    /// </summary>
    public class GroupAccumulators
    {
        public List<TimeSpan> TripActualStart { get; } = new();
        public List<TimeSpan> ScheduledPickup { get; } = new();
        public List<TimeSpan> PickupArrival { get; } = new();
        public List<TimeSpan> ActualPickup { get; } = new();
        public List<TimeSpan> ActualDropOff { get; } = new();
        public List<TimeSpan> ScheduledDropOff { get; } = new();
        public List<TimeSpan> TripActualEnd { get; } = new();

        public void SortAll()
        {
            TripActualStart.Sort();
            ScheduledPickup.Sort();
            PickupArrival.Sort();
            ActualPickup.Sort();
            ActualDropOff.Sort();
            ScheduledDropOff.Sort();
            TripActualEnd.Sort();
        }
    }

    public static partial class AccumulatorBuilder
    {
        /// <summary>
        /// Builds and sorts the accumulator lists for one group. Mutates each
        /// trip's PickupArrival/ActualPickup/ActualDropoff in place when the
        /// MidGroup auto-fill quirk applies — same side effect the original
        /// code had, preserved here for fidelity.
        /// </summary>
        public static GroupAccumulators Build(TripGrouping.DriverDayGroup group)
        {
            var acc = new GroupAccumulators();

            foreach (var groupedTrip in group.Trips)
            {
                var trip = groupedTrip.Trip;

                if (groupedTrip.Pattern == TripGrouping.AccumPattern.MidGroup)
                {
                    ApplyAutoFillQuirk(trip);
                    AddMidGroup(acc, trip);
                }
                else if (groupedTrip.Pattern == TripGrouping.AccumPattern.LastTripContinuingGroup)
                {
                    AddLastTripContinuingGroup(acc, trip);
                }
                else // NewGroupStart
                {
                    AddNewGroupStart(acc, trip);
                }
            }

            acc.SortAll();
            return acc;
        }

        /// <summary>
        /// If a trip has no recorded PickupArrival/ActualPickup/ActualDropoff at
        /// all but does have a ScheduledPickup, default those three fields from
        /// the schedule. Only ever applied to MidGroup trips in the original code.
        /// </summary>
        private static void ApplyAutoFillQuirk(Trip trip)
        {
            if (trip.PickupArrival == TimeSpan.Zero &&
                trip.ActualPickup == TimeSpan.Zero &&
                trip.ActualDropoff == TimeSpan.Zero &&
                trip.ScheduledPickup != TimeSpan.Zero)
            {
                trip.PickupArrival = trip.ScheduledPickup;
                trip.ActualPickup = trip.ScheduledPickup;
                trip.ActualDropoff = trip.ScheduledDropoff;
            }
        }

        private static void AddMidGroup(GroupAccumulators acc, Trip trip)
        {
            acc.TripActualStart.Add(trip.TripActualStart);
            acc.ScheduledPickup.Add(trip.ScheduledPickup);
            acc.PickupArrival.Add(trip.PickupArrival == TimeSpan.Zero ? trip.ScheduledPickup : trip.PickupArrival);
            acc.ActualPickup.Add(trip.ActualPickup == TimeSpan.Zero ? trip.PickupArrival : trip.ActualPickup);
            acc.ActualDropOff.Add(trip.ActualDropoff == TimeSpan.Zero ? trip.PickupArrival : trip.ActualDropoff);
            acc.ScheduledDropOff.Add(trip.ScheduledDropoff);
            acc.TripActualEnd.Add(trip.TripActualEnd);
        }

        private static void AddLastTripContinuingGroup(GroupAccumulators acc, Trip trip)
        {
            acc.TripActualStart.Add(trip.TripActualStart);
            acc.ScheduledPickup.Add(trip.ScheduledPickup);
            acc.PickupArrival.Add(trip.PickupArrival);
            acc.ActualPickup.Add(trip.ActualPickup == TimeSpan.Zero ? trip.PickupArrival : trip.ActualPickup);
            acc.ActualDropOff.Add(trip.ActualDropoff == TimeSpan.Zero ? trip.PickupArrival : trip.ActualDropoff);
            acc.ScheduledDropOff.Add(trip.ScheduledDropoff);
            acc.TripActualEnd.Add(trip.TripActualEnd);
        }

        private static void AddNewGroupStart(GroupAccumulators acc, Trip trip)
        {
            acc.TripActualStart.Add(trip.TripActualStart);
            acc.ScheduledPickup.Add(trip.ScheduledPickup);
            acc.PickupArrival.Add(trip.PickupArrival);
            acc.ActualPickup.Add(trip.ActualPickup == TimeSpan.Zero ? trip.PickupArrival : trip.ActualPickup);
            acc.ActualDropOff.Add(trip.ActualDropoff == TimeSpan.Zero ? trip.ScheduledDropoff : trip.ActualDropoff);
            acc.ScheduledDropOff.Add(trip.ScheduledDropoff);
            acc.TripActualEnd.Add(trip.TripActualEnd);
        }
    }

}
