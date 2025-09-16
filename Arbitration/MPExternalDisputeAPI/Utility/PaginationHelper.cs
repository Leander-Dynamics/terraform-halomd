using MPExternalDisputeAPI.Model;

namespace MPExternalDisputeAPI.Utility
{
    public class PaginationHelper
    {
        public static PagedResponse<T> CreatePagedResponse<T>(PaginationFilter paginationFilter, List<T> response, int? totalRecords)
        {
            var data = response;
            var nextPage = paginationFilter.PageNumber >= 0 ? paginationFilter.PageNumber + 1 : 0;
            var previousPage = paginationFilter.PageNumber - 1 >= 1 ? paginationFilter.PageNumber - 1 : 0;

            Paging paging = new Paging();
            paging.PageNumber = paginationFilter.PageNumber;
            paging.PageSize = paginationFilter.PageSize;
            paging.TotalRecords = totalRecords;
            paging.NextPage = nextPage;
            paging.PreviousPage = previousPage;

            return new PagedResponse<T>(data, paging);
        }
    }
}
