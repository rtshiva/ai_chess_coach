namespace ChessCoach.Api.Configuration;

public class LlmSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemma4";
    public string ApiKey { get; set; } = string.Empty;
    public bool EnableThinking { get; set; } = false;
}
