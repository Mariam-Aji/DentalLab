using DentalLab.Api.Dtos;

namespace DentalLab.Api.Services;

public interface ILabOrderQuoteService
{
    Task<(OrderQuoteDto? quote, string? error)> GetOrderQuoteAsync(int orderId, int labId);
    Task<(object? result, string? error)> SetFinalPriceAsync(int orderId, int labId, decimal finalPrice);
}
