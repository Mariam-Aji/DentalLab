using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

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
            .Include(b => b.Author)
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

    public async Task<int?> GetAdminIdAsync()
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
        return admin?.Id;
    }

    public async Task SaveNotificationAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateNotificationAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<Notification?> GetNotificationByPostTitleAsync(int adminId, string postTitle)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.RecipientId == adminId
                                   && n.Message.Contains($"'{postTitle}'"));
    }

    public async Task<IEnumerable<BlogPost>> GetPendingDoctorPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionDoctor && b.Status == BlogPostStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteBlogPostAsync(BlogPost post)
    {
        _context.BlogPosts.Remove(post);

        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<List<Notification>> GetNotificationsByRecipientIdAsync(int recipientId)
    {
        return await _context.Notifications
            .Include(n => n.BlogPost)
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.Id)
            .ToListAsync();
    }
    public async Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<BlogPost>();

        var keywords = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(k => k.Trim().ToLower())
                                 .ToList();

        if (!keywords.Any())
            return new List<BlogPost>();

        var query = _context.BlogPosts
            .Include(b => b.Author)
            .AsNoTracking();

        var parameter = Expression.Parameter(typeof(BlogPost), "b");
        Expression? finalExpression = null;

        var stringContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;

        foreach (var keyword in keywords)
        {
            var keywordConstant = Expression.Constant(keyword, typeof(string));

            var titleProp = Expression.Property(parameter, "Title");
            var titleToLower = Expression.Call(titleProp, toLowerMethod);
            var titleContains = Expression.Call(titleToLower, stringContainsMethod, keywordConstant);

            var contentProp = Expression.Property(parameter, "Content");
            var contentToLower = Expression.Call(contentProp, toLowerMethod);
            var contentContains = Expression.Call(contentToLower, stringContainsMethod, keywordConstant);

            var authorProp = Expression.Property(parameter, "Author");
            var authorNotNull = Expression.NotEqual(authorProp, Expression.Constant(null, typeof(User)));
            var authorNameProp = Expression.Property(authorProp, "Name");
            var authorNameToLower = Expression.Call(authorNameProp, toLowerMethod);
            var authorNameContains = Expression.Call(authorNameToLower, stringContainsMethod, keywordConstant);
            var authorCriteriaCombined = Expression.AndAlso(authorNotNull, authorNameContains);

            var currentKeywordExpression = Expression.OrElse(
                Expression.OrElse(titleContains, contentContains),
                authorCriteriaCombined
            );

            finalExpression = finalExpression == null
                ? currentKeywordExpression
                : Expression.OrElse(finalExpression, currentKeywordExpression);
        }

        if (finalExpression != null)
        {
            var lambda = Expression.Lambda<Func<BlogPost, bool>>(finalExpression, parameter);
            query = query.Where(lambda);
        }

        return await query.ToListAsync();
    }
    public async Task<IEnumerable<BlogPost>> GetPendingLabPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionLab && b.Status == BlogPostStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
    public async Task<IEnumerable<BlogPost>> GetPendingAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => (b.Type == BlogPostType.CommunityDiscussionDoctor || b.Type == BlogPostType.CommunityDiscussionLab)
                        && b.Status == BlogPostStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
    public async Task<IEnumerable<BlogPost>> GetApprovedDoctorPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionDoctor && b.Status == BlogPostStatus.Approved)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetApprovedLabPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionLab && b.Status == BlogPostStatus.Approved)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetApprovedAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => (b.Type == BlogPostType.CommunityDiscussionDoctor || b.Type == BlogPostType.CommunityDiscussionLab)
                        && b.Status == BlogPostStatus.Approved)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
    public async Task<IEnumerable<BlogPost>> GetRejectedDoctorPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionDoctor && b.Status == BlogPostStatus.Rejected)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedLabPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionLab && b.Status == BlogPostStatus.Rejected)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => (b.Type == BlogPostType.CommunityDiscussionDoctor || b.Type == BlogPostType.CommunityDiscussionLab)
                        && b.Status == BlogPostStatus.Rejected)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
    public async Task<bool> DeleteDoctorPostAsync(int postId)
    {
        var post = await _context.BlogPosts
            .FirstOrDefaultAsync(b => b.Id == postId && b.Type == BlogPostType.CommunityDiscussionDoctor);

        if (post == null) return false;

        _context.BlogPosts.Remove(post);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<BlogPost>> GetDoctorAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Type == BlogPostType.CommunityDiscussionDoctor)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

   
    public async Task<IEnumerable<BlogPost>> GetPendingPostsByDoctorIdAsync(int doctorId)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.AuthorId == doctorId
                        && b.Type == BlogPostType.CommunityDiscussionDoctor
                        && b.Status == BlogPostStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedPostsByDoctorIdAsync(int doctorId)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.AuthorId == doctorId
                        && b.Type == BlogPostType.CommunityDiscussionDoctor
                        && b.Status == BlogPostStatus.Rejected)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

}