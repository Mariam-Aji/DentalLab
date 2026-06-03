using DentalLab.Api.Data;
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

    // لتحديث حالة الإشعار (مثل جعله IsRead = true)
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
        if (post.Attachments != null && post.Attachments.Any())
        {
            _context.FileResources.RemoveRange(post.Attachments);
        }

        _context.BlogPosts.Remove(post);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<Notification>> GetNotificationsByRecipientIdAsync(int recipientId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.Id)
            .ToListAsync();
    }
}