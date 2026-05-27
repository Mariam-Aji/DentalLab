using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
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
                CreatedAt = DateTime.UtcNow
            };
            var savedPost = await _blogRepo.SaveBlogPostAsync(blogPost);

            var response = new BlogPostResponseDto
            {
                PostId = savedPost.Id,
                Title = savedPost.Title,
                Content = savedPost.Content,
                Type = savedPost.Type.ToString(),
                AuthorId = savedPost.AuthorId,
                IsSensitiveRedacted = savedPost.IsSensitiveRedacted,
                CreatedAt = savedPost.CreatedAt,
                Attachments = savedPost.Attachments.Select(a => new BlogAttachmentDto
                {
                    Id = a.Id,
                    Path = a.Path,
                    Type = a.Type.ToString(), 
                    UploadedAt = a.UploadedAt,
                    BlogPostId = savedPost.Id
                }).ToList()
            };

            return (response, null);
        }
        public async Task<(BlogPostResponseDto? result, string? error)> UpdateDoctorPostAsync(int postId, UpdatePostDto dto, int doctorId)
        {
            var post = await _blogRepo.GetBlogPostWithAttachmentsByIdAsync(postId);
            if (post == null)
            {
                return (null, "المنشور المحدد غير موجود.");
            }

            if (post.AuthorId != doctorId)
            {
                return (null, "غير مصرح لك بتعديل هذا المنشور.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                post.Title = dto.Title;
            }

            if (!string.IsNullOrWhiteSpace(dto.Content))
            {
                post.Content = dto.Content;
            }

            if (dto.IsSensitiveRedacted.HasValue)
            {
                post.IsSensitiveRedacted = dto.IsSensitiveRedacted.Value;
            }

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
                    if (!allowedExtensions.Contains(ext))
                        return (null, $"الامتداد {ext} غير مسموح به للملف {file.FileName}.");

                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(blogUploadsFolder, fileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var relativePath = Path.Combine("uploads", "blogs", doctorId.ToString(), fileName).Replace("\\", "/");

                    post.Attachments.Add(new FileResource
                    {
                        Path = relativePath,
                        Type = FileType.Other, 
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            var success = await _blogRepo.UpdateBlogPostAsync(post);
            if (!success)
            {
                return (null, "لم يتم إجراء أي تغييرات أو فشل التحديث في قاعدة البيانات.");
            }

            var response = new BlogPostResponseDto
            {
                PostId = post.Id,
                Title = post.Title,
                Content = post.Content,
                Type = post.Type.ToString(),
                AuthorId = post.AuthorId,
                IsSensitiveRedacted = post.IsSensitiveRedacted,
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
        public async Task<List<BlogPostResponseDto>> GetDoctorPostsAsync(int doctorId)
        {
            // 1. جلب المنشورات الخاصة بالطبيب من المستودع
            var posts = await _blogRepo.GetBlogPostsByAuthorIdAsync(doctorId);

            // 2. تحويل قائمة المنشورات إلى الـ DTO النظيف المعتمد مسبقاً
            return posts.Select(post => new BlogPostResponseDto
            {
                PostId = post.Id,
                Title = post.Title,
                Content = post.Content,
                Type = post.Type.ToString(),
                AuthorId = post.AuthorId,
                IsSensitiveRedacted = post.IsSensitiveRedacted,
                CreatedAt = post.CreatedAt,
                Attachments = post.Attachments.Select(a => new BlogAttachmentDto
                {
                    Id = a.Id,
                    Path = a.Path,
                    Type = a.Type.ToString(), // تظهر كلمة "Other"
                    UploadedAt = a.UploadedAt,
                    BlogPostId = post.Id
                }).ToList()
            }).ToList();
        }
    }
}