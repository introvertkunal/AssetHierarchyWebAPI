using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetController : ControllerBase
    {
        private readonly IAssetHierarchyService _service;

        public AssetController(IAssetHierarchyService service)
        {
            _service = service;
        }

        // Add Node
        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Asset name cannot be empty.");

            var result = await _service.AddNodeAsync(name, parentId);
            return Ok(result);
        }

        // Remove Node 
        [HttpDelete("remove")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Remove(int id)
        {
            if (id < 1)
                return BadRequest("Provide a valid Asset ID");

            var result = await _service.RemoveNodeAsync(id);
            return Ok(result);
        }

        // Get full hierarchy
        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchy()
        {
            var hierarchy = await _service.GetHierarchyAsync();
            return Ok(hierarchy);
        }

        // Search node
        [LogMissingName]
        [HttpGet("search")]
        public async Task<IActionResult> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Asset name cannot be empty.");

            var node = await _service.SearchNode(name);

            if (node == null)
                return NotFound($"Asset '{name}' not found.");

            return Ok(new
            {
                Id = node.Id,
                Name = node.NodeName,
                ParentName = node.ParentName,
                Children = node.Children,
                Signals = node.Signals
            });
        }

        // Update Node (rename asset)
        [HttpPut("update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("New asset name cannot be empty.");

            var result = await _service.UpdateNode(id, newName);
            return Ok(result);
        }

        // Reorder Node (move under new parent)
        [HttpPut("reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reorder(int id, int? newParentId)
        {
            var result = await _service.ReorderNode(id, newParentId);
            return Ok(result);
        }

        // Replace with uploaded JSON file
        [HttpPost("replace-file")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplaceFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            try
            {
                var result = await _service.ReplaceJsonFileAsync(file);
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return BadRequest("File is not in Correct Format");
            }
        }


        // Download current persistence file (only for JSON/XML, not DB)
        [HttpGet("downloadFile")]
        [Authorize(Roles = "Admin,User")]
        public IActionResult DownloadFile([FromServices] IConfiguration configuration)
        {
            string format = configuration["storageFormat"] ?? "json";

            string folderPath = Directory.GetCurrentDirectory();

            string fileName = "asset_hierarchy.json";
            string contentType = "application/json";

            if (format == "xml")
            {
                 fileName = "asset_hierarchy.xml";
                 contentType =  "application/xml";
            }
                
            string filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"File '{fileName}' not found.");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, fileName);
        }
    }
}
