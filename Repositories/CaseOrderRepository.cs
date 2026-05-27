using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories;

public class CaseOrderRepository : ICaseOrderRepository
{
    private readonly ApplicationDbContext _context;
    public CaseOrderRepository(ApplicationDbContext context) => _context = context;

    public async Task<CaseOrder> CreateOrderAsync(CaseOrder order)
    {
        await _context.CaseOrders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<CaseOrder?> GetOrderByIdAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task UpdateOrderAsync(CaseOrder order)
    {
        _context.CaseOrders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task AddOrderItemAsync(CaseOrderItem item)
    {
        await _context.CaseOrderItems.AddAsync(item);
        await _context.SaveChangesAsync();
    }

    public async Task<LabPrice?> GetUnitPriceAsync(int labId, CompensationType type)
    {
        return await _context.LabPrices
            .FirstOrDefaultAsync(lp => lp.LabId == labId && lp.CompensationType == type);
    }

    public async Task<bool> IsDentistConnectedToLab(int dentistId, int labId)
    {
        return await _context.ConnectionRequests.AnyAsync(cr =>
            cr.FromDentistId == dentistId &&
            cr.ToLabId == labId &&
            cr.Status == ConnectionRequestStatus.Accepted);
    }
    public async Task<CaseOrder?> GetOrderWithItemsAsync(int orderId)
    {
        return await _context.CaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId);
    }

    public async Task<LabPrice?> GetLabPriceAsync(int labId, CompensationType type)
    {
        return await _context.LabPrices
            .FirstOrDefaultAsync(x => x.LabId == labId && x.CompensationType == type);
    }
    public async Task<bool> AddPatientAndBindToOrderAsync(CaseOrder order, Patient patient)
    {
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(); 

        order.PatientId = patient.Id;
        _context.CaseOrders.Update(order);

        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<List<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients.ToListAsync();
    }

    public async Task<Patient?> GetPatientByIdAsync(int patientId)
    {
        return await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
    }
    public async Task<Patient?> GetPatientWithFilesByIdAsync(int patientId)
    {
        return await _context.Patients
            .Include(p => p.Files) 
            .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    public async Task<bool> UpdatePatientAsync(Patient patient)
    {
        _context.Patients.Update(patient);
        return await _context.SaveChangesAsync() > 0;
    }
    //
}