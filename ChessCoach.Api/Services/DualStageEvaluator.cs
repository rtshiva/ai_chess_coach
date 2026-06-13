using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Domain;

namespace ChessCoach.Api.Services;

public class DualStageEvaluator
{
    private readonly IEnginePoolManager _pool;

    public DualStageEvaluator(IEnginePoolManager pool)
    {
        _pool = pool;
    }

    public async Task<MoveEvaluationFacts> ProcessPipelineAsync(string fenBefore, string userMoveUci, string fenAfter, CancellationToken ct)
    {
        var contextBefore = BoardStateContext.FromFen(fenBefore);
        
        // Stage A: MultiPV=3 Primary Pass on Root Position
        string stageAOutput = await _pool.ExecuteEngineQueryAsync($"position fen {fenBefore}", "go depth 16 movetime 800", ct);
        var stageAResult = UciParser.Translate(stageAOutput);
        
        var bestLine = stageAResult.ParallelLines.OrderBy(l => l.MoveIndex).First();
        var normalizedRoot = contextBefore.NormalizeToWhiteCentric(bestLine.RawType, bestLine.RawValue);
        
        var matchedLine = stageAResult.ParallelLines.FirstOrDefault(l => l.UciMove == userMoveUci);
        GameEvaluation normalizedUserMove;

        if (matchedLine != null)
        {
            normalizedUserMove = contextBefore.NormalizeToWhiteCentric(matchedLine.RawType, matchedLine.RawValue);
        }
        else
        {
            // Stage B: Targeted fallback confirmation on the resulting line
            string stageBOutput = await _pool.ExecuteEngineQueryAsync($"position fen {fenBefore} moves {userMoveUci}", "go depth 14 movetime 200", ct);
            var stageBResult = UciParser.Translate(stageBOutput).ParallelLines.OrderBy(l => l.MoveIndex).First();
            
            // Derive context explicitly from the next state's FEN layout from frontend
            var contextAfter = BoardStateContext.FromFen(fenAfter);
            normalizedUserMove = contextAfter.NormalizeToWhiteCentric(stageBResult.RawType, stageBResult.RawValue);
        }

        return DetermineClassificationDeltas(normalizedRoot, normalizedUserMove, contextBefore.SideToMove, bestLine.UciMove);
    }

    private MoveEvaluationFacts DetermineClassificationDeltas(GameEvaluation root, GameEvaluation user, string turn, string bestUci)
    {
        return new MoveEvaluationFacts(root, user, bestUci);
    }
}
