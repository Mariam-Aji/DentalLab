using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetDoctorNotificationsAsync(int doctorId);
    }
}
