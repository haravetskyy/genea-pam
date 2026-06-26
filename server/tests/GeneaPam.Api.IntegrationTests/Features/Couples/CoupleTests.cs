using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Features.Couples;
using GeneaPam.Api.Features.Couples.Create;
using GeneaPam.Api.Features.Persons.Create;
using GeneaPam.Api.Features.Trees.Create;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Couples;

public sealed class CoupleTests(ApiFactory factory) : IntegrationTest(factory)
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

    // =========================================================
    // CREATE COUPLE
    // =========================================================

    [Fact]
    public async Task CreateCouple_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            $"/trees/{Guid.NewGuid()}/couples",
            new { personAId = Guid.NewGuid(), personBId = Guid.NewGuid() }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCouple_CrossUserTree_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("cc_crossuser_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);

        var otherToken = await RegisterAndLoginAsync("cc_crossuser_other@example.com");
        SetBearer(otherToken);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId = Guid.NewGuid(), personBId = Guid.NewGuid() }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCouple_NonExistentTree_Returns404()
    {
        var token = await RegisterAndLoginAsync("cc_notree@example.com");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{Guid.NewGuid()}/couples",
            new { personAId = Guid.NewGuid(), personBId = Guid.NewGuid() }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCouple_SamePersonBothSides_Returns422()
    {
        var token = await RegisterAndLoginAsync("cc_same_person@example.com");
        var treeId = await CreateTreeAsync(token);
        var personId = await CreatePersonAsync(token, treeId);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId = personId, personBId = personId }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateCouple_SamePersonBothSides_ReturnsProblemErrorCode()
    {
        var token = await RegisterAndLoginAsync("cc_same_person_code@example.com");
        var treeId = await CreateTreeAsync(token);
        var personId = await CreatePersonAsync(token, treeId);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId = personId, personBId = personId }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Couple.SamePersonBothSides", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateCouple_StampsAuditFieldsViaMiddleware()
    {
        var token = await RegisterAndLoginAsync("cc_audit@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var pB = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var couple = await db.Couples.AsNoTracking().SingleAsync(c => c.Id == coupleId);

        Assert.False(string.IsNullOrEmpty(couple.CreatedBy));
        Assert.False(string.IsNullOrEmpty(couple.UpdatedBy));
        Assert.Equal(couple.CreatedBy, couple.UpdatedBy);
        Assert.NotEqual(default, couple.CreatedAt);
        Assert.NotEqual(default, couple.UpdatedAt);
    }

    [Fact]
    public async Task CreateCouple_ValidPersons_Returns201WithCorrectBody()
    {
        var token = await RegisterAndLoginAsync("cc_valid@example.com");
        var treeId = await CreateTreeAsync(token);
        var personAId = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var personBId = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId, personBId }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateCoupleResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(treeId, body.TreeId);
        Assert.Equal(personAId, body.PersonAId);
        Assert.Equal(personBId, body.PersonBId);
        Assert.Equal("Partners", body.Type.Value);
    }

    [Fact]
    public async Task CreateCouple_LocationHeaderPointsToCouple()
    {
        var token = await RegisterAndLoginAsync("cc_location@example.com");
        var treeId = await CreateTreeAsync(token);
        var personAId = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var personBId = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId, personBId }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateCouple_WithExplicitType_RoundTripsThatType()
    {
        var token = await RegisterAndLoginAsync("cc_type_roundtrip@example.com");
        var treeId = await CreateTreeAsync(token);
        var personAId = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var personBId = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new
            {
                personAId,
                personBId,
                type = "Married",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateCoupleResponse>();
        Assert.NotNull(body);
        Assert.Equal("Married", body.Type.Value);
    }

    [Theory]
    [InlineData("Married")]
    [InlineData("Partners")]
    [InlineData("Separated")]
    [InlineData("Divorced")]
    [InlineData("Other")]
    public async Task CreateCouple_EachValidType_RoundTrips(string type)
    {
        var token = await RegisterAndLoginAsync($"cc_type_{type.ToLowerInvariant()}@example.com");
        var treeId = await CreateTreeAsync(token);
        var personAId = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var personBId = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new
            {
                personAId,
                personBId,
                type,
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateCoupleResponse>();
        Assert.Equal(type, body!.Type.Value);
    }

    [Fact]
    public async Task CreateCouple_TypeOmitted_DefaultsToPartners()
    {
        var token = await RegisterAndLoginAsync("cc_type_default@example.com");
        var treeId = await CreateTreeAsync(token);
        var personAId = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var personBId = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId, personBId }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateCoupleResponse>();
        Assert.Equal("Partners", body!.Type.Value);
    }

    [Fact]
    public async Task CreateCouple_InvalidType_Returns422WithProblemCode()
    {
        var token = await RegisterAndLoginAsync("cc_type_invalid@example.com");
        var treeId = await CreateTreeAsync(token);
        var personAId = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var personBId = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new
            {
                personAId,
                personBId,
                type = "Engaged",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Couple.TypeInvalid", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateCouple_TypeDbCheckConstraint_RejectsOutOfSetValue()
    {
        var token = await RegisterAndLoginAsync("cc_type_dbcheck@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var pB = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Bypass the app layer and write a bad value straight to the column.
        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE couples SET type = 'Engaged' WHERE id = {0}",
                coupleId
            );
        });

        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("ck_couples_type", ex.Message);
    }

    [Fact]
    public async Task CreateCouple_TwoDistinctCouples_BothGetPersisted()
    {
        var token = await RegisterAndLoginAsync("cc_two_couples@example.com");
        var treeId = await CreateTreeAsync(token);
        var p1 = await CreatePersonAsync(token, treeId, "A", "A");
        var p2 = await CreatePersonAsync(token, treeId, "B", "B");
        var p3 = await CreatePersonAsync(token, treeId, "C", "C");
        var p4 = await CreatePersonAsync(token, treeId, "D", "D");

        var coupleId1 = await CreateCoupleAsync(token, treeId, p1, p2);
        var coupleId2 = await CreateCoupleAsync(token, treeId, p3, p4);

        Assert.NotEqual(coupleId1, coupleId2);
    }

    // =========================================================
    // DELETE COUPLE
    // =========================================================

    [Fact]
    public async Task DeleteCouple_Unauthenticated_Returns401()
    {
        var response = await Client.DeleteAsync(
            $"/trees/{Guid.NewGuid()}/couples/{Guid.NewGuid()}"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCouple_CrossUserTree_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("dc_crossuser_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);
        var pA = await CreatePersonAsync(ownerToken, treeId, "A", "A");
        var pB = await CreatePersonAsync(ownerToken, treeId, "B", "B");
        var coupleId = await CreateCoupleAsync(ownerToken, treeId, pA, pB);

        var otherToken = await RegisterAndLoginAsync("dc_crossuser_other@example.com");
        SetBearer(otherToken);

        var response = await Client.DeleteAsync($"/trees/{treeId}/couples/{coupleId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCouple_NonExistentCouple_Returns404()
    {
        var token = await RegisterAndLoginAsync("dc_nocouple@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.DeleteAsync($"/trees/{treeId}/couples/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCouple_Owner_Returns204()
    {
        var token = await RegisterAndLoginAsync("dc_owner@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "A", "A");
        var pB = await CreatePersonAsync(token, treeId, "B", "B");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);
        SetBearer(token);

        var response = await Client.DeleteAsync($"/trees/{treeId}/couples/{coupleId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCouple_DoesNotDeletePersons()
    {
        var token = await RegisterAndLoginAsync("dc_persons_survive@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "A", "A");
        var pB = await CreatePersonAsync(token, treeId, "B", "B");
        var coupleId = await CreateCoupleAsync(token, treeId, pA, pB);
        SetBearer(token);

        await Client.DeleteAsync($"/trees/{treeId}/couples/{coupleId}");

        var personAResponse = await Client.GetAsync($"/trees/{treeId}/persons/{pA}");
        var personBResponse = await Client.GetAsync($"/trees/{treeId}/persons/{pB}");

        Assert.Equal(HttpStatusCode.OK, personAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, personBResponse.StatusCode);
    }
}
