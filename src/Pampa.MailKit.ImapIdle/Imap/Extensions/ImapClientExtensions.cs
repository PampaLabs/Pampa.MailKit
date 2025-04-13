namespace MailKit.Net.Imap;

/// <summary>
/// Extension methods for <see cref="T:MailKit.Net.ImapClient" />.
/// </summary>
public static class ImapClientExtensions
{
    /// <summary>
    /// Asynchronously establish a connection to the specified IMAP server.
    /// </summary>
    /// <param name="client">The IMAP client.</param>
    /// <param name="server">The mail server.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task ConnectAsync(this ImapClient client, MailServer server, CancellationToken cancellationToken = default)
        => await client.ConnectAsync(server.Host, server.Port, server.SecureSocket, cancellationToken);

    /// <summary>
    /// Asynchronously authenticate using the specified user name and password.
    /// </summary>
    /// <param name="client">The IMAP client.</param>
    /// <param name="credential"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task AuthenticateAsync(this ImapClient client, MailCredential credential, CancellationToken cancellationToken = default)
        => await client.AuthenticateAsync(credential.Username, credential.Password, cancellationToken);

    /// <summary>
    /// Subscribes to incoming messages on the IMAP client and invokes
    /// the provided message handler when a new message is received.
    /// </summary>
    /// <param name="client">The IMAP client.</param>
    /// <param name="options">The IMAP client options.</param>
    /// <param name="messageHandler">The handler that will be invoked for each received message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="IAsyncDisposable"/> that can be used to unsubscribe from the message reception.
    /// </returns>
    public static async Task<IAsyncDisposable> IdleSubscribeAsync(this ImapClient client, ImapClientOptions options, MessageHandler messageHandler, CancellationToken cancellationToken = default)
    {
        var receiver = new ImapClientReceiverFactory().Create(client, options);
        var subscription = await receiver.SubscribeAsync(messageHandler, cancellationToken);
        return subscription;
    }
}