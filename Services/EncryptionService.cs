using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OllamaRoleplay.Services;

/// <summary>
/// Service de chiffrement AES-256 pour les données sauvegardées
/// </summary>
public static class EncryptionService
{
    // Clé dérivée de l'identifiant machine (unique par PC)
    private static readonly byte[] Key;
    private static readonly byte[] IV;

    static EncryptionService()
    {
        // Génère une clé unique basée sur le nom de la machine + username
        var machineId = $"OllamaRoleplay_{Environment.MachineName}_{Environment.UserName}_SecretKey2024!";
        using var sha256 = SHA256.Create();
        Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
        
        // IV fixe mais dérivé aussi
        var ivSource = $"OllamaRP_IV_{Environment.MachineName}";
        using var md5 = MD5.Create();
        IV = md5.ComputeHash(Encoding.UTF8.GetBytes(ivSource));
    }

    /// <summary>
    /// Chiffre une chaîne de caractères
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            return plainText; // En cas d'erreur, retourne le texte original
        }
    }

    /// <summary>
    /// Déchiffre une chaîne de caractères
    /// </summary>
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            // Si le déchiffrement échoue, c'est peut-être un ancien fichier non chiffré
            return encryptedText;
        }
    }

    /// <summary>
    /// Sauvegarde un fichier chiffré
    /// </summary>
    public static void SaveEncrypted(string filePath, string content)
    {
        var encrypted = Encrypt(content);
        File.WriteAllText(filePath, encrypted);
    }

    /// <summary>
    /// Charge un fichier chiffré
    /// </summary>
    public static string LoadEncrypted(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;
        var encrypted = File.ReadAllText(filePath);
        return Decrypt(encrypted);
    }
}
