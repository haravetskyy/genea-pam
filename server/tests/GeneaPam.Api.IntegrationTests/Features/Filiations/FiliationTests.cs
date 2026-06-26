using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Features.Couples.AddFiliation;
using GeneaPam.Api.Features.Persons.Create;
using GeneaPam.Api.Features.Trees.Create;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Filiations;

public sealed class FiliationTests(ApiFactory factory) : IntegrationTest(factory)
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
            new { firstName, lastName }
        );
        var body = await response.Content.ReadFromJsonAsync<CreatePersonResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task AddFiliation_SingleParent_Returns201WithChildParentEdge()
    {
        var token = await RegisterAndLoginAsync("fil_single@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AddFiliationResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(child, body.ChildPersonId);
        Assert.Equal(parent, body.ParentPersonId);
        Assert.Equal("Biological", body.ParentageType.Value);
    }

    [Fact]
    public async Task AddFiliation_AsymmetricTwoParent_BothPersistWithIndependentParentage()
    {
        var token = await RegisterAndLoginAsync("fil_asym@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var bioParent = await CreatePersonAsync(token, treeId, "Bio", "Smith");
        var stepParent = await CreatePersonAsync(token, treeId, "Step", "Jones");
        SetBearer(token);

        var bio = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new
            {
                childPersonId = child,
                parentPersonId = bioParent,
                parentageType = "Biological",
            }
        );
        var step = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new
            {
                childPersonId = child,
                parentPersonId = stepParent,
                parentageType = "Step",
            }
        );

        Assert.Equal(HttpStatusCode.Created, bio.StatusCode);
        Assert.Equal(HttpStatusCode.Created, step.StatusCode);
        var bioBody = await bio.Content.ReadFromJsonAsync<AddFiliationResponse>();
        var stepBody = await step.Content.ReadFromJsonAsync<AddFiliationResponse>();
        Assert.Equal("Biological", bioBody!.ParentageType.Value);
        Assert.Equal("Step", stepBody!.ParentageType.Value);
        Assert.NotEqual(bioBody.Id, stepBody.Id);
    }

    [Fact]
    public async Task AddFiliation_ChildWithParentsFromTwoCouples_AllPersist()
    {
        // A child can hold parent links from two different couples (e.g. bio + adoptive),
        // because Filiation no longer references a couple at all.
        var token = await RegisterAndLoginAsync("fil_multicouple@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var bioMum = await CreatePersonAsync(token, treeId, "BioMum", "Smith");
        var bioDad = await CreatePersonAsync(token, treeId, "BioDad", "Smith");
        var adoptiveMum = await CreatePersonAsync(token, treeId, "AdoptMum", "Jones");
        SetBearer(token);

        var r1 = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new
            {
                childPersonId = child,
                parentPersonId = bioMum,
                parentageType = "Biological",
            }
        );
        var r2 = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new
            {
                childPersonId = child,
                parentPersonId = bioDad,
                parentageType = "Biological",
            }
        );
        var r3 = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new
            {
                childPersonId = child,
                parentPersonId = adoptiveMum,
                parentageType = "Adoptive",
            }
        );

        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r3.StatusCode);
    }

    [Fact]
    public async Task AddFiliation_InvalidParentageType_Returns422()
    {
        var token = await RegisterAndLoginAsync("fil_badparentage@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new
            {
                childPersonId = child,
                parentPersonId = parent,
                parentageType = "Godparent",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Filiation.ParentageTypeInvalid",
            problem.GetProperty("errorCode").GetString()
        );
    }

    [Fact]
    public async Task AddFiliation_SelfParent_Returns422()
    {
        var token = await RegisterAndLoginAsync("fil_self@example.com");
        var treeId = await CreateTreeAsync(token);
        var person = await CreatePersonAsync(token, treeId, "Solo", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = person, parentPersonId = person }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Filiation.SelfParent", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task AddFiliation_DuplicateChildParent_Returns409()
    {
        var token = await RegisterAndLoginAsync("fil_dup@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);

        var first = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );
        var second = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Filiation.Duplicate", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task AddFiliation_ParentInDifferentTree_Returns422()
    {
        var token = await RegisterAndLoginAsync("fil_crosstree@example.com");
        var treeA = await CreateTreeAsync(token, "Tree A");
        var treeB = await CreateTreeAsync(token, "Tree B");
        var child = await CreatePersonAsync(token, treeA, "Kid", "Smith");
        var parentInB = await CreatePersonAsync(token, treeB, "Mum", "Smith");
        SetBearer(token);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeA}/filiations",
            new { childPersonId = child, parentPersonId = parentInB }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Filiation.PersonNotInTree", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task SelfParent_DbCheckConstraint_RejectsDirectWrite()
    {
        var token = await RegisterAndLoginAsync("fil_dbcheck_self@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);
        await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE filiations SET parent_person_id = child_person_id WHERE child_person_id = {0}",
                child
            );
        });
        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("ck_filiations_no_self_parent", ex.Message);
    }

    [Fact]
    public async Task DeletePerson_AsParent_CascadesFiliation()
    {
        var token = await RegisterAndLoginAsync("del_parent@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);
        await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );

        var delete = await Client.DeleteAsync($"/trees/{treeId}/persons/{parent}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Filiations.AnyAsync(f => f.ParentPersonId == parent));
        // The child Person survives.
        Assert.True(await db.Persons.AnyAsync(p => p.Id == child));
    }

    [Fact]
    public async Task DeletePerson_AsChild_CascadesFiliation()
    {
        var token = await RegisterAndLoginAsync("del_child@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);
        await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );

        var delete = await Client.DeleteAsync($"/trees/{treeId}/persons/{child}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Filiations.AnyAsync(f => f.ChildPersonId == child));
        Assert.True(await db.Persons.AnyAsync(p => p.Id == parent));
    }

    [Fact]
    public async Task DeletePerson_InCouple_CascadesCouple()
    {
        var token = await RegisterAndLoginAsync("del_couple@example.com");
        var treeId = await CreateTreeAsync(token);
        var pA = await CreatePersonAsync(token, treeId, "Alice", "Smith");
        var pB = await CreatePersonAsync(token, treeId, "Bob", "Smith");
        SetBearer(token);
        await Client.PostAsJsonAsync(
            $"/trees/{treeId}/couples",
            new { personAId = pA, personBId = pB }
        );

        var delete = await Client.DeleteAsync($"/trees/{treeId}/persons/{pA}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // The couple they were in is gone; the other partner survives.
        Assert.False(await db.Couples.AnyAsync(c => c.PersonAId == pA || c.PersonBId == pA));
        Assert.True(await db.Persons.AnyAsync(p => p.Id == pB));
    }

    [Fact]
    public async Task DuplicateChildParent_DbUniqueConstraint_RejectsDirectWrite()
    {
        var token = await RegisterAndLoginAsync("fil_dbunique@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        var other = await CreatePersonAsync(token, treeId, "Other", "Smith");
        SetBearer(token);
        await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );
        await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = other }
        );

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Force a duplicate (child, parent) at the DB layer.
        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE filiations SET parent_person_id = {0} WHERE child_person_id = {1} AND parent_person_id = {2}",
                parent,
                child,
                other
            );
        });
        Assert.Equal("23505", ex.SqlState); // unique_violation
        Assert.Contains("ux_filiations_child_parent", ex.Message);
    }

    [Fact]
    public async Task RemoveFiliation_Owner_Returns204_AndDoesNotDeletePersons()
    {
        var token = await RegisterAndLoginAsync("fil_remove@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);
        var add = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );
        var filiationId = (await add.Content.ReadFromJsonAsync<AddFiliationResponse>())!.Id;

        var remove = await Client.DeleteAsync($"/trees/{treeId}/filiations/{filiationId}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        // Both persons survive; the link is gone.
        Assert.Equal(
            HttpStatusCode.OK,
            (await Client.GetAsync($"/trees/{treeId}/persons/{child}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await Client.GetAsync($"/trees/{treeId}/persons/{parent}")).StatusCode
        );
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Filiations.AnyAsync(f => f.Id == filiationId));
    }

    [Fact]
    public async Task RemoveFiliation_NonExistent_Returns404()
    {
        var token = await RegisterAndLoginAsync("fil_remove_404@example.com");
        var treeId = await CreateTreeAsync(token);
        SetBearer(token);

        var response = await Client.DeleteAsync($"/trees/{treeId}/filiations/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddFiliation_CrossUserTree_Returns404()
    {
        var ownerToken = await RegisterAndLoginAsync("fil_xuser_owner@example.com");
        var treeId = await CreateTreeAsync(ownerToken);
        var child = await CreatePersonAsync(ownerToken, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(ownerToken, treeId, "Mum", "Smith");

        var otherToken = await RegisterAndLoginAsync("fil_xuser_other@example.com");
        SetBearer(otherToken);

        var response = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddFiliation_StampsAuditFieldsViaMiddleware()
    {
        var token = await RegisterAndLoginAsync("fil_audit@example.com");
        var treeId = await CreateTreeAsync(token);
        var child = await CreatePersonAsync(token, treeId, "Kid", "Smith");
        var parent = await CreatePersonAsync(token, treeId, "Mum", "Smith");
        SetBearer(token);
        var add = await Client.PostAsJsonAsync(
            $"/trees/{treeId}/filiations",
            new { childPersonId = child, parentPersonId = parent }
        );
        var filiationId = (await add.Content.ReadFromJsonAsync<AddFiliationResponse>())!.Id;

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var filiation = await db.Filiations.AsNoTracking().SingleAsync(f => f.Id == filiationId);

        Assert.False(string.IsNullOrEmpty(filiation.CreatedBy));
        Assert.False(string.IsNullOrEmpty(filiation.UpdatedBy));
        Assert.Equal(filiation.CreatedBy, filiation.UpdatedBy);
        Assert.NotEqual(default, filiation.CreatedAt);
        Assert.NotEqual(default, filiation.UpdatedAt);
    }
}
