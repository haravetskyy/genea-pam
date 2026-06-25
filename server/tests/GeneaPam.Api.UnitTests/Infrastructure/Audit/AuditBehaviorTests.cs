using System.Security.Claims;
using GeneaPam.Api.Infrastructure.Audit;
using Microsoft.AspNetCore.Http;

namespace GeneaPam.Api.UnitTests.Infrastructure.Audit;

public sealed class AuditBehaviorTests
{
    private static IHttpContextAccessor AuthenticatedAccessor(string userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")
        );
        return new HttpContextAccessor { HttpContext = context };
    }

    private sealed class CreateCmd : ICreateCommand
    {
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    private sealed class UpdateCmd : IUpdateCommand
    {
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }

    [Fact]
    public void Before_CreateCommand_StampsCreatedByWithUserId()
    {
        var accessor = AuthenticatedAccessor("user-42");
        var behavior = new AuditBehavior(accessor);
        var cmd = new CreateCmd();

        behavior.Before(cmd);

        Assert.Equal("user-42", cmd.CreatedBy);
    }

    [Fact]
    public void Before_CreateCommand_StampsNonNullCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var accessor = AuthenticatedAccessor("user-42");
        var behavior = new AuditBehavior(accessor);
        var cmd = new CreateCmd();

        behavior.Before(cmd);

        Assert.True(cmd.CreatedAt >= before);
    }

    [Fact]
    public void Before_UpdateCommand_StampsUpdatedByWithUserId()
    {
        var accessor = AuthenticatedAccessor("user-99");
        var behavior = new AuditBehavior(accessor);
        var cmd = new UpdateCmd();

        behavior.Before(cmd);

        Assert.Equal("user-99", cmd.UpdatedBy);
    }

    [Fact]
    public void Before_UpdateCommand_StampsNonNullUpdatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var accessor = AuthenticatedAccessor("user-99");
        var behavior = new AuditBehavior(accessor);
        var cmd = new UpdateCmd();

        behavior.Before(cmd);

        Assert.True(cmd.UpdatedAt >= before);
    }
}
