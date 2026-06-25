using GeneaPam.Api.Features.Couples;
using GeneaPam.Api.Features.Persons;
using GeneaPam.Api.Features.Trees;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Tree> Trees => Set<Tree>();
    public DbSet<PersonFact> PersonFacts => Set<PersonFact>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Couple> Couples => Set<Couple>();
    public DbSet<Filiation> Filiations => Set<Filiation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
