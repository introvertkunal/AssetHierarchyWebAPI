

using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAssetHierarchyService(builder.Configuration);

//string format = builder.Configuration["StorageFormat"] ?? "json";

//if (format == "xml")
//    builder.Services.AddSingleton<IAssetHierarchyService, XmlAssetHierarchyService>();
//else
//    builder.Services.AddSingleton<IAssetHierarchyService, JsonAssetHierarchyService>();

builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); 

var app = builder.Build();
app.UseMiddleware<AssetHierarchyWebAPI.RateLimitingMiddleware>();
app.MapControllers();
app.Run();




