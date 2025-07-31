using Matchboxd.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Matchboxd.API.Controller;
[ApiController]
[Route("api/[controller]")]
public class MatchImportController : ControllerBase
{
    private readonly MatchImportService _importService;

    public MatchImportController(MatchImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import()
    {
        await _importService.ImportMatchesAsync();
        return Ok("Matches imported!");
    }
}