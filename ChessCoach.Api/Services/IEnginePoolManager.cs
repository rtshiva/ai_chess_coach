using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChessCoach.Api.Services;

public interface IEnginePoolManager
{
    Task<string> ExecuteEngineQueryAsync(string positionCmd, string goCmd, CancellationToken ct);
}
