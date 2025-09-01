using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Services
{
    public class DBAssetSignalService : IAssetSignal
    {
        private readonly AssetContext _context;

        public DBAssetSignalService(AssetContext context)
        {
            _context = context;
        }

        private bool IsValidSignalType(string signalType)
        {
            return signalType.Equals("Integer", StringComparison.OrdinalIgnoreCase) ||
                   signalType.Equals("Real", StringComparison.OrdinalIgnoreCase);
        }

        // Add a new signal under a given AssetNode
        public async Task<string> AddSignalAsync(int assetId, AssetSignals signal)
        {
            try
            {
                var node = await _context.AssetHierarchy.FindAsync(assetId);
                if (node == null)
                    return $"Asset with Id {assetId} not found.";

                if (!IsValidSignalType(signal.SignalType))
                    return $"Invalid SignalType '{signal.SignalType}'. Only 'Integer' or 'Real' are allowed.";

                bool exists = await _context.AssetSignal
                    .AnyAsync(s => s.AssetNodeId == assetId && s.SignalName == signal.SignalName);

                if (exists)
                    return $"Signal '{signal.SignalName}' already exists under Asset {node.Name}.";

                signal.AssetNodeId = assetId;

                await _context.AssetSignal.AddAsync(signal);
                await _context.SaveChangesAsync();

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
                var signal = await _context.AssetSignal.FindAsync(signalId);
                if (signal == null)
                    return $"Signal with Id {signalId} not found.";

                _context.AssetSignal.Remove(signal);
                await _context.SaveChangesAsync();

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
                var signal = await _context.AssetSignal.FindAsync(signalId);
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

                signal.SignalName = updatedSignal.SignalName;
                signal.SignalType = updatedSignal.SignalType;
                signal.Description = updatedSignal.Description;

                await _context.SaveChangesAsync();
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
    }
}
