using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Data;
using ChessCoach.Api.Data.Entities;
using ChessCoach.Api.Domain;

namespace ChessCoach.Api.Services;

public class ActiveCoachingPipeline
{
    private readonly DualStageEvaluator _evaluator;
    private readonly ILlmClient _llm;
    private readonly ChessCoachDbContext _dbContext;

    public ActiveCoachingPipeline(DualStageEvaluator evaluator, ILlmClient llm, ChessCoachDbContext dbContext)
    {
        _evaluator = evaluator;
        _llm = llm;
        _dbContext = dbContext;
    }

    public async Task<CoachingResponse> ProcessTurnAsync(string fenBefore, string userMoveUci, string fenAfter, string moveHistory, string promptTemplate, CancellationToken ct)
    {
        // 1. Compute/Retrieve Position Facts Base
        var evaluationFacts = await _evaluator.ProcessPipelineAsync(fenBefore, userMoveUci, fenAfter, ct);
        
        // 2. Persist to Event Store
        var analysisEvent = new MoveAnalysisEvent
        {
            UserId = 1, // Default user for now
            GameId = Guid.NewGuid(), // Default game for now
            PlySequenceId = 1, // Default ply
            FenBefore = fenBefore,
            UserMoveUci = userMoveUci,
            CentipawnLoss = evaluationFacts.CentipawnLoss,
            StructuralQuality = evaluationFacts.IsAcceptableChoice ? "Good" : "Blunder",
            PrimaryCategory = "General", // Placeholder until heuristic engine is built
            SubCategory = "General",     // Placeholder until heuristic engine is built
            TacticalEngineVersion = "v1"
        };
        
        _dbContext.MoveAnalysisEvents.Add(analysisEvent);
        await _dbContext.SaveChangesAsync(ct);

        // 3. (REMOVED) Operational Gating: We now forward ALL moves to the LLM for commentary

        // 4. Structural Motif Construction (Strictly Fenced Prompt Construction)
        string contextualPrompt;
        if (!string.IsNullOrWhiteSpace(promptTemplate))
        {
            contextualPrompt = promptTemplate
                .Replace("{userMoveUci}", userMoveUci)
                .Replace("{fenBefore}", fenBefore)
                .Replace("{bestUciMove}", evaluationFacts.BestUciMove)
                .Replace("{centipawnLoss}", evaluationFacts.CentipawnLoss.ToString())
                .Replace("{bestLinePv}", evaluationFacts.BestLinePvSequence)
                .Replace("{userLinePv}", evaluationFacts.UserLinePvSequence)
                .Replace("{moveHistory}", moveHistory);
        }
        else
        {
            contextualPrompt = $"You are an expert chess coach. The user played {userMoveUci} from FEN {fenBefore}. This was a mistake resulting in a {evaluationFacts.CentipawnLoss} centipawn loss compared to the best move {evaluationFacts.BestUciMove}. The engine expected the continuation for the best move to be {evaluationFacts.BestLinePvSequence}, but your move leads to {evaluationFacts.UserLinePvSequence}. Provide a brief, encouraging 1-sentence explanation of why they lost material or position.";
        }
        
        // 5. Hit the LLM
        string coachText = await _llm.GenerateFencedTextAsync(contextualPrompt);

        return new CoachingResponse {
            ExplanationText = coachText
        };
    }

    public async IAsyncEnumerable<string> ProcessTurnStreamAsync(string fenBefore, string userMoveUci, string fenAfter, string moveHistory, string promptTemplate, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. Compute/Retrieve Position Facts Base
        var evaluationFacts = await _evaluator.ProcessPipelineAsync(fenBefore, userMoveUci, fenAfter, ct);
        
        // 2. Persist to Event Store
        var analysisEvent = new MoveAnalysisEvent
        {
            UserId = 1,
            GameId = Guid.NewGuid(),
            PlySequenceId = 1,
            FenBefore = fenBefore,
            UserMoveUci = userMoveUci,
            CentipawnLoss = evaluationFacts.CentipawnLoss,
            StructuralQuality = evaluationFacts.IsAcceptableChoice ? "Good" : "Blunder",
            PrimaryCategory = "General",
            SubCategory = "General",
            TacticalEngineVersion = "v1"
        };
        
        _dbContext.MoveAnalysisEvents.Add(analysisEvent);
        await _dbContext.SaveChangesAsync(ct);

        // 3. Yield metadata
        yield return $"[METADATA] {{ \"bestUciMove\": \"{evaluationFacts.BestUciMove}\" }}";


        // 3. Structural Motif Construction
        string contextualPrompt;
        if (!string.IsNullOrWhiteSpace(promptTemplate))
        {
            contextualPrompt = promptTemplate
                .Replace("{userMoveUci}", userMoveUci)
                .Replace("{fenBefore}", fenBefore)
                .Replace("{bestUciMove}", evaluationFacts.BestUciMove)
                .Replace("{centipawnLoss}", evaluationFacts.CentipawnLoss.ToString())
                .Replace("{bestLinePv}", evaluationFacts.BestLinePvSequence)
                .Replace("{userLinePv}", evaluationFacts.UserLinePvSequence)
                .Replace("{moveHistory}", moveHistory);
        }
        else
        {
            contextualPrompt = $"You are an expert chess coach. The user played {userMoveUci} from FEN {fenBefore}. This was a mistake resulting in a {evaluationFacts.CentipawnLoss} centipawn loss compared to the best move {evaluationFacts.BestUciMove}. The engine expected the continuation for the best move to be {evaluationFacts.BestLinePvSequence}, but your move leads to {evaluationFacts.UserLinePvSequence}. Provide a brief, encouraging 1-sentence explanation of why they lost material or position.";
        }
        
        // 4. Hit the LLM with streaming
        await foreach (var chunk in _llm.GenerateStreamedTextAsync(contextualPrompt, ct))
        {
            yield return chunk;
        }
    }
}
