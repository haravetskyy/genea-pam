using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Features.Couples.AddFiliation;
using GeneaPam.Api.Features.Couples.Create;
using GeneaPam.Api.Features.Persons.Create;
using GeneaPam.Api.Features.Trees.Create;
using GeneaPam.Api.Features.Trees.Graph;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Trees;

public sealed class TreeGraphTests(ApiFactory factory) : IntegrationTest(factory)
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
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
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

    private async Task<Guid> CreatePersonAsync(
        string token,
        Guid treeId,
        string firstName = "Jane",
        string lastName = "Doe",
        DateOnly? birthDate = null,
        DateOnly? deathDate = null
    )
    {
        SetBearer(token);
        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName,
                lastName,
                gender = "Female",
                birthDate,
                birthDatePrecision = birthDate.HasValue ? "Year" : (string?)null,
                deathDate,
                deathDatePrecision = deathDate.HasValue ? "Year" : (string?)null,
            }
        );
        var body = await response.Content.ReadFromJsonAsync<CreatePersonResponse>();
        return body!.Id;
    }

    private async Task<Guid> CreateCoupleAsync(
        string token,
        Guid treeId,
        Guid personAId,
        Guid personBId
    )
    {
        SetBearer(token);
        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId, personBId }
        );
        var body = await response.Content.ReadFromJsonAsync<CreateCoupleResponse>();
        return body!.Id;
    }

    private async Task<Guid> AddFiliationAsync(
        string token,
        Guid treeId,
        Guid coupleId,
        Guid childPersonId
    )
    {
        SetBearer(token);
        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples/{coupleId}/filiations",
            new { childPersonId }
        );
        var body = await response.Content.ReadFromJsonAsync<AddFiliationResponse>();
        return body!.Id;
    }

    // =========================================================
    // GET TREE GRAPH
    // =========================================================

    [Fact]
    public async Task GetTreeGraph_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync($"/trees/{Guid.NewGuid()}/graph");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTreeGraph_CrossUserTree_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("graph_crossuser_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);

        var otherToken = await RegisterAndLoginAsync("graph_crossuser_other@example.com");
        SetBearer(otherToken);

        var response = await Client.GetAsync($"/trees/{treeId}/graph");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTreeGraph_EmptyTree_ReturnsEmptyNodesAndEdges()
    {
        var token = await RegisterAndLoginAsync("graph_empty@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.GetAsync($"/trees/{treeId}/graph");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetTreeGraphResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Nodes);
        Assert.Empty(body.Edges);
    }

    [Fact]
    public async Task GetTreeGraph_WithPersons_ReturnsCorrectNodeShape()
    {
        var token = await RegisterAndLoginAsync("graph_nodes@example.com");
        var treeId = await CreateTreeAsync(token);
        await CreatePersonAsync(
            token,
            treeId,
            "Alice",
            "Smith",
            birthDate: new DateOnly(1990, 1, 1)
        );
        await CreatePersonAsync(
            token,
            treeId,
            "Bob",
            "Smith",
            birthDate: new DateOnly(1985, 5, 15),
            deathDate: new DateOnly(2020, 3, 10)
        );
        SetBearer(token);

        var response = await Client.GetAsync($"/trees/{treeId}/graph");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetTreeGraphResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Nodes.Count);

        var alice = body.Nodes.Single(n => n.FullName == "Alice Smith");
        Assert.Equal(1990, alice.BirthYear);
        Assert.Null(alice.DeathYear);
        Assert.True(alice.IsLiving);

        var bob = body.Nodes.Single(n => n.FullName == "Bob Smith");
        Assert.Equal(1985, bob.BirthYear);
        Assert.Equal(2020, bob.DeathYear);
        Assert.False(bob.IsLiving);
    }

    [Fact]
    public async Task GetTreeGraph_WithCouple_ReturnsCoupleEdge()
    {
        var token = await RegisterAndLoginAsync("graph_couple_edge@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var pB = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);
        SetBearer(token);

        var response = await Client.GetAsync($"/trees/{treeId}/graph");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetTreeGraphResponse>();
        Assert.NotNull(body);

        var edge = Assert.Single(body.Edges);
        Assert.Equal(coupleId, edge.Id);
        Assert.Equal("Couple", edge.Type);
        Assert.Equal(pA, edge.PersonAId);
        Assert.Equal(pB, edge.PersonBId);
        Assert.Null(edge.CoupleId);
        Assert.Null(edge.ChildId);
    }

    [Fact]
    public async Task GetTreeGraph_WithFiliation_ReturnsFiliationEdge()
    {
        var token = await RegisterAndLoginAsync("graph_filiation_edge@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var pB = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        var child = await CreatePersonAsync(token, treeId, "Charlie", "Smith");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);
        var filiationId = await AddFiliationAsync(token, treeId, coupleId, child);
        SetBearer(token);

        var response = await Client.GetAsync($"/trees/{treeId}/graph");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetTreeGraphResponse>();
        Assert.NotNull(body);

        var filiationEdge = body.Edges.Single(e => e.Type == "Filiation");
        Assert.Equal(filiationId, filiationEdge.Id);
        Assert.Equal(coupleId, filiationEdge.CoupleId);
        Assert.Equal(child, filiationEdge.ChildId);
        Assert.Null(filiationEdge.PersonAId);
        Assert.Null(filiationEdge.PersonBId);
    }

    [Fact]
    public async Task GetTreeGraph_FullGraph_ReturnsAllNodesAndEdges()
    {
        var token = await RegisterAndLoginAsync("graph_full@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var pB = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        var child = await CreatePersonAsync(token, treeId, "Charlie", "Smith");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);
        await AddFiliationAsync(token, treeId, coupleId, child);
        SetBearer(token);

        var response = await Client.GetAsync($"/trees/{treeId}/graph");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetTreeGraphResponse>();
        Assert.NotNull(body);
        Assert.Equal(3, body.Nodes.Count);
        Assert.Equal(2, body.Edges.Count);
        Assert.Single(body.Edges, e => e.Type == "Couple");
        Assert.Single(body.Edges, e => e.Type == "Filiation");
    }
}
