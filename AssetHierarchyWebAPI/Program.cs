

using AssetHierarchyWebAPI.Extensions;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "https://asset-hierarchy-frontend.vercel.app")
                .AllowAnyHeader()
                .AllowAnyMethod();
        
        });

});

builder.Services.AddAssetHierarchyService(builder.Configuration);

//string format = builder.Configuration["StorageFormat"] ?? "json";

//if (format == "xml")
//    builder.Services.AddSingleton<IAssetHierarchyService, XmlAssetHierarchyService>();
//else
//    builder.Services.AddSingleton<IAssetHierarchyService, JsonAssetHierarchyService>();

builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); 

var app = builder.Build();
app.UseCors("AllowReactApp");
//app.UseMiddleware<AssetHierarchyWebAPI.RateLimitingMiddleware>();
app.UseMiddleware<AssetHierarchyWebAPI.Middlewares.MissingNameLoggingMiddleware>();
app.MapControllers();
app.Run();




