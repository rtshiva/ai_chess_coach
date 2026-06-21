using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChessCoach.Api.Controllers;

[ApiController]
[Route("api/analyze")]
public class AnalysisController : ControllerBase
{
    private readonly ActiveCoachingPipeline _pipeline;

    public AnalysisController(ActiveCoachingPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzeMove([FromBody] AnalyzeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FenBefore) || string.IsNullOrWhiteSpace(request.MovePlayedUci) || string.IsNullOrWhiteSpace(request.FenAfter))
            return BadRequest("Invalid request parameters");

        var response = await _pipeline.ProcessTurnAsync(request.FenBefore, request.MovePlayedUci, request.FenAfter, request.PromptTemplate, ct);

        return Ok(new { explanation = response.ExplanationText });
    }
}

public class AnalyzeRequest
{
    public string FenBefore { get; set; } = string.Empty;
    public string MovePlayedUci { get; set; } = string.Empty;
    public string FenAfter { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
}
