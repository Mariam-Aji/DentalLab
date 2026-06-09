using DentalLab.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

public interface IBlogRepository
{
    Task<BlogPost> SaveBlogPostAsync(BlogPost post);
    Task<BlogPost?> GetBlogPostWithAttachmentsByIdAsync(int postId);
    Task<bool> UpdateBlogPostAsync(BlogPost post);
    Task<List<BlogPost>> GetBlogPostsByAuthorIdAsync(int authorId);
    Task<int?> GetAdminIdAsync();
    Task SaveNotificationAsync(Notification notification);

    Task<bool> UpdateNotificationAsync(Notification notification);

    Task<Notification?> GetNotificationByPostTitleAsync(int adminId, string postTitle);
    Task<IEnumerable<BlogPost>> GetPendingDoctorPostsAsync();
    Task<bool> DeleteBlogPostAsync(BlogPost post);

    Task<List<Notification>> GetNotificationsByRecipientIdAsync(int recipientId);
}