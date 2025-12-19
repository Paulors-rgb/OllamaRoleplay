using System.IO;
using System.Runtime.InteropServices;

namespace OllamaRoleplay.Services;

/// <summary>
/// Service d'enregistrement audio via Windows API (mciSendString)
/// </summary>
public class AudioRecorderService : IDisposable
{
    [DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, IntPtr returnString, int returnSize, IntPtr callback);

    private readonly string _tempDir;
    private string? _currentRecordingPath;
    private bool _isRecording;
    private bool _disposed;

    public bool IsRecording => _isRecording;
    public string? LastRecordingPath => _currentRecordingPath;

    public AudioRecorderService()
    {
        _tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Recordings");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Démarre l'enregistrement audio
    /// </summary>
    public bool StartRecording()
    {
        if (_isRecording) return false;

        try
        {
            _currentRecordingPath = Path.Combine(_tempDir, $"rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
            
            // Ouvrir le périphérique audio
            mciSendString("close all", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("open new type waveaudio alias capture", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("set capture time format ms", IntPtr.Zero, 0, IntPtr.Zero);
            
            // Format: 16kHz, mono, 16-bit (optimal pour SenseVoice)
            mciSendString("set capture bitspersample 16", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("set capture samplespersec 16000", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("set capture channels 1", IntPtr.Zero, 0, IntPtr.Zero);
            
            // Démarrer l'enregistrement
            mciSendString("record capture", IntPtr.Zero, 0, IntPtr.Zero);
            
            _isRecording = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur démarrage enregistrement: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Arrête l'enregistrement et sauvegarde le fichier
    /// </summary>
    public string? StopRecording()
    {
        if (!_isRecording) return null;

        try
        {
            // Arrêter et sauvegarder
            mciSendString("stop capture", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString($"save capture \"{_currentRecordingPath}\"", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("close capture", IntPtr.Zero, 0, IntPtr.Zero);
            
            _isRecording = false;
            
            if (File.Exists(_currentRecordingPath))
            {
                return _currentRecordingPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur arrêt enregistrement: {ex.Message}");
        }

        _isRecording = false;
        return null;
    }

    /// <summary>
    /// Annule l'enregistrement en cours
    /// </summary>
    public void CancelRecording()
    {
        if (_isRecording)
        {
            mciSendString("stop capture", IntPtr.Zero, 0, IntPtr.Zero);
            mciSendString("close capture", IntPtr.Zero, 0, IntPtr.Zero);
            _isRecording = false;
        }

        // Supprimer le fichier temporaire
        if (_currentRecordingPath != null && File.Exists(_currentRecordingPath))
        {
            try { File.Delete(_currentRecordingPath); } catch { }
        }
    }

    /// <summary>
    /// Nettoie les anciens enregistrements (plus de 1 heure)
    /// </summary>
    public void CleanOldRecordings()
    {
        try
        {
            var cutoff = DateTime.Now.AddHours(-1);
            foreach (var file in Directory.GetFiles(_tempDir, "rec_*.wav"))
            {
                if (File.GetCreationTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CancelRecording();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
