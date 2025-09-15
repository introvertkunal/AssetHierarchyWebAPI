using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AssetContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(AssetContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string operation, int? entityId, string? entityName)
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
    }
}