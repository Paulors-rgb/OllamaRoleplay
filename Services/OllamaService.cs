using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using OllamaRoleplay.Models;

namespace OllamaRoleplay.Services;

/// <summary>
/// Statistiques de génération
/// </summary>
public class GenerationStats
{
    public int PromptTokens { get; set; }
    public int ResponseTokens { get; set; }
    public double DurationSeconds { get; set; }
    public double TokensPerSecond => DurationSeconds > 0 ? ResponseTokens / DurationSeconds : 0;
}

/// <summary>
/// Service de communication avec Ollama - Optimisé RAM et GPU
/// </summary>
public class OllamaService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private bool _disposed;
    
    // Dernières statistiques de génération
    public GenerationStats? LastStats { get; private set; }

    public OllamaService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _baseUrl = settingsService.Settings.OllamaBaseUrl.TrimEnd('/');
        
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 4
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<List<OllamaModel>> GetModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OllamaModelsResponse>(json, JsonOptions);
            return result?.Models ?? new List<OllamaModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur récupération modèles: {ex.Message}");
            return new List<OllamaModel>();
        }
    }

    public async Task<bool> IsOllamaRunningAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string model,
        List<ChatMessage> messages,
        Character character,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var settings = _settingsService.Settings;
        var startTime = DateTime.Now;
        LastStats = new GenerationStats();
        
        // Construire les messages pour l'API
        var apiMessages = new List<OllamaChatMessage>();
        
        // Message système avec les instructions du personnage
        var systemPrompt = BuildSystemPrompt(character, settings.AllowInternetAccess);
        apiMessages.Add(new OllamaChatMessage { Role = "system", Content = systemPrompt });
        
        // Ajouter l'historique de conversation
        var historyLimit = Math.Min(messages.Count, settings.MaxConversationHistory);
        var recentMessages = messages.TakeLast(historyLimit);
        
        foreach (var msg in recentMessages)
        {
            apiMessages.Add(new OllamaChatMessage
            {
                Role = msg.Role,
                Content = msg.Content
            });
        }

        var request = new OllamaChatRequest
        {
            Model = model,
            Messages = apiMessages,
            Stream = true,
            Options = new OllamaOptions
            {
                Temperature = settings.Temperature,
                NumCtx = settings.ContextLength,
                NumGpu = -1
            }
        };

        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = httpContent
        };

        using var response = await _httpClient.SendAsync(
            httpRequest, 
            HttpCompletionOption.ResponseHeadersRead, 
            ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;

            OllamaChatResponse? chatResponse = null;
            try
            {
                chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
            }
            catch { continue; }

            if (chatResponse?.Message?.Content != null)
            {
                yield return chatResponse.Message.Content;
            }

            if (chatResponse?.Done == true)
            {
                // Récupérer les statistiques finales
                LastStats = new GenerationStats
                {
                    PromptTokens = chatResponse.PromptEvalCount ?? 0,
                    ResponseTokens = chatResponse.EvalCount ?? 0,
                    DurationSeconds = (DateTime.Now - startTime).TotalSeconds
                };
                break;
            }
        }
    }

    private string BuildSystemPrompt(Character character, bool allowInternet)
    {
        var basePrompt = character.GenerateSystemPrompt();
        
        string internetRestriction = "";
        if (!allowInternet)
        {
            internetRestriction = """

=== INTERNET RESTRICTION ===
You do NOT have access to the internet. You cannot search online or access URLs.
Use ONLY your internal knowledge. If asked about recent events, say you don't have access to current information.
""";
        }

        return basePrompt + internetRestriction;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
