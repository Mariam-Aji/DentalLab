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


        var completePost = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(savedPost.Id);

        var adminId = await _blogRepo.GetAdminIdAsync();
        if (adminId.HasValue)
        {
            await _blogRepo.SaveNotificationAsync(new Notification
            {
                RecipientId = adminId.Value,
                BlogPostId = savedPost.Id,
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
            AuthorName = completePost?.Author != null ? completePost.Author.Name : "طبيب معروف", // 🎯 إظهار اسم الكاتب المطلوب
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
            BlogPostId = post.Id,
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
        // 1. جلب المنشور مع المرفقات للتأكد من وجوده
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null) return (false, "المنشور المحدد غير موجود.");

        int targetDoctorId = post.AuthorId;
        string postTitle = post.Title;

        // 2. تحديث حالة المنشور إلى Rejected (والتي تساوي قيمتها 2 في الـ enum)
        post.Status = BlogPostStatus.Rejected;

        // 3. حفظ التعديل في قاعدة البيانات عبر الـ Repository الحالية
        var isUpdated = await _blogRepo.UpdateBlogPostAsync(post);
        if (!isUpdated) return (false, "حدث خطأ أثناء محاولة تحديث حالة المنشور إلى مرفوض.");

        // 4. تحديث إشعار الأدمن المعلق القديم واعتياره مقروءاً
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

        // 5. إرسال إشعار للطبيب لإعلامه بالرفض (مع إبقاء الـ BlogPostId مربوطاً ليتمكن من رؤيته وتعديله لاحقاً)
        await _blogRepo.SaveNotificationAsync(new Notification
        {
            RecipientId = targetDoctorId,
            BlogPostId = post.Id, // 🎯 أصبحت آمنة الآن لأن المنشور لم يُحذف
            Message = $"🛑 نعتذر منك، لقد تم رفض نشر مقالك الطبي المعنون بـ '{postTitle}' لمخالفته شروط المراجعة، يمكنك تعديله وإعادة إرساله.",
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
                AuthorName = post.Author != null ? post.Author.Name : "طبيب معروف",
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
                BlogPostId = post.Id,
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
            AuthorName = post.Author != null ? post.Author.Name : "طبيب معروف",
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