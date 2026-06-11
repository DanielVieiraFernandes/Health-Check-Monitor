using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace HealthCheck.Framework.Services.Cryptography;

public class SMTPCredentialProvider : ISMTPCredentialProvider
{
    /// <summary>
    ///Tamanho da chave AES-256: 32 bytes (256 bits).
    /// </summary>
    private const int KeySizeInBytes = 32;

    /// <summary>
    ///Nonce recomendado para GCM: 12 bytes.
    /// </summary>
    private const int NonceSizeInBytes = 12;

    /// <summary>
    ///Tag de autenticação (integridade + autenticidade): 16 bytes.
    /// </summary>
    private const int TagSizeInBytes = 16;

    /// <summary>
    ///Chave simétrica carregada da configuração
    /// </summary>
    private readonly byte[] _key;

    public SMTPCredentialProvider(IConfiguration configuration)
    {
        var keyBase64 = configuration["SmtpAes256GcmKey"];

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Sem chave válida não é possível criptografar/descriptografar com segurança.
        //Então, lanço uma exceção para encerrar a aplicação e notificar o dev para corrigir
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (string.IsNullOrWhiteSpace(keyBase64))
            throw new InvalidOperationException("A chave de criptografia SMTP não foi configurada.");

        try
        {
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // A chave vem em Base64 para facilitar armazenamento em configuração textual.
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            _key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("A chave de criptografia SMTP deve estar em Base64 válido.", ex);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Garante que a chave tenha exatamente 32 bytes para AES-256.
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (_key.Length != KeySizeInBytes)
            throw new InvalidOperationException("A chave de criptografia SMTP deve ter 32 bytes (AES-256).");
    }

    public string Decrypt(string cipherText)
    {
        //Evita processamento de entrada inválida.
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("O texto cifrado não pode ser nulo ou vazio.", nameof(cipherText));

        byte[] payload;

        try
        {
            //O payload recebido é Base64 no formato: nonce + tag + ciphertext.
            payload = Convert.FromBase64String(cipherText);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("O texto cifrado informado não está em formato válido.", ex);
        }

        //Precisa conter ao menos nonce + tag + algum conteúdo criptografado.
        if (payload.Length <= NonceSizeInBytes + TagSizeInBytes)
            throw new InvalidOperationException("Payload cifrado inválido para essa criptografia.");

        //Extrai cada parte do payload para a operação de decrypt no GCM.
        var nonce = payload[..NonceSizeInBytes];
        var tag = payload[NonceSizeInBytes..(NonceSizeInBytes + TagSizeInBytes)];
        var ciphertextBytes = payload[(NonceSizeInBytes + TagSizeInBytes)..];

        var plainTextBytes = new byte[ciphertextBytes.Length];

        //AES-GCM valida a tag automaticamente; se houver alteração no payload, a operação falha.
        using var aesGcm = new AesGcm(_key, TagSizeInBytes);
        aesGcm.Decrypt(nonce, ciphertextBytes, tag, plainTextBytes);

        //Converte os bytes descriptografados para string UTF-8.
        return Encoding.UTF8.GetString(plainTextBytes);
    }

    public string Encrypt(string plainText)
    {
        //Evita criptografar conteúdo inválido.
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("O texto plano não pode ser nulo ou vazio.", nameof(plainText));

        //Converte texto para bytes antes da criptografia.
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

        //Nonce aleatório e único por operação (requisito de segurança do GCM).
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
        var ciphertextBytes = new byte[plainTextBytes.Length];
        var tag = new byte[TagSizeInBytes];

        // Criptografa e gera tag de autenticação.
        using var aesGcm = new AesGcm(_key, TagSizeInBytes);
        aesGcm.Encrypt(nonce, plainTextBytes, ciphertextBytes, tag);

        // Formato persistido/transmitido: nonce + tag + ciphertext.
        // Isso permite descriptografar depois sem metadados externos.
        var payload = new byte[NonceSizeInBytes + TagSizeInBytes + ciphertextBytes.Length];

        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSizeInBytes);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeInBytes, TagSizeInBytes);
        Buffer.BlockCopy(ciphertextBytes, 0, payload, NonceSizeInBytes + TagSizeInBytes, ciphertextBytes.Length);

        //Base64 para facilitar armazenamento em banco/config/API sem perda binária.
        return Convert.ToBase64String(payload);
    }
}
