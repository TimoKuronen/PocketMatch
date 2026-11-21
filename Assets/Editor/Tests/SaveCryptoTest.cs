using System;
using NUnit.Framework;

/// <summary>
/// Edit Mode coverage for the local save encryption round trip.
/// </summary>
public class SaveCryptoTest
{
    private const string Key = "0123456789abcdef0123456789abcdef";
    private const string OtherKey = "fedcba9876543210fedcba9876543210";

    [Test]
    public void EncryptThenDecrypt_ReturnsOriginalText()
    {
        const string plain = "{\"coins\": 120, \"nextLevelIndex\": 3}";

        string cipher = SaveCrypto.Encrypt(plain, Key);

        Assert.AreNotEqual(plain, cipher);
        Assert.AreEqual(plain, SaveCrypto.Decrypt(cipher, Key));
    }

    [Test]
    public void Decrypt_WithDifferentKey_DoesNotReturnOriginalText()
    {
        const string plain = "{\"coins\": 120}";
        string cipher = SaveCrypto.Encrypt(plain, Key);

        string result = null;
        try
        {
            result = SaveCrypto.Decrypt(cipher, OtherKey);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
        }

        Assert.AreNotEqual(plain, result);
    }

    [Test]
    public void Decrypt_NonBase64Text_Throws()
    {
        Assert.Throws<FormatException>(() => SaveCrypto.Decrypt("!!!", Key));
    }

    [Test]
    public void Decrypt_TextThatIsNotACipherBlock_Throws()
    {
        Assert.Catch<Exception>(() => SaveCrypto.Decrypt("bm90IGEgc2F2ZQ==", Key));
    }
}
