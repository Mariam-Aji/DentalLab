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
            if (file == null || file.Length == 0)
                throw new Exception("File is empty");

            var ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".stl")
                throw new Exception("Only STL files are allowed");

            var caseOrder = await _context.CaseOrders.FindAsync(caseOrderId);

            if (caseOrder == null)
                throw new Exception("CaseOrder not found");

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
    }
}