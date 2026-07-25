using MailKit.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Compliance;
using Neba.Api.Email;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Email;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Email;

[IntegrationTest]
[Component("Email")]
public sealed class GoogleWorkspaceEmailSenderTests : IClassFixture<MailpitFixture>
{
    private readonly MailpitFixture _fixture;
    private readonly FakeLogger<GoogleWorkspaceEmailSender> _logger;
    private readonly GoogleWorkspaceEmailSender _sut;

    public GoogleWorkspaceEmailSenderTests(MailpitFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _logger = new FakeLogger<GoogleWorkspaceEmailSender>();
        _sut = new GoogleWorkspaceEmailSender(BuildSettings(), _logger);
    }

    private EmailSettings BuildSettings(
        string replyToAddress = "support@bowlneba.com",
        string replyToName = "BowlNEBA Support")
        => new()
        {
            Host = _fixture.SmtpHost,
            Port = _fixture.SmtpPort,
            UserName = "test",
            AppPassword = "test",
            FromAddress = "noreply@bowlneba.com",
            FromName = "BowlNEBA",
            ReplyToAddress = replyToAddress,
            ReplyToName = replyToName,
            TlsMode = SecureSocketOptions.None,
        };

    [Fact(DisplayName = "SendAsync should deliver email with correct headers and body")]
    public async Task SendAsync_ShouldDeliverEmail_WithCorrectHeadersAndBody()
    {
        // Arrange
        await _fixture.DeleteAllMessagesAsync();
        var message = EmailMessageFactory.Create(
            to: "recipient@example.com",
            subject: "Welcome to NEBA",
            htmlBody: "<p>Hello!</p>");

        // Act
        await _sut.SendAsync(message, CancellationToken.None);

        // Assert
        var messages = await _fixture.GetMessagesAsync();
        messages.Count.ShouldBe(1);
        messages[0].To[0].Address.ShouldBe("recipient@example.com");
        messages[0].Subject.ShouldBe("Welcome to NEBA");
        messages[0].From.Address.ShouldBe("noreply@bowlneba.com");
        messages[0].From.Name.ShouldBe("BowlNEBA");

        var detail = await _fixture.GetMessageDetailAsync(messages[0].Id);
        detail.Html.Trim().ShouldBe("<p>Hello!</p>");
    }

    [Fact(DisplayName = "SendAsync should use ReplyTo from message when provided")]
    public async Task SendAsync_ShouldUseReplyTo_FromMessage_WhenProvided()
    {
        // Arrange
        await _fixture.DeleteAllMessagesAsync();
        var message = EmailMessageFactory.Create(replyTo: "custom@example.com");

        // Act
        await _sut.SendAsync(message, CancellationToken.None);

        // Assert
        var messages = await _fixture.GetMessagesAsync();
        messages.Count.ShouldBe(1);
        messages[0].ReplyTo.ShouldHaveSingleItem();
        messages[0].ReplyTo[0].Address.ShouldBe("custom@example.com");
    }

    [Fact(DisplayName = "SendAsync should use ReplyTo from settings when message has no ReplyTo")]
    public async Task SendAsync_ShouldUseReplyTo_FromSettings_WhenMessageHasNoReplyTo()
    {
        // Arrange
        await _fixture.DeleteAllMessagesAsync();
        var message = EmailMessageFactory.Create(replyTo: null);

        // Act
        await _sut.SendAsync(message, CancellationToken.None);

        // Assert
        var messages = await _fixture.GetMessagesAsync();
        messages.Count.ShouldBe(1);
        messages[0].ReplyTo.ShouldHaveSingleItem();
        messages[0].ReplyTo[0].Address.ShouldBe("support@bowlneba.com");
    }

    [Fact(DisplayName = "SendAsync should omit ReplyTo when neither message nor settings provide one")]
    public async Task SendAsync_ShouldOmitReplyTo_WhenNeitherMessageNorSettingsProvideOne()
    {
        // Arrange
        await _fixture.DeleteAllMessagesAsync();
        var sut = new GoogleWorkspaceEmailSender(
            BuildSettings(replyToAddress: string.Empty, replyToName: string.Empty),
            _logger);
        var message = EmailMessageFactory.Create(replyTo: null);

        // Act
        await sut.SendAsync(message, CancellationToken.None);

        // Assert
        var messages = await _fixture.GetMessagesAsync();
        messages.Count.ShouldBe(1);
        messages[0].ReplyTo.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SendAsync should mask recipient address in both formatted message and structured state")]
    public async Task SendAsync_ShouldMaskRecipientAddress_InFormattedMessageAndStructuredState()
    {
        // Arrange
        await _fixture.DeleteAllMessagesAsync();

        // [LoggerMessage]-generated redaction is only applied by the ExtendedLogger created
        // through the DI logging pipeline (ILoggingBuilder.EnableRedaction()) — a FakeLogger<T>
        // constructed directly via `new` bypasses that pipeline entirely and never redacts.
        // Uses the real RedactionConfiguration.AddRedaction() production wiring (rather than
        // a hand-rolled equivalent) so this test also exercises that extension method.
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders().AddFakeLogging();
        builder.AddRedaction();

        await using var provider = builder.Services.BuildServiceProvider();

        var redactingLogger = provider.GetRequiredService<ILogger<GoogleWorkspaceEmailSender>>();
        var collector = provider.GetFakeLogCollector();
        var sut = new GoogleWorkspaceEmailSender(BuildSettings(), redactingLogger);

        var message = EmailMessageFactory.Create(
            to: "log-target@example.com",
            subject: "Log Verification");

        // Act
        await sut.SendAsync(message, CancellationToken.None);

        // Assert
        var logs = collector.GetSnapshot();
        logs.Count.ShouldBe(1);
        logs[0].Level.ShouldBe(LogLevel.Information);
        // ApplyDiscriminator (default true) folds the tag name into the redacted value to
        // prevent cross-tag correlation, so the exact masked length is an implementation
        // detail — assert on the externally-visible contract instead: first character kept,
        // everything else starred out, no fragment of the real address anywhere.
        logs[0].Message.ShouldMatch(@"^Email sent to l\*+: Log Verification$");
        logs[0].Message.ShouldNotContain("log-target@example.com");
        logs[0].Message.ShouldNotContain("@example.com");

        var toAddressTag = logs[0].GetStructuredStateValue("ToAddress");
        toAddressTag.ShouldNotBeNull();
        toAddressTag.ShouldMatch(@"^l\*+$");
        toAddressTag.ShouldNotContain("log-target@example.com");
        toAddressTag.ShouldNotContain("@example.com");
    }
}