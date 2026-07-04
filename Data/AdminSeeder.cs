using DentalLab.Api.Models;
using DentalLab.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DentalLab.Api.Data;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;


        await db.Database.MigrateAsync();

        var email = settings.Email.Trim().ToLower();

        var exists = await db.Users.AnyAsync(u => u.Email == email && u.Role == UserRole.Admin);
        if (exists)
        {
            Console.WriteLine("[AdminSeeder] Admin account already exists. Skipping.");
            return;
        }

        var admin = new User
        {
            Name = settings.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.Password),
            Role = UserRole.Admin,
            Status = AccountStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine($"[AdminSeeder] Admin account created: {admin.Email}");
    }
}
