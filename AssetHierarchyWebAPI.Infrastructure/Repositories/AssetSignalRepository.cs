using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using AssetHierarchyWebAPI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Infrastructure.Repositories
{
    public class AssetSignalRepository : IAssetSignalRepository
    {
        private readonly AssetContext _context;

        public AssetSignalRepository(AssetContext context)
        {
            _context = context;
        }

        public async Task<AssetSignals> AddSignalAsync(AssetSignals signal)
        {
            await _context.AssetSignal.AddAsync(signal);
            await _context.SaveChangesAsync();
            return signal;
        }

        public async Task RemoveSignalAsync(int signalId)
        {
            var signal = await _context.AssetSignal.FindAsync(signalId);
            if (signal != null)
            {
                _context.AssetSignal.Remove(signal);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateSignalAsync(AssetSignals signal)
        {
            _context.AssetSignal.Update(signal);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AssetSignals>> GetSignalsByNodeIdAsync(int nodeId)
        {
            return await _context.AssetSignal
                .Where(s => s.AssetNodeId == nodeId)
                .ToListAsync();
        }

        public async Task<AssetSignals?> GetSignalByNameAndNodeIdAsync(string signalName, int nodeId)
        {
            return await _context.AssetSignal
                .FirstOrDefaultAsync(s => s.AssetNodeId == nodeId && s.SignalName == signalName);
        }

        public async Task<AssetSignals?> GetSignalByIdAsync(int signalId)
        {
            return await _context.AssetSignal
                .FirstOrDefaultAsync(s => s.SignalId == signalId);
        }
    }
}