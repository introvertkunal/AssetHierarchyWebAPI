using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using System.Text.Json;
using System.Xml.Serialization;

namespace AssetHierarchyWebAPI.Services
{
    public class JsonAssetHierarchyService : IAssetHierarchyService
    {
        private List<AssetNode> _rootNodes = new();
        private const string FilePath_json = "asset_hierarchy.json";

        public JsonAssetHierarchyService()
        { 
                LoadFromJson();
                
        }

        public string addNode(string name, string parentName)
        {
            var newNode = new AssetNode
            {
                Name = name,
            };

            if (string.IsNullOrEmpty(parentName))
            {
                _rootNodes.Add(newNode);
                SaveToJSONFIle();
                return "Asset added successfully as a root node.";
            }
            else
            {
                var parent = FindNode(_rootNodes, parentName);
                if (parent != null)
                {
                    parent.Children.Add(newNode);
                    SaveToJSONFIle();
                    return $"Asset '{name}' added successfully under parent '{parentName}'.";
                }
                else
                {
                    return $"Parent node '{parentName}' not found. Asset '{name}' not added.";
                }
            }
           
        }

        public string removeNode(string name)
        {
            if(RemoveRecursive(_rootNodes, name))
            {
                SaveToJSONFIle();
                return $"Asset '{name}' removed successfully.";
            }
            else
            {
                return $"Asset '{name}' not found.";
            }   
        }

        public List<AssetNode> GetHierarchy()
        {
            return _rootNodes;
        }

        private AssetNode FindNode(List<AssetNode> nodes, string name)
        {
            foreach (var node in nodes)
            {
                if (node.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return node;

                var found = FindNode(node.Children, name);
                if (found != null)
                    return found;
            }

            return null;
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
                {
                    return true;
                }
            }

            return false;
        }

        private void SaveToJSONFIle()
        {
            var json = JsonSerializer.Serialize(_rootNodes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath_json, json);
        }

        private void LoadFromJson()
        {
            if (File.Exists(FilePath_json))
            {
                var json = File.ReadAllText(FilePath_json);
                _rootNodes = JsonSerializer.Deserialize<List<AssetNode>>(json) ?? new List<AssetNode>();
            }
        }
    }
}
