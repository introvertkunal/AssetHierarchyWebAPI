//    private static void PrintHierarchy(List<AssetNode> nodes, int level)
//    {
//        foreach (var node in nodes)
//        {
//            Console.WriteLine($"{new string(' ', level * 2)}- {node.Name}");
//            PrintHierarchy(node.Children, level + 1);
//        }
//    }
//}

using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

string format = builder.Configuration["StorageFormat"] ?? "json";

if (format == "xml")
    builder.Services.AddSingleton<IAssetHierarchyService, XmlAssetHierarchyService>();
else
    builder.Services.AddSingleton<IAssetHierarchyService, JsonAssetHierarchyService>();

builder.Services.AddControllers()
    .AddXmlSerializerFormatters(); 

var app = builder.Build();
app.MapControllers();
app.Run();




