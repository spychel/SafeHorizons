using Microsoft.AspNetCore.Mvc;

namespace SafeHorizons.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IWebHostEnvironment environment, ILogger<FilesController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Получить файл по имени
    /// </summary>
    /// <param name="fileName">наименование файла с расширением</param>
    /// <returns></returns>
    [HttpGet("get/{fileName}")]
    public async Task<IActionResult> GetFile([FromRoute] string fileName)
    {
        try
        {
            // Проверяем безопасность имени файла
            if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || Path.GetExtension(fileName) != ".drawio")
            {
                return BadRequest("Invalid file name");
            }

            var filePath = Path.Combine(_environment.WebRootPath, fileName);

            // Проверяем существование файла
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FileName}", fileName);
                return NotFound($"File {fileName} not found");
            }

            // Читаем файл и возвращаем как поток
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/xml", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {FileName}", fileName);
            return StatusCode(500, "Internal server error");
        }
    }
}