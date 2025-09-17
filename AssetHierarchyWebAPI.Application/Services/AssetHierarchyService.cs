using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using AutoMapper;
using Newtonsoft.Json;

namespace AssetHierarchyWebAPI.Application.Services
{
    public class AssetHierarchyService : IAssetHierarchyService
    {
        private readonly IAssetNodeRepository _nodeRepository;
        private readonly IFileService _fileService;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly IAssetSignalRepository _nodeSignalRepository;

        public AssetHierarchyService(
            IAssetNodeRepository nodeRepository,
            IFileService fileService,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IMapper mapper,
            IAssetSignalRepository nodeSignalRepository)
        {
            _nodeRepository = nodeRepository;
            _fileService = fileService;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _mapper = mapper;
            _nodeSignalRepository = nodeSignalRepository;
        }

        private bool IsValidName(string name)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9_ ]*$");
        }

        // ----------------- ADD NODE -----------------
        public async Task<ServiceResponse> AddNodeAsync(string name, int? parentId)
        {
            if (!IsValidName(name))
                return new ServiceResponse { Success = false, Message = $"Invalid asset name '{name}'." };

            if (await _nodeRepository.NodeExistsAsync(name))
                return new ServiceResponse { Success = false, Message = $"Asset '{name}' already exists." };

            if (parentId != null && !await _nodeRepository.NodeExistsByIdAsync(parentId.Value))
                return new ServiceResponse { Success = false, Message = $"Parent with Id {parentId} not found." };

            var newNode = new AssetNode { Name = name, ParentId = parentId };
            await _nodeRepository.AddNodeAsync(newNode);

            var parentName = parentId != null
                ? (await _nodeRepository.GetNodeByIdAsync(parentId.Value))?.Name ?? "Root"
                : "Root";

            await _auditLogService.LogAsync($"New Asset '{name}' added under '{parentName}'", newNode.Id, newNode.Name);
            await _notificationService.SendAsync($"New Asset '{name}' added under '{parentName}'");
            await _fileService.UpdateJsonFileAsync();

            return new ServiceResponse { Success = true, Message = $"Asset {name} added successfully." };
        }

        // ----------------- GET HIERARCHY -----------------
        public async Task<List<AssetNodeDto>> GetHierarchyAsync()
        {
            var allNodes = await _nodeRepository.GetAllNodesAsync(true, true);
            return BuildHierarchy(allNodes, null);
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

        // ----------------- REMOVE NODE -----------------
        public async Task<ServiceResponse> RemoveNodeAsync(int id)
        {
            var node = await _nodeRepository.GetNodeByIdAsync(id, true);
            if (node == null)
                return new ServiceResponse { Success = false, Message = "Asset does not exist." };

            await DeleteNodeRecursive(node);
            await _fileService.UpdateJsonFileAsync();

            return new ServiceResponse { Success = true, Message = $"Asset {node.Name} and its children removed successfully." };
        }

        private async Task DeleteNodeRecursive(AssetNode node)
        {
            foreach (var child in node.Children.ToList())
            {
                await DeleteNodeRecursive(child);
            }

            var parentName = node.ParentId != null
                ? (await _nodeRepository.GetNodeByIdAsync(node.ParentId.Value))?.Name ?? "Root"
                : "Root";

            await _nodeRepository.RemoveNodeAsync(node);
            await _auditLogService.LogAsync($"Asset '{node.Name}' removed from '{parentName}'", node.Id, node.Name);
            await _notificationService.SendAsync($"Asset '{node.Name}' removed from '{parentName}'");
        }

        // ----------------- UPDATE NODE -----------------
        public async Task<ServiceResponse> UpdateNode(int id, string newName)
        {
            if (!IsValidName(newName))
                return new ServiceResponse { Success = false, Message = $"Invalid asset name '{newName}'." };

            var node = await _nodeRepository.GetNodeByIdAsync(id);
            if (node == null)
                return new ServiceResponse { Success = false, Message = $"Asset with ID {id} does not exist." };

            if (await _nodeRepository.NodeExistsAsync(newName))
                return new ServiceResponse { Success = false, Message = $"Asset name '{newName}' already exists." };

            var prevName = node.Name;
            node.Name = newName;
            await _nodeRepository.UpdateNodeAsync(node);

            var parentName = node.ParentId != null
                ? (await _nodeRepository.GetNodeByIdAsync(node.ParentId.Value))?.Name ?? "Root"
                : "Root";

            await _auditLogService.LogAsync($"Asset '{prevName}' renamed to '{newName}' under '{parentName}'", node.Id, prevName);
            await _notificationService.SendAsync($"Asset '{prevName}' renamed to '{newName}' under '{parentName}'");
            await _fileService.UpdateJsonFileAsync();

            return new ServiceResponse { Success = true, Message = $"{prevName} renamed to {newName}." };
        }

        // ----------------- REORDER NODE -----------------
        public async Task<ServiceResponse> ReorderNode(int id, int? newParentId)
        {
            var node = await _nodeRepository.GetNodeByIdAsync(id);
            if (node == null)
                return new ServiceResponse { Success = false, Message = "Asset does not exist." };

            if (newParentId != null)
            {
                if (!await _nodeRepository.NodeExistsByIdAsync(newParentId.Value))
                    return new ServiceResponse { Success = false, Message = "New parent does not exist." };

                if (id == newParentId)
                    return new ServiceResponse { Success = false, Message = "A node cannot be its own parent." };

                if (await _nodeRepository.IsDescendantAsync(id, newParentId.Value))
                    return new ServiceResponse { Success = false, Message = "Invalid move: cannot assign descendant as parent." };
            }

            var oldParentName = node.ParentId != null
                ? (await _nodeRepository.GetNodeByIdAsync(node.ParentId.Value))?.Name ?? "Root"
                : "Root";

            node.ParentId = newParentId;
            await _nodeRepository.UpdateNodeAsync(node);

            var newParentName = newParentId != null
                ? (await _nodeRepository.GetNodeByIdAsync(newParentId.Value))?.Name ?? "Root"
                : "Root";

            await _auditLogService.LogAsync($"Asset '{node.Name}' moved from '{oldParentName}' to '{newParentName}'", node.Id, node.Name);
            await _notificationService.SendAsync($"Asset '{node.Name}' moved from '{oldParentName}' to '{newParentName}'");
            await _fileService.UpdateJsonFileAsync();

            return new ServiceResponse { Success = true, Message = "Node reordered successfully." };
        }

        // ----------------- SEARCH NODE -----------------
        public async Task<AssetSearchResult?> SearchNode(string name)
        {
            var node = await _nodeRepository.GetNodeByNameAsync(name);
            if (node == null)
                return null;

            var parentName = node.ParentId != null
                ? (await _nodeRepository.GetNodeByIdAsync(node.ParentId.Value))?.Name
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

        // ----------------- REPLACE JSON FILE -----------------
        public async Task<ServiceResponse> ReplaceJsonFileAsync(Stream fileStream)
        {
            try
            {
                var nodes = await _fileService.DeserializeJsonAsync<List<AssetNode>>(fileStream);
                if (nodes == null || !nodes.Any())
                    return new ServiceResponse { Success = false, Message = "No nodes found in JSON" };

                ValidateUniqueNames(nodes);
                await _nodeRepository.ClearHierarchyAsync();

                foreach (var node in nodes.Where(n => n.ParentId == null))
                {
                    await InsertNodeRecursive(node, null);
                }

                await _auditLogService.LogAsync("JSON File is Uploaded", null, null);
                return new ServiceResponse { Success = true, Message = "JSON File Uploaded Successfully" };
            }
            catch (JsonReaderException)
            {
                return new ServiceResponse { Success = false, Message = "JSON File Contains Duplicate Keys" };
            }
            catch (Exception)
            {
                return new ServiceResponse { Success = false, Message = "JSON File is not in Correct Format" };
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
