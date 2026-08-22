using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DentalLab.Api.Data;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public class SearchRepository : ISearchRepository
{
    private readonly ApplicationDbContext _context;

    public SearchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Lab>> SearchLabsByNameAsync(string labName)
    {
        return await _context.Labs
            .Include(l => l.Owner)
            .Include(l => l.Prices)
            .Include(l => l.ConnectionRequests) // 👈 جلب علاقات الاتصال للتحقق منها
            .Where(l => (l.Owner.Name != null && l.Owner.Name.Contains(labName)) ||
                        (l.Owner.NamePlace != null && l.Owner.NamePlace.Contains(labName)))
            .ToListAsync();
    }

    public async Task<List<BlogPost>> SearchBlogPostsAsync(string query)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Approved &&
                       (b.Title.Contains(query) ||
                        b.Content.Contains(query) ||
                        (b.Author != null && (
                            b.Author.Name.Contains(query) ||
                            (b.Author.NamePlace != null && b.Author.NamePlace.Contains(query))
                        ))))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}