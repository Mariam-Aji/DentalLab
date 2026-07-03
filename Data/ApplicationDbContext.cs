using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace DentalLab.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Lab> Labs { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<CaseOrder> CaseOrders { get; set; }
    public DbSet<CaseOrderItem> CaseOrderItems { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ConnectionRequest> ConnectionRequests { get; set; }
    public DbSet<ScanVisitRequest> ScanVisitRequests { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<LabPrice> LabPrices { get; set; }
    public DbSet<LabSubscriptionPayment> LabSubscriptionPayments { get; set; }
    public DbSet<FileResource> FileResources { get; set; }
    public DbSet<EmailOtp> EmailOtps { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<LabScanSlot> LabScanSlots { get; set; }
    public DbSet<Advertisement> Advertisements { get; set; }
    public DbSet<OrderInvoice> OrderInvoices { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OrderInvoice>()
        .HasOne(i => i.CaseOrder)
        .WithOne(o => o.Invoice)
        .HasForeignKey<OrderInvoice>(i => i.CaseOrderId)
        .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrderInvoiceItem>()
        .HasOne(item => item.OrderInvoice)
        .WithMany(invoice => invoice.InvoiceItems)
        .HasForeignKey(item => item.OrderInvoiceId)
        .OnDelete(DeleteBehavior.Cascade); // ?? Cascade ??? ???? ??????

        // 3. ??? ??? ?????? ??????? (Precision) ???? ????? ??????? ??????????
        modelBuilder.Entity<OrderInvoice>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2); // 18 ??? ???? ????? ??? ???????

        modelBuilder.Entity<OrderInvoiceItem>()
            .Property(item => item.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderInvoiceItem>()
            .Property(item => item.LineTotal)
            .HasPrecision(18, 2);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<CaseOrder>()
         .HasOne(co => co.Invoice)
         .WithOne(i => i.CaseOrder)
         .HasForeignKey<OrderInvoice>(i => i.CaseOrderId)
         .OnDelete(DeleteBehavior.Cascade); 
        modelBuilder.Entity<OrderInvoice>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Advertisement>()
        .Property(a => a.Target)
        .HasConversion<int>();
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.BlogPost)                  
            .WithMany()                                  
            .HasForeignKey(n => n.BlogPostId)  ;           
        modelBuilder.Entity<Advertisement>()
                .HasOne(a => a.User)                 
                .WithMany(u => u.Advertisements)    
                .HasForeignKey(a => a.UserId)        
                .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasConversion<string>();

        modelBuilder.Entity<EmailOtp>()
            .HasIndex(o => new { o.UserId, o.Purpose, o.Code })
            .IsUnique();

        modelBuilder.Entity<EmailOtp>()
            .Property(o => o.Purpose)
            .HasConversion<string>();

        modelBuilder.Entity<EmailOtp>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Lab>()
            .HasOne(l => l.Owner)
            .WithOne(u => u.LabProfile)
            .HasForeignKey<Lab>(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lab>()
            .HasMany(l => l.CaseOrders)
            .WithOne(c => c.AssignedLab)
            .HasForeignKey(c => c.AssignedLabId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Lab>()
            .HasMany(l => l.SubscriptionPayments)
            .WithOne(p => p.Lab)
            .HasForeignKey(p => p.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabPrice>()
            .HasOne(p => p.Lab)
            .WithMany(l => l.Prices)
            .HasForeignKey(p => p.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Lab>()
            .Property(l => l.Availability)
            .HasConversion<string>();

        modelBuilder.Entity<Patient>()
            .HasMany(p => p.CaseOrders)
            .WithOne(c => c.Patient)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CaseOrder>()
            .HasOne(c => c.CreatedBy)
            .WithMany(u => u.CreatedCases)
            .HasForeignKey(c => c.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CaseOrderItem>()
            .HasOne(i => i.CaseOrder)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CaseOrder>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<CaseOrderItem>()
            .Property(i => i.CompensationType)
            .HasConversion<string>();

        modelBuilder.Entity<CaseOrder>()
            .Property(c => c.ImpressionType)
            .HasConversion<string>();

        modelBuilder.Entity<CaseOrderItem>()
            .Property(i => i.ToothNumbers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null) ?? new List<int>())
            .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                (l, r) => l.SequenceEqual(r),
                v => v.Aggregate(0, (a, b) => HashCode.Combine(a, b)),
                v => v.ToList()));

        modelBuilder.Entity<Template>()
            .Property(t => t.Compensation)
            .HasConversion<string>();

        modelBuilder.Entity<Template>()
            .Property(t => t.DefaultImpression)
            .HasConversion<string>();

      
        modelBuilder.Entity<LabScanSlot>()
                .HasOne(slot => slot.Lab)          // ?????? ???? ????? ???? ????
                .WithMany(lab => lab.ScanSlots)    // ?????? ????? ???? ?? ???? (ScanSlots)
                .HasForeignKey(slot => slot.LabId) // ????? ??? ??????? ??????? ?? ???? ????????
                .OnDelete(DeleteBehavior.Cascade); // ??? ?? ??? ???????? ??? ??? ???? ??????? ???????? ?? ??? database

        // ??? ????? ????? ?? ?????? (?? ????? ??? IsUnique ? Multiplicity)
        modelBuilder.Entity<ScanVisitRequest>()
            .HasOne(request => request.Slot)
            .WithMany()
            .HasForeignKey(request => request.LabScanSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // ????? ????? ???????? ?? ?????? ????? ???????? ???????? (Multiple Cascade Paths)
        modelBuilder.Entity<ScanVisitRequest>()
            .HasOne(request => request.Lab)
            .WithMany(lab => lab.ScanVisitRequests)
            .HasForeignKey(request => request.LabId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Recipient)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileResource>()
            .Property(f => f.Type)
            .HasConversion<string>();

        modelBuilder.Entity<FileResource>()
            .HasOne(f => f.BlogPost)
            .WithMany(b => b.Attachments)
            .HasForeignKey(f => f.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileResource>()
            .HasOne(f => f.CaseOrder)
            .WithMany(c => c.Files)
            .HasForeignKey(f => f.CaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileResource>()
            .HasOne(f => f.Patient)
            .WithMany(p => p.Files)
            .HasForeignKey(f => f.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FileResource>()
            .HasOne(f => f.Lab)
            .WithMany(l => l.Gallery)
            .HasForeignKey(f => f.LabId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConnectionRequest>()
            .Property(c => c.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ConnectionRequest>()
            .HasOne(c => c.FromDentist)
            .WithMany(u => u.SentConnectionRequests)
            .HasForeignKey(c => c.FromDentistId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConnectionRequest>()
            .HasOne(c => c.ToLab)
            .WithMany(l => l.ConnectionRequests)
            .HasForeignKey(c => c.ToLabId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlogPost>()
            .Property(b => b.Type)
            .HasConversion<string>();

        modelBuilder.Entity<BlogPost>()
            .HasOne(b => b.Author)
            .WithMany()
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LabPrice>()
            .Property(p => p.CompensationType)
            .HasConversion<string>();

        modelBuilder.Entity<LabSubscriptionPayment>()
            .Property(p => p.Method)
            .HasConversion<string>();
    }
}