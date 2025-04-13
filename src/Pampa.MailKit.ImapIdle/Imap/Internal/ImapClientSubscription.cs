namespace MailKit.Net.Imap;

internal class ImapClientSubscription : IAsyncDisposable
{
    public event MessageHandler OnMessageReceived = (_, _) => Task.CompletedTask;

    public event Func<Task> OnDisposeAsync = () => Task.CompletedTask;

    public async Task HandleMessage(IMessageSummary message, CancellationToken cancellationToken = default)
    {
        await OnMessageReceived(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
    }
}