
using DentalLab.Api.Dtos;
using DentalLab.Api.DTOs;
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

        Task<(CreatePatientDto? result, string? error)> AddPatientToCaseOrderAsync(int caseOrderId, CreatePatientDto patientDto, IWebHostEnvironment env); Task<List<CreatePatientDto>> GetAllPatientsAsync();
        Task<object> BindExistingPatientToOrderAsync(int caseOrderId, int patientId);
        Task<(object? result, string? error)> UpdatePatientDetailsAsync(int patientId, UpdatePatientDto dto, int dentistId);
        Task<List<CaseOrderDetailDto>> GetAllOrdersWithDetailsAsync();
        Task<(bool Success, string? Error)> AddItemsToExistingOrderAsync(int caseOrderId, int labId, AddCaseOrderItemsDto dto);
        Task<(bool Success, string? Message, decimal RefundAmount)> CancelAndProcessOrderAsync(int caseOrderId, int labId, CancelCaseOrderDto dto);
        Task<CaseOrderInvoiceDto> GetOrCreateOrderInvoiceAsync(int orderId, int dentistId);
        Task<List<CaseOrderInvoiceDto>> GetOrCreateDentistInvoicesAsync(int dentistId);
        Task<object> GetDentistOrdersTrackingAsync(int dentistId);
        Task<List<object>> GetOrdersByDentistAndLabAsync(int dentistId, int labId);
        Task<DentistOwnProfileDetailsDto?> FetchDentistPersonalProfileAsync(int userId);
        Task<(DentistOwnProfileDetailsDto? Profile, string? Error)> ModifyDentistPersonalProfileAsync(int userId, EditDentistOwnProfileDto dto);
        Task<List<CompensationDemandChartDto>> GetCompensationDemandChartDataAsync();
        Task<List<CaseStatusCountChartDto>> GetCaseStatusChartDataAsync();
        Task<List<PatientCaseOrderDto>> GetOrdersByPatientIdAsync(int patientId);
        Task<List<PatientCaseOrderDto>> GetOrdersWithPatientsAsync();
        Task<string> SendFullOrderNotificationToLabAsync(int orderId, int labId, int dentistId);
        Task<(bool Success, string? Message)> SendReplyToLabAsync(int caseOrderId, int dentistUserId, ReplyToLabDto dto);
    }
}
//

