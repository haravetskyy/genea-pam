using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Features.Persons.Create;
using GeneaPam.Api.Features.Persons.Get;
using GeneaPam.Api.Features.Persons.Update;
using GeneaPam.Api.Features.Trees.Create;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Persons;

public sealed class PersonTests(ApiFactory factory) : IntegrationTest(factory)
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

    private async Task<Guid> CreatePersonAsync(
        string token,
        Guid treeId,
        string firstName = "Jane",
        string lastName = "Doe"
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
            }
        );
        var body = await response.Content.ReadFromJsonAsync<CreatePersonResponse>();
        return body!.Id;
    }

    // --- CREATE ---

    [Fact]
    public async Task CreatePerson_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            $"/trees/{Guid.NewGuid()}/persons",
            new { firstName = "Jane", lastName = "Doe" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePerson_AuthenticatedWithOwnTree_Returns201WithId()
    {
        var token = await RegisterAndLoginAsync("persons_create@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                gender = "Female",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreatePersonResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Jane", body.FirstName);
        Assert.Equal("Doe", body.LastName);
    }

    [Fact]
    public async Task CreatePerson_CrossUserTree_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("persons_create_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);

        var otherToken = await RegisterAndLoginAsync("persons_create_other@example.com");
        SetBearer(otherToken);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new { firstName = "Jane", lastName = "Doe" }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GET ---

    [Fact]
    public async Task GetPerson_Owner_Returns200()
    {
        var token = await RegisterAndLoginAsync("persons_get@example.com");
        var treeId = await CreateTreeAsync(token);
        var personId = await CreatePersonAsync(token, treeId);

        var response = await Client.GetAsync($"/trees/{treeId}/persons/{personId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GetPersonResponse>();
        Assert.Equal(personId, body!.Id);
        Assert.Equal("Jane", body.FirstName);
        Assert.Equal("Doe", body.LastName);
    }

    [Fact]
    public async Task GetPerson_CrossUser_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("persons_get_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);
        var personId = await CreatePersonAsync(ownerToken, treeId);

        var otherToken = await RegisterAndLoginAsync("persons_get_other@example.com");
        SetBearer(otherToken);

        var response = await Client.GetAsync($"/trees/{treeId}/persons/{personId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPerson_NotFound_Returns404()
    {
        var token = await RegisterAndLoginAsync("persons_get_notfound@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.GetAsync($"/trees/{treeId}/persons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- UPDATE ---

    [Fact]
    public async Task UpdatePerson_Owner_Returns200WithUpdatedData()
    {
        var token = await RegisterAndLoginAsync("persons_update@example.com");
        var treeId = await CreateTreeAsync(token);
        var personId = await CreatePersonAsync(token, treeId);

        SetBearer(token);
        var response = await Client.PutAsJsonAsync(
            $"/trees/{treeId}/persons/{personId}",
            new
            {
                firstName = "John",
                lastName = "Smith",
                gender = "Male",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UpdatePersonResponse>();
        Assert.Equal("John", body!.FirstName);
        Assert.Equal("Smith", body.LastName);
        Assert.Equal("Male", body.Gender);
    }

    [Fact]
    public async Task UpdatePerson_CrossUser_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("persons_update_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);
        var personId = await CreatePersonAsync(ownerToken, treeId);

        var otherToken = await RegisterAndLoginAsync("persons_update_other@example.com");
        SetBearer(otherToken);

        var response = await Client.PutAsJsonAsync(
            $"/trees/{treeId}/persons/{personId}",
            new
            {
                firstName = "Hijacked",
                lastName = "Person",
                gender = "Male",
            }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- DELETE ---

    [Fact]
    public async Task DeletePerson_Owner_Returns204()
    {
        var token = await RegisterAndLoginAsync("persons_delete@example.com");
        var treeId = await CreateTreeAsync(token);
        var personId = await CreatePersonAsync(token, treeId);

        SetBearer(token);
        var response = await Client.DeleteAsync($"/trees/{treeId}/persons/{personId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync($"/trees/{treeId}/persons/{personId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePerson_CrossUser_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("persons_delete_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);
        var personId = await CreatePersonAsync(ownerToken, treeId);

        var otherToken = await RegisterAndLoginAsync("persons_delete_other@example.com");
        SetBearer(otherToken);

        var response = await Client.DeleteAsync($"/trees/{treeId}/persons/{personId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePerson_AlsoDeletesFromTree_TreeStillExists()
    {
        var token = await RegisterAndLoginAsync("persons_delete_tree@example.com");
        var treeId = await CreateTreeAsync(token);
        var personId = await CreatePersonAsync(token, treeId);

        SetBearer(token);
        await Client.DeleteAsync($"/trees/{treeId}/persons/{personId}");

        var treeResponse = await Client.GetAsync($"/trees/{treeId}");
        Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);
    }
}
