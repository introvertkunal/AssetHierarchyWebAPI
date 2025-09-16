using AssetHierarchyWebAPI.API.Hubs;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Application.Mappings;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Infrastructure.Data;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using AssetHierarchyWebAPI.Infrastructure.Services;
using AssetHierarchyWebAPI.Infrastructure.Stores;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("https://localhost:5173", "https://asset-hierarchy-management.vercel.app")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});


// Storage-based services (db/json/xml)
builder.Services.AddAssetHierarchyServices(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();

// Common services (always needed regardless of storage format)
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

// Identity-related services
builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();

// Logging with Serilog
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
          .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day);
});

// Controllers, SignalR, AutoMapper
builder.Services.AddControllers()
    .AddXmlSerializerFormatters();

builder.Services.AddSignalR();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

var app = builder.Build();

// Middleware & Routing
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AssetHierarchyWebAPI.Middlewares.MissingNameLoggingMiddleware>();

// 🔹 Endpoints
app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// 🔹 Identity Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    await IdentitySeeder.SeedAsync(roleManager, userManager);
}

app.Run();
