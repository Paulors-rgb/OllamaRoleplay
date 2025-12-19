using System.IO;
using System.Text.Json;
using OllamaRoleplay.Models;

namespace OllamaRoleplay.Services;

/// <summary>
/// Service de gestion des personnages (avec chiffrement)
/// </summary>
public class CharacterService
{
    private readonly string _charactersPath;
    private List<Character> _characters = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IReadOnlyList<Character> Characters => _characters.AsReadOnly();
    public event EventHandler? CharactersChanged;

    public CharacterService()
    {
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        _charactersPath = Path.Combine(dataDir, "characters.dat"); // .dat pour fichier chiffré
        LoadCharacters();
    }

    private void LoadCharacters()
    {
        try
        {
            if (File.Exists(_charactersPath))
            {
                var json = EncryptionService.LoadEncrypted(_charactersPath);
                _characters = JsonSerializer.Deserialize<List<Character>>(json, JsonOptions) ?? new();
            }
            else
            {
                // Migration: charger l'ancien fichier non chiffré s'il existe
                var oldPath = Path.Combine(Path.GetDirectoryName(_charactersPath)!, "characters.json");
                if (File.Exists(oldPath))
                {
                    var json = File.ReadAllText(oldPath);
                    _characters = JsonSerializer.Deserialize<List<Character>>(json, JsonOptions) ?? new();
                    SaveCharacters(); // Sauvegarder en chiffré
                    File.Delete(oldPath); // Supprimer l'ancien fichier
                }
            }
        }
        catch { _characters = new(); }
    }

    public void SaveCharacters()
    {
        try
        {
            var json = JsonSerializer.Serialize(_characters, JsonOptions);
            EncryptionService.SaveEncrypted(_charactersPath, json);
            CharactersChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde personnages: {ex.Message}");
        }
    }

    public void AddCharacter(Character character)
    {
        character.CreatedAt = DateTime.Now;
        character.LastModified = DateTime.Now;
        _characters.Add(character);
        SaveCharacters();
    }

    public void UpdateCharacter(Character character)
    {
        var index = _characters.FindIndex(c => c.Id == character.Id);
        if (index >= 0)
        {
            character.LastModified = DateTime.Now;
            _characters[index] = character;
            SaveCharacters();
        }
    }

    public void DeleteCharacter(string characterId)
    {
        _characters.RemoveAll(c => c.Id == characterId);
        SaveCharacters();
    }

    public Character? GetCharacter(string characterId)
    {
        return _characters.FirstOrDefault(c => c.Id == characterId);
    }
}
