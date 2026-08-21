using DentalLab.Api.Dtos;
using DentalLab.Api.DTOs;
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

            if (!string.IsNullOrWhiteSpace(itemDto.Notes))
            {
                order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? itemDto.Notes
                    : $"{order.Notes}\n{itemDto.Notes}";
            }

            await _repo.UpdateOrderAsync(order);

            return new CaseOrderItemResponseDto
            {
                CaseOrderId = order.Id,
                CaseOrderItemId = newItem.Id,
                Status = order.Status.ToString(),
                CompensationType = itemDto.CompensationType,
                ToothNumbers = itemDto.ToothNumbers,
                Notes = order.Notes 
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

        public async Task<(CreatePatientDto? result, string? error)> AddPatientToCaseOrderAsync(int caseOrderId, CreatePatientDto patientDto, IWebHostEnvironment env)
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
                return (null, "فشل في حفظ بيانات المريض بقاعدة البيانات.");
            }

            var uploadedFileDtos = new List<PatientFileResponseDto>();

            // 🌟 التحقق من وجود مجموعة صور ومعالجتها تكرارياً
            if (patientDto.Photos != null && patientDto.Photos.Any())
            {
                var fileResources = new List<FileResource>();
                var uploadsRoot = Path.Combine(env.ContentRootPath, "uploads", "patients", newPatient.Id.ToString());
                Directory.CreateDirectory(uploadsRoot);

                foreach (var photo in patientDto.Photos)
                {
                    if (photo.Length > 0)
                    {
                        var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
                        var allowed = new[] { ".jpg", ".jpeg", ".png", ".stl" }; // أضف الامتدادات المسموحة
                        if (!allowed.Contains(ext)) continue;

                        var fileName = $"{Guid.NewGuid():N}{ext}";
                        var fullPath = Path.Combine(uploadsRoot, fileName);

                        await using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }

                        var relativePath = Path.Combine("uploads", "patients", newPatient.Id.ToString(), fileName).Replace("\\", "/");

                        // إضافة كل صورة للقائمة مع تحديد نوعها
                        fileResources.Add(new FileResource
                        {
                            Path = relativePath,
                            Type = FileType.PhotoBefore, // 👈 نوع الصورة مخزن كـ "صور قبل"
                            PatientId = newPatient.Id
                        });
                    }
                }

                // حفظ جميع الصور دفعة واحدة في قاعدة البيانات
                if (fileResources.Any())
                {
                    await _repo.AddPatientFilesAsync(fileResources);

                    foreach (var file in fileResources)
                    {
                        uploadedFileDtos.Add(new PatientFileResponseDto
                        {
                            FileId = file.Id,
                            Path = file.Path,
                            FileType = file.Type.ToString() // ستظهر كـ "PhotoBefore"
                        });
                    }
                }
            }

            patientDto.PatientId = newPatient.Id;
            patientDto.CaseOrderId = caseOrderId;
            patientDto.Photos = null; // إخفاء ملفات الـ IFormFile الخام
            patientDto.UploadedFiles = uploadedFileDtos; // إظهار المسارات وأنواعها فقط

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
                ProcessedTeeth = p.ProcessedTeeth ?? new List<string>(),

                // 👈 استخدام p.Files بدلاً من p.FileResources
                UploadedFiles = p.Files?
                    .Select(f => new PatientFileResponseDto
                    {
                        FileId = f.Id,
                        Path = f.Path,
                        FileType = f.Type.ToString() // ستظهر نوع الملف (مثلاً PhotoBefore, XRay, إلخ)
                    }).ToList() ?? new List<PatientFileResponseDto>()
            }).ToList();
        }

        //
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

             
                var lab = await _repo.GetLabByIdAsync(labId);
                int targetUserId = lab != null ? lab.UserId : labId;

                string alertText = $"قام الطبيب بتعديل الطلبية رقم ({caseOrderId}) وإضافة عناصر تعويضية جديدة، بانتظار مراجعتكم وتحديد السعر النهائي.";

                var notification = new Notification
                {
                    RecipientId = targetUserId, // تم التعديل لاستخدام UserId
                    Message = alertText,
                    Type = NotificationType.StatusChanged,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.SaveNotificationAsync(notification);

                string labGroupId = $"Lab_{labId}";

                await _hubContext.Clients.Group(labGroupId).SendAsync("ReceiveOrderNotification", alertText);

                await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("ReceiveOrderNotification", alertText);
                // ==========================================

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
                    RecipientId = lab.UserId, // هنا الكود الخاص بك كان صحيحاً من البداية
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

                // هنا أيضاً الكود الخاص بك كان صحيحاً
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
        public async Task<object> GetDentistOrdersTrackingAsync(int dentistId)
        {
            return await _repo.GetDentistOrdersWithDetailsAsync(dentistId);
        }
        public async Task<List<object>> GetOrdersByDentistAndLabAsync(int dentistId, int labId)
        {
            return await _repo.GetOrdersByDentistAndLabAsync(dentistId, labId);
        }
        public async Task<DentistOwnProfileDetailsDto?> FetchDentistPersonalProfileAsync(int userId)
        {
            var user = await _repo.GetUserByIdAsync(userId);
            if (user == null) return null;

            return new DentistOwnProfileDetailsDto
            {
                DentistId = user.Id,
                Name = user.Name, // 👈 تعيين اسم الطبيب المجلوب من جدول المستخدمين
                Email = user.Email,
                Phone = user.Phone,
                CityPlace = user.CityPlace,
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }
        public async Task<(DentistOwnProfileDetailsDto? Profile, string? Error)> ModifyDentistPersonalProfileAsync(int userId, EditDentistOwnProfileDto dto)
        {
            var user = await _repo.GetUserByIdAsync(userId);
            if (user == null) return (null, "المستخدم غير موجود.");

            // 🌟 تحديث الاسم إذا تم إرساله ولم يكن فارغاً
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                user.Name = dto.Name;
            }

            if (dto.Phone != null) user.Phone = dto.Phone;
            if (dto.CityPlace != null) user.CityPlace = dto.CityPlace;

            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                if (dto.ProfilePicture.Length > 5 * 1024 * 1024)
                    return (null, "حجم الصورة كبير جداً (الحد الأقصى 5 ميغابايت).");

                var ext = Path.GetExtension(dto.ProfilePicture.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png" };
                if (!allowed.Contains(ext))
                    return (null, "امتداد الصورة غير مدعوم.");

                var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "dentists", userId.ToString(), "profile");
                Directory.CreateDirectory(uploadsRoot);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsRoot, fileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.ProfilePicture.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldFullPath = Path.Combine(_env.ContentRootPath, user.ProfilePictureUrl.Replace("/", "\\"));
                    if (File.Exists(oldFullPath))
                    {
                        try { File.Delete(oldFullPath); } catch { }
                    }
                }

                user.ProfilePictureUrl = Path.Combine("uploads", "dentists", userId.ToString(), "profile", fileName).Replace("\\", "/");
            }

            var success = await _repo.UpdateUserAsync(user);
            if (!success) return (null, "فشل في حفظ التعديلات بقاعدة البيانات.");

            var updatedProfile = new DentistOwnProfileDetailsDto
            {
                DentistId = user.Id,
                Name = user.Name, // 👈 إرجاع الاسم المحدث في الاستجابة
                Email = user.Email,
                Phone = user.Phone,
                CityPlace = user.CityPlace,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return (updatedProfile, null);
        }
        public async Task<List<CompensationDemandChartDto>> GetCompensationDemandChartDataAsync()
        {
            return await _repo.GetCompensationDemandChartDataAsync();
        }
        public async Task<List<CaseStatusCountChartDto>> GetCaseStatusChartDataAsync()
        {
            return await _repo.GetCaseStatusChartDataAsync();
        }
        public async Task<List<PatientCaseOrderDto>> GetOrdersByPatientIdAsync(int patientId)
        {
            var orders = await _repo.GetOrdersByPatientIdAsync(patientId);

            return orders.Select(o => new PatientCaseOrderDto
            {
                Id = o.Id,
                Title = o.Title,
                Status = o.Status.ToString(),
                ImpressionStage = o.ImpressionStage.ToString(),
                ImpressionType = o.ImpressionType.ToString(),
                Shade = o.Shade,
                IsUrgent = o.IsUrgent,
                IsTemporary = o.IsTemporary,
                DeliveryDate = o.DeliveryDate,
                Notes = o.Notes,
                EstimatedPrice = o.EstimatedPrice,
                FinalPrice = o.FinalPrice,
                IsPaid = o.IsPaid,
                PaidAt = o.PaidAt,
                CreatedAt = o.CreatedAt,

                PatientId = o.PatientId ?? 0,
                PatientName = o.Patient?.FullName ?? "",
                CreatedById = o.CreatedById,
                DentistName = o.CreatedBy?.Name ?? "",
                AssignedLabId = o.AssignedLabId,
                LabName = o.AssignedLab?.Description ?? (o.AssignedLabId.HasValue ? $"Lab #{o.AssignedLabId}" : null)
            }).ToList();
        
        }
        public async Task<List<PatientCaseOrderDto>> GetOrdersWithPatientsAsync()
        {
            var orders = await _repo.GetOrdersWithPatientsAsync();

            return orders.Select(o => new PatientCaseOrderDto
            {
                Id = o.Id,
                Title = o.Title,
                Status = o.Status.ToString(),
                ImpressionStage = o.ImpressionStage.ToString(),
                ImpressionType = o.ImpressionType.ToString(),
                Shade = o.Shade,
                IsUrgent = o.IsUrgent,
                IsTemporary = o.IsTemporary,
                DeliveryDate = o.DeliveryDate,
                Notes = o.Notes,
                EstimatedPrice = o.EstimatedPrice,
                FinalPrice = o.FinalPrice,
                IsPaid = o.IsPaid,
                PaidAt = o.PaidAt,
                CreatedAt = o.CreatedAt,

                PatientId = o.PatientId ?? 0,
                PatientName = o.Patient?.FullName ?? "",
                CreatedById = o.CreatedById,
                DentistName = o.CreatedBy?.Name ?? "",
                AssignedLabId = o.AssignedLabId,
                LabName = o.AssignedLab?.Description ?? (o.AssignedLabId.HasValue ? $"Lab #{o.AssignedLabId}" : null)
            }).ToList();
        }
        public async Task<string> SendFullOrderNotificationToLabAsync(int orderId, int labId, int dentistId)
        {
            // 1. جلب الطلبية مع تفاصيلها وعلاقاتها (بما فيها المخبر وصاحبه إن وُجد في الـ Include)
            var order = await _repo.GetOrderWithDetailsAsync(orderId);

            if (order == null)
                return "الطلبية غير موجودة.";

            if (order.CreatedById != dentistId)
                return "هذه الطلبية لا تخص هذا الطبيب.";

            if (order.AssignedLabId != labId)
                return "هذه الطلبية غير مرسلة لهذا المخبر.";

            // 2. الحصول على معرف المستخدم الخاص بالمخبر مباشرة من علاقة الـ AssignedLab
            // (تأكد أن جدول Lab يحتوي على حقل UserId أو أن العلاقة تضم المستخدم المرتبط بالمخبر)
            int? labOwnerUserId = order.AssignedLab?.UserId; // إذا كان حقل UserId موجوداً في جدول Lab

            if (labOwnerUserId == null)
            {
                // إذا لم يكن مخزناً مباشرة، يمكنك جلبه عبر استعلام من المستودع باستخدام الـ labId
                labOwnerUserId = await _repo.GetLabOwnerUserIdAsync(labId);
            }

            if (labOwnerUserId == null)
                return "لم يتم العثور على حساب مستخدم مرتبط بهذا المخبر.";

            // 3. بناء نص الإشعار
            var notificationMessage = $"تلقيت تفاصيل الطلبية الشاملة رقم #{order.Id} ('{order.Title}') من الطبيب.";

            var notification = new Notification
            {
                RecipientId = labOwnerUserId.Value,
                Type = NotificationType.InfoRequested,
                Message = notificationMessage,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddNotificationAsync(notification);

            var notificationPayload = new
            {
                notification.Id,
                notification.Message,
                notification.Type,
                notification.CreatedAt,
                OrderDetails = new
                {
                    order.Id,
                    order.Title,
                    order.Status,
                    order.ImpressionStage,
                    order.ImpressionType,
                    order.Shade,
                    order.IsTemporary,
                    order.IsUrgent,
                    order.DeliveryDate,
                    order.Notes,
                    order.HasAccessories,
                    order.RequiredImages,
                    order.EstimatedPrice,
                    order.FinalPrice,
                    order.IsPaid,
                    order.PaidAt,
                    order.CreatedAt,
                    Items = order.Items.Select(item => new
                    {
                        item.Id,
                        item.CompensationType,
                        item.ToothNumbers
                    }),
                    order.PatientId
                }
            };

            // 5. إرسال الإشعار عبر SignalR حصراً للمخبر المستهدف
            await _hubContext.Clients.User(labOwnerUserId.Value.ToString())
                .SendAsync("ReceiveOrderNotification", notificationPayload);

            return "تم إرسال إشعار تفاصيل الطلبية للمخبر بنجاح.";
        }

        public async Task<(bool Success, string? Message)> SendReplyToLabAsync(int caseOrderId, int dentistUserId, ReplyToLabDto dto)
        {
            try
            {
                // 1. جلب الطلبية للتأكد من وجودها
                var order = await _repo.GetOrderByIdAsync(caseOrderId);
                if (order == null)
                    return (false, "الطلبية غير موجودة.");

                // 2. التأكد من أن الطبيب الذي يحاول الإرسال هو صاحب الطلبية
                if (order.CreatedById != dentistUserId)
                    return (false, "غير مصرح لك بإضافة ملاحظات لهذه الطلبية.");

                // 3. التأكد من أن الطلبية مسندة لمخبر
                if (order.AssignedLabId == null)
                    return (false, "لا يمكن إرسال رد، الطلبية غير مسندة لأي مخبر بعد.");

                // 4. تحديث الملاحظات (يمكنك استبدال النص أو الإضافة عليه، هنا سنقوم بالإضافة مع فاصل سطر إذا كان هناك ملاحظات سابقة)
                if (string.IsNullOrWhiteSpace(order.Notes))
                {
                    order.Notes = dto.Notes;
                }
                else
                {
                    order.Notes += $"\nرد الطبيب: {dto.Notes}";
                }

                await _repo.UpdateOrderAsync(order);

                // ==========================================
                // 5. آلية الإشعار الناجحة بدقة:
                // جلب بيانات المخبر للحصول على UserId الحقيقي
                // ==========================================
                var lab = await _repo.GetLabByIdAsync(order.AssignedLabId.Value);
                if (lab == null)
                    return (false, "لم يتم العثور على بيانات المخبر المرتبط بالطلبية.");

                int targetUserId = lab.UserId;

                string alertText = $"قام الطبيب بإرسال توضيح/ملاحظات جديدة للطلبية رقم ({caseOrderId}).";

                var notification = new Notification
                {
                    RecipientId = targetUserId,
                    Message = alertText,
                    Type = NotificationType.StatusChanged, // تم التعديل لتتوافق مع الأنواع المعرفة لديك
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.SaveNotificationAsync(notification);

                // إرسال الإشعار الفوري (SignalR) للمجموعة وللمستخدم بدقة
                string labGroupId = $"Lab_{order.AssignedLabId.Value}";

                await _hubContext.Clients.Group(labGroupId).SendAsync("ReceiveOrderNotification", alertText);
                await _hubContext.Clients.User(targetUserId.ToString()).SendAsync("ReceiveOrderNotification", alertText);
                // ==========================================

                return (true, "تم إرسال الرد للمخبر بنجاح.");
            }
            catch (Exception ex)
            {
                return (false, $"حدث خطأ داخلي أثناء معالجة الرد: {ex.Message}");
            }
        }
    }
}

