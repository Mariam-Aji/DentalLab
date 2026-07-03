using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        // 1. جلب الطلبية والتحقق من وجودها
        var caseOrder = await _context.CaseOrders
            .FirstOrDefaultAsync(c => c.Id == caseOrderId);

        if (caseOrder == null)
        {
            throw new KeyNotFoundException("الطلبية غير موجودة.");
        }

        // 2. التحقق من الصلاحية (أن الطبيب الحالي هو مالك الطلبية)
        if (caseOrder.CreatedById != dentistId)
        {
            throw new UnauthorizedAccessException("غير مسموح لك بالتعديل على هذه الطلبية، أنت لست الطبيب المنشئ لها.");
        }

        // 3. إنشاء كائن المريض الجديد
        var newPatient = new Patient
        {
            FullName = patientDto.FullName,
            Age = patientDto.Age,
            ClinicalNotes = patientDto.ClinicalNotes,
            ProcessedTeeth = patientDto.ProcessedTeeth
        };

        // 4. إضافة المريض للسياق (دون حفظ فوري في الداتا بيز)
        _context.Patients.Add(newPatient);

        // 5. ربط المريض بالطلبية مباشرة (Entity Framework ذكي بما يكفي لربطهما معاً)
        caseOrder.Patient = newPatient; // أو caseOrder.PatientId = newPatient.Id; بعد الحفظ، ولكن هذه الطريقة أفضل برمجياً

        _context.CaseOrders.Update(caseOrder);

        // 🌟 6. حفظ التغييرات كاملة دفعة واحدة في قاعدة البيانات (Single Transaction)
        await _context.SaveChangesAsync();

        return newPatient;
    }
}