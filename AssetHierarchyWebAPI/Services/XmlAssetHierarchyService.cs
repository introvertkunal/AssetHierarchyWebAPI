using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using System.Collections.Concurrent;
using System.Xml.Serialization;

namespace AssetHierarchyWebAPI.Services
{
    public class XmlAssetHierarchyService : IAssetHierarchyService
    {
        private List<AssetNode> _rootNodes = new();
        private readonly ConcurrentDictionary<int, AssetNode> _nodeMap = new();
        private const string FilePath_xml = "asset_hierarchy.xml";
        private int _idCounter = 1;

        public XmlAssetHierarchyService()
        {
            LoadFromXmlAsync().GetAwaiter().GetResult();
        }

        // Search Asset
        public async Task<AssetSearchResult> SearchNode(string name)
        {
            var node = _nodeMap.Values.FirstOrDefault(n =>
                n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (node == null) return null;

            var parentName = node.ParentId.HasValue &&
                             _nodeMap.TryGetValue(node.ParentId.Value, out var parent)
                ? parent.Name
                : null;

            return new AssetSearchResult
            {
                Id = node.Id,
                NodeName = node.Name,
                ParentName = parentName,
                Children = node.Children.Select(c => c.Name).ToList(),
                Signals = node.Signals.Select(s => new SignalResult
                {
                    SignalId = s.SignalId,
                    SignalName = s.SignalName,
                    SignalType = s.SignalType,
                    Description = s.Description
                }).ToList()
            };
        }

        // Add Asset
        public async Task<string> AddNodeAsync(string name, int? parentId)
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
            await SaveToXmlFileAsync();
            return $"Asset '{name}' added successfully.";
        }

        // Remove Asset
        public async Task<string> RemoveNodeAsync(int id)
        {
            if (!_nodeMap.ContainsKey(id))
                return $"Asset with Id {id} not found.";

            if (RemoveRecursive(_rootNodes, id))
            {
                _nodeMap.TryRemove(id, out _);
                await SaveToXmlFileAsync();
                return $"Asset with Id {id} removed successfully.";
            }

            return $"Asset with Id {id} not found.";
        }

        // Get Hierarchy
        public async Task<List<AssetNode>> GetHierarchyAsync() => _rootNodes;

        // Update Node
        public async Task<string> UpdateNode(int id, string newName)
        {
            if (!_nodeMap.TryGetValue(id, out var node))
                return $"Asset with Id {id} not found.";

            if (_nodeMap.Values.Any(n => n.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && n.Id != id))
                return $"Asset name '{newName}' already exists.";

            node.Name = newName;
            await SaveToXmlFileAsync();
            return $"Asset Id {id} renamed to '{newName}'.";
        }

        // Reorder Node
        public async Task<string> ReorderNode(int id, int? newParentId)
        {
            if (!_nodeMap.TryGetValue(id, out var node))
                return $"Asset with Id {id} not found.";
           
            if (newParentId.HasValue && IsDescendant(node, newParentId.Value))
                return "Invalid move: cannot assign descendant as parent.";

            if (node.ParentId == null)
                _rootNodes.Remove(node);
            else if (_nodeMap.TryGetValue(node.ParentId.Value, out var oldParent))
                oldParent.Children.Remove(node);

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

            await SaveToXmlFileAsync();
            return $"Asset Id {id} moved successfully.";
        }

        // Replace JSON file with uploaded one
        public async Task<string> ReplaceJsonFileAsync(IFormFile file)
        {
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var content = await reader.ReadToEndAsync();

                var deserializeData = System.Text.Json.JsonSerializer.Deserialize<List<AssetNode>>(content);
                if (deserializeData != null)
                {
                    _rootNodes = deserializeData;
                    BuildNodeMap();
                    await SaveToXmlFileAsync();
                }
                else
                {
                    throw new ArgumentException("File is empty or not provided.");
                }
                return "JSON File is uploaded Successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error replacing XML file: {ex.Message}");
                return "JSON File is not in Correct Format";
            }
        }

        // ---------------- Helpers ----------------

        private async Task LoadFromXmlAsync()
        {
            try
            {
                if (File.Exists(FilePath_xml))
                {
                    using var stream = new FileStream(FilePath_xml, FileMode.Open, FileAccess.Read);
                    var serializer = new XmlSerializer(typeof(List<AssetNode>));
                    _rootNodes = (serializer.Deserialize(stream) as List<AssetNode>) ?? new List<AssetNode>();
                    BuildNodeMap();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading from XML file: {ex.Message}");
                _rootNodes = new List<AssetNode>();
            }
        }

        private async Task SaveToXmlFileAsync()
        {
            try
            {
                using var stream = new FileStream(FilePath_xml, FileMode.Create, FileAccess.Write, FileShare.None);
                var serializer = new XmlSerializer(typeof(List<AssetNode>));
                serializer.Serialize(stream, _rootNodes);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to XML file: {ex.Message}");
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

        private bool RemoveRecursive(ICollection<AssetNode> nodes, int id)
        {
            var nodeList = nodes.ToList();

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (nodeList[i].Id == id)
                {
                    nodes.Remove(nodeList[i]);
                    return true;
                }
                if (RemoveRecursive(nodeList[i].Children, id))
                    return true;
            }
            return false;
        }

        private bool IsDescendant(AssetNode node, int potentialParentId)
        {
            if (!_nodeMap.TryGetValue(potentialParentId, out var parent)) return false;

            while (parent != null)
            {
                if (parent.ParentId == node.Id)
                    return true;

                parent = parent.ParentId.HasValue &&
                         _nodeMap.TryGetValue(parent.ParentId.Value, out var grandParent)
                         ? grandParent : null;
            }
            return false;
        }
    }
}
