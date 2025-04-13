using MailKit.Security;

namespace MailKit;

/// <summary>
/// Represents mail server
/// </summary>
public class MailServer
{
    /// <summary>
    /// The secure socket options to when connecting.
    /// </summary>
    public SecureSocketOptions SecureSocket { get; set; } = SecureSocketOptions.Auto;

    /// <summary>
    /// The host name to connect to.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// The port to connect to. If the specified port is 0, then the default port will be used.
    /// </summary>
    public int Port { get; set; }
}