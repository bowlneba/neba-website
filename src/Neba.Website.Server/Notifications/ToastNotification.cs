namespace Neba.Website.Server.Notifications;

/// <summary>
/// Represents a transient toast notification payload.
/// </summary>
internal sealed record ToastNotification(string Title, string Message, NotifySeverity Severity);