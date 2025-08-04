using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using System.Xml.Serialization;

namespace AssetHierarchyWebAPI.Services
{
    public class XmlAssetHierarchyService : IAssetHierarchyService
    {
        private List<AssetNode> _rootNodes = new();
        private const string FilePath_xml = "asset_hierarchy.xml";
        public XmlAssetHierarchyService()
        { 
            LoadFromXml();
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
                SaveToXmlFile();
                return "Asset added successfully as a root node.";

            }
            else
            {
                var parent = FindNode(_rootNodes, parentName);
                if (parent != null)
                {
                    parent.Children.Add(newNode);
                    SaveToXmlFile();
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
            if (RemoveRecursive(_rootNodes, name))
            {
                SaveToXmlFile();
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
                var foundChild = FindNode(node.Children, name);
                if (foundChild != null)
                    return foundChild;
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
        private void LoadFromXml()
        {
            if (File.Exists(FilePath_xml))
            {
                var serializer = new XmlSerializer(typeof(List<AssetNode>));
                using (var reader = new StreamReader(FilePath_xml))
                {
                    _rootNodes = serializer.Deserialize(reader) as List<AssetNode> ?? new List<AssetNode>();
                }

            }
        }
        private void SaveToXmlFile()
        {
            var serializer = new XmlSerializer(typeof(List<AssetNode>));
            using (var writer = new StreamWriter(FilePath_xml))
            {
                serializer.Serialize(writer, _rootNodes);
            }
        }
    }
    
}
