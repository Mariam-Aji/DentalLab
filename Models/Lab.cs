using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DentalLab.Api.Models;

public enum AvailabilityStatus { Available, Busy, NotAvailable }

public class Lab
{
    [Key]
    public int Id { get; set; }
    public string? Description { get; set; }
    [Required]
    public int UserId { get; set; }
    public User Owner { get; set; } = null!;

    public int YearsOfExperience { get; set; }
    public List<string> Specialties { get; set; } = new();
    public List<string> Materials { get; set; } = new();
    public AvailabilityStatus Availability { get; set; } = AvailabilityStatus.Available;
    public bool HasScanVisitService { get; set; }
    public double AverageRating { get; set; }



    public DateTime? SubscriptionStartUtc { get; set; }
    public DateTime? SubscriptionEndUtc { get; set; }
    public int SubscriptionGraceDays { get; set; } = 5;

    // Navigation
    public List<CaseOrder> CaseOrders { get; set; } = new();
    public List<Rating> Ratings { get; set; } = new();
    public List<LabPrice> Prices { get; set; } = new();
    public List<ConnectionRequest> ConnectionRequests { get; set; } = new();
    public List<LabSubscriptionPayment> SubscriptionPayments { get; set; } = new();
    public List<FileResource> Gallery { get; set; } = new();
    public List<LabScanSlot> ScanSlots { get; set; } = new();
    public List<ScanVisitRequest> ScanVisitRequests { get; set; } = new();
}