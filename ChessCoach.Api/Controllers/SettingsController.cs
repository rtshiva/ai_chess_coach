using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChessCoach.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly string _settingsPath = "appsettings.json";

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        if (!System.IO.File.Exists(_settingsPath))
        {
            return NotFound("appsettings.json not found.");
        }

        var json = await System.IO.File.ReadAllTextAsync(_settingsPath);
        return Content(json, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSettings([FromBody] JsonObject newSettings)
    {
        if (!System.IO.File.Exists(_settingsPath))
        {
            return NotFound("appsettings.json not found.");
        }

        var json = await System.IO.File.ReadAllTextAsync(_settingsPath);
        var currentSettings = JsonNode.Parse(json)!.AsObject();

        // Update fields if provided
        if (newSettings.TryGetPropertyValue("StockfishPath", out var stockfishPathNode))
        {
            currentSettings["StockfishPath"] = stockfishPathNode?.GetValue<string>();
        }

        if (newSettings.TryGetPropertyValue("LlmSettings", out var llmSettingsNode) && llmSettingsNode is JsonObject llmSettings)
        {
            if (!currentSettings.ContainsKey("LlmSettings"))
            {
                currentSettings["LlmSettings"] = new JsonObject();
            }
            var currentLlm = currentSettings["LlmSettings"]!.AsObject();
            
            if (llmSettings.TryGetPropertyValue("BaseUrl", out var baseUrlNode))
                currentLlm["BaseUrl"] = baseUrlNode?.GetValue<string>();

            if (llmSettings.TryGetPropertyValue("ModelName", out var modelNameNode))
                currentLlm["ModelName"] = modelNameNode?.GetValue<string>();

            if (llmSettings.TryGetPropertyValue("EnableThinking", out var enableThinkingNode))
                currentLlm["EnableThinking"] = enableThinkingNode?.GetValue<bool>();
        }

        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
        await System.IO.File.WriteAllTextAsync(_settingsPath, currentSettings.ToJsonString(writeOptions));

        return Ok(new { message = "Settings updated successfully. Please restart the backend." });
    }
}
