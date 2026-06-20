using System.Net.Http.Json;
using System.Text.Json.Serialization;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Neba.TestFactory.Infrastructure;

public sealed class MailpitFixture : IAsyncLifetime
{
    private const int SmtpContainerPort = 1025;
    private const int HttpContainerPort = 8025;

    private readonly IContainer _container = new ContainerBuilder("axllent/mailpit:latest")
        .WithPortBinding(SmtpContainerPort, true)
        .WithPortBinding(HttpContainerPort, true)
        .WithCommand("--smtp-auth-accept-any", "--smtp-auth-allow-insecure")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("\\[http\\] starting on"))
        .Build();

    public string SmtpHost => _container.Hostname;
    public int SmtpPort => _container.GetMappedPublicPort(SmtpContainerPort);

    public async Task DeleteAllMessagesAsync()
    {
        using var client = CreateHttpClient();
        var response = await client.DeleteAsync(new Uri("/api/v1/messages", UriKind.Relative));
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MailpitMessage>> GetMessagesAsync()
    {
        using var client = CreateHttpClient();
        var result = await client.GetFromJsonAsync<MailpitMessagesResponse>("/api/v1/messages");
        return result?.Messages ?? [];
    }

    public async Task<MailpitMessageDetail> GetMessageDetailAsync(string id)
    {
        using var client = CreateHttpClient();
        return await client.GetFromJsonAsync<MailpitMessageDetail>($"/api/v1/message/{id}")
            ?? throw new InvalidOperationException($"Message '{id}' not found.");
    }

    private HttpClient CreateHttpClient() =>
        new() { BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpContainerPort)}") };

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

public sealed record MailpitMessage(
    [property: JsonPropertyName("ID")] string Id,
    [property: JsonPropertyName("From")] MailpitAddress From,
    [property: JsonPropertyName("To")] IReadOnlyList<MailpitAddress> To,
    [property: JsonPropertyName("Subject")] string Subject,
    [property: JsonPropertyName("ReplyTo")] IReadOnlyList<MailpitAddress> ReplyTo);

public sealed record MailpitMessageDetail(
    [property: JsonPropertyName("ID")] string Id,
    [property: JsonPropertyName("From")] MailpitAddress From,
    [property: JsonPropertyName("To")] IReadOnlyList<MailpitAddress> To,
    [property: JsonPropertyName("Subject")] string Subject,
    [property: JsonPropertyName("HTML")] string Html,
    [property: JsonPropertyName("ReplyTo")] IReadOnlyList<MailpitAddress> ReplyTo);

public sealed record MailpitAddress(
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("Address")] string Address);

internal sealed record MailpitMessagesResponse(
    [property: JsonPropertyName("messages")] IReadOnlyList<MailpitMessage> Messages);