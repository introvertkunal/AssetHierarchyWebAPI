using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Application.Services;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using AssetHierarchyWebAPI.Infrastructure.Repositories;
using AssetHierarchyWebAPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetHierarchyWebAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAssetHierarchyServices(this IServiceCollection services, IConfiguration configuration)
        {
            string format = configuration["storageFormat"] ?? "db";

            if (format == "db")
            {
                // Register DbContext
                services.AddDbContext<AssetContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("AssetConnStr")));

                // Repositories
                services.AddScoped<IAssetNodeRepository, AssetNodeRepository>();
                services.AddScoped<IAssetSignalRepository, AssetSignalRepository>();

                // Services
                services.AddScoped<IAssetHierarchyService, AssetHierarchyService>();
                services.AddScoped<IAssetSignalService, AssetSignalService>();
            }
            //else if (format == "json")
            //{
            //    // Future JSON services
            //    services.AddScoped<IAssetHierarchyService, JsonAssetHierarchyService>();
            //    services.AddScoped<IAssetSignalService, JsonAssetSignalService>();
            //}
            //else if (format == "xml")
            //{
            //    // Future XML services
            //    services.AddScoped<IAssetHierarchyService, XmlAssetHierarchyService>();
            //    services.AddScoped<IAssetSignalService, XmlAssetSignalService>();
            //}

            return services;
        }
    }
}
