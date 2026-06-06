using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using DentalLab.Api.Models;

namespace DentalLab.Api.Dtos;

public class CreateAdvertisementDto
{
    public string? Title { get; set; }

    public string? Content { get; set; } = null!;

    public TargetAudience Target { get; set; }

    public IFormFileCollection? ImageFiles { get; set; }

    public DateTime? ExpiresAt { get; set; }
}