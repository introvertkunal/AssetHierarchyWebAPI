
using AssetHierarchyWebAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssetController : ControllerBase
    {
        private readonly IAssetHierarchyService _service;
        private readonly IConfiguration _configuration;

        public AssetController(IAssetHierarchyService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        // Add Node
        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Asset name cannot be empty.");

            var result = await _service.AddNodeAsync(name, parentId);
            return string.IsNullOrEmpty(result) ? Ok("Node added successfully.") : BadRequest(result);
        }

        // Remove Node 
        [HttpDelete("remove")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(int id)
        {
            if (id < 1)
                return BadRequest("Provide a valid Asset ID");

            var result = await _service.RemoveNodeAsync(id);
            return string.IsNullOrEmpty(result) ? Ok("Node removed successfully.") : BadRequest(result);
        }

        // Get full hierarchy
        [HttpGet("hierarchy")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetHierarchy()
        {
            var hierarchy = await _service.GetHierarchyAsync();
            return Ok(hierarchy);
        }

        // Search node
        [HttpGet("search")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Asset name cannot be empty.");

            var result = await _service.SearchNode(name);
            if (result == null)
                return NotFound($"Asset '{name}' not found.");

            return Ok(new
            {
                Id = result.Id,
                Name = result.NodeName,
                ParentName = result.ParentName,
                Children = result.Children,
                Signals = result.Signals
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
            return string.IsNullOrEmpty(result) ? Ok("Node updated successfully.") : BadRequest(result);
        }

        // Reorder Node (move under new parent)
        [HttpPut("reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reorder(int id, int? newParentId)
        {
            var result = await _service.ReorderNode(id, newParentId);
            return string.IsNullOrEmpty(result) ? Ok("Node reordered successfully.") : BadRequest(result);
        }

        // Replace with uploaded JSON file
        [HttpPost("replace-file")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplaceFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            using var stream = file.OpenReadStream();
            var result = await _service.ReplaceJsonFileAsync(stream);
            return string.IsNullOrEmpty(result) ? Ok("File replaced successfully.") : BadRequest(result);
        }

        // Download current persistence file (only for JSON/XML, not DB)
        [HttpGet("downloadFile")]
        [Authorize(Roles = "Admin,User")]
        public IActionResult DownloadFile()
        {
            string format = _configuration["storageFormat"] ?? "json";

            string folderPath = Directory.GetCurrentDirectory();
            string fileName = "asset_hierarchy.json";
            string contentType = "application/json";

            if (format == "xml")
            {
                fileName = "asset_hierarchy.xml";
                contentType = "application/xml";
            }

            string filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"File '{fileName}' not found.");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, fileName);
        }
    }
}
