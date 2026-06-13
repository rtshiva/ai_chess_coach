using System.Collections.Generic;

namespace ChessCoach.Api.Domain;

public class CoachingResponse
{
    public string ExplanationText { get; set; } = string.Empty;
    public List<string> DrillMiniPuzzles { get; set; } = new();
}
