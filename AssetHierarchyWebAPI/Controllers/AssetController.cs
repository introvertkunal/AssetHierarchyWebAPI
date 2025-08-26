using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IAssetHierarchyService _service;

        public AssetController(IAssetHierarchyService service)
        {
            _service = service;
        }

        // Add Node
        [HttpPost("add")]
        public IActionResult Add(string name, int? parentId)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest("Asset name cannot be empty.");

            var result = _service.AddNode(name, parentId);
            return Ok(result);
        }

        // Remove Node 
        [HttpDelete("remove")]
        public IActionResult Remove(int id)
        {
            if(id < 1)
            {
                return BadRequest("Provide a Valid Asset ID");
            }
            var result = _service.RemoveNode(id);
            return Ok(result);
        }

        // Get full hierarchy
        [HttpGet("hierarchy")]
        public IActionResult GetHierarchy()
        {
            return Ok(_service.GetHierarchy());
        }

        // Search node
        [LogMissingName]
        [HttpGet("search")]
        public IActionResult Search(string name)
        {

            if (string.IsNullOrEmpty(name))
                return BadRequest("Asset name cannot be empty.");

            var node = _service.SearchNode(name);

            if (node == null)
                return NotFound($"Asset '{name}' not found.");

            return Ok(new
            {
                Id = node.Id,
                Name = node.NodeName,
                ParentName = node.ParentName,
                Children = node.Children
            });
        }

        // Update Node (rename asset)
        [HttpPut("update")]
        public IActionResult Update(int id, string newName)
        {
            if (string.IsNullOrEmpty(newName))
                return BadRequest("New asset name cannot be empty.");

            var result = _service.UpdateNode(id, newName);
            return Ok(result);
        }

        // Reorder Node (move under new parent)
        [HttpPut("reorder")]
        public IActionResult Reorder(int id, int? newParentId)
        {
            var result = _service.ReorderNode(id, newParentId);
            return Ok(result);
        }

        // Replace with uploaded JSON/Xml file
        [HttpPost("replace-file")]
        public async Task<IActionResult> ReplaceFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var nodes = JsonSerializer.Deserialize<List<AssetNode>>(json);
                await _service.ReplaceJsonFileAsync(file);
                return Ok("File uploaded and replaced successfully.");
            }
            catch
            {
                return BadRequest("File not in correct format.");
            }
        }

        // Download current persistence file
        [HttpGet("downloadFile")]
        public IActionResult DownloadFile([FromServices] IConfiguration configuration)
        {
            string format = configuration["storageFormat"] ?? "json";
            string folderPath = Directory.GetCurrentDirectory();

            string fileName = format == "xml" ? "asset_hierarchy.xml" : "asset_hierarchy.json";
            string contentType = format == "xml" ? "application/xml" : "application/json";

            string filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound($"File '{fileName}' not found.");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, fileName);
        }
    }
}
