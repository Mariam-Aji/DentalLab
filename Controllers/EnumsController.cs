using DentalLab.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnumsController : ControllerBase
{
   

    [HttpGet("case-status")]
    public IActionResult GetCaseStatus()
        => Ok(Enum.GetNames<CaseStatus>());

    [HttpGet("blog-post-type")]
    public IActionResult GetBlogPostType()
        => Ok(Enum.GetNames<BlogPostType>());

    [HttpGet("user-role")]
    public IActionResult GetUserRole()
        => Ok(Enum.GetNames<UserRole>());

    [HttpGet("account-status")]
    public IActionResult GetAccountStatus()
        => Ok(Enum.GetNames<AccountStatus>());

    [HttpGet("compensation-type")]
    public IActionResult GetCompensationType()
        => Ok(Enum.GetNames<CompensationType>());

    [HttpGet("impression-type")]
    public IActionResult GetImpressionType()
        => Ok(Enum.GetNames<ImpressionType>());

    [HttpGet("scan-visit-status")]
    public IActionResult GetScanVisitStatus()
        => Ok(Enum.GetNames<ScanVisitStatus>());

    [HttpGet("notification-type")]
    public IActionResult GetNotificationType()
        => Ok(Enum.GetNames<NotificationType>());

    [HttpGet("availability-status")]
    public IActionResult GetAvailabilityStatus()
        => Ok(Enum.GetNames<AvailabilityStatus>());

    [HttpGet("file-type")]
    public IActionResult GetFileType()
        => Ok(Enum.GetNames<FileType>());

    [HttpGet("connection-request-status")]
    public IActionResult GetConnectionRequestStatus()
        => Ok(Enum.GetNames<ConnectionRequestStatus>());
}
