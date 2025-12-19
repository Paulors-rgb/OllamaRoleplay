using System.IO;
using System.Text.Json;
using OllamaRoleplay.Models;

namespace OllamaRoleplay.Services;

/// <summary>
/// Service de gestion des conversations (avec chiffrement et auto-save par personnage)
/// </summary>
public class ConversationService
{
    private readonly string _conversationsDir;
    private ConversationSession? _currentSession;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConversationSession? CurrentSession => _currentSession;

    public ConversationService()
    {
        _conversationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Conversations");
        Directory.CreateDirectory(_conversationsDir);
    }

    /// <summary>
    /// Obtient le chemin du fichier de conversation pour un personnage
    /// </summary>
    private string GetCharacterConversationPath(string characterId)
    {
        return Path.Combine(_conversationsDir, $"chat_{characterId}.dat");
    }

    /// <summary>
    /// Démarre ou charge une session pour un personnage
    /// </summary>
    public void StartNewSession(Character character, string model)
    {
        // Sauvegarder la session actuelle avant de changer
        AutoSaveCurrentSession();
        
        // Essayer de charger une conversation existante pour ce personnage
        var existingSession = LoadCharacterSession(character.Id);
        
        if (existingSession != null)
        {
            _currentSession = existingSession;
            _currentSession.ModelUsed = model; // Mettre à jour le modèle
        }
        else
        {
            _currentSession = new ConversationSession
            {
                CharacterId = character.Id,
                CharacterName = character.Name,
                ModelUsed = model
            };
        }
    }

    /// <summary>
    /// Charge la session d'un personnage depuis le fichier chiffré
    /// </summary>
    private ConversationSession? LoadCharacterSession(string characterId)
    {
        try
        {
            var filePath = GetCharacterConversationPath(characterId);
            if (File.Exists(filePath))
            {
                var json = EncryptionService.LoadEncrypted(filePath);
                return JsonSerializer.Deserialize<ConversationSession>(json, JsonOptions);
            }
        }
        catch { /* Ignorer les erreurs */ }
        return null;
    }

    /// <summary>
    /// Sauvegarde automatique de la session actuelle
    /// </summary>
    public void AutoSaveCurrentSession()
    {
        if (_currentSession == null || string.IsNullOrEmpty(_currentSession.CharacterId)) return;

        try
        {
            var filePath = GetCharacterConversationPath(_currentSession.CharacterId);
            var json = JsonSerializer.Serialize(_currentSession, JsonOptions);
            EncryptionService.SaveEncrypted(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur auto-save: {ex.Message}");
        }
    }

    public void AddMessage(ChatMessage message)
    {
        if (_currentSession == null) return;
        
        message.CharacterId = _currentSession.CharacterId;
        _currentSession.Messages.Add(message);
        _currentSession.LastActivity = DateTime.Now;
        
        // Auto-save après chaque message
        AutoSaveCurrentSession();
    }

    /// <summary>
    /// Modifie un message existant
    /// </summary>
    public void EditMessage(string messageId, string newContent)
    {
        if (_currentSession == null) return;
        
        var message = _currentSession.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            message.Content = newContent;
            AutoSaveCurrentSession();
        }
    }

    /// <summary>
    /// Supprime un message
    /// </summary>
    public void DeleteMessage(string messageId)
    {
        if (_currentSession == null) return;
        
        _currentSession.Messages.RemoveAll(m => m.Id == messageId);
        AutoSaveCurrentSession();
    }

    public void SaveCurrentSession()
    {
        AutoSaveCurrentSession();
    }

    public List<ConversationSession> GetSavedSessions()
    {
        var sessions = new List<ConversationSession>();
        
        try
        {
            foreach (var file in Directory.GetFiles(_conversationsDir, "chat_*.dat"))
            {
                try
                {
                    var json = EncryptionService.LoadEncrypted(file);
                    var session = JsonSerializer.Deserialize<ConversationSession>(json, JsonOptions);
                    if (session != null) sessions.Add(session);
                }
                catch { /* Ignorer les fichiers corrompus */ }
            }
        }
        catch { /* Ignorer les erreurs */ }

        return sessions.OrderByDescending(s => s.LastActivity).ToList();
    }

    public void LoadSession(ConversationSession session)
    {
        AutoSaveCurrentSession();
        _currentSession = session;
    }

    public void ClearCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.Messages.Clear();
            AutoSaveCurrentSession();
        }
    }

    /// <summary>
    /// Supprime la conversation d'un personnage
    /// </summary>
    public void DeleteCharacterConversation(string characterId)
    {
        try
        {
            var filePath = GetCharacterConversationPath(characterId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch { /* Ignorer */ }
    }

    public void DeleteSession(string sessionId)
    {
        try
        {
            foreach (var file in Directory.GetFiles(_conversationsDir, "chat_*.dat"))
            {
                try
                {
                    var json = EncryptionService.LoadEncrypted(file);
                    var session = JsonSerializer.Deserialize<ConversationSession>(json, JsonOptions);
                    if (session?.Id == sessionId)
                    {
                        File.Delete(file);
                        break;
                    }
                }
                catch { /* Ignorer */ }
            }
        }
        catch { /* Ignorer */ }
    }
}
