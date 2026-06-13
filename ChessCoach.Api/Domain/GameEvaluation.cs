using System;

namespace ChessCoach.Api.Domain;

public enum ScoreType { Centipawn, ForcedMate }

public record GameEvaluation(ScoreType Type, int Value) : IComparable<GameEvaluation>
{
    public int CompareTo(GameEvaluation? other)
    {
        if (other == null) return 1;
        if (this.Type == ScoreType.ForcedMate && other.Type == ScoreType.Centipawn) return this.Value > 0 ? 1 : -1;
        if (this.Type == ScoreType.Centipawn && other.Type == ScoreType.ForcedMate) return other.Value > 0 ? -1 : 1;
        
        if (this.Type == ScoreType.ForcedMate && other.Type == ScoreType.ForcedMate)
        {
            // Fewer moves to mate is a stronger position. Sign represents perspective.
            return other.Value.CompareTo(this.Value);
        }
        return this.Value.CompareTo(other.Value);
    }
}
