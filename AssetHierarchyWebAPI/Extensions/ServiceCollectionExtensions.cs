using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Controllers;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAssetHierarchyService(this IServiceCollection services, IConfiguration configuration)
        {
            string format = configuration["storageFormat"] ?? "json";

            if(format == "xml")
            {
                services.AddScoped<Interfaces.IAssetHierarchyService, Services.XmlAssetHierarchyService>();
            }
            else if(format == "json")
            {
                services.AddScoped<Interfaces.IAssetHierarchyService, Services.JsonAssetHierarchyService>();      
            }
            else if(format == "db")
            {
                services.AddScoped<IAssetHierarchyService, DBAssetHierarchyService >();
                services.AddDbContext<AssetContext>(options => options.UseSqlServer(configuration.GetConnectionString("AssetConnStr")));
            }

            return services;
        }
    }
}
