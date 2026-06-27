using System.Text;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Fingerprints;
using gym_management_server.Fingerprints.Providers;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.Fingerprints;
using gym_management_server.Repositories.Expenses;
using gym_management_server.Repositories.InvoiceItems;
using gym_management_server.Repositories.Invoices;
using gym_management_server.Repositories.MemberDataServices;
using gym_management_server.Repositories.Members;
using gym_management_server.Repositories.OtpTokens;
using gym_management_server.Repositories.ServicePackages;
using gym_management_server.Repositories.Trainers;
using gym_management_server.Repositories.Users;
using gym_management_server.Services;
using gym_management_server.Services.Auth;
using gym_management_server.Services.CheckIns;
using gym_management_server.Services.Devices;
using gym_management_server.Services.Email;
using gym_management_server.Services.Fingerprints;
using gym_management_server.Services.Expenses;
using gym_management_server.Services.InvoiceItems;
using gym_management_server.Services.Invoices;
using gym_management_server.Services.MemberDataServices;
using gym_management_server.Services.Members;
using gym_management_server.Services.ServicePackages;
using gym_management_server.Services.Trainers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<GymManagementServiceMapObjects>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();
builder.Services.AddScoped<CheckInService>();
builder.Services.AddScoped<IMemberDataServiceRepository, MemberDataServiceRepository>();
builder.Services.AddScoped<MemberDataServiceService>();
builder.Services.AddScoped<IServicePackageRepository, ServicePackageRepository>();
builder.Services.AddScoped<ServicePackageService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<IInvoiceItemRepository, InvoiceItemRepository>();
builder.Services.AddScoped<InvoiceItemService>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<ITrainerRepository, TrainerRepository>();
builder.Services.AddScoped<TrainerService>();

// Auth services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOtpTokenRepository, OtpTokenRepository>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();

// Fingerprint hardware abstraction (vendor-neutral).
// Register each vendor provider against IFingerprintProvider; the factory indexes them by Vendor.
builder.Services.AddSingleton<IFingerprintProvider, MockFingerprintProvider>();
builder.Services.AddSingleton<IFingerprintProvider, ZkTecoFingerprintProvider>();
builder.Services.AddSingleton<IFingerprintProvider, SupremaFingerprintProvider>();
builder.Services.AddSingleton<IFingerprintProvider, DigitalPersonaFingerprintProvider>();
builder.Services.AddSingleton<IFingerprintProviderFactory, FingerprintProviderFactory>();
builder.Services.AddSingleton<ITemplateProtector, AesTemplateProtector>();

// Fingerprint & device data + business services.
builder.Services.AddScoped<IFingerprintTemplateRepository, FingerprintTemplateRepository>();
builder.Services.AddScoped<IAttendanceDeviceRepository, AttendanceDeviceRepository>();
builder.Services.AddScoped<FingerprintService>();
builder.Services.AddScoped<AttendanceDeviceService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gym Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
});

builder.Services.AddDbContext<GymManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit Program class to the test project for WebApplicationFactory.
public partial class Program { }
