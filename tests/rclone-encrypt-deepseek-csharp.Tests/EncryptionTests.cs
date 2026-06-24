using System.Text;
using RcloneEncrypt.Crypto;

namespace RcloneEncrypt.Tests;

public class EncryptionTests
{
    private const string TestPassword = "Testpassword1";
    private const string TestSalt = "mysalt";
    private const string TestContent = "abandon ability able about above absent absorb abstract absurd abuse access accident account accuse achieve";
    private static readonly byte[] TestContentBytes = Encoding.UTF8.GetBytes(TestContent);

    [Fact]
    public void EncryptDecrypt_RoundTrip_Success()
    {
        var keyMaterial = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var encrypted = FileEncryption.EncryptFile(keyMaterial.FileKey, TestContentBytes);
        var decrypted = FileEncryption.DecryptFile(keyMaterial.FileKey, encrypted);

        Assert.Equal(TestContentBytes, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithSalt_RoundTrip_Success()
    {
        var keyMaterial = KeyDerivation.DeriveKeyMaterial(TestPassword, TestSalt);
        var encrypted = FileEncryption.EncryptFile(keyMaterial.FileKey, TestContentBytes);
        var decrypted = FileEncryption.DecryptFile(keyMaterial.FileKey, encrypted);

        Assert.Equal(TestContentBytes, decrypted);
    }

    [Fact]
    public void DifferentSalts_ProduceDifferentCiphertexts()
    {
        var km1 = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var km2 = KeyDerivation.DeriveKeyMaterial(TestPassword, TestSalt);

        Assert.NotEqual(km1.FileKey, km2.FileKey);
        Assert.NotEqual(km1.NameKey, km2.NameKey);
        Assert.NotEqual(km1.NameIv, km2.NameIv);
    }

    [Fact]
    public void WrongPassword_FailsDecryption()
    {
        var keyMaterial = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var encrypted = FileEncryption.EncryptFile(keyMaterial.FileKey, TestContentBytes);

        var wrongKey = KeyDerivation.DeriveKeyMaterial("wrongpassword", null);

        Assert.Throws<InvalidDataException>(() =>
            FileEncryption.DecryptFile(wrongKey.FileKey, encrypted));
    }

    [Fact]
    public void FileHasRcloneMagicHeader()
    {
        var keyMaterial = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var encrypted = FileEncryption.EncryptFile(keyMaterial.FileKey, TestContentBytes);

        var magic = "RCLONE\x00\x00"u8;
        for (var i = 0; i < 8; i++)
            Assert.Equal(magic[i], encrypted[i]);
    }

    [Theory]
    [InlineData("TEST_FILE.txt", FilenameEncoding.Base32)]
    [InlineData("TEST_FILE.txt", FilenameEncoding.Base64)]
    [InlineData("hello-world.txt", FilenameEncoding.Base32)]
    [InlineData("hello-world.txt", FilenameEncoding.Base64)]
    public void EncryptDecryptFilename_RoundTrip_Success(string filename, FilenameEncoding encoding)
    {
        var keyMaterial = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var plainBytes = Encoding.UTF8.GetBytes(filename);

        var encryptedNameBytes = FileEncryption.EncryptName(keyMaterial.NameKey, keyMaterial.NameIv, plainBytes);
        var encryptedStr = FilenameEncodingHelper.Encode(encryptedNameBytes, encoding);

        var decodedBytes = FilenameEncodingHelper.Decode(encryptedStr, encoding);
        var decryptedBytes = FileEncryption.DecryptName(keyMaterial.NameKey, keyMaterial.NameIv, decodedBytes);
        var decryptedStr = Encoding.UTF8.GetString(decryptedBytes);

        Assert.Equal(filename, decryptedStr);
    }

    [Fact]
    public void FilenameEncoding_Base32_ProducesLowercaseNoPadding()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
        var encoded = FilenameEncodingHelper.Encode(data, FilenameEncoding.Base32);

        Assert.Matches("^[a-z2-7]+$", encoded);
        Assert.DoesNotContain("=", encoded);
    }

    [Fact]
    public void FilenameEncoding_Base64_ProducesNoPadding()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };
        var encoded = FilenameEncodingHelper.Encode(data, FilenameEncoding.Base64);

        Assert.DoesNotContain("=", encoded);
    }

    [Fact]
    public void FilenameEncoding_DecodeMatchesEncode()
    {
        var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0xFF, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };

        var b32encoded = FilenameEncodingHelper.Encode(original, FilenameEncoding.Base32);
        var b32decoded = FilenameEncodingHelper.Decode(b32encoded, FilenameEncoding.Base32);
        Assert.Equal(original, b32decoded);

        var b64encoded = FilenameEncodingHelper.Encode(original, FilenameEncoding.Base64);
        var b64decoded = FilenameEncodingHelper.Decode(b64encoded, FilenameEncoding.Base64);
        Assert.Equal(original, b64decoded);
    }

    [Fact]
    public void EncryptDecrypt_WithPasswordFlag_MatchesInteractive()
    {
        var keyMaterial1 = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var keyMaterial2 = KeyDerivation.DeriveKeyMaterial(TestPassword, null);

        Assert.Equal(keyMaterial1.FileKey, keyMaterial2.FileKey);
        Assert.Equal(keyMaterial1.NameKey, keyMaterial2.NameKey);
        Assert.Equal(keyMaterial1.NameIv, keyMaterial2.NameIv);
    }

    [Fact]
    public void LargeContent_EncryptDecrypt_RoundTrip()
    {
        var largeContent = new byte[1024 * 1024];
        new Random(42).NextBytes(largeContent);

        var keyMaterial = KeyDerivation.DeriveKeyMaterial(TestPassword, null);
        var encrypted = FileEncryption.EncryptFile(keyMaterial.FileKey, largeContent);
        var decrypted = FileEncryption.DecryptFile(keyMaterial.FileKey, encrypted);

        Assert.Equal(largeContent, decrypted);
    }
}
