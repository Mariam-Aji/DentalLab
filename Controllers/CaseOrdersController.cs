using DentalLab.Api.Dtos;
using DentalLab.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalLab.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaseOrdersController : ControllerBase
{
    private readonly ICaseOrderService _service;
    public CaseOrdersController(ICaseOrderService service) => _service = service;

    [HttpPost("initiate/{labId}")]
    public async Task<IActionResult> CreateOrderHead([FromForm] CreateCaseOrderDto dto, int labId)
    {
        try
        {
            int dentistId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            List<string> imageUrls = new List<string>();
            if (dto.ImageFiles != null && dto.ImageFiles.Any())
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads/cases");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in dto.ImageFiles)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    string url = $"{Request.Scheme}://{Request.Host}/uploads/cases/{fileName}";
                    imageUrls.Add(url);
                }
            }

            var result = await _service.CreateInitialOrderAsync(dto, dentistId, labId, imageUrls);

            return new JsonResult(result, new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
    [HttpPost("{orderId}/add-item")]
    public async Task<IActionResult> AddItemToOrder(int orderId, [FromForm] CaseOrderItemDto dto)
    {
        try
        {
            int dentistId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.AddItemToOrderAsync(orderId, dto, dentistId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}