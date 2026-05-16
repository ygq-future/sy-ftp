using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using sy_ftp.Helpers;
using sy_ftp.Models;

namespace sy_ftp.Services;

/// <summary>
/// Service for exporting and importing encrypted configuration backups.
/// </summary>
public static class ConfigBackupService
{
    private static readonly byte[] MagicHeader = "SFTPBAK\0"u8.ToArray();
    private const int Version = 1;
    private const int SaltSize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Iterations = 600_000;

    /// <summary>
    /// Exports the current configuration to an encrypted backup file.
    /// </summary>
    /// <param name="backupPassword">Password to encrypt the backup</param>
    /// <param name="filePath">Destination file path</param>
    public static void ExportConfig(string backupPassword, string filePath)
    {
        var settings = SettingsService.Current;

        // Convert hosts to DTO format with plaintext passwords
        var exportHosts = new List<HostDto>();
        foreach (var host in settings.Hosts)
        {
            exportHosts.Add(new HostDto
            {
                Id = host.Id,
                Name = host.Name,
                Host = host.Host,
                Port = host.Port,
                Username = host.Username,
                Password = host.Password, // Already plaintext in memory
                Tags = host.Tags,
                DownloadPath = host.DownloadPath
            });
        }

        // Create export data structure
        var exportData = new
        {
            Version = 1,
            ExportDate = DateTime.UtcNow,
            Settings = new
            {
                settings.Theme,
                settings.AccentColor,
                settings.Language,
                settings.DefaultDownloadPath,
                settings.DefaultDataPath,
                settings.WindowTopmost,
                settings.BackgroundImagePath,
                settings.BackgroundOpacity
            },
            Hosts = exportHosts
        };

        // Serialize to JSON (passwords are plaintext in the JSON)
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Encrypt with backup password
        var encrypted = EncryptWithBackupPassword(json, backupPassword);

        // Write to file
        File.WriteAllBytes(filePath, encrypted);
    }

    /// <summary>
    /// Imports configuration from an encrypted backup file.
    /// </summary>
    /// <param name="backupPassword">Password to decrypt the backup</param>
    /// <param name="filePath">Source backup file path</param>
    public static void ImportConfig(string backupPassword, string filePath)
    {
        // Read backup file
        var encrypted = File.ReadAllBytes(filePath);

        // Decrypt with backup password
        var json = DecryptWithBackupPassword(encrypted, backupPassword);
        if (string.IsNullOrEmpty(json))
            throw new InvalidOperationException("Failed to decrypt backup file. Invalid password or corrupted file.");

        // Deserialize
        var importData = JsonSerializer.Deserialize<ImportData>(json);
        if (importData?.Hosts == null)
            throw new InvalidOperationException("Invalid backup file format.");

        // Convert HostDto back to FtpHost with plaintext passwords
        var restoredHosts = new List<FtpHost>();
        foreach (var dto in importData.Hosts)
        {
            var host = new FtpHost
            {
                Name = dto.Name,
                Host = dto.Host,
                Port = dto.Port,
                Username = dto.Username,
                Password = dto.Password, // Plaintext password from backup
                Tags = dto.Tags,
                DownloadPath = dto.DownloadPath
            };
            restoredHosts.Add(host);
        }

        // Update current settings
        var settings = SettingsService.Current;
        if (importData.Settings != null)
        {
            settings.Theme = importData.Settings.Theme ?? settings.Theme;
            settings.AccentColor = importData.Settings.AccentColor ?? settings.AccentColor;
            settings.Language = importData.Settings.Language ?? settings.Language;
            settings.DefaultDownloadPath = importData.Settings.DefaultDownloadPath;
            settings.DefaultDataPath = importData.Settings.DefaultDataPath;
            settings.WindowTopmost = importData.Settings.WindowTopmost;
            settings.BackgroundImagePath = importData.Settings.BackgroundImagePath;
            // Opacity defaults to 0 in struct deserialization; only apply if non-zero
            if (importData.Settings.BackgroundOpacity > 0)
                settings.BackgroundOpacity = importData.Settings.BackgroundOpacity;
        }

        settings.Hosts = restoredHosts;

        // Save to disk (passwords will be encrypted by EncryptedStringConverter)
        SettingsService.Save();
    }

    private static byte[] EncryptWithBackupPassword(string plaintext, string password)
    {
        // Generate random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Derive key from password using PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(32); // 256-bit key

        // Generate random nonce
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        // Encrypt using AES-256-GCM
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Build file format: Magic(8) + Version(4) + Salt(32) + Nonce(12) + Ciphertext + Tag(16)
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(MagicHeader);
        writer.Write(Version);
        writer.Write(salt);
        writer.Write(nonce);
        writer.Write(ciphertext);
        writer.Write(tag);

        return ms.ToArray();
    }

    private static string DecryptWithBackupPassword(byte[] encrypted, string password)
    {
        try
        {
            using var ms = new MemoryStream(encrypted);
            using var reader = new BinaryReader(ms);

            // Verify magic header
            var magic = reader.ReadBytes(MagicHeader.Length);
            if (!magic.SequenceEqual(MagicHeader))
                return string.Empty;

            // Read version
            var version = reader.ReadInt32();
            if (version != Version)
                return string.Empty;

            // Read salt, nonce, ciphertext, tag
            var salt = reader.ReadBytes(SaltSize);
            var nonce = reader.ReadBytes(NonceSize);
            var ciphertext = reader.ReadBytes((int)(ms.Length - ms.Position - TagSize));
            var tag = reader.ReadBytes(TagSize);

            // Derive key from password
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(32);

            // Decrypt
            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// DTO for host data in backup files. Does NOT use EncryptedStringConverter,
    /// so passwords are plaintext in the JSON (which is then encrypted with backup password).
    /// </summary>
    private class HostDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Plaintext, no converter
        public string Tags { get; set; } = string.Empty;
        public string? DownloadPath { get; set; }
    }

    private class ImportData
    {
        public int Version { get; set; }
        public DateTime ExportDate { get; set; }
        public SettingsData? Settings { get; set; }
        public List<HostDto> Hosts { get; set; } = new();
    }

    private class SettingsData
    {
        public string? Theme { get; set; }
        public string? AccentColor { get; set; }
        public string? Language { get; set; }
        public string? DefaultDownloadPath { get; set; }
        public string? DefaultDataPath { get; set; }
        public bool WindowTopmost { get; set; }
        public string? BackgroundImagePath { get; set; }
        public double BackgroundOpacity { get; set; }
    }
}
