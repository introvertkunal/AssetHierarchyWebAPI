using AssetHierarchyWebAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("replace-json")]

        public IActionResult ReplaceJsonFile(IFormFile file)
        {
            if(file == null || file.Length == 0)
            {
                return BadRequest("File is empty or not provided.");
            }
            try
            {
                _service.ReplaceJsonFile(file);
                return Ok("JSON file Uploaded successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
