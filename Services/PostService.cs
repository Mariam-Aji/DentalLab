using DentalLab.Api.Repositories;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _repository;

    public PostService(IPostRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> DeletePostAsync(int postId)
    {
        var post = await _repository.GetPostByIdAsync(postId);

        if (post == null)
        {
            return false;
        }

        await _repository.DeletePostAsync(post);
        return true;
    }
}