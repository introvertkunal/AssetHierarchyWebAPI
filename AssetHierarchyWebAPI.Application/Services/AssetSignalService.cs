using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using System.Text.RegularExpressions;

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
            return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9 ]*$");
        }

        private bool IsValidSignalType(string signalType)
        {
            return !string.IsNullOrWhiteSpace(signalType) &&
                   signalType.Equals("Integer", StringComparison.OrdinalIgnoreCase) ||
                   signalType.Equals("Real", StringComparison.OrdinalIgnoreCase);
        }

        private AssetSignals MapToEntity(AssetSignalDto dto)
        {
            return new AssetSignals
            {
                SignalId = dto.SignalId,
                SignalName = dto.SignalName,
                SignalType = dto.SignalType,
                Description = dto.Description,
                AssetNodeId = dto.AssetNodeId
            };
        }

        private AssetSignalDto MapToDto(AssetSignals entity)
        {
            return new AssetSignalDto
            {
                SignalId = entity.SignalId,
                SignalName = entity.SignalName,
                SignalType = entity.SignalType,
                Description = entity.Description,
                AssetNodeId = entity.AssetNodeId
            };
        }

        public async Task<ServiceResult> AddSignalAsync(int assetId, AssetSignalDto signalDto)
        {
            try
            {
                if (signalDto == null)
                    return new ServiceResult(false, "Signal data is null.");

                if (!IsValidName(signalDto.SignalName))
                    return new ServiceResult(false, $"Invalid signal name '{signalDto.SignalName}'.");

                var node = await _nodeRepository.GetNodeByIdAsync(assetId);
                if (node == null)
                    return new ServiceResult(false, $"Asset with Id {assetId} not found.");

                if (!IsValidSignalType(signalDto.SignalType))
                    return new ServiceResult(false, $"Invalid SignalType '{signalDto.SignalType}'. Only 'Integer' or 'Real' are allowed.");

                var existingSignal = await _signalRepository.GetSignalByNameAndNodeIdAsync(signalDto.SignalName, assetId);
                if (existingSignal != null)
                    return new ServiceResult(false, $"Signal '{signalDto.SignalName}' already exists under Asset '{node.Name}'.");

                var signal = MapToEntity(signalDto);
                signal.AssetNodeId = assetId;
                await _signalRepository.AddSignalAsync(signal);
                await _fileService.UpdateJsonFileAsync();

                await _auditLogService.LogAsync($"Added Signal '{signal.SignalName}' under Asset '{node.Name}'", signal.SignalId, signal.SignalName);
                await _notificationService.SendAsync($"New Signal '{signal.SignalName}' ({signal.SignalType}) added under Asset '{node.Name}'");

                return new ServiceResult(true, $"Signal '{signal.SignalName}' added to Asset '{node.Name}'.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(false, $"Error adding signal: {ex.Message}");
            }
        }

        public async Task<ServiceResult> RemoveSignalAsync(int signalId)
        {
            try
            {
                if (signalId < 1)
                    return new ServiceResult(false, "Invalid signal Id.");

                var signal = await _signalRepository.GetSignalByIdAsync(signalId);
                if (signal == null)
                    return new ServiceResult(false, $"Signal with Id {signalId} not found.");

                var node = await _nodeRepository.GetNodeByIdAsync(signal.AssetNodeId);
                string parentName = node?.Name ?? "Unknown";

                await _signalRepository.RemoveSignalAsync(signalId);
                await _fileService.UpdateJsonFileAsync();

                await _auditLogService.LogAsync($"Removed Signal '{signal.SignalName}' from Asset '{parentName}'", signalId, signal.SignalName);
                await _notificationService.SendAsync($"Signal '{signal.SignalName}' removed from Asset '{parentName}'");

                return new ServiceResult(true, $"Signal '{signal.SignalName}' removed successfully.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(false, $"Error removing signal: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateSignalAsync(int signalId, AssetSignalDto updatedSignalDto)
        {
            try
            {
                if (updatedSignalDto == null)
                    return new ServiceResult(false, "Updated signal data is null.");

                var signal = await _signalRepository.GetSignalByIdAsync(signalId);
                if (signal == null)
                    return new ServiceResult(false, $"Signal with Id {signalId} not found.");

                if (!IsValidName(updatedSignalDto.SignalName))
                    return new ServiceResult(false, $"Invalid signal name '{updatedSignalDto.SignalName}'.");

                if (!IsValidSignalType(updatedSignalDto.SignalType))
                    return new ServiceResult(false, $"Invalid SignalType '{updatedSignalDto.SignalType}'. Only 'Integer' or 'Real' are allowed.");

                var existingSignal = await _signalRepository.GetSignalByNameAndNodeIdAsync(updatedSignalDto.SignalName, signal.AssetNodeId);
                if (existingSignal != null && existingSignal.SignalId != signalId)
                    return new ServiceResult(false, $"Signal '{updatedSignalDto.SignalName}' already exists under this asset.");

                var node = await _nodeRepository.GetNodeByIdAsync(signal.AssetNodeId);
                string parentName = node?.Name ?? "Unknown";
                string oldName = signal.SignalName;

                signal.SignalName = updatedSignalDto.SignalName;
                signal.SignalType = updatedSignalDto.SignalType;
                signal.Description = updatedSignalDto.Description;

                await _signalRepository.UpdateSignalAsync(signal);
                await _fileService.UpdateJsonFileAsync();

                await _auditLogService.LogAsync($"Updated Signal '{oldName}' to '{signal.SignalName}' under Asset '{parentName}'", signalId, signal.SignalName);
                await _notificationService.SendAsync($"Signal '{oldName}' updated to '{signal.SignalName}' ({signal.SignalType}) under Asset '{parentName}'");

                return new ServiceResult(true, $"Signal '{signal.SignalName}' updated successfully.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(false, $"Error updating signal: {ex.Message}");
            }
        }

        public async Task<List<AssetSignalDto>> GetSignalsByNodeIdAsync(int nodeId)
        {
            try
            {
                if (nodeId < 1)
                    return new List<AssetSignalDto>();

                if (!await _nodeRepository.NodeExistsByIdAsync(nodeId))
                    return new List<AssetSignalDto>();

                var signals = await _signalRepository.GetSignalsByNodeIdAsync(nodeId);
                return signals.Select(MapToDto).ToList();
            }
            catch (Exception)
            {
                return new List<AssetSignalDto>();
            }
        }
    }
}