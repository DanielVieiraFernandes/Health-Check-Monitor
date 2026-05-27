namespace HealthCheck.Framework.Services.Email.Models;

public class EmailBody
{
    /// <summary>
    /// Destinatário principal
    /// </summary>
    public string To { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Destinatários de cópia
    /// </summary>
    public List<string> Cc { get; set; } = [];

    /// <summary>
    /// Destinatários de cópia oculta - Bcc significa "Blind Carbon Copy", ou seja, cópia oculta. Os destinatários listados em Bcc receberão o e-mail,
    /// mas seus endereços não serão visíveis para os outros destinatários (To e Cc).
    /// Isso é útil para enviar e-mails para múltiplos destinatários sem expor suas informações de contato uns aos outros.
    /// </summary>
    public List<string> Bcc { get; set; } = [];

    /// <summary>
    /// Assunto do e-mail
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<EmailAttachment> Attachments { get; set; } = [];
}
