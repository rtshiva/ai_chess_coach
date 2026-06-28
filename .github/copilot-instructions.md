# Copilot Instructions for AI Chess Coach

This document provides context and instructions for GitHub Copilot to assist in development within this repository.

## Build, Test, and Run Commands

### Backend (.NET 9)
- **Build the entire solution**: `dotnet build`
- **Run the API**: `dotnet run --project ChessCoach.Api\ChessCoach.Api.csproj`
- **Run all tests**: `dotnet test`
- **Run a specific test**: `dotnet test --filter "FullyQualifiedName~DualStageEvaluatorTests"`
- **Run a single test method**: `dotnet test --filter "FullyQualifiedName~ProcessPipelineAsync_StageAHit_ReturnsFactsWithoutFallback"`

### Frontend (Vanilla JS)
- Serve the `frontend/` directory via any static web server (e.g., `npx serve frontend`).

## High-Level Architecture

The project follows an **Engine-to-Fact-to-LLM pipeline** designed for stateless, mathematically accurate coaching.

### 1. Engine Orchestration
A bounded pool of long-lived Stockfish processes is managed via `System.Threading.Channels` in the `EnginePoolManager`. This approach minimizes process startup latency and optimizes resource usage.

### 2. Dual-Stage Evaluation
To balance performance and accuracy, evaluation happens in two stages:
- **Stage A (Primary)**: Analyzes the board before the move using `MultiPV=3`. If the user's move is among the top engine moves, the process terminates early with the calculated centipawn delta.
- **Stage B (Fallback)**: If the user's move is not in the top 3, a targeted analysis of the resulting position is performed to determine exact loss.

### 3. Active Coaching Pipeline & Event Sourcing
The `ActiveCoachingPipeline` implements an event-driven approach. Every analyzed move is logged as an immutable event into a SQLite database (`chesscoach.db`) using Entity Framework Core. The pipeline uses "operational gating" to bypass LLM calls if the move is deemed acceptable, reducing cost and latency.

### 4. LLM Integration
A `ConfigurableLlmClient` constructs structured prompts for models like Gemini or Llama3. These prompts contain precise engine findings (FENs, UCI moves, PV sequences) enclosed in a fenced format, which the LLM then translates into pedagogical feedback.

## Key Conventions

### Process & Concurrency
- **UCI Communication**: When communicating with unmanaged Stockfish processes, use `SemaphoreSlim(1, 1)` to prevent command interlacing and ensure thread safety for individual workers.
- **Worker Distribution**: Use `System.Threading.Channels` (specifically a bounded channel) for distributing work to the engine process pool (`EnginePoolManager`).

### Domain Logic & Data
- **White-Centric Normalization**: All engine scores (Centipawns/Scores) and evaluation results must be normalized to be **White-centric** via `BoardStateContext`. This prevents logic errors caused by sign-flips when the active side changes.
- **Event Sourcing**: Treat move analyses as immutable events stored in SQLite.
- **Structured Prompting**: When generating LLM prompts, always use structured placeholders (e.g., `{userMoveUci}`, `{bestLinePv}`) to ensure consistent parsing and reliability.
