using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabInvoiceService : ILabInvoiceService
{
    private readonly ApplicationDbContext   _db;
    private readonly ILabInvoiceSyncService _sync;

    public LabInvoiceService(ApplicationDbContext db, ILabInvoiceSyncService sync)
    {
        _db   = db;
        _sync = sync;
    }

    public async Task<(LabPaidInvoicesResponseDto? result, string? error)> GetPaidInvoicesAsync(int userId)
    {
        // جلب معرف المخبر
        var labId = await _db.Labs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (labId == null)
            return (null, "Lab not found.");

        // ─── جلب الطلبيات المدفوعة مع بنودها ────────────────────────
        var paidOrders = await _db.CaseOrders
            .Include(o => o.Items)
            .Where(o => o.AssignedLabId == labId && o.IsPaid)
            .ToListAsync();

        // تأكد أن كل فاتورة صحيحة ومكتملة — يصلح أي سعر صفر أو بند ناقص
        foreach (var order in paidOrders)
            await _sync.EnsureInvoiceItemsAsync(order);

        // ─── جلب الفواتير من الـ DB بعد ضمان صحتها ──────────────────
        var orderIds = paidOrders.Select(o => o.Id).ToList();

        var orderInvoices = await _db.OrderInvoices
            .AsNoTracking()
            .Include(i => i.InvoiceItems)
            .Include(i => i.CaseOrder)
                .ThenInclude(o => o!.CreatedBy)
            .Where(i => i.CaseOrderId.HasValue && orderIds.Contains(i.CaseOrderId.Value))
            .OrderByDescending(i => i.CaseOrder!.PaidAt ?? i.CreatedAt)
            .Select(i => new LabPaidOrderInvoiceDto
            {
                InvoiceId      = i.Id,
                CaseOrderId    = i.CaseOrder!.Id,
                CaseOrderTitle = i.CaseOrder.Title,
                DentistName    = i.CaseOrder.CreatedBy != null ? i.CaseOrder.CreatedBy.Name : string.Empty,
                FinalPrice     = i.CaseOrder.FinalPrice,
                TotalAmount    = i.TotalAmount,
                PaidAt         = i.CaseOrder.PaidAt,
                Items          = i.InvoiceItems.Select(item => new LabPaidOrderInvoiceItemDto
                {
                    CompensationType = item.CompensationType,
                    ToothNumbers     = item.ToothNumbers,
                    UnitPrice        = item.UnitPrice,
                    TeethCount       = item.TeethCount,
                    LineTotal        = item.LineTotal,
                    PriceNote        = item.UnitPrice == 0
                        ? $"لم يتم تحديد سعر لنوع التعويض \"{item.CompensationType}\" من قبل المخبر"
                        : null
                }).ToList()
            })
            .ToListAsync();

        // ─── فواتير الإعلانات المدفوعة ───────────────────────────────
        var adInvoices = await _db.Advertisements
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsPaid)
            .OrderByDescending(a => a.PaidAt ?? a.CreatedAt)
            .Select(a => new LabPaidAdInvoiceDto
            {
                AdvertisementId = a.Id,
                AdTitle         = a.Title,
                AdContent       = a.Content,
                Price           = a.Price ?? 0,
                PaidAt          = a.PaidAt
            })
            .ToListAsync();

        return (new LabPaidInvoicesResponseDto
        {
            OrderInvoices = orderInvoices,
            AdInvoices    = adInvoices
        }, null);
    }
}
