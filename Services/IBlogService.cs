using DentalLab.Api.Dtos;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public interface IBlogService
    {
        Task<(BlogPostResponseDto? result, string? error)> CreateDoctorPostAsync(CreatePostDto dto, int doctorId);
        Task<(BlogPostResponseDto? result, string? error)> UpdateDoctorPostAsync(int postId, UpdatePostDto dto, int doctorId);
        Task<List<BlogPostResponseDto>> GetDoctorPostsAsync(int doctorId);
    }
    //
}