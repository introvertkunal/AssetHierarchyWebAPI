using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IAssetNodeRepository _nodeRepository;
        private readonly string _filePath;

        public FileService(IAssetNodeRepository nodeRepository, IConfiguration configuration)
        {
            _nodeRepository = nodeRepository;
            _filePath = configuration["AssetHierarchy:JsonFilePath"] ?? "asset_hierarchy.json";
        }

        public async Task UpdateJsonFileAsync()
        {
            var allNodes = await _nodeRepository.GetAllNodesAsync(true, true);
            var hierarchy = BuildHierarchy(allNodes, null);
            var json = JsonConvert.SerializeObject(hierarchy, Formatting.Indented);
            await File.WriteAllTextAsync(_filePath, json);
        }

        private List<AssetNode> BuildHierarchy(List<AssetNode> allNodes, int? parentId)
        {
            return allNodes
                .Where(n => n.ParentId == parentId)
                .Select(n => new AssetNode
                {
                    Id = n.Id,
                    Name = n.Name,
                    ParentId = n.ParentId,
                    Children = BuildHierarchy(allNodes, n.Id),
                    Signals = n.Signals
                })
                .ToList();
        }

        public async Task<T> DeserializeJsonAsync<T>(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            var json = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public async Task BackupJsonFileAsync(string filePath, int keepLast)
        {
            string directory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            if (File.Exists(filePath))
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
                File.Copy(filePath, backupFilePath);

                var backups = Directory.GetFiles(directory, $"{fileNameWithoutExt}_*{extension}")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                foreach (var oldFile in backups.Skip(keepLast))
                {
                    try { File.Delete(oldFile); }
                    catch (Exception ex) { Console.WriteLine($"Error deleting file: {ex.Message}"); }
                }
            }
        }
    }
}