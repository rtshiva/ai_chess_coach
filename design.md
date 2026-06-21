# Chess Tutor: Intelligent Pedagogical Agent Architecture

## 1. System Objective

The Chess Tutor aims to act as a proactive, encouraging coach that intercepts user moves, queries a high-strength chess engine (Stockfish 17), evaluates the quality of the move, and utilizes a Large Language Model (Gemini / LLaMA3) to provide structural, pedagogical feedback *only when necessary*.

## 2. Core Architecture

### 2.1 Web Frontend (`frontend/index.html`)
- Pure HTML/CSS/JS interface using `chessboardjs` and `chess.js`.
- Validates legality locally before allowing moves.
- Sends the state `FenBefore`, `MovePlayedUci`, `FenAfter`, and the user's `PromptTemplate` to the backend.
- Displays visual feedback (spinner during API calls) and the coach's textual response.

### 2.2 Orchestration API (`ChessCoach.Api`)
- **Target**: ASP.NET Core 9.0
- **Controller (`AnalysisController.cs`)**: Receives REST POST requests containing the FEN and the move. Delegates directly to the `ActiveCoachingPipeline`.

### 2.3 Deep Engine Integration (`EngineWorker.cs` / `EnginePoolManager.cs` / `UciParser.cs`)
- Wraps an unmanaged Stockfish 17 process.
- Communicates via UCI (Universal Chess Interface) using `SemaphoreSlim(1,1)` to prevent command interlacing.
- **UciParser**: Specifically parses Stockfish output to find `multipv`, `score`, and extracts the entire `pv` (Principal Variation) sequence as a string (`PvSequence`). This PV extraction is critical, as it allows the downstream LLM to see exactly what future moves the engine anticipated.

### 2.4 The Dual Stage Evaluator (`DualStageEvaluator.cs`)
- Determines if the user's move is good, okay, or a blunder.
- **Stage A (Root Analysis)**: Analyzes the board *before* the move using `MultiPV=3`. If the user's move is among the top 3 engine moves, it calculates the centipawn delta immediately and short-circuits.
- **Stage B (Fallback Analysis)**: If the move wasn't in the top 3, the evaluator asks the engine to evaluate the board *after* the user's move explicitly to determine the exact evaluation drop (Centipawn Loss).
- **Output**: Returns a `MoveEvaluationFacts` object containing `BestUciMove`, `CentipawnLoss`, and the critical `BestLinePvSequence` and `UserLinePvSequence` strings.

### 2.5 Active Coaching Pipeline (`ActiveCoachingPipeline.cs`)
- **Event Sourcing**: Instantly logs every analyzed move to a SQLite database (`chesscoach.db`) upon execution, capturing the FEN, move, loss, and tactical parameters.
- **Operational Gating**: If the user's move is acceptable (`IsAcceptableChoice == true`), the pipeline completely bypasses the LLM and returns a hardcoded generic praise response (saving tokens and latency).
- **Dynamic Prompting**: If the move is a blunder, it constructs a prompt for the LLM. It consumes a `PromptTemplate` provided by the frontend, replacing the following placeholders:
  - `{userMoveUci}`: The move the user played.
  - `{fenBefore}`: The board state.
  - `{bestUciMove}`: The engine's preferred first move.
  - `{centipawnLoss}`: The numeric mistake value.
  - `{bestLinePv}`: The sequence of future moves the engine expected.
  - `{userLinePv}`: The sequence of future moves the engine expects because of the blunder.
- If no template is provided, it uses a generic pedagogical fallback.

### 2.6 LLM Integration (`ConfigurableLlmClient.cs`)
- Generic HTTP client that sends the constructed prompt string to an LLM provider.
- Configured via `appsettings.json` (e.g. Ollama locally, or external Gemini endpoint).

### 2.7 Storage & Analytics (`ChessCoachDbContext.cs` / `HierarchicalTaxonomyProfile.cs`)
- **Database**: SQLite using Entity Framework Core 9.0.
- **Entity**: `MoveAnalysisEvent` logs raw, immutable facts about every move the user makes.
- **Domain Projection**: `HierarchicalTaxonomyProfile` projects the flat event log into a hierarchical understanding of the user's skills (e.g., categorizing blunders into Domains like `Tactics -> Forks` or `Strategy -> PawnStructure`) to compute blunder counts and Error Rates.