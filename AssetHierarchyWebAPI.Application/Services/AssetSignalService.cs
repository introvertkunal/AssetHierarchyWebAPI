using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Application.Services
{
    public class AssetSignalService : IAssetSignalService
    {
        private readonly IAssetSignalRepository _signalRepository;
        private readonly IAssetNodeRepository _nodeRepository;
        private readonly IFileService _fileService;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public AssetSignalService(
            IAssetSignalRepository signalRepository,
            IAssetNodeRepository nodeRepository,
            IFileService fileService,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _signalRepository = signalRepository;
            _nodeRepository = nodeRepository;
            _fileService = fileService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
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

        public async Task<string> AddSignalAsync(int assetId, AssetSignals signal)
        {
            try
            {
                if (!IsValidName(signal.SignalName))
                    return $"Invalid signal name '{signal.SignalName}'.";

                var node = await _nodeRepository.GetNodeByIdAsync(assetId);
                if (node == null)
                    return $"Asset with Id {assetId} not found.";

                if (!IsValidSignalType(signal.SignalType))
                    return $"Invalid SignalType '{signal.SignalType}'. Only 'Integer' or 'Real' are allowed.";

                var existingSignal = await _signalRepository.GetSignalByNameAndNodeIdAsync(signal.SignalName, assetId);
                if (existingSignal != null)
                    return $"Signal '{signal.SignalName}' already exists under Asset '{node.Name}'.";

                signal.AssetNodeId = assetId;
                await _signalRepository.AddSignalAsync(signal);
                await _fileService.UpdateJsonFileAsync();

                await _auditLogService.LogAsync($"Added Signal '{signal.SignalName}' under Asset '{node.Name}'", signal.SignalId, signal.SignalName);
                await _notificationService.SendAsync($"New Signal '{signal.SignalName}' ({signal.SignalType}) added under Asset '{node.Name}'");

                return $"Signal '{signal.SignalName}' added to Asset '{node.Name}'.";
            }
            catch (Exception ex)
            {
                return $"Error adding signal: {ex.Message}";
            }
        }

        public async Task<string> RemoveSignalAsync(int signalId)
        {
            try
            {
                var signal = await _signalRepository.GetSignalByIdAsync(signalId);
                if (signal == null)
                    return $"Signal with Id {signalId} not found.";

                var node = await _nodeRepository.GetNodeByIdAsync(signal.AssetNodeId);
                string parentName = node?.Name ?? "Unknown";

                await _signalRepository.RemoveSignalAsync(signalId);
                await _fileService.UpdateJsonFileAsync();

                await _auditLogService.LogAsync($"Removed Signal '{signal.SignalName}' from Asset '{parentName}'", signalId, signal.SignalName);
                await _notificationService.SendAsync($"Signal '{signal.SignalName}' removed from Asset '{parentName}'");

                return $"Signal '{signal.SignalName}' removed successfully.";
            }
            catch (Exception ex)
            {
                return $"Error removing signal: {ex.Message}";
            }
        }

        public async Task<string> UpdateSignalAsync(int signalId, AssetSignals updatedSignal)
        {
            try
            {
                var signal = await _signalRepository.GetSignalsByIdAsync(signalId);
                if (signal == null)
                    return $"Signal with Id {signalId} not found.";

                if (!IsValidSignalType(updatedSignal.SignalType))
                    return $"Invalid SignalType '{updatedSignal.SignalType}'. Only 'Integer' or 'Real' are allowed.";

                var existingSignal = await _signalRepository.GetSignalByNameAndNodeIdAsync(updatedSignal.SignalName, signal.AssetNodeId);
                if (existingSignal != null && existingSignal.SignalId != signalId)
                    return $"Signal '{updatedSignal.SignalName}' already exists under this asset.";

                var node = await _nodeRepository.GetNodeByIdAsync(signal.AssetNodeId);
                string parentName = node?.Name ?? "Unknown";
                string oldName = signal.SignalName;

                signal.SignalName = updatedSignal.SignalName;
                signal.SignalType = updatedSignal.SignalType;
                signal.Description = updatedSignal.Description;

                await _signalRepository.UpdateSignalAsync(signal);
                await _fileService.UpdateJsonFileAsync();

                await _auditLogService.LogAsync($"Updated Signal '{oldName}' to '{signal.SignalName}' under Asset '{parentName}'", signalId, signal.SignalName);
                await _notificationService.SendAsync($"Signal '{oldName}' updated to '{signal.SignalName}' ({signal.SignalType}) under Asset '{parentName}'");

                return $"Signal '{signal.SignalName}' updated successfully.";
            }
            catch (Exception ex)
            {
                return $"Error updating signal: {ex.Message}";
            }
        }

        public async Task<List<AssetSignals>> GetSignalsByNodeIdAsync(int nodeId)
        {
            try
            {
                if (!await _nodeRepository.NodeExistsByIdAsync(nodeId))
                    return new List<AssetSignals>();

                return await _signalRepository.GetSignalsByNodeIdAsync(nodeId);
            }
            catch (Exception)
            {
                return new List<AssetSignals>();
            }
        }
    }
}