using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalLab.Api.Dtos.Complaints;
using DentalLab.Api.Services.Interfaces;

namespace DentalLab.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        // تقديم شكوى جديدة (متاح للأطباء Dentist فقط)
        [Authorize(Roles = "Dentist")]
        [HttpPost]
        [HttpPost("create/{labId?}")]
        public async Task<IActionResult> CreateComplaint(int? labId, [FromForm] CreateComplaintDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "المستخدم غير مسجل الدخول." });
                }

                var response = await _complaintService.CreateComplaintAsync(userId, labId, dto);

                return Ok(new
                {
                    message = "تم تقديم الشكوى وإرسال الإشعار بنجاح.",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"حدث خطأ: {ex.Message}" });
            }
        }

        // عرض إشعارات المستخدم الحالي
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "المستخدم غير مسجل الدخول." });
                }

                var notifications = await _complaintService.GetUserNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"حدث خطأ: {ex.Message}" });
            }
        }

        // عرض شكاوى الطبيب الخاصة به (مع حقول الجدول والردود)
        [Authorize(Roles = "Dentist")]
        [HttpGet("my-complaints")]
        public async Task<IActionResult> GetMyComplaints()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "المستخدم غير مسجل الدخول." });
                }

                var complaints = await _complaintService.GetDentistComplaintsAsync(userId);
                return Ok(complaints);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"حدث خطأ: {ex.Message}" });
            }
        }

        // عرض الشكاوى الموجهة للإدارة
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminComplaints()
        {
            try
            {
                var complaints = await _complaintService.GetAdminComplaintsAsync();
                return Ok(complaints);
            }
            catch (Exception ex)
            {




                return StatusCode(500, new { message = $"حدث خطأ: {ex.Message}" });
            }
        }

    
        [HttpGet("labs")]
        public async Task<IActionResult> GetLabComplaints()
        {
            try
            {
                var complaints = await _complaintService.GetLabComplaintsAsync();
                return Ok(complaints);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"حدث خطأ: {ex.Message}" });
            }
        }
        // الرد على شكوى محددة مع تمرير معرف الطبيب ومعرف الشكوى في الراوت (متاح للأدمن فقط)
        [Authorize(Roles = "Admin")]
        [HttpPost("reply/{dentistId}/{complaintId}")]
        public async Task<IActionResult> ReplyToComplaint(int dentistId, int complaintId, [FromForm] ReplyComplaintDto dto)
        {
            try
            {
                var updatedComplaint = await _complaintService.ReplyToComplaintAsync(dentistId, complaintId, dto);

                return Ok(new
                {
                    message = "تم إرسال الرد بنجاح وتحديث الشكوى وإرسال الإشعار للطبيب.",
                    data = updatedComplaint
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"حدث خطأ: {ex.Message}" });
            }
        }
    }
}