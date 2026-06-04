using HealthCheck.Framework.Services.Cryptography;
using Xunit;

namespace HealthCheck.Tests.Cryptography;

public class BcryptPasswordEncrypterTests
{
    private readonly IPasswordEncrypter _encrypter = new BcryptPasswordEncrypter();

    [Fact]
    public void Encrypt_NaoDeveSerIgualASenhaOriginal()
    {
        var senha = "Senha@123";
        var hash = _encrypter.Encrypt(senha);

        Assert.NotEqual(senha, hash);
        Assert.StartsWith("$2", hash); // prefixo BCrypt
    }

    [Fact]
    public void Compare_SenhaCorreta_DeveRetornarTrue()
    {
        var senha = "MinhaSenhaForte!";
        var hash = _encrypter.Encrypt(senha);

        var resultado = _encrypter.Compare(senha, hash);
        Assert.True(resultado);
    }

    [Fact]
    public void Compare_SenhaIncorreta_DeveRetornarFalse()
    {
        var senha = "SenhaOriginal";
        var hash = _encrypter.Encrypt(senha);

        var resultado = _encrypter.Compare("SenhaDiferente", hash);
        Assert.False(resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("12345678")]
    [InlineData("!@#$%¨&*()_+")]
    [InlineData("uma senha bem longa com espacos e acentuacao áéíóú")]
    public void EncryptECompare_ComDiferentesSenhas_DeveFuncionar(string senha)
    {
        var hash = _encrypter.Encrypt(senha);
        Assert.True(_encrypter.Compare(senha, hash));
    }

    [Fact]
    public void Encrypt_DuasExecucoesMesmaSenha_DevemGerarHashesDiferentes()
    {
        var senha = "MesmaSenha";
        var hash1 = _encrypter.Encrypt(senha);
        var hash2 = _encrypter.Encrypt(senha);

        Assert.NotEqual(hash1, hash2); // salt aleatório
        Assert.True(_encrypter.Compare(senha, hash1));
        Assert.True(_encrypter.Compare(senha, hash2));
    }

    [Fact]
    public void Compare_HashVazio_DeveLancarArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _encrypter.Compare("qualquer", ""));
    }
}
