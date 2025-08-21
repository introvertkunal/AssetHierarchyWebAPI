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

        [HttpPost("add")]

        public IActionResult Add(string name, string parentName)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Asset name cannot be empty.");
            }
            return Ok(_service.addNode(name, parentName));
        }

        [HttpDelete("remove")]

        public IActionResult Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Asset name cannot be empty.");
            }
            
            return Ok(_service.removeNode(name));
        }

        [HttpGet("hierarchy")]

        public IActionResult GetHierarchy()
        {
            
            return Ok(_service.GetHierarchy());
        }

        [HttpGet("search")]
        [LogMissingName]

        public IActionResult search(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Asset name cannot be empty.");
            }
            var exists = _service.searchNode(name);

            if(!exists)
            {
                return NotFound($"Asset '{name}' not found.");
            }
            return Ok($"Asset '{name}' found in the hierarchy.");
        }

        [HttpPost("replace-json")]

       

        public async Task<IActionResult> ReplaceJsonFileAsync(IFormFile file)
        {
             
            if(file == null || file.Length == 0)
            {
                return BadRequest("File is empty or not provided.");
            }
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var nodes = JsonSerializer.Deserialize<List<AssetNode>>(json);
                await _service.ReplaceJsonFileAsync(file);
                return Ok("JSON file Uploaded successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest("File not in Correct Format");
            }
        }

        [HttpGet("downloadFile")]

        public IActionResult DownloadFile([FromServices] IConfiguration configuration)
        {
            string format = configuration["storageFormat"] ?? "json";
            string folderPath = Directory.GetCurrentDirectory();

            string fileName = format == "xml" ? "asset_hierarchy.xml" : "asset_hierarchy.json";
            string contentType = format == "xml" ? "application/xml" : "application/json";

            string filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"File '{fileName}' not found.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            return File(fileBytes, contentType, fileName);
        }



    }
}
