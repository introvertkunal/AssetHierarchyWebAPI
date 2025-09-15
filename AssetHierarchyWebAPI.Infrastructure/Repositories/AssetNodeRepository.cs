using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using AssetHierarchyWebAPI.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Infrastructure.Repositories
{
    public class AssetNodeRepository : IAssetNodeRepository
    {
        private readonly AssetContext _context;

        public AssetNodeRepository(AssetContext context)
        {
            _context = context;
        }

        public async Task<AssetNode> AddNodeAsync(AssetNode node)
        {
            await _context.AssetHierarchy.AddAsync(node);
            await _context.SaveChangesAsync();
            return node;
        }

        public async Task<AssetNode?> GetNodeByIdAsync(int id, bool includeChildren = false, bool includeSignals = false)
        {
            var query = _context.AssetHierarchy.AsQueryable();
            if (includeChildren)
                query = query.Include(n => n.Children);
            if (includeSignals)
                query = query.Include(n => n.Signals);
            return await query.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<AssetNode?> GetNodeByNameAsync(string name)
        {
            return await _context.AssetHierarchy
                .Include(n => n.Children)
                .Include(n => n.Signals)
                .FirstOrDefaultAsync(n => n.Name.ToLower() == name.ToLower());
        }

        public async Task<List<AssetNode>> GetAllNodesAsync(bool includeChildren = false, bool includeSignals = false)
        {
            var query = _context.AssetHierarchy.AsNoTracking();
            if (includeChildren)
                query = query.Include(n => n.Children);
            if (includeSignals)
                query = query.Include(n => n.Signals);
            return await query.ToListAsync();
        }

        public async Task<bool> NodeExistsAsync(string name)
        {
            return await _context.AssetHierarchy.AnyAsync(n => n.Name == name);
        }

        public async Task<bool> NodeExistsByIdAsync(int id)
        {
            return await _context.AssetHierarchy.AnyAsync(n => n.Id == id);
        }

        public async Task RemoveNodeAsync(AssetNode node)
        {
            _context.AssetHierarchy.Remove(node);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateNodeAsync(AssetNode node)
        {
            _context.AssetHierarchy.Update(node);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsDescendantAsync(int nodeId, int potentialParentId)
        {
            var parent = await _context.AssetHierarchy.FindAsync(potentialParentId);
            while (parent != null)
            {
                if (parent.ParentId == nodeId)
                    return true;
                parent = await _context.AssetHierarchy.FindAsync(parent.ParentId);
            }
            return false;
        }
    }
}