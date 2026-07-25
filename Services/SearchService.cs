using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services;

public class SearchService : ISearchService
{
    private readonly ISearchRepository _searchRepo;

    public SearchService(ISearchRepository searchRepo)
    {
        _searchRepo = searchRepo;
    }

    public async Task<List<object>> SearchLabsByNameAsync(string labName, int? currentUserId)
    {
        var labs = await _searchRepo.SearchLabsByNameAsync(labName);

        // 1. فحص هل المستخدم زائر أم مسجل دخول
        bool isVisitor = !currentUserId.HasValue;

        return labs.Select(l => new
        {
            LabId = l.Id,
            OwnerId = l.Owner.Id,
            LabName = l.Owner.Name,
            PlaceName = l.Owner.NamePlace,
            Email = l.Owner.Email,
            Phone = l.Owner.Phone,
            City = l.Owner.CityPlace,
            Country = l.Owner.CountryPlace,
            ProfilePictureUrl = l.Owner.ProfilePictureUrl, // صورة البروفايل من جدول المستخدمين
            YearsOfExperience = l.YearsOfExperience,
            Specialties = l.Specialties,
            Materials = l.Materials,
            AverageRating = l.AverageRating,
            HasScanVisitService = l.HasScanVisitService,

            // حالة التوفر والاتصال العامة
            IsVisitor = isVisitor,
            AvailabilityStatus = l.Availability.ToString(),
            IsAccountActive = l.Owner.Status == AccountStatus.Active,
            IsOnlineAvailable = !isVisitor && l.Availability == AvailabilityStatus.Available && l.Owner.Status == AccountStatus.Active,

            // 2. فحص هل الطبيب صاحب التوكن متصل بهذا المخبر أم لا
            IsConnectedWithCurrentDoctor = !isVisitor && l.ConnectionRequests.Any(cr =>
                cr.FromDentistId == currentUserId &&
                cr.Status == ConnectionRequestStatus.Accepted
            )
        }).Cast<object>().ToList();
    }

    public async Task<List<object>> SearchBlogPostsAsync(string query)
    {
        var posts = await _searchRepo.SearchBlogPostsAsync(query);

        return posts.Select(b => new
        {
            b.Id,
            b.Title,
            b.Content,
            Type = b.Type.ToString(),
            CreatedAt = b.CreatedAt,
            Author = b.Author != null ? new
            {
                b.Author.Id,
                b.Author.Name,
                b.Author.NamePlace,
                b.Author.ProfilePictureUrl,
                Role = b.Author.Role.ToString()
            } : null,
            Attachments = b.Attachments.Select(a => new
            {
                a.Id,
                FilePath = a.Path,
                FileName = Path.GetFileName(a.Path),
                a.Type
            }).ToList()
        }).Cast<object>().ToList();
    }
}