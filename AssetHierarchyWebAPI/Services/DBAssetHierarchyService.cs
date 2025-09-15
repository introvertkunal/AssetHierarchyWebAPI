using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Hubs;
using AssetHierarchyWebAPI.Interfaces;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace AssetHierarchyWebAPI.Services
{
    public class DBAssetHierarchyService : IAssetHierarchyService
    {
        private readonly AssetContext _context;
        private const string FilePath_json = "asset_hierarchy.json";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubcontext;
        private readonly INotificationStore _notificationStore;
        public DBAssetHierarchyService(
            AssetContext context, 
            IHttpContextAccessor httpContextAccessor, 
            IHubContext<NotificationHub> hubcontext,
            INotificationStore notificationStore
            )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _hubcontext = hubcontext;
            _notificationStore = notificationStore;
        }

        private async Task LogAuditAsync(string operation, int? entityId, string? entityName)
        {
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Unknown";

            var log = new AuditLog
            {
                UserName = userName,
                Operation = operation,
                EntityId = entityId,
                EntityName = entityName,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private bool IsValidName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9_ ]*$");
        }

        private async Task SendNotificationAsync(string message)
        {
            var id = _notificationStore.AddNotification(message);
            await _hubcontext.Clients.All.SendAsync("ReceiveNotification", id, message);
        }


        // Add Node
        public async Task<string> AddNodeAsync(string name, int? parentId)
        {
            try
            {
                if (!IsValidName(name))
                    return $"Invalid asset name '{name}'.";

                if (await _context.AssetHierarchy.AnyAsync(n => n.Name == name))
                    return $"Asset '{name}' already exists.";

                if (parentId != null && !await _context.AssetHierarchy.AnyAsync(n => n.Id == parentId))
                    return $"Parent with Id {parentId} not found.";

                var newNode = new AssetNode { Name = name, ParentId = parentId };
                await _context.AssetHierarchy.AddAsync(newNode);
                await _context.SaveChangesAsync();

                await UpdateJsonFileAsync();

                string parentName = parentId != null
                    ? await _context.AssetHierarchy.Where(p => p.Id == parentId).Select(p => p.Name).FirstOrDefaultAsync()
                    : "Root";
                await LogAuditAsync($"New Asset '{name}' added under '{parentName}'", newNode.Id, newNode.Name);

                

                await SendNotificationAsync($"New Asset '{name}' added under '{parentName}'");

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

        // Remove Node
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
            await _context.Entry(node).Collection(n => n.Children).LoadAsync();

            foreach (var child in node.Children.ToList())
            {
                await DeleteNodeRecursive(child);
            }

            string parentName = node.ParentId != null
                ? await _context.AssetHierarchy.Where(p => p.Id == node.ParentId).Select(p => p.Name).FirstOrDefaultAsync()
                : "Root";

            _context.AssetHierarchy.Remove(node);
            await LogAuditAsync($"Asset '{node.Name}' removed from '{parentName}'", node.Id, node.Name);

            await SendNotificationAsync($"Asset '{node.Name}' removed from '{parentName}'");
        }

        // Update Node Name
        public async Task<string> UpdateNode(int id, string newName)
        {
            try
            {
                if (!IsValidName(newName))
                    return $"Invalid asset name '{newName}'.";

                var node = await _context.AssetHierarchy.Include(n => n.Parent).FirstOrDefaultAsync(n => n.Id == id);
                if (node == null)
                    return $"Asset with ID {id} does not exist.";

                if (await _context.AssetHierarchy.AnyAsync(n => n.Name == newName && n.Id != id))
                    return $"Asset name '{newName}' already exists.";

                var prevName = node.Name;
                node.Name = newName;

                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();
                await LogAuditAsync($"Update Node to new name {newName}", node.Id, prevName);

                string parentName = node.Parent?.Name ?? "Root";
                await SendNotificationAsync($"Asset '{prevName}' renamed to '{newName}' under '{parentName}'");

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

                var oldParentName = node.ParentId != null
                    ? await _context.AssetHierarchy.Where(p => p.Id == node.ParentId).Select(p => p.Name).FirstOrDefaultAsync()
                    : "Root";

                node.ParentId = newParentId;
                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();
                await LogAuditAsync($"Reorder Node to new ParentId {newParentId}", node.Id, node.Name);

                var newParentName = newParentId != null
                    ? await _context.AssetHierarchy.Where(p => p.Id == newParentId).Select(p => p.Name).FirstOrDefaultAsync()
                    : "Root";

                await SendNotificationAsync($"Asset '{node.Name}' moved from '{oldParentName}' to '{newParentName}'");

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

        // Replace hierarchy from JSON
        public async Task<string> ReplaceJsonFileAsync(IFormFile file)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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

                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    NullValueHandling = NullValueHandling.Ignore
                };

                file.OpenReadStream().Seek(0, SeekOrigin.Begin);
                using var reader = new DuplicateKeyCheckingReader(new StreamReader(file.OpenReadStream()));
                var serializer = JsonSerializer.Create(settings);
                var nodes = serializer.Deserialize<List<AssetNode>>(reader);

                if (nodes == null || nodes.Count == 0)
                    throw new Exception("No nodes found in JSON");

                var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                void ValidateUniqueNames(IEnumerable<AssetNode> nodes)
                {
                    foreach (var node in nodes)
                    {
                        if (!allNames.Add(node.Name))
                            throw new Exception($"Duplicate asset name '{node.Name}' found in JSON.");

                        if (node.Children != null && node.Children.Any())
                            ValidateUniqueNames(node.Children);
                    }
                }

                ValidateUniqueNames(nodes);

                _context.AssetSignal.RemoveRange(_context.AssetSignal);
                _context.AssetHierarchy.RemoveRange(_context.AssetHierarchy);

                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetHierarchy', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetSignal', RESEED, 0)");
                await _context.SaveChangesAsync();

                foreach (var node in nodes.Where(n => n.ParentId == null))
                {
                    await InsertNodeRecursive(node, null);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await LogAuditAsync("JSON File is Uploaded", null, null);

                return "JSON File Uploaded Successfully";
            }
            catch (JsonReaderException)
            {
                await transaction.RollbackAsync();
                return "JSON File Contains Duplicate Keys";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return "JSON File is not in Correct Format";
            }
        }

        private async Task InsertNodeRecursive(AssetNode node, int? newParentId)
        {
            if (!IsValidName(node.Name))
                throw new Exception($"Invalid asset name '{node.Name}'.");

            var newNode = new AssetNode
            {
                Name = node.Name,
                ParentId = newParentId
            };

            await _context.AssetHierarchy.AddAsync(newNode);
            await _context.SaveChangesAsync();

            if (node.Signals != null && node.Signals.Any())
            {
                foreach (var signal in node.Signals)
                {
                    if (!IsValidName(signal.SignalName))
                        throw new Exception($"Invalid signal name '{signal.SignalName}'.");

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

        // Search Node
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

        public class DuplicateKeyCheckingReader : JsonTextReader
        {
            private readonly Stack<HashSet<string>> _keys = new Stack<HashSet<string>>();

            public DuplicateKeyCheckingReader(TextReader reader) : base(reader) { }

            public override bool Read()
            {
                var result = base.Read();

                if (TokenType == JsonToken.StartObject)
                {
                    _keys.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
                else if (TokenType == JsonToken.PropertyName)
                {
                    var currentKey = Value?.ToString();
                    if (_keys.Count > 0)
                    {
                        var currentObjectKeys = _keys.Peek();
                        if (!currentObjectKeys.Add(currentKey))
                        {
                            throw new JsonReaderException($"Duplicate key detected: '{currentKey}'");
                        }
                    }
                }
                else if (TokenType == JsonToken.EndObject)
                {
                    _keys.Pop();
                }

                return result;
            }
        }
    }
}
