using System.Threading.Tasks;

namespace ChessCoach.Api.Services;

public interface ILlmClient
{
    Task<string> GenerateFencedTextAsync(string prompt);
}
