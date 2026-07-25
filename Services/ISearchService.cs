using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public interface ISearchService
{
    Task<List<object>> SearchLabsByNameAsync(string labName, int? currentUserId);
    Task<List<object>> SearchBlogPostsAsync(string query);
}