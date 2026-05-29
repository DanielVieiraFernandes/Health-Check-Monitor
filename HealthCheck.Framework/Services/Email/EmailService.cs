using FluentValidation.Results;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Email.Models;
using MailKit.Net.Smtp;
using MimeKit;
using System.Net;

namespace HealthCheck.Framework.Services.Email;

public class EmailService
{
    private readonly ISMTPCredentialProvider _SMTPCredentialProvider;
    private readonly EmailCredentials _credentials;
    private readonly SemaphoreSlim _smtpLock = new(1, 1);
    private readonly SmtpClient _smtpClient = new();
    private readonly string _email;
    private readonly string _password;

    private const int MaxRetryAttempts = 3;

    public EmailService(EmailCredentials credentials, ISMTPCredentialProvider SMTPCredentialProvider)
    {
        _credentials = credentials;
        _SMTPCredentialProvider = SMTPCredentialProvider;

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // DESCRIPTOGRAFO UMA ÚNICA VEZ AS CREDENCIAIS PARA EVITAR CUSTO REPETIDO EM CADA DISPARO
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        _email = _SMTPCredentialProvider.Decrypt(_credentials.Email);
        _password = _SMTPCredentialProvider.Decrypt(_credentials.Password);
    }

    public virtual async Task<Result<object>> SendSystemEmail(EmailBody emailBody, CancellationToken cancellationToken = default)
    {
        if (emailBody is null)
            throw new ArgumentNullException(nameof(emailBody));

        var lockAcquired = false;

        try
        {
            await _smtpLock.WaitAsync(cancellationToken);
            lockAcquired = true;

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    await EnsureConnectedAndAuthenticatedAsync(cancellationToken);

                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    // ADICIONO AS CONFIGURAÇÕES DE REMETENTE, DESTINATÁRIO, ASSUNTO E CORPO DO E-MAIL
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("SISTEMA DE MONITORAMENTO", _email));
                    message.To.Add(new MailboxAddress(emailBody.Name, emailBody.To));
                    message.Subject = emailBody.Subject;

                    message.Body = new TextPart(emailBody.IsHtml ? "html" : "plain")
                    {
                        Text = emailBody.Body
                    };

                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    // COM O CLIENTE REUTILIZADO E AUTENTICADO, APENAS DISPARO O E-MAIL
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    await _smtpClient.SendAsync(message, cancellationToken);

                    return Result<object>.AsSuccess(new { });
                }
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // SE FOR UMA EXCEÇÃO DE OPERAÇÃO CANCELADA, NÃO FAÇO RETENTATIVA E LANÇO A EXCEÇÃO PARA O CHAMADOR TRATAR
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                catch (OperationCanceledException)
                {
                    throw;
                }
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // SE FOR UMA EXCEÇÃO DE CONEXÃO, TENTO RECONECTAR E REAUTENTICAR ANTES DE NOVA TENTATIVA
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                catch (Exception ex) when (attempt < MaxRetryAttempts && IsTransient(ex))
                {
                    await SafeDisconnectAsync();
                    var backoffDelay = TimeSpan.FromMilliseconds(200 * attempt);
                    await Task.Delay(backoffDelay, cancellationToken);
                }
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // SE FOR UMA EXCEÇÃO IRRECUPERÁVEL OU ULTRAPASSOU AS TENTATIVAS, RETORNO O ERRO
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                catch (Exception ex)
                {
                    await SafeDisconnectAsync();
                    return BuildFailure(ex.Message);
                }

            }
        }
        finally
        {
            if (lockAcquired)
                _smtpLock.Release();
        }

        return BuildFailure("Não foi possível enviar o e-mail após múltiplas tentativas.");
    }

    private async Task EnsureConnectedAndAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!_smtpClient.IsConnected)
        {
            await _smtpClient.ConnectAsync(_credentials.Host, _credentials.Port, _credentials.EnableSSL, cancellationToken);
        }

        if (!_smtpClient.IsAuthenticated)
        {
            await _smtpClient.AuthenticateAsync(_email, _password, cancellationToken);
        }
    }

    private static bool IsTransient(Exception exception)
        => exception is SmtpCommandException
        || exception is SmtpProtocolException
        || exception is IOException
        || exception is TimeoutException;

    private async Task SafeDisconnectAsync()
    {
        if (_smtpClient.IsConnected)
        {
            await _smtpClient.DisconnectAsync(true);
        }
    }

    private static Result<object> BuildFailure(string message)
    {
        var validationResult = new ValidationResult([
            new ValidationFailure("Email", message)
        ]);

        return Result<object>.AsFailure(new Failure(HttpStatusCode.ServiceUnavailable, validationResult));
    }


}
