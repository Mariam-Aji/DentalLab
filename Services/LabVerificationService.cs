using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class LabVerificationService : ILabVerificationService
    {
        private readonly ILabVerificationRepository _verificationRepository;

        public LabVerificationService(ILabVerificationRepository verificationRepository)
        {
            _verificationRepository = verificationRepository;
        }

        public async Task<IEnumerable<PendingLabDto>> GetPendingLabsOnlyAsync()
        {
            var rawUsers = await _verificationRepository.GetPendingLabAccountsAsync();

            return rawUsers.Select(user => new PendingLabDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                NamePlace = user.NamePlace,
                AccountStatus = user.Status.ToString(),
                VerificationDocumentPath = user.VerificationDocumentPath,
                CreatedAt = user.CreatedAt,
                LabId = user.LabProfile?.Id,
                YearsOfExperience = user.LabProfile?.YearsOfExperience ?? 0,
                Specialties = user.LabProfile?.Specialties ?? new List<string>()
            });
        }
        public async Task<(bool Success, string Message)> SuspendLabAccountAsync(int userId)
        {
            var user = await _verificationRepository.GetUserByIdAsync(userId);

            if (user == null) return (false, "المستخدم غير موجود.");
            if (user.Role != UserRole.Lab) return (false, "هذا الحساب ليس لمخبر.");
            if (user.Status == AccountStatus.Suspended) return (false, "الحساب محظور بالفعل مسبقاً.");

            user.Status = AccountStatus.Suspended;
            await _verificationRepository.UpdateUserStatusAsync(user);

            return (true, "تم حظر حساب المخبر بنجاح.");
        }
    }
}