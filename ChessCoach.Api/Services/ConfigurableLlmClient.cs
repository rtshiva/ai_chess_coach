using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ChessCoach.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ChessCoach.Api.Services;

public class ConfigurableLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;

    public ConfigurableLlmClient(HttpClient httpClient, IOptions<LlmSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
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
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("response", out var responseText))
                {
                    return responseText.GetString() ?? "No response generated.";
                }
            }
            return "API connection succeeded, but failed to parse response.";
        }
        catch (Exception ex)
        {
            return $"Failed to connect to LLM API: {ex.Message}";
        }
    }
}
