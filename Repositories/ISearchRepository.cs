using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ISearchRepository
{
    Task<List<Lab>> SearchLabsByNameAsync(string labName);
    Task<List<BlogPost>> SearchBlogPostsAsync(string query);
}