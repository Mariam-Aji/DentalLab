using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class DentistVerificationService : IDentistVerificationService
    {
        private readonly IDentistVerificationRepository _dentistRepository;

        public DentistVerificationService(IDentistVerificationRepository dentistRepository)
        {
            _dentistRepository = dentistRepository;
        }

        public async Task<IEnumerable<PendingDentistDto>> GetPendingDentistsOnlyAsync()
        {
            var rawUsers = await _dentistRepository.GetPendingDentistAccountsAsync();

            return rawUsers.Select(user => new PendingDentistDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                NamePlace = user.NamePlace,
                CityPlace = user.CityPlace,
                AccountStatus = user.Status.ToString(),
                VerificationDocumentPath = user.VerificationDocumentPath,
                CreatedAt = user.CreatedAt
            });
        }
        public async Task<(bool Success, string Message)> SuspendDentistAccountAsync(int userId)
        {
            var user = await _dentistRepository.GetUserByIdAsync(userId);

            if (user == null) return (false, "المستخدم غير موجود.");
            if (user.Role != UserRole.Dentist) return (false, "هذا الحساب ليس لطبيب أسنان.");
            if (user.Status == AccountStatus.Suspended) return (false, "الحساب محظور بالفعل مسبقاً.");

            user.Status = AccountStatus.Suspended;
            await _dentistRepository.UpdateUserStatusAsync(user);

            return (true, "تم حظر حساب طبيب الأسنان بنجاح.");
        }
    }
}