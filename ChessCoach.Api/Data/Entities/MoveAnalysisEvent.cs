using System;

namespace ChessCoach.Api.Data.Entities;

public class MoveAnalysisEvent
{
    public long EventId { get; set; }
    public int UserId { get; set; }
    public Guid GameId { get; set; }
    public int PlySequenceId { get; set; }
    public string FenBefore { get; set; } = string.Empty;
    public string UserMoveUci { get; set; } = string.Empty;
    public int CentipawnLoss { get; set; }
    public string StructuralQuality { get; set; } = string.Empty;
    public string PrimaryCategory { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string TacticalEngineVersion { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
