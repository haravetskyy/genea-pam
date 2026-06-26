using GeneaPam.Api.Features.Persons;
using GeneaPam.Api.Features.Trees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Couples;

public sealed class CoupleConfiguration : IEntityTypeConfiguration<Couple>
{
    public void Configure(EntityTypeBuilder<Couple> builder)
    {
        builder.HasKey(c => c.Id);
        builder.ToTable(
            "couples",
            t =>
                t.HasCheckConstraint(
                    "ck_couples_type",
                    "type IN ('Married','Partners','Separated','Divorced','Other')"
                )
        );

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.TreeId).HasColumnName("tree_id");
        builder.Property(c => c.PersonAId).HasColumnName("person_a_id");
        builder.Property(c => c.PersonBId).HasColumnName("person_b_id");
        builder
            .Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion(v => v.Value, s => CoupleType.TryParse(s)!)
            .IsRequired();
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(c => c.TreeId);

        builder
            .HasOne<Tree>()
            .WithMany()
            .HasForeignKey(c => c.TreeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(c => c.PersonAId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(c => c.PersonBId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
