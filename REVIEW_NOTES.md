# Review Notes

## Architecture Assessment
The system follows a clear **Fact-Gated LLM Pipeline**, successfully isolating core engine logic from the presentation layer. 

**Strengths:**
*   **Engine Management**: The `EnginePoolManager` uses `System.Threading.Channels` effectively to maintain a warmed pool of Stockfish processes, minimizing latency and overhead.
*   **Fact Engineering**: High modularity in `DualStageEvaluator` and `UciParser` ensures the LLM receives clean, structured data rather than raw, noisy output.

## Identified Issues & Risks

### 1. Thread Safety (High Priority)
- **Location**: `ChessCoach.Api/Services/EnginePoolManager.cs`
- **Issue**: The `List<EngineWorker>` is not thread-safe. Concurrent requests resolving "unhealthy" statuses may cause race conditions during the list modification phase (`Remove`/`Add`).
- **Recommendation**: Replace with a concurrent collection or remove the redundant list if only used for disposal logic.

### 2. Input Validation (Medium Priority)
- **Location**: `ChessCoach.Api/Controllers/AnalysisController.cs`
- **Issue**: Lack of strict validation on FEN strings and UCI moves before they reach the processing pipeline.
- **Recommendation**: Implement a verification step using a chess library to ensure valid state transitions before consuming engine resources.

### 3. Prompt Injection (Medium Priority)
- **Location**: `ChessCoach.Api/Services/ConfigurableLlmClient.cs`
- **Issue**: The `PromptTemplate` is passed directly to the LLM without sanitization, potentially allowing users to override system instructions.
- **Recommendation**: Sanitize user inputs or use a more rigid system prompt to confine the LLM's scope.

### 4. Resource Management (Low/Medium Priority)
- **Location**: `ChessCoach.Api/Services/EngineWorker.cs`
- **Issue**: Absence of explicit execution timeouts for Stockfish commands could result in "stuck" workers during deep calculations or engine hangs.
- **Recommendation**: Implement a `CancellationToken` with a hard timeout on the extraction step to ensure pool continuity.
