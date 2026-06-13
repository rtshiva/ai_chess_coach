using System;
using ChessCoach.Api.Domain;
using Xunit;

namespace ChessCoach.Api.Tests.Domain;

public class BoardStateContextTests
{
    [Fact]
    public void FromFen_ValidFen_ParsesCorrectly()
    {
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var context = BoardStateContext.FromFen(fen);
        
        Assert.Equal(fen, context.Fen);
        Assert.Equal("w", context.SideToMove);
    }

    [Fact]
    public void FromFen_InvalidFen_ThrowsArgumentException()
    {
        var fen = "invalid_fen";
        
        Assert.Throws<ArgumentException>(() => BoardStateContext.FromFen(fen));
    }

    [Theory]
    [InlineData("cp", 100, "w", ScoreType.Centipawn, 100)]
    [InlineData("cp", 100, "b", ScoreType.Centipawn, -100)]
    [InlineData("cp", -50, "w", ScoreType.Centipawn, -50)]
    [InlineData("cp", -50, "b", ScoreType.Centipawn, 50)]
    [InlineData("mate", 3, "w", ScoreType.ForcedMate, 3)]
    [InlineData("mate", 3, "b", ScoreType.ForcedMate, 3)]
    public void NormalizeToWhiteCentric_NormalizesCorrectly(string type, int value, string sideToMove, ScoreType expectedType, int expectedValue)
    {
        var fen = $"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR {sideToMove} KQkq - 0 1";
        var context = BoardStateContext.FromFen(fen);
        
        var result = context.NormalizeToWhiteCentric(type, value);
        
        Assert.Equal(expectedType, result.Type);
        Assert.Equal(expectedValue, result.Value);
    }
}
