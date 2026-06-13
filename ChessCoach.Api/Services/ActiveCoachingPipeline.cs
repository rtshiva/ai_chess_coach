using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Domain;

namespace ChessCoach.Api.Services;

public class ActiveCoachingPipeline
{
    private readonly DualStageEvaluator _evaluator;
    private readonly ILlmClient _llm;

    public ActiveCoachingPipeline(DualStageEvaluator evaluator, ILlmClient llm)
    {
        _evaluator = evaluator;
        _llm = llm;
    }

    public async Task<CoachingResponse> ProcessTurnAsync(string fenBefore, string userMoveUci, string fenAfter, CancellationToken ct)
    {
        // 1. Compute/Retrieve Position Facts Base
        var evaluationFacts = await _evaluator.ProcessPipelineAsync(fenBefore, userMoveUci, fenAfter, ct);
        
        // 2. Operational Gating: Local Templates bypass LLM execution for good/excellent steps
        if (evaluationFacts.IsAcceptableChoice)
        {
            return new CoachingResponse {
                ExplanationText = $"Good move. The engine preferred {evaluationFacts.BestUciMove}. You played {userMoveUci} with only {evaluationFacts.CentipawnLoss} centipawn loss."
            };
        }

        // 3. Structural Motif Construction (Strictly Fenced Prompt Construction)
        string contextualPrompt = $"You are an expert chess coach. The user played {userMoveUci} from FEN {fenBefore}. This was a mistake resulting in a {evaluationFacts.CentipawnLoss} centipawn loss compared to the best move {evaluationFacts.BestUciMove}. Provide a brief, encouraging 1-sentence explanation of why they lost material or position.";
        
        // 4. Hit the LLM
        string coachText = await _llm.GenerateFencedTextAsync(contextualPrompt);

        return new CoachingResponse {
            ExplanationText = coachText
        };
    }
}
