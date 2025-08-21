using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AssetHierarchyWebAPI.Services
{
    public class JsonAssetHierarchyService : IAssetHierarchyService
    {
        private readonly List<AssetNode> _rootNodes = new();
        private readonly ConcurrentDictionary<string, AssetNode> _nodeMap = new(StringComparer.OrdinalIgnoreCase);
        private const string FilePath_json = "asset_hierarchy.json";
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private bool _isDirty = false; 

        public JsonAssetHierarchyService()
        {
            LoadFromJsonAsync().GetAwaiter().GetResult();
        }

        
        public bool searchNode(string name) => _nodeMap.ContainsKey(name);

        public string addNode(string name, string parentName)
        {
            if (_nodeMap.ContainsKey(name))
                return $"Asset '{name}' already exists.";

            var newNode = new AssetNode { Name = name };

            if (string.IsNullOrWhiteSpace(parentName))
            {
                _rootNodes.Add(newNode);
            }
            else if (_nodeMap.TryGetValue(parentName, out var parent))
            {
                parent.Children.Add(newNode);
            }
            else
            {
                return $"Parent node '{parentName}' not found. Asset '{name}' not added.";
            }

            _nodeMap[name] = newNode;
            _isDirty = true;
            return $"Asset '{name}' added successfully.";
        }

        public string removeNode(string name)
        {
            if (!_nodeMap.ContainsKey(name))
                return $"Asset '{name}' not found.";

            if (RemoveRecursive(_rootNodes, name))
            {
                _nodeMap.TryRemove(name, out _);
                _isDirty = true;
                return $"Asset '{name}' removed successfully.";
            }
            return $"Asset '{name}' not found.";
        }

        public List<AssetNode> GetHierarchy() => _rootNodes;

       
        public async Task ReplaceJsonFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or not provided.");

            string fullPath = Path.GetFullPath(FilePath_json);
            string directory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);

            
            if (File.Exists(fullPath))
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
                File.Copy(fullPath, backupFilePath);

                CleanupOldBackups(directory, fileNameWithoutExt, extension, keepLast: 5);
            }

            // Replace main file
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(stream);
            }

            await LoadFromJsonAsync();
        }

        // Save only when needed
        public async Task SaveChangesAsync()
        {
            if (!_isDirty) return; 
            try
            {
                var json = JsonSerializer.Serialize(_rootNodes, _jsonOptions);
                await File.WriteAllTextAsync(FilePath_json, json);
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to JSON file: {ex.Message}");
            }
        }

        private async Task LoadFromJsonAsync()
        {
            try
            {
                if (File.Exists(FilePath_json))
                {
                    var json = await File.ReadAllTextAsync(FilePath_json);
                    var nodes = JsonSerializer.Deserialize<List<AssetNode>>(json) ?? new List<AssetNode>();
                    
                   
                    _rootNodes.Clear();
                    _rootNodes.AddRange(nodes);
                    BuildNodeMap();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Loading from JSON file: {ex.Message}");
                
               
                
            }
        }

      

        private void BuildNodeMap()
        {
            _nodeMap.Clear();
            foreach (var root in _rootNodes)
                AddToMapRecursive(root);
        }

        private void AddToMapRecursive(AssetNode node)
        {
            _nodeMap[node.Name] = node;
            foreach (var child in node.Children)
                AddToMapRecursive(child);
        }

        private bool RemoveRecursive(List<AssetNode> nodes, string name)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    nodes.RemoveAt(i);
                    return true;
                }
                if (RemoveRecursive(nodes[i].Children, name))
                    return true;
            }
            return false;
        }

        private void CleanupOldBackups(string directory, string baseName, string extension, int keepLast)
        {
            var backups = Directory.GetFiles(directory, $"{baseName}_*{extension}")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            foreach (var oldFile in backups.Skip(keepLast))
            {
                try { File.Delete(oldFile); } catch { /* ignore */ }
            }
        }
    }
}
