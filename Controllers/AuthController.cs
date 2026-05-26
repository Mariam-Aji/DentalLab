using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AuthController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequestDto dto)
    {
        var (result, error) = await _accountService.LoginAsync(dto);
        if (error != null) return Unauthorized(new { message = error });

        return Ok(result);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromForm] VerifyEmailOtpDto dto)
    {
        var error = await _accountService.VerifyEmailOtpAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        return Ok(new { message = "Email verified. Waiting for admin approval." });
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromForm] ResendOtpDto dto)
    {
        var error = await _accountService.ResendVerificationOtpAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        return Ok(new { message = "OTP resent." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordRequestDto dto)
    {
        var error = await _accountService.RequestPasswordResetAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        return Ok(new { message = "Password reset OTP sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordDto dto)
    {
        var error = await _accountService.ResetPasswordAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        return Ok(new { message = "Password updated." });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromForm] RefreshTokenRequestDto dto)
    {
        var (result, error) = await _accountService.RefreshTokenAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromForm] LogoutRequestDto dto)
    {
        var error = await _accountService.LogoutAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        return Ok(new { message = "Logged out." });
    }
}
