namespace HealthCheck.Framework.Services.Cryptography;

public class BcryptPasswordEncrypter : IPasswordEncrypter
{
    //==================================================================================================
    // Implementação de hash de senha com BCrypt (padrão recomendado para senhas).
    //==================================================================================================
    // Utiliza BCrypt para gerar um hash seguro para a senha, conforme recomendação comum para autenticação.
    public string Encrypt(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(8));

    // Verifica se a senha informada corresponde ao hash armazenado.
    public bool Compare(string password, string passwordHashed)
        => BCrypt.Net.BCrypt.Verify(password, passwordHashed);
}
