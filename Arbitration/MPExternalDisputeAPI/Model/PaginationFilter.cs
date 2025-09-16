using System.ComponentModel;

namespace MPExternalDisputeAPI.Model
{
    /// <summary>
    /// Paging filter input 
    /// </summary>
    public class PaginationFilter
    {
        /// <summary>
        /// Page number of the current page 
        /// </summary>
        [DefaultValue(0)]
        public int PageNumber { get; set; }

        /// <summary>
        /// Page size(i.e. Number of records to be retrieved
        /// </summary>
        [DefaultValue(50)]
        public int PageSize { get; set; }

    }

    /// <summary>
    /// Paged response to be return as API get
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResponse<T>
    {
        public PagedResponse(IEnumerable<T> data) { Disputes = data; }
        public PagedResponse(IEnumerable<T> data, Paging paging) { Disputes = data; PagerInfo = paging; }
        public IEnumerable<T>? Disputes { get; set; } = null;
        public Paging? PagerInfo { get; set; } = null;
    }

    public class Paging
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int? NextPage { get; set; }
        public int? PreviousPage { get; set; }
        public int? TotalRecords { get; set; }
    }
}


