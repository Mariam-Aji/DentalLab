using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<NotificationHub> _hubContext;
        public CaseOrderService(
            ICaseOrderRepository repo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _env = env;
        }
        public CaseOrderService(
        ICaseOrderRepository repo,
        IWebHostEnvironment env,
        IHubContext<NotificationHub> hubContext) // 👈 تأكدي من وجودها هنا
        {
            _repo = repo;
            _env = env;
            _hubContext = hubContext; // 👈 وتعيينها هنا
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

                Status = CaseStatus.Pennding,

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
        public async Task<(bool Success, string? Error)> AddItemsToExistingOrderAsync(int caseOrderId, int labId, AddCaseOrderItemsDto dto)
        {
            var order = await _repo.GetOrderByIdAsync(caseOrderId);
            if (order == null) return (false, "طلب التعويض (CaseOrder) غير موجود.");

            if (dto.CompensationTypes == null || !dto.CompensationTypes.Any())
            {
                return (false, "لا توجد عناصر جديدة لإضافتها.");
            }

            try
            {
                List<CaseOrderItem> itemsToAdd = new();
                decimal totalNewItemsPrice = 0;

                for (int i = 0; i < dto.CompensationTypes.Count; i++)
                {
                    var compType = (CompensationType)dto.CompensationTypes[i];

                    var newToothNumbers = new List<int>();
                    if (i < dto.ToothNumbersGrouped.Count && !string.IsNullOrWhiteSpace(dto.ToothNumbersGrouped[i]))
                    {
                        newToothNumbers = dto.ToothNumbersGrouped[i]
                            .Split(',')
                            .Select(int.Parse)
                            .ToList();
                    }

                    if (!newToothNumbers.Any()) continue;

                    var existingItem = order.Items?.FirstOrDefault(item => item.CompensationType == compType);

                    if (existingItem != null)
                    {
                        existingItem.ToothNumbers ??= new List<int>();

                        var trulyNewTeeth = newToothNumbers.Except(existingItem.ToothNumbers).ToList();

                        if (trulyNewTeeth.Any())
                        {
                            existingItem.ToothNumbers.AddRange(trulyNewTeeth);

                            var labPrice = await _repo.GetUnitPriceAsync(labId, compType);
                            decimal unitPrice = labPrice?.UnitPrice ?? 0;
                            totalNewItemsPrice += unitPrice * trulyNewTeeth.Count;
                        }
                    }
                    else
                    {
                        var newItem = new CaseOrderItem
                        {
                            CaseOrderId = caseOrderId,
                            CompensationType = compType,
                            ToothNumbers = newToothNumbers
                        };
                        itemsToAdd.Add(newItem);

                        var labPrice = await _repo.GetUnitPriceAsync(labId, compType);
                        decimal unitPrice = labPrice?.UnitPrice ?? 0;
                        totalNewItemsPrice += unitPrice * newToothNumbers.Count;
                    }
                }

                if (itemsToAdd.Any())
                {
                    order.Items ??= new List<CaseOrderItem>();
                    foreach (var item in itemsToAdd)
                    {
                        order.Items.Add(item);
                    }
                }

                order.EstimatedPrice = (order.EstimatedPrice ?? 0) + totalNewItemsPrice;
                order.Status = CaseStatus.WaitingForClarification;

                await _repo.UpdateOrderAsync(order);

                string alertText = $"قام الطبيب بتعديل الطلبية رقم ({caseOrderId}) وإضافة عناصر تعويضية جديدة، بانتظار مراجعتكم وتحديد السعر النهائي.";

                var notification = new Notification
                {
                    RecipientId = labId,
                    Message = alertText,
                    Type = NotificationType.StatusChanged,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.SaveNotificationAsync(notification);

                string labGroupId = $"Lab_{labId}";

                await _hubContext.Clients.Group(labGroupId).SendAsync("ReceiveOrderNotification", alertText);

                await _hubContext.Clients.User(labId.ToString()).SendAsync("ReceiveOrderNotification", alertText);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ داخلي أثناء معالجة التعديلات والإرسال: {ex.Message}");
            }
        }
        public async Task<(bool Success, string? Message, decimal RefundAmount)> CancelAndProcessOrderAsync(int caseOrderId, int labId, CancelCaseOrderDto dto)
{
    var order = await _repo.GetOrderByIdAsync(caseOrderId);
    if (order == null) 
        return (false, "طلب التعويض غير موجود.", 0);

    if (order.AssignedLabId != labId)
        return (false, "هذه الطلبية لا تنتمي للمخبر المحدّد.", 0);

    try
    {
        var timeElapsed = DateTime.UtcNow - order.CreatedAt;
        decimal estimatedPrice = order.EstimatedPrice ?? 0;
        decimal refundAmount = 0;
        string financialAlertMessage = "";

        if (timeElapsed.TotalDays <= 1)
        {
            refundAmount = estimatedPrice;
            financialAlertMessage = $"تم إلغاء الطلب في غضون 24 ساعة. تم استرداد المبلغ بالكامل: $.";
        }
        else
        {
            refundAmount = estimatedPrice * 0.5m;
            financialAlertMessage = $"تنبيه: مضى أكثر من يوم على إنشاء الطلب، تم خصم 50% كغرامة إلغاء.  المسترد: $.";
        }

        string cleanReason = string.IsNullOrWhiteSpace(dto.CancellationReason) ? "لم يتم ذكر سبب محدد" : dto.CancellationReason;
        string alertText = $"قام الطبيب بإلغاء الطلبية رقم ({caseOrderId}). سبب الإلغاء: {cleanReason}. {financialAlertMessage}";

        var lab = await _repo.GetLabByIdAsync(labId); 
        if (lab == null) return (false, "لم يتم العثور على بيانات المخبر.", 0);

        var notification = new Notification
        {
            RecipientId = lab.UserId, 
            Message = alertText,
            Type = NotificationType.Cancellation,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.SaveNotificationAsync(notification);

        var isDeleted = await _repo.DeleteOrderAsync(order);
        if (!isDeleted) return (false, "فشل في عملية حذف الطلبية من السيرفر.", 0);

        string labGroupId = $"Lab_{labId}";
        await _hubContext.Clients.Group(labGroupId).SendAsync("ReceiveOrderNotification", alertText);
        await _hubContext.Clients.User(lab.UserId.ToString()).SendAsync("ReceiveOrderNotification", alertText);

        return (true, financialAlertMessage, refundAmount);
    }
    catch (Exception ex)
    {
        return (false, $"حدث خطأ داخلي أثناء الإلغاء: {ex.Message}", 0);
    }
}
        public async Task<CaseOrderInvoiceDto> GetOrCreateOrderInvoiceAsync(int orderId, int dentistId)
        {
            var order = await _repo.GetOrderWithItemsAsync(orderId);

            if (order == null)
                throw new Exception("الطلبية غير موجودة.");

            if (order.CreatedById != dentistId)
                throw new UnauthorizedAccessException("غير مصرح لك بالوصول إلى هذه الفاتورة.");

            var existingInvoice = await _repo.GetInvoiceByOrderIdAsync(orderId);

            decimal total = 0;
            var itemsDtoList = new List<CaseOrderInvoiceItemDto>();

            foreach (var item in order.Items)
            {
                var price = await _repo.GetLabPriceAsync(order.AssignedLabId!.Value, item.CompensationType);
                decimal unitPrice = price?.UnitPrice ?? 0;
                int teethCount = item.ToothNumbers?.Count ?? 0;
                decimal lineTotal = unitPrice * teethCount;

                total += lineTotal;

                itemsDtoList.Add(new CaseOrderInvoiceItemDto
                {
                    CaseOrderItemId = item.Id,
                    CaseOrderId = order.Id,
                    CompensationType = item.CompensationType,
                    ToothNumbers = item.ToothNumbers,
                    UnitPrice = unitPrice,
                    LineTotal = lineTotal
                });
            }

            if (existingInvoice == null)
            {
                var newInvoice = new OrderInvoice
                {
                    CaseOrderId = order.Id,
                    TotalAmount = total,
                    CreatedAt = DateTime.UtcNow,
                };

                await _repo.AddInvoiceAsync(newInvoice);
            }
           

            var invoiceDto = new CaseOrderInvoiceDto
            {
                CaseOrderId = order.Id,
                Status = order.Status.ToString(),
                EstimatedTotal = total,
                Message = "هذه فاتورة تقديرية للسعر النهائي، وقد يحدث اختلاف بسيط عند اعتماد المخبر.",
                Items = itemsDtoList
            };

            order.EstimatedPrice = total;
            await _repo.UpdateOrderAsync(order);

            return invoiceDto;
        }
        public async Task<List<CaseOrderInvoiceDto>> GetOrCreateDentistInvoicesAsync(int dentistId)
        {
            var dentistOrders = await _repo.GetDentistOrdersWithItemsAsync(dentistId);
            if (!dentistOrders.Any())
            {
                return new List<CaseOrderInvoiceDto>();
            }

            var orderIds = dentistOrders.Select(o => o.Id).ToList();

            var existingInvoices = await _repo.GetInvoicesByOrderIdsAsync(orderIds);

            var existingInvoicesMap = existingInvoices
                .Where(i => i.CaseOrderId.HasValue)
                .ToDictionary(i => i.CaseOrderId!.Value);

            List<OrderInvoice> newInvoicesToSave = new();
            List<CaseOrderInvoiceDto> finalInvoicesResult = new();

            foreach (var order in dentistOrders)
            {
                decimal totalOrderPrice = 0;
                var itemsDtoList = new List<CaseOrderInvoiceItemDto>();
                var invoiceItemsToSave = new List<OrderInvoiceItem>();

                if (existingInvoicesMap.TryGetValue(order.Id, out var savedInvoice))
                {
                    foreach (var savedItem in savedInvoice.InvoiceItems)
                    {
                        itemsDtoList.Add(new CaseOrderInvoiceItemDto
                        {
                            CaseOrderItemId = savedItem.Id,
                            CaseOrderId = order.Id,
                            CompensationType = Enum.Parse<DentalLab.Api.Models.CompensationType>(savedItem.CompensationType),
                            ToothNumbers = string.IsNullOrEmpty(savedItem.ToothNumbers)
                                ? new List<int>()
                                : savedItem.ToothNumbers.Split(',').Select(int.Parse).ToList(),
                            UnitPrice = savedItem.UnitPrice,
                            LineTotal = savedItem.LineTotal
                        });
                    }

                    finalInvoicesResult.Add(new CaseOrderInvoiceDto
                    {
                        CaseOrderId = order.Id,
                        Status = order.Status.ToString(),
                        EstimatedTotal = savedInvoice.TotalAmount,
                        Message = "هذه فاتورة معتمدة ومخزنة مسبقاً في النظام.",
                        Items = itemsDtoList
                    });

                    continue;
                }

                foreach (var item in order.Items)
                {
                    var price = await _repo.GetLabPriceAsync(order.AssignedLabId!.Value, item.CompensationType);
                    decimal unitPrice = price?.UnitPrice ?? 0;
                    int teethCount = item.ToothNumbers?.Count ?? 0;
                    decimal lineTotal = unitPrice * teethCount;

                    totalOrderPrice += lineTotal;

                    itemsDtoList.Add(new CaseOrderInvoiceItemDto
                    {
                        CaseOrderId = order.Id,
                        CompensationType = item.CompensationType,
                        ToothNumbers = item.ToothNumbers ?? new List<int>(),
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal
                    });

                    invoiceItemsToSave.Add(new OrderInvoiceItem
                    {
                        CompensationType = item.CompensationType.ToString(),
                        ToothNumbers = string.Join(",", item.ToothNumbers ?? new List<int>()),
                        UnitPrice = unitPrice,
                        TeethCount = teethCount,
                        LineTotal = lineTotal
                    });
                }

                var newInvoice = new OrderInvoice
                {
                    CaseOrderId = order.Id,
                    TotalAmount = totalOrderPrice,
                    CreatedAt = DateTime.UtcNow,
                    InvoiceItems = invoiceItemsToSave
                };

                newInvoicesToSave.Add(newInvoice);

                order.EstimatedPrice = totalOrderPrice;
                await _repo.UpdateOrderAsync(order);

                finalInvoicesResult.Add(new CaseOrderInvoiceDto
                {
                    CaseOrderId = order.Id,
                    Status = order.Status.ToString(),
                    EstimatedTotal = totalOrderPrice,
                    Message = "هذه فاتورة تقديرية للسعر النهائي، وقد يحدث اختلاف بسيط عند اعتماد المخبر.",
                    Items = itemsDtoList
                });
            }

            if (newInvoicesToSave.Any())
            {
                await _repo.AddInvoicesRangeAsync(newInvoicesToSave);
            }

            return finalInvoicesResult;
        }
    }
}
