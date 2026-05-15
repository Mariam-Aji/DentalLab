using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories;

public interface IAccountsRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task AddUserAsync(User user);
    Task AddLabAsync(Lab lab);
    Task AddEmailOtpAsync(EmailOtp otp);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task AddFileResourceAsync(FileResource file);
    Task SaveChangesAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<Lab?> GetLabByIdTrackingAsync(int id);
    Task<Lab?> GetLabByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<EmailOtp?> GetEmailOtpAsync(int userId, string code, EmailOtpPurpose purpose);
    Task<EmailOtp?> GetLatestEmailOtpAsync(int userId, EmailOtpPurpose purpose);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);
    Task<int?> GetLabIdByUserIdAsync(int userId);
    Task<Lab?> GetLabByUserIdAsync(int userId);
}
