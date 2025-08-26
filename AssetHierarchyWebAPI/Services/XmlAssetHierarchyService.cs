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
            LoadFromXml();
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
            SaveToXmlFile();
            return $"Asset '{name}' added successfully.";
        }

        public string RemoveNode(int id)
        {
            if (!_nodeMap.ContainsKey(id))
                return $"Asset with Id {id} not found.";

            if (RemoveRecursive(_rootNodes, id))
            {
                _nodeMap.TryRemove(id, out _);
                SaveToXmlFile();
                return $"Asset with Id {id} removed successfully.";
            }
            return $"Asset with Id {id} not found.";
        }

        public List<AssetNode> GetHierarchy() => _rootNodes;

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

        private void LoadFromXml()
        {
            try
            {
                if (File.Exists(FilePath_xml))
                {
                    var serializer = new XmlSerializer(typeof(List<AssetNode>));
                    using (var reader = new StreamReader(FilePath_xml))
                    {
                        _rootNodes = serializer.Deserialize(reader) as List<AssetNode> ?? new List<AssetNode>();
                    }
                    BuildNodeMap();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading from XML file: {ex.Message}");
                _rootNodes = new List<AssetNode>();
            }
        }

        private void SaveToXmlFile()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<AssetNode>));
                using (var writer = new StreamWriter(FilePath_xml))
                {
                    serializer.Serialize(writer, _rootNodes);
                }
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

        // ---------------- New Methods ----------------

        public string UpdateNode(int id, string newName)
        {
            if (!_nodeMap.TryGetValue(id, out var node))
                return $"Asset with Id {id} not found.";

            node.Name = newName;
            SaveToXmlFile();
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

            SaveToXmlFile();
            return $"Asset Id {id} moved successfully.";
        }

        public async Task ReplaceJsonFileAsync(IFormFile file)
        {
            try
            {
                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    var deserializeData = System.Text.Json.JsonSerializer.Deserialize<List<AssetNode>>(stream.ReadToEnd());
                    if (deserializeData != null)
                    {
                        _rootNodes = deserializeData;
                        BuildNodeMap();
                        SaveToXmlFile();
                    }
                    else
                    {
                        throw new ArgumentException("File is empty or not provided.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error replacing XML file: {ex.Message}");
                throw new ArgumentException("File is empty or not provided.", ex);
            }
        }
    }
}
