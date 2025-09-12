using AssetHierarchyWebAPI.Data;
using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Hubs;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("https://localhost:5173") 
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddAssetHierarchyService(builder.Configuration);

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();


builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
        .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers()
    .AddXmlSerializerFormatters();

builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AssetHierarchyWebAPI.Middlewares.MissingNameLoggingMiddleware>();


app.MapControllers();

app.MapHub<NotificationHub>("/notificationHub");


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AssetHierarchyWebAPI.Models.AppUser>>();

    await IdentitySeeder.SeedAsync(roleManager, userManager);
}


app.Run();
