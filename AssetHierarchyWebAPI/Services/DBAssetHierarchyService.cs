using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Services
{
    public class DBAssetHierarchyService : IAssetHierarchyService
    {
        private readonly AssetContext _context;
        public DBAssetHierarchyService(AssetContext Context)
        {
            _context = Context;
        }

        public string AddNode(string name, int? parentId)
        {
            if(_context.AssetHierarchy.Any(n => n.Name == name))
                return $"Asset '{name}' already exists.";

            if(parentId != null && !_context.AssetHierarchy.Any(n=> n.Id == parentId))
                return $"Parent with Id {parentId} not found.";

            var newNode = new AssetNode
            {
                Name = name,
                ParentId = parentId
            };

            _context.AssetHierarchy.Add(newNode);
            _context.SaveChanges();
            return $"Asset {name} is added Successfully";
        }

        public List<AssetNode> GetHierarchy()
        {
            return _context.AssetHierarchy
                      .Where(n => n.ParentId == null)
                      .Include(n => n.Children)
                      .ToList();
        }

        public string RemoveNode(int id)
        {
            var node = _context.AssetHierarchy
                         .Include(n => n.Children)
                         .FirstOrDefault(n => n.Id == id);

            if (node == null)
                return "Asset is not Exist";

            DeleteChildren(node);

            _context.AssetHierarchy.Remove(node);
            _context.SaveChanges();

            return $"Asset {node.Name} Removed Successfully";
        }

        private void DeleteChildren(AssetNode node)
        {
            if(node.Children != null && node.Children.Any())
            {
                foreach(var child in node.Children.ToList())
                {
                    DeleteChildren(child);
                    _context.AssetHierarchy.Remove(child);
                    _context.SaveChanges();
                }
            }
        }

        public string ReorderNode(int id, int? newParentId)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceJsonFileAsync(IFormFile file)
        {
            throw new NotImplementedException();
        }

        public AssetSearchResult SearchNode(string name)
        {
            var node = _context.AssetHierarchy
                             .Include(n => n.Children)
                             .FirstOrDefault(n => n.Name.ToLower() == name.ToLower());

            if (node == null)
                return null;

            var parentName = node.ParentId != null ? _context.AssetHierarchy
                                                     .Where(n=> n.Id == node.ParentId)
                                                     .Select(n => n.Name).FirstOrDefault() : null;

            return new AssetSearchResult
            {
                Id = node.Id,
                NodeName = node.Name,
                ParentName = parentName,
                Children = node.Children.Select(c => c.Name).ToList()
            };
        }

        public string UpdateNode(int id, string newName)
        {
            var node = _context.AssetHierarchy.FirstOrDefault(n => n.Id == id);

            if (node == null)
                return $"Asset is not Exist";

            var prevName = node.Name;
            node.Name = newName;

            _context.SaveChanges();

            return $"{prevName} is renamed to {node.Name} ";



        }
    }
}
