using GeneaPam.Api.Features.Couples;
using GeneaPam.Api.Features.Persons;
using GeneaPam.Api.Features.Trees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneaPam.Api.Features.Facts;

public sealed class FactConfiguration : IEntityTypeConfiguration<Fact>
{
    public void Configure(EntityTypeBuilder<Fact> builder)
    {
        builder.HasKey(f => f.Id);
        builder.ToTable(
            "facts",
            t =>
            {
                t.HasCheckConstraint(
                    "ck_facts_exactly_one_owner",
                    "(owner_person_id IS NULL) <> (owner_couple_id IS NULL)"
                );
                t.HasCheckConstraint(
                    "ck_facts_type",
                    "type IN ('Birth','Death','Marriage','Separation','Divorce','Occupation','Nationality','Religion','Other')"
                );
                t.HasCheckConstraint(
                    "ck_facts_precision",
                    "precision IS NULL OR precision IN ('FullDate','MonthYear','YearOnly','Approximate')"
                );
            }
        );

        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.TreeId).HasColumnName("tree_id");
        builder
            .Property(f => f.Type)
            .HasColumnName("type")
            .HasConversion(v => v.Value, s => FactType.TryParse(s)!)
            .IsRequired();
        builder.Property(f => f.CustomLabel).HasColumnName("custom_label");
        builder.Property(f => f.OwnerPersonId).HasColumnName("owner_person_id");
        builder.Property(f => f.OwnerCoupleId).HasColumnName("owner_couple_id");
        builder.Property(f => f.DateValue).HasColumnName("date_value");
        builder
            .Property(f => f.Precision)
            .HasColumnName("precision")
            .HasConversion(v => v!.Value, s => DatePrecision.TryParse(s));
        builder.Property(f => f.PlaceText).HasColumnName("place_text");
        builder.Property(f => f.Lat).HasColumnName("lat");
        builder.Property(f => f.Lng).HasColumnName("lng");
        builder.Property(f => f.TextValue).HasColumnName("text_value");
        builder.Property(f => f.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(f => f.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(f => f.TreeId);
        builder.HasIndex(f => f.OwnerPersonId);
        builder.HasIndex(f => f.OwnerCoupleId);

        builder
            .HasOne<Tree>()
            .WithMany()
            .HasForeignKey(f => f.TreeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne<Person>()
            .WithMany()
            .HasForeignKey(f => f.OwnerPersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<Couple>()
            .WithMany()
            .HasForeignKey(f => f.OwnerCoupleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
