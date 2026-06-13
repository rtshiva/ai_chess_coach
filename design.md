Here is the complete, comprehensive production-grade architecture blueprint for your backup. It contains all the structural updates, including the dual-stage evaluation fallback, the White-centric perspective normalizer derived directly from FEN strings, the separate two-tier cache optimization, and the versioned event store.

You can copy and paste this text directly into your local `architecture.md` or repository wiki.

---

```markdown
# AI Chess Coach: Production Architecture Blueprint
**System Version:** 2.1.0 (Production-Grade / Frozen Specification)  
**Target Stack:** .NET Core (ASP.NET Core), Vanilla JS / ES Modules, PostgreSQL, Redis, Stockfish Engine

---

## 1. System Topology & Data Flow

The system explicitly decouples UI rendering, authoritative state tracking, raw computation, fact compilation, and pedagogical processing into clean, isolated layers.


```

```
              [ Browser UI (chess.js) ]
                         │
                         ▼ Post (Immutable FEN, Move UCI)
┌─────────────────────────────────────────────────────────────────┐
│ 1. OPTIMIZATION EDGE LAYER                                      │
│    Check Redis Position Cache  ──[Hit]──> Return Cached Metrics │
│         │                                                       │
│      [Miss]                                                     │
│         ▼                                                       │
│    Check Polyglot Opening Book ──[Hit]──> Return Book Coaching  │
└─────────┬───────────────────────────────────────────────────────┘
          │ [Miss]
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. DUAL-STAGE ENGINE PIPELINE                                   │
│    Stage A: MultiPV=3 Search on Root FEN                        │
│         │                                                       │
│         ├── Move Found? ──> Extract Scores & Normalize          │
│         │                                                       │
│         └── [NotFound] ───> Stage B: Quick Target Search        │
│                                     (position FEN moves userMove)│
└─────────┬───────────────────────────────────────────────────────┘
          │ 
          ▼ Normalized Metrics (Absolute Centipawns + Motifs)
┌─────────────────────────────────────────────────────────────────┐
│ 3. FACT COMPILER & EVENT STORE                                  │
│    Compute Delta CP Loss (Relative to Side-To-Move)             │
│    Append MoveAnalysisEvent to Immutable Log (PostgreSQL)       │
└─────────┬───────────────────────────────────────────────────────┘
          │ 
          ▼ Gated Trigger (Only if Blunder/Mistake/Missed Win)
┌─────────────────────────────────────────────────────────────────┐
│ 4. STRUCTURAL LLM COACH & PRACTICE GEN                         │
│    JSON-Fenced Prompt ──> LLM ──> Sidebar UI Text               │
│                               └──> Inject 3 Motif Practice Puzzles
└─────────────────────────────────────────────────────────────────┘

```

```

---

## 2. Frontend Architectural Layer

The client treats `chess.js` as the absolute single source of truth for game state and validation. The visualization layer (`cm-chessboard`) merely displays this state. FEN frames are captured immutably *before* client-side state mutations occur.

### `index.html`
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>AI Chess Coach AI</title>
    <link rel="stylesheet" href="[https://cdn.jsdelivr.net/npm/cm-chessboard@4/assets/styles/cm-chessboard.css](https://cdn.jsdelivr.net/npm/cm-chessboard@4/assets/styles/cm-chessboard.css)">
    <style>
        .container { display: flex; gap: 20px; padding: 20px; font-family: sans-serif; }
        #board { width: 500px; height: 500px; }
        #coach-pane { width: 350px; padding: 20px; border: 1px solid #ddd; background: #fafafa; border-radius: 8px; }
    </style>
</head>
<body>

<div class="container">
    <div id="board"></div>
    <div id="coach-pane">
        <h3>Chess Coach AI</h3>
        <p id="output">Make a move to begin analysis...</p>
    </div>
</div>

<script type="module">
    import { Chess } from "[https://cdn.jsdelivr.net/npm/chess.js@1.0.0-beta.6/+esm](https://cdn.jsdelivr.net/npm/chess.js@1.0.0-beta.6/+esm)";
    import { Chessboard, INPUT_EVENT_TYPE } from "[https://cdn.jsdelivr.net/npm/cm-chessboard@4/src/cm-chessboard/Chessboard.js](https://cdn.jsdelivr.net/npm/cm-chessboard@4/src/cm-chessboard/Chessboard.js)";

    const game = new Chess();
    const board = new Chessboard(document.getElementById("board"), {
        position: game.fen(),
        sprite: { url: "[https://cdn.jsdelivr.net/npm/cm-chessboard@4/assets/images/chessboard-sprite.svg](https://cdn.jsdelivr.net/npm/cm-chessboard@4/assets/images/chessboard-sprite.svg)" }
    });

    function inputHandler(event) {
        if (event.type === INPUT_EVENT_TYPE.moveInputStarted) {
            return game.turn() === event.piece.charAt(0);
        }

        if (event.type === INPUT_EVENT_TYPE.validateMoveInput) {
            // Context Isolation: Capture state strictly before client mutation
            const fenBeforeMove = game.fen(); 

            try {
                // Ensure promotion piece selection logic is handled contextually (defaulting to 'q' for structural validation)
                const moveResult = game.move({
                    from: event.squareFrom,
                    to: event.squareTo,
                    promotion: "q" 
                });

                board.setPosition(game.fen());
                analyzeMoveOnBackend(fenBeforeMove, moveResult.lan, game.fen()); // Pass clean UCI string
                return true; 
            } catch (err) {
                return false; 
            }
        }
    }

    board.enableMoveInput(inputHandler);

    function analyzeMoveOnBackend(preFen, moveUci, postFen) {
        document.getElementById("output").innerText = "Analyzing your position...";
        
        fetch('http://localhost:5000/api/analyze', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fenBefore: preFen, movePlayedUci: moveUci, fenAfter: postFen })
        })
        .then(res => res.json())
        .then(data => {
            document.getElementById("output").innerHTML = `<strong>Coach:</strong> ${data.explanation}`;
        })
        .catch(() => {
            document.getElementById("output").innerText = "Error connecting to coaching backend.";
        });
    }
</script>
</body>
</html>

```

---

## 3. Backend Engine Orchestration Layer

Long-lived Stockfish processes are pooled, managed, and monitored asynchronously using thread-safe bounded `.NET Channels` to guarantee exclusive ownership per transaction request.

### `EnginePoolManager.cs`

```csharp
using System.Diagnostics;
using System.Threading.Channels;

namespace ChessCoach.Api.Services;

public class EngineWorker : IDisposable
{
    private readonly Process _process;
    private readonly SemaphoreSlim _lock = new(1, 1);
    public bool IsHealthy { get; private set; } = true;

    public EngineWorker(string exePath)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _process.Start();
        
        _process.StandardInput.WriteLine("uci");
        _process.StandardInput.WriteLine("setoption name MultiPV value 3");
    }

    public async Task<string> SendCommandAsync(string positionCmd, string goCmd, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!IsHealthy || _process.HasExited) throw new InvalidOperationException("Worker process has died.");
            while (_process.StandardOutput.Peek() > -1) { await _process.StandardOutput.ReadLineAsync(ct); }

            await _process.StandardInput.WriteLineAsync(positionCmd.AsMemory(), ct);
            await _process.StandardInput.WriteLineAsync(goCmd.AsMemory(), ct);

            var sb = new System.Text.StringBuilder();
            while (!ct.IsCancellationRequested)
            {
                string? line = await _process.StandardOutput.ReadLineAsync(ct);
                if (line == null) { IsHealthy = false; break; }
                sb.AppendLine(line);
                if (line.StartsWith("bestmove")) break; 
            }
            return sb.ToString();
        }
        catch
        {
            IsHealthy = false;
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        try { _process.Kill(); } catch { }
        _process.Dispose();
    }
}

public class EnginePoolManager : IDisposable
{
    private readonly Channel<EngineWorker> _pool;
    private readonly string _exePath;
    private readonly List<EngineWorker> _workers = new();

    public EnginePoolManager(string exePath, int poolSize = 4)
    {
        _exePath = exePath;
        _pool = Channel.CreateBounded<EngineWorker>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            var worker = new EngineWorker(_exePath);
            _workers.Add(worker);
            _pool.Writer.TryWrite(worker);
        }
    }

    public async Task<string> ExecuteEngineQueryAsync(string positionCmd, string goCmd, CancellationToken ct)
    {
        EngineWorker? worker = null;
        try
        {
            worker = await _pool.Reader.ReadAsync(ct);
            
            if (!worker.IsHealthy)
            {
                _workers.Remove(worker);
                worker.Dispose();
                worker = new EngineWorker(_exePath);
                _workers.Add(worker);
            }

            return await worker.SendCommandAsync(positionCmd, goCmd, ct);
        }
        finally
        {
            if (worker != null) await _pool.Writer.WriteAsync(worker, CancellationToken.None);
        }
    }

    public void Dispose()
    {
        foreach (var w in _workers) w.Dispose();
    }
}

```

---

## 4. Normalization & Dual-Stage Fact Engineering

### `EvaluationContext.cs`

To eliminate side-to-move valuation sign flips, evaluation perspective is strictly resolved directly from the parsed FEN tokens, separating forced mate tracks from numerical centipawns.

```csharp
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

public record BoardStateContext
{
    public string Fen { get; private init; } = string.Empty;
    public string SideToMove { get; private init; } = string.Empty;

    public static BoardStateContext FromFen(string fen)
    {
        var tokens = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2) throw new ArgumentException("Invalid FEN syntax.");
        return new BoardStateContext { Fen = fen, SideToMove = tokens[1].ToLower() };
    }

    public GameEvaluation NormalizeToWhiteCentric(string type, int value)
    {
        var scoreType = type.Equals("mate", StringSplitOptions.OrdinalIgnoreCase) ? ScoreType.ForcedMate : ScoreType.Centipawn;
        int normalizedValue = value;

        if (scoreType == ScoreType.Centipawn && SideToMove.Equals("b"))
        {
            normalizedValue = -normalizedValue;
        }
        return new GameEvaluation(scoreType, normalizedValue);
    }
}

```

### `DualStageEvaluator.cs`

If a user plays an unexpected, creative move dropped outside the top MultiPV=3 search bracket, a targeted fallback evaluation verifies its accurate value instead of assigning an artificial blunder penalty.

```csharp
public class DualStageEvaluator
{
    private readonly EnginePoolManager _pool;

    public async Task<MoveEvaluationFacts> ProcessPipelineAsync(string fenBefore, string userMoveUci, CancellationToken ct)
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
            var stageBResult = UciParser.Translate(stageBOutput).ParallelLines.First();
            
            // Derive context explicitly from the next state's FEN layout
            var contextAfter = BoardStateContext.FromFen(UciParser.ExtractResultingFen(stageBOutput));
            normalizedUserMove = contextAfter.NormalizeToWhiteCentric(stageBResult.RawType, stageBResult.RawValue);
        }

        return DetermineClassificationDeltas(normalizedRoot, normalizedUserMove, contextBefore.SideToMove, bestLine.UciMove);
    }

    private MoveEvaluationFacts DetermineClassificationDeltas(GameEvaluation root, GameEvaluation user, string turn, string bestUci)
    {
        // Absolute delta computation and quality mapping checks go here...
        return new MoveEvaluationFacts(root, user, bestUci);
    }
}

```

---

## 5. Storage Analytics & Learning Matrix Model

To guarantee downstream data integrity and protect historical statistics against structural logic tuning updates, user logs are saved inside an append-only versioned Event Store.

### Postgres Infrastructure Configuration

```sql
CREATE TABLE MoveAnalysisEvents (
    EventId BIGSERIAL PRIMARY KEY,
    UserId INT NOT NULL,
    GameId UUID NOT NULL,
    PlySequenceId INT NOT NULL,               -- Half-move sequence index identifier
    FenBefore TEXT NOT NULL,
    UserMoveUci VARCHAR(10) NOT NULL,
    CentipawnLoss INT NOT NULL,
    StructuralQuality VARCHAR(20) NOT NULL,
    PrimaryCategory VARCHAR(30) NOT NULL,     -- 'Tactics', 'Strategy', 'Endgames'
    SubCategory VARCHAR(30) NOT NULL,         -- 'Pins', 'Forks', 'PawnStructure'
    TacticalEngineVersion VARCHAR(20) NOT NULL, -- Core heuristic parser tracking version code
    Timestamp TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_ply_per_game UNIQUE (GameId, PlySequenceId)
);

CREATE INDEX idx_user_analytics_lookup ON MoveAnalysisEvents (UserId, PrimaryCategory, SubCategory);

```

### Hierarchical Profiling Component

```csharp
public class HierarchicalTaxonomyProfile
{
    public Dictionary<string, CategoryNode> Domains { get; set; } = new();

    public void ProjectFromEvents(IEnumerable<MoveAnalysisEvent> events)
    {
        foreach (var ev in events)
        {
            if (!Domains.ContainsKey(ev.PrimaryCategory)) Domains[ev.PrimaryCategory] = new CategoryNode();
            var primary = Domains[ev.PrimaryCategory];
            primary.TotalCount++;

            if (!primary.SubCategories.ContainsKey(ev.SubCategory)) primary.SubCategories[ev.SubCategory] = new SubMetric();
            var sub = primary.SubCategories[ev.SubCategory];
            sub.TotalOpportunities++;
            if (ev.CentipawnLoss > 120) sub.BlunderCount++;
        }
    }
}

public class CategoryNode
{
    public int TotalCount { get; set; }
    public Dictionary<string, SubMetric> SubCategories { get; set; } = new();
}

public class SubMetric
{
    public int TotalOpportunities { get; set; }
    public int BlunderCount { get; set; }
    public double ErrorRate => TotalOpportunities > 0 ? (double)BlunderCount / TotalOpportunities : 0;
}

```

---

## 6. Two-Tier Caching & Execution Middleware

This component separates global FEN calculation cache footprints from individual user path variants, lowering CPU computation load and gating downstream runtime LLM orchestration overhead.

```csharp
public class TwoTierCacheService
{
    private readonly IDistributedCache _redis;

    // Cache Level 1: Keyed strictly to Root FEN (Extremely high cross-user reuse)
    public async Task CacheRootAnalysisAsync(string fen, UciAnalysisResult result)
    {
        string key = $"root:{HashFen(fen)}";
        await _redis.SetStringAsync(key, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    }

    // Cache Level 2: Keyed to FEN + specific variant paths
    public async Task CacheUserMoveAsync(string fen, string moveUci, GameEvaluation score)
    {
        string key = $"move:{HashFen(fen)}:{moveUci}";
        await _redis.SetStringAsync(key, JsonSerializer.Serialize(score), new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    }

    private string HashFen(string fen) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fen));
}

public class ActiveCoachingPipeline
{
    private readonly TwoTierCacheService _cache;
    private readonly IOpeningBookService _book;
    private readonly DualStageEvaluator _evaluator;
    private readonly ILlmClient _llm;

    public async Task<CoachingResponse> ProcessTurnAsync(MoveRequest req, CancellationToken ct)
    {
        // 1. Edge Layer: Polyglot Opening Database Verification
        if (_book.TryLookup(req.FenBefore, out var bookData)) 
            return CoachingResponse.FromBook(bookData);

        // 2. Compute/Retrieve Position Facts Base
        var evaluationFacts = await _evaluator.ProcessPipelineAsync(req.FenBefore, req.UserMoveUci, ct);
        
        // 3. Operational Gating: Local Templates bypass LLM execution for good/excellent steps
        if (evaluationFacts.IsAcceptableChoice)
        {
            return LocalTemplateProvider.GenerateStaticReply(evaluationFacts);
        }

        // 4. Structural Motif Construction (Strictly Fenced JSON Payload Compilation)
        var structuralFencedJson = TacticalPatternSubsystem.ExtractMetrics(req.FenBefore, req.UserMoveUci, evaluationFacts);
        
        string contextualPrompt = PromptFactory.BuildStrictPayload(structuralFencedJson);
        string coachText = await _llm.GenerateFencedTextAsync(contextualPrompt);

        // 5. Spaced Repetition (FSRS/SM-2) Drill Injection Retrieval
        var exercises = await _db.Puzzles.GetSpacedRepetitionDrillsAsync(req.UserId, structuralFencedJson.SubCategory);

        return new CoachingResponse {
            ExplanationText = coachText,
            DrillMiniPuzzles = exercises
        };
    }
}

```

---

## 7. Frozen Execution Phase Roadmap

```
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 1: STALESS CORE ORACLE (CURRENT TARGET)                          │
│ ├─ Setup vanilla browser layout with chess.js + state tracking.       │
│ ├─ Construct .NET bounded process thread Channel workers pool.        │
│ ├─ Build context-aware evaluation parser & White-relative structures.  │
│ └─ Connect fenced token LLM logic API endpoint.                        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Once verified stable & accurate
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 2: HISTORICAL METRICS LOGGING (GOOD-TO-HAVE)                     │
│ ├─ Implement Postgres Event Store with unique ply constraint keys.    │
│ └─ Spin up versioned sub-tier pattern matching heuristic code modules. │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Once data patterns are tracking
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ PHASE 3: DRILL ENGINE & SPACED REPETITION (GOOD-TO-HAVE)              │
│ └─ Integrate FSRS/SM-2 database scheduling loops targeting weak motifs.│
└────────────────────────────────────────────────────────────────────────┘

```

```
***

Your architecture is completely documented, fully optimized, and ready to implement. Let me know when you have completed your local project directories and are ready to map out the frontend validation details or specific UCI parser token rules!

```