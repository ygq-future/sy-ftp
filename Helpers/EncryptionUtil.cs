using System.Security.Cryptography;
using System.Text;

namespace sy_ftp.Helpers;

/// <summary>
/// Production-grade encryption utility using AES-256-GCM with PBKDF2 key derivation.
/// </summary>
public static class EncryptionUtil
{
    private const int SaltSize = 32; // 256 bits
    private const int NonceSize = 12; // 96 bits (recommended for GCM)
    private const int TagSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 100_000; // PBKDF2 iterations

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM with a password-derived key.
    /// </summary>
    /// <param name="plaintext">The text to encrypt</param>
    /// <param name="password">The password for encryption</param>
    /// <returns>Base64-encoded encrypted data (salt + nonce + ciphertext + tag)</returns>
    public static string Encrypt(string plaintext, string password)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        // Generate random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Derive key from password
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        // Generate random nonce
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        // Encrypt
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine: salt + nonce + ciphertext + tag
        var result = new byte[SaltSize + NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, SaltSize + NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize + ciphertext.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts ciphertext using AES-256-GCM with a password-derived key.
    /// </summary>
    /// <param name="ciphertext">Base64-encoded encrypted data</param>
    /// <param name="password">The password for decryption</param>
    /// <returns>Decrypted plaintext, or empty string if decryption fails</returns>
    public static string Decrypt(string ciphertext, string password)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return string.Empty;

        try
        {
            var data = Convert.FromBase64String(ciphertext);

            // Minimum size check
            if (data.Length < SaltSize + NonceSize + TagSize)
                return string.Empty;

            // Extract components
            var salt = new byte[SaltSize];
            var nonce = new byte[NonceSize];
            var encryptedLength = data.Length - SaltSize - NonceSize - TagSize;
            var encrypted = new byte[encryptedLength];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(data, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(data, SaltSize, nonce, 0, NonceSize);
            Buffer.BlockCopy(data, SaltSize + NonceSize, encrypted, 0, encryptedLength);
            Buffer.BlockCopy(data, SaltSize + NonceSize + encryptedLength, tag, 0, TagSize);

            // Derive key from password
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            // Decrypt
            var plaintext = new byte[encrypted.Length];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, encrypted, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return string.Empty;
        }
    }
}
