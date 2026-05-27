using DentalLab.Api.Data;
using DentalLab.Api.Repositories;
using DentalLab.Api.Services;
using DentalLab.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ? ?? ??????? 1: ??? ??????? ??? Controllers ?? ???? ???? ??? ?? ?? ?????????
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

// ? ?? ??????? 2: ????? ????? CORS ????? ???? ?????? ?? ?? ???? ?? Live Server ????
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLiveServer", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // ???? ??? ??? ???? (Localhost / 127.0.0.1)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<RefreshTokenSettings>(builder.Configuration.GetSection("RefreshTokenSettings"));
builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection("AdminSeed"));

builder.Services.AddScoped<ILabRepository, LabRepository>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IConnectionForLabRepository, ConnectionForLabRepository>();
builder.Services.AddScoped<IConnectionForLabService, ConnectionForLabService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=DentalLabDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IAdminAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ILabGalleryService, LabGalleryService>();
builder.Services.AddScoped<ILabProfileService, LabProfileService>();
builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileResourceRepository, FileResourceRepository>();
builder.Services.AddScoped<IScanVisitService, ScanVisitService>();
builder.Services.AddScoped<IScanVisitRepository, ScanVisitRepository>();
builder.Services.AddScoped<ICaseOrderRepository, CaseOrderRepository>();
builder.Services.AddScoped<ICaseOrderService, CaseOrderService>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DentalLab.Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\""
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("JwtSettings are missing or invalid.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var adminSeed = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(adminSeed.Email) && !string.IsNullOrWhiteSpace(adminSeed.Password))
    {
        var exists = db.Users.Any(u => u.Email == adminSeed.Email.Trim());
        if (!exists)
        {
            var admin = new DentalLab.Api.Models.User
            {
                Name = string.IsNullOrWhiteSpace(adminSeed.Name) ? "System Admin" : adminSeed.Name.Trim(),
                Email = adminSeed.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminSeed.Password),
                Role = DentalLab.Api.Models.UserRole.Admin,
                Status = DentalLab.Api.Models.AccountStatus.Active,
                EmailVerifiedAt = DateTime.UtcNow
            };

            db.Users.Add(admin);
            db.SaveChanges();
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});

app.UseCors("AllowLiveServer");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationHub");
app.MapControllers();

app.Run();
//