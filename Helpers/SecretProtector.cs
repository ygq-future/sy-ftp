using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace sy_ftp.Helpers;

/// <summary>
/// AES-256-GCM protection for sensitive strings (currently: host passwords).
/// Key: 32 random bytes in %LocalAppData%/SY-FTP/key.bin.
/// Windows: DPAPI-wrapped (CurrentUser). macOS/Linux: file mode 0600.
/// Values are tagged with "enc.v1:" prefix so legacy plaintext is auto-detected.
/// </summary>
internal static class SecretProtector
{
    private const string Prefix = "enc.v1:";
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly object _lock = new();
    private static byte[]? _key;

    private static readonly string KeyFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SY-FTP", "key.bin");

    public static string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;

        var key = GetKey();
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var gcm = new AesGcm(key, TagSize);
        gcm.Encrypt(nonce, plain, cipher, tag);

        var blob = new byte[NonceSize + cipher.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, blob, 0, NonceSize);
        Buffer.BlockCopy(cipher, 0, blob, NonceSize, cipher.Length);
        Buffer.BlockCopy(tag, 0, blob, NonceSize + cipher.Length, TagSize);
        return Prefix + Convert.ToBase64String(blob);
    }

    public static string Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
            return value;

        try
        {
            var blob = Convert.FromBase64String(value[Prefix.Length..]);
            if (blob.Length < NonceSize + TagSize) return string.Empty;

            var key = GetKey();
            var nonce = blob.AsSpan(0, NonceSize);
            var tag = blob.AsSpan(blob.Length - TagSize, TagSize);
            var cipher = blob.AsSpan(NonceSize, blob.Length - NonceSize - TagSize);
            var plain = new byte[cipher.Length];

            using var gcm = new AesGcm(key, TagSize);
            gcm.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte[] GetKey()
    {
        if (_key is not null) return _key;
        lock (_lock)
        {
            _key ??= LoadOrCreateKey();
            return _key;
        }
    }

    private static byte[] LoadOrCreateKey()
    {
        var dir = Path.GetDirectoryName(KeyFile)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(KeyFile))
            return UnwrapKey(File.ReadAllBytes(KeyFile));

        var key = RandomNumberGenerator.GetBytes(KeySize);
        File.WriteAllBytes(KeyFile, WrapKey(key));
        RestrictPermissions(KeyFile);
        return key;
    }

    private static byte[] WrapKey(byte[] key)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
        return key;
    }

    private static byte[] UnwrapKey(byte[] raw)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                return ProtectedData.Unprotect(raw, null, DataProtectionScope.CurrentUser);
            }
            catch when (raw.Length == KeySize)
            {
                return raw;
            }
        }
        if (raw.Length != KeySize)
            throw new InvalidDataException("Corrupt key file.");
        return raw;
    }

    private static void RestrictPermissions(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch { }
    }
}
