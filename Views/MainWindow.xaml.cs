using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using OllamaRoleplay.Models;

namespace OllamaRoleplay.Views;

public partial class MainWindow : Window
{
    private ObservableCollection<MessageViewModel> _messages = new();
    private CancellationTokenSource? _cts;
    private Character? _selectedCharacter;
    private OllamaModel? _selectedModel;
    private string _currentLang = "fr";

    public MainWindow()
    {
        InitializeComponent();
        MessagesItemsControl.ItemsSource = _messages;
        
        _currentLang = App.SettingsService.Settings.AppLanguage;
        AppLanguageComboBox.SelectedIndex = _currentLang == "en" ? 1 : 0;
        
        LoadData();
        UpdateUILanguage();
        _ = CheckConnectionAsync();
    }

    private void LoadData()
    {
        CharacterListBox.ItemsSource = App.CharacterService.Characters;
        if (App.CharacterService.Characters.Count > 0)
            CharacterListBox.SelectedIndex = 0;
        InternetCheckBox.IsChecked = App.SettingsService.Settings.AllowInternetAccess;
        
        // Charger les URLs sauvegardées
        CosyVoiceUrlBox.Text = App.SettingsService.Settings.CosyVoiceUrl;
        SenseVoiceUrlBox.Text = App.SettingsService.Settings.SenseVoiceUrl;
        
        UpdateInternetStatus();
    }

    private void UpdateUILanguage()
    {
        Title = Languages.Get("AppTitle", _currentLang);
        LblStatusOllama.Text = Languages.Get("StatusOllama", _currentLang);
        BtnCheckConnection.Content = Languages.Get("CheckConnection", _currentLang);
        LblLLMModel.Text = Languages.Get("LLMModel", _currentLang);
        LblInternetAccess.Text = Languages.Get("InternetAccess", _currentLang);
        LblCharacters.Text = Languages.Get("Characters", _currentLang);
        LblEditCharacter.Text = Languages.Get("EditCharacter", _currentLang);
        LblName.Text = Languages.Get("Name", _currentLang);
        LblAge.Text = Languages.Get("Age", _currentLang);
        LblGender.Text = Languages.Get("Gender", _currentLang);
        LblLanguage.Text = Languages.Get("Language", _currentLang);
        LblDescription.Text = Languages.Get("Description", _currentLang);
        LblLikes.Text = Languages.Get("Likes", _currentLang);
        LblDislikes.Text = Languages.Get("Dislikes", _currentLang);
        LblPersonality.Text = Languages.Get("Personality", _currentLang);
        LblVoiceSample.Text = Languages.Get("VoiceSample", _currentLang);
        BtnSaveCharacter.Content = Languages.Get("Save", _currentLang);
        StatusText.Text = Languages.Get("Ready", _currentLang);
        LblAppLanguage.Text = Languages.Get("AppLanguage", _currentLang);
        
        if (_selectedCharacter == null)
            HeaderCharName.Text = Languages.Get("NoCharacter", _currentLang);
        UpdateInternetStatus();
    }

    private void AppLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppLanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string lang)
        {
            _currentLang = lang;
            App.SettingsService.UpdateSettings(s => s.AppLanguage = lang);
            UpdateUILanguage();
        }
    }

    private async Task CheckConnectionAsync()
    {
        StatusText.Text = Languages.Get("Checking", _currentLang);
        
        var ollamaConnected = await App.OllamaService.IsOllamaRunningAsync();
        StatusIndicator.Fill = ollamaConnected ? Brushes.LimeGreen : Brushes.Red;
        
        // Utiliser l'URL du champ de texte et sauvegarder - OpenVoice TTS
        var ttsUrl = CosyVoiceUrlBox.Text.Trim();
        if (!string.IsNullOrEmpty(ttsUrl))
        {
            App.SettingsService.UpdateSettings(s => s.CosyVoiceUrl = ttsUrl);
        }
        
        var ttsConnected = await App.TTSService.CheckAvailableAsync(ttsUrl);
        TTSStatusIndicator.Fill = ttsConnected ? Brushes.LimeGreen : Brushes.Orange;
        
        // SenseVoice STT
        var senseUrl = SenseVoiceUrlBox.Text.Trim();
        if (!string.IsNullOrEmpty(senseUrl))
        {
            App.SettingsService.UpdateSettings(s => s.SenseVoiceUrl = senseUrl);
        }
        
        var sttConnected = await App.STTService.CheckSenseVoiceAsync(senseUrl);
        STTStatusIndicator.Fill = sttConnected ? Brushes.LimeGreen : Brushes.Orange;
        
        StatusText.Text = ollamaConnected ? Languages.Get("Connected", _currentLang) : Languages.Get("NotConnected", _currentLang);
        
        if (ttsConnected)
            StatusText.Text += " | TTS ✓";
        if (sttConnected)
            StatusText.Text += " | STT ✓";

        if (ollamaConnected)
            await LoadModelsAsync();
    }

    private async Task LoadModelsAsync()
    {
        var models = await App.OllamaService.GetModelsAsync();
        ModelComboBox.ItemsSource = models;

        if (models.Count == 0)
        {
            StatusText.Text = Languages.Get("NoModels", _currentLang);
            return;
        }

        var savedModel = App.SettingsService.Settings.SelectedModel;
        if (!string.IsNullOrEmpty(savedModel))
        {
            var model = models.FirstOrDefault(m => m.Name == savedModel);
            if (model != null) ModelComboBox.SelectedItem = model;
        }

        if (ModelComboBox.SelectedItem == null && models.Count > 0)
            ModelComboBox.SelectedIndex = 0;

        _selectedModel = ModelComboBox.SelectedItem as OllamaModel;
        StatusText.Text = $"{models.Count} {Languages.Get("ModelsAvailable", _currentLang)}";
    }

    private async void CheckConnection_Click(object sender, RoutedEventArgs e)
    {
        try { await CheckConnectionAsync(); }
        catch (Exception ex) { StatusText.Text = $"{Languages.Get("Error", _currentLang)}: {ex.Message}"; }
    }

    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedModel = ModelComboBox.SelectedItem as OllamaModel;
        if (_selectedModel != null)
        {
            ModelSizeText.Text = $"{Languages.Get("Size", _currentLang)}: {_selectedModel.SizeFormatted}";
            App.SettingsService.UpdateSettings(s => s.SelectedModel = _selectedModel.Name);
        }
    }

    private void InternetCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        App.SettingsService.UpdateSettings(s => s.AllowInternetAccess = InternetCheckBox.IsChecked == true);
        UpdateInternetStatus();
    }

    private void UpdateInternetStatus()
    {
        InternetStatusText.Text = InternetCheckBox.IsChecked == true 
            ? Languages.Get("Enabled", _currentLang) 
            : Languages.Get("Disabled", _currentLang);
    }

    private void CharacterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sauvegarder le chat actuel avant de changer
        App.ConversationService.AutoSaveCurrentSession();
        
        _selectedCharacter = CharacterListBox.SelectedItem as Character;
        if (_selectedCharacter != null)
        {
            CharacterEditor.Visibility = Visibility.Visible;
            CharNameBox.Text = _selectedCharacter.Name;
            CharAgeBox.Text = _selectedCharacter.Age.ToString();
            CharLanguageBox.Text = _selectedCharacter.Language;
            CharDescBox.Text = _selectedCharacter.Description;
            CharLikesBox.Text = _selectedCharacter.Likes;
            CharDislikesBox.Text = _selectedCharacter.Dislikes;
            CharPersonalityBox.Text = _selectedCharacter.Personality;
            CharVoicePath.Text = _selectedCharacter.VoiceSamplePath;
            
            // Genre
            CharGenderCombo.SelectedIndex = _selectedCharacter.Gender == "Female" ? 1 : 0;

            HeaderCharName.Text = _selectedCharacter.Name;
            HeaderCharDesc.Text = _selectedCharacter.Description;

            // Charger la conversation existante pour ce personnage
            App.ConversationService.StartNewSession(_selectedCharacter, _selectedModel?.Name ?? "");
            LoadMessagesFromSession();
        }
        else
        {
            CharacterEditor.Visibility = Visibility.Collapsed;
            HeaderCharName.Text = Languages.Get("NoCharacter", _currentLang);
            _messages.Clear();
        }
    }

    private void LoadMessagesFromSession()
    {
        _messages.Clear();
        if (App.ConversationService.CurrentSession?.Messages != null)
        {
            foreach (var msg in App.ConversationService.CurrentSession.Messages)
            {
                _messages.Add(new MessageViewModel(msg, _currentLang));
            }
        }
        MessagesScrollViewer.ScrollToEnd();
    }

    private void NewCharacter_Click(object sender, RoutedEventArgs e)
    {
        var newChar = new Character
        {
            Name = _currentLang == "fr" ? "Nouveau Personnage" : "New Character",
            Age = 25,
            Gender = "Female",
            Language = _currentLang == "fr" ? "Français" : "English"
        };
        App.CharacterService.AddCharacter(newChar);
        RefreshCharacterList();
        CharacterListBox.SelectedItem = App.CharacterService.Characters.FirstOrDefault(c => c.Id == newChar.Id);
    }

    private void RefreshCharacterList()
    {
        CharacterListBox.ItemsSource = null;
        CharacterListBox.ItemsSource = App.CharacterService.Characters;
    }

    private void DeleteCharacter_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCharacter == null) return;
        
        var result = MessageBox.Show(Languages.Get("ConfirmDelete", _currentLang), 
            Languages.Get("Confirmation", _currentLang), MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            App.ConversationService.DeleteCharacterConversation(_selectedCharacter.Id);
            App.CharacterService.DeleteCharacter(_selectedCharacter.Id);
            RefreshCharacterList();
            CharacterEditor.Visibility = Visibility.Collapsed;
            _selectedCharacter = null;
            _messages.Clear();
            HeaderCharName.Text = Languages.Get("NoCharacter", _currentLang);
        }
    }

    private void SaveCharacter_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCharacter == null) return;

        _selectedCharacter.Name = CharNameBox.Text ?? (_currentLang == "fr" ? "Sans nom" : "Unnamed");
        _selectedCharacter.Age = int.TryParse(CharAgeBox.Text, out var age) ? Math.Max(18, age) : 18; // Minimum 18 ans
        _selectedCharacter.Language = CharLanguageBox.Text ?? (_currentLang == "fr" ? "Français" : "English");
        _selectedCharacter.Description = CharDescBox.Text ?? "";
        _selectedCharacter.Likes = CharLikesBox.Text ?? "";
        _selectedCharacter.Dislikes = CharDislikesBox.Text ?? "";
        _selectedCharacter.Personality = CharPersonalityBox.Text ?? "";
        _selectedCharacter.VoiceSamplePath = CharVoicePath.Text ?? "";
        
        // Genre
        if (CharGenderCombo.SelectedItem is ComboBoxItem genderItem && genderItem.Tag is string gender)
            _selectedCharacter.Gender = gender;

        App.CharacterService.UpdateCharacter(_selectedCharacter);
        
        HeaderCharName.Text = _selectedCharacter.Name;
        HeaderCharDesc.Text = _selectedCharacter.Description;
        
        var selectedId = _selectedCharacter.Id;
        RefreshCharacterList();
        CharacterListBox.SelectedItem = App.CharacterService.Characters.FirstOrDefault(c => c.Id == selectedId);
        
        StatusText.Text = $"{Languages.Get("CharacterSaved", _currentLang)}: {_selectedCharacter.Name}";
    }

    private void SelectVoiceSample_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*",
            Title = Languages.Get("VoiceSample", _currentLang)
        };
        
        if (dialog.ShowDialog() == true)
        {
            CharVoicePath.Text = dialog.FileName;
        }
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                int caretIndex = InputTextBox.CaretIndex;
                InputTextBox.Text = InputTextBox.Text.Insert(caretIndex, Environment.NewLine);
                InputTextBox.CaretIndex = caretIndex + Environment.NewLine.Length;
            }
            else
            {
                SendMessage_Click(sender, e);
            }
            e.Handled = true;
        }
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            StatusText.Text = Languages.Get("ErrorWriteMessage", _currentLang);
            return;
        }
        if (_selectedCharacter == null)
        {
            StatusText.Text = Languages.Get("ErrorSelectCharacter", _currentLang);
            return;
        }
        if (_selectedModel == null)
        {
            StatusText.Text = Languages.Get("ErrorSelectModel", _currentLang);
            return;
        }

        if (App.ConversationService.CurrentSession == null)
            App.ConversationService.StartNewSession(_selectedCharacter, _selectedModel.Name);

        var userText = InputTextBox.Text.Trim();
        InputTextBox.Text = "";

        var userMsg = new ChatMessage { Role = "user", Content = userText };
        _messages.Add(new MessageViewModel(userMsg, _currentLang));
        App.ConversationService.AddMessage(userMsg);

        SendButton.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Visible;
        ProgressIndicator.Visibility = Visibility.Visible;
        StatusText.Text = Languages.Get("Generating", _currentLang);
        StatsText.Text = "";

        _cts = new CancellationTokenSource();
        var startTime = DateTime.Now;

        try
        {
            var assistantMsg = new ChatMessage { Role = "assistant", Content = "" };
            var assistantVm = new MessageViewModel(assistantMsg, _currentLang);
            _messages.Add(assistantVm);

            var conversationMsgs = App.ConversationService.CurrentSession?.Messages
                .Where(m => m.Role != "system").ToList() ?? new();

            await foreach (var token in App.OllamaService.ChatStreamAsync(
                _selectedModel.Name, conversationMsgs, _selectedCharacter, _cts.Token))
            {
                assistantMsg.Content += token;
                var index = _messages.Count - 1;
                if (index >= 0)
                    _messages[index] = new MessageViewModel(assistantMsg, _currentLang);
                MessagesScrollViewer.ScrollToEnd();
            }

            App.ConversationService.AddMessage(assistantMsg);
            
            // Afficher les statistiques
            var stats = App.OllamaService.LastStats;
            if (stats != null)
            {
                var duration = stats.DurationSeconds;
                var tokens = stats.ResponseTokens;
                var tps = stats.TokensPerSecond;
                StatsText.Text = string.Format(Languages.Get("Stats", _currentLang), 
                    duration.ToString("F1"), tokens, tps.ToString("F1"));
            }
            
            StatusText.Text = Languages.Get("Generated", _currentLang);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = Languages.Get("Cancelled", _currentLang);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{Languages.Get("Error", _currentLang)}: {ex.Message}";
        }
        finally
        {
            SendButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
            ProgressIndicator.Visibility = Visibility.Collapsed;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void StopGeneration_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void ClearChat_Click(object sender, RoutedEventArgs e)
    {
        _messages.Clear();
        App.ConversationService.ClearCurrentSession();
        StatusText.Text = Languages.Get("ChatCleared", _currentLang);
        StatsText.Text = "";
    }

    private void SaveChat_Click(object sender, RoutedEventArgs e)
    {
        App.ConversationService.SaveCurrentSession();
        StatusText.Text = Languages.Get("ChatSaved", _currentLang);
    }

    // Menu contextuel - Copier
    private void CopyMessage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu 
            && contextMenu.PlacementTarget is Border border && border.Tag is string messageId)
        {
            var msg = App.ConversationService.CurrentSession?.Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                Clipboard.SetText(msg.Content);
                StatusText.Text = Languages.Get("Copy", _currentLang) + " ✓";
            }
        }
    }

    // Menu contextuel - Modifier
    private void EditMessage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu 
            && contextMenu.PlacementTarget is Border border && border.Tag is string messageId)
        {
            var msg = App.ConversationService.CurrentSession?.Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                var dialog = new EditMessageDialog(msg.Content, _currentLang);
                if (dialog.ShowDialog() == true)
                {
                    App.ConversationService.EditMessage(messageId, dialog.NewContent);
                    LoadMessagesFromSession();
                    StatusText.Text = Languages.Get("Edit", _currentLang) + " ✓";
                }
            }
        }
    }

    // Menu contextuel - Supprimer
    private void DeleteMessage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu 
            && contextMenu.PlacementTarget is Border border && border.Tag is string messageId)
        {
            App.ConversationService.DeleteMessage(messageId);
            LoadMessagesFromSession();
            StatusText.Text = Languages.Get("DeleteMessage", _currentLang) + " ✓";
        }
    }

    // Menu contextuel - TTS avec clonage vocal OpenVoice
    private async void PlayMessageTTS_Click(object sender, RoutedEventArgs e)
    {
        if (!App.TTSService.IsAvailable)
        {
            StatusText.Text = "OpenVoice non disponible - Lancez l'API";
            return;
        }

        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu 
            && contextMenu.PlacementTarget is Border border && border.Tag is string messageId)
        {
            var msg = App.ConversationService.CurrentSession?.Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                StatusText.Text = "🔊 Génération vocale...";
                
                // Utiliser le sample vocal du personnage si disponible
                var voiceSample = _selectedCharacter?.VoiceSamplePath;
                var language = _selectedCharacter?.Language ?? "Français";
                
                string? audioPath = await App.TTSService.GenerateClonedSpeechAsync(
                    msg.Content, 
                    voiceSample,
                    language,
                    1.0f
                );
                
                if (audioPath != null)
                {
                    App.TTSService.PlayAudio(audioPath);
                    StatusText.Text = $"🔊 {Languages.Get("PlayVoice", _currentLang)}";
                }
                else
                {
                    StatusText.Text = "❌ Erreur génération vocale";
                }
            }
        }
    }

    // Gestion du bouton micro - Mode toggle (clic pour démarrer, clic pour arrêter)
    private bool _isRecording = false;
    
    private async void MicButton_Click(object sender, RoutedEventArgs e)
    {
        if (!App.STTService.IsSenseVoiceAvailable)
        {
            StatusText.Text = "Whisper STT non connecté - Vérifiez la connexion";
            return;
        }

        if (!_isRecording)
        {
            // Démarrer l'enregistrement
            if (App.AudioRecorder.StartRecording())
            {
                _isRecording = true;
                MicButton.Content = "⏹️";
                MicButton.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge
                StatusText.Text = "🎤 Enregistrement en cours... (cliquez pour arrêter)";
            }
            else
            {
                StatusText.Text = "Erreur: impossible de démarrer l'enregistrement";
            }
        }
        else
        {
            // Arrêter et transcrire
            _isRecording = false;
            MicButton.Content = "🎤";
            MicButton.Background = (Brush)FindResource("SecondaryBrush");
            StatusText.Text = "Transcription en cours...";
            
            var audioPath = App.AudioRecorder.StopRecording();
            StatusText.Text = $"Audio: {(audioPath != null ? "OK" : "NULL")}";
            
            if (audioPath != null && File.Exists(audioPath))
            {
                var fileInfo = new FileInfo(audioPath);
                StatusText.Text = $"Fichier: {fileInfo.Length} bytes - Envoi STT...";
                
                try
                {
                    var result = await App.STTService.TranscribeAsync(audioPath);
                    
                    if (result != null)
                    {
                        StatusText.Text = $"STT OK: {result.Text?.Length ?? 0} chars";
                        
                        if (!string.IsNullOrEmpty(result.Text))
                        {
                            var emotionInfo = result.Emotion != "NEUTRAL" ? $" {result.EmotionEmoji}" : "";
                            StatusText.Text = $"✓ {result.Text.Substring(0, Math.Min(30, result.Text.Length))}...{emotionInfo}";
                            
                            InputTextBox.Text = result.Text;
                            
                            // Envoyer automatiquement le message
                            await SendMessageAsync();
                        }
                        else
                        {
                            StatusText.Text = "STT: texte vide";
                        }
                    }
                    else
                    {
                        StatusText.Text = "STT: result NULL";
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"STT Error: {ex.Message}";
                }
                
                // Nettoyer le fichier temporaire
                try { File.Delete(audioPath); } catch { }
            }
            else
            {
                StatusText.Text = $"Erreur: fichier audio non créé ({audioPath})";
            }
        }
    }
}

// ViewModel pour l'affichage des messages
public class MessageViewModel
{
    private string _lang;
    
    public string MessageId { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }
    public string TimeStamp { get; set; }
    public string RoleLabel => Role == "user" ? Languages.Get("You", _lang) : Languages.Get("Character", _lang);
    public Brush Background => Role == "user" 
        ? new SolidColorBrush(Color.FromRgb(103, 58, 183)) 
        : new SolidColorBrush(Color.FromRgb(0, 150, 136));
    public HorizontalAlignment Alignment => Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public MessageViewModel(ChatMessage msg, string lang = "fr")
    {
        _lang = lang;
        MessageId = msg.Id;
        Role = msg.Role;
        Content = msg.Content;
        TimeStamp = msg.Timestamp.ToString("HH:mm");
    }
}

// Dialogue pour modifier un message
public class EditMessageDialog : Window
{
    public string NewContent { get; private set; } = "";
    private TextBox _textBox;

    public EditMessageDialog(string currentContent, string lang)
    {
        Title = Languages.Get("EditMessage", lang);
        Width = 450;
        Height = 250;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        _textBox = new TextBox
        {
            Text = currentContent,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(15),
            Padding = new Thickness(10),
            FontSize = 14,
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80))
        };
        Grid.SetRow(_textBox, 0);
        
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(15, 5, 15, 15)
        };
        
        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(10, 5, 10, 5),
            Background = new SolidColorBrush(Color.FromRgb(103, 58, 183)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        okButton.Click += (s, e) => { NewContent = _textBox.Text; DialogResult = true; };
        
        var cancelButton = new Button
        {
            Content = Languages.Get("Delete", lang) == "Delete" ? "Cancel" : "Annuler",
            Width = 80,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10, 5, 10, 5)
        };
        cancelButton.Click += (s, e) => DialogResult = false;
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 1);
        
        grid.Children.Add(_textBox);
        grid.Children.Add(buttonPanel);
        Content = grid;
    }
}
