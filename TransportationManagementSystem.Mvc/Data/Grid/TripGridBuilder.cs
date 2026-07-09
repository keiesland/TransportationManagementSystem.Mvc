using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransportationManagementSystem.Data.ExtensionMethods;
using Microsoft.AspNetCore.Http;

namespace TransportationManagementSystem.Data.Grid
{
    // inherits the general purpose GridBuilder class and adds application-specific 
    // methods for loading and clearing filter route segments in route dictionary.
    // Also adds application-specific Boolean flags for sorting and filtering. 
    public class TripGridBuilder : GridBuilder
    {
        // this constructor gets current route data from session
        public TripGridBuilder(ISession sess) : base(sess) { }

        // this constructor stores filtering route segments, as well as
        // the paging and sorting route segments stored by the base constructor
        public TripGridBuilder(ISession sess, TripGridDTO values,
            string defaultSortField) : base(sess, values, defaultSortField)
        {
            // store filter route segments - add filter prefixes if this is initial load
            // of page with default values rather than route values (route values have prefix)
            bool isInitial = values.Driver.IndexOf(FilterPrefix.Driver) == -1;

            routes.DriverFilter = (isInitial) ? FilterPrefix.Driver + values.Driver : values.Driver;
            routes.TripDateFilter = (isInitial) ? FilterPrefix.TripDate + values.TripDate : values.TripDate;
        }

        // load new filter route segments contained in a string array - add filter prefix 
        // to each one. 
        public void LoadFilterSegments(string[] filter)
        {
            routes.DriverFilter = FilterPrefix.Driver + filter[0];
            routes.TripDateFilter = FilterPrefix.TripDate + filter[1];
            routes.PageNumber = 1;
        }


        public void ClearFilterSegments()
        {
            routes.ClearSummaryFilters();
            routes.PageNumber = 1;
        }

        //~~ filter flags ~~//
        string def = TripGridDTO.DefaultFilter;
        public bool IsFilterByDriver => routes.DriverFilter != def;
        public bool IsFilterByTripDate => routes.TripDateFilter != def;

        //~~ sort flags ~~//
        public bool IsSortByDriver =>
            routes.SortField.EqualsNoCase(nameof(Driver));

        public bool IsSortByTripDate =>
            routes.SortField.EqualsNoCase(nameof(TripDate));
    }

}
