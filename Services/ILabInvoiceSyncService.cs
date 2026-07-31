using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

public interface ILabInvoiceSyncService
{
    /// <summary>
    /// يتحقق من فاتورة الطلبية — إذا كانت غير موجودة أو فاقدة البنود ينشئها/يكملها
    /// يُستدعى فقط عند طلب المخبر الفواتير المدفوعة
    /// </summary>
    Task EnsureInvoiceItemsAsync(CaseOrder order);
}
