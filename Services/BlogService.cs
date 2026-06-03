using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public class BlogService : IBlogService
{
    private readonly IBlogRepository _blogRepo;
    private readonly IWebHostEnvironment _env;

    public BlogService(IBlogRepository blogRepo, IWebHostEnvironment env)
    {
        _blogRepo = blogRepo;
        _env = env;
    }

    public async Task<(BlogPostResponseDto? result, string? error)> CreateDoctorPostAsync(CreatePostDto dto, int doctorId)
    {
        var attachments = new List<FileResource>();

        if (dto.DocumentFiles != null && dto.DocumentFiles.Any())
        {
            var blogUploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "blogs", doctorId.ToString());
            Directory.CreateDirectory(blogUploadsFolder);

            foreach (var file in dto.DocumentFiles)
            {
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedExtensions.Contains(ext))
                    return (null, $"الامتداد {ext} غير مسموح به.");

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(blogUploadsFolder, fileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = Path.Combine("uploads", "blogs", doctorId.ToString(), fileName).Replace("\\", "/");

                attachments.Add(new FileResource
                {
                    Path = relativePath,
                    Type = FileType.Other,
                    UploadedAt = DateTime.UtcNow
                });
            }
        }

        var blogPost = new BlogPost
        {
            Title = dto.Title,
            Content = dto.Content,
            Type = BlogPostType.CommunityDiscussionDoctor,
            AuthorId = doctorId,
            IsSensitiveRedacted = dto.IsSensitiveRedacted,
            Attachments = attachments,
            Status = BlogPostStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var savedPost = await _blogRepo.SaveBlogPostAsync(blogPost);

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            await _blogRepo.SaveNotificationAsync(new Notification
            {
                RecipientId = adminId.Value,
                Message = $"طلب موافقة: قام الطبيب بكتابة منشور جديد بعنوان '{savedPost.Title}' بانتظار مراجعتك.",
                Type = NotificationType.StatusChanged,
                IsRead = false
            });
        }

        var response = new BlogPostResponseDto
        {
            PostId = savedPost.Id,
            Title = savedPost.Title,
            Content = savedPost.Content,
            Type = savedPost.Type.ToString(),
            AuthorId = savedPost.AuthorId,
            IsSensitiveRedacted = savedPost.IsSensitiveRedacted,
            Status = "Pending",
            ReviewMessage = "المنشور معلق بانتظار موافقة الأدمن ليتم نشره في العلن.",
            CreatedAt = savedPost.CreatedAt,
            Attachments = savedPost.Attachments.Select(a => new BlogAttachmentDto { Id = a.Id, Path = a.Path, Type = a.Type.ToString(), UploadedAt = a.UploadedAt, BlogPostId = savedPost.Id }).ToList()
        };

        return (response, null);
    }

    public async Task<(BlogPostResponseDto? result, string? error)> ApprovePostAsync(int postId)
    {
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null)
        {
            return (null, "المنشور المحدد غير موجود.");
        }

        int targetDoctorId = post.AuthorId;
        string postTitle = post.Title;

        post.Status = BlogPostStatus.Approved;
        var isUpdated = await _blogRepo.UpdateBlogPostAsync(post);
        if (!isUpdated)
        {
            return (null, "حدث خطأ أثناء محاولة تحديث حالة المنشور في قاعدة البيانات.");
        }

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            var pendingNotification = await _blogRepo.GetNotificationByPostTitleAsync(adminId.Value, postTitle);
            if (pendingNotification != null)
            {
                pendingNotification.IsRead = true;
                await _blogRepo.UpdateNotificationAsync(pendingNotification);
            }
        }

        await _blogRepo.SaveNotificationAsync(new Notification
        {
            RecipientId = targetDoctorId, 
            Message = $"🎉 تهانينا! تمت الموافقة على نشر منشورك بعنوان '{postTitle}' وهو متاح للعامة الآن.",
            Type = NotificationType.StatusChanged,
            IsRead = false
        });

        var response = new BlogPostResponseDto
        {
            PostId = post.Id,
            Title = post.Title,
            Content = post.Content,
            Type = post.Type.ToString(),
            AuthorId = post.AuthorId,
            AuthorName = post.Author != null ? post.Author.Name : "طبيب معروف",
            IsSensitiveRedacted = post.IsSensitiveRedacted,
            Status = "Approved",
            ReviewMessage = "تم قبول المنشور ونُشر بنجاح!",
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

        return (response, null);
    }

    public async Task<(bool success, string? error)> RejectPostAsync(int postId)
    {
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null) return (false, "المنشور المحدد غير موجود.");

        if (post.Attachments != null && post.Attachments.Any())
        {
            foreach (var attachment in post.Attachments)
            {
                var fullPath = Path.Combine(_env.ContentRootPath, attachment.Path.Replace("/", "\\"));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        int targetDoctorId = post.AuthorId;
        string postTitle = post.Title;

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            var pendingNotification = await _blogRepo.GetNotificationByPostTitleAsync(adminId.Value, postTitle);
            if (pendingNotification != null)
            {
                pendingNotification.IsRead = true;
                await _blogRepo.UpdateNotificationAsync(pendingNotification);
            }
        }

        var deleted = await _blogRepo.DeleteBlogPostAsync(post);
        if (!deleted) return (false, "حدث خطأ أثناء محاولة حذف المنشور من قاعدة البيانات.");

        await _blogRepo.SaveNotificationAsync(new Notification
        {
            RecipientId = targetDoctorId,
            Message = $"🛑 نعتذر منك، لقد تم رفض نشر مقالك الطبي المعنون بـ '{postTitle}' لمخالفته شروط المراجعة وتم حذفه.",
            Type = NotificationType.StatusChanged,
            IsRead = false
        });

        return (true, null);
    }

    public async Task<List<BlogPostResponseDto>> GetDoctorPostsAsync(int doctorId)
    {
        var posts = await _blogRepo.GetBlogPostsByAuthorIdAsync(doctorId);
        var resultList = new List<BlogPostResponseDto>();

        foreach (var post in posts)
        {
            resultList.Add(new BlogPostResponseDto
            {
                PostId = post.Id,
                Title = post.Title,
                Content = post.Content,
                Type = post.Type.ToString(),
                AuthorId = post.AuthorId,
                IsSensitiveRedacted = post.IsSensitiveRedacted,
                Status = post.Status.ToString(),
                ReviewMessage = post.Status == BlogPostStatus.Pending ? "معلق بانتظار المراجعة" : "منشور علني ومقبول",
                CreatedAt = post.CreatedAt,
                Attachments = post.Attachments.Select(a => new BlogAttachmentDto { Id = a.Id, Path = a.Path, Type = a.Type.ToString(), UploadedAt = a.UploadedAt, BlogPostId = post.Id }).ToList()
            });
        }

        return resultList;
    }

    public async Task<(BlogPostResponseDto? result, string? error)> UpdateDoctorPostAsync(int postId, UpdatePostDto dto, int doctorId)
    {
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null) return (null, "المنشور المحدد غير موجود.");
        if (post.AuthorId != doctorId) return (null, "غير مصرح لك بتعديل هذا المنشور.");

        if (!string.IsNullOrWhiteSpace(dto.Title)) post.Title = dto.Title;
        if (!string.IsNullOrWhiteSpace(dto.Content)) post.Content = dto.Content;
        if (dto.IsSensitiveRedacted.HasValue) post.IsSensitiveRedacted = dto.IsSensitiveRedacted.Value;

        post.Status = BlogPostStatus.Pending;

        if (dto.NewDocumentFiles != null && dto.NewDocumentFiles.Any())
        {
            post.Attachments.Clear();
            var blogUploadsFolder = Path.Combine(_env.ContentRootPath, "uploads", "blogs", doctorId.ToString());
            Directory.CreateDirectory(blogUploadsFolder);

            foreach (var file in dto.NewDocumentFiles)
            {
                if (file.Length == 0) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowedExtensions.Contains(ext)) return (null, $"الامتداد {ext} غير مسموح به.");

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(blogUploadsFolder, fileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = Path.Combine("uploads", "blogs", doctorId.ToString(), fileName).Replace("\\", "/");
                post.Attachments.Add(new FileResource { Path = relativePath, Type = FileType.Other, UploadedAt = DateTime.UtcNow });
            }
        }

        var success = await _blogRepo.UpdateBlogPostAsync(post);
        if (!success) return (null, "لم يتم إجراء أي تغييرات.");

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            await _blogRepo.SaveNotificationAsync(new Notification
            {
                RecipientId = adminId.Value,
                Message = $"تحديث موافقة: قام الطبيب بتعديل منشوره بعنوان '{post.Title}' وهو بانتظار مراجعتك مجدداً.",
                Type = NotificationType.StatusChanged,
                IsRead = false
            });
        }

        var response = new BlogPostResponseDto
        {
            PostId = post.Id,
            Title = post.Title,
            Content = post.Content,
            Type = post.Type.ToString(),
            AuthorId = post.AuthorId,
            IsSensitiveRedacted = post.IsSensitiveRedacted,
            Status = "Pending",
            ReviewMessage = "تم تعديل المنشور بنجاح وإعادته للمراجعة معلقاً.",
            CreatedAt = post.CreatedAt,
            Attachments = post.Attachments.Select(a => new BlogAttachmentDto { Id = a.Id, Path = a.Path, Type = a.Type.ToString(), UploadedAt = a.UploadedAt, BlogPostId = post.Id }).ToList()
        };

        return (response, null);
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetPendingDoctorPostsAsync()
    {
        var posts = await _blogRepo.GetPendingDoctorPostsAsync();

        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : "طبيب غير معروف",
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = "Pending",
            ReviewMessage = "معلق بانتظار المراجعة",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto { Id = a.Id, Path = a.Path, Type = a.Type.ToString(), UploadedAt = a.UploadedAt, BlogPostId = b.Id }).ToList()
        });
    }

    public async Task<List<Notification>> GetNotificationsByRecipientIdAsync(int recipientId)
    {
        return await _blogRepo.GetNotificationsByRecipientIdAsync(recipientId);
    }
}