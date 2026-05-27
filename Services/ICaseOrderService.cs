using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
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

        Task<(CreatePatientDto? result, string? error)> AddPatientToCaseOrderAsync(int caseOrderId, CreatePatientDto patientDto);
        Task<List<CreatePatientDto>> GetAllPatientsAsync();
        Task<object> BindExistingPatientToOrderAsync(int caseOrderId, int patientId);
        Task<(object? result, string? error)> UpdatePatientDetailsAsync(int patientId, UpdatePatientDto dto, int dentistId);
    }

}