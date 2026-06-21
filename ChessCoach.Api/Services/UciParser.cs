using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChessCoach.Api.Services;

public class UciLine
{
    public int MoveIndex { get; set; }
    public string UciMove { get; set; } = string.Empty;
    public string RawType { get; set; } = string.Empty;
    public int RawValue { get; set; }
    public string PvSequence { get; set; } = string.Empty;
}

public class UciAnalysisResult
{
    public List<UciLine> ParallelLines { get; set; } = new();
}

public static class UciParser
{
    public static UciAnalysisResult Translate(string output)
    {
        var result = new UciAnalysisResult();
        var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("info") && line.Contains("multipv") && line.Contains("score") && line.Contains("pv"))
            {
                var multiPvMatch = Regex.Match(line, @"multipv\s+(\d+)");
                var scoreMatch = Regex.Match(line, @"score\s+(cp|mate)\s+(-?\d+)");
                // Capture the entire rest of the line as the PV sequence, and the first word as the UciMove
                var pvMatch = Regex.Match(line, @"pv\s+(([a-h][1-8][a-h][1-8][qrbn]?)(.*))");

                if (multiPvMatch.Success && scoreMatch.Success && pvMatch.Success)
                {
                    result.ParallelLines.Add(new UciLine
                    {
                        MoveIndex = int.Parse(multiPvMatch.Groups[1].Value),
                        RawType = scoreMatch.Groups[1].Value,
                        RawValue = int.Parse(scoreMatch.Groups[2].Value),
                        PvSequence = pvMatch.Groups[1].Value.Trim(),
                        UciMove = pvMatch.Groups[2].Value.Trim()
                    });
                }
            }
        }

        return result;
    }

    public static string ExtractResultingFen(string output)
    {
        var match = Regex.Match(output, @"Fen:\s+(.+)");
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return string.Empty;
    }
}
