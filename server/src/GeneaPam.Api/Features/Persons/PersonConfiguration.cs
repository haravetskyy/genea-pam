using GeneaPam.Api.Features.Trees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Persons;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.ToTable("persons");

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.TreeId).HasColumnName("tree_id");
        builder.Property(p => p.FirstName).HasColumnName("first_name").IsRequired();
        builder.Property(p => p.LastName).HasColumnName("last_name").IsRequired();
        builder.Property(p => p.Gender).HasColumnName("gender");
        builder.Property(p => p.BirthDate).HasColumnName("birth_date");
        builder.Property(p => p.BirthDatePrecision).HasColumnName("birth_date_precision");
        builder.Property(p => p.DeathDate).HasColumnName("death_date");
        builder.Property(p => p.DeathDatePrecision).HasColumnName("death_date_precision");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.TreeId);

        builder
            .HasOne<Tree>()
            .WithMany()
            .HasForeignKey(p => p.TreeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
