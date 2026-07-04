using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface ILabBlogRepository
{
    Task<BlogPost> SaveBlogPostAsync(BlogPost post);
    Task<BlogPost?> GetBlogPostWithAttachmentsByIdAsync(int postId);
    Task<bool> UpdateBlogPostAsync(BlogPost post);
    Task<bool> DeleteBlogPostAsync(BlogPost post);
    Task<int?> GetAdminIdAsync();
    Task SaveNotificationAsync(Notification notification);
    Task<bool> UpdateNotificationAsync(Notification notification);
    Task<Notification?> GetNotificationByPostTitleAsync(int adminId, string postTitle);
    Task<List<BlogPost>> GetBlogPostsByAuthorIdAndStatusAsync(int authorId, BlogPostType type, BlogPostStatus? status);
    Task<List<BlogPost>> GetBlogPostsByTypeAndStatusAsync(BlogPostType type, BlogPostStatus? status);
    Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm);
}