using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _context; // أو اسم الـ DbContext لديكِ

    public PatientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient> CreatePatientForCaseAsync(int dentistId, int caseOrderId, Patient patientDto)
    {
        // 1. جلب الطلبية والتحقق من وجودها ومن أن الطبيب الحالي هو من أنشأها
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

        // 2. إنشاء كائن المريض الجديد
        var newPatient = new Patient
        {
            FullName = patientDto.FullName,
            Age = patientDto.Age,
            ClinicalNotes = patientDto.ClinicalNotes,
            ProcessedTeeth = patientDto.ProcessedTeeth,
            // إذا كان هناك ملفات مرفقة قادمة مع المريض يمكنكِ تعيينها هنا أيضاً
        };

        // 3. إضافة المريض لقاعدة البيانات ليأخذ Id
        _context.Patients.Add(newPatient);
        await _context.SaveChangesAsync();

        // 4. ربط الطلبية بالمريض الجديد وتحديث حالة الطلبية إذا لزم الأمر
        caseOrder.PatientId = newPatient.Id;

        _context.CaseOrders.Update(caseOrder);
        await _context.SaveChangesAsync();

        return newPatient;
    }
}