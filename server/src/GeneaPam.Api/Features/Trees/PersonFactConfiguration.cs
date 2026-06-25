using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Trees;

public sealed class PersonFactConfiguration : IEntityTypeConfiguration<PersonFact>
{
    public void Configure(EntityTypeBuilder<PersonFact> builder)
    {
        builder.HasKey(p => p.Id);
        builder.ToTable("person_facts");

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TreeId).HasColumnName("tree_id");

        builder
            .HasOne<Tree>()
            .WithMany()
            .HasForeignKey(p => p.TreeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
