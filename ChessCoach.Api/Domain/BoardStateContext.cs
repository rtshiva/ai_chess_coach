using System;

namespace ChessCoach.Api.Domain;

public record BoardStateContext
{
    public string Fen { get; private init; } = string.Empty;
    public string SideToMove { get; private init; } = string.Empty;

    public static BoardStateContext FromFen(string fen)
    {
        var tokens = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2) throw new ArgumentException("Invalid FEN syntax.");
        return new BoardStateContext { Fen = fen, SideToMove = tokens[1].ToLower() };
    }

    public GameEvaluation NormalizeToWhiteCentric(string type, int value)
    {
        var scoreType = type.Equals("mate", StringComparison.OrdinalIgnoreCase) ? ScoreType.ForcedMate : ScoreType.Centipawn;
        int normalizedValue = value;

        if (scoreType == ScoreType.Centipawn && SideToMove.Equals("b"))
        {
            normalizedValue = -normalizedValue;
        }
        return new GameEvaluation(scoreType, normalizedValue);
    }
}
