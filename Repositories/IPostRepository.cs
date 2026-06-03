using DentalLab.Api.Models;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

public interface IPostRepository
{
    Task<BlogPost?> GetPostByIdAsync(int postId);
    Task DeletePostAsync(BlogPost post);
    //
}