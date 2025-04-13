namespace MailKit.Net.Imap;

internal class ImapClientReceiverFactory
{
    public static Dictionary<ImapClient, ImapClientReceiver> _clients { get; } = [];

    public ImapClientReceiver Create(ImapClient client, ImapClientOptions options)
    {
        if (!_clients.ContainsKey(client))
        {
            var receiever = new ImapClientReceiver(client, options);
            receiever.OnDisposeAsync += async () => _clients.Remove(client);

            _clients.Add(client, receiever);
        }

        return _clients[client];
    }
}