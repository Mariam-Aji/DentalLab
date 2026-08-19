using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
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
    private readonly IHubContext<NotificationHub> _hubContext; 

    public BlogService(IBlogRepository blogRepo, IWebHostEnvironment env, IHubContext<NotificationHub> hubContext)
    {
        _blogRepo = blogRepo;
        _env = env;
        _hubContext = hubContext;
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
            AuthorName = completePost?.Author != null ? completePost.Author.Name : "طبيب معروف",
            AuthorProfilePictureUrl = completePost?.Author?.ProfilePictureUrl,
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

        int targetUserId = post.AuthorId;
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

        // معالجة الـ Attachments بأمان تام لتلافي أي تحذيرات null
        var attachmentsList = post.Attachments != null
            ? post.Attachments.Select(a => new BlogAttachmentDto
            {
                Id = a.Id,
                Path = a.Path,
                Type = a.Type.ToString(),
                UploadedAt = a.UploadedAt,
                BlogPostId = post.Id
            }).ToList()
            : new List<BlogAttachmentDto>();

        var postResponseDto = new BlogPostResponseDto
        {
            PostId = post.Id,
            Title = post.Title,
            Content = post.Content,
            Type = post.Type.ToString(),
            AuthorId = post.AuthorId,
            AuthorName = post.Author?.Name ?? "مستخدم معروف", // استخدام Null-coalescing لتجنب الـ Null reference
            AuthorProfilePictureUrl = post.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = post.IsSensitiveRedacted,
            Status = "Approved",
            ReviewMessage = "تم قبول المنشور ونُشر بنجاح!",
            CreatedAt = post.CreatedAt,
            Attachments = attachmentsList
        };

        var notification = new Notification
        {
            RecipientId = targetUserId,
            BlogPostId = post.Id,
            Message = $"🎉 تهانينا! تمت الموافقة على نشر مقالك بعنوان '{postTitle}' وهو متاح للعامة الآن.",
            Type = NotificationType.StatusChanged,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _blogRepo.SaveNotificationAsync(notification);

        try
        {
            var notificationPayload = new
            {
                notification.Id,
                notification.Type,
                notification.CreatedAt,
                notification.Message,
                Data = postResponseDto
            };

            // إرسال الإشعار الفوري عبر SignalR باستخدام الحقل المحقون _hubContext بنجاح
            await _hubContext.Clients.User(targetUserId.ToString())
                .SendAsync("ReceiveOrderNotification", notificationPayload);
        }
        catch
        {
            // تجاهل خطأ فشل الإرسال الفوري لكي لا يعطل عملية قبول المنشور الأساسية
        }

        return (postResponseDto, null);
    }

    public async Task<(bool success, string? error)> RejectPostAsync(int postId)
    {
        // 1. جلب المنشور مع المرفقات ومعلومات الكاتب
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null)
        {
            return (false, "المنشور المحدد غير موجود.");
        }

        int targetUserId = post.AuthorId;
        string postTitle = post.Title;

        // 2. تحديث حالة المنشور إلى مرفوض
        post.Status = BlogPostStatus.Rejected;
        var isUpdated = await _blogRepo.UpdateBlogPostAsync(post);
        if (!isUpdated)
        {
            return (false, "حدث خطأ أثناء محاولة تحديث حالة المنشور إلى مرفوض.");
        }

        // 3. تحديث حالة إشعار الأدمن (جعل الإشعار مقروءاً)
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

        // 4. بناء رسالة الإشعار الموجهة لصاحب المنشور للأسباب المتعلقة بالخصوصية وشروط النشر
        string notificationMessage = $"🛑 نعتذر منك، لقد تم رفض نشر مقالك المعنون بـ '{postTitle}' من قبل الإدارة لأسباب تتعلق بسياسة الخصوصية وشروط الاستخدام.";

        var newNotification = new Notification
        {
            RecipientId = targetUserId,
            BlogPostId = post.Id,
            Message = notificationMessage,
            Type = NotificationType.StatusChanged,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        // 5. حفظ الإشعار في قاعدة البيانات
        await _blogRepo.SaveNotificationAsync(newNotification);

        // 6. إرسال الإشعار الفوري عبر SignalR
        try
        {
            var notificationPayload = new
            {
                newNotification.Id,
                newNotification.Type,
                newNotification.CreatedAt,
                newNotification.Message,
                Data = new { PostId = post.Id, Title = post.Title, Status = "Rejected" }
            };

            await _hubContext.Clients.User(targetUserId.ToString())
                .SendAsync("ReceiveOrderNotification", notificationPayload);
        }
        catch
        {
            // تجاهل خطأ فشل الإرسال الفوري لضمان عدم تعطل عملية الرفض الأساسية
        }

        return (true, null);
    }
    public async Task<List<BlogPostResponseDto>> GetDoctorPostsAsync(int doctorId)
    {
        var posts = await _blogRepo.GetBlogPostsByAuthorIdAsync(doctorId);
        var resultList = new List<BlogPostResponseDto>();

        var sortedPosts = posts.OrderByDescending(p => p.CreatedAt);

        foreach (var post in sortedPosts)
        {
            resultList.Add(new BlogPostResponseDto
            {
                PostId = post.Id,
                Title = post.Title,
                Content = post.Content,
                Type = post.Type.ToString(),
                AuthorId = post.AuthorId,
                AuthorName = post.Author != null ? post.Author.Name : "طبيب معروف",
                AuthorProfilePictureUrl = post.Author?.ProfilePictureUrl,
                IsSensitiveRedacted = post.IsSensitiveRedacted,
                Status = post.Status.ToString(),
                ReviewMessage = post.Status == BlogPostStatus.Pending ? "معلق بانتظار المراجعة" :
                                post.Status == BlogPostStatus.Rejected ? "تم رفض المنشور لمخالفته شروط النشر." : "منشور علني ومقبول",
                CreatedAt = post.CreatedAt,
                Attachments = post.Attachments.Select(a => new BlogAttachmentDto
                {
                    Id = a.Id,
                    Path = a.Path,
                    Type = a.Type.ToString(),
                    UploadedAt = a.UploadedAt,
                    BlogPostId = post.Id
                }).ToList()
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
            AuthorProfilePictureUrl = post.Author?.ProfilePictureUrl,
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
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
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
                    AuthorProfilePictureUrl = p.Author?.ProfilePictureUrl,
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

    public async Task<IEnumerable<BlogPostResponseDto>> GetPendingLabPostsAsync()
    {
        var posts = await _blogRepo.GetPendingLabPostsAsync();

        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : "مخبري غير معروف",
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = "Pending",
            ReviewMessage = "معلق بانتظار مراجعة الأدمن",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto
            {
                Id = a.Id,
                Path = a.Path,
                Type = a.Type.ToString(),
                UploadedAt = a.UploadedAt,
                BlogPostId = b.Id
            }).ToList()
        });
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetPendingAllPostsAsync()
    {
        var posts = await _blogRepo.GetPendingAllPostsAsync();

        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : (b.Type == BlogPostType.CommunityDiscussionDoctor ? "طبيب غير معروف" : "مخبري غير معروف"),
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = "Pending",
            ReviewMessage = "معلق بانتظار المراجعة",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto
            {
                Id = a.Id,
                Path = a.Path,
                Type = a.Type.ToString(),
                UploadedAt = a.UploadedAt,
                BlogPostId = b.Id
            }).ToList()
        });
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetApprovedDoctorPostsAsync()
    {
        var posts = await _blogRepo.GetApprovedDoctorPostsAsync();
        return MapToResponseDto(posts, "طبيب غير معروف");
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetApprovedLabPostsAsync()
    {
        var posts = await _blogRepo.GetApprovedLabPostsAsync();
        return MapToResponseDto(posts, "مخبري غير معروف");
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetApprovedAllPostsAsync()
    {
        var posts = await _blogRepo.GetApprovedAllPostsAsync();
        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : (b.Type == BlogPostType.CommunityDiscussionDoctor ? "طبيب غير معروف" : "مخبري غير معروف"),
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = "Approved",
            ReviewMessage = "منشور علني ومقبول",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto { Id = a.Id, Path = a.Path, Type = a.Type.ToString(), UploadedAt = a.UploadedAt, BlogPostId = b.Id }).ToList()
        });
    }

    private IEnumerable<BlogPostResponseDto> MapToResponseDto(IEnumerable<BlogPost> posts, string defaultAuthorName)
    {
        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : defaultAuthorName,
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = "Approved",
            ReviewMessage = "منشور علني ومقبول",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto { Id = a.Id, Path = a.Path, Type = a.Type.ToString(), UploadedAt = a.UploadedAt, BlogPostId = b.Id }).ToList()
        });
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetRejectedDoctorPostsAsync()
    {
        var posts = await _blogRepo.GetRejectedDoctorPostsAsync();
        return MapToResponseDto(posts, "طبيب غير معروف");
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetRejectedLabPostsAsync()
    {
        var posts = await _blogRepo.GetRejectedLabPostsAsync();
        return MapToResponseDto(posts, "مخبري غير معروف");
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetRejectedAllPostsAsync()
    {
        var posts = await _blogRepo.GetRejectedAllPostsAsync();
        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : (b.Type == BlogPostType.CommunityDiscussionDoctor ? "طبيب غير معروف" : "مخبري غير معروف"),
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = "Rejected",
            ReviewMessage = "تم رفض المنشور من قبل إدارة المنصة لمخالفته شروط النشر.",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto
            {
                Id = a.Id,
                Path = a.Path,
                Type = a.Type.ToString(),
                UploadedAt = a.UploadedAt,
                BlogPostId = b.Id
            }).ToList()
        });
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetPendingPostsByDoctorIdAsync(int doctorId)
    {
        var posts = await _blogRepo.GetPendingPostsByDoctorIdAsync(doctorId);
        return MapToResponseDto(posts);
    }

    public async Task<IEnumerable<BlogPostResponseDto>> GetRejectedPostsByDoctorIdAsync(int doctorId)
    {
        var posts = await _blogRepo.GetRejectedPostsByDoctorIdAsync(doctorId);
        return MapToResponseDto(posts);
    }

    private IEnumerable<BlogPostResponseDto> MapToResponseDto(IEnumerable<BlogPost> posts)
    {
        return posts.Select(b => new BlogPostResponseDto
        {
            PostId = b.Id,
            Title = b.Title,
            Content = b.Content,
            Type = b.Type.ToString(),
            AuthorId = b.AuthorId,
            AuthorName = b.Author != null ? b.Author.Name : "طبيب غير معروف",
            AuthorProfilePictureUrl = b.Author?.ProfilePictureUrl,
            IsSensitiveRedacted = b.IsSensitiveRedacted,
            Status = b.Status.ToString(),
            ReviewMessage = b.Status == BlogPostStatus.Rejected
                ? "تم رفض المنشور لمخالفته شروط النشر المعتمدة لدينا."
                : "المنشور معلق بانتظار مراجعة الإدارة.",
            CreatedAt = b.CreatedAt,
            Attachments = b.Attachments.Select(a => new BlogAttachmentDto
            {
                Id = a.Id,
                Path = a.Path,
                BlogPostId = b.Id
            }).ToList()
        });
    }

    public async Task<bool> DeleteDoctorPostAsync(int postId)
    {
        return await _blogRepo.DeleteDoctorPostAsync(postId);
    }
    public async Task<(bool success, string? error)> DeletePostByAdminAsync(int postId)
    {
        // 1. جلب المنشور مع المرفقات ومعلومات الكاتب
        var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
        if (post == null)
        {
            return (false, "المنشور المحدد غير موجود.");
        }

        int targetUserId = post.AuthorId;
        string postTitle = post.Title;

        // 2. فك ارتباط الإشعارات المرتبطة بهذا المنشور أولاً لتجنب خطأ Foreign Key Conflict
        var relatedNotifications = await _blogRepo.GetNotificationsByBlogPostIdAsync(postId);
        if (relatedNotifications != null && relatedNotifications.Any())
        {
            foreach (var notification in relatedNotifications)
            {
                notification.BlogPostId = null; // تفريغ المعرف لكسر قيد الـ FK
                await _blogRepo.UpdateNotificationAsync(notification);
            }
        }

        // 3. حذف الملفات الفيزيائية المرفقة من السيرفر إن وجدت
        if (post.Attachments != null && post.Attachments.Any())
        {
            foreach (var attachment in post.Attachments)
            {
                if (!string.IsNullOrEmpty(attachment.Path))
                {
                    var fullPath = Path.Combine(_env.ContentRootPath, attachment.Path.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (File.Exists(fullPath))
                    {
                        try { File.Delete(fullPath); } catch { }
                    }
                }
            }
        }

        // 4. حذف المنشور من قاعدة البيانات
        var isDeleted = await _blogRepo.DeleteDoctorPostAsync(postId);
        if (!isDeleted)
        {
            return (false, "حدث خطأ أثناء محاولة حذف المنشور من قاعدة البيانات.");
        }

        // 5. إنشاء إشعار جديد يوضح سبب الحذف لصاحب المنشور
        string notificationMessage = $"⚠️ نود إعلامك بأنه تم حذف منشورك الموسوم بـ '{postTitle}' من قبل الإدارة لأسباب تتعلق بسياسة الخصوصية وشروط الاستخدام.";

        var newNotification = new Notification
        {
            RecipientId = targetUserId,
            BlogPostId = null,
            Message = notificationMessage,
            Type = NotificationType.StatusChanged,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _blogRepo.SaveNotificationAsync(newNotification);

        // 6. إرسال الإشعار الفوري عبر SignalR
        try
        {
            var notificationPayload = new
            {
                newNotification.Id,
                newNotification.Type,
                newNotification.CreatedAt,
                newNotification.Message,
                Data = new { PostId = postId, Title = postTitle }
            };

            await _hubContext.Clients.User(targetUserId.ToString())
                .SendAsync("ReceiveOrderNotification", notificationPayload);
        }
        catch
        {
            // تجاهل خطأ فشل الإرسال الفوري لضمان عدم تعطل عملية الحذف
        }

        return (true, null);
    }
}