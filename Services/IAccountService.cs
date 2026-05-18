using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
//
namespace DentalLab.Api.Services;

public interface IAccountService
{
    Task<(AccountCreatedDto? result, string? error)> CreateDentistAsync(DentistRegistrationDto dto);
    Task<(AccountCreatedDto? result, string? error)> CreateLabAsync(LabRegistrationDto dto);
    Task<(LoginResponseDto? result, string? error)> LoginAsync(LoginRequestDto dto);
    Task<string?> VerifyEmailOtpAsync(VerifyEmailOtpDto dto);
    Task<string?> ResendVerificationOtpAsync(ResendOtpDto dto);
    Task<string?> RequestPasswordResetAsync(ForgotPasswordRequestDto dto);
    Task<string?> ResetPasswordAsync(ResetPasswordDto dto);
    Task<(LoginResponseDto? result, string? error)> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<string?> LogoutAsync(LogoutRequestDto dto);
    Task<User?> GetUserByIdAsync(int id);
    Task<Lab?> GetLabByIdAsync(int id);
    Task<Lab?> GetLabByUserIdAsync(int userId);
}
