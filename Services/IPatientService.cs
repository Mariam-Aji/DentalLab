using DentalLab.Api.Models;
using System.Threading.Tasks;

public interface IPatientService
{
    Task<Patient> CreatePatientForCaseAsync(int dentistId, int caseOrderId, Patient patientDto);
}