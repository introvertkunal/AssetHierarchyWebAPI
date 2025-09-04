using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace AssetHierarchyWebAPI.Services
{
    public class DBAssetHierarchyService : IAssetHierarchyService
    {
        private readonly AssetContext _context;
        private const string FilePath_json = "asset_hierarchy.json";
        private readonly IAssetHierarchyService _service;

        public DBAssetHierarchyService(AssetContext context, IAssetHierarchyService service)
        {
            _context = context;
            _service = service;
        }

       


        private bool IsValidName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9 ]*$");
        }

        // Add Node
        public async Task<string> AddNodeAsync(string name, int? parentId)
        {
            try
            {
                if (!IsValidName(name))
                    return $"Invalid asset name '{name}'. Name must start with a letter and contain only letters, digits, or spaces.";

                if (await _context.AssetHierarchy.AnyAsync(n => n.Name == name))
                    return $"Asset '{name}' already exists.";

                if (parentId != null && !await _context.AssetHierarchy.AnyAsync(n => n.Id == parentId))
                    return $"Parent with Id {parentId} not found.";

                var newNode = new AssetNode { Name = name, ParentId = parentId };
                await _context.AssetHierarchy.AddAsync(newNode);
                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();

                return $"Asset {name} added successfully.";
            }
            catch (Exception ex)
            {
                return $"Failed to add node '{name}': {ex.Message}";
            }
        }

        // Get full hierarchy
        public async Task<List<AssetNode>> GetHierarchyAsync()
        {
            try
            {
                var allNodes = await _context.AssetHierarchy
                                             .AsNoTracking()
                                             .ToListAsync();

                return BuildHierarchy(allNodes, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching hierarchy: {ex.Message}");
                return new List<AssetNode>();
            }
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
                    Children = BuildHierarchy(allNodes, n.Id)
                })
                .ToList();
        }

        // Remove Node (cascade handles children/signals)
        public async Task<string> RemoveNodeAsync(int id)
        {
            try
            {
                var node = await _context.AssetHierarchy
                    .Include(n => n.Children)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (node == null)
                    return "Asset does not exist";

                await DeleteNodeRecursive(node);

                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();

                return $"Asset {node.Name} and its children removed successfully.";
            }
            catch (Exception ex)
            {
                return $"Failed to remove node with ID {id}: {ex.Message}";
            }
        }

        private async Task DeleteNodeRecursive(AssetNode node)
        {
            // Load children explicitly
            await _context.Entry(node).Collection(n => n.Children).LoadAsync();

            foreach (var child in node.Children.ToList())
            {
                await DeleteNodeRecursive(child);
            }

            _context.AssetHierarchy.Remove(node);
        }


        // Update Node Name
        public async Task<string> UpdateNode(int id, string newName)
        {
            try
            {
                if (!IsValidName(newName))
                    return $"Invalid asset name '{newName}'. Name must start with a letter and contain only letters, digits, or spaces.";

                var node = await _context.AssetHierarchy.FindAsync(id);
                if (node == null)
                    return $"Asset with ID {id} does not exist.";

                if (await _context.AssetHierarchy.AnyAsync(n => n.Name == newName && n.Id != id))
                    return $"Asset name '{newName}' already exists. Choose a different name.";

                var prevName = node.Name;
                node.Name = newName;

                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();
                return $"{prevName} renamed to {node.Name}.";
            }
            catch (Exception ex)
            {
                return $"Failed to update node with ID {id}: {ex.Message}";
            }
        }

        // Reorder Node
        public async Task<string> ReorderNode(int id, int? newParentId)
        {
            try
            {
                var node = await _context.AssetHierarchy.FindAsync(id);
                if (node == null)
                    return "Asset does not exist.";

                if (newParentId != null)
                {
                    if (!await _context.AssetHierarchy.AnyAsync(n => n.Id == newParentId))
                        return "New parent does not exist.";

                    if (id == newParentId)
                        return "A node cannot be its own parent.";

                    if (await IsDescendant(id, newParentId.Value))
                        return "Invalid move: cannot assign descendant as parent.";
                }

                node.ParentId = newParentId;
                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();

                return "Node reordered successfully.";
            }
            catch (Exception ex)
            {
                return $"Failed to reorder node with ID {id}: {ex.Message}";
            }
        }

        private async Task<bool> IsDescendant(int nodeId, int newParentId)
        {
            var parent = await _context.AssetHierarchy.FindAsync(newParentId);
            while (parent != null)
            {
                if (parent.ParentId == nodeId)
                    return true;

                parent = await _context.AssetHierarchy.FindAsync(parent.ParentId);
            }
            return false;
        }

        // Replace hierarchy from JSON (with transaction & rollback)
        public async Task<string> ReplaceJsonFileAsync(IFormFile file)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string fullPath = Path.GetFullPath(FilePath_json);
                string directory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                string extension = Path.GetExtension(fullPath);

                // backup old file
                if (File.Exists(fullPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string backupFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
                    File.Copy(fullPath, backupFilePath);

                    CleanupOldBackups(directory, fileNameWithoutExt, extension, keepLast: 5);
                }

                // save new file physically
                using (var stream1 = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await file.CopyToAsync(stream1);
                }

                // read the uploaded file content
                using var stream = new StreamReader(file.OpenReadStream());
                var json = await stream.ReadToEndAsync();

                // strict deserialization settings
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error, 
                    NullValueHandling = NullValueHandling.Ignore         
                };

                // try deserializing – throws if required field missing or unknown field present
                var nodes = JsonConvert.DeserializeObject<List<AssetNode>>(json, settings);
                if (nodes == null || nodes.Count == 0)
                    throw new Exception("No nodes found in JSON");

                // clear old data
                _context.AssetSignal.RemoveRange(_context.AssetSignal);
                _context.AssetHierarchy.RemoveRange(_context.AssetHierarchy);

                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetHierarchy', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetSignal', RESEED, 0)");
                await _context.SaveChangesAsync();

                // insert new hierarchy
                foreach (var node in nodes.Where(n => n.ParentId == null))
                {
                    await InsertNodeRecursive(node, null);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return "JSON File Uploaded Successfully";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return "JSON File is not in Correct Format";
            }
        }


        private async Task InsertNodeRecursive(AssetNode node, int? newParentId)
        {
            var result = await AddNodeAsync(node.Name, newParentId);

            if (result.StartsWith("Invalid asset name") || result.Contains("failed", StringComparison.OrdinalIgnoreCase))
                throw new Exception(result); 

            var newNode = await _context.AssetHierarchy.FirstOrDefaultAsync(n => n.Name == node.Name && n.ParentId == newParentId);

            if (newNode == null)
                throw new Exception("Failed to insert node during JSON import.");


            if (node.Signals != null && node.Signals.Any())
            {
                foreach (var signal in node.Signals)
                {
                    
                    var newSignal = new AssetSignals
                    {
                        
                        SignalName = signal.SignalName,
                        SignalType = signal.SignalType,
                        Description = signal.Description,
                        AssetNodeId = newNode.Id
                    };
                    await _context.AssetSignal.AddAsync(newSignal);
                }
                await _context.SaveChangesAsync();
            }


            if (node.Children != null && node.Children.Any())
            {
                foreach (var child in node.Children)
                {
                    await InsertNodeRecursive(child, newNode.Id);
                }
            }
        }


        // Search Node + Signals
        public async Task<AssetSearchResult?> SearchNode(string name)
        {
            try
            {
                var node = await _context.AssetHierarchy
                                         .Include(n => n.Children)
                                         .Include(n => n.Signals)
                                         .FirstOrDefaultAsync(n => n.Name.ToLower() == name.ToLower());

                if (node == null) return null;

                var parentName = node.ParentId != null
                    ? await _context.AssetHierarchy.Where(n => n.Id == node.ParentId)
                                                   .Select(n => n.Name)
                                                   .FirstOrDefaultAsync()
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
            catch (Exception ex)
            {
                Console.WriteLine($"Search failed: {ex.Message}");
                return null;
            }
        }

        private void CleanupOldBackups(string directory, string baseName, string extension, int keepLast)
        {
            var backups = Directory.GetFiles(directory, $"{baseName}_*{extension}")
                                   .OrderByDescending(f => File.GetCreationTime(f))
                                   .ToList();

            foreach (var oldFile in backups.Skip(keepLast))
            {
                try { File.Delete(oldFile); }
                catch (Exception ex) { Console.WriteLine($"Error deleting file: {ex.Message}"); }
            }
        }

        // Update JSON file after any change
        private async Task UpdateJsonFileAsync()
        {
            try
            {
                var allNodes = await _context.AssetHierarchy
                                             .Include(n => n.Signals)
                                             .Include(n => n.Children)
                                             .ToListAsync();

                
                var hierarchy = BuildHierarchy(allNodes, null);

                var json = JsonConvert.SerializeObject(hierarchy, Formatting.Indented);

                await File.WriteAllTextAsync(FilePath_json, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating JSON file: {ex.Message}");
            }
        }

    }
}
