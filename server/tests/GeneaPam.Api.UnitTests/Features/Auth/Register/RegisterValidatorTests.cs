using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Features.Auth.Register;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Validators.Email.Disposable.Registrars;

namespace GeneaPam.Api.UnitTests.Features.Auth.Register;

public sealed class RegisterValidatorTests
{
    private const string SafePassword = "SafeP@ss!99xyz";
    private const string BreachedPassword = "password123";

    private static string HibpPrefix(string password)
    {
        var hash = System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.UTF8.GetBytes(password)
        );
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[..5];
    }

    private static string HibpSuffix(string password)
    {
        var hash = System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.UTF8.GetBytes(password)
        );
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[5..];
    }

    private static RegisterValidator BuildValidator(FakeDnsResolver dns, StubHibpHandler hibp)
    {
        var services = new ServiceCollection();
        services.AddEmailDisposableValidatorAsSingleton();
        services
            .AddHttpClient("hibp", c => c.BaseAddress = new Uri("https://hibp.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => hibp);
        var sp = services.BuildServiceProvider();

        var disposable =
            sp.GetRequiredService<Soenneker.Validators.Email.Disposable.Abstract.IEmailDisposableValidator>();
        var factory = sp.GetRequiredService<IHttpClientFactory>();

        return new RegisterValidator(dns, factory, disposable);
    }

    [Fact]
    public async Task ValidInput_ReturnsValue()
    {
        var dns = new FakeDnsResolver();
        dns.AllowMx("gmail.com");
        var hibp = StubHibpHandler.Clean(HibpPrefix(SafePassword));

        var validator = BuildValidator(dns, hibp);

        var result = await validator.ValidateToErrorOrAsync(
            new RegisterRequest("alice@gmail.com", SafePassword, "Alice"),
            CancellationToken.None
        );

        Assert.False(result.IsError);
        Assert.Equal("alice@gmail.com", result.Value.Email);
    }

    [Fact]
    public async Task InvalidEmailSyntax_ReturnsEmailInvalid()
    {
        var dns = new FakeDnsResolver();
        var hibp = StubHibpHandler.Clean(HibpPrefix(SafePassword));

        var validator = BuildValidator(dns, hibp);

        var result = await validator.ValidateToErrorOrAsync(
            new RegisterRequest("not-an-email", SafePassword, "Alice"),
            CancellationToken.None
        );

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == AuthErrors.EmailInvalid.Code);
    }

    [Fact]
    public async Task ShortPassword_ReturnsPasswordTooShort()
    {
        var dns = new FakeDnsResolver();
        dns.AllowMx("gmail.com");
        var hibp = StubHibpHandler.Clean(HibpPrefix("Ab1!"));

        var validator = BuildValidator(dns, hibp);

        var result = await validator.ValidateToErrorOrAsync(
            new RegisterRequest("alice@gmail.com", "Ab1!", "Alice"),
            CancellationToken.None
        );

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == AuthErrors.PasswordTooShort.Code);
    }

    [Fact]
    public async Task BreachedPassword_ReturnsPasswordBreached()
    {
        var dns = new FakeDnsResolver();
        dns.AllowMx("gmail.com");
        var hibp = StubHibpHandler.Breached(
            HibpPrefix(BreachedPassword),
            HibpSuffix(BreachedPassword)
        );

        var validator = BuildValidator(dns, hibp);

        var result = await validator.ValidateToErrorOrAsync(
            new RegisterRequest("alice@gmail.com", BreachedPassword, "Alice"),
            CancellationToken.None
        );

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == AuthErrors.PasswordBreached.Code);
    }

    [Fact]
    public async Task DisposableEmail_ReturnsEmailDisposable()
    {
        var dns = new FakeDnsResolver();
        var hibp = StubHibpHandler.Clean(HibpPrefix(SafePassword));

        var validator = BuildValidator(dns, hibp);

        var result = await validator.ValidateToErrorOrAsync(
            new RegisterRequest("eve@mailinator.com", SafePassword, "Eve"),
            CancellationToken.None
        );

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == AuthErrors.EmailDisposable.Code);
    }
}

internal sealed class FakeDnsResolver : IDnsResolver
{
    private readonly HashSet<string> _denied = new(StringComparer.OrdinalIgnoreCase);

    public void AllowMx(string domain) { }

    public void DenyMx(string domain) => _denied.Add(domain);

    public Task<bool> HasMxRecordAsync(
        string domain,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(!_denied.Contains(domain));
}

internal sealed class StubHibpHandler : HttpMessageHandler
{
    private readonly string _prefix;
    private readonly string _body;

    private StubHibpHandler(string prefix, string body)
    {
        _prefix = prefix;
        _body = body;
    }

    public static StubHibpHandler Clean(string prefix) =>
        new(prefix, "0000000000000000000000000000000000001:0\r\n");

    public static StubHibpHandler Breached(string prefix, string suffix) =>
        new(prefix, $"{suffix}:42\r\n0000000000000000000000000000000000001:0\r\n");

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(_body),
        };
        return Task.FromResult(response);
    }
}
