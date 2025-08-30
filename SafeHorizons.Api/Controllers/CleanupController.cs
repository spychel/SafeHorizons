using Microsoft.AspNetCore.Mvc;
using SafeHorizons.Api.Services;

namespace SafeHorizons.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleanupController : ControllerBase
{
    private readonly IFileCleanupService _cleanupService;
    private readonly ILogger<CleanupController> _logger;

    public CleanupController(IFileCleanupService cleanupService, ILogger<CleanupController> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunCleanup()
    {
        try
        {
            var oldFilesCount = await _cleanupService.GetOldFilesCountAsync(TimeSpan.FromMinutes(30));
            await _cleanupService.CleanupOldFilesAsync(TimeSpan.FromMinutes(30));

            return Ok(new
            {
                Message = "Cleanup completed",
                DeletedFiles = oldFilesCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual cleanup failed");
            return StatusCode(500, "Cleanup failed");
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var oldFilesCount = await _cleanupService.GetOldFilesCountAsync(TimeSpan.FromMinutes(30));
        return Ok(new
        {
            OldFilesCount = oldFilesCount,
            MaxAgeMinutes = 30
        });
    }
}