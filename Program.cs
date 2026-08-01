
using DentalLab.Api.Data;
// using DentalLab.Api.Hubs; // ?? ??????? ?? ??????? ??? ??? ???? ??? Hub ??????
using DentalLab.Api.Repositories;
using DentalLab.Api.Repositories.Interfaces;
using DentalLab.Api.Services;
using DentalLab.Api.Services.Interfaces;
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

// 1. ????? ??? Controllers ??????? ??? JSON ???? ??????? ????????? ???? ??? Enums ?????
// 1. إعداد الـ Controllers والتعديل هنا لإظهار الحقول حتى لو كانت null
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never; // <--- تم التعديل هنا لكي تظهر الحقول حتى لو كانت null

    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include; // <--- تم التعديل هنا أيضاً لضمان ظهورها مع Newtonsoft إذا تم استخدامها
    });

builder.Services.AddEndpointsApiExplorer();

// 2. ????? ???? ??? SignalR ????????? ???????
builder.Services.AddSignalR();

// 3. ????? ?????? ?????? ??? CORS ??? React (?????? ????????? ??? ????? ??????? ???? Credentials)
builder.Services.AddCors(options =>
{
    // ????? ??? React ??????? ????? ???? ???? Credentials ???? ??? ????? ?? Port 5173
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // <--- ???? ???? ?? withCredentials ?? ??????? ??? SPA ???? SignalR
    });

    // ??????? ?????? ???????
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// 4. ??? ????????? (Settings) ?? ??? ??? appsettings.json
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<RefreshTokenSettings>(builder.Configuration.GetSection("RefreshTokenSettings"));
builder.Services.Configure<AdminSeedSettings>(builder.Configuration.GetSection("AdminSeed"));

// 5. ??? ???? ????? ????????? ?????? (Dependency Injection)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
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
builder.Services.AddScoped<ILabMyFatoorahCodeService, LabMyFatoorahCodeService>();
builder.Services.AddScoped<ILabInvoiceService, LabInvoiceService>();
builder.Services.AddScoped<ILabComplaintService, LabComplaintService>();
builder.Services.AddScoped<ILabDentistProfileService, LabDentistProfileService>();
builder.Services.AddScoped<ILabInvoiceSyncService, LabInvoiceSyncService>();
builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileResourceRepository, FileResourceRepository>();
builder.Services.AddScoped<IScanVisitService, ScanVisitService>();
builder.Services.AddScoped<IScanVisitRepository, ScanVisitRepository>();
builder.Services.AddScoped<ILabScanSlotRepository, LabScanSlotRepository>();
builder.Services.AddScoped<ILabScanSlotService, LabScanSlotService>();
builder.Services.AddScoped<ICaseOrderRepository, CaseOrderRepository>();
builder.Services.AddScoped<ICaseOrderService, CaseOrderService>();
builder.Services.AddScoped<ILabOrderRepository, LabOrderRepository>();
builder.Services.AddScoped<ILabOrderService, LabOrderService>();
builder.Services.AddScoped<ILabOrderQuoteRepository, LabOrderQuoteRepository>();
builder.Services.AddScoped<ILabOrderQuoteService, LabOrderQuoteService>();
builder.Services.AddScoped<ILabOrderStatusRepository, LabOrderStatusRepository>();
builder.Services.AddScoped<ILabOrderStatusService, LabOrderStatusService>();
builder.Services.AddScoped<ILabConnectedDoctorsRepository, LabConnectedDoctorsRepository>();
builder.Services.AddScoped<ILabConnectedDoctorsService, LabConnectedDoctorsService>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<ILabBlogRepository, LabBlogRepository>();
builder.Services.AddScoped<ILabBlogService, LabBlogService>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IAdvertisementRepository, AdvertisementRepository>();
builder.Services.AddScoped<IDentistNotificationService, DentistNotificationService>();
builder.Services.AddScoped<IAdvertisementService, AdvertisementService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IPostService, PostService>();

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<ILabVerificationRepository, LabVerificationRepository>();
builder.Services.AddScoped<ILabVerificationService, LabVerificationService>();
builder.Services.AddScoped<IDentistVerificationRepository, DentistVerificationRepository>();
builder.Services.AddScoped<IDentistVerificationService, DentistVerificationService>();
builder.Services.AddScoped<ILabSubscriptionRepository, LabSubscriptionRepository>();
builder.Services.AddScoped<ILabSubscriptionService, LabSubscriptionService>();
builder.Services.AddScoped<ILabPaymentRepository, LabPaymentRepository>();
builder.Services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();
builder.Services.AddScoped<ISubscriptionReportRepository, SubscriptionReportRepository>();
builder.Services.AddScoped<ISubscriptionReportService, SubscriptionReportService>();
// أضف هذه السطور مع بقية تسجيلات الخدمات في ملف Program.cs
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
// تسجيل طبقة الخدمات (Services)
builder.Services.AddScoped<IFinancialReportService, FinancialReportService>();
builder.Services.AddHttpClient<GatewayPaymentService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddHttpClient<IMyFatoorahService, MyFatoorahService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IFinancialRepository, FinancialRepository>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IProfilePictureService, ProfilePictureService>();
builder.Services.AddScoped<ILabAdvertisementService, LabAdvertisementService>();
builder.Services.AddScoped<ILabAdCreateService, LabAdCreateService>();
builder.Services.AddScoped<ICaseOrderRepository, CaseOrderRepository>();
builder.Services.AddScoped<IAdvertisementRepository, AdvertisementRepository>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IMyFatoorahService, MyFatoorahService>();
builder.Services.AddScoped<IEmailService, EmailService>();
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
        Description = "Enter 'Bearer' [space] and then your valid token.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIs...\""
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

// 7. ????? ????? ?????? ?? ??? JWT ?????? ?????? ?????? ??? ??? Query String ????? ???? SignalR Hub
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // يقبل التوكن من Query String لكل مسارات SignalR
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/notificationHub") ||
                     path.StartsWithSegments("/notifications")))

                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();


    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DentalLab.Api v1");
        c.RoutePrefix = string.Empty;
    });


app.UseHttpsRedirection();

// ?? [??????? ??????? ??? Middleware] ????? ????? ??? CORS ??? React ??? ??? Auth ???? ????? ??? Pre-flight requests
app.UseCors("AllowReactApp");

// ????? ???? ??????? ??????? ???? ????? ??????
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

// ??? ???? ??? Hub ?????? ????????? ??????? ????? ??? ??? ??? Tokens ??????
app.MapHub<NotificationHub>("/notificationHub");

app.MapControllers();

// تشغيل السيدر لإنشاء حساب الأدمن عند بدء التطبيق
await AdminSeeder.SeedAsync(app.Services);

app.Run();
