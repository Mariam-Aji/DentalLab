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
    }
}