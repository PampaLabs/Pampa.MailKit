namespace MailKit;

/// <summary>
/// The handler delegate responsible for processing incoming messages.
/// </summary>
/// <param name="message">The message to process.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns></returns>
public delegate Task MessageHandler(IMessageSummary message, CancellationToken cancellationToken = default);
