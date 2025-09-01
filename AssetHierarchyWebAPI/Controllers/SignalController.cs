using AssetHierarchyWebAPI.Interfaces;
using AssetHierarchyWebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignalController : ControllerBase
    {
        private readonly IAssetSignal _signalService;

        public SignalController(IAssetSignal signalService)
        {
            _signalService = signalService;
        }

        // Add Signal under an Asset
        [HttpPost("{assetId}/add")]
        public async Task<IActionResult> AddSignal(int assetId, [FromBody] AssetSignals signal)
        {
            if (signal == null || string.IsNullOrWhiteSpace(signal.SignalName))
                return BadRequest("Signal details are invalid.");

            var result = await _signalService.AddSignalAsync(assetId, signal);
            return Ok(result);
        }

        // Remove Signal
        [HttpDelete("{signalId}/remove")]
        public async Task<IActionResult> RemoveSignal(int signalId)
        {
            if (signalId < 1)
                return BadRequest("Invalid signal Id.");

            var result = await _signalService.RemoveSignalAsync(signalId);
            return Ok(result);
        }

        // Update Signal
        [HttpPut("{signalId}/update")]
        public async Task<IActionResult> UpdateSignal(int signalId, [FromBody] AssetSignals updatedSignal)
        {
            if (updatedSignal == null || string.IsNullOrWhiteSpace(updatedSignal.SignalName))
                return BadRequest("Updated signal details are invalid.");

            var result = await _signalService.UpdateSignalAsync(signalId, updatedSignal);
            return Ok(result);
        }

        // Get all signals under a Node
        [HttpGet("node/{nodeId}")]
        public async Task<IActionResult> GetSignalsByNodeId(int nodeId)
        {
            if (nodeId < 1)
                return BadRequest("Invalid node Id.");

            var signals = await _signalService.GetSignalsByNodeIdAsync(nodeId);

            if (signals == null || !signals.Any())
                return NotFound($"No signals found for AssetNode with Id {nodeId}.");

            return Ok(signals);
        }
    }
}
