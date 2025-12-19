namespace OllamaRoleplay.Models;

/// <summary>
/// Paramètres de l'application
/// </summary>
public class AppSettings
{
    public string SelectedModel { get; set; } = string.Empty;
    public bool AllowInternetAccess { get; set; } = false;
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    
    /// <summary>URL de l'API TTS (OpenVoice + MeloTTS)</summary>
    public string CosyVoiceUrl { get; set; } = "http://127.0.0.1:9233";
    
    /// <summary>URL de l'API STT (Whisper)</summary>
    public string SenseVoiceUrl { get; set; } = "http://127.0.0.1:9234";
    
    public float Temperature { get; set; } = 0.8f;
    public int ContextLength { get; set; } = 4096;
    public int MaxTokens { get; set; } = 2048;
    public string DefaultLanguage { get; set; } = "Français";
    public bool DarkMode { get; set; } = true;
    public bool AutoSaveConversations { get; set; } = true;
    public int MaxConversationHistory { get; set; } = 50;
    public string AppLanguage { get; set; } = "fr"; // "fr" ou "en"
}
