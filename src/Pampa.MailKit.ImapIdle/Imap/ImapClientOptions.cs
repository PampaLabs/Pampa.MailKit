namespace MailKit.Net.Imap;

/// <summary>
/// Represents IMAP client options.
/// </summary>
public class ImapClientOptions
{
    /// <summary>
    /// Mail credentials
    /// </summary>
    public MailCredential? Credential { get; set; }

    /// <summary>
    /// Mail server
    /// </summary>
    public MailServer? Server { get; set; }
}