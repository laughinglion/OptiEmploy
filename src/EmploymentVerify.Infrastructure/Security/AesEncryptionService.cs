using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EmploymentVerify.Infrastructure.Security;

public interface IFieldEncryption
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class AesEncryptionService : IFieldEncryption
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:FieldKey"]
            ?? throw new InvalidOperationException("Encryption:FieldKey is not configured.");
        // Derive a 32-byte key from the config value
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Prepend IV to ciphertext, then Base64 encode
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        var fullBytes = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[16];
        var cipher = new byte[fullBytes.Length - 16];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(fullBytes, 16, cipher, 0, cipher.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
