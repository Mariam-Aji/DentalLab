using DentalLab.Api.Models;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories
{
    public interface IBlogRepository
    {
        Task<BlogPost> SaveBlogPostAsync(BlogPost post);
        Task<BlogPost?> GetBlogPostWithAttachmentsByIdAsync(int postId);
        Task<bool> UpdateBlogPostAsync(BlogPost post);
        Task<List<BlogPost>> GetBlogPostsByAuthorIdAsync(int authorId);
       // 
    }
}