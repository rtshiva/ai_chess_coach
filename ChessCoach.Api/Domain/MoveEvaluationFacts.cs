using System;

namespace ChessCoach.Api.Domain;

public class MoveEvaluationFacts
{
    public GameEvaluation RootEvaluation { get; }
    public GameEvaluation UserEvaluation { get; }
    public string BestUciMove { get; }
    public string BestLinePvSequence { get; }
    public string UserLinePvSequence { get; }
    public int CentipawnLoss { get; }
    public bool IsAcceptableChoice { get; }
    
    public MoveEvaluationFacts(GameEvaluation root, GameEvaluation user, string bestUciMove, string bestLinePvSequence, string userLinePvSequence)
    {
        RootEvaluation = root;
        UserEvaluation = user;
        BestUciMove = bestUciMove;
        BestLinePvSequence = bestLinePvSequence;
        UserLinePvSequence = userLinePvSequence;
        
        // In a true implementation, dealing with Forced Mate subtraction requires complex logic.
        // For Phase 1 stateless oracle, we simplify CentipawnLoss if both are CP scores.
        if (root.Type == ScoreType.Centipawn && user.Type == ScoreType.Centipawn)
        {
            // Root is from the perspective of White, and so is User Evaluation.
            // But we want CentipawnLoss to be a positive penalty relative to whoever moved.
            CentipawnLoss = Math.Abs(root.Value - user.Value);
        }
        else
        {
            CentipawnLoss = 0; // Fallback for mate evaluations in Phase 1
        }

        IsAcceptableChoice = CentipawnLoss <= 30; // Within 0.3 pawns is acceptable
    }
}
