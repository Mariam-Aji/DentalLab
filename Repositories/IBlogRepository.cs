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
    Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm);
    Task<IEnumerable<BlogPost>> GetPendingLabPostsAsync();
    Task<IEnumerable<BlogPost>> GetPendingAllPostsAsync();
    Task<IEnumerable<BlogPost>> GetApprovedDoctorPostsAsync();
    Task<IEnumerable<BlogPost>> GetApprovedLabPostsAsync();
    Task<IEnumerable<BlogPost>> GetApprovedAllPostsAsync();
    Task<IEnumerable<BlogPost>> GetRejectedDoctorPostsAsync();
    Task<IEnumerable<BlogPost>> GetRejectedLabPostsAsync();
    Task<IEnumerable<BlogPost>> GetRejectedAllPostsAsync();
    Task<IEnumerable<BlogPost>> GetPendingPostsByDoctorIdAsync(int doctorId);
    Task<IEnumerable<BlogPost>> GetRejectedPostsByDoctorIdAsync(int doctorId);
    Task<bool> DeleteDoctorPostAsync(int postId);
}