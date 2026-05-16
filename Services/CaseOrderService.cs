using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;

namespace DentalLab.Api.Services
{
    public class CaseOrderService : ICaseOrderService
    {
        private readonly ICaseOrderRepository _repo;

        public CaseOrderService(ICaseOrderRepository repo) => _repo = repo;

        public async Task<OrderResponseDto> CreateInitialOrderAsync(CreateCaseOrderDto dto, int dentistId, int labId, List<string> imageUrls)
        {
            if (!await _repo.IsDentistConnectedToLab(dentistId, labId))
                throw new InvalidOperationException("لا يمكن إنشاء الطلب. يجب أن يكون المخبر قد قبل طلب الاتصال الخاص بك أولاً.");

            var order = new CaseOrder
            {
                Title = dto.Title,
                CreatedById = dentistId,
                AssignedLabId = labId,
                Status = CaseStatus.PlasticImpression,
                Shade = dto.Shade,
                IsTemporary = dto.IsTemporary,
                ImpressionType = dto.ImpressionType,
                IsUrgent = dto.IsUrgent,
                DeliveryDate = dto.DeliveryDate,
                Notes = dto.Notes,
                HasAccessories = dto.HasAccessories,
                RequiredImages = imageUrls,
                EstimatedPrice = 0,
                CreatedAt = DateTime.UtcNow
            };

            var createdOrder = await _repo.CreateOrderAsync(order);

            return new OrderResponseDto
            {
                OrderId = createdOrder.Id,
                Status = createdOrder.Status.ToString(),
                ImpressionType = null,
                CompensationType = null,
                ToothNumbers = null,
                TotalEstimatedPrice = null
            };
        }

        public async Task<OrderResponseDto> AddItemToOrderAsync(int orderId, CaseOrderItemDto itemDto, int dentistId)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null || order.CreatedById != dentistId)
                throw new UnauthorizedAccessException("الطلب غير موجود أو لا تملك صلاحية الوصول.");

            var labPrice = await _repo.GetUnitPriceAsync(order.AssignedLabId!.Value, itemDto.CompensationType);
            decimal unitPrice = labPrice?.UnitPrice ?? 0;
            decimal itemTotalPrice = unitPrice * itemDto.ToothNumbers.Count;

            var newItem = new CaseOrderItem
            {
                CaseOrderId = orderId,
                CompensationType = itemDto.CompensationType,
                ToothNumbers = itemDto.ToothNumbers
            };

            await _repo.AddOrderItemAsync(newItem);

            order.EstimatedPrice = (order.EstimatedPrice ?? 0) + itemTotalPrice;
            await _repo.UpdateOrderAsync(order);

            return new OrderResponseDto
            {
                OrderId = order.Id,
                Status = order.Status.ToString(),
                ImpressionType = order.ImpressionType,
                CompensationType = itemDto.CompensationType,
                ToothNumbers = itemDto.ToothNumbers,
                TotalEstimatedPrice = order.EstimatedPrice
            };
        }
    }
}