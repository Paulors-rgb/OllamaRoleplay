using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace OllamaRoleplay.Services;

/// <summary>
/// Service STT (Speech-to-Text) avec SenseVoice
/// </summary>
public class STTService : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _senseVoiceUrl;
    private bool _disposed;

    public bool IsSenseVoiceAvailable { get; private set; }
    public string CurrentUrl => _senseVoiceUrl;

    public STTService(string? initialUrl = null)
    {
        _senseVoiceUrl = initialUrl ?? "http://127.0.0.1:9234";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    }

    public void SetUrl(string url) => _senseVoiceUrl = url.TrimEnd('/');

    public async Task<bool> CheckSenseVoiceAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{_senseVoiceUrl}/health", cts.Token);
            IsSenseVoiceAvailable = response.IsSuccessStatusCode;
            return IsSenseVoiceAvailable;
        }
        catch
        {
            IsSenseVoiceAvailable = false;
            return false;
        }
    }

    public async Task<bool> CheckSenseVoiceAsync(string url)
    {
        SetUrl(url);
        return await CheckSenseVoiceAsync();
    }

    /// <summary>
    /// Transcrit un fichier audio en texte avec détection d'émotion
    /// </summary>
    public async Task<TranscriptionResult?> TranscribeAsync(string audioPath, string language = "auto")
    {
        if (!IsSenseVoiceAvailable || !File.Exists(audioPath))
            return null;

        try
        {
            using var content = new MultipartFormDataContent();
            
            var audioBytes = await File.ReadAllBytesAsync(audioPath);
            var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "file", Path.GetFileName(audioPath));
            content.Add(new StringContent(language), "lang");

            var response = await _httpClient.PostAsync($"{_senseVoiceUrl}/transcribe", content);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SenseVoiceResponse>(json);
                
                if (result?.Success == true)
                {
                    return new TranscriptionResult
                    {
                        Text = result.Text ?? "",
                        Emotion = result.Emotion ?? "NEUTRAL",
                        Language = result.Language ?? "unknown",
                        Event = result.Event ?? "Speech"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur STT: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Transcrit depuis un stream audio (pour enregistrement en direct)
    /// </summary>
    public async Task<TranscriptionResult?> TranscribeFromBytesAsync(byte[] audioData, string language = "auto")
    {
        if (!IsSenseVoiceAvailable || audioData == null || audioData.Length == 0)
            return null;

        try
        {
            using var content = new MultipartFormDataContent();
            
            var audioContent = new ByteArrayContent(audioData);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "file", "recording.wav");
            content.Add(new StringContent(language), "lang");

            var response = await _httpClient.PostAsync($"{_senseVoiceUrl}/transcribe", content);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SenseVoiceResponse>(json);
                
                if (result?.Success == true)
                {
                    return new TranscriptionResult
                    {
                        Text = result.Text ?? "",
                        Emotion = result.Emotion ?? "NEUTRAL",
                        Language = result.Language ?? "unknown",
                        Event = result.Event ?? "Speech"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur STT: {ex.Message}");
        }

        return null;
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

/// <summary>
/// Résultat de transcription
/// </summary>
public class TranscriptionResult
{
    public string Text { get; set; } = "";
    public string Emotion { get; set; } = "NEUTRAL";
    public string Language { get; set; } = "unknown";
    public string Event { get; set; } = "Speech";
    
    public string EmotionEmoji => Emotion.ToUpper() switch
    {
        "HAPPY" => "😊",
        "SAD" => "😢",
        "ANGRY" => "😠",
        "FEARFUL" => "😨",
        "DISGUSTED" => "🤢",
        "SURPRISED" => "😲",
        _ => "😐"
    };
}

/// <summary>
/// Réponse de l'API SenseVoice
/// </summary>
internal class SenseVoiceResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("emotion")]
    public string? Emotion { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("event")]
    public string? Event { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("language")]
    public string? Language { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; set; }
}
