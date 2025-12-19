namespace OllamaRoleplay.Models;

/// <summary>
/// Modèle LLM disponible via Ollama
/// </summary>
public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
    public string ModifiedAt { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Digest { get; set; } = string.Empty;

    public string DisplayName => Name.Split(':')[0];
    public string SizeFormatted => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Réponse de l'API Ollama pour la liste des modèles
/// </summary>
public class OllamaModelsResponse
{
    public List<OllamaModel> Models { get; set; } = new();
}

/// <summary>
/// Requête de chat vers Ollama
/// </summary>
public class OllamaChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OllamaChatMessage> Messages { get; set; } = new();
    public bool Stream { get; set; } = true;
    public OllamaOptions? Options { get; set; }
}

/// <summary>
/// Message pour l'API Ollama
/// </summary>
public class OllamaChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Options de génération Ollama
/// </summary>
public class OllamaOptions
{
    public int? NumCtx { get; set; } = 4096;
    public float? Temperature { get; set; } = 0.8f;
    public float? TopP { get; set; } = 0.9f;
    public int? NumGpu { get; set; } = -1; // -1 = utiliser tous les GPU disponibles
}

/// <summary>
/// Réponse streaming de Ollama
/// </summary>
public class OllamaChatResponse
{
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OllamaChatMessage? Message { get; set; }
    public bool Done { get; set; }
    public long? TotalDuration { get; set; }
    public long? LoadDuration { get; set; }
    public int? PromptEvalCount { get; set; }
    public int? EvalCount { get; set; }
}
