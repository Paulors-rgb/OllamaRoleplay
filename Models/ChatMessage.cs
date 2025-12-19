using System.Text.Json.Serialization;

namespace OllamaRoleplay.Models;

/// <summary>
/// Message de conversation
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Role { get; set; } = "user"; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? CharacterId { get; set; }

    [JsonIgnore]
    public bool IsUser => Role == "user";
    
    [JsonIgnore]
    public bool IsAssistant => Role == "assistant";
}

/// <summary>
/// Session de conversation
/// </summary>
public class ConversationSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastActivity { get; set; } = DateTime.Now;
    public string ModelUsed { get; set; } = string.Empty;
}
