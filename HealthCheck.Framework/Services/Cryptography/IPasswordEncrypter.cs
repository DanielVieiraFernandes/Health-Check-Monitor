namespace HealthCheck.Framework.Services.Cryptography;

public interface IPasswordEncrypter
{
    string Encrypt(string password);
    bool Compare(string password, string passwordHashed);
}
