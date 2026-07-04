using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public class LabBlogService : ILabBlogService
{
    private readonly ILabBlogRepository _blogRepo;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<NotificationHub> _hubContext;

    public LabBlogService(ILabBlogRepository blogRepo, IWebHostEnvironment env, IHubContext<NotificationHub> hubContext)
    {
        _blogRepo = blogRepo;
        _env = env;
        _hubContext = hubContext;
    }

    public async Task<(BlogPostResponseDto? result, string? error)> CreateLabPostAsync(CreatePostDto dto, int labId)
    {
        return await CreatePostAsync(
            dto,
            labId,
            "lab",
            "مخبري",
            "طلب موافقة: قام المخبري بكتابة منشور جديد بعنوان '{0}' بانتظار مراجعتك.",
            "المنشور معلق بانتظار موافقة الأدمن ليتم نشره في العلن.",
            "مخبري معروف");
    }

    public async Task<(BlogPostResponseDto? result, string? error)> UpdateLabPostAsync(int postId, UpdatePostDto dto, int labId)
    {
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null) return (null, "المنشور المحدد غير موجود.");
        if (post.AuthorId != labId) return (null, "غير مصرح لك بتعديل هذا المنشور.");
        if (post.Type != BlogPostType.CommunityDiscussionLab) return (null, "هذا المنشور ليس من نوع المخبري.");

        if (!string.IsNullOrWhiteSpace(dto.Title)) post.Title = dto.Title;
        if (!string.IsNullOrWhiteSpace(dto.Content)) post.Content = dto.Content;
        if (dto.IsSensitiveRedacted.HasValue) post.IsSensitiveRedacted = dto.IsSensitiveRedacted.Value;

        post.Status = BlogPostStatus.Pending;

        if (dto.NewDocumentFiles != null && dto.NewDocumentFiles.Any())
        {
            post.Attachments.Clear();
            var attachmentsResult = await SaveAttachmentsAsync(dto.NewDocumentFiles, labId, "lab");
            if (attachmentsResult.Error != null)
            {
                return (null, attachmentsResult.Error);
            }

            foreach (var attachment in attachmentsResult.Files)
            {
                post.Attachments.Add(attachment);
            }
        }

        var success = await _blogRepo.UpdateBlogPostAsync(post);
        if (!success) return (null, "لم يتم إجراء أي تغييرات.");

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            var message = string.Format("تحديث موافقة: قام المخبري بتعديل منشوره بعنوان '{0}' وهو بانتظار مراجعتك مجدداً.", post.Title);
            await SaveAndBroadcastAdminNotificationAsync(adminId.Value, post.Id, message);
        }

        return (MapPostResponse(post, post.Author?.Name ?? "مخبري معروف", "تم تعديل المنشور بنجاح وإعادته للمراجعة معلقاً."), null);
    }

    public async Task<List<BlogPostResponseDto>> GetLabApprovedPostsAsync(int labId)
    {
        return await GetPostsByAuthorAndStatusAsync(labId, BlogPostType.CommunityDiscussionLab, BlogPostStatus.Approved, "مخبري معروف", "منشور علني ومقبول");
    }

    public async Task<List<BlogPostResponseDto>> GetLabPendingPostsAsync(int labId)
    {
        return await GetPostsByAuthorAndStatusAsync(labId, BlogPostType.CommunityDiscussionLab, BlogPostStatus.Pending, "مخبري معروف", "المنشور معلق بانتظار موافقة الأدمن ليتم نشره في العلن.");
    }

    public async Task<List<BlogPostResponseDto>> GetLabRejectedPostsAsync(int labId)
    {
        return await GetPostsByAuthorAndStatusAsync(labId, BlogPostType.CommunityDiscussionLab, BlogPostStatus.Rejected, "مخبري معروف", "تم رفض المنشور ويمكن تعديله وإعادة إرساله.");
    }

    public async Task<List<BlogPostResponseDto>> GetAllApprovedLabPostsAsync()
    {
        return await GetPostsByTypeAndStatusAsync(BlogPostType.CommunityDiscussionLab, BlogPostStatus.Approved, "مخبري معروف", "منشور علني ومقبول");
    }

    public async Task<List<BlogPostResponseDto>> GetApprovedDoctorPostsAsync()
    {
        return await GetPostsByTypeAndStatusAsync(BlogPostType.CommunityDiscussionDoctor, BlogPostStatus.Approved, "طبيب معروف", "منشور علني ومقبول");
    }

    public async Task<(bool success, string? error)> DeleteLabPostAsync(int postId, int labId)
    {
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null) return (false, "المنشور المحدد غير موجود.");
        if (post.AuthorId != labId) return (false, "غير مصرح لك بحذف هذا المنشور.");
        if (post.Type != BlogPostType.CommunityDiscussionLab) return (false, "هذا المنشور ليس من نوع المخبري.");

        DeletePostFiles(post);
        var success = await _blogRepo.DeleteBlogPostAsync(post);
        return success ? (true, null) : (false, "فشل حذف المنشور.");
    }

    private async Task<(BlogPostResponseDto? result, string? error)> CreatePostAsync(
        CreatePostDto dto,
        int authorId,
        string folderName,
        string authorFallback,
        string adminNotificationTemplate,
        string pendingReviewMessage,
        string authorNameFallback)
    {
        var attachmentsResult = await SaveAttachmentsAsync(dto.DocumentFiles, authorId, folderName);
        if (attachmentsResult.Error != null)
        {
            return (null, attachmentsResult.Error);
        }

        var blogPost = new BlogPost
        {
            Title = dto.Title,
            Content = dto.Content,
            Type = BlogPostType.CommunityDiscussionLab,
            AuthorId = authorId,
            IsSensitiveRedacted = dto.IsSensitiveRedacted,
            Attachments = attachmentsResult.Files,
            Status = BlogPostStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var savedPost = await _blogRepo.SaveBlogPostAsync(blogPost);
        var completePost = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(savedPost.Id);

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            var message = string.Format(adminNotificationTemplate, savedPost.Title);
            await SaveAndBroadcastAdminNotificationAsync(adminId.Value, savedPost.Id, message);
        }

        return (MapPostResponse(savedPost, completePost?.Author?.Name ?? authorNameFallback, pendingReviewMessage), null);
    }

    private async Task<(List<FileResource> Files, string? Error)> SaveAttachmentsAsync(IEnumerable<IFormFile>? files, int authorId, string folderName)
    {
        var attachments = new List<FileResource>();

        if (files == null || !files.Any())
        {
            return (attachments, null);
        }

        var blogUploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "blogs", folderName, authorId.ToString());
        Directory.CreateDirectory(blogUploadsFolder);

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(ext))
            {
                return (attachments, $"الامتداد {ext} غير مسموح به.");
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(blogUploadsFolder, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("uploads", "blogs", folderName, authorId.ToString(), fileName).Replace("\\", "/");

            attachments.Add(new FileResource
            {
                Path = relativePath,
                Type = FileType.Other,
                UploadedAt = DateTime.UtcNow
            });
        }

        return (attachments, null);
    }

    private void DeletePostFiles(BlogPost post)
    {
        if (post.Attachments == null || post.Attachments.Count == 0)
        {
            return;
        }

        foreach (var attachment in post.Attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.Path))
            {
                continue;
            }

            var fullPath = Path.Combine(_env.ContentRootPath, attachment.Path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    private async Task SaveAndBroadcastAdminNotificationAsync(int adminId, int postId, string message)
    {
        await _blogRepo.SaveNotificationAsync(new Notification
        {
            RecipientId = adminId,
            BlogPostId = postId,
            Message = message,
            Type = NotificationType.StatusChanged,
            IsRead = false
        });

        await _hubContext.Clients.User(adminId.ToString()).SendAsync("ReceiveOrderNotification", message);
    }

    private async Task<List<BlogPostResponseDto>> GetPostsByAuthorAndStatusAsync(int authorId, BlogPostType type, BlogPostStatus? status, string authorNameFallback, string reviewMessage)
    {
        var posts = await _blogRepo.GetBlogPostsByAuthorIdAndStatusAsync(authorId, type, status);
        return posts.Select(post => MapPostResponse(post, post.Author?.Name ?? authorNameFallback, reviewMessage)).ToList();
    }

    private async Task<List<BlogPostResponseDto>> GetPostsByTypeAndStatusAsync(BlogPostType type, BlogPostStatus? status, string authorNameFallback, string reviewMessage)
    {
        var posts = await _blogRepo.GetBlogPostsByTypeAndStatusAsync(type, status);
        return posts.Select(post => MapPostResponse(post, post.Author?.Name ?? authorNameFallback, reviewMessage)).ToList();
    }

    private static BlogPostResponseDto MapPostResponse(BlogPost post, string authorName, string reviewMessage)
    {
        return new BlogPostResponseDto
        {
            PostId = post.Id,
            Title = post.Title,
            Content = post.Content,
            Type = post.Type.ToString(),
            AuthorId = post.AuthorId,
            AuthorName = authorName,
            IsSensitiveRedacted = post.IsSensitiveRedacted,
            Status = post.Status.ToString(),
            ReviewMessage = reviewMessage,
            CreatedAt = post.CreatedAt,
            Attachments = post.Attachments.Select(a => new BlogAttachmentDto
            {
                Id = a.Id,
                Path = a.Path,
                Type = a.Type.ToString(),
                UploadedAt = a.UploadedAt,
                BlogPostId = post.Id
            }).ToList()
        };
    }

    public async Task<(object? Data, string? Error)> SearchBlogPostsServiceAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return (null, "يرجى إدخال كلمة مفتاحية للبحث.");

        var posts = await _blogRepo.SearchBlogPostsAsync(searchTerm);

        if (posts == null || posts.Count == 0)
        {
            return (new
            {
                TotalResults = 0,
                Message = "لم يتم العثور على أي مقالات تطابق هذا البحث.",
                CategorizedPosts = new Dictionary<string, object>()
            }, null);
        }

        var categorizedData = posts
            .GroupBy(p => p.Type.ToString())
            .ToDictionary(
                group => group.Key,
                group => group.Select(p => new
                {
                    p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    AuthorName = p.Author != null ? p.Author.Name : "كاتب مجهول",
                    AuthorId = p.AuthorId,
                    Status = p.Status.ToString(),
                    p.CreatedAt
                }).ToList()
            );

        var response = new
        {
            TotalResults = posts.Count,
            SearchQuery = searchTerm,
            CategorizedPosts = categorizedData
        };

        return (response, null);
    }
}
