using DentalLab.Api.Data; // عدل حسب مسار الـ DbContext لديك
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<BlogPost?> GetBlogPostWithAttachmentsByIdAsync(int id)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<bool> UpdateBlogPostAsync(BlogPost post)
    {
        _context.BlogPosts.Update(post);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int?> GetAdminIdAsync()
    {
        var admin = await _context.Users
            .FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
        return admin?.Id;
    }

    public async Task SaveNotificationAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<Notification?> GetNotificationByPostTitleAsync(int recipientId, string postTitle)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.RecipientId == recipientId && n.Message.Contains(postTitle) && !n.IsRead);
    }

    public async Task<bool> UpdateNotificationAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteBlogPostAsync(BlogPost post)
    {
        _context.BlogPosts.Remove(post);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<BlogPost>> GetBlogPostsByAuthorIdAsync(int authorId)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.AuthorId == authorId)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetNotificationsByRecipientIdAsync(int recipientId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.Id)
            .ToListAsync();
    }

    public async Task<List<BlogPost>> SearchBlogPostsAsync(string searchTerm)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Title.Contains(searchTerm) || b.Content.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPendingDoctorPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Pending && b.Type == BlogPostType.CommunityDiscussionDoctor)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPendingLabPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Pending && b.Type == BlogPostType.CommunityDiscussionLab)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPendingAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Pending)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetApprovedDoctorPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Approved && b.Type == BlogPostType.CommunityDiscussionDoctor)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetApprovedLabPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Approved && b.Type == BlogPostType.CommunityDiscussionLab)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetApprovedAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Approved)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedDoctorPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Rejected && b.Type == BlogPostType.CommunityDiscussionDoctor)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedLabPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Rejected && b.Type == BlogPostType.CommunityDiscussionLab)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedAllPostsAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.Status == BlogPostStatus.Rejected)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPendingPostsByDoctorIdAsync(int doctorId)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.AuthorId == doctorId && b.Status == BlogPostStatus.Pending)
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRejectedPostsByDoctorIdAsync(int doctorId)
    {
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Attachments)
            .Where(b => b.AuthorId == doctorId && b.Status == BlogPostStatus.Rejected)
            .ToListAsync();
    }

    public async Task<bool> DeleteDoctorPostAsync(int postId)
    {
        var post = await _context.BlogPosts.FindAsync(postId);
        if (post == null) return false;

        _context.BlogPosts.Remove(post);
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<IEnumerable<Notification>> GetNotificationsByBlogPostIdAsync(int postId)
    {
        return await _context.Notifications
            .Where(n => n.BlogPostId == postId)
            .ToListAsync();
    }
}