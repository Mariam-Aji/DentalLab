using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using DentalLab.Api.Models; 

namespace DentalLab.Api.Dtos;

public class UpdateAdvertisementDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public TargetAudience? Target { get; set; } 
    public DateTime? ExpiresAt { get; set; }
    public List<IFormFile>? ImageFiles { get; set; }
}