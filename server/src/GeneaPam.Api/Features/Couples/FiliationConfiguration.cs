using GeneaPam.Api.Features.Persons;
using GeneaPam.Api.Features.Trees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Couples;

public sealed class FiliationConfiguration : IEntityTypeConfiguration<Filiation>
{
    public void Configure(EntityTypeBuilder<Filiation> builder)
    {
        builder.HasKey(f => f.Id);
        builder.ToTable(
            "filiations",
            t =>
            {
                t.HasCheckConstraint(
                    "ck_filiations_no_self_parent",
                    "child_person_id <> parent_person_id"
                );
                t.HasCheckConstraint(
                    "ck_filiations_parentage_type",
                    "parentage_type IN ('Biological','Adoptive','Step','Foster')"
                );
            }
        );

        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.TreeId).HasColumnName("tree_id");
        builder.Property(f => f.ChildPersonId).HasColumnName("child_person_id");
        builder.Property(f => f.ParentPersonId).HasColumnName("parent_person_id");
        builder
            .Property(f => f.ParentageType)
            .HasColumnName("parentage_type")
            .HasConversion(v => v.Value, s => ParentageType.TryParse(s) ?? ParentageType.Biological)
            .IsRequired();
        builder.Property(f => f.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(f => f.TreeId);
        builder
            .HasIndex(f => new { f.ChildPersonId, f.ParentPersonId })
            .IsUnique()
            .HasDatabaseName("ux_filiations_child_parent");

        builder
            .HasOne<Tree>()
            .WithMany()
            .HasForeignKey(f => f.TreeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(f => f.ChildPersonId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(f => f.ParentPersonId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
