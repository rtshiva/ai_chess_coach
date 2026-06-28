using System;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ChessCoach.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChessCoach.Api.Services;

public class ConfigurableLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<ConfigurableLlmClient> _logger;

    public ConfigurableLlmClient(HttpClient httpClient, IOptions<LlmSettings> settings, ILogger<ConfigurableLlmClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateFencedTextAsync(string prompt)
    {
        // If no base URL is provided, return a fallback template (useful for running locally without Ollama running)
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            return "Local Fallback: This is a mistake because you lost material.";
        }

        try
        {
            // Assuming Ollama /api/generate format for this placeholder:
            // {"model": "llama3", "prompt": prompt, "stream": false}
            var payload = new
            {
                model = _settings.ModelName,
                prompt = prompt,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync(_settings.BaseUrl, payload);
            var responseString = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try 
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(responseString);
                    if (json.TryGetProperty("response", out var responseText))
                    {
                        return responseText.GetString() ?? "No response generated.";
                    }
                    _logger.LogWarning("LLM API returned success, but 'response' property was missing. Raw JSON: {RawJson}", responseString);
                    return $"API connection succeeded, but failed to parse response. Check backend console for details.";
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON from LLM API. Raw response: {RawResponse}", responseString);
                    return "API connection succeeded, but returned invalid JSON. Check backend console for details.";
                }
            }
            else
            {
                _logger.LogError("LLM API returned HTTP {StatusCode}. Raw response: {RawResponse}", response.StatusCode, responseString);
                return $"API request failed with HTTP {response.StatusCode}. Check backend console for details.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to LLM API.");
            return $"Failed to connect to LLM API: {ex.Message}";
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamedTextAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            yield return "Local Fallback: This is a mistake because you lost material.";
            yield break;
        }

        yield return $"**[Model: {_settings.ModelName}]**\n\n";

        var payload = new
        {
            model = _settings.ModelName,
            prompt = prompt,
            stream = true,
            think = _settings.EnableThinking
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl)
        {
            Content = JsonContent.Create(payload)
        };

        var sw = Stopwatch.StartNew();
        var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("[{Time}] LLM Client: Sending request to {Url} with model {Model}\nExact JSON Sent to Ollama:\n{JsonPayload}", 
            DateTime.Now.ToString("HH:mm:ss.fff"), _settings.BaseUrl, _settings.ModelName, jsonPayload);
        
        Console.WriteLine("\n--- LLM RESPONSE STREAM START ---");
        HttpResponseMessage? response = null;
        string? errorMessage = null;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to LLM API for streaming.");
            errorMessage = $"Failed to connect to LLM API: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return errorMessage;
            yield break;
        }

        _logger.LogInformation("[{Time}] LLM Client: Received initial HTTP response headers after {Ms} ms.", DateTime.Now.ToString("HH:mm:ss.fff"), sw.ElapsedMilliseconds);

        using var stream = await response!.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        bool firstChunk = true;

        while (!reader.EndOfStream)
        {
            if (ct.IsCancellationRequested) break;
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            string? chunkText = null;
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(line);
                if (json.TryGetProperty("response", out var responseText))
                {
                    chunkText = responseText.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON stream chunk from LLM API: {Line}", line);
            }

            if (chunkText != null)
            {
                if (firstChunk)
                {
                    _logger.LogInformation("\n[{Time}] LLM Client: Yielded FIRST text chunk after {Ms} ms.", DateTime.Now.ToString("HH:mm:ss.fff"), sw.ElapsedMilliseconds);
                    firstChunk = false;
                }
                Console.Write(chunkText);
                yield return chunkText;
            }
        }
        Console.WriteLine("\n--- LLM RESPONSE STREAM END ---");
    }
}
