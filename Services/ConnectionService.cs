//using DentalLab.Api.Hubs;
using DentalLab.Api.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly IConnectionRepository _connectionRepository;
        private readonly IHubContext<NotificationHub> _hubContext; 

        public ConnectionService(
            IConnectionRepository connectionRepository,
            IHubContext<NotificationHub> hubContext)
        {
            _connectionRepository = connectionRepository;
            _hubContext = hubContext;
        }

        public async Task<string> SendFollowRequestAsync(int dentistId, string userRole, int labId)
        {
            if (userRole != "Dentist")
                return "صلاحية غير كافية: فقط الأطباء يمكنهم إرسال طلبات المتابعة.";

            if (!await _connectionRepository.LabExistsAsync(labId))
            {
                return "المخبر غير موجود، يرجى التأكد من الرقم المرسل.";
            }

            if (await _connectionRepository.RequestExistsAsync(dentistId, labId))
            {
                return "لقد أرسلت طلباً مسبقاً لهذا المخبر أو أنت متابع له بالفعل.";
            }

            var request = new ConnectionRequest
            {
                FromDentistId = dentistId,
                ToLabId = labId,
                Status = ConnectionRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var success = await _connectionRepository.CreateRequestAsync(request);

            if (success)
            {
                var labOwnerUserId = await _connectionRepository.GetLabOwnerUserIdAsync(labId);
                if (labOwnerUserId != null)
                {
                    var notificationMessage = "لديك طلب متابعة اتصال جديد من أحد الأطباء، يرجى مراجعته وقبوله.";

                    var notification = new Notification
                    {
                        RecipientId = labOwnerUserId.Value,
                        Type = NotificationType.InfoRequested,
                        Message = notificationMessage,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _connectionRepository.AddNotificationAsync(notification);

                    await _hubContext.Clients.User(labOwnerUserId.Value.ToString())
                        .SendAsync("ReceiveNotification", new
                        {
                            notification.Id,
                            notification.Message,
                            notification.Type,
                            notification.CreatedAt
                        });
                }
            }

            return success
                ? "تم إرسال طلب المتابعة بنجاح، بانتظار موافقة صاحب المخبر."
                : "فشل في تنفيذ العملية.";
        }
    }
}