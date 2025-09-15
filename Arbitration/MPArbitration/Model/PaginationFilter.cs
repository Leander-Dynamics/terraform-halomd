using System.ComponentModel;

namespace MPArbitration.Model
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
        /// <summary>
        ///  Paged response to hold defualt paged data's 
        /// </summary>
        /// <param name="data"> input data's</param>
        public PagedResponse(IEnumerable<T> data) { Disputes = data; }
        /// <summary>
        /// Paged response to hold paged data's 
        /// </summary>
        /// <param name="data"> input data's</param>
        /// <param name="paging"> Paging informations</param>
        public PagedResponse(IEnumerable<T> data, Paging paging) { Disputes = data; PagerInfo = paging; }

        /// <summary>
        /// Disputes as IEnumerable
        /// </summary>
        public IEnumerable<T>? Disputes { get; set; } = null;

        /// <summary>
        /// To hold pager information
        /// </summary>
        public Paging? PagerInfo { get; set; }
    }

    /// <summary>
    /// Entity to handle pager information
    /// </summary>
    public class Paging
    {
        /// <summary>
        /// Page number
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Next page number if available
        /// </summary>
        public int? NextPage { get; set; }

        /// <summary>
        /// Previous page number
        /// </summary>
        public int? PreviousPage { get; set; }

        /// <summary>
        /// Total number of records
        /// </summary>
        public int? TotalRecords { get; set; }
    }
}
