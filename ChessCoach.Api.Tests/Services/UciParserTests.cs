using System;
using System.Linq;
using ChessCoach.Api.Services;
using Xunit;

namespace ChessCoach.Api.Tests.Services;

public class UciParserTests
{
    [Fact]
    public void Translate_ParsesMultiPvOutputCorrectly()
    {
        var output = @"
info depth 16 seldepth 22 multipv 1 score cp 35 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv e2e4 e7e5
info depth 16 seldepth 20 multipv 2 score cp 20 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv d2d4 d7d5
info depth 16 seldepth 20 multipv 3 score mate 3 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv g1f3 d7d5
bestmove e2e4 ponder e7e5";

        var result = UciParser.Translate(output);

        Assert.Equal(3, result.ParallelLines.Count);
        
        var line1 = result.ParallelLines.Single(l => l.MoveIndex == 1);
        Assert.Equal("e2e4", line1.UciMove);
        Assert.Equal("cp", line1.RawType);
        Assert.Equal(35, line1.RawValue);

        var line3 = result.ParallelLines.Single(l => l.MoveIndex == 3);
        Assert.Equal("g1f3", line3.UciMove);
        Assert.Equal("mate", line3.RawType);
        Assert.Equal(3, line3.RawValue);
    }

    [Fact]
    public void ExtractResultingFen_ExtractsFenFromPositionOutput()
    {
        var output = @"
Fen: rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1
Key: 11111111
";
        
        var fen = UciParser.ExtractResultingFen(output);
        
        Assert.Equal("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1", fen);
    }
}
