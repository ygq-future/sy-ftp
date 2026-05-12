using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using sy_ftp.Helpers;

namespace sy_ftp.Models;

/// <summary>
/// Serializes a string as its encrypted form. In memory the property stays plaintext,
/// so consumers (FtpService) use it as before. Legacy plaintext on disk is auto-upgraded
/// on the next save because Unprotect() passes unknown-prefix values through unchanged.
/// </summary>
public sealed class EncryptedStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        return SecretProtector.Unprotect(raw);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(SecretProtector.Protect(value));
    }
}
