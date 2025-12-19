using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace OllamaRoleplay.Services;

/// <summary>
/// Service TTS avec OpenVoice + MeloTTS pour le clonage vocal
/// Port: 9233
/// </summary>
public class TTSService : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _openVoiceUrl;
    private readonly string _audioOutputDir;
    private readonly string _voiceSamplesDir;
    private bool _disposed;
    
    private System.Media.SoundPlayer? _soundPlayer;

    public bool IsAvailable { get; private set; }
    public string CurrentUrl => _openVoiceUrl;

    // Langues supportées par OpenVoice + MeloTTS
    public static readonly Dictionary<string, string> SupportedLanguages = new()
    {
        ["Français"] = "FR",
        ["French"] = "FR",
        ["English"] = "EN_NEWEST",
        ["Anglais"] = "EN_NEWEST",
        ["Spanish"] = "ES",
        ["Espagnol"] = "ES",
        ["Chinese"] = "ZH",
        ["Chinois"] = "ZH",
        ["Japanese"] = "JP",
        ["Japonais"] = "JP",
        ["Korean"] = "KR",
        ["Coréen"] = "KR",
    };

    public TTSService(string? initialUrl = null)
    {
        _openVoiceUrl = initialUrl ?? "http://127.0.0.1:9233";
        _audioOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Audio");
        _voiceSamplesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "VoiceSamples");
        Directory.CreateDirectory(_audioOutputDir);
        Directory.CreateDirectory(_voiceSamplesDir);
        
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public void SetUrl(string url) => _openVoiceUrl = url.TrimEnd('/');

    public async Task<bool> CheckAvailableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync($"{_openVoiceUrl}/health", cts.Token);
            IsAvailable = response.IsSuccessStatusCode;
            return IsAvailable;
        }
        catch
        {
            IsAvailable = false;
            return false;
        }
    }

    public async Task<bool> CheckAvailableAsync(string url)
    {
        SetUrl(url);
        return await CheckAvailableAsync();
    }


    /// <summary>
    /// Génère un audio avec clonage vocal via OpenVoice
    /// </summary>
    /// <param name="text">Texte à synthétiser</param>
    /// <param name="voiceSamplePath">Chemin vers l'audio de référence pour cloner la voix</param>
    /// <param name="language">Langue (Français, English, etc.)</param>
    /// <param name="speed">Vitesse (0.5 - 2.0)</param>
    public async Task<string?> GenerateClonedSpeechAsync(
        string text, 
        string? voiceSamplePath = null,
        string language = "Français",
        float speed = 1.0f)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Mapper la langue
            var langCode = SupportedLanguages.TryGetValue(language, out var code) ? code : "FR";
            
            // Construire la requête JSON (format Segmind compatible)
            var requestData = new
            {
                text = text,
                language = langCode,
                speed = speed,
                input_audio = GetVoiceAudioSource(voiceSamplePath)
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            System.Diagnostics.Debug.WriteLine($"TTS Request: {text.Substring(0, Math.Min(50, text.Length))}...");
            
            var response = await _httpClient.PostAsync($"{_openVoiceUrl}/v1/openvoice", content);
            
            if (response.IsSuccessStatusCode)
            {
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                var outputPath = Path.Combine(_audioOutputDir, $"tts_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
                await File.WriteAllBytesAsync(outputPath, audioBytes);
                System.Diagnostics.Debug.WriteLine($"TTS OK: {audioBytes.Length} bytes -> {outputPath}");
                return outputPath;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"TTS Error {response.StatusCode}: {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Exception: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// TTS simple sans clonage (plus rapide)
    /// </summary>
    public async Task<string?> GenerateSimpleSpeechAsync(string text, string language = "Français", float speed = 1.0f)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var langCode = SupportedLanguages.TryGetValue(language, out var code) ? code : "FR";
            var voiceMap = new Dictionary<string, string>
            {
                ["FR"] = "fr", ["EN_NEWEST"] = "en", ["ES"] = "es",
                ["ZH"] = "zh", ["JP"] = "jp", ["KR"] = "kr"
            };
            var voice = voiceMap.TryGetValue(langCode, out var v) ? v : "fr";

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(text), "text");
            formData.Add(new StringContent(voice), "voice");
            formData.Add(new StringContent(speed.ToString("F1")), "speed");

            var response = await _httpClient.PostAsync($"{_openVoiceUrl}/simple-tts", formData);
            
            if (response.IsSuccessStatusCode)
            {
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                var outputPath = Path.Combine(_audioOutputDir, $"simple_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
                await File.WriteAllBytesAsync(outputPath, audioBytes);
                return outputPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Simple TTS Error: {ex.Message}");
        }
        return null;
    }


    /// <summary>
    /// Obtient la source audio (URL ou chemin local)
    /// </summary>
    private string GetVoiceAudioSource(string? voiceSamplePath)
    {
        // Si pas de sample, utiliser un sample par défaut
        if (string.IsNullOrEmpty(voiceSamplePath) || !File.Exists(voiceSamplePath))
        {
            // URL d'un sample par défaut (voix neutre)
            return "https://segmind-sd-models.s3.amazonaws.com/display_images/openvoice-ip.mp3";
        }
        
        // Retourner le chemin absolu pour le fichier local
        return Path.GetFullPath(voiceSamplePath);
    }

    /// <summary>
    /// Copie un fichier audio vers le dossier des samples de voix
    /// </summary>
    public string? CopyVoiceSample(string sourcePath, string characterId)
    {
        try
        {
            if (!File.Exists(sourcePath))
                return null;

            var extension = Path.GetExtension(sourcePath).ToLower();
            if (extension != ".mp3" && extension != ".wav" && extension != ".ogg")
                return null;

            var destPath = Path.Combine(_voiceSamplesDir, $"{characterId}_voice{extension}");
            File.Copy(sourcePath, destPath, overwrite: true);
            return destPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Copy voice sample error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Supprime le sample de voix d'un personnage
    /// </summary>
    public void DeleteVoiceSample(string characterId)
    {
        try
        {
            var patterns = new[] { $"{characterId}_voice.mp3", $"{characterId}_voice.wav", $"{characterId}_voice.ogg" };
            foreach (var pattern in patterns)
            {
                var path = Path.Combine(_voiceSamplesDir, pattern);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        catch { }
    }

    public void PlayAudio(string audioPath)
    {
        try
        {
            StopAudio();
            if (File.Exists(audioPath))
            {
                _soundPlayer = new System.Media.SoundPlayer(audioPath);
                _soundPlayer.Play();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Play audio error: {ex.Message}");
        }
    }

    public void StopAudio()
    {
        try
        {
            _soundPlayer?.Stop();
            _soundPlayer?.Dispose();
            _soundPlayer = null;
        }
        catch { }
    }

    /// <summary>
    /// Nettoie les anciens fichiers audio (plus de 1 jour)
    /// </summary>
    public void CleanOldAudioFiles()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-1);
            foreach (var file in Directory.GetFiles(_audioOutputDir, "*.wav"))
            {
                if (File.GetCreationTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopAudio();
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
