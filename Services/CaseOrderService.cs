using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DentalLab.Api.Services
{
    public class CaseOrderService : ICaseOrderService
    {
        private readonly ICaseOrderRepository _repo;
        private readonly IWebHostEnvironment _env;

        public CaseOrderService(
            ICaseOrderRepository repo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _env = env;
        }

        public async Task<(OrderResponseDto? result, string? error)>
      CreateInitialOrderAsync(
      CreateCaseOrderDto dto,
      int dentistId,
      int labId)
        {
            if (!await _repo.IsDentistConnectedToLab(dentistId, labId))
            {
                return (null,
                    "لا يمكن إنشاء الطلب. يجب أن يكون المخبر قد قبل طلب الاتصال أولاً.");
            }

            List<string> imageUrls = new();

            if (dto.RequiredImages != null && dto.RequiredImages.Any())
            {
                var uploadsRoot = Path.Combine(
                    _env.ContentRootPath,
                    "uploads",
                    "cases",
                    dentistId.ToString(),
                    "required-images");

                Directory.CreateDirectory(uploadsRoot);

                foreach (var file in dto.RequiredImages)
                {
                    var validationError = ValidateOrderImage(file);

                    if (validationError != null)
                        return (null, validationError);

                    var ext = Path.GetExtension(file.FileName)
                        .ToLowerInvariant();

                    var fileName = $"{Guid.NewGuid():N}{ext}";

                    var fullPath = Path.Combine(
                        uploadsRoot,
                        fileName);

                    await using (var stream =
                        new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var relativePath = Path.Combine(
                        "uploads",
                        "cases",
                        dentistId.ToString(),
                        "required-images",
                        fileName)
                        .Replace("\\", "/");

                    imageUrls.Add(relativePath);
                }
            }

            var order = new CaseOrder
            {
                Title = dto.Title,

                CreatedById = dentistId,

                AssignedLabId = labId,

                Status = CaseStatus.Accepted,

                // الجديد
                ImpressionStage = dto.ImpressionStage,

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

            var createdOrder =
                await _repo.CreateOrderAsync(order);

            return (new OrderResponseDto
            {
                OrderId = createdOrder.Id,

                //Status = createdOrder.Status.ToString(),

                ImpressionStage = createdOrder.ImpressionStage
            }, null);
        }

        public async Task<CaseOrderItemResponseDto> AddItemToOrderAsync(
     int orderId,
     CaseOrderItemDto itemDto,
     int dentistId)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null || order.CreatedById != dentistId)
                throw new UnauthorizedAccessException("Unauthorized");

            var labPrice = await _repo.GetUnitPriceAsync(
                order.AssignedLabId!.Value,
                itemDto.CompensationType);

            decimal unitPrice = labPrice?.UnitPrice ?? 0;
            decimal itemTotal = unitPrice * itemDto.ToothNumbers.Count;

            var newItem = new CaseOrderItem
            {
                CaseOrderId = orderId,
                CompensationType = itemDto.CompensationType,
                ToothNumbers = itemDto.ToothNumbers
            };

            await _repo.AddOrderItemAsync(newItem);

            order.EstimatedPrice = (order.EstimatedPrice ?? 0) + itemTotal;

            await _repo.UpdateOrderAsync(order);

            return new CaseOrderItemResponseDto
            {
                CaseOrderId = order.Id,
                CaseOrderItemId = newItem.Id,
                Status = order.Status.ToString(),
                CompensationType = itemDto.CompensationType,
                ToothNumbers = itemDto.ToothNumbers,
             
            };
        }

        private string? ValidateOrderImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Invalid file";

            const long maxBytes = 5 * 1024 * 1024;

            if (file.Length > maxBytes)
                return "Image too large";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowed = new[] { ".jpg", ".jpeg", ".png" };

            if (!allowed.Contains(ext))
                return "Invalid format";

            return null;
        }
        public async Task<CaseOrderInvoiceDto> GetOrderInvoiceAsync(int orderId)
        {
            var order = await _repo.GetOrderWithItemsAsync(orderId);

            if (order == null)
                throw new Exception("Order not found");

            var invoice = new CaseOrderInvoiceDto
            {
                CaseOrderId = order.Id,
                Status = order.Status.ToString(),
                Message = "هذه فاتورة تقديرية للسعر النهائي، وقد يحدث اختلاف بسيط عند اعتماد المخبر.",
            };

            decimal total = 0;

            foreach (var item in order.Items)
            {
                var price = await _repo.GetLabPriceAsync(
                    order.AssignedLabId!.Value,
                    item.CompensationType);

                decimal unitPrice = price?.UnitPrice ?? 0;

                int teethCount = item.ToothNumbers?.Count ?? 0;

                decimal lineTotal = unitPrice * teethCount;

                total += lineTotal;

                invoice.Items.Add(new CaseOrderInvoiceItemDto
                {
                    CaseOrderItemId = item.Id,
                    CaseOrderId = order.Id,
                    CompensationType = item.CompensationType,
                    ToothNumbers = item.ToothNumbers,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                });
            }

            invoice.EstimatedTotal = total;

            order.EstimatedPrice = total;
            await _repo.UpdateOrderAsync(order);

            return invoice;
        }
    }
}