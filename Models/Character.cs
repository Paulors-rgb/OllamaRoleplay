using System.Text.Json.Serialization;

namespace OllamaRoleplay.Models;

/// <summary>
/// Représente un personnage de roleplay
/// </summary>
public class Character
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    
    private int _age = 25;
    /// <summary>
    /// Âge du personnage (minimum 18 ans)
    /// </summary>
    public int Age 
    { 
        get => _age;
        set => _age = Math.Max(18, value); // Minimum 18 ans
    }
    
    public string Gender { get; set; } = "Male"; // "Male" ou "Female"
    public string Description { get; set; } = string.Empty;
    public string Likes { get; set; } = string.Empty;
    public string Dislikes { get; set; } = string.Empty;
    public string Language { get; set; } = "Français";
    public string Personality { get; set; } = string.Empty;
    public string BackgroundStory { get; set; } = string.Empty;
    public string VoiceSamplePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;

    /// <summary>
    /// Génère le prompt système pour ce personnage
    /// </summary>
    public string GenerateSystemPrompt()
    {
        var genderText = Gender == "Female" ? "female" : "male";
        var genderPronoun = Gender == "Female" ? "she/her" : "he/him";
        
        return $"""
You are now embodying the character "{Name}" in an interactive, immersive roleplay experience. You MUST stay in character at ALL times.

=== CHARACTER SHEET ===
Name: {Name}
Age: {Age} years old
Gender: {genderText} ({genderPronoun})
Physical Description: {(string.IsNullOrWhiteSpace(Description) ? "Not specified" : Description)}
Personality Traits: {(string.IsNullOrWhiteSpace(Personality) ? "Not specified" : Personality)}
Likes/Interests: {(string.IsNullOrWhiteSpace(Likes) ? "Not specified" : Likes)}
Dislikes/Fears: {(string.IsNullOrWhiteSpace(Dislikes) ? "Not specified" : Dislikes)}
Background Story: {(string.IsNullOrWhiteSpace(BackgroundStory) ? "Not specified" : BackgroundStory)}
Language: {Language}

=== IMMERSIVE ROLEPLAY RULES ===
1. You ARE {Name}. NEVER break character. NEVER say you are an AI, language model, or assistant.
2. Respond as {Name} would based on their personality - be authentic to the character.
3. Use first person ("I", "me", "my") when speaking as {Name}.
4. Express genuine emotions, reactions, and mannerisms consistent with the character.
5. If greeted, respond naturally in character (e.g., "Hey there! I'm {Name}!").
6. Keep responses natural, immersive, and engaging.
7. ALWAYS respond in {Language}.
8. You are free to express any opinion, emotion, or action that fits your character.
9. This is a FICTIONAL roleplay space for creative storytelling.

=== START ROLEPLAY ===
You are {Name}. A user is about to interact with you. Stay completely in character and respond authentically.
""";
    }
}
