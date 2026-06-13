using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Domain;
using ChessCoach.Api.Services;
using Moq;
using Xunit;

namespace ChessCoach.Api.Tests.Services;

public class ActiveCoachingPipelineTests
{
    [Fact]
    public async Task ProcessTurnAsync_AcceptableMove_ShortCircuitsLlm()
    {
        var mockPool = new Mock<IEnginePoolManager>();
        var evaluator = new DualStageEvaluator(mockPool.Object);
        var mockLlm = new Mock<ILlmClient>();

        var pipeline = new ActiveCoachingPipeline(evaluator, mockLlm.Object);

        // Mock Stage A output: user plays e2e4 (best move) so centipawn loss is 0
        var rootFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var stageAOutput = @"
info depth 16 multipv 1 score cp 35 pv e2e4
bestmove e2e4";

        mockPool.Setup(p => p.ExecuteEngineQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageAOutput);

        var fenAfter = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
        var response = await pipeline.ProcessTurnAsync(rootFen, "e2e4", fenAfter, CancellationToken.None);

        Assert.Contains("Good move", response.ExplanationText);
        mockLlm.Verify(l => l.GenerateFencedTextAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTurnAsync_MistakeMove_InvokesLlm()
    {
        var mockPool = new Mock<IEnginePoolManager>();
        var evaluator = new DualStageEvaluator(mockPool.Object);
        var mockLlm = new Mock<ILlmClient>();

        var pipeline = new ActiveCoachingPipeline(evaluator, mockLlm.Object);

        // Mock Stage A output without h2h4
        var rootFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var stageAOutput = @"
info depth 16 multipv 1 score cp 35 pv e2e4
bestmove e2e4";

        // Mock Stage B output for h2h4
        var stageBOutput = @"
info depth 14 multipv 1 score cp 100 pv e7e5
bestmove e7e5
Fen: rnbqkbnr/pppppppp/8/8/7P/8/PPPPPPP1/RNBQKBNR b KQkq h3 0 1";

        mockPool.Setup(p => p.ExecuteEngineQueryAsync($"position fen {rootFen}", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageAOutput);

        mockPool.Setup(p => p.ExecuteEngineQueryAsync(It.Is<string>(s => s.Contains("moves h2h4")), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stageBOutput);

        mockLlm.Setup(l => l.GenerateFencedTextAsync(It.IsAny<string>()))
            .ReturnsAsync("LLM Analysis: You lost center control.");

        var fenAfter = "rnbqkbnr/pppppppp/8/8/7P/8/PPPPPPP1/RNBQKBNR b KQkq h3 0 1";
        var response = await pipeline.ProcessTurnAsync(rootFen, "h2h4", fenAfter, CancellationToken.None);

        Assert.Equal("LLM Analysis: You lost center control.", response.ExplanationText);
        mockLlm.Verify(l => l.GenerateFencedTextAsync(It.IsAny<string>()), Times.Once);
    }
}
