namespace MailKit;

/// <summary>
/// Represents mail authentication values.
/// </summary>
public class MailCredential
{
    /// <summary>
    /// The user name.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The password.
    /// </summary>
    public string? Password { get; set; }
}