namespace MailKit.Net.Imap;

internal class ImapClientContext
{
    public ImapClientContext(CancellationTokenSource stoppingTokenSource)
    {
        StoppingTokenSource = stoppingTokenSource;
        Status = new ImapClientStatus();
    }

    public ImapClientStatus Status { get; }

    public CancellationTokenSource StoppingTokenSource { get; }
}