using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetHierarchyWebAPI.Services
{
    public class DBAssetHierarchyService : IAssetHierarchyService
    {
        private readonly AssetContext _context;

        private const string FilePath_json = "asset_hierarchy.json";
        public DBAssetHierarchyService(AssetContext Context)
        {
            _context = Context;
        }

        // Method for Add Asset
        public string AddNode(string name, int? parentId)
        {
            if(_context.AssetHierarchy.Any(n => n.Name == name))
                return $"Asset '{name}' already exists.";

            if(parentId != null && !_context.AssetHierarchy.Any(n=> n.Id == parentId))
                return $"Parent with Id {parentId} not found.";

            var newNode = new AssetNode
            {
                Name = name,
                ParentId = parentId
            };

            _context.AssetHierarchy.Add(newNode);
            _context.SaveChanges();
            return $"Asset {name} is added Successfully";
        }

        // This Return Asset Hierarchy
        public List<AssetNode> GetHierarchy()
        {
            var allNodes = _context.AssetHierarchy.ToList();

            return BuildHierarchy(allNodes, null);
        }

        // Used to Return Asset with depth Hierarchy
        private List<AssetNode> BuildHierarchy(List<AssetNode> allNodes, int? parentId)
        {
            return allNodes
                .Where(n => n.ParentId == parentId)
                .Select(n => new AssetNode
                {
                    Id = n.Id,
                    Name = n.Name,
                    ParentId = n.ParentId,
                    Children = BuildHierarchy(allNodes, n.Id)
                })
                .ToList();
        }

        // Method for Remove Asset
        public string RemoveNode(int id)
        {
            var node = _context.AssetHierarchy
                         .Include(n => n.Children)
                         .FirstOrDefault(n => n.Id == id);

            if (node == null)
                return "Asset is not Exist";

            DeleteChildren(node);

            _context.AssetHierarchy.Remove(node);
            _context.SaveChanges();

            return $"Asset {node.Name} Removed Successfully";
        }

        // If Asset have Children then Delete Children First
        private void DeleteChildren(AssetNode node)
        {
            if(node.Children != null && node.Children.Any())
            {
                foreach(var child in node.Children.ToList())
                {
                    DeleteChildren(child);
                    _context.AssetHierarchy.Remove(child);
                    _context.SaveChanges();
                }
            }
        }

        // Method for Rename Asset Name
        public string UpdateNode(int id, string newName)
        {
            var node = _context.AssetHierarchy.FirstOrDefault(n => n.Id == id);

            if (node == null)
                return $"Asset is not Exist";

            var prevName = node.Name;
            node.Name = newName;

            _context.SaveChanges();

            return $"{prevName} is renamed to {node.Name} ";

        }

        // Method that Reorder Hierarchy
        public string ReorderNode(int id, int? newParentId)
        {
           
            var node = _context.AssetHierarchy.FirstOrDefault(n => n.Id == id);
            if (node == null)
                return "Asset does not exist";

     
            if (newParentId != null)
            {
                if (!_context.AssetHierarchy.Any(n => n.Id == newParentId))
                    return "New parent does not exist";

         
                if (id == newParentId)
                    return "A node cannot be its own parent";

                
                if (IsDescendant(id, newParentId.Value))
                    return "Invalid move: cannot assign descendant as parent";
            }

            node.ParentId = newParentId;
            _context.SaveChanges();

            return "Node reordered successfully";
        }

        // Prevent cycles (child cannot become parent of its ancestor)
        private bool IsDescendant(int nodeId, int newParentId)
        {
            var parent = _context.AssetHierarchy.FirstOrDefault(n => n.Id == newParentId);
            while (parent != null)
            {
                if (parent.ParentId == nodeId)
                    return true;

                parent = _context.AssetHierarchy.FirstOrDefault(n => n.Id == parent.ParentId);
            }
            return false;
        }


        // Method for Version and Load Data from JSON File
        public async Task ReplaceJsonFileAsync(IFormFile file)
        {
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

            using (var stream1 = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(stream1);
            }

            using var stream = new StreamReader(file.OpenReadStream());
            var json = await stream.ReadToEndAsync();
            var nodes = JsonSerializer.Deserialize<List<AssetNode>>(json) ?? new List<AssetNode>();

            // Clear existing
            _context.AssetHierarchy.RemoveRange(_context.AssetHierarchy);
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetHierarchy', RESEED, 0)");

            await _context.SaveChangesAsync();

            // Dictionary to map old IDs from JSON → new IDs from DB
            var idMap = new Dictionary<int, int>();

            
            foreach (var node in nodes.Where(n => n.ParentId == null))
            {
                await InsertNodeRecursive(node, null, idMap);
            }

            await _context.SaveChangesAsync();
        }

        private async Task InsertNodeRecursive(AssetNode node, int? newParentId, Dictionary<int, int> idMap)
        {
            
            var newNode = new AssetNode
            {
                Name = node.Name,
                ParentId = newParentId
            };

            _context.AssetHierarchy.Add(newNode);
            await _context.SaveChangesAsync();

            
            idMap[node.Id] = newNode.Id;

         
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    await InsertNodeRecursive(child, newNode.Id, idMap);
                }
            }
        }

        // Method for Search Asset
        public AssetSearchResult SearchNode(string name)
        {
            var node = _context.AssetHierarchy
                             .Include(n => n.Children)
                             .FirstOrDefault(n => n.Name.ToLower() == name.ToLower());

            if (node == null)
                return null;

            var parentName = node.ParentId != null ? _context.AssetHierarchy
                                                     .Where(n=> n.Id == node.ParentId)
                                                     .Select(n => n.Name).FirstOrDefault() : null;

            return new AssetSearchResult
            {
                Id = node.Id,
                NodeName = node.Name,
                ParentName = parentName,
                Children = node.Children.Select(c => c.Name).ToList()
            };
        }

        private void CleanupOldBackups(string directory, string baseName, string extension, int keepLast)
        {
            var backups = Directory.GetFiles(directory, $"{baseName}_*{extension}")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            foreach (var oldFile in backups.Skip(keepLast))
            {
                try { File.Delete(oldFile); } 
                
                catch(Exception ex)
                {
                    Console.WriteLine($"Error Occur while deleting file: {ex.Message}");
                }
            }
        }

    }
}
