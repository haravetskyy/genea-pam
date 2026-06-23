using GeneaPam.Api.Infrastructure.Email;
using GeneaPam.Api.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Email;

public sealed class WelcomeEmailTests(EmailApiFactory factory) : IClassFixture<EmailApiFactory>
{
    [Fact]
    public async Task DispatchWelcomeEmailJob_PostsToResendWithCorrectPayload()
    {
        factory.WireMock
            .Given(Request.Create().WithPath("/emails").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"id\":\"fake-id\"}"));

        await using var scope = factory.Services.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IJobDispatcher>();

        await dispatcher.SendAsync(new WelcomeEmailJob(
            To: "test@example.com",
            UserName: "Alice",
            LanguagePreference: "en"));

        await Task.Delay(TimeSpan.FromSeconds(3));

        var call = factory.WireMock.LogEntries
            .FirstOrDefault(e => e.RequestMessage.Path == "/emails");

        Assert.NotNull(call);

        var body = call.RequestMessage.Body!;
        Assert.Contains("test@example.com", body);
        Assert.Contains("html", body);

        var html = System.Text.Json.JsonDocument.Parse(body)
            .RootElement.GetProperty("html").GetString();
        Assert.NotNull(html);
        Assert.NotEmpty(html);
    }
}
