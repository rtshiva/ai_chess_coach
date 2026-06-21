using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
}
