using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Repositories;

public class AdvertisementRepository : IAdvertisementRepository
{
    private readonly ApplicationDbContext _context;

    public AdvertisementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> SaveUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> IsUserExistsAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<Advertisement> SaveAdvertisementAsync(Advertisement advertisement)
    {
        await _context.Advertisements.AddAsync(advertisement);
        await _context.SaveChangesAsync();
        return advertisement;
    }

    public async Task<List<Advertisement>> GetAllAdvertisementsAsync()
    {
        return await _context.Advertisements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Advertisement?> GetAdvertisementByIdAsync(int id)
    {
        
        return await _context.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> UpdateAdvertisementAsync(Advertisement advertisement)
    {
        
        var entry = _context.Entry(advertisement);

        if (entry.State == EntityState.Detached)
        {
            _context.Advertisements.Attach(advertisement);
            entry.State = EntityState.Modified;
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAdvertisementAsync(Advertisement advertisement)
    {
        _context.Advertisements.Remove(advertisement);
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<List<Advertisement>> GetAdvertisementsForDentistsAsync()
    {
        return await _context.Advertisements
            .AsNoTracking()
            .Where(a => a.IsActive && a.Target == TargetAudience.Dentists) 
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Advertisement>> GetAdvertisementsForLabsAsync()
    {
        return await _context.Advertisements
            .AsNoTracking()
            .Where(a => a.IsActive && a.Target == TargetAudience.Labs) 
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    public async Task<User?> GetAdminUserAsync()
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
    }

    public async Task<bool> SaveNotificationAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<Advertisement?> GetByIdAsync(int id)
    {
        return await _context.Advertisements.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> SaveChangesStatusAsync(Advertisement advertisement)
    {
        _context.Advertisements.Update(advertisement);
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }
    public async Task<List<Advertisement>> GetAdvertisementsByUserIdAsync(int userId)
    {
        return await _context.Advertisements
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteUserAsync(User user)
    {
        _context.Users.Remove(user);
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<List<User>> SearchLabsByNameAsync(string labName)
    {
        return await _context.Users
            .Include(u => u.LabProfile) 
            .Where(u => u.Role == UserRole.Lab &&
                        u.NamePlace != null &&
                        u.NamePlace.Contains(labName))
            .ToListAsync();
    }
}