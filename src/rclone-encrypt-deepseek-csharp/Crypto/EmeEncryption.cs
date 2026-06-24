using System.Security.Cryptography;

namespace RcloneEncrypt.Crypto;

public static class EmeEncryption
{
    private const int BlockSize = 16;

    public static byte[] Encrypt(byte[] key, byte[] tweak, byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var encryptor = aes.CreateEncryptor();
        return Transform(encryptor, aes, tweak, plaintext, true);
    }

    public static byte[] Decrypt(byte[] key, byte[] tweak, byte[] ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        var decryptor = aes.CreateDecryptor();
        return Transform(decryptor, aes, tweak, ciphertext, false);
    }

    private static byte[] Transform(ICryptoTransform transform, Aes aes, byte[] tweak, byte[] input, bool encrypt)
    {
        var m = input.Length / BlockSize;
        var output = new byte[input.Length];

        var lTable = TabulateL(aes, m);

        var ppj = new byte[BlockSize];
        for (var j = 0; j < m; j++)
        {
            Buffer.BlockCopy(input, j * BlockSize, ppj, 0, BlockSize);
            XorBlocks(ppj, lTable[j]);
            transform.TransformBlock(ppj, 0, BlockSize, output, j * BlockSize);
        }

        var mp = new byte[BlockSize];
        Buffer.BlockCopy(output, 0, mp, 0, BlockSize);
        XorBlocks(mp, tweak);
        for (var j = 1; j < m; j++)
        {
            var block = new byte[BlockSize];
            Buffer.BlockCopy(output, j * BlockSize, block, 0, BlockSize);
            XorBlocks(mp, block);
        }

        var mc = new byte[BlockSize];
        transform.TransformBlock(mp, 0, BlockSize, mc, 0);

        var mBlock = new byte[BlockSize];
        Buffer.BlockCopy(mp, 0, mBlock, 0, BlockSize);
        XorBlocks(mBlock, mc);

        var cccj = new byte[BlockSize];
        for (var j = 1; j < m; j++)
        {
            MultByTwo(mBlock, mBlock);
            Buffer.BlockCopy(output, j * BlockSize, cccj, 0, BlockSize);
            XorBlocks(cccj, mBlock);
            Buffer.BlockCopy(cccj, 0, output, j * BlockSize, BlockSize);
        }

        var ccc1 = new byte[BlockSize];
        Buffer.BlockCopy(mc, 0, ccc1, 0, BlockSize);
        XorBlocks(ccc1, tweak);
        for (var j = 1; j < m; j++)
        {
            var block = new byte[BlockSize];
            Buffer.BlockCopy(output, j * BlockSize, block, 0, BlockSize);
            XorBlocks(ccc1, block);
        }
        Buffer.BlockCopy(ccc1, 0, output, 0, BlockSize);

        var tempBlock = new byte[BlockSize];
        for (var j = 0; j < m; j++)
        {
            Buffer.BlockCopy(output, j * BlockSize, tempBlock, 0, BlockSize);
            transform.TransformBlock(tempBlock, 0, BlockSize, output, j * BlockSize);
            Buffer.BlockCopy(output, j * BlockSize, tempBlock, 0, BlockSize);
            XorBlocks(tempBlock, lTable[j]);
            Buffer.BlockCopy(tempBlock, 0, output, j * BlockSize, BlockSize);
        }

        return output;
    }

    private static byte[][] TabulateL(Aes aes, int m)
    {
        var eZero = new byte[BlockSize];
        var li = new byte[BlockSize];
        using var enc = aes.CreateEncryptor();
        enc.TransformBlock(eZero, 0, BlockSize, li, 0);

        var lTable = new byte[m][];
        for (var i = 0; i < m; i++)
        {
            MultByTwo(li, li);
            lTable[i] = new byte[BlockSize];
            Buffer.BlockCopy(li, 0, lTable[i], 0, BlockSize);
        }
        return lTable;
    }

    private static void MultByTwo(byte[] outBytes, byte[] inBytes)
    {
        var tmp = new byte[BlockSize];
        tmp[0] = (byte)(2 * inBytes[0]);
        tmp[0] ^= (byte)(135 & (byte)(-(inBytes[15] >> 7)));
        for (var j = 1; j < BlockSize; j++)
        {
            tmp[j] = (byte)(2 * inBytes[j]);
            tmp[j] += (byte)(inBytes[j - 1] >> 7);
        }
        Buffer.BlockCopy(tmp, 0, outBytes, 0, BlockSize);
    }

    private static void XorBlocks(byte[] target, byte[] source)
    {
        for (var i = 0; i < BlockSize; i++)
            target[i] ^= source[i];
    }
}
