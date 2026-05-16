using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services
{
    public interface ICaseOrderService
    {
        Task<OrderResponseDto> CreateInitialOrderAsync(CreateCaseOrderDto dto, int dentistId, int labId, List<string> imageUrls);
        Task<OrderResponseDto> AddItemToOrderAsync(int orderId, CaseOrderItemDto itemDto, int dentistId);
    }
}