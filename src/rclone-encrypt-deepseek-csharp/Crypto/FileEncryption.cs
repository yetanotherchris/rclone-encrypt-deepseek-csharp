using LibSodium;

namespace RcloneEncrypt.Crypto;

public static class FileEncryption
{
    private static readonly byte[] Magic = "RCLONE\x00\x00"u8.ToArray();
    private const int MagicLen = 8;
    private const int NonceLen = 24;
    private const int HeaderLen = MagicLen + NonceLen;
    private const int ChunkSize = 64 * 1024;

    public static byte[] EncryptFile(byte[] fileKey, byte[] plaintext)
    {
        using var stream = new MemoryStream();

        stream.Write(Magic);
        var nonce = new byte[NonceLen];
        RandomGenerator.Fill(nonce);
        stream.Write(nonce);

        var offset = 0;
        while (offset < plaintext.Length)
        {
            var chunkLen = Math.Min(ChunkSize, plaintext.Length - offset);
            var chunk = plaintext[offset..(offset + chunkLen)];

            var ciphertextBuf = new byte[chunkLen + SecretBox.MacLen];
            SecretBox.Encrypt(ciphertextBuf, chunk, fileKey, mac: default, nonce: nonce);
            stream.Write(ciphertextBuf, 0, chunkLen + SecretBox.MacLen);

            IncrementNonce(nonce);
            offset += chunkLen;
        }

        return stream.ToArray();
    }

    public static byte[] DecryptFile(byte[] fileKey, byte[] ciphertext)
    {
        for (var i = 0; i < MagicLen; i++)
        {
            if (ciphertext[i] != Magic[i])
                throw new InvalidDataException("Invalid magic bytes - not an rclone encrypted file");
        }

        var nonce = new byte[NonceLen];
        Buffer.BlockCopy(ciphertext, MagicLen, nonce, 0, NonceLen);

        using var output = new MemoryStream();
        var offset = HeaderLen;

        while (offset < ciphertext.Length)
        {
            var remaining = ciphertext.Length - offset;
            var encryptedLen = Math.Min(SecretBox.MacLen + ChunkSize, remaining);
            var encryptedChunk = new byte[encryptedLen];
            Buffer.BlockCopy(ciphertext, offset, encryptedChunk, 0, encryptedLen);

            var plaintextBuf = new byte[encryptedLen - SecretBox.MacLen];
            try
            {
                var result = SecretBox.Decrypt(plaintextBuf, encryptedChunk, fileKey, mac: default, nonce: nonce);
                output.Write(result);
            }
            catch
            {
                throw new InvalidDataException("Decryption failed - wrong password or corrupted data");
            }

            IncrementNonce(nonce);
            offset += encryptedLen;
        }

        return output.ToArray();
    }

    public static byte[] EncryptName(byte[] nameKey, byte[] nameIv, byte[] plaintext)
    {
        var padded = PadPkcs7(plaintext);
        return EmeEncryption.Encrypt(nameKey, nameIv, padded);
    }

    public static byte[] DecryptName(byte[] nameKey, byte[] nameIv, byte[] ciphertext)
    {
        var decrypted = EmeEncryption.Decrypt(nameKey, nameIv, ciphertext);
        return UnpadPkcs7(decrypted);
    }

    private static byte[] PadPkcs7(byte[] data)
    {
        var padLen = 16 - (data.Length % 16);
        var padded = new byte[data.Length + padLen];
        Buffer.BlockCopy(data, 0, padded, 0, data.Length);
        for (var i = data.Length; i < padded.Length; i++)
            padded[i] = (byte)padLen;
        return padded;
    }

    private static byte[] UnpadPkcs7(byte[] data)
    {
        if (data.Length == 0)
            return data;

        var padLen = data[^1];
        if (padLen < 1 || padLen > 16)
            throw new InvalidDataException("Invalid PKCS#7 padding");

        for (var i = data.Length - padLen; i < data.Length; i++)
        {
            if (data[i] != padLen)
                throw new InvalidDataException("Invalid PKCS#7 padding");
        }

        var unpadded = new byte[data.Length - padLen];
        Buffer.BlockCopy(data, 0, unpadded, 0, unpadded.Length);
        return unpadded;
    }

    private static void IncrementNonce(byte[] nonce)
    {
        for (var i = nonce.Length - 1; i >= 0; i--)
        {
            nonce[i]++;
            if (nonce[i] != 0)
                break;
        }
    }
}
