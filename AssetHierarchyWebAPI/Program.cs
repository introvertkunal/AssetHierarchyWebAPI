using AssetHierarchyWebAPI.API.Hubs;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Application.Mappings;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Infrastructure.Data;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using AssetHierarchyWebAPI.Infrastructure.Services;
using AssetHierarchyWebAPI.Infrastructure.Stores;
using AssetHierarchyWebAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AssetHierarchyWebAPI.Infrastructure.RabbitMQConfig;
using Serilog;
using DotNetEnv;
using Microsoft.AspNetCore.SignalR;


var builder = WebApplication.CreateBuilder(args);

Env.Load();


builder.Configuration["ConnectionStrings:AssetConnStr"] = Environment.GetEnvironmentVariable("ASSET_CONN_STR");
builder.Configuration["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_KEY");
builder.Configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER");
builder.Configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

builder.Configuration["Authentication:Google:ClientId"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
builder.Configuration["Authentication:Google:ClientSecret"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

builder.Configuration["Authentication:GitHub:ClientId"] = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");
builder.Configuration["Authentication:GitHub:ClientSecret"] = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");

builder.Configuration["AssetHierarchy:JsonFilePath"] = Environment.GetEnvironmentVariable("ASSET_JSON_PATH");

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
builder.Services.AddScoped<IFileService, FileService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var nodeRepo = sp.GetRequiredService<IAssetNodeRepository>();
    var nodeSignalRepo = sp.GetRequiredService<IAssetSignalRepository>();
    var auditLog = sp.GetRequiredService<IAuditLogService>();
    var context = sp.GetRequiredService<AssetContext>();

    
    var jsonPath = config["AssetHierarchy:JsonFilePath"]
                   ?? Path.Combine(AppContext.BaseDirectory, "asset_hierarchy.json");

    return new FileService(nodeRepo, nodeSignalRepo, auditLog, context, jsonPath);
});

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

builder.Services.AddSingleton<RabbitMQSettings>();
builder.Services.AddHostedService<SignalResultConsumerService>();

builder.Services.AddHostedService<BackgroundServiceInsertSignal>();




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



//Identity Seeding
using (var scope = app.Services.CreateScope())
{

    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AssetContext>();

    try
    {
        // Apply migrations and create DB if missing
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
    
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var dbContext = services.GetRequiredService<AssetContext>();

    await IdentitySeeder.SeedAsync(roleManager, userManager,dbContext);
}




app.Run();
