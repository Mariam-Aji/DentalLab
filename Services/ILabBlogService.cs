using DentalLab.Api.Dtos;
using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

public interface ILabBlogService
{
    Task<(BlogPostResponseDto? result, string? error)> CreateLabPostAsync(CreatePostDto dto, int labId);
    Task<(BlogPostResponseDto? result, string? error)> UpdateLabPostAsync(int postId, UpdatePostDto dto, int labId);
    Task<List<BlogPostResponseDto>> GetLabApprovedPostsAsync(int labId);
    Task<List<BlogPostResponseDto>> GetLabPendingPostsAsync(int labId);
    Task<List<BlogPostResponseDto>> GetLabRejectedPostsAsync(int labId);
    Task<List<BlogPostResponseDto>> GetAllApprovedLabPostsAsync();
    Task<List<BlogPostResponseDto>> GetApprovedDoctorPostsAsync();
    Task<(bool success, string? error)> DeleteLabPostAsync(int postId, int labId);
    Task<(object? Data, string? Error)> SearchBlogPostsServiceAsync(string searchTerm);
}