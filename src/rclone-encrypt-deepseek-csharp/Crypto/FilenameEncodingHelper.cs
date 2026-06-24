using System.Text;

namespace RcloneEncrypt.Crypto;

public enum FilenameEncoding
{
    Base32,
    Base64
}

public static class FilenameEncodingHelper
{
    private const string Base32Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

    public static string Encode(byte[] data, FilenameEncoding encoding)
    {
        return encoding switch
        {
            FilenameEncoding.Base32 => RcloneBase32Encode(data),
            FilenameEncoding.Base64 => RcloneBase64Encode(data),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    public static byte[] Decode(string encoded, FilenameEncoding encoding)
    {
        return encoding switch
        {
            FilenameEncoding.Base32 => RcloneBase32Decode(encoded),
            FilenameEncoding.Base64 => RcloneBase64Decode(encoded),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    public static FilenameEncoding Parse(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "base32" => FilenameEncoding.Base32,
            "base64" => FilenameEncoding.Base64,
            null => FilenameEncoding.Base32,
            _ => throw new ArgumentException($"Unknown filename encoding: {value}. Supported: base32, base64")
        };
    }

    private static string RcloneBase32Encode(byte[] data)
    {
        var byteLen = data.Length;
        var charLen = (byteLen * 8 + 4) / 5;
        var result = new char[charLen];

        var buffer = 0;
        var bitsRemaining = 0;
        var charIndex = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsRemaining += 8;

            while (bitsRemaining >= 5)
            {
                bitsRemaining -= 5;
                var index = (buffer >> bitsRemaining) & 0x1F;
                result[charIndex++] = Base32Alphabet[index];
            }
        }

        if (bitsRemaining > 0)
        {
            var index = (buffer << (5 - bitsRemaining)) & 0x1F;
            result[charIndex++] = Base32Alphabet[index];
        }

        return new string(result);
    }

    private static byte[] RcloneBase32Decode(string encoded)
    {
        var charLen = encoded.Length;
        var byteLen = charLen * 5 / 8;
        var result = new byte[byteLen];

        var buffer = 0;
        var bitsRemaining = 0;
        var byteIndex = 0;

        foreach (var c in encoded)
        {
            buffer = (buffer << 5) | CharToValue(c);
            bitsRemaining += 5;

            if (bitsRemaining >= 8)
            {
                bitsRemaining -= 8;
                if (byteIndex < byteLen)
                    result[byteIndex++] = (byte)((buffer >> bitsRemaining) & 0xFF);
            }
        }

        return result;
    }

    private static int CharToValue(char c)
    {
        if (c >= 'a' && c <= 'z')
            return c - 'a';
        if (c >= '2' && c <= '7')
            return c - '2' + 26;
        throw new ArgumentException($"Invalid base32 character: {c}");
    }

    private static string RcloneBase64Encode(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=');
    }

    private static byte[] RcloneBase64Decode(string encoded)
    {
        var padding = (4 - (encoded.Length % 4)) % 4;
        var padded = encoded + new string('=', padding);
        return Convert.FromBase64String(padded);
    }
}
