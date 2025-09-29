
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


        [HttpPost("add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Asset name cannot be empty.");

            var result = await _service.AddNodeAsync(name, parentId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpDelete("remove")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Remove(int id)
        {
            if (id < 1)
                return BadRequest("Provide a valid Asset ID");

            var result = await _service.RemoveNodeAsync(id);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPut("update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("New asset name cannot be empty.");

            var result = await _service.UpdateNode(id, newName);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPut("reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reorder(int id, int? newParentId)
        {
            var result = await _service.ReorderNode(id, newParentId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPost("replace-file")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplaceFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            using var stream = file.OpenReadStream();
            var result = await _service.ReplaceJsonFileAsync(stream);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("hierarchy")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetHierarchy()
        {
            var hierarchy = await _service.GetHierarchyAsync();
            return Ok(hierarchy);
        }


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
