using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ApplicationDbContext _context;

    public PostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BlogPost?> GetPostByIdAsync(int postId)
    {
        return await _context.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == postId);
    }

    public async Task DeletePostAsync(BlogPost post)
    {
        _context.BlogPosts.Remove(post);
        await _context.SaveChangesAsync();
    }
    //
}