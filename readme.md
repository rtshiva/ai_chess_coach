# AI Chess Coach: Production-Grade Tactical Oracle



An architecture-focused, stateless engine designed to provide immediate, mathematically accurate feedback on chess blunders and mistakes. This tool helps players bridge the gap between "this move felt wrong" and "this is exactly why it lost the game."



## Overview



Unlike many "coaching" AIs that provide vague feedback, this system leverages a strict **Engine-to-Fact-to-LLM pipeline**. By isolating board state and engine analysis from the creative text-generation layer, it delivers precise, logical explanations based on structural motifs (pins, forks, back-rank weaknesses) rather than hallucinatory advice.



### Core Philosophy

*   **Mathematical Truth:** We calculate loss in Centipawns (CP) and Forced Mates, never guessing.

*   **Context-Aware:** Our system derives the active side's perspective directly from FEN strings, eliminating fragile sign-flip bugs.

*   **Stateless by Design:** The core analysis loop is intentionally stateless, making it extremely scalable and low-latency.

*   **Fact-Gated LLM:** The LLM does not "guess" why a move is bad. It receives a structured, fenced JSON payload of the engine's findings and translates those immutable facts into human-readable advice.



## Project Structure (Phase 1 Focus)



This project is built using a modular, phase-gated architecture:



1.  **Frontend:** A clean, minimal `chess.js` and `cm-chessboard` interface designed for high-frequency move input and immediate feedback rendering.

2.  **Engine Orchestration:** A .NET Core service utilizing `System.Threading.Channels` to manage a bounded pool of long-lived Stockfish processes, ensuring high-performance analysis without process startup overhead.

3.  **Fact Engineering:** A custom C# normalization layer that bridges Stockfish's raw UCI output into reliable `GameEvaluation` metrics.



## Roadmap



This project follows a strict development trajectory to ensure stability and accuracy before adding complexity:



*   **Phase 1 (Completed):** Establish the core "Stateless Oracle"—engine integration, dual-stage evaluation logic, and Server-Sent Events (SSE) streaming for real-time coaching via local LLM prompts.

*   **Phase 2 (Completed):** Implementation of an immutable Event Store (SQLite/Entity Framework Core) to securely log historical evaluations, move accuracy, and generated coach feedback.

*   **Phase 3 (Next Steps):** Integration of Spaced Repetition (FSRS/SM-2) for curated drill delivery based on historical user weaknesses.



## Tech Stack

*   **Backend:** .NET 9 Web API

*   **Frontend:** Vanilla JS (ES Modules), `chess.js`

*   **Engine:** Stockfish 17 (MultiPV=3)

*   **LLM Integration:** Ollama (Local Models, e.g., Qwen3.5, Gemma4) via Streaming API

*   **Data/State:** SQLite (Entity Framework Core)



---



### Acknowledgments



This project architecture was developed with the assistance of **Gemini**, a large language model developed by Google. Gemini acted as a lead system collaborator, helping to define the structural boundaries, resolve complex engine edge cases (such as the Horizon Effect and UCI normalization), and establish the implementation roadmap.



---



### Getting Started

1.  **Install dependencies:** 
    *   Ensure you have the .NET 9 SDK installed.
    *   Download the latest Stockfish binary.
    *   Install [Ollama](https://ollama.com/) and run `ollama run qwen3.5:9b` (or your preferred local model).

2.  **Configuration:** Open `ChessCoach.Api/appsettings.json` and update the `"StockfishPath"` value to point to your downloaded Stockfish executable.

3.  **Run Backend:** Navigate to the `ChessCoach.Api` directory and run `dotnet run`. This will automatically boot the server and create the SQLite database.

4.  **Run Frontend:** Open `frontend/index.html` in any modern web browser to start playing against the Coach!



---

*Developed for educational purposes in the pursuit of chess mastery.*

