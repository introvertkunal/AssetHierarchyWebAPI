

using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args );

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

builder.Host.UseSerilog((context,config) =>
{
    config.WriteTo.Console()
        .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); 

var app = builder.Build();
app.UseCors("AllowAll");
//app.UseMiddleware<AssetHierarchyWebAPI.RateLimitingMiddleware>();
app.UseMiddleware<AssetHierarchyWebAPI.Middlewares.MissingNameLoggingMiddleware>();
app.MapControllers();
app.Run();




