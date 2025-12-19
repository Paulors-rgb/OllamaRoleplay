using System.IO;
using System.Windows;
using System.Windows.Media;
using OllamaRoleplay.Services;

namespace OllamaRoleplay;

public partial class App : Application
{
    public static CharacterService CharacterService { get; private set; } = null!;
    public static OllamaService OllamaService { get; private set; } = null!;
    public static ConversationService ConversationService { get; private set; } = null!;
    public static SettingsService SettingsService { get; private set; } = null!;
    public static TTSService TTSService { get; private set; } = null!;
    public static STTService STTService { get; private set; } = null!;
    public static AudioRecorderService AudioRecorder { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Activer l'accélération GPU pour WPF
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
        
        // Gestion globale des exceptions pour éviter les crashs
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Initialiser les services
        SettingsService = new SettingsService();
        CharacterService = new CharacterService();
        OllamaService = new OllamaService(SettingsService);
        ConversationService = new ConversationService();
        TTSService = new TTSService(SettingsService.Settings.CosyVoiceUrl);
        STTService = new STTService(SettingsService.Settings.SenseVoiceUrl);
        AudioRecorder = new AudioRecorderService();

        base.OnStartup(e);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogError(e.ExceptionObject as Exception);
        if (e.IsTerminating)
        {
            MessageBox.Show(
                "Une erreur critique s'est produite. L'application va se fermer.\n\n" +
                "Vérifiez le fichier error.log pour plus de détails.",
                "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogError(e.Exception);
        MessageBox.Show(
            $"Une erreur s'est produite:\n{e.Exception.Message}\n\nL'application va continuer.",
            "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogError(e.Exception);
        e.SetObserved();
    }

    private static void LogError(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch { }
    }
}
