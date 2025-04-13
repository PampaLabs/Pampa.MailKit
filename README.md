# MailKit IMAP IDLE

This package offers extension methods for MailKit to enable reactive email processing using the IMAP IDLE feature.

It allows for seamless, real-time email updates by subscribing to incoming messages as they arrive. This approach eliminates the need for polling, making it ideal for building efficient email clients or systems that need to handle messages as they arrive.

## Installation

To use this extension with `MailKit`, you will first need to install the package.

```
dotnet add package Pampa.MailKit.ImapIdle
```

## Usage

The `IdleSubscribeAsync` method allows you to subscribe to the IMAP client’s message reception and process each received message with a handler. The handler is a delegate that defines how each incoming message should be handled.

```csharp
using MailKit.Net.Imap;
using MailKit.Security;

var credentials = new MailCredential
{
    Username = "your-email@example.com",
    Password = "your-password"
};

var server = new MailServer
{
    Host = "imap.example.com",
    Port = 993
};

var options = new ImapClientOptions
{
    Credential = credentials,
    Server = server
};

var client = new ImapClient();

await client.IdleSubscribeAsync(options, async (message, cancellationToken) =>
{
    Console.WriteLine($"Received message: {message.Envelope.Subject}");
});

await Task.Delay(10_000);

await subscription.DisposeAsync();
```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.
