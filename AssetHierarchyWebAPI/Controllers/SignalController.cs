using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SignalController : ControllerBase
    {
        private readonly IAssetSignalService _signalService;

        public SignalController(IAssetSignalService signalService)
        {
            _signalService = signalService;
        }

        [HttpPost("{assetId}/add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSignal(int assetId, [FromBody] AssetSignalDto signalDto)
        {
            if (signalDto == null || string.IsNullOrWhiteSpace(signalDto.SignalName))
                return BadRequest("Signal details are invalid.");

            var result = await _signalService.AddSignalAsync(assetId, signalDto);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpDelete("{signalId}/remove")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveSignal(int signalId)
        {
            if (signalId < 1)
                return BadRequest("Invalid signal Id.");

            var result = await _signalService.RemoveSignalAsync(signalId);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpPut("{signalId}/update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSignal(int signalId, [FromBody] AssetSignalDto updatedSignalDto)
        {
            if (updatedSignalDto == null || string.IsNullOrWhiteSpace(updatedSignalDto.SignalName))
                return BadRequest("Updated signal details are invalid.");

            var result = await _signalService.UpdateSignalAsync(signalId, updatedSignalDto);
            return result.Success ? Ok(result.Message) : BadRequest(result.Message);
        }

        [HttpGet("node/{nodeId}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetSignalsByNodeId(int nodeId)
        {
            if (nodeId < 1)
                return BadRequest("Invalid node Id.");

            var signals = await _signalService.GetSignalsByNodeIdAsync(nodeId);
            return signals == null || !signals.Any()
                ? NotFound($"No signals found for AssetNode with Id {nodeId}.")
                : Ok(signals);
        }
    }
}