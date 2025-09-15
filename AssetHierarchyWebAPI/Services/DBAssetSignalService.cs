using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Hubs;
using AssetHierarchyWebAPI.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace AssetHierarchyWebAPI.Services
{
    public class DBAssetSignalService : IAssetSignal
    {
        private readonly AssetContext _context;
        private const string FilePath_json = "asset_hierarchy.json";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubcontext;

        public DBAssetSignalService(AssetContext context, IHttpContextAccessor httpContextAccessor, IHubContext<NotificationHub> hubcontext)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _hubcontext = hubcontext;
        }

        private bool IsValidName(string name)
        {
            return Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9 ]*$");
        }

        private bool IsValidSignalType(string signalType)
        {
            return signalType.Equals("Integer", StringComparison.OrdinalIgnoreCase) ||
                   signalType.Equals("Real", StringComparison.OrdinalIgnoreCase);
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

        private async Task SendNotificationAsync(string message)
        {
            await _hubcontext.Clients.All.SendAsync("ReceiveNotification", message);
        }

        // Add a new signal under a given AssetNode
        public async Task<string> AddSignalAsync(int assetId, AssetSignals signal)
        {
            try
            {
                if (!IsValidName(signal.SignalName))
                    return $"Invalid signal name '{signal.SignalName}'.";



                var node = await _context.AssetHierarchy.FindAsync(assetId);
                if (node == null)
                    return $"Asset with Id {assetId} not found.";

                if (!IsValidSignalType(signal.SignalType))
                    return $"Invalid SignalType '{signal.SignalType}'. Only 'Integer' or 'Real' are allowed.";

                bool exists = await _context.AssetSignal
                    .AnyAsync(s => s.AssetNodeId == assetId && s.SignalName == signal.SignalName);

                if (exists)
                    return $"Signal '{signal.SignalName}' already exists under Asset '{node.Name}'.";

                signal.AssetNodeId = assetId;

                await _context.AssetSignal.AddAsync(signal);
                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();
                await LogAuditAsync($"Added Signal under Asset {assetId}", signal.SignalId, signal.SignalName);

                await SendNotificationAsync($"New Signal '{signal.SignalName}' ({signal.SignalType}) added under Asset '{node.Name}'");

                return $"Signal '{signal.SignalName}' added to Asset '{node.Name}'.";
            }
            catch (Exception ex)
            {
                return $"Error adding signal: {ex.Message}";
            }
        }

        // Remove a signal by Id
        public async Task<string> RemoveSignalAsync(int signalId)
        {
            try
            {
                var signal = await _context.AssetSignal.Include(s => s.AssetNode).FirstOrDefaultAsync(s => s.SignalId == signalId);
                if (signal == null)
                    return $"Signal with Id {signalId} not found.";

                string parentName = signal.AssetNode?.Name ?? "Unknown";

                _context.AssetSignal.Remove(signal);
                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();
                await LogAuditAsync("Removed Signal", signal.SignalId, signal.SignalName);

                await SendNotificationAsync($"Signal '{signal.SignalName}' removed from Asset '{parentName}'");

                return $"Signal '{signal.SignalName}' removed successfully.";
            }
            catch (Exception ex)
            {
                return $"Error removing signal: {ex.Message}";
            }
        }

        // Update signal details
        public async Task<string> UpdateSignalAsync(int signalId, AssetSignals updatedSignal)
        {
            try
            {
                var signal = await _context.AssetSignal.Include(s => s.AssetNode).FirstOrDefaultAsync(s => s.SignalId == signalId);
                if (signal == null)
                    return $"Signal with Id {signalId} not found.";

                if (!IsValidSignalType(updatedSignal.SignalType))
                    return $"Invalid SignalType '{updatedSignal.SignalType}'. Only 'Integer' or 'Real' are allowed.";

                // Check for duplicate signal name
                bool exists = await _context.AssetSignal
                    .AnyAsync(s => s.AssetNodeId == signal.AssetNodeId &&
                                   s.SignalName == updatedSignal.SignalName &&
                                   s.SignalId != signalId);

                if (exists)
                    return $"Signal '{updatedSignal.SignalName}' already exists under this asset.";

                string oldName = signal.SignalName;
                string parentName = signal.AssetNode?.Name ?? "Unknown";

                signal.SignalName = updatedSignal.SignalName;
                signal.SignalType = updatedSignal.SignalType;
                signal.Description = updatedSignal.Description;

                await _context.SaveChangesAsync();
                await UpdateJsonFileAsync();
                await LogAuditAsync("Updated Signal", signal.SignalId, signal.SignalName);

                await SendNotificationAsync($"Signal '{oldName}' updated to '{signal.SignalName}' ({signal.SignalType}) under Asset '{parentName}'");

                return $"Signal '{signal.SignalName}' updated successfully.";
            }
            catch (Exception ex)
            {
                return $"Error updating signal: {ex.Message}";
            }
        }

        // Get all signals for a given AssetNode
        public async Task<List<AssetSignals>> GetSignalsByNodeIdAsync(int nodeId)
        {
            try
            {
                var nodeExists = await _context.AssetHierarchy.AnyAsync(n => n.Id == nodeId);
                if (!nodeExists)
                    return new List<AssetSignals>();

                return await _context.AssetSignal
                                     .Where(s => s.AssetNodeId == nodeId)
                                     .AsNoTracking()
                                     .ToListAsync();
            }
            catch (Exception)
            {
                return new List<AssetSignals>();
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

        private List<AssetNode> BuildHierarchy(List<AssetNode> allNodes, int? parentId)
        {
            return allNodes
                .Where(n => n.ParentId == parentId)
                .Select(n => new AssetNode
                {
                    Id = n.Id,
                    Name = n.Name,
                    ParentId = n.ParentId,

                    Signals = n.Signals?.Select(s => new AssetSignals
                    {
                        SignalId = s.SignalId,
                        SignalName = s.SignalName,
                        SignalType = s.SignalType,
                        Description = s.Description,
                        AssetNodeId = s.AssetNodeId
                    }).ToList(),

                    Children = BuildHierarchy(allNodes, n.Id)
                })
                .ToList();
        }
    }
}
