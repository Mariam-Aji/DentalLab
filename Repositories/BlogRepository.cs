using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories
{
    public class BlogRepository : IBlogRepository
    {
        private readonly ApplicationDbContext _context;

        public BlogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BlogPost> SaveBlogPostAsync(BlogPost post)
        {
            await _context.BlogPosts.AddAsync(post);
            await _context.SaveChangesAsync();
            return post;
        }
        public async Task<BlogPost?> GetBlogPostWithAttachmentsByIdAsync(int postId)
        {
            return await _context.BlogPosts
                .Include(b => b.Attachments) 
                .FirstOrDefaultAsync(b => b.Id == postId);
        }

        public async Task<bool> UpdateBlogPostAsync(BlogPost post)
        {
            _context.BlogPosts.Update(post);
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<List<BlogPost>> GetBlogPostsByAuthorIdAsync(int authorId)
        {
            return await _context.BlogPosts
                .Include(b => b.Attachments) 
                .Where(b => b.AuthorId == authorId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
        //
    }
}