using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class AccountsRepository : IAccountsRepository, IAdminAccountsRepository
{
    private readonly ApplicationDbContext _db;

    public AccountsRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<bool> EmailExistsAsync(string email)
        => _db.Users.AnyAsync(u => u.Email == email);

    public async Task AddUserAsync(User user)
        => await _db.Users.AddAsync(user);

    public async Task AddLabAsync(Lab lab)
        => await _db.Labs.AddAsync(lab);

    public async Task AddEmailOtpAsync(EmailOtp otp)
        => await _db.EmailOtps.AddAsync(otp);

    public async Task AddRefreshTokenAsync(RefreshToken token)
        => await _db.RefreshTokens.AddAsync(token);

    public async Task AddFileResourceAsync(FileResource file)
        => await _db.FileResources.AddAsync(file);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();

    public Task<User?> GetUserByIdAsync(int id)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetUserByIdTrackingAsync(int id)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<Lab?> GetLabByIdAsync(int id)
        => _db.Labs.AsNoTracking().Include(l => l.Owner).Include(l => l.Gallery).FirstOrDefaultAsync(l => l.Id == id);

    public Task<Lab?> GetLabByIdTrackingAsync(int id)
        => _db.Labs.FirstOrDefaultAsync(l => l.Id == id);

    public Task<User?> GetUserByEmailAsync(string email)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<EmailOtp?> GetEmailOtpAsync(int userId, string code, EmailOtpPurpose purpose)
        => _db.EmailOtps.FirstOrDefaultAsync(o => o.UserId == userId && o.Code == code && o.Purpose == purpose);

    public Task<EmailOtp?> GetLatestEmailOtpAsync(int userId, EmailOtpPurpose purpose)
        => _db.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose)
            .OrderByDescending(o => o.ExpiresAtUtc)
            .FirstOrDefaultAsync();

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        => _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task<int?> GetLabIdByUserIdAsync(int userId)
    {
        return await _db.Labs
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
    }

    public Task<Lab?> GetLabByUserIdAsync(int userId)
        => _db.Labs.AsNoTracking()
            .Include(l => l.Owner)
            .Include(l => l.Gallery)
            .Include(l => l.Prices)
            .FirstOrDefaultAsync(l => l.UserId == userId);

    public Task<List<User>> GetPendingDentistApprovalsAsync()
        => _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Dentist && u.Status == AccountStatus.PendingAdminApproval)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    public Task<List<User>> GetPendingLabApprovalsAsync()
        => _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Lab && u.Status == AccountStatus.PendingAdminApproval)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
}
