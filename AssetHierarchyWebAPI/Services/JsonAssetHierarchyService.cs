using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AssetHierarchyWebAPI.Services
{
    public class JsonAssetHierarchyService : IAssetHierarchyService
    {
        private readonly List<AssetNode> _rootNodes = new();
        private readonly ConcurrentDictionary<int, AssetNode> _nodeMap = new();
        private const string FilePath_json = "asset_hierarchy.json";
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private bool _isDirty = false;
        private int _idCounter = 1; 

        public JsonAssetHierarchyService()
        {
            LoadFromJsonAsync().GetAwaiter().GetResult();
        }

        public AssetSearchResult SearchNode(string name)
        {
            var node = _nodeMap.Values.FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (node == null) return null;

            var parentName = node.ParentId.HasValue && _nodeMap.TryGetValue(node.ParentId.Value, out var parent)
                ? parent.Name
                : null;

            return new AssetSearchResult
            {
                Id = node.Id,
                NodeName = node.Name,
                ParentName = parentName,
                Children = node.Children.Select(c => c.Name).ToList()
            };
        }

        public string AddNode(string name, int? parentId)
        {
            if (_nodeMap.Values.Any(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return $"Asset '{name}' already exists.";

            var newNode = new AssetNode
            {
                Id = _idCounter++,
                Name = name,
                ParentId = parentId
            };

            if (parentId == null)
            {
                _rootNodes.Add(newNode);
            }
            else if (_nodeMap.TryGetValue(parentId.Value, out var parent))
            {
                parent.Children.Add(newNode);
            }
            else
            {
                return $"Parent node with Id {parentId} not found. Asset '{name}' not added.";
            }

            _nodeMap[newNode.Id] = newNode;
            _isDirty = true;
            SaveChangesAsync().GetAwaiter().GetResult();
            return $"Asset '{name}' added successfully.";
        }

        public string RemoveNode(int id)
        {
            if (!_nodeMap.ContainsKey(id))
                return $"Asset with Id {id} not found.";

            if (RemoveRecursive(_rootNodes, id))
            {
                _nodeMap.TryRemove(id, out _);
                _isDirty = true;
                SaveChangesAsync().GetAwaiter().GetResult();
                return $"Asset with Id {id} removed successfully.";
            }
            return $"Asset with Id {id} not found.";
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

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(stream);
            }

            await LoadFromJsonAsync();
        }

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

            if (_nodeMap.Count > 0)
                _idCounter = _nodeMap.Keys.Max() + 1;
        }

        private void AddToMapRecursive(AssetNode node)
        {
            _nodeMap[node.Id] = node;
            foreach (var child in node.Children)
                AddToMapRecursive(child);
        }

        private bool RemoveRecursive(List<AssetNode> nodes, int id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id == id)
                {
                    nodes.RemoveAt(i);
                    return true;
                }
                if (RemoveRecursive(nodes[i].Children, id))
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

        // ----------- New Methods --------------

        public string UpdateNode(int id, string newName)
        {
            if (!_nodeMap.TryGetValue(id, out var node))
                return $"Asset with Id {id} not found.";

            node.Name = newName;
            _isDirty = true;
            SaveChangesAsync().GetAwaiter().GetResult();
            return $"Asset Id {id} renamed to '{newName}'.";
        }

        public string ReorderNode(int id, int? newParentId)
        {
            if (!_nodeMap.TryGetValue(id, out var node))
                return $"Asset with Id {id} not found.";

            // Remove from old parent or root
            if (node.ParentId == null)
                _rootNodes.Remove(node);
            else if (_nodeMap.TryGetValue(node.ParentId.Value, out var oldParent))
                oldParent.Children.Remove(node);

            // Add to new parent or root
            if (newParentId == null)
            {
                _rootNodes.Add(node);
                node.ParentId = null;
            }
            else if (_nodeMap.TryGetValue(newParentId.Value, out var newParent))
            {
                newParent.Children.Add(node);
                node.ParentId = newParentId;
            }
            else
            {
                return $"New parent with Id {newParentId} not found.";
            }

            _isDirty = true;
            SaveChangesAsync().GetAwaiter().GetResult();
            return $"Asset Id {id} moved successfully.";
        }
    }
}
