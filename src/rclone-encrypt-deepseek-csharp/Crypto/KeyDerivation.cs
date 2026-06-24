using System.Text;
using Norgerman.Cryptography.Scrypt;

namespace RcloneEncrypt.Crypto;

public static class KeyDerivation
{
    private static readonly byte[] DefaultSalt =
    [
        0xA8, 0x0D, 0xF4, 0x3A, 0x8F, 0xBD, 0x03, 0x08,
        0xA7, 0xCA, 0xB8, 0x3E, 0x58, 0x1F, 0x86, 0xB1
    ];

    private const int N = 16384;
    private const int R = 8;
    private const int P = 1;
    private const int DkLen = 80;

    public static KeyMaterial DeriveKeyMaterial(string password, string? salt)
    {
        var saltBytes = string.IsNullOrEmpty(salt) ? DefaultSalt : Encoding.UTF8.GetBytes(salt);

        var keyMaterial = ScryptUtil.Scrypt(password, saltBytes, N, R, P, DkLen);

        var fileKey = new byte[32];
        var nameKey = new byte[32];
        var nameIv = new byte[16];

        Buffer.BlockCopy(keyMaterial, 0, fileKey, 0, 32);
        Buffer.BlockCopy(keyMaterial, 32, nameKey, 0, 32);
        Buffer.BlockCopy(keyMaterial, 64, nameIv, 0, 16);

        return new KeyMaterial(fileKey, nameKey, nameIv);
    }
}

public readonly record struct KeyMaterial(byte[] FileKey, byte[] NameKey, byte[] NameIv);
