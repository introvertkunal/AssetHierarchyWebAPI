
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
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

        // Add Signal under an Asset
        [HttpPost("{assetId}/add")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSignal(int assetId, [FromBody] AssetSignals signal)
        {
            if (signal == null || string.IsNullOrWhiteSpace(signal.SignalName))
                return BadRequest("Signal details are invalid.");

            var result = await _signalService.AddSignalAsync(assetId, signal);
            return string.IsNullOrEmpty(result) ? Ok("Signal added successfully.") : BadRequest(result);
        }

        // Remove Signal
        [HttpDelete("{signalId}/remove")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveSignal(int signalId)
        {
            if (signalId < 1)
                return BadRequest("Invalid signal Id.");

            var result = await _signalService.RemoveSignalAsync(signalId);
            return string.IsNullOrEmpty(result) ? Ok("Signal removed successfully.") : BadRequest(result);
        }

        // Update Signal
        [HttpPut("{signalId}/update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSignal(int signalId, [FromBody] AssetSignals updatedSignal)
        {
            if (updatedSignal == null || string.IsNullOrWhiteSpace(updatedSignal.SignalName))
                return BadRequest("Updated signal details are invalid.");

            var result = await _signalService.UpdateSignalAsync(signalId, updatedSignal);
            return string.IsNullOrEmpty(result) ? Ok("Signal updated successfully.") : BadRequest(result);
        }

        // Get all signals under a Node
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