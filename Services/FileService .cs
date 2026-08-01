using DentalLab.Api.Data;
using DentalLab.Api.Models;
using DentalLab.Api.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DentalLab.Api.Services
{
    public class FileService : IFileService
    {
        private readonly IFileResourceRepository _fileRepo;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public FileService(
            IFileResourceRepository fileRepo,
            IWebHostEnvironment env,
            ApplicationDbContext context)
        {
            _fileRepo = fileRepo;
            _env = env;
            _context = context;
        }

        public async Task<string> UploadStlToCaseAsync(int caseOrderId, IFormFile file)
        {
            // 1. التحقق من وجود الملف
            if (file == null || file.Length == 0)
                throw new ArgumentException("لم يتم رفع أي ملف، يرجى اختيار ملف صالح.");

            // 2. التحقق من اللاحقة (حصر الرفع بـ .stl فقط)
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".stl")
                throw new InvalidOperationException("عذراً، يرجى رفع ملف بصيغة STL حصراً (.stl).");

            // 3. التحقق من وجود الطلب
            var caseOrder = await _context.CaseOrders.FindAsync(caseOrderId);

            if (caseOrder == null)
                throw new KeyNotFoundException("الطلب غير موجود.");

            // 4. إعداد مسار الحفظ
            var uploadsPath = Path.Combine(
                _env.ContentRootPath,
                "uploads",
                "cases",
                caseOrderId.ToString(),
                "stl");

            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine(
                "uploads",
                "cases",
                caseOrderId.ToString(),
                "stl",
                fileName).Replace("\\", "/");

            // 5. حفظ السجل في قاعدة البيانات
            var fileEntity = new FileResource
            {
                Path = relativePath,
                Type = FileType.DigitalScan,
                CaseOrderId = caseOrderId
            };

            await _fileRepo.AddAsync(fileEntity);
            await _fileRepo.SaveChangesAsync();

            return relativePath;
        }
        public async Task<List<FileResource>> GetStlFilesByCaseAsync(int caseOrderId)
        {
            var caseOrder = await _context.CaseOrders.FindAsync(caseOrderId);
            if (caseOrder == null)
            {
                throw new Exception("الطلبية غير موجودة.");
            }

            return await _fileRepo.GetStlFilesByCaseOrderIdAsync(caseOrderId);
        }
    }
}