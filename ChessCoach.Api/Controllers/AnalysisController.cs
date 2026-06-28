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

        var response = await _pipeline.ProcessTurnAsync(request.FenBefore, request.MovePlayedUci, request.FenAfter, request.MoveHistory, request.PromptTemplate, ct);

        return Ok(new { explanation = response.ExplanationText });
    }

    [HttpPost("stream")]
    public async Task AnalyzeMoveStream([FromBody] AnalyzeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FenBefore) || string.IsNullOrWhiteSpace(request.MovePlayedUci) || string.IsNullOrWhiteSpace(request.FenAfter))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Invalid request parameters");
            return;
        }

        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var chunk in _pipeline.ProcessTurnStreamAsync(request.FenBefore, request.MovePlayedUci, request.FenAfter, request.MoveHistory, request.PromptTemplate, ct))
            {
                // Write SSE formatted data
                var data = $"data: {chunk.Replace("\n", "\\n")}\n\n";
                await Response.WriteAsync(data, ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (Exception ex)
        {
            var data = $"data: [ERROR] {ex.Message.Replace("\n", "\\n")}\n\n";
            await Response.WriteAsync(data, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}

public class AnalyzeRequest
{
    public string FenBefore { get; set; } = string.Empty;
    public string MovePlayedUci { get; set; } = string.Empty;
    public string FenAfter { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
    public string MoveHistory { get; set; } = string.Empty;
}
