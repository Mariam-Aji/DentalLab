using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class CalendarService : ICalendarService
{
    private readonly ApplicationDbContext _db;

    public CalendarService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<CalendarMonthDto> GetMonthlyCalendarAsync(int labId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        // جلب الطلبات التي تاريخ تسليمها في هذا الشهر لهذا المختبر
        // مستبعد: Pennding (لم يقبلها المختبر بعد) و Cancelled
        var orders = await _db.CaseOrders
            .Where(o =>
                o.AssignedLabId == labId &&
                o.DeliveryDate.HasValue &&
                o.DeliveryDate.Value >= startDate &&
                o.DeliveryDate.Value < endDate &&
                o.Status != CaseStatus.Pennding &&
                o.Status != CaseStatus.Cancelled)
            .Select(o => new { Date = DateOnly.FromDateTime(o.DeliveryDate!.Value) })
            .ToListAsync();

        // جلب مواعيد المسح المحجوزة في هذا الشهر لهذا المختبر
        var scanSlots = await _db.LabScanSlots
            .Where(s =>
                s.LabId == labId &&
                s.IsBooked &&
                s.Date >= startDate &&
                s.Date < endDate)
            .Select(s => new { Date = DateOnly.FromDateTime(s.Date) })
            .ToListAsync();

        // تجميع البيانات حسب اليوم
        var ordersByDay = orders
            .GroupBy(o => o.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var scansByDay = scanSlots
            .GroupBy(s => s.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        // بناء قائمة الأيام (فقط الأيام التي فيها بيانات)
        var allDates = ordersByDay.Keys.Union(scansByDay.Keys).OrderBy(d => d).ToList();

        var days = allDates.Select(date => new CalendarDayDto
        {
            Date = date,
            OrdersCount = ordersByDay.GetValueOrDefault(date, 0),
            ScanVisitsCount = scansByDay.GetValueOrDefault(date, 0)
        }).ToList();

        return new CalendarMonthDto
        {
            Year = year,
            Month = month,
            Days = days
        };
    }

    /// <inheritdoc/>
    public async Task<CalendarDayDetailDto> GetDayDetailAsync(int labId, DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        // ── الطلبات ──────────────────────────────────────────────────────────
        var orders = await _db.CaseOrders
            .Include(o => o.CreatedBy)
            .Include(o => o.Items)
            .Include(o => o.Files)
            .Where(o =>
                o.AssignedLabId == labId &&
                o.DeliveryDate.HasValue &&
                o.DeliveryDate.Value >= dayStart &&
                o.DeliveryDate.Value < dayEnd &&
                o.Status != CaseStatus.Pennding &&
                o.Status != CaseStatus.Cancelled)
            .ToListAsync();

        var orderDtos = orders.Select(o => new CalendarOrderDto
        {
            OrderId              = o.Id,
            Title                = o.Title,
            Status               = o.Status.ToString(),
            ImpressionStage      = o.ImpressionStage.ToString(),
            ImpressionType       = o.ImpressionType.ToString(),
            Shade                = o.Shade,
            IsTemporary          = o.IsTemporary,
            IsUrgent             = o.IsUrgent,
            DeliveryDate         = o.DeliveryDate,
            Notes                = o.Notes,
            EstimatedPrice       = o.EstimatedPrice,
            FinalPrice           = o.FinalPrice,
            IsPaid               = o.IsPaid,
            CreatedAt            = o.CreatedAt,
            HasAccessories       = o.HasAccessories,
            DentistId            = o.CreatedById,
            DentistName          = o.CreatedBy?.Name ?? "",
            DentistEmail         = o.CreatedBy?.Email ?? "",
            DentistPhone         = o.CreatedBy?.Phone,
            DentistClinicAddress = o.CreatedBy?.AddressPlace,
            LabId                = o.AssignedLabId,
            RequiredImages       = o.RequiredImages ?? new List<string>(),
            Items = o.Items.Select(i => new OrderDetailsItemDto
            {
                ItemId           = i.Id,
                CompensationType = i.CompensationType.ToString(),
                ToothNumbers     = i.ToothNumbers
            }).ToList(),
            Files = o.Files.Select(f => new FileDto
            {
                Id         = f.Id,
                Path       = f.Path,
                Type       = f.Type.ToString(),
                UploadedAt = f.UploadedAt
            }).ToList()
        }).ToList();

        
        // 1) المواعيد المحجوزة فقط في هذا اليوم
        var bookedSlots = await _db.LabScanSlots
            .Where(s =>
                s.LabId == labId &&
                s.IsBooked &&
                s.Date >= dayStart &&
                s.Date < dayEnd)
            .OrderBy(s => s.Time)
            .ToListAsync();

        // 2) نجيب الـ ScanVisitRequests مع بيانات الطبيب لهذه المواعيد
        var slotIds = bookedSlots.Select(s => s.Id).ToList();

        var bookingBySlotId = await _db.ScanVisitRequests
            .Include(r => r.Dentist)
            .Where(r => slotIds.Contains(r.LabScanSlotId))
            .ToDictionaryAsync(r => r.LabScanSlotId);

        // 3) ندمجهم
        var scanVisitDtos = bookedSlots.Select(s =>
        {
            bookingBySlotId.TryGetValue(s.Id, out var booking);
            var dentist = booking?.Dentist;
            return new CalendarScanVisitDto
            {
                Id                 = s.Id,
                Time               = s.Time,
                Period             = s.Period.ToString(),
                DoctorName         = dentist?.Name,
                DoctorPhone        = dentist?.Phone,
                DoctorNamePlace    = dentist?.NamePlace,
                DoctorAddressPlace = dentist?.AddressPlace,
                DoctorCityPlace    = dentist?.CityPlace,
                DoctorCountryPlace = dentist?.CountryPlace,
            };
        }).ToList();

        return new CalendarDayDetailDto
        {
            Date       = date,
            Orders     = orderDtos,
            ScanVisits = scanVisitDtos
        };
    }
}
