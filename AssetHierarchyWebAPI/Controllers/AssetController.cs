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
    }
}
