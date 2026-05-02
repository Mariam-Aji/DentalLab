using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using DentalLab.Api.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;

namespace DentalLab.Api.Services;

public class AccountService : IAccountService
{
    private readonly IAccountsRepository _repo;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailSender _emailSender;
    private readonly OtpSettings _otpSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly RefreshTokenSettings _refreshTokenSettings;

    public AccountService(
        IAccountsRepository repo,
        IWebHostEnvironment env,
        IEmailSender emailSender,
        IOptions<OtpSettings> otpSettings,
        IOptions<JwtSettings> jwtSettings,
        IOptions<RefreshTokenSettings> refreshTokenSettings)
    {
        _repo = repo;
        _env = env;
        _emailSender = emailSender;
        _otpSettings = otpSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _refreshTokenSettings = refreshTokenSettings.Value;
    }

    public async Task<(AccountCreatedDto? result, string? error)> CreateDentistAsync(DentistRegistrationDto dto)
    {
        var email = dto.Email.Trim();
        var exists = await _repo.EmailExistsAsync(email);
        if (exists) return (null, "Email already exists.");

        var fileError = ValidateVerificationDocument(dto.VerificationDocument);
        if (fileError != null) return (null, fileError);

        var storedPath = await SaveVerificationDocumentAsync(dto.VerificationDocument);

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim()),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            NamePlace = string.IsNullOrWhiteSpace(dto.NamePlace) ? null : dto.NamePlace.Trim(),
            AddressPlace = string.IsNullOrWhiteSpace(dto.AddressPlace) ? null : dto.AddressPlace.Trim(),
            CityPlace = string.IsNullOrWhiteSpace(dto.CityPlace) ? null : dto.CityPlace.Trim(),
            CountryPlace = string.IsNullOrWhiteSpace(dto.CountryPlace) ? null : dto.CountryPlace.Trim(),
            VerificationDocumentPath = storedPath,
            Role = UserRole.Dentist,
            Status = AccountStatus.PendingVerification
        };

        await _repo.AddUserAsync(user);
        await _repo.SaveChangesAsync();

        await CreateAndSendOtpAsync(user, EmailOtpPurpose.EmailVerification);

        return (new AccountCreatedDto
        {
            UserId = user.Id,
            Role = user.Role,
            Status = user.Status
        }, null);
    }

    public async Task<(AccountCreatedDto? result, string? error)> CreateLabAsync(LabRegistrationDto dto)
    {
        var email = dto.Email.Trim();
        var exists = await _repo.EmailExistsAsync(email);
        if (exists) return (null, "Email already exists.");

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim()),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            NamePlace = string.IsNullOrWhiteSpace(dto.NamePlace) ? null : dto.NamePlace.Trim(),
            AddressPlace = string.IsNullOrWhiteSpace(dto.AddressPlace) ? null : dto.AddressPlace.Trim(),
            CityPlace = string.IsNullOrWhiteSpace(dto.CityPlace) ? null : dto.CityPlace.Trim(),
            CountryPlace = string.IsNullOrWhiteSpace(dto.CountryPlace) ? null : dto.CountryPlace.Trim(),
            
            Role = UserRole.Lab,
            Status = AccountStatus.PendingVerification
        };

        var lab = new Lab
        {
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            YearsOfExperience = dto.YearsOfExperience ?? 0,
            Specialties = dto.Specialties?.Select(s => s.Trim()).ToList() ?? new List<string>(),
            Materials = dto.Materials?.Select(m => m.Trim()).ToList() ?? new List<string>(),
            Availability = dto.Availability ?? AvailabilityStatus.Available,
            HasScanVisitService = dto.HasScanVisitService
        };

        await _repo.AddUserAsync(user);
        await _repo.SaveChangesAsync();

        lab.UserId = user.Id;
        await _repo.AddLabAsync(lab);
        await _repo.SaveChangesAsync();

        await CreateAndSendOtpAsync(user, EmailOtpPurpose.EmailVerification);

        return (new AccountCreatedDto
        {
            UserId = user.Id,
            Role = user.Role,
            Status = user.Status,
            LabId = lab.Id
        }, null);
    }

    public Task<User?> GetUserByIdAsync(int id) => _repo.GetUserByIdAsync(id);

    public Task<Lab?> GetLabByIdAsync(int id) => _repo.GetLabByIdAsync(id);

    public async Task<(LoginResponseDto? result, string? error)> LoginAsync(LoginRequestDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _repo.GetUserByEmailAsync(email);
        if (user == null) return (null, "Invalid email or password.");

        var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!ok) return (null, "Invalid email or password.");

        int? labId = null;
        if (user.Role == UserRole.Lab)
        {
            labId = await _repo.GetLabIdByUserIdAsync(user.Id);
        }

        if (user.Status != AccountStatus.Active)
        {
            var message = user.Status switch
            {
                AccountStatus.PendingVerification => "Email is not verified yet.",
                AccountStatus.PendingAdminApproval => "Waiting for admin approval.",
                AccountStatus.Suspended => "Account is suspended.",
                _ => "Limited access."
            };

            return (new LoginResponseDto
            {
                UserId = user.Id,
                LabId = labId,
                Role = user.Role,
                Status = user.Status,
                AccessMode = "ReadOnly",
                AccessToken = null,
                ExpiresAtUtc = null,
                Message = message
            }, null);
        }

        var (token, expiresAtUtc) = CreateJwtToken(user);
        var (refreshToken, refreshExpiresAtUtc) = await CreateRefreshTokenAsync(user);
        return (new LoginResponseDto
        {
            UserId = user.Id,
            LabId = labId,
            Role = user.Role,
            Status = user.Status,
            AccessMode = "Full",
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc
        }, null);
    }

    public async Task<string?> VerifyEmailOtpAsync(VerifyEmailOtpDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _repo.GetUserByEmailAsync(email);
        if (user == null) return "User not found.";
        if (user.Status != AccountStatus.PendingVerification) return "Account is already verified.";

        var otp = await _repo.GetEmailOtpAsync(user.Id, dto.Code.Trim(), EmailOtpPurpose.EmailVerification);
        if (otp == null) return "Invalid OTP.";
        if (otp.UsedAtUtc != null) return "OTP already used.";
        if (otp.ExpiresAtUtc < DateTime.UtcNow) return "OTP expired.";

        otp.UsedAtUtc = DateTime.UtcNow;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.Status = AccountStatus.PendingAdminApproval;

        await _repo.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ResendVerificationOtpAsync(ResendOtpDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _repo.GetUserByEmailAsync(email);
        if (user == null) return "User not found.";
        if (user.Status != AccountStatus.PendingVerification) return "Account is already verified.";

        var latest = await _repo.GetLatestEmailOtpAsync(user.Id, EmailOtpPurpose.EmailVerification);
        if (latest != null && latest.UsedAtUtc == null && latest.ExpiresAtUtc > DateTime.UtcNow)
        {
            return "OTP is still valid.";
        }

        await CreateAndSendOtpAsync(user, EmailOtpPurpose.EmailVerification);
        return null;
    }

    public async Task<string?> RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _repo.GetUserByEmailAsync(email);
        if (user == null) return "User not found.";

        await CreateAndSendOtpAsync(user, EmailOtpPurpose.PasswordReset);
        return null;
    }

    public async Task<string?> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var email = dto.Email.Trim();
        var user = await _repo.GetUserByEmailAsync(email);
        if (user == null) return "User not found.";

        var otp = await _repo.GetEmailOtpAsync(user.Id, dto.Code.Trim(), EmailOtpPurpose.PasswordReset);
        if (otp == null) return "Invalid OTP.";
        if (otp.UsedAtUtc != null) return "OTP already used.";
        if (otp.ExpiresAtUtc < DateTime.UtcNow) return "OTP expired.";

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword.Trim());
        otp.UsedAtUtc = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        return null;
    }

    public async Task<(LoginResponseDto? result, string? error)> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var tokenHash = HashToken(dto.RefreshToken.Trim());
        var stored = await _repo.GetRefreshTokenByHashAsync(tokenHash);
        if (stored == null) return (null, "Invalid refresh token.");
        if (stored.RevokedAtUtc != null) return (null, "Refresh token revoked.");
        if (stored.ExpiresAtUtc < DateTime.UtcNow) return (null, "Refresh token expired.");

        var user = stored.User;
        if (user.Status != AccountStatus.Active)
        {
            return (null, "Account is not active.");
        }

        int? labId = null;
        if (user.Role == UserRole.Lab)
        {
            labId = await _repo.GetLabIdByUserIdAsync(user.Id);
        }

        var (token, expiresAtUtc) = CreateJwtToken(user);
        var (newRefreshToken, refreshExpiresAtUtc) = await CreateRefreshTokenAsync(user);

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = HashToken(newRefreshToken);
        await _repo.SaveChangesAsync();

        return (new LoginResponseDto
        {
            UserId = user.Id,
            LabId = labId,
            Role = user.Role,
            Status = user.Status,
            AccessMode = "Full",
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc
        }, null);
    }

    public async Task<string?> LogoutAsync(LogoutRequestDto dto)
    {
        var tokenHash = HashToken(dto.RefreshToken.Trim());
        var stored = await _repo.GetRefreshTokenByHashAsync(tokenHash);
        if (stored == null) return "Invalid refresh token.";
        if (stored.RevokedAtUtc != null) return null;

        stored.RevokedAtUtc = DateTime.UtcNow;
        await _repo.SaveChangesAsync();
        return null;
    }

    private string? ValidateVerificationDocument(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null || file.Length == 0) return "Verification document is required.";

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes) return "Verification document must be 5MB or less.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowed.Contains(ext)) return "Verification document must be an image (JPG or PNG).";

        return null;
    }

    private async Task CreateAndSendOtpAsync(User user, EmailOtpPurpose purpose)
    {
        var code = GenerateOtpCode(_otpSettings.Length);
        var otp = new EmailOtp
        {
            UserId = user.Id,
            Code = code,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_otpSettings.ExpiryMinutes),
            Purpose = purpose
        };

        await _repo.AddEmailOtpAsync(otp);
        await _repo.SaveChangesAsync();

        var subject = purpose == EmailOtpPurpose.EmailVerification ? "Verify your email" : "Reset your password";
        var body = purpose == EmailOtpPurpose.EmailVerification
            ? $"Your verification code is: {code}. It expires in {_otpSettings.ExpiryMinutes} minutes."
            : $"Your password reset code is: {code}. It expires in {_otpSettings.ExpiryMinutes} minutes.";
        await _emailSender.SendEmailAsync(user.Email, subject, body);
    }

    private async Task<(string token, DateTime expiresAtUtc)> CreateRefreshTokenAsync(User user)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenSettings.ExpiryDays);
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = expiresAtUtc
        };

        await _repo.AddRefreshTokenAsync(refresh);
        await _repo.SaveChangesAsync();

        return (rawToken, expiresAtUtc);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string GenerateOtpCode(int length)
    {
        var digits = new char[length];
        for (var i = 0; i < length; i++)
        {
            var value = RandomNumberGenerator.GetInt32(0, 10);
            digits[i] = (char)('0' + value);
        }
        return new string(digits);
    }

    private (string token, DateTime expiresAtUtc) CreateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAtUtc);
    }

    private async Task<string> SaveVerificationDocumentAsync(Microsoft.AspNetCore.Http.IFormFile file)
    {
        var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "verification");
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return Path.Combine("uploads", "verification", fileName).Replace("\\", "/");
    }
}
