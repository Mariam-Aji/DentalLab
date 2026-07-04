using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public interface IBlogService
    {
        Task<(BlogPostResponseDto? result, string? error)> CreateDoctorPostAsync(CreatePostDto dto, int doctorId);
        Task<(BlogPostResponseDto? result, string? error)> UpdateDoctorPostAsync(int postId, UpdatePostDto dto, int doctorId);
        Task<List<BlogPostResponseDto>> GetDoctorPostsAsync(int doctorId);

        Task<(BlogPostResponseDto? result, string? error)> ApprovePostAsync(int postId);

        Task<(bool success, string? error)> RejectPostAsync(int postId);
        Task<IEnumerable<BlogPostResponseDto>> GetPendingDoctorPostsAsync();
        Task<List<Notification>> GetNotificationsByRecipientIdAsync(int recipientId);
        Task<(object? Data, string? Error)> SearchBlogPostsServiceAsync(string searchTerm);
        Task<IEnumerable<BlogPostResponseDto>> GetPendingLabPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetPendingAllPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetApprovedDoctorPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetApprovedLabPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetApprovedAllPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetRejectedDoctorPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetRejectedLabPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetRejectedAllPostsAsync();
        Task<IEnumerable<BlogPostResponseDto>> GetPendingPostsByDoctorIdAsync(int doctorId);

        Task<IEnumerable<BlogPostResponseDto>> GetRejectedPostsByDoctorIdAsync(int doctorId);
        Task<bool> DeleteDoctorPostAsync(int postId);

    }
}