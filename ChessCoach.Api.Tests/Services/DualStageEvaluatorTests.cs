using System;
using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Domain;
using ChessCoach.Api.Services;
using Moq;
using Xunit;

namespace ChessCoach.Api.Tests.Services;

public class DualStageEvaluatorTests
{
    [Fact]
    public async Task ProcessPipelineAsync_StageAHit_ReturnsFactsWithoutFallback()
    {
        var mockPool = new Mock<IEnginePoolManager>();
        var evaluator = new DualStageEvaluator(mockPool.Object);
        
        var rootFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var userMoveUci = "e2e4";
        var ct = CancellationToken.None;

        // Mock Stage A output containing the user move (e2e4 is in the MultiPV results)
        var stageAOutput = @"
info depth 16 seldepth 22 multipv 1 score cp 35 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv e2e4 e7e5
info depth 16 seldepth 20 multipv 2 score cp 20 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv d2d4 d7d5
info depth 16 seldepth 20 multipv 3 score cp 15 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv g1f3 d7d5
bestmove e2e4 ponder e7e5";

        mockPool.Setup(p => p.ExecuteEngineQueryAsync($"position fen {rootFen}", "go depth 16 movetime 800", ct))
            .ReturnsAsync(stageAOutput);

        var fenAfter = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
        var facts = await evaluator.ProcessPipelineAsync(rootFen, userMoveUci, fenAfter, ct);

        Assert.Equal("e2e4", facts.BestUciMove);
        Assert.Equal(ScoreType.Centipawn, facts.RootEvaluation.Type);
        Assert.Equal(35, facts.RootEvaluation.Value);
        Assert.Equal(ScoreType.Centipawn, facts.UserEvaluation.Type);
        Assert.Equal(35, facts.UserEvaluation.Value);
        Assert.Equal(0, facts.CentipawnLoss);
        Assert.True(facts.IsAcceptableChoice);
        
        // Verify Stage B was NEVER called
        mockPool.Verify(p => p.ExecuteEngineQueryAsync(It.Is<string>(s => s.Contains("moves")), It.IsAny<string>(), ct), Times.Never);
    }

    [Fact]
    public async Task ProcessPipelineAsync_StageBMiss_InvokesFallbackAndComputesLoss()
    {
        var mockPool = new Mock<IEnginePoolManager>();
        var evaluator = new DualStageEvaluator(mockPool.Object);
        
        var rootFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var userMoveUci = "h2h4"; // A bad move not in top 3
        var ct = CancellationToken.None;

        // Mock Stage A output missing h2h4
        var stageAOutput = @"
info depth 16 seldepth 22 multipv 1 score cp 35 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv e2e4 e7e5
info depth 16 seldepth 20 multipv 2 score cp 20 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv d2d4 d7d5
info depth 16 seldepth 20 multipv 3 score cp 15 nodes 248740 nps 1184476 hashfull 30 tbhits 0 time 210 pv g1f3 d7d5
bestmove e2e4 ponder e7e5";

        mockPool.Setup(p => p.ExecuteEngineQueryAsync($"position fen {rootFen}", "go depth 16 movetime 800", ct))
            .ReturnsAsync(stageAOutput);

        // Mock Stage B output for the fallback
        var stageBOutput = @"
info depth 14 seldepth 18 multipv 1 score cp -40 nodes 100000 pv e7e5
bestmove e7e5
Fen: rnbqkbnr/pppppppp/8/8/7P/8/PPPPPPP1/RNBQKBNR b KQkq h3 0 1
Key: 22222222";

        mockPool.Setup(p => p.ExecuteEngineQueryAsync($"position fen {rootFen} moves {userMoveUci}", "go depth 14 movetime 200", ct))
            .ReturnsAsync(stageBOutput);

        var fenAfter = "rnbqkbnr/pppppppp/8/8/7P/8/PPPPPPP1/RNBQKBNR b KQkq h3 0 1";
        var facts = await evaluator.ProcessPipelineAsync(rootFen, userMoveUci, fenAfter, ct);

        Assert.Equal("e2e4", facts.BestUciMove);
        Assert.Equal(ScoreType.Centipawn, facts.RootEvaluation.Type);
        Assert.Equal(35, facts.RootEvaluation.Value); // White perspective +35
        
        Assert.Equal(ScoreType.Centipawn, facts.UserEvaluation.Type);
        // In Stage B, it's Black's turn and score is -40 for Black, which means White is +40. Wait, Black is -40 means Black is losing by 40 cp. So White is +40.
        // Actually, our Normalizer rule for Black's turn is to negate Centipawn. So -(-40) = +40.
        Assert.Equal(40, facts.UserEvaluation.Value);
        
        Assert.Equal(5, facts.CentipawnLoss); // |35 - 40| = 5
        Assert.True(facts.IsAcceptableChoice);
        
        // Verify Stage B WAS called
        mockPool.Verify(p => p.ExecuteEngineQueryAsync($"position fen {rootFen} moves {userMoveUci}", "go depth 14 movetime 200", ct), Times.Once);
    }
}
