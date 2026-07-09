using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Data.ExtensionMethods;

namespace TransportationManagementSystem.Data.Grid
{
    public static class FilterPrefix
    {
        // static class of constants used to add and remove user-friendly
        // prefix from filter route segment values. Public class rather
        // than private constants bc also used by GamesGridBuilder class.
        public const string Driver = "driver-";
        public const string TripDate = "date-";
    }

    // inherits dictionary of strings, adds a Clone() method. Adds properties
    // to get and set general paging, sorting, and filtering values from dictionary. 
    // Adds methods to set sort field value and sort direction value based on sort field, re-set filter values.
    public class RideDictionary : Dictionary<string, string>
    {
        public int PageNumber
        {
            get => Get(nameof(GridDTO.PageNumber)).ToInt();
            set => this[nameof(GridDTO.PageNumber)] = value.ToString();
        }

        public int PageSize
        {
            get => Get(nameof(GridDTO.PageSize)).ToInt();
            set => this[nameof(GridDTO.PageSize)] = value.ToString();
        }

        public string SortField
        {
            get => Get(nameof(GridDTO.SortField));
            set => this[nameof(GridDTO.SortField)] = value;
        }

        public string SortDirection
        {
            get => Get(nameof(GridDTO.SortDirection));
            set => this[nameof(GridDTO.SortDirection)] = value;
        }

        public void SetSortAndDirection(string fieldName, RideDictionary current)
        {
            this[nameof(GridDTO.SortField)] = fieldName;

            // set sort direction based on comparison of new and current sort field. if 
            // sort field is same as current, toggle between ascending and descending. 
            // if it's different, should always be ascending.
            if (current.SortField.EqualsNoCase(fieldName) &&
                current.SortDirection == "asc")
                this[nameof(GridDTO.SortDirection)] = "desc";
            else
                this[nameof(GridDTO.SortDirection)] = "asc";
        }

        public string DriverFilter
        {
            get => Get(nameof(TripGridDTO.Driver))?.Replace(FilterPrefix.Driver, "");
            set => this[nameof(TripGridDTO.Driver)] = value;
        }

        public string TripDateFilter
        {
            get => Get(nameof(TripGridDTO.TripDate))?.Replace(FilterPrefix.TripDate, "");
            set => this[nameof(TripGridDTO.TripDate)] = value;
        }

        public string DriverSummaryFilter
        {
            get => Get(nameof(SummaryGridDTO.Driver))?.Replace(FilterPrefix.Driver, "");
            set => this[nameof(SummaryGridDTO.Driver)] = value;
        }

        public string TripDateSummaryFilter
        {
            get => Get(nameof(SummaryGridDTO.TripDate))?.Replace(FilterPrefix.TripDate, "");
            set => this[nameof(SummaryGridDTO.TripDate)] = value;
        }

        public void ClearFilters() =>
            DriverFilter = TripDateFilter = TripGridDTO.DefaultFilter;

        public void ClearSummaryFilters() =>
            DriverSummaryFilter = TripDateSummaryFilter = SummaryGridDTO.DefaultFilter;

        private string Get(string key) => Keys.Contains(key) ? this[key] : null;

        // return a new dictionary that contains the same values as this dictionary.
        // needed so that pages can change the route values when calculating paging, sorting,
        // and filtering links, without changing the values of the current route
        public RideDictionary Clone()
        {
            var clone = new RideDictionary();
            foreach (var key in Keys)
            {
                clone.Add(key, this[key]);
            }
            return clone;
        }
    }

}
