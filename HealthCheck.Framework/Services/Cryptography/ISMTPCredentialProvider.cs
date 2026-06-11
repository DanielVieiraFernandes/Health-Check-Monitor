namespace HealthCheck.Framework.Services.Cryptography;

public interface ISMTPCredentialProvider
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
