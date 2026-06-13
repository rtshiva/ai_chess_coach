using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChessCoach.Api.Services;

public class EnginePoolManager : IEnginePoolManager, IDisposable
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
