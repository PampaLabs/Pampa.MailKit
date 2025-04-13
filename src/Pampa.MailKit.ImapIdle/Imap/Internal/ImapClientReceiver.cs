using System.Reactive;
using System.Reactive.Linq;

namespace MailKit.Net.Imap;

internal class ImapClientReceiver : IAsyncDisposable
{
    private readonly ImapClient _client;

    private readonly ImapClientConnector _connector;

    private readonly ImapClientSubscriptionCollection _subscriptions = [];

    private int _currentMessages = 0;

    private CancellationTokenSource _stoppingToken = null!;

    public event Func<Task> OnDisposeAsync = () => Task.CompletedTask;

    public ImapClientReceiver(ImapClient client, ImapClientOptions options)
    {
        _client = client;
        _connector = new ImapClientConnector(options);
    }

    public async Task<IAsyncDisposable> SubscribeAsync(MessageHandler messageHandler, CancellationToken cancellationToken = default)
    {
        var subscription = new ImapClientSubscription();

        subscription.OnMessageReceived += messageHandler;
        subscription.OnDisposeAsync += async () => await UnsubscribeAsync(subscription);

        await SubscribeAsync(subscription, cancellationToken);

        return subscription;
    }

    public async Task SubscribeAsync(ImapClientSubscription subscription, CancellationToken cancellationToken = default)
    {
        _subscriptions.Add(subscription);

        await StartAsync(cancellationToken);
    }

    public async Task UnsubscribeAsync(ImapClientSubscription subscription, CancellationToken cancellationToken = default)
    {
        _subscriptions.Remove(subscription);

        if (_subscriptions.Count == 0)
        {
            await DisposeAsync();
        }
    }

    private async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_stoppingToken is not null) return;

        _stoppingToken = new();

        // connect to the IMAP server and get our initial list of messages
        try
        {
            await ReconnectAsync(cancellationToken);
            // await FetchMessageSummariesAsync(false);
            _currentMessages = _client.Inbox.Count;
        }
        catch (OperationCanceledException)
        {
            await _client.DisconnectAsync(true);
            return;
        }

        _ = IdleAsync(_stoppingToken.Token);
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        await _connector.ConnectAsync(_client, cancellationToken);
    }

    private async Task IdleAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await WaitForNewMessagesAsync(cancellationToken);

                if (result is not null)
                {
                    if (result.HasIncomingMessages)
                    {
                        await FetchMessageSummariesAsync(cancellationToken);
                        result.HasIncomingMessages = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<ImapClientStatus?> WaitForNewMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_client.Capabilities.HasFlag(ImapCapabilities.Idle))
                {
                    // Note: IMAP servers are only supposed to drop the connection after 30 minutes, so normally
                    // we'd IDLE for a max of, say, ~29 minutes... but GMail seems to drop idle connections after
                    // about 10 minutes, so we'll only idle for 9 minutes.
                    var stoppingTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(9));

                    var context = new ImapClientContext(stoppingTokenSource);

                    // keep track of changes to the number of messages in the folder (this is how we'll tell if new messages have arrived).
                    using var countChangedSub = Observable.FromEventPattern<EventArgs>(
                            handler => _client.Inbox.CountChanged += handler,
                            handler => _client.Inbox.CountChanged -= handler
                        )
                        .Select(ev => Unit.Default)
                        .Subscribe(_ => OnCountChanged(context));

                    // keep track of messages being expunged so that when the CountChanged event fires, we can tell if it's
                    // because new messages have arrived vs messages being removed (or some combination of the two).
                    using var messageExpungedSub = Observable.FromEventPattern<MessageEventArgs>(
                            handler => _client.Inbox.MessageExpunged += handler,
                            handler => _client.Inbox.MessageExpunged -= handler
                        )
                        .Select(ev => ev.EventArgs)
                        .Subscribe(args => OnMessageExpunged(context, args));

                    await _client.IdleAsync(context.StoppingTokenSource.Token, cancellationToken);

                    return context.Status;
                }
                else
                {
                    // Note: we don't want to spam the IMAP server with NOOP commands, so lets wait a minute
                    // between each NOOP command.
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    await _client.NoOpAsync(cancellationToken);
                }
                break;
            }
            catch (ImapProtocolException)
            {
                // protocol exceptions often result in the client getting disconnected
                await ReconnectAsync(cancellationToken);
            }
            catch (IOException)
            {
                // I/O exceptions always result in the client getting disconnected
                await ReconnectAsync(cancellationToken);
            }
        }

        return null;
    }

    private async Task FetchMessageSummariesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // fetch summary information for messages that we don't already have
                IEnumerable<IMessageSummary> fetched = await _client.Inbox.FetchAsync(_currentMessages, -1, MessageSummaryItems.Full | MessageSummaryItems.UniqueId, cancellationToken);

                foreach (IMessageSummary message in fetched)
                {
                    _currentMessages++;

                    foreach (var subscription in _subscriptions)
                    {
                        await subscription.HandleMessage(message);
                    }
                }

                break;
            }
            catch (ImapProtocolException)
            {
                // protocol exceptions often result in the client getting disconnected
                await ReconnectAsync(cancellationToken);
            }
            catch (IOException)
            {
                // I/O exceptions always result in the client getting disconnected
                await ReconnectAsync(cancellationToken);
            }
        }
    }

    // Note: the CountChanged event will fire when new messages arrive in the folder and/or when messages are expunged.
    private void OnCountChanged(ImapClientContext context)
    {
        var folder = _client.Inbox;

        // Note: because we are keeping track of the MessageExpunged event and updating our
        // 'messages' list, we know that if we get a CountChanged event and folder.Count is
        // larger than messages.Count, then it means that new messages have arrived.
        if (folder.Count > _currentMessages)
        {
            int arrived = folder.Count - _currentMessages;

            // Note: your first instinct may be to fetch these new messages now, but you cannot do
            // that in this event handler (the ImapFolder is not re-entrant).
            //
            // Instead, cancel the `done` token and update our state so that we know new messages
            // have arrived. We'll fetch the summaries for these new messages later...
            context.Status.HasIncomingMessages = true;
            context.StoppingTokenSource?.Cancel();
        }
    }

    private void OnMessageExpunged(ImapClientContext lookupState, MessageEventArgs e)
    {
        var folder = _client.Inbox;

        if (e.Index < _currentMessages)
        {
            _currentMessages--;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stoppingToken.Cancel();

        await _client.DisconnectAsync(true);

        await OnDisposeAsync();
    }
}