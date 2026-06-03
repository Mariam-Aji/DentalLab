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

        public async Task<(CreatePatientDto? result, string? error)> AddPatientToCaseOrderAsync(int caseOrderId, CreatePatientDto patientDto)
        {
            var caseOrder = await _repo.GetOrderByIdAsync(caseOrderId);
            if (caseOrder == null)
            {
                return (null, "الطلبية المحددة غير موجودة.");
            }

            var newPatient = new Patient
            {
                FullName = patientDto.FullName,
                Age = patientDto.Age,
                ClinicalNotes = patientDto.ClinicalNotes,
                ProcessedTeeth = patientDto.ProcessedTeeth
            };

            var success = await _repo.AddPatientAndBindToOrderAsync(caseOrder, newPatient);

            if (!success)
            {
                return (null, "فشل في حفظ البيانات بقاعدة البيانات.");
            }

            patientDto.PatientId = newPatient.Id; 
            patientDto.CaseOrderId = caseOrderId;  

            return (patientDto, null);
        }
        public async Task<List<CreatePatientDto>> GetAllPatientsAsync()
        {
            var patients = await _repo.GetAllPatientsAsync();

            return patients.Select(p => new CreatePatientDto
            {
                PatientId = p.Id,
                CaseOrderId = 0, 
                FullName = p.FullName,
                Age = p.Age,
                ClinicalNotes = p.ClinicalNotes,
                ProcessedTeeth = p.ProcessedTeeth
            }).ToList();
        }

        public async Task<object> BindExistingPatientToOrderAsync(int caseOrderId, int patientId)
        {
            var caseOrder = await _repo.GetOrderByIdAsync(caseOrderId);
            if (caseOrder == null)
            {
                return new { success = false, message = "الطلبية المحددة غير موجودة." };
            }

            var patient = await _repo.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                return new { success = false, message = "المريض المحدد غير موجود في النظام." };
            }

            caseOrder.PatientId = patientId;
            await _repo.UpdateOrderAsync(caseOrder);

            return new
            {
                message = "تم إسناد المريض  بنجاح.",
                patientDetails = new CreatePatientDto
                {
                    PatientId = patient.Id,
                    CaseOrderId = caseOrderId, 
                    FullName = patient.FullName,
                    Age = patient.Age,
                    ClinicalNotes = patient.ClinicalNotes,
                    ProcessedTeeth = patient.ProcessedTeeth
                }
            };
        }
        public async Task<(object? result, string? error)> UpdatePatientDetailsAsync(int patientId, UpdatePatientDto dto, int dentistId)
        {
            var patient = await _repo.GetPatientWithFilesByIdAsync(patientId);
            if (patient == null)
            {
                return (null, "المريض المحدد غير موجود.");
            }

            

            if (!string.IsNullOrWhiteSpace(dto.FullName))
            {
                patient.FullName = dto.FullName;
            }

            if (dto.Age.HasValue)
            {
                patient.Age = dto.Age.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.ClinicalNotes))
            {
                patient.ClinicalNotes = dto.ClinicalNotes;
            }

            if (dto.ProcessedTeeth != null && dto.ProcessedTeeth.Any(t => !string.IsNullOrWhiteSpace(t)))
            {
                patient.ProcessedTeeth = dto.ProcessedTeeth.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            }

            if (dto.NewPhotos != null && dto.NewPhotos.Any())
            {
                var uploadsRoot = Path.Combine(
                    _env.ContentRootPath,
                    "uploads",
                    "patients",
                    patientId.ToString());

                Directory.CreateDirectory(uploadsRoot);

                foreach (var file in dto.NewPhotos)
                {
                    var validationError = ValidateOrderImage(file);
                    if (validationError != null) return (null, $"{file.FileName}: {validationError}");

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var fullPath = Path.Combine(uploadsRoot, fileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var relativePath = Path.Combine("uploads", "patients", patientId.ToString(), fileName).Replace("\\", "/");

                    var newFileResource = new FileResource
                    {
                        Path = relativePath,
                        Type = dto.NewPhotosType,
                        UploadedAt = DateTime.UtcNow
                    };

                    patient.Files.Add(newFileResource);
                }
            }

            var success = await _repo.UpdatePatientAsync(patient);
            if (!success) return (null, "فشل في تحديث بيانات المريض بقاعدة البيانات.");

            return (new
            {
                message = "تم تحديث سجل المريض وحفظ الصور الجديدة بنجاح.",
                patientDetails = new
                {
                    PatientId = patient.Id,
                    FullName = patient.FullName,      
                    Age = patient.Age,              
                    ClinicalNotes = patient.ClinicalNotes, 
                    ProcessedTeeth = patient.ProcessedTeeth,
                    Photos = patient.Files.Select(f => new
                    {
                        f.Id,
                        f.Path,
                        Type = f.Type.ToString(),
                        f.UploadedAt
                    }).ToList()
                }
            }, null);
        }
        public async Task<List<CaseOrderDetailDto>> GetAllOrdersWithDetailsAsync()
        {
            return await _repo.GetAllCaseOrdersWithDetailsAsync();
        }
        //
    }
}