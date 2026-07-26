using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabInvoiceSyncService : ILabInvoiceSyncService
{
    private readonly ApplicationDbContext _db;

    public LabInvoiceSyncService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task EnsureInvoiceItemsAsync(CaseOrder order)
    {
        if (order.AssignedLabId == null) return;

        int labId = order.AssignedLabId.Value;

        // جلب الفاتورة مع بنودها
        var invoice = await _db.OrderInvoices
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.CaseOrderId == order.Id);

        // جلب بنود الطلبية
        var orderItems = await _db.CaseOrderItems
            .Where(i => i.CaseOrderId == order.Id)
            .ToListAsync();

        if (!orderItems.Any()) return;

        // جلب كل أسعار هذا المخبر (بدون فلتر على النوع — أجلب الكل ونفلتر بالذاكرة)
        var allLabPrices = await _db.LabPrices
            .AsNoTracking()
            .Where(p => p.LabId == labId)
            .ToListAsync();

        // قاموس: اسم النوع (string) → السعر — لتجنب مشاكل enum comparison
        var priceByName = allLabPrices
            .ToDictionary(
                p => p.CompensationType.ToString(),
                p => p.UnitPrice,
                StringComparer.OrdinalIgnoreCase);

        // ─── الحالة 1: لا فاتورة — أنشئها مع كل البنود ──────────────
        if (invoice == null)
        {
            var allItems = BuildInvoiceItems(orderItems, priceByName);

            await _db.OrderInvoices.AddAsync(new OrderInvoice
            {
                CaseOrderId  = order.Id,
                TotalAmount  = allItems.Sum(i => i.LineTotal),
                CreatedAt    = DateTime.UtcNow,
                InvoiceItems = allItems
            });

            await _db.SaveChangesAsync();
            return;
        }

        bool changed = false;

        // ─── الحالة 2: بنود موجودة بسعر صفر وعند المخبر سعر — حدّثها ─
        foreach (var invoiceItem in invoice.InvoiceItems)
        {
            if (invoiceItem.UnitPrice != 0) continue;
            if (!priceByName.TryGetValue(invoiceItem.CompensationType, out var currentPrice)) continue;
            if (currentPrice == 0) continue;

            invoiceItem.UnitPrice = currentPrice;
            invoiceItem.LineTotal = currentPrice * invoiceItem.TeethCount;
            changed = true;
        }

        // ─── الحالة 3: بنود ناقصة — أضفها ──────────────────────────
        var existingTypes = invoice.InvoiceItems
            .Select(i => i.CompensationType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingItems = orderItems
            .Where(oi => !existingTypes.Contains(oi.CompensationType.ToString()))
            .ToList();

        if (missingItems.Any())
        {
            var newItems = BuildInvoiceItems(missingItems, priceByName);
            foreach (var ni in newItems)
                ni.OrderInvoiceId = invoice.Id;

            invoice.InvoiceItems.AddRange(newItems);
            changed = true;
        }

        if (!changed) return;

        invoice.TotalAmount = invoice.InvoiceItems.Sum(i => i.LineTotal);
        _db.OrderInvoices.Update(invoice);
        await _db.SaveChangesAsync();
    }

    private static List<OrderInvoiceItem> BuildInvoiceItems(
        List<CaseOrderItem> items,
        Dictionary<string, decimal> priceByName)
    {
        return items.Select(item =>
        {
            priceByName.TryGetValue(item.CompensationType.ToString(), out decimal unitPrice);
            int teethCount = item.ToothNumbers?.Count ?? 0;

            return new OrderInvoiceItem
            {
                CompensationType = item.CompensationType.ToString(),
                ToothNumbers     = string.Join(",", item.ToothNumbers ?? new List<int>()),
                UnitPrice        = unitPrice,
                TeethCount       = teethCount,
                LineTotal        = unitPrice * teethCount
            };
        }).ToList();
    }
}
