namespace AssetHierarchyWebAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAssetHierarchyService(this IServiceCollection services, IConfiguration configuration)
        {
            string format = configuration["storageFormat"] ?? "json";

            if(format == "xml")
            {
                services.AddSingleton<Interfaces.IAssetHierarchyService, Services.XmlAssetHierarchyService>();
            }
            else
            {
                services.AddSingleton<Interfaces.IAssetHierarchyService, Services.JsonAssetHierarchyService>();      
            }

            return services;
        }
    }
}
