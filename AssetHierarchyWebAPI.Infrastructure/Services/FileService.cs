using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly IAssetNodeRepository _nodeRepository;
        private readonly IAssetSignalRepository _nodeSignalRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly string _filePath;
        private readonly AssetContext _context;

        public FileService(
        IAssetNodeRepository nodeRepository,
        IAssetSignalRepository nodeSignalRepository,
        IAuditLogService auditLogService,
        AssetContext context,
        string filePath)
        {
            _nodeRepository = nodeRepository;
            _nodeSignalRepository = nodeSignalRepository;
            _auditLogService = auditLogService;
            _context = context;
            _filePath = filePath;
        }

        public async Task UpdateJsonFileAsync()
        {
            var allNodes = await _nodeRepository.GetAllNodesAsync(true, true);
            var hierarchy = BuildHierarchy(allNodes, null);
            var json = JsonConvert.SerializeObject(hierarchy, Formatting.Indented);
            await File.WriteAllTextAsync(_filePath, json);
        }

        private List<AssetNodeDto> BuildHierarchy(List<AssetNode> allNodes, int? parentId)
        {
            return allNodes
                .Where(n => n.ParentId == parentId)
                .Select(n => new AssetNodeDto
                {
                    Id = n.Id,
                    Name = n.Name,
                    ParentId = n.ParentId,
                    Children = BuildHierarchy(allNodes, n.Id),
                    Signals = n.Signals.Select(s => new AssetSignalDto
                    {
                        SignalId = s.SignalId,
                        SignalName = s.SignalName
                    }).ToList()
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

        public async Task<ServiceResponse> ReplaceJsonFileAsync(Stream fileStream)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Deserialize with duplicate key detection
                var nodes = await DeserializeJsonAsync<List<AssetNode>>(fileStream);
                if (nodes == null || !nodes.Any())
                    return new ServiceResponse { Success = false, Message = "No nodes found in JSON" };

                // Validate hierarchy
                ValidateUniqueNames(nodes);

                _context.AssetSignal.RemoveRange(_context.AssetSignal);

                _context.AssetHierarchy.RemoveRange(_context.AssetHierarchy);

                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetHierarchy', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('AssetSignal', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('SignalValues', RESEED, 0)");


                // Insert hierarchy recursively
                foreach (var node in nodes.Where(n => n.ParentId == null))
                {
                    await InsertNodeRecursive(node, null);
                }

                // Save DB changes
                await _context.SaveChangesAsync();

                // Backup old JSON & write new one AFTER DB is successful
                await BackupJsonFileAsync(_filePath, keepLast: 5);
                await UpdateJsonFileAsync();

                // Audit log
                await _auditLogService.LogAsync("JSON File is Uploaded", null, null);

                await transaction.CommitAsync();
                return new ServiceResponse { Success = true, Message = "JSON File Uploaded Successfully" };
            }
            catch (JsonReaderException ex)
            {
                await transaction.RollbackAsync();
                return new ServiceResponse { Success = false, Message = $"Invalid JSON: {ex.Message}" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ServiceResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        private void ValidateUniqueNames(IEnumerable<AssetNode> nodes)
        {
            var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Validate(IEnumerable<AssetNode> nodeList)
            {
                foreach (var node in nodeList)
                {
                    if (!allNames.Add(node.Name))
                        throw new Exception($"Duplicate asset name '{node.Name}' found in JSON.");
                    if (node.Children != null && node.Children.Any())
                        Validate(node.Children);
                }
            }
            Validate(nodes);
        }

        private bool IsValidName(string name)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9_ ]*$");
        }

        private async Task InsertNodeRecursive(AssetNode node, int? newParentId)
        {
            if (!IsValidName(node.Name))
                throw new Exception($"Invalid asset name '{node.Name}'.");

            var newNode = new AssetNode { Name = node.Name, ParentId = newParentId };
            await _nodeRepository.AddNodeAsync(newNode);

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
                    await _nodeSignalRepository.AddSignalAsync(newSignal);
                }
            }

            if (node.Children != null && node.Children.Any())
            {
                foreach (var child in node.Children)
                {
                    await InsertNodeRecursive(child, newNode.Id);
                }
            }
        }
    }
}