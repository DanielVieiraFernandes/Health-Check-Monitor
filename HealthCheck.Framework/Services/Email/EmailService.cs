namespace HealthCheck.Framework.Services.Email;

public class EmailService
{
    //public void ExemploEnvioEmailMailKit()
    //{
    // 1. Montagem da Mensagem (usando MimeKit)
    //var message = new MimeMessage();
    //message.From.Add(new MailboxAddress("Sistema de Alertas", "USERNAME"));

    // Destinatário
    //message.To.Add(new MailboxAddress("Daniel Vieira", "daniel@danielvieiradev.com.br"));

    //message.Subject = "⚠️ Nova Exceção Capturada no Sistema";

    //// Corpo do E-mail
    //message.Body = new TextPart("plain")
    //{
    //    Text = $"Uma nova exceção ocorreu na aplicação:\n\n{mensagemErro}"
    //};

    // 2. Conexão e Disparo (usando MailKit)
    //using var client = new SmtpClient();

    // Conecta ao servidor (SecureSocketOptions.SslOnConnect é o padrão seguro para porta 465)
    //await client.ConnectAsync("HOST", "PORT", SecureSocketOptions.SslOnConnect);

    // Autentica com as credenciais seguras
    //await client.AuthenticateAsync("USERNAME", "PASSWORD");

    // Envia
    //await client.SendAsync(message);

    // Desconecta de forma limpa
    //await client.DisconnectAsync(true);

    //Console.WriteLine("Alerta via MailKit enviado com sucesso!");
    //}

}
