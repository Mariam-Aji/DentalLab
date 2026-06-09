using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context; 

    public PatientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient> CreatePatientForCaseAsync(int dentistId, int caseOrderId, Patient patientDto)
    {
        var caseOrder = await _context.CaseOrders
            .FirstOrDefaultAsync(c => c.Id == caseOrderId);

        if (caseOrder == null)
        {
            throw new KeyNotFoundException("الطلبية غير موجودة.");
        }

        if (caseOrder.CreatedById != dentistId)
        {
            throw new UnauthorizedAccessException("غير مسموح لك بالتعديل على هذه الطلبية، أنت لست الطبيب المنشئ لها.");
        }

        var newPatient = new Patient
        {
            FullName = patientDto.FullName,
            Age = patientDto.Age,
            ClinicalNotes = patientDto.ClinicalNotes,
            ProcessedTeeth = patientDto.ProcessedTeeth,
        };

        _context.Patients.Add(newPatient);
        await _context.SaveChangesAsync();

        caseOrder.PatientId = newPatient.Id;

        _context.CaseOrders.Update(caseOrder);
        await _context.SaveChangesAsync();

        return newPatient;
    }
}