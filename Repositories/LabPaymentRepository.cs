using System.Threading.Tasks;
using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class LabPaymentRepository : ILabPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public LabPaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CaseOrder?> GetTargetOrderAsync(int orderId)
        {
            return await _context.CaseOrders
                .Include(o => o.Patient)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<OrderInvoice?> GetInvoiceByOrderIdAsync(int orderId)
        {
            return await _context.OrderInvoices.FirstOrDefaultAsync(i => i.CaseOrderId == orderId);
        }

        //public async Task<OrderInvoice?> GetInvoiceByGatewayReferenceAsync(string gatewayId)
        //{
        //    return await _context.OrderInvoices.FirstOrDefaultAsync(i => i.MyFatoorahInvoiceId == gatewayId);
        //}

        public async Task SaveInvoiceAsync(OrderInvoice invoice)
        {
            await _context.OrderInvoices.AddAsync(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task CommitInvoiceChangesAsync(OrderInvoice invoice)
        {
            _context.OrderInvoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrderStatusAsync(CaseOrder order)
        {
            _context.CaseOrders.Update(order);
            await _context.SaveChangesAsync();
        }
    }
}