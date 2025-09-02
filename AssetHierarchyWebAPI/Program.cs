using AssetHierarchyWebAPI.Data;
using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddAssetHierarchyService(builder.Configuration);

builder.Services.AddIdentityServices(builder.Configuration);

builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
        .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers()
    .AddXmlSerializerFormatters();

var app = builder.Build();

app.UseCors("AllowAll");

app.UseMiddleware<AssetHierarchyWebAPI.Middlewares.MissingNameLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AssetHierarchyWebAPI.Models.AppUser>>();

    await IdentitySeeder.SeedAsync(roleManager, userManager);
}

app.MapControllers();

app.Run();
