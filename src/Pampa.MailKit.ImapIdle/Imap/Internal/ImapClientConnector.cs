namespace MailKit.Net.Imap;

internal class ImapClientConnector
{
    private readonly ImapClientOptions _options;

    public ImapClientConnector(ImapClientOptions options)
    {
        _options = options;
    }

    public async Task ConnectAsync(ImapClient client, CancellationToken cancellationToken = default)
    {
        if (!client.IsConnected)
        {
            await client.ConnectAsync(_options.Server!, cancellationToken);
        }

        if (!client.IsAuthenticated)
        {
            await client.AuthenticateAsync(_options.Credential!, cancellationToken);

            await client.Inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        }
    }
}