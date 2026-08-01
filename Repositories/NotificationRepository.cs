using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetDoctorNotificationsAsync(int doctorId)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == doctorId) // أو DoctorId حسب تسميتك في قاعدة البيانات
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}