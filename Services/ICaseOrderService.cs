using DentalLab.Api.Dtos;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public interface ICaseOrderService
    {
        Task<(OrderResponseDto? result, string? error)> CreateInitialOrderAsync(CreateCaseOrderDto dto, int dentistId, int labId);
        Task<CaseOrderItemResponseDto> AddItemToOrderAsync(
      int orderId,
      CaseOrderItemDto itemDto,
      int dentistId);
        Task<CaseOrderInvoiceDto> GetOrderInvoiceAsync(int orderId);
    }
}