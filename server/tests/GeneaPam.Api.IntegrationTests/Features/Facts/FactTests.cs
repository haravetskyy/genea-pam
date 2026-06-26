using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Features.Persons.Create;
using GeneaPam.Api.Features.Persons.Get;
using GeneaPam.Api.Features.Trees.Create;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Facts;

public sealed class FactTests(ApiFactory factory) : IntegrationTest(factory)
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

    private async Task<Guid> CreateTreeAsync(string token)
    {
        SetBearer(token);
        var response = await Client.PostAsJsonAsync(
            "/trees",
            new { name = "Smith Family", description = "desc" }
        );
        var body = await response.Content.ReadFromJsonAsync<CreateTreeResponse>();
        return body!.Id;
    }

    // =========================================================
    // FACT WRITE/READ VIA PERSON FLOW
    // =========================================================

    [Fact]
    public async Task CreatePerson_WithBirthFact_RoundTripsThroughGet()
    {
        var token = await RegisterAndLoginAsync("fact_roundtrip@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var create = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[]
                {
                    new
                    {
                        type = "Birth",
                        dateValue = new DateOnly(1900, 3, 4),
                        precision = "FullDate",
                        placeText = "Kraków",
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreatePersonResponse>();

        var get = await Client.GetAsync($"/trees/{treeId}/persons/{created!.Id}");
        var fetched = await get.Content.ReadFromJsonAsync<GetPersonResponse>();

        var fact = Assert.Single(fetched!.Facts);
        Assert.Equal("Birth", fact.Type.Value);
        Assert.Equal(new DateOnly(1900, 3, 4), fact.DateValue);
        Assert.Equal("FullDate", fact.Precision!.Value);
        Assert.Equal("Kraków", fact.PlaceText);
    }

    [Fact]
    public async Task CreatePerson_WithBirthAndDeathFacts_BothPersistedAndRead()
    {
        var token = await RegisterAndLoginAsync("fact_birthdeath@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var create = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[]
                {
                    new { type = "Birth", dateValue = new DateOnly(1900, 1, 1) },
                    new { type = "Death", dateValue = new DateOnly(1970, 1, 1) },
                },
            }
        );
        var created = await create.Content.ReadFromJsonAsync<CreatePersonResponse>();

        var get = await Client.GetAsync($"/trees/{treeId}/persons/{created!.Id}");
        var fetched = await get.Content.ReadFromJsonAsync<GetPersonResponse>();

        Assert.Equal(2, fetched!.Facts.Count);
        Assert.Contains(fetched.Facts, f => f.Type.Value == "Birth");
        Assert.Contains(fetched.Facts, f => f.Type.Value == "Death");
    }

    // =========================================================
    // ENUM ROUND-TRIP IN DB (string storage)
    // =========================================================

    [Fact]
    public async Task FactEnums_StoredAsStringsInDb()
    {
        var token = await RegisterAndLoginAsync("fact_enum_db@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var create = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[]
                {
                    new
                    {
                        type = "Birth",
                        dateValue = new DateOnly(1900, 1, 1),
                        precision = "Approximate",
                    },
                },
            }
        );
        var created = await create.Content.ReadFromJsonAsync<CreatePersonResponse>();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var type = await db
            .Database.SqlQueryRaw<string>(
                "SELECT type AS \"Value\" FROM facts WHERE owner_person_id = {0}",
                created!.Id
            )
            .SingleAsync();
        var precision = await db
            .Database.SqlQueryRaw<string>(
                "SELECT precision AS \"Value\" FROM facts WHERE owner_person_id = {0}",
                created.Id
            )
            .SingleAsync();

        Assert.Equal("Birth", type);
        Assert.Equal("Approximate", precision);
    }

    // =========================================================
    // OWNER CHECK (exactly one owner)
    // =========================================================

    [Fact]
    public async Task FactOwnerCheck_NeitherOwner_RejectedByDb()
    {
        var token = await RegisterAndLoginAsync("fact_no_owner@example.com");
        var treeId = await CreateTreeAsync(token);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO facts (id, tree_id, type, is_primary, created_by, created_at, updated_by, updated_at) "
                    + "VALUES ({0}, {1}, 'Birth', false, 'x', now(), 'x', now())",
                Guid.NewGuid(),
                treeId
            );
        });

        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("ck_facts_exactly_one_owner", ex.Message);
    }

    [Fact]
    public async Task FactOwnerCheck_BothOwners_RejectedByDb()
    {
        var token = await RegisterAndLoginAsync("fact_both_owners@example.com");
        var treeId = await CreateTreeAsync(token);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO facts (id, tree_id, type, owner_person_id, owner_couple_id, is_primary, created_by, created_at, updated_by, updated_at) "
                    + "VALUES ({0}, {1}, 'Birth', {2}, {3}, false, 'x', now(), 'x', now())",
                Guid.NewGuid(),
                treeId,
                Guid.NewGuid(),
                Guid.NewGuid()
            );
        });

        Assert.Equal("23514", ex.SqlState);
        Assert.Contains("ck_facts_exactly_one_owner", ex.Message);
    }

    // =========================================================
    // CASCADE ON PERSON DELETE
    // =========================================================

    [Fact]
    public async Task DeletePerson_CascadesItsFacts()
    {
        var token = await RegisterAndLoginAsync("fact_cascade@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var create = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[] { new { type = "Birth", dateValue = new DateOnly(1900, 1, 1) } },
            }
        );
        var created = await create.Content.ReadFromJsonAsync<CreatePersonResponse>();

        await Client.DeleteAsync($"/trees/{treeId}/persons/{created!.Id}");

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var remaining = await db.Facts.CountAsync(f => f.OwnerPersonId == created.Id);
        Assert.Equal(0, remaining);
    }

    // =========================================================
    // VALIDATOR RULES
    // =========================================================

    [Fact]
    public async Task CreatePerson_DateOnAttributeFact_Returns422()
    {
        var token = await RegisterAndLoginAsync("fact_date_on_attr@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[]
                {
                    new
                    {
                        type = "Occupation",
                        textValue = "Blacksmith",
                        dateValue = new DateOnly(1900, 1, 1),
                    },
                },
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Fact.DateOnAttribute", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreatePerson_OtherFactWithoutCustomLabel_Returns422()
    {
        var token = await RegisterAndLoginAsync("fact_other_no_label@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[] { new { type = "Other", textValue = "Something" } },
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Fact.CustomLabelRequired", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreatePerson_InvalidFactType_Returns422()
    {
        var token = await RegisterAndLoginAsync("fact_bad_type@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[] { new { type = "Coronation" } },
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Fact.TypeInvalid", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreatePerson_TwoBirthFacts_Returns422()
    {
        var token = await RegisterAndLoginAsync("fact_two_births@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[]
                {
                    new { type = "Birth", dateValue = new DateOnly(1900, 1, 1) },
                    new { type = "Birth", dateValue = new DateOnly(1901, 1, 1) },
                },
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Person.DuplicateBirthFact", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreatePerson_OtherFactWithCustomLabel_Returns201()
    {
        var token = await RegisterAndLoginAsync("fact_other_ok@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/persons",
            new
            {
                firstName = "Jane",
                lastName = "Doe",
                facts = new[]
                {
                    new
                    {
                        type = "Other",
                        customLabel = "Baptism",
                        textValue = "St. Mary's",
                    },
                },
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatePersonResponse>();
        var fact = Assert.Single(body!.Facts);
        Assert.Equal("Other", fact.Type.Value);
        Assert.Equal("Baptism", fact.CustomLabel);
    }
}
