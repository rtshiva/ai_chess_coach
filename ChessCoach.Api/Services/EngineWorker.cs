using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
        _process.StandardInput.WriteLine("isready");
        
        while (true)
        {
            var line = _process.StandardOutput.ReadLine();
            if (line == null || line == "readyok") break;
        }
    }

    public async Task<string> SendCommandAsync(string positionCmd, string goCmd, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!IsHealthy || _process.HasExited) throw new InvalidOperationException("Worker process has died.");

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
