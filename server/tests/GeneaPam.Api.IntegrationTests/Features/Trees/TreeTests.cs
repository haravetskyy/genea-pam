using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Features.Trees.Create;
using GeneaPam.Api.Features.Trees.Get;
using GeneaPam.Api.Features.Trees.List;
using GeneaPam.Api.Features.Trees.Update;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Trees;

public sealed class TreeTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string SafePassword = "SafeP@ss!99xyz";

    private static string HibpPrefix(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[..5];
    }

    private void StubHibpClean(string password)
    {
        var prefix = HibpPrefix(password);
        WireMock
            .Given(Request.Create().WithPath($"/range/{prefix}").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBody("0000000000000000000000000000000000001:0\r\n")
            );
    }

    private async Task<string> RegisterAndLoginAsync(string email)
    {
        StubHibpClean(SafePassword);
        await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email,
                password = SafePassword,
                displayName = "Test User",
            }
        );

        var loginResponse = await Client.PostAsJsonAsync(
            "/auth/login",
            new { email, password = SafePassword }
        );

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }

    private void SetBearer(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> CreateTreeAsync(string token, string name = "Smith Family")
    {
        SetBearer(token);
        var response = await Client.PostAsJsonAsync("/trees", new { name, description = "desc" });
        var body = await response.Content.ReadFromJsonAsync<CreateTreeResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task CreateTree_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            "/trees",
            new { name = "Smith Family", description = "My family tree" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTree_Authenticated_Returns201WithId()
    {
        var token = await RegisterAndLoginAsync("trees_create@example.com");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            "/trees",
            new { name = "Smith Family", description = "My family tree" }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateTreeResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task CreateTree_BlankName_Returns422()
    {
        var token = await RegisterAndLoginAsync("trees_blankname@example.com");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            "/trees",
            new { name = "", description = "My family tree" }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tree.NameRequired", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateTree_StampsAuditFieldsViaMiddleware()
    {
        var token = await RegisterAndLoginAsync("trees_audit@example.com");
        var id = await CreateTreeAsync(token);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tree = await db.Trees.AsNoTracking().SingleAsync(t => t.Id == id);

        Assert.False(string.IsNullOrEmpty(tree.CreatedBy));
        Assert.False(string.IsNullOrEmpty(tree.UpdatedBy));
        Assert.Equal(tree.OwnerId, tree.CreatedBy);
        Assert.NotEqual(default, tree.CreatedAt);
        Assert.NotEqual(default, tree.UpdatedAt);
    }

    [Fact]
    public async Task GetTree_Owner_Returns200()
    {
        var token = await RegisterAndLoginAsync("trees_get@example.com");
        var id = await CreateTreeAsync(token);

        var response = await Client.GetAsync($"/trees/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetTreeResponse>();
        Assert.Equal(id, body!.Id);
        Assert.Equal("Smith Family", body.Name);
    }

    [Fact]
    public async Task GetTree_CrossUser_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("trees_owner@example.com");
        var id = await CreateTreeAsync(ownerToken);

        var otherToken = await RegisterAndLoginAsync("trees_other@example.com");
        SetBearer(otherToken);

        var response = await Client.GetAsync($"/trees/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListTrees_ReturnsOnlyOwnTrees()
    {
        var token = await RegisterAndLoginAsync("trees_list@example.com");
        await CreateTreeAsync(token, "Tree A");
        await CreateTreeAsync(token, "Tree B");

        var response = await Client.GetAsync("/trees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ListTreesResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Trees.Count);
    }

    [Fact]
    public async Task UpdateTree_Owner_Returns200WithUpdatedData()
    {
        var token = await RegisterAndLoginAsync("trees_update@example.com");
        var id = await CreateTreeAsync(token);

        var response = await Client.PutAsJsonAsync(
            $"/trees/{id}",
            new { name = "Updated Name", description = "Updated desc" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UpdateTreeResponse>();
        Assert.Equal("Updated Name", body!.Name);
        Assert.Equal("Updated desc", body.Description);
    }

    [Fact]
    public async Task UpdateTree_CrossUser_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("trees_update_owner@example.com");
        var id = await CreateTreeAsync(ownerToken);

        var otherToken = await RegisterAndLoginAsync("trees_update_other@example.com");
        SetBearer(otherToken);

        var response = await Client.PutAsJsonAsync(
            $"/trees/{id}",
            new { name = "Hijacked", description = "" }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTree_Owner_Returns204()
    {
        var token = await RegisterAndLoginAsync("trees_delete@example.com");
        var id = await CreateTreeAsync(token);

        var response = await Client.DeleteAsync($"/trees/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync($"/trees/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTree_CrossUser_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("trees_delete_owner@example.com");
        var id = await CreateTreeAsync(ownerToken);

        var otherToken = await RegisterAndLoginAsync("trees_delete_other@example.com");
        SetBearer(otherToken);

        var response = await Client.DeleteAsync($"/trees/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
